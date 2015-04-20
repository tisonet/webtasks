using System;
using System.Runtime.Serialization;

namespace WebTasksImpl.Models
{
    [DataContract]
    public class WebTaskStatus
    {
        [DataMember]
        public Guid WebTaskId { get; set; }

        [DataMember]
        public WebTaskState State { get; set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public WebTaskProgress[] Progresses { get; set; }

        [DataMember]
        public WebTaskResult Result { get; set; }
    }
}