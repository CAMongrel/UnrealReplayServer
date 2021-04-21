/*
The MIT License (MIT)
Copyright (c) 2021 Henning Thoele
*/

using System.Threading.Tasks;
using UnrealReplayServer.Databases.Models;

namespace UnrealReplayServer.Databases
{
    public interface IEventDatabase
    {
        Task AddEvent(string setSessionName, string group, int? time1, int? time2, string meta, bool? incrementSize, byte[] data);
        Task<EventEntry[]> GetEventsByGroup(string sessionName, string groupName);
        Task UpdateEvent(string setSessionName, string eventName, string group, int? time1, int? time2, string meta, bool? incrementSize, byte[] data);
        Task<EventEntry> FindEventByName(string eventName);
        Task<string[]> FindSessionNamesByGroup(string group);
    }
}