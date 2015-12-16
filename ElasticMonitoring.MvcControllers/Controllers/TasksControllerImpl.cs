using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

using RemoteQueue.Handling;

using SKBKontur.Catalogue.Objects;
using SKBKontur.Catalogue.Objects.ValueExtracting;
using SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.MvcControllers.Controllers.ObjectTreeViewBuilding;
using SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.MvcControllers.Controllers.TaskDetailsTreeView;
using SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.MvcControllers.Models;
using SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.TaskIndexedStorage.Client;

namespace SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.MvcControllers.Controllers
{
    public class TasksControllerImpl
    {
        public TasksControllerImpl(ITaskSearchClient taskSearchClient, IRemoteTaskQueue remoteTaskQueue)
        {
            this.taskSearchClient = taskSearchClient;
            this.remoteTaskQueue = remoteTaskQueue;
        }

        public TaskDetailsModel Details(string id, UrlHelper urlHelper, bool currentUserHasAccessToWriteAction, bool currentUserHasAccessToTaskData)
        {
            var taskData = remoteTaskQueue.GetTaskInfo(id);
            return new TaskDetailsModel
                {
                    AllowControlTaskExecution = currentUserHasAccessToWriteAction,
                    TaskName = taskData.Context.Name,
                    TaskId = taskData.Context.Id,
                    State = taskData.Context.State,
                    EnqueueTime = new DateTime(taskData.Context.Ticks, DateTimeKind.Utc),
                    StartExecutedTime = TickToDateTime(taskData.Context.StartExecutingTicks),
                    FinishExecutedTime = TickToDateTime(taskData.Context.FinishExecutingTicks),
                    MinimalStartTime = TickToDateTime(taskData.Context.MinimalStartTicks),
                    ParentTaskId = taskData.Context.ParentTaskId,
                    ChildTaskIds = remoteTaskQueue.GetChildrenTaskIds(taskData.Context.Id),
                    ExceptionInfo = taskData.ExceptionInfos.LastOrDefault().Return(x => x.ExceptionMessageInfo, string.Empty),
                    AttemptCount = taskData.Context.Attempts,
                    DetailsTree = BuildDetailsTree(taskData, id, urlHelper, currentUserHasAccessToTaskData)
                };
        }

        public byte[] GetBytes(string id, string path, out string fileDownloadName)
        {
            var taskData = remoteTaskQueue.GetTaskInfo(id).TaskData;
            var value = ObjectValueExtractor.Extract(taskData.GetType(), taskData, path);
            if(value.GetType() != typeof(byte[]))
                throw new Exception(string.Format("Type of property by path '{0}' has type '{1}' instead of '{2}'", path, value.GetType(), typeof(byte[])));
            fileDownloadName = string.Format("{0}_{1}_{2}.data", DateTime.UtcNow.ToString("yyyy.MM.dd hh:mm:ss"), path, id);
            return (byte[])value;
        }

        private static DateTime? TickToDateTime(long? startExecutingTicks)
        {
            if(!startExecutingTicks.HasValue)
                return null;
            return new DateTime(startExecutingTicks.Value, DateTimeKind.Utc);
        }

        public void Cancel(string id)
        {
            remoteTaskQueue.CancelTask(id);
        }

        public void Rerun(string id)
        {
            remoteTaskQueue.RerunTask(id, TimeSpan.FromTicks(0));
        }

        private static ObjectTreeModel BuildDetailsTree(RemoteTaskInfo taskData, string taskId, UrlHelper urlHelper, bool currentUserHasAccessToTaskData)
        {
            if(!currentUserHasAccessToTaskData)
                return null;
            var builder = new ObjectTreeViewBuilder(new TaskDataBuildersProvider());
            var result = builder.Build(taskData.TaskData, new TaskDataBuildingContext {TaskId = taskId, UrlHelper = urlHelper});
            if(result != null)
                result.Name = "TaskData";
            return result;
        }

