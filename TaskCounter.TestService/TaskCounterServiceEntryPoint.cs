﻿using RemoteTaskQueue.FunctionalTests.Common;
using RemoteTaskQueue.TaskCounter.Http;
using RemoteTaskQueue.TaskCounter.Scheduler;

using SKBKontur.Catalogue.ServiceLib;
using SKBKontur.Catalogue.ServiceLib.HttpHandlers;
using SKBKontur.Catalogue.ServiceLib.Services;

namespace SKBKontur.Catalogue.RemoteTaskQueue.TaskCounter.TestService
{
    public class TaskCounterServiceEntryPoint : ApplicationBase
    {
        protected override string ConfigFileName { get { return "taskCounterService.csf"; } }

        private static void Main()
        {
            new TaskCounterServiceEntryPoint().Run();
        }

        private void Run()
        {
            Container.ConfigureForTestRemoteTaskQueue();
            Container.Configurator.ForAbstraction<IHttpHandler>().UseType<TaskCounterHttpHandler>();
            Container.Get<ITaskCounterServiceSchedulableRunner>().Start();
            Container.Get<HttpService>().Run();
        }
    }
}