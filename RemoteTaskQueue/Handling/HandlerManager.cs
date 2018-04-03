﻿using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using MoreLinq;

using RemoteQueue.Cassandra.Entities;
using RemoteQueue.Cassandra.Repositories;
using RemoteQueue.Cassandra.Repositories.GlobalTicksHolder;
using RemoteQueue.Cassandra.Repositories.Indexes;
using RemoteQueue.Cassandra.Repositories.Indexes.StartTicksIndexes;
using RemoteQueue.LocalTasks.TaskQueue;
using RemoteQueue.Profiling;
using RemoteQueue.Tracing;

using SKBKontur.Catalogue.Objects;
using SKBKontur.Catalogue.ServiceLib.Logging;

namespace RemoteQueue.Handling
{
    public class HandlerManager : IHandlerManager
    {
        public HandlerManager([NotNull] string taskTopic, int maxRunningTasksCount, ILocalTaskQueue localTaskQueue, IHandleTasksMetaStorage handleTasksMetaStorage, IGlobalTime globalTime)
        {
            this.taskTopic = taskTopic;
            this.maxRunningTasksCount = maxRunningTasksCount;
            this.localTaskQueue = localTaskQueue;
            this.handleTasksMetaStorage = handleTasksMetaStorage;
            this.globalTime = globalTime;
            allTaskIndexShardKeysToRead = allTaskStatesToRead.Select(x => new TaskIndexShardKey(taskTopic, x)).ToArray();
        }

        public string Id => $"HandlerManager_{taskTopic}";

        [NotNull]
        public LiveRecordTicksMarkerState[] GetCurrentLiveRecordTicksMarkers()
        {
            return allTaskIndexShardKeysToRead.Select(x => handleTasksMetaStorage.TryGetCurrentLiveRecordTicksMarker(x) ?? new LiveRecordTicksMarkerState(x, Timestamp.Now.Ticks)).ToArray();
        }

        public void Run()
        {
            var toTicks = Timestamp.Now.Ticks;
            TaskIndexRecord[] taskIndexRecords;
            using(metricsContext.Timer("GetIndexRecords").NewContext())
                taskIndexRecords = handleTasksMetaStorage.GetIndexRecords(toTicks, allTaskIndexShardKeysToRead);
            Log.For(this).Info($"Number of live minimalStartTicksIndex records for topic '{taskTopic}': {taskIndexRecords.Length}");
            foreach(var taskIndexRecordsBatch in taskIndexRecords.Batch(maxRunningTasksCount, Enumerable.ToArray))
            {
                var taskIds = taskIndexRecordsBatch.Select(x => x.TaskId).ToArray();
                Dictionary<string, TaskMetaInformation> taskMetas;
                using(metricsContext.Timer("GetMetas").NewContext())
                    taskMetas = handleTasksMetaStorage.GetMetas(taskIds);
                foreach(var taskIndexRecord in taskIndexRecordsBatch)
                {
                    if(taskMetas.TryGetValue(taskIndexRecord.TaskId, out var taskMeta) && taskMeta.Id != taskIndexRecord.TaskId)
                        throw new InvalidProgramStateException($"taskIndexRecord.TaskId ({taskIndexRecord.TaskId}) != taskMeta.TaskId ({taskMeta.Id})");
                    using(var taskTraceContext = new RemoteTaskHandlingTraceContext(taskMeta))
                    {
                        LocalTaskQueueingResult result;
                        using(metricsContext.Timer("TryQueueTask").NewContext())
                            result = localTaskQueue.TryQueueTask(taskIndexRecord, taskMeta, TaskQueueReason.PullFromQueue, taskTraceContext.TaskIsBeingTraced);
                        taskTraceContext.Finish(result.TaskIsSentToThreadPool, () => globalTime.GetNowTicks());
                        if(result.QueueIsFull)
                        {
                            metricsContext.Meter("QueueIsFull").Mark();
                            return;
                        }
                        if(result.QueueIsStopped)
                        {
                            metricsContext.Meter("QueueIsStopped").Mark();
                            return;
                        }
                    }
                }
            }
        }

        private readonly string taskTopic;
        private readonly int maxRunningTasksCount;
        private readonly ILocalTaskQueue localTaskQueue;
        private readonly IHandleTasksMetaStorage handleTasksMetaStorage;
        private readonly IGlobalTime globalTime;
        private readonly TaskIndexShardKey[] allTaskIndexShardKeysToRead;
        private static readonly TaskState[] allTaskStatesToRead = {TaskState.New, TaskState.WaitingForRerun, TaskState.WaitingForRerunAfterError, TaskState.InProcess};
        private readonly MetricsContext metricsContext = MetricsContext.For(nameof(HandlerManager));
    }
}