        public TaskSearchResultsModel BuildResultsByIteratorContext(string iteratorContext, bool currentUserHasAccessToWriteAction, bool currentUserHasAccessToTaskData)
        {
            var taskSearchResponse = taskSearchClient.SearchNext(iteratorContext);

            var tasksIds = taskSearchResponse.Ids;
            var nextScrollId = taskSearchResponse.NextScrollId;
            return new TaskSearchResultsModel
                {
                    AllowControlTaskExecution = currentUserHasAccessToWriteAction,
                    AllowViewTaskData = currentUserHasAccessToTaskData,
                    Tasks = remoteTaskQueue.GetTaskInfos(tasksIds).Select(x => new TaskModel
                        {
                            Id = x.Context.Id,
                            Name = x.Context.Name,
                            State = x.Context.State,
                            EnqueueTime = FromTicks(x.Context.Ticks),
                            StartExecutionTime = FromTicks(x.Context.StartExecutingTicks),
                            FinishExecutionTime = FromTicks(x.Context.FinishExecutingTicks),
                            MinimalStartTime = FromTicks(x.Context.MinimalStartTicks),
                            AttemptCount = x.Context.Attempts,
                            ParentTaskId = x.Context.ParentTaskId,
                        }).ToArray(),
                    IteratorContext = nextScrollId,
                };
        }

        public TaskSearchResultsModel BuildResultsBySearchConditions(TaskSearchConditionsModel taskSearchConditions, bool currentUserHasAccessToWriteAction, bool currentUserHasAccessToTaskData)
        {
            if(taskSearchConditions.RangeStart == null)
                throw new Exception("Range start should be specified");
            var start = taskSearchConditions.RangeStart.Value.Ticks;
            var end = (taskSearchConditions.RangeEnd ?? DateTime.UtcNow).Ticks;
            var taskSearchResponse = taskSearchClient.SearchFirst(new TaskSearchRequest
                {
                    FromTicksUtc = start,
                    ToTicksUtc = end,
                    QueryString = taskSearchConditions.SearchString,
                    TaskNames = taskSearchConditions.TaskNames,
                    TaskStates = taskSearchConditions.TaskStates
                });

            var tasksIds = taskSearchResponse.Ids;
            var total = taskSearchResponse.TotalCount;
            var nextScrollId = taskSearchResponse.NextScrollId;

            var taskSearchResultsModel = new TaskSearchResultsModel
                {
                    AllowControlTaskExecution = currentUserHasAccessToWriteAction,
                    AllowViewTaskData = currentUserHasAccessToTaskData,
                    Tasks = remoteTaskQueue.GetTaskInfos(tasksIds).Select(x => new TaskModel
                        {
                            Id = x.Context.Id,
                            Name = x.Context.Name,
                            State = x.Context.State,
                            EnqueueTime = FromTicks(x.Context.Ticks),
                            StartExecutionTime = FromTicks(x.Context.StartExecutingTicks),
                            FinishExecutionTime = FromTicks(x.Context.FinishExecutingTicks),
                            MinimalStartTime = FromTicks(x.Context.MinimalStartTicks),
                            AttemptCount = x.Context.Attempts,
                            ParentTaskId = x.Context.ParentTaskId,
                        }).ToArray(),
                    IteratorContext = nextScrollId,
                    TotalResultCount = total
                };
            return taskSearchResultsModel;
        }

        public TasksRerunModel StartRerunTasks(TaskSearchConditionsModel taskSearchConditions)
        {
            if(taskSearchConditions.RangeStart == null)
                throw new Exception("Range start should be specified");
            if(taskSearchConditions.RangeEnd == null)
                throw new Exception("Range start should be specified");
            var taskSearchResponse = taskSearchClient.SearchFirst(new TaskSearchRequest
                {
                    FromTicksUtc = taskSearchConditions.RangeStart.Value.Ticks,
                    ToTicksUtc = (taskSearchConditions.RangeEnd ?? DateTime.UtcNow).Ticks,
                    QueryString = taskSearchConditions.SearchString,
                    TaskNames = taskSearchConditions.TaskNames,
                    TaskStates = taskSearchConditions.TaskStates
                });
            return DoRerunTasks(taskSearchResponse);
        }

