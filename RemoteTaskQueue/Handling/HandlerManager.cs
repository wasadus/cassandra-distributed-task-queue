﻿using System;
using System.Linq;

using JetBrains.Annotations;

using MoreLinq;

using RemoteQueue.Cassandra.Entities;
using RemoteQueue.Cassandra.Repositories;
using RemoteQueue.Cassandra.Repositories.GlobalTicksHolder;
using RemoteQueue.Cassandra.Repositories.Indexes;
using RemoteQueue.Cassandra.Repositories.Indexes.StartTicksIndexes;
using RemoteQueue.LocalTasks.TaskQueue;
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
            allTaskIndexShardKeysToRead = allTaskStatesToRead
                .Select(x => string.IsNullOrEmpty(taskTopic) ? TaskIndexShardKey.AnyTaskTopic(x) : new TaskIndexShardKey(taskTopic, x))
                .ToArray();
        }

        public string Id { get { return string.Format("HandlerManager_{0}", taskTopic); } }

        [NotNull]
        public LiveRecordTicksMarkerState[] GetCurrentLiveRecordTicksMarkers()
        {
            return allTaskIndexShardKeysToRead.Select(x => handleTasksMetaStorage.TryGetCurrentLiveRecordTicksMarker(x) ?? new LiveRecordTicksMarkerState(x, Timestamp.Now.Ticks)).ToArray();
        }

        public void Run()
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var taskIndexRecords = handleTasksMetaStorage.GetIndexRecords(nowTicks, allTaskIndexShardKeysToRead);
            Log.For(this).InfoFormat("Number of live minimalStartTicksIndex records for topic '{0}': {1}", taskTopic, taskIndexRecords.Length);
            foreach (var taskIndexRecordsBatch in taskIndexRecords.Batch(maxRunningTasksCount, Enumerable.ToArray))
            {
                var taskMetas = handleTasksMetaStorage.GetMetasQuiet(taskIndexRecordsBatch.Select(x => x.TaskId).ToArray());
                for(var i = 0; i < taskIndexRecordsBatch.Length; i++)
                {
                    var taskMeta = taskMetas[i];
                    var taskIndexRecord = taskIndexRecordsBatch[i];
                    if(taskMeta != null && taskMeta.Id != taskIndexRecord.TaskId)
                        throw new InvalidProgramStateException(string.Format("taskIndexRecord.TaskId ({0}) != taskMeta.TaskId ({1})", taskIndexRecord.TaskId, taskMeta.Id));
                    using(var taskTraceContext = new RemoteTaskHandlingTraceContext(taskMeta))
                    {
                        bool queueIsFull, queueIsStopped, taskIsSentToThreadPool;
                        localTaskQueue.TryQueueTask(taskIndexRecord, taskMeta, TaskQueueReason.PullFromQueue, out queueIsFull, out queueIsStopped, out taskIsSentToThreadPool, taskTraceContext.TaskIsBeingTraced);
                        taskTraceContext.Finish(taskIsSentToThreadPool, () => globalTime.GetNowTicks());
                        if(queueIsFull || queueIsStopped)
                            return;
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
    }
}