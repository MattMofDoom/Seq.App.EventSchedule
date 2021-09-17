using System;

namespace Seq.App.EventSchedule.Classes
{
    public class ScheduleCounters
    {
        public DateTime LastDay;
        public DateTime LastError;
        public DateTime LastLog;
        public DateTime LastUpdate;
        public DateTime EndTime;
        public DateTime StartTime;
        public int ErrorCount;
        public bool IsUpdating;
        public bool SkippedShowtime;
        public bool EventLogged;
        public bool IsShowtime;
        public int LogCount;
        public int RetryCount;
    }
}
