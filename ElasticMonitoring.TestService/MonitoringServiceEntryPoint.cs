﻿using RemoteTaskQueue.FunctionalTests.Common;
using RemoteTaskQueue.Monitoring.Indexer;
using RemoteTaskQueue.Monitoring.Storage;

using SKBKontur.Catalogue.ServiceLib;
using SKBKontur.Catalogue.ServiceLib.Services;

namespace SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.TestService
{
    public class MonitoringServiceEntryPoint : ApplicationBase
    {
        protected override string ConfigFileName { get { return "monitoringService.csf"; } }

        private static void Main()
        {
            new MonitoringServiceEntryPoint().Run();
        }

        private void Run()
        {
            Container.ConfigureForTestRemoteTaskQueue();
            Container.Get<ElasticAvailabilityChecker>().WaitAlive();
            Container.Get<RtqElasticsearchSchema>().ActualizeTemplate(local : true);
            Container.Get<MonitoringServiceSchedulableRunner>().Start();
            Container.Get<HttpService>().Run();
        }
    }
}