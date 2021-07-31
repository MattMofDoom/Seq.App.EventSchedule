using System;
using Seq.Apps;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedParameter.Global

namespace Seq.App.EventSchedule.Tests.Support
{
    public static class Some
    {
        public static string String()
        {
            return Guid.NewGuid().ToString();
        }

        public static uint Uint()
        {
            return 5417u;
        }

        public static uint EventType()
        {
            return Uint();
        }

        public static string EventId()
        {
            return "event-" + String();
        }

        public static DateTime UtcTimestamp()
        {
            return DateTime.UtcNow;
        }

        public static Host Host()
        {
            return new Host("https://seq.example.com", String());
        }

        public static EventScheduleReactor Reactor(string start, int repeatInterval, bool repeat = false, string dayOfMonth = null)
        {
            return new EventScheduleReactor
            {
                Diagnostics = true,
                ScheduleTime = start,
                RepeatSchedule = repeat,
                ScheduleInterval = repeatInterval,
                ScheduleLogLevel = "Information",
                IncludeDaysOfMonth = dayOfMonth,
                Priority = "P3",
                Responders = "Everyone Ever",
                AlertMessage = "An alert!",
                AlertDescription = "An alert has arisen!",
                Tags = "Alert,Message",
                IncludeApp = true
            };
        }

        public abstract class ParametersMustBeNamed
        {
        }
    }
}