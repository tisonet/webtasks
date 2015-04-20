using System;
using WebTasksImpl.Models;

namespace WebTasksImpl.Contracts
{
    public interface IWebTaskRunner
    {
        /// <summary>
        /// Run a new webtask with default configuration.
        /// Webtask cannot be canceled, and cannot sent progress.
        /// </summary>
        /// <param name="action">Webtask body to execute.</param>
        /// <returns>Returns webtask id, which is used for getting webtask status.</returns>
        Guid RunWebTask(Func<WebTaskResult> action);

        /// <summary>
        /// Run a new webtask with a given configuration.
        /// Webtask cannot be canceled, and can not sent progress.
        /// </summary>
        /// <param name="action">Webtask body to execute.</param>
        /// <param name="configuration">Web task configuration.</param>
        /// <returns>Returns webtask id, which is used for getting webtask status.</returns>      
        Guid RunWebTask(Func<WebTaskResult> action, WebTaskConfiguration configuration);

        /// <summary>
        /// Run a new webtask with a default configuration.
        /// Webtask can be canceled and can sent progress by using context <see cref="IWebTaskActionContext"/>.
        /// </summary>
        /// <param name="action">Webtask body to execute.</param>
        /// <returns>Returns webtask id, which is used for getting webtask status.</returns>      
        Guid RunWebTask(Func<IWebTaskActionContext, WebTaskResult> action);

        /// <summary>
        /// Run a new webtask with a given configuration.
        /// Webtask can be canceled and can sent progress by using context <see cref="IWebTaskActionContext"/>.
        /// </summary>
        /// <param name="action">Webtask body to execute.</param>
        /// <param name="configuration">Web task configuration.</param>
        /// <returns>Returns webtask id, which is used for getting webtask status.</returns>   
        Guid RunWebTask(Func<IWebTaskActionContext, WebTaskResult> action, WebTaskConfiguration configuration);

        /// <summary>
        /// Return status of webtask.
        /// </summary>
        /// <param name="taskId">Task id.</param>
        /// <returns></returns>
        WebTaskStatus GetWebTaskStatus(Guid taskId);

        /// <summary>
        /// Cancel a webtask.
        /// Cacelling is not immediately, it can takes some time. 
        /// </summary>
        /// <param name="taskId"></param>
        void CancelTask(Guid taskId);
    }


}
