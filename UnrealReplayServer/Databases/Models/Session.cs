/*
The MIT License (MIT)
Copyright (c) 2021 Henning Thoele
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnrealReplayServer.Databases.Models
{
    public class SessionViewer
    {
        public string Username { get; set; } = string.Empty;

        public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.MinValue;
    }

    public class Session
    {
        public bool IsLive { get; set; } = false;

        public string SessionName { get; set; } = string.Empty;

        public string AppVersion { get; set; } = string.Empty;

        public string NetVersion { get; set; } = string.Empty;

        public int Changelist { get; set; } = 0;

        public string Meta { get; set; } = string.Empty;

        public string PlatformFriendlyName { get; set; } = string.Empty;

        public int TotalDemoTimeMs { get; set; } = 0;

        public int TotalUploadedBytes { get; set; } = 0;

        public int TotalChunks { get; set; } = 0;

        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;

        public string[] Users { get; set; } = Array.Empty<string>();

        public Dictionary<string, SessionViewer> Viewers { get; set; } = new Dictionary<string, SessionViewer>();

        public SessionFile HeaderFile { get; set; }

        public List<SessionFile> SessionFiles = new List<SessionFile>();

        internal string AddViewer(string user)
        {
            if (Viewers.ContainsKey(user))
            {
                Viewers.Remove(user);
            }

            Viewers.Add(user, new SessionViewer()
            {
                Username = user,
                LastSeen = DateTimeOffset.UtcNow
            });

            return "Viewer_" + Viewers.Count + "_" + user.Length;
        }

        internal void RefreshViewer(string viewerName, bool final)
        {
            if (Viewers.ContainsKey(viewerName))
            {
                if (final)
                {
                    Viewers.Remove(viewerName);
                }
                else
                {
                    // Update "last seen" timestamp
                    Viewers[viewerName].LastSeen = DateTimeOffset.UtcNow;
                }
            }
        }

        internal void CheckViewersTimeout()
        {
            CheckViewersTimeout(TimeSpan.FromSeconds(30), DateTimeOffset.UtcNow);
        }

        internal void CheckViewersTimeout(TimeSpan maxDeltaTime, DateTimeOffset referenceTime)
        {
            var keys = new string[Viewers.Keys.Count];
            Viewers.Keys.CopyTo(keys, 0);

            foreach (var key in keys)
            {
                var viewer = Viewers[key];
                if (viewer.LastSeen < referenceTime - maxDeltaTime)
                {
                    Viewers.Remove(key);
                    Log($"Removed viewer {viewer.Username} due to inaactivity");
                }
            }
        }

        private void Log(string ling)
        {
            // Empty
        }
    }
}
