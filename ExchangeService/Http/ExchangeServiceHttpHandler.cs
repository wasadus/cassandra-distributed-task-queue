﻿using System;

using RemoteQueue.Configuration;
using RemoteQueue.Handling;

using SKBKontur.Catalogue.ServiceLib.HttpHandlers;

namespace ExchangeService.Http
{
    public class ExchangeServiceHttpHandler : IHttpHandler
    {
        public ExchangeServiceHttpHandler(IExchangeSchedulableRunner exchangeSchedulableRunner)
        {
            runner = exchangeSchedulableRunner;
        }

        [HttpMethod]
        public void Start()
        {
            runner.Start();
        }

        [HttpMethod]
        public void Stop()
        {
            runner.Stop();
        }

        [HttpMethod]
        public void ChangeTaskTtl(TimeSpan ttl)
        {
            ((RemoteTaskQueue)runner.RemoteTaskQueue).ChangeTaskTtl(ttl);
        }

        private readonly IExchangeSchedulableRunner runner;
    }
}