﻿using System.Collections.Generic;

using JetBrains.Annotations;

using RemoteQueue.Cassandra.Entities;

namespace RemoteQueue.Cassandra.Repositories.BlobStorages
{
    public interface ITaskDataStorage
    {
        [NotNull]
        BlobId Write([NotNull] string taskId, [NotNull] byte[] taskData);

        [CanBeNull]
        byte[] Read([NotNull] TaskMetaInformation taskMeta);

        /// <remarks>
        ///     Return TaskId -> TaskData map. Result does NOT contain entries for non existing blobs
        /// </remarks>
        [NotNull]
        Dictionary<string, byte[]> Read([NotNull] TaskMetaInformation[] taskMetas);
    }
}