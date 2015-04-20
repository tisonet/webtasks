using System;
using WebTasksImpl.Models;

namespace WebTasksImpl.Impl
{
    internal static class WebTaskFactory
    {
        public static WebTask CreateWebTask(WebTaskConfiguration configuration)
        {
            WebTask webTask = new WebTask
            {
                Id = Guid.NewGuid(),
                State = WebTaskState.Idle,
                Created = DateTime.UtcNow,
                Expiration = configuration.ResultExpiration,
                Timeout = configuration.WebTaskTimeout ,
                PersistResultWhenPickedOut = configuration.PersistResultWhenPickedOut
            };

            return webTask;
        }


        public static WebTask CreateWebTaskWithDefaultConfiguration()
        {
            return CreateWebTask(new WebTaskConfiguration
            {
                WebTaskTimeout = WebTaskConfiguration.DefaultWebTaskTimeout,
                ResultExpiration =  WebTaskConfiguration.DefaultResultExpiration,
                PersistResultWhenPickedOut = WebTaskConfiguration.DefaultPersistResultWhenPickedOut,
            });
        }
    }
}
