using System;
using System.Collections.Generic;

namespace WebTasksImpl.Models
{
    internal class WebTask
    {
        public WebTask()
        {
            Progresses = new List<WebTaskProgress>();
        }

        public Guid Id { get; set; }
        public WebTaskState State { get; set; }

        public DateTime Created { get; set; }
        public TimeSpan Expiration { get; set; }
        public DateTime? Finished { get; set; }
        public TimeSpan Timeout { get; set; }
        public bool PersistResultWhenPickedOut { get; set; }

        public WebTaskResult Result { get; set; }
        public ICollection<WebTaskProgress> Progresses { get; set; }
        public string Error { get; set; }

    }

    internal static class WebTaskExtensions
    {
        public static bool IsExpired(this WebTask me)
        {
            return me.Finished != null && me.Finished.Value.Add(me.Expiration) < DateTime.UtcNow;
        }
    }
}
