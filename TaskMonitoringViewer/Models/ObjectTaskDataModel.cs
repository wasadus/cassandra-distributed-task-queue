using System.Web.Mvc;

using SKBKontur.Catalogue.RemoteTaskQueue.TaskMonitoringViewer.RenderingHelpers;

namespace SKBKontur.Catalogue.RemoteTaskQueue.TaskMonitoringViewer.Models
{
    public class ObjectTaskDataModel : ITaskDataValue
    {
        public MvcHtmlString Render(HtmlHelper htmlHelper)
        {
            return htmlHelper.ObjectValue(this);
        }

        public TaskDataProperty[] Properties { get; set; }
    }
}