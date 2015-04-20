using System;

namespace WebTasksImpl.Models
{
    public class WebTaskConfiguration
    {
        public static readonly TimeSpan DefaultResultExpiration = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan DefaultWebTaskTimeout = TimeSpan.FromMinutes(10);
        public static readonly bool DefaultPersistResultWhenPickedOut = false;

        public WebTaskConfiguration()
        {
            WebTaskTimeout = DefaultWebTaskTimeout;
            ResultExpiration = DefaultResultExpiration;
        }


        public TimeSpan ResultExpiration { get; set; }
        public TimeSpan WebTaskTimeout { get; set; }
        public bool PersistResultWhenPickedOut { get; set; }
    }
}