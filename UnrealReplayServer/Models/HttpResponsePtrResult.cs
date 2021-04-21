using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnrealReplayServer.Models
{
    public class ResultEventEntry
    {
        public string Id { get; set; }

        public string Group { get; set; }

        public string Meta { get; set; }

        public int Time1 { get; set; }

        public int Time2 { get; set; }
    }

    public class HttpResponsePtrResult
    {
        public ResultEventEntry[] Events { get; set; } = Array.Empty<ResultEventEntry>();
    }
}
