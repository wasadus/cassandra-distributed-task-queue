﻿using System;
using System.Linq;

using JetBrains.Annotations;

using SKBKontur.Catalogue.Objects;

namespace RemoteTaskQueue.Monitoring.TaskCounter
{
    public class RtqTaskCounterSettings
    {
        [NotNull]
        public TimeSpan[] BladeDelays { get; set; } =
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(15)
            };

        public TimeSpan DelayBetweenEventFeedingIterations { get; set; } = TimeSpan.FromSeconds(1);

        public TimeSpan StatePersistingInterval { get; set; } = TimeSpan.FromMinutes(1);

        public TimeSpan PendingTaskExecutionUpperBound { get; set; } = TimeSpan.FromMinutes(30);

        public TimeSpan StateGarbageTtl => BladeDelays.Max().Multiply(2);
    }
}