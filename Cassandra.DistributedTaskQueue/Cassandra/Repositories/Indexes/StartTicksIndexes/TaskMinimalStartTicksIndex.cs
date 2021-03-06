﻿using System;
using System.Collections.Generic;

using GroBuf;

using JetBrains.Annotations;

using SkbKontur.Cassandra.DistributedTaskQueue.Handling;
using SkbKontur.Cassandra.ThriftClient.Abstractions;
using SkbKontur.Cassandra.ThriftClient.Clusters;
using SkbKontur.Cassandra.ThriftClient.Connections;
using SkbKontur.Cassandra.TimeBasedUuid;

using Vostok.Logging.Abstractions;

namespace SkbKontur.Cassandra.DistributedTaskQueue.Cassandra.Repositories.Indexes.StartTicksIndexes
{
    public class TaskMinimalStartTicksIndex : ITaskMinimalStartTicksIndex
    {
        public TaskMinimalStartTicksIndex(ICassandraCluster cassandraCluster,
                                          ISerializer serializer,
                                          IRtqSettings rtqSettings,
                                          IOldestLiveRecordTicksHolder oldestLiveRecordTicksHolder,
                                          ILog logger)
        {
            this.cassandraCluster = cassandraCluster;
            this.serializer = serializer;
            this.oldestLiveRecordTicksHolder = oldestLiveRecordTicksHolder;
            this.logger = logger.ForContext(nameof(TaskMinimalStartTicksIndex));
            keyspaceName = rtqSettings.QueueKeyspace;
        }

        [CanBeNull]
        public LiveRecordTicksMarkerState TryGetCurrentLiveRecordTicksMarker([NotNull] TaskIndexShardKey taskIndexShardKey)
        {
            return oldestLiveRecordTicksHolder.TryGetCurrentMarkerValue(taskIndexShardKey)?.State;
        }

        public void AddRecord([NotNull] TaskIndexRecord taskIndexRecord, long timestamp, TimeSpan? ttl)
        {
            oldestLiveRecordTicksHolder.MoveMarkerBackwardIfNecessary(taskIndexRecord.TaskIndexShardKey, taskIndexRecord.MinimalStartTicks);
            DoWriteRecord(taskIndexRecord, timestamp, ttl);
        }

        public void WriteRecord([NotNull] TaskIndexRecord taskIndexRecord, long timestamp, TimeSpan? ttl)
        {
            DoWriteRecord(taskIndexRecord, timestamp, ttl);
        }

        private void DoWriteRecord([NotNull] TaskIndexRecord taskIndexRecord, long timestamp, TimeSpan? ttl)
        {
            var rowKey = CassandraNameHelper.GetRowKey(taskIndexRecord.TaskIndexShardKey, taskIndexRecord.MinimalStartTicks);
            var columnName = CassandraNameHelper.GetColumnName(taskIndexRecord.MinimalStartTicks, taskIndexRecord.TaskId);
            RetrieveColumnFamilyConnection().AddColumn(rowKey, new Column
                {
                    Name = columnName,
                    Timestamp = timestamp,
                    Value = serializer.Serialize(taskIndexRecord.TaskId),
                    TTL = ttl.HasValue ? (int)ttl.Value.TotalSeconds : (int?)null,
                });
        }

        public void RemoveRecord([NotNull] TaskIndexRecord taskIndexRecord, long timestamp)
        {
            var rowKey = CassandraNameHelper.GetRowKey(taskIndexRecord.TaskIndexShardKey, taskIndexRecord.MinimalStartTicks);
            var columnName = CassandraNameHelper.GetColumnName(taskIndexRecord.MinimalStartTicks, taskIndexRecord.TaskId);
            RetrieveColumnFamilyConnection().DeleteColumn(rowKey, columnName, timestamp);
        }

        [NotNull]
        public IEnumerable<TaskIndexRecord> GetRecords([NotNull] TaskIndexShardKey taskIndexShardKey, long toTicks, int batchSize)
        {
            var fromTicks = TryGetFromTicks(taskIndexShardKey, out var liveRecordTicksMarker);
            if (!fromTicks.HasValue)
                return new TaskIndexRecord[0];
            return new GetEventsEnumerable(liveRecordTicksMarker, serializer, RetrieveColumnFamilyConnection(), fromTicks.Value, toTicks, batchSize, logger);
        }

        private long? TryGetFromTicks([NotNull] TaskIndexShardKey taskIndexShardKey, out ILiveRecordTicksMarker liveRecordTicksMarker)
        {
            liveRecordTicksMarker = oldestLiveRecordTicksHolder.TryGetCurrentMarkerValue(taskIndexShardKey);
            if (liveRecordTicksMarker == null)
                return null;
            var overlapDuration = GetOverlapDuration(taskIndexShardKey);
            var fromTicks = liveRecordTicksMarker.State.CurrentTicks - overlapDuration.Ticks;
            var safetyBelt = (Timestamp.Now - TimeSpan.FromHours(6)).Ticks;
            if (fromTicks < safetyBelt)
            {
                logger.Warn("fromTicks ({FromTicks}) < safetyBelt ({SafetyBelt})", new {FromTicks = new Timestamp(fromTicks), SafetyBelt = new Timestamp(safetyBelt)});
                return safetyBelt;
            }
            return fromTicks;
        }

        private TimeSpan GetOverlapDuration([NotNull] TaskIndexShardKey taskIndexShardKey)
        {
            lock (locker)
            {
                var now = Timestamp.Now;
                if (!lastBigOverlapMomentsByShardKey.TryGetValue(taskIndexShardKey, out var lastBigOverlapMoment) || now - lastBigOverlapMoment > TimeSpan.FromMinutes(1))
                {
                    lastBigOverlapMomentsByShardKey[taskIndexShardKey] = now;
                    //Сложно рассчитать математически правильный размер отката, и код постановки таски может измениться,
                    //что потребует изменения этого отката. Поэтому берется, как кажется, с запасом
                    return TimeSpan.FromMinutes(8); // Против адских затупов кассандры
                }
                return TimeSpan.FromMinutes(1); // Штатная зона нестабильности
            }
        }

        [NotNull]
        private IColumnFamilyConnection RetrieveColumnFamilyConnection()
        {
            return cassandraCluster.RetrieveColumnFamilyConnection(keyspaceName, ColumnFamilyName);
        }

        public const string ColumnFamilyName = "TaskMinimalStartTicksIndex";

        private readonly ICassandraCluster cassandraCluster;
        private readonly ISerializer serializer;
        private readonly IOldestLiveRecordTicksHolder oldestLiveRecordTicksHolder;
        private readonly ILog logger;
        private readonly string keyspaceName;
        private readonly object locker = new object();
        private readonly Dictionary<TaskIndexShardKey, Timestamp> lastBigOverlapMomentsByShardKey = new Dictionary<TaskIndexShardKey, Timestamp>();
    }
}