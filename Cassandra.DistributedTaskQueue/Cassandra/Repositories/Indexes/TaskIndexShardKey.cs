using System;

using JetBrains.Annotations;

using SkbKontur.Cassandra.DistributedTaskQueue.Cassandra.Entities;

namespace SkbKontur.Cassandra.DistributedTaskQueue.Cassandra.Repositories.Indexes
{
    public class TaskIndexShardKey : IEquatable<TaskIndexShardKey>
    {
        public TaskIndexShardKey([NotNull] string taskTopic, TaskState taskState)
        {
            TaskTopic = taskTopic;
            TaskState = taskState;
        }

        [NotNull]
        public string TaskTopic { get; private set; }

        public TaskState TaskState { get; private set; }

        public bool Equals(TaskIndexShardKey other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(TaskTopic, other.TaskTopic) && TaskState == other.TaskState;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((TaskIndexShardKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (TaskTopic.GetHashCode() * 397) ^ (int)TaskState;
            }
        }

        public override string ToString()
        {
            return string.Format("TaskTopic: {0}, TaskState: {1}", TaskTopic, TaskState);
        }
    }
}