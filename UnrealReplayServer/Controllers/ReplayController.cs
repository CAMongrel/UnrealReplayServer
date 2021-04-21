/*
The MIT License (MIT)
Copyright (c) 2021 Henning Thoele
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnrealReplayServer.Databases;
using UnrealReplayServer.Databases.Models;
using UnrealReplayServer.Models;

namespace UnrealReplayServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReplayController : ControllerBase
    {
        private readonly ILogger<ReplayController> _logger;

        private ISessionDatabase sessionDatabase = null;
        private IEventDatabase eventDatabase = null;

        public ReplayController(ILogger<ReplayController> logger, ISessionDatabase setSessionDatabase, IEventDatabase setEventDatabase)
        {
            _logger = logger;

            sessionDatabase = setSessionDatabase;
            eventDatabase = setEventDatabase;
        }

        #region Uploading
        [HttpPost]
        public async Task<StartSessionResponse> PostStartSession(string app, string version, int? cl, string friendlyName)
        {
            string session = Guid.NewGuid().ToString("N");

            _logger.LogInformation($"ReplayController.PostStartSession -- session: {session}, app: {app}, version: {version}, cl: {cl}, friendlyName: {friendlyName}");

            var sessionId = await sessionDatabase.CreateSession(session, app, version, cl, friendlyName);

            return new StartSessionResponse()
            {
                SessionId = sessionId
            };
        }

        [HttpPost("{session}")]
        public async Task<StartSessionResponse> PostStartSession(string session, string app, string version, int? cl, string friendlyName)
        {
            _logger.LogInformation($"ReplayController.PostStartSession -- session: {session}, app: {app}, version: {version}, cl: {cl}, friendlyName: {friendlyName}");

            var sessionId = await sessionDatabase.CreateSession(session, app, version, cl, friendlyName);

            return new StartSessionResponse()
            {
                SessionId = sessionId
            };
        }

        [HttpPost("{session}/stopUploading")]
        public async Task<StartSessionResponse> PostStopStreaming(string session, int numChunks, int time, int absSize)
        {
            _logger.LogInformation($"ReplayController.PostStopStreaming -- session: {session}, numChunks: {numChunks}, time: {time}, absSize: {absSize}");

            await sessionDatabase.StopSession(session, time, numChunks, absSize);

            byte[] data = null;
            using (var ms = new MemoryStream(2048))
            {
                await Request.Body.CopyToAsync(ms);
                data = ms.ToArray();  // returns base64 encoded string JSON result
            }

            _logger.LogDebug($"    data: {data.Length}");

            return null;
        }

        [HttpPost("{session}/users")]
        public async Task<StartSessionResponse> PostUsers(string session, [FromBody] SessionUserList userList)
        {
            _logger.LogInformation($"ReplayController.PostUsers -- session: {session}");

            if (userList != null)
            {
                _logger.LogDebug($"ReplayController.PostUsers -- userList: {userList.Users.Length}");
                foreach (var usr in userList.Users)
                {
                    _logger.LogDebug($"    {usr}");
                }

                await sessionDatabase.SetUsers(session, userList.Users.ToArray());
            }
            else
            {
                _logger.LogDebug($"ReplayController.PostUsers -- userList: <null>");

                await sessionDatabase.SetUsers(session, Array.Empty<string>());
            }

            return null;
        }

        [HttpPost("{session}/file/{filename}")]
        public async Task<StartSessionResponse> PostFile(string session, string filename, int? numChunks, int? time, 
            int? mTime1, int? mTime2, int? absSize)
        {
            _logger.LogInformation($"ReplayController.PostFile -- session: {session}, filename: {filename}, numChunks: {numChunks}, time: {time}" +
                $", mTime1: {mTime1}, mTime2: {mTime2}, absSize: {absSize}");

            byte[] data = null;
            using (var ms = new MemoryStream((int)Request.ContentLength))
            {
                await Request.Body.CopyToAsync(ms);
                data = ms.ToArray();  // returns base64 encoded string JSON result
            }

            _logger.LogDebug($"    data: {data.Length}");

            if (filename.ToLowerInvariant() == "replay.header")
            {
                SessionFile sessionFile = new SessionFile()
                {
                    Data = data,
                    Filename = filename
                };
                await sessionDatabase.SetHeader(session, sessionFile, numChunks.Value, time.Value);
            }
            else if (filename.ToLowerInvariant().StartsWith("stream."))
            {
                if (int.TryParse(filename.ToLowerInvariant().Substring("stream.".Length), out int chunkIndex) == true)
                {
                    SessionFile sessionFile = new SessionFile()
                    {
                        Data = data,
                        Filename = filename,
                        StartTimeMs = mTime1.Value,
                        EndTimeMs = mTime2.Value,
                        ChunkIndex = chunkIndex
                    };
                    await sessionDatabase.AddChunk(session, sessionFile, time.Value, numChunks.Value, absSize.Value);
                }
            }

            return null;
        }

        [HttpPost("{session}/event")]
        public async Task<StartSessionResponse> PostAddEvent(string session, string group, int? time1, int? time2, string meta, bool? incrementSize)
        {
            _logger.LogInformation($"ReplayController.PostAddEvent -- session: {session}, group: {group}, time1: {time1}, time2: {time2}" +
                $", meta: {meta}, incrementSize: {incrementSize}");

            byte[] data = null;
            using (var ms = new MemoryStream((int)Request.ContentLength))
            {
                await Request.Body.CopyToAsync(ms);
                data = ms.ToArray();  // returns base64 encoded string JSON result
            }

            _logger.LogDebug($"    data: {data.Length}");

            await eventDatabase.AddEvent(session, group, time1, time2, meta, incrementSize, data);

            return null;
        }

        [HttpPost("{session}/event/{session2}_{eventName}")]
        public async Task<StartSessionResponse> PostUpdateEvent(string session, string session2, string eventName, string group, int? time1, int? time2, string meta, bool? incrementSize)
        {
            _logger.LogInformation($"ReplayController.PostUpdateEvent -- session: {session}" +
                $", session2: {session2}, eventName: {eventName}" +
                $", group: {group}, time1: {time1}, time2: {time2}" +
                $", meta: {meta}, incrementSize: {incrementSize}");

            byte[] data = null;
            using (var ms = new MemoryStream((int)Request.ContentLength))
            {
                await Request.Body.CopyToAsync(ms);
                data = ms.ToArray();  // returns base64 encoded string JSON result
            }

            _logger.LogDebug($"    data: {data.Length}");

            await eventDatabase.UpdateEvent(session2, eventName, group, time1, time2, meta, incrementSize, data);

            return null;
        }
        #endregion

        #region Downloading
        [HttpPost("{sessionName}/startDownloading")]
        public async Task<StartDownloadingResponse> StartDownloading(string sessionName, string user)
        {
            _logger.LogInformation($"ReplayController.StartDownloading -- sessionName: {sessionName}" +
                $", user: {user}");

            Session session = await sessionDatabase.GetSessionByName(sessionName);
            if (session == null)
            {
                return null;
            }

            string viewerId = session.AddViewer(user);

            var resp = new StartDownloadingResponse()
            {
                NumChunks = session.TotalChunks,
                State = string.Empty,
                Time = session.TotalDemoTimeMs,
                ViewerId = viewerId
            };

            return resp;
        }

        [HttpPost("{sessionName}/viewer/{viewerName}")]
        public async Task<StartDownloadingResponse> ViewerHeartbeat(string sessionName, string viewerName, bool? final)
        {
            _logger.LogInformation($"ReplayController.ViewerHeartbeat -- sessionName: {sessionName}" +
                $", viewerName: {viewerName}, final: {final}");

            Session session = await sessionDatabase.GetSessionByName(sessionName);
            if (session == null)
            {
                return null;
            }

            session.RefreshViewer(viewerName, final != null && final.Value == true);

            return null;
        }

        [HttpGet("{sessionName}/file/replay.header")]
        public async Task<byte[]> GetHeaderFile(string sessionName)
        {
            _logger.LogInformation($"ReplayController.GetHeaderFile -- sessionName: {sessionName}, filename: replay.header");

            byte[] resultData = null;

            var headerFile = await sessionDatabase.GetSessionHeader(sessionName);
            if (headerFile != null)
            {
                resultData = headerFile.Data;

                Response.ContentType = "application/octet-stream";
                Response.ContentLength = resultData.Length;

                Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            return resultData;
        }

        [HttpGet("{sessionName}/file/stream.{chunkIndex}")]
        public async Task<byte[]> GetStreamChunkFile(string sessionName, int? chunkIndex)
        {
            _logger.LogInformation($"ReplayController.GetFile -- sessionName: {sessionName}, filename: {chunkIndex}");

            byte[] resultData = null;

            Response.StatusCode = (int)HttpStatusCode.NotFound;

            if (chunkIndex != null)
            {
                var session = await sessionDatabase.GetSessionByName(sessionName);
                var sessionFile = await sessionDatabase.GetSessionChunk(sessionName, chunkIndex.Value);
                if (session != null && 
                    sessionFile != null)
                {
                    resultData = sessionFile.Data;

                    Response.Headers.Add("NumChunks", session.TotalChunks.ToString());
                    Response.Headers.Add("Time", session.TotalDemoTimeMs.ToString());
                    Response.Headers.Add("State", session.IsLive ? "Live" : string.Empty);
                    Response.Headers.Add("MTime1", session.IsLive ? throw new NotImplementedException() : "0");
                    Response.Headers.Add("MTime2", sessionFile.EndTimeMs.ToString());

                    Response.ContentType = "application/octet-stream";
                    Response.ContentLength = resultData.Length;

                    Response.StatusCode = (int)HttpStatusCode.OK;
                }
            }

            return resultData;
        }

        [HttpGet("{sessionName}/event")]
        public async Task<HttpResponsePtrResult> GetGroup(string sessionName, string group)
        {
            _logger.LogInformation($"ReplayController.GetGroup -- sessionName: {sessionName}, group: {group}");

            var result = new HttpResponsePtrResult();

            var events = await eventDatabase.GetEventsByGroup(sessionName, group);

            result.Events = new ResultEventEntry[events.Length];
            for (int i = 0; i < events.Length; i++)
            {
                result.Events[i] = new ResultEventEntry()
                {
                    Group = events[i].GroupName,
                    Meta = events[i].Meta,
                    Time1 = events[i].Time1,
                    Time2 = events[i].Time2,
                    Id = events[i].EventId
                };
            }

            return result;
        }
        #endregion

        #region Searching
        [HttpGet]
        public async Task<SearchReplaysResponse> SearchReplays(string app, int? cl, string version, string meta, string user, bool? recent)
        {
            var result = new SearchReplaysResponse();

            var replayList = await sessionDatabase.FindReplays(app, cl, version, meta, user, recent);

            result.Replays = new SearchReplaysResponse.SearchReplaysResponseEntry[replayList.Length];
            for (int i = 0; i < replayList.Length; i++)
            {
                var repl = replayList[i];
                result.Replays[i] = new SearchReplaysResponse.SearchReplaysResponseEntry()
                {
                    App = repl.AppVersion,
                    bIsLive = repl.IsLive,
                    Changelist = repl.Changelist,
                    DemoTimeInMs = repl.TotalDemoTimeMs,
                    FriendlyName = repl.PlatformFriendlyName,
                    NumViewers = repl.Viewers.Count,
                    SessionName = repl.SessionName,
                    SizeInBytes = repl.TotalUploadedBytes,
                    Timestamp = repl.CreationDate.UtcDateTime,
                    shouldKeep = false,
                };
            }

            return result;
        }
        #endregion
    }
}