        public TasksRerunModel ContinueRerunTasks(string iteratorContext)
        {
            return DoRerunTasks(taskSearchClient.SearchNext(iteratorContext));
        }

        public TasksCancelModel StartCancelTasks(TaskSearchConditionsModel taskSearchConditions)
        {
            if(taskSearchConditions.RangeStart == null)
                throw new Exception("Range start should be specified");
            if(taskSearchConditions.RangeEnd == null)
                throw new Exception("Range start should be specified");
            var taskSearchResponse = taskSearchClient.SearchFirst(new TaskSearchRequest
                {
                    FromTicksUtc = taskSearchConditions.RangeStart.Value.Ticks,
                    ToTicksUtc = (taskSearchConditions.RangeEnd ?? DateTime.UtcNow).Ticks,
                    QueryString = taskSearchConditions.SearchString,
                    TaskNames = taskSearchConditions.TaskNames,
                    TaskStates = taskSearchConditions.TaskStates
                });
            return DoCancelTasks(taskSearchResponse);
        }

        public TasksCancelModel ContinueCancelTasks(string iteratorContext)
        {
            return DoCancelTasks(taskSearchClient.SearchNext(iteratorContext));
        }

        private const int maxTasksToRerun = 10000;
        private const int maxTasksToCancel = 10000;

        private TasksRerunModel DoRerunTasks(TaskSearchResponse taskSearchResponse)
        {
            if(taskSearchResponse.TotalCount > maxTasksToRerun)
                throw new Exception(string.Format("Found too many tasks. Max tasks to rerun = {0}. Please detalize search query", maxTasksToRerun));

            var rerunned = 0;
            var notRerunned = 0;

            foreach(var id in taskSearchResponse.Ids)
            {
                if(remoteTaskQueue.RerunTask(id, TimeSpan.Zero))
                    rerunned++;
                else
                    notRerunned++;
            }

            return new TasksRerunModel
                {
                    IteratorContext = taskSearchResponse.NextScrollId,
                    Rerunned = rerunned,
                    NotRerunned = notRerunned,
                    TotalTasksToRerun = taskSearchResponse.TotalCount
                };
        }

        private TasksCancelModel DoCancelTasks(TaskSearchResponse taskSearchResponse)
        {
            if(taskSearchResponse.TotalCount > maxTasksToCancel)
                throw new Exception(string.Format("Found too many tasks. Max tasks to cancel = {0}. Please detalize search query", maxTasksToCancel));

            var canceled = 0;
            var notCanceled = 0;

            foreach(var id in taskSearchResponse.Ids)
            {
                if(remoteTaskQueue.CancelTask(id))
                    canceled++;
                else
                    notCanceled++;
            }

            return new TasksCancelModel
                {
                    IteratorContext = taskSearchResponse.NextScrollId,
                    Canceled = canceled,
                    NotCanceled = notCanceled,
                    TotalTasksToCancel = taskSearchResponse.TotalCount
                };
        }

        public DateTime? ParseDateTime(string start)
        {
            if(string.IsNullOrWhiteSpace(start))
                return null;
            DateTime result;
            if(!DateTime.TryParse(start, CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.AssumeUniversal, out result))
                return null;
            return result;
        }

        private static DateTime? FromTicks(long? ticks)
        {
            if(ticks.HasValue)
                return new DateTime(ticks.Value, DateTimeKind.Utc);
            return null;
        }

        private readonly ITaskSearchClient taskSearchClient;
        private readonly IRemoteTaskQueue remoteTaskQueue;
    }
}