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
using UnrealReplayServer.Models;

namespace UnrealReplayServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventController : ControllerBase
    {
        private readonly ILogger<EventController> _logger;

        private ISessionDatabase sessionDatabase = null;
        private IEventDatabase eventDatabase = null;

        public EventController(ILogger<EventController> logger, ISessionDatabase setSessionDatabase, IEventDatabase setEventDatabase)
        {
            _logger = logger;
            
            sessionDatabase = setSessionDatabase;
            eventDatabase = setEventDatabase;
        }

        #region Searching
        [HttpGet]
        public async Task<SearchReplaysResponse> SearchForReplaysByEvent(string group)
        {
            _logger.LogInformation("EventController.SearchForReplaysByEvent: group: " + group);

            var result = new SearchReplaysResponse();

            var replayList = await sessionDatabase.FindReplaysByGroup(group, eventDatabase);

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

        [HttpGet("{eventName}")]
        public async Task<byte[]> RequestReplay(string eventName)
        {
            _logger.LogInformation("EventController.RequestReplays: eventName: " + eventName);

            var eventEntry = await eventDatabase.FindEventByName(eventName);
            if (eventEntry == null)
            {
                return null;
            }

            Response.ContentType = "application/octet-stream";
            Response.ContentLength = eventEntry.Data.Length;

            Response.StatusCode = (int)HttpStatusCode.OK;

            return eventEntry.Data;
        }
        #endregion
    }
}
