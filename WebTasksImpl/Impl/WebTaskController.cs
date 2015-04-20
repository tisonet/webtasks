using System;
using System.Linq;
using System.Threading;
using WebTasksImpl.Contracts;
using WebTasksImpl.Models;

namespace WebTasksImpl.Impl
{
    internal class WebTaskController : IWebTaskActionContext
    {
        private readonly WebTask _webTask;
        private readonly CancellationTokenSource _tokenSource;

        public WebTaskController(WebTask webTask)
        {
            if (webTask == null)
            {
                throw new ArgumentNullException("webTask");
            }

            _webTask = webTask;
            _tokenSource = new CancellationTokenSource();
        }

        public WebTask WebTask
        {
            get { return _webTask; }
        }

        public WebTaskStatus GetStatus()
        {
            return new WebTaskStatus
            {
                WebTaskId = _webTask.Id,
                State = _webTask.State,
                Result = _webTask.Result,
                Progresses = _webTask.Progresses.ToArray(),
                Error = _webTask.Error,
            };
    }

        public void Completed<T>(T result) where T : WebTaskResult
        {
            _webTask.Finished = DateTime.UtcNow;
            _webTask.State = WebTaskState.Finished;
            _webTask.Result = result;

        }

        public void Cancel()
        {
           _tokenSource.Cancel();
        }

        public void Started()
        {
            _webTask.State = WebTaskState.Running;
        }

        public CancellationToken CancellationToken
        {
            get { return _tokenSource.Token; }
        }
        
        public void ReportProgress(WebTaskProgress progress)
        {
           _webTask.Progresses.Add(progress);
        }

        public void Canceled()
        {
            _webTask.Finished = DateTime.UtcNow;
            _webTask.State = WebTaskState.Canceled;
        }

        internal void Faulted(Exception exception)
        {
            //TODO: log exception.

            _webTask.Finished = DateTime.UtcNow;
            _webTask.State = WebTaskState.Failed;
            _webTask.Error = exception.Message;
        }
    }
}
