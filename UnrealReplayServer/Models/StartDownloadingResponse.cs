using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnrealReplayServer.Models
{
    public class StartDownloadingResponse
    {
        public string State { get; set; } = string.Empty;

        public int NumChunks { get; set; } = 0;

        public int Time { get; set; } = 0;

        public string ViewerId { get; set; } = string.Empty;
    }
}
