﻿using System;

using RemoteQueue.Handling;
using RemoteQueue.Handling.HandlerResults;

using SKBKontur.Catalogue.RemoteTaskQueue.TaskDatas;

namespace ExchangeService.UserClasses
{
    public class FakePeriodicTaskHandler : TaskHandler<FakePeriodicTaskData>
    {
        public FakePeriodicTaskHandler(ITestCounterRepository testCounterRepository)
        {
            this.testCounterRepository = testCounterRepository;
        }

        protected override HandleResult HandleTask(FakePeriodicTaskData taskData)
        {
            testCounterRepository.IncrementCounter(Context.Id);
            return Rerun(TimeSpan.FromSeconds(0.1));
        }

        private readonly ITestCounterRepository testCounterRepository;
    }
}