/*
The MIT License (MIT)
Copyright (c) 2021 Henning Thoele
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnrealReplayServer.Databases.Models;

namespace UnrealReplayServer.Databases
{
    public class EventDatabase : IEventDatabase
    {
        private Dictionary<string, EventEntry> eventList = new Dictionary<string, EventEntry>();
        private Dictionary<string, List<EventEntry>> eventListBySession = new Dictionary<string, List<EventEntry>>();

        public async Task AddEvent(string setSessionName, string group, int? time1, int? time2, string meta, bool? incrementSize, byte[] data)
        {
            string eventName = Guid.NewGuid().ToString("N");
            var newEntry = new EventEntry()
            {
                GroupName = group,
                Meta = meta,
                SessionName = setSessionName,
                Time1 = time1.Value,
                Time2 = time2.Value,
                Data = data,
                EventId = eventName
            };

            eventList.Add(eventName, newEntry);

            if (eventListBySession.ContainsKey(setSessionName) == false)
            {
                eventListBySession.Add(setSessionName, new List<EventEntry>());
            }

            var list = eventListBySession[setSessionName];
            list.Add(newEntry);

            Log("[EVENT ADD] Adding event: " + eventName);
        }

        public async Task UpdateEvent(string setSessionName, string eventName, string group, int? time1, int? time2, string meta, bool? incrementSize, byte[] data)
        {
            if (eventList.ContainsKey(eventName) == false)
            {
                return;
            }

            var entry = eventList[eventName];

            entry.GroupName = group;
            entry.Meta = meta;
            entry.SessionName = setSessionName;
            entry.Time1 = time1.Value;
            entry.Time2 = time2.Value;
            entry.Data = data;

            Log("[EVENT UPDATE] Updating event: " + eventName);
        }

        public async Task<EventEntry[]> GetEventsByGroup(string sessionName, string groupName)
        {
            if (eventListBySession.ContainsKey(sessionName) == false)
            {
                return Array.Empty<EventEntry>();
            }

            return await Task.Run(() =>
            {
                var list = eventListBySession[sessionName];
                var entries = (from ee in list where ee.GroupName == groupName select ee).ToArray();
                return entries;
            });
        }

        public async Task<EventEntry> FindEventByName(string eventName)
        {
            if (eventList.ContainsKey(eventName) == false)
            {
                return null;
            }

            return eventList[eventName];
        }

        public async Task<string[]> FindSessionNamesByGroup(string group)
        {
            return await Task.Run(() =>
            {
                List<string> result = new List<string>();

                foreach (var pair in eventList)
                {
                    if (pair.Value.GroupName == group)
                    {
                        if (result.Contains(pair.Value.SessionName) == false)
                        {
                            result.Add(pair.Value.SessionName);
                        }
                    }
                }

                return result.ToArray();
            });
        }

        private void Log(string line)
        {
            // Empty
        }
    }
}
