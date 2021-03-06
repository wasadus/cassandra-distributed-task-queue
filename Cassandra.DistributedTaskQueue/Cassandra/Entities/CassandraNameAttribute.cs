using System;

using JetBrains.Annotations;

namespace SkbKontur.Cassandra.DistributedTaskQueue.Cassandra.Entities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class CassandraNameAttribute : Attribute
    {
        public CassandraNameAttribute([NotNull] string name)
        {
            Name = name;
        }

        [NotNull]
        public string Name { get; private set; }
    }
}