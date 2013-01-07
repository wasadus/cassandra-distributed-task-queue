using RemoteQueue.Handling;

namespace SKBKontur.Catalogue.RemoteTaskQueue.TaskDatas.MonitoringTestTaskData
{
    public class BetaTaskData : ITaskData
    {
        public string QueueId { get; set; }
        public bool IsProcess { get; set; }
        public string OwnTaskId { get; set; }
    }
}