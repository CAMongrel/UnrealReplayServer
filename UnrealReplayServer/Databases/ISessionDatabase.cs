/*
The MIT License (MIT)
Copyright (c) 2021 Henning Thoele
*/

using System.Threading.Tasks;
using UnrealReplayServer.Databases.Models;

namespace UnrealReplayServer.Databases
{
    public interface ISessionDatabase
    {
        Task<bool> AddChunk(string sessionName, SessionFile sessionFile, int totalDemoTimeMs, int totalChunks, int totalBytes);
        Task<string> CreateSession(string setSessionName, string setAppVersion, string setNetVersion, int? setChangelist, string setPlatformFriendlyName);
        Task<Session[]> FindReplays(string app, int? cl, string version, string meta, string user, bool? recent);
        Task<Session[]> FindReplaysByGroup(string group, IEventDatabase eventDatabase);
        Task<Session> GetSessionByName(string sessionName);
        Task<SessionFile> GetSessionHeader(string sessionName);
        Task<SessionFile> GetSessionChunk(string sessionName, int chunkIndex);
        Task<bool> SetHeader(string sessionName, SessionFile sessionFile, int streamChunkIndex, int totalDemoTimeMs);
        Task<bool> SetUsers(string sessionName, string[] users);
        Task StopSession(string sessionName, int totalDemoTimeMs, int totalChunks, int totalBytes);
        Task CheckViewerInactivity();
    }
}