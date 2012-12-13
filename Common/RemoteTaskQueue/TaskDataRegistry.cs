﻿using System;
using System.Collections.Generic;

using RemoteQueue.Handling;
using RemoteQueue.UserClasses;

using SKBKontur.Catalogue.RemoteTaskQueue.TaskDatas;

namespace SKBKontur.Catalogue.RemoteTaskQueue.Common.RemoteTaskQueue
{
    public class TaskDataRegistry : ITaskDataRegistry
    {
        public TaskDataRegistry()
        {
            Register<FakeFailTaskData>("FakeFailTaskData");
            Register<FakePeriodicTaskData>("FakePeriodicTaskData");
        }

        public KeyValuePair<Type, string>[] GetAllTaskDataInfos()
        {
            return list.ToArray();
        }

        private void Register<T>(string taskName) where T : ITaskData
        {
            list.Add(new KeyValuePair<Type, string>(typeof(T), taskName));
        }

        private readonly List<KeyValuePair<Type, string>> list = new List<KeyValuePair<Type, string>>();
    }
}