using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using JetBrains.Annotations;

using SKBKontur.Catalogue.Core.Sharding.Hashes;

namespace RemoteQueue.Configuration
{
    public class TaskTopicResolver : ITaskTopicResolver
    {
        public TaskTopicResolver(ITaskDataRegistry taskDataRegistry)
        {
            foreach(var taskName in taskDataRegistry.GetAllTaskNames())
                nameToTopic.Add(taskName, ResolveTopic(taskName));
        }

        [NotNull]
        private static string ResolveTopic([NotNull] string taskName)
        {
            return (Math.Abs(taskName.GetPersistentHashCode()) % topicsCount).ToString(CultureInfo.InvariantCulture);
        }

        [NotNull]
        public string[] GetAllTaskTopics()
        {
            return nameToTopic.Values.Distinct().ToArray();
        }

        [NotNull]
        public string GetTaskTopic([NotNull] string taskName)
        {
            return nameToTopic[taskName];
        }

        private const int topicsCount = 2;
        private readonly Dictionary<string, string> nameToTopic = new Dictionary<string, string>();
    }
}