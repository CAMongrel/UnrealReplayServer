using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnrealReplayServer.Databases.Models
{
    public class EventEntry
    {
        public string SessionName { get; set; }

        public string GroupName { get; set; }

        public string EventId { get; set; }

        public int Time1 { get; set; }

        public int Time2 { get; set; }

        public string Meta { get; set; }

        public byte[] Data { get; set; }
    }

}
