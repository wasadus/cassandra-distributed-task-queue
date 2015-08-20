﻿using GroBuf;

using RemoteQueue.Cassandra.Repositories;
using RemoteQueue.Cassandra.Repositories.Indexes.StartTicksIndexes;
using RemoteQueue.Profiling;

using SKBKontur.Catalogue.CassandraPrimitives.RemoteLock;

namespace RemoteQueue.Handling
{
    public interface IRemoteTaskQueueInternals
    {
        ISerializer Serializer { get; }
        ITaskMinimalStartTicksIndex TaskMinimalStartTicksIndex { get; }
        IHandleTasksMetaStorage HandleTasksMetaStorage { get; }
        IHandleTaskCollection HandleTaskCollection { get; }
        IHandleTaskExceptionInfoStorage HandleTaskExceptionInfoStorage { get; }
        IRemoteLockCreator RemoteLockCreator { get; }
        IRemoteTaskQueueProfiler RemoteTaskQueueProfiler { get; }
        IRemoteTaskQueue RemoteTaskQueue { get; }
    }
}