using System;

namespace Seq.App.EventSchedule.Classes
{
    public class ScheduleCounters
    {
        public DateTime EndTime;
        public int ErrorCount;
        public bool EventLogged;
        public bool IsShowtime;
        public bool IsUpdating;
        public DateTime LastDay;
        public DateTime LastError;
        public DateTime LastLog;
        public DateTime LastUpdate;
        public int LogCount;
        public bool LoggingEvents;
        public int RetryCount;
        public bool SkippedShowtime;
        public DateTime StartTime;
    }
}