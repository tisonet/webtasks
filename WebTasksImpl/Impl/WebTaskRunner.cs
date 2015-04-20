using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using WebTasksImpl.Contracts;
using WebTasksImpl.Models;

namespace WebTasksImpl.Impl
{
    public class WebTaskRunner : IWebTaskRunner
    {
        private readonly WebTaskCleaner _cleaner;
        private readonly ConcurrentDictionary<Guid, WebTaskController> _taskControllers;

        public WebTaskRunner()  : this(TimeSpan.FromMinutes(1))
        {
        }     

        internal WebTaskRunner(TimeSpan cleaningInterval)
        {
            _taskControllers = new ConcurrentDictionary<Guid, WebTaskController>();
         
            _cleaner = new WebTaskCleaner(_taskControllers, cleaningInterval);
        }

        public WebTaskStatus GetWebTaskStatus(Guid taskId)
        {
            WebTaskController controller;

            _taskControllers.TryGetValue(taskId, out controller);

            if (controller != null)
            {
                WebTaskStatus status = controller.GetStatus();

                if (status.State == WebTaskState.Finished && !controller.WebTask.PersistResultWhenPickedOut)
                {
                    _taskControllers.TryRemove(status.WebTaskId, out controller);
                }

                return status;
            }

            return null;
        }

        public void CancelTask(Guid taskId)
        {
            WebTaskController controller;

            _taskControllers.TryGetValue(taskId, out controller);

            if (controller != null)
            {
                controller.Cancel();
            }
        }

        public Guid RunWebTask(Func<WebTaskResult> action)
        {
            WebTask webTask = WebTaskFactory.CreateWebTaskWithDefaultConfiguration();

            RunWebTaskWithoutContext(action, webTask);

            return webTask.Id;
        }

        public Guid RunWebTask(Func<WebTaskResult> action, WebTaskConfiguration configuration) 
        {
            WebTask webTask = WebTaskFactory.CreateWebTask(configuration);

            RunWebTaskWithoutContext(action, webTask);

            return webTask.Id;
        }

        public Guid RunWebTask(Func<IWebTaskActionContext, WebTaskResult> action) 
        {
            WebTask webTask = WebTaskFactory.CreateWebTaskWithDefaultConfiguration();

            RunWebTaskWithContext(action, webTask);

            return webTask.Id;
        }

        public Guid RunWebTask(Func<IWebTaskActionContext, WebTaskResult> action, WebTaskConfiguration configuration)
        {
            WebTask webTask = WebTaskFactory.CreateWebTask(configuration);

            RunWebTaskWithContext(action, webTask);

            return webTask.Id;
        }

        private WebTaskController CreateAndStoreWebTaskController(WebTask webTask)
        {
            WebTaskController controller = new WebTaskController(webTask);

            _taskControllers.TryAdd(webTask.Id, controller);

            return controller;
        }

        private WebTaskResult RunWithoutContext(object state)
        {
            var conf = (Tuple<WebTaskController, Func<WebTaskResult>>)state;

            WebTaskController controller = conf.Item1;
            Func<WebTaskResult> action = conf.Item2;

            controller.Started();

            WebTaskResult result = action();

            return result;
        }

        private void RunWebTaskWithoutContext(Func<WebTaskResult> action, WebTask webTask)
        {
            WebTaskController controller = CreateAndStoreWebTaskController(webTask);

            Task.Factory.StartNew<WebTaskResult>(RunWithoutContext, Tuple.Create(controller, action), controller.CancellationToken)
                .ContinueWith(tpl => FinishWithoutContext(tpl));
        }

        private void RunWebTaskWithContext(Func<IWebTaskActionContext, WebTaskResult> action, WebTask webTask)
        {
            WebTaskController controller = CreateAndStoreWebTaskController(webTask);

            Task.Factory.StartNew<WebTaskResult>(RunWithContext, Tuple.Create(controller, action), controller.CancellationToken)
                .ContinueWith(tpl => FinishWithContext(tpl));
        }

        private WebTaskResult RunWithContext(object state) 
        {
            var conf = (Tuple<WebTaskController, Func<IWebTaskActionContext, WebTaskResult>>)state;

            WebTaskController controller = conf.Item1;
            Func<IWebTaskActionContext, WebTaskResult> action = conf.Item2;

            controller.Started();

            WebTaskResult result = action(controller);

            return result;
        }

        private void FinishWithoutContext(Task<WebTaskResult> tpl)
        {
            var conf = (Tuple<WebTaskController, Func<WebTaskResult>>)tpl.AsyncState;

            WebTaskController controller = conf.Item1;

            SetWebTaskStateOnController(tpl, controller);
        }

        private void FinishWithContext(Task<WebTaskResult> tpl) 
        {
            var conf = (Tuple<WebTaskController, Func<IWebTaskActionContext, WebTaskResult>>)tpl.AsyncState;

            WebTaskController controller = conf.Item1;

            SetWebTaskStateOnController(tpl, controller);
        }

        private static void SetWebTaskStateOnController(Task<WebTaskResult> tpl, WebTaskController controller)
        {
            if (tpl.Status == TaskStatus.Faulted)
            {
                controller.Faulted(tpl.Exception.InnerException ?? tpl.Exception);
            }
            else if (tpl.Status == TaskStatus.Canceled)
            {
                controller.Canceled();
            }
            else if (tpl.IsCompleted)
            {
                controller.Completed(tpl.Result);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private class WebTaskCleaner
        {
            private readonly ConcurrentDictionary<Guid, WebTaskController> _controllersStorage;

            private readonly Timer _timer = new Timer();

            public WebTaskCleaner(ConcurrentDictionary<Guid, WebTaskController> controllersStorage, TimeSpan cleaningInterval)
            {
                if (controllersStorage == null)
                {
                    throw new ArgumentNullException("controllersStorage");
                }

                _controllersStorage = controllersStorage;

                _timer = new Timer(cleaningInterval.TotalMilliseconds);
                _timer.Elapsed += ClearExpired;
                _timer.Start();
            }

            private void ClearExpired(object sender, ElapsedEventArgs elapsedEventArgs)
            {
                _timer.Stop();

                WebTask[] currentWebTask = _controllersStorage.Values.Select(ctrl => ctrl.WebTask).ToArray();

                List<Guid> expiredWebTaskIds = new List<Guid>();

                foreach (var webTask in currentWebTask)
                {
                    if (webTask.IsExpired())
                    {
                        expiredWebTaskIds.Add(webTask.Id);
                        // TODO:log  expired, will be del;eted
                    }
                }

                foreach (var expiredWebTaskId in expiredWebTaskIds)
                {
                    WebTaskController ctrl;
                    _controllersStorage.TryRemove(expiredWebTaskId, out ctrl);
                }

                _timer.Start();
            }
        }

    }
}