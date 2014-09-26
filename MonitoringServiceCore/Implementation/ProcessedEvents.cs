using System.Collections.Generic;

using RemoteQueue.Cassandra.Entities;

namespace SKBKontur.Catalogue.RemoteTaskQueue.MonitoringServiceCore.Implementation
{
    public class ProcessedEvents : IProcessedEvents
    {
        public void AddEvents(IEnumerable<TaskMetaUpdatedEvent> events)
        {
            lock(set)
            {
                foreach (var elementaryEvent in events)
                    set.Add(elementaryEvent);
            }
        }

        public void RemoveEvents(long threshold)
        {
            lock(set)
            {
                set.RemoveWhere(e => e.Ticks < threshold);
            }
        }

        public bool Contains(TaskMetaUpdatedEvent elementaryEvent)
        {
            lock (set)
            {
                return set.Contains(elementaryEvent);
            }
        }

        public int GetCount()
        {
            lock(set)
            {
                return set.Count;
            }
        }

        public void Clear()
        {
            lock(set)
            {
                set.Clear();
            }
        }

        private readonly HashSet<TaskMetaUpdatedEvent> set = new HashSet<TaskMetaUpdatedEvent>();
    }
}