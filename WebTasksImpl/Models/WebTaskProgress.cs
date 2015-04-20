using System.Runtime.Serialization;

namespace WebTasksImpl.Models
{
    [DataContract]
    public class WebTaskProgress
    {
        [DataMember]
        public int Total { get; set; }

        [DataMember]
        public int Current { get; set; }

        [DataMember]
        public string Message { get; set; }
    }
}