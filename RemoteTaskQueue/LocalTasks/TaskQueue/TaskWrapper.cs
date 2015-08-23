using System;

using JetBrains.Annotations;

using log4net;

using RemoteQueue.Handling;

namespace RemoteQueue.LocalTasks.TaskQueue
{
    internal class TaskWrapper
    {
        public TaskWrapper([NotNull] string taskId, TaskQueueReason taskQueueReason, bool taskIsBeingTraced, [NotNull] HandlerTask handlerTask, [NotNull] LocalTaskQueue localTaskQueue)
        {
            this.taskId = taskId;
            this.taskQueueReason = taskQueueReason;
            this.taskIsBeingTraced = taskIsBeingTraced;
            this.handlerTask = handlerTask;
            this.localTaskQueue = localTaskQueue;
            finished = false;
        }

        public bool Finished { get { return finished; } }

        public void Run()
        {
            LocalTaskProcessingResult result;
            try
            {
                result = handlerTask.RunTask();
            }
            catch(Exception e)
            {
                result = LocalTaskProcessingResult.Undefined;
                logger.Error("������ �� ����� ��������� ����������� ������.", e);
            }
            try
            {
                finished = true;
                localTaskQueue.TaskFinished(taskId, taskQueueReason, taskIsBeingTraced, result);
            }
            catch(Exception e)
            {
                logger.Warn("������ �� ����� ��������� ������.", e);
            }
        }

        private readonly string taskId;
        private readonly TaskQueueReason taskQueueReason;
        private readonly bool taskIsBeingTraced;
        private readonly HandlerTask handlerTask;
        private readonly LocalTaskQueue localTaskQueue;
        private volatile bool finished;
        private readonly ILog logger = LogManager.GetLogger(typeof(LocalTaskQueue));
    }
}