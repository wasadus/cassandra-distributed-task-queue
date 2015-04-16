using RemoteQueue.Handling;
using RemoteQueue.Handling.HandlerResults;

using SKBKontur.Catalogue.RemoteTaskQueue.TaskDatas;

namespace ExchangeService.UserClasses
{
    public class ByteArrayAndNestedTaskHandler : TaskHandler<ByteArrayAndNestedTaskData>
    {
        protected override HandleResult HandleTask(ByteArrayAndNestedTaskData taskData)
        {
            return Finish();
        }
    }
}