using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnrealReplayServer.Databases.Models;

namespace UnrealReplayServer.Databases
{
    public class SessionDatabase : ISessionDatabase
    {
        private Dictionary<string, Session> SessionList = new Dictionary<string, Session>();

        public async Task<string> CreateSession(string setSessionName, string setAppVersion, string setNetVersion, int? setChangelist,
            string setPlatformFriendlyName)
        {
            if (SessionList.ContainsKey(setSessionName))
            {
                SessionList.Remove(setSessionName);
            }

            Session newSession = new Session()
            {
                AppVersion = setAppVersion,
                NetVersion = setNetVersion,
                PlatformFriendlyName = setPlatformFriendlyName,
                Changelist = setChangelist != null ? setChangelist.Value : 0,
                SessionName = setSessionName
            };
            SessionList.Add(newSession.SessionName, newSession);
            return newSession.SessionName;
        }

        public async Task<Session> GetSessionByName(string sessionName)
        {
            if (SessionList.ContainsKey(sessionName))
            {
                return SessionList[sessionName];
            }
            return null;
        }

        public async Task<SessionFile> GetSessionHeader(string sessionName)
        {
            if (SessionList.ContainsKey(sessionName))
            {
                return SessionList[sessionName].HeaderFile;
            }
            return null;
        }

        public async Task<SessionFile> GetSessionChunk(string sessionName, int chunkIndex)
        {
            if (SessionList.ContainsKey(sessionName))
            {
                var session = SessionList[sessionName];
                if (chunkIndex >= 0 && chunkIndex < session.SessionFiles.Count)
                {
                    return session.SessionFiles[chunkIndex];
                }
            }
            return null;
        }

        public async Task<bool> SetUsers(string sessionName, string[] users)
        {
            if (SessionList.ContainsKey(sessionName) == false)
            {
                LogError($"Session {sessionName} not found");
                return false;
            }

            var session = SessionList[sessionName];
            session.Users = users;

            return true;
        }

        public async Task<bool> SetHeader(string sessionName, SessionFile sessionFile, int streamChunkIndex, int totalDemoTimeMs)
        {
            if (SessionList.ContainsKey(sessionName) == false)
            {
                LogError($"Session {sessionName} not found");
                return false;
            }

            var session = SessionList[sessionName];
            session.HeaderFile = sessionFile;
            session.TotalDemoTimeMs = totalDemoTimeMs;

            Log($"[HEADER] Stats for {sessionName}: TotalDemoTimeMs={session.TotalDemoTimeMs}");

            return true;
        }

        public async Task<bool> AddChunk(string sessionName, SessionFile sessionFile, int totalDemoTimeMs, int totalChunks, int totalBytes)
        {
            if (SessionList.ContainsKey(sessionName) == false)
            {
                LogError($"Session {sessionName} not found");
                return false;
            }

            var session = SessionList[sessionName];
            session.TotalDemoTimeMs = totalDemoTimeMs;
            session.TotalChunks = totalChunks;
            session.TotalUploadedBytes = totalBytes;
            session.SessionFiles.Add(sessionFile);

            Log($"[CHUNK] Stats for {sessionName}: TotalDemoTimeMs={session.TotalDemoTimeMs}, TotalChunks={session.TotalChunks}, " +
                $"TotalUploadedBytes={session.TotalUploadedBytes}");

            return true;
        }

        public async Task StopSession(string sessionName, int totalDemoTimeMs, int totalChunks, int totalBytes)
        {
            if (SessionList.ContainsKey(sessionName) == false)
            {
                LogError($"Session {sessionName} not found");
                return;
            }

            var session = SessionList[sessionName];
            session.TotalDemoTimeMs = totalDemoTimeMs;
            session.TotalChunks = totalChunks;
            session.TotalUploadedBytes = totalBytes;

            Log($"[END] Stats for {sessionName}: TotalDemoTimeMs={session.TotalDemoTimeMs}, TotalChunks={session.TotalChunks}, " +
                $"TotalUploadedBytes={session.TotalUploadedBytes}");
        }

        public async Task<Session[]> FindReplaysByGroup(string group, IEventDatabase eventDatabase)
        {
            return await Task.Run(async () =>
            {
                var sessionNames = await eventDatabase.FindSessionNamesByGroup(group);
                if (sessionNames == null ||
                    sessionNames.Length == 0)
                {
                    return Array.Empty<Session>();
                }

                List<Session> sessions = new List<Session>();

                for (int i = 0; i < sessionNames.Length; i++)
                {
                    if (SessionList.ContainsKey(sessionNames[i]))
                    {
                        sessions.Add(SessionList[sessionNames[i]]);
                    }
                }

                return sessions.ToArray();
            });
        }

        public async Task<Session[]> FindReplays(string app, int? cl, string version, string meta, string user, bool? recent)
        {
            return await Task.Run(() =>
            {
                List<Session> sessions = new List<Session>();

                var values = SessionList.Values;
                foreach (var entry in values)
                {
                    bool shouldAdd = true;
                    if (app != null)
                    {
                        shouldAdd &= entry.AppVersion == app;
                    }
                    if (cl != null)
                    {
                        shouldAdd &= entry.Changelist == cl;
                    }
                    if (version != null)
                    {
                        shouldAdd &= entry.NetVersion == version;
                    }
                    if (user != null)
                    {
                        shouldAdd &= entry.Users.Contains(user);
                    }

                    if (shouldAdd)
                    {
                        sessions.Add(entry);
                    }
                }

                return sessions.ToArray();
            });
        }

        public async Task CheckViewerInactivity()
        {
            await Task.Run(() =>
            {
                var sessions = new Session[SessionList.Count];
                SessionList.Values.CopyTo(sessions, 0);

                for (int i = 0; i < sessions.Length; i++)
                {
                    sessions[i].CheckViewersTimeout();
                }
            });
        }

        private void Log(string line)
        {
            // Empty
        }

        private void LogError(string line)
        {
            // Empty
        }
    }
}
