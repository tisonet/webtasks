using System.Threading;
using WebTasksImpl.Models;

namespace WebTasksImpl.Contracts
{
    public interface IWebTaskActionContext
    {
        CancellationToken CancellationToken { get; }
        void ReportProgress(WebTaskProgress progress);
    }
}