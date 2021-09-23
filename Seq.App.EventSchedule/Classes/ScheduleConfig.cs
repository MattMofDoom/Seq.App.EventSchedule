using System;
using System.Collections.Generic;
using Lurgle.Dates.Classes;
using Lurgle.Dates.Enums;
using Seq.Apps.LogEvents;

namespace Seq.App.EventSchedule.Classes
{
    public class ScheduleConfig
    {
        public readonly Dictionary<string, string> LogTokenLookup = new Dictionary<string, string>();
        public readonly Dictionary<string, string> ResponderLookup = new Dictionary<string, string>();
        public string ApiKey;
        public bool BypassLocal;
        public string Country;
        public List<DayOfWeek> DaysOfWeek = new List<DayOfWeek>();
        public bool Diagnostics;
        public string DueDate;

        public List<DateTime> ExcludeDays;

        public List<string> HolidayMatch = new List<string>();
        public List<AbstractApiHolidays> Holidays = new List<AbstractApiHolidays>();
        public bool IncludeApp;
        public bool IncludeBank;
        public List<DateTime> IncludeDays;
        public bool IncludeDescription;
        public bool IncludeWeekends;
        public string InitialTimeEstimate;

        public bool IsTags;

        public string[] LocalAddresses = Array.Empty<string>();
        public List<string> LocaleMatch = new List<string>();
        public List<MonthOfYear> MonthsOfYear = new List<MonthOfYear>();

        public string Priority;
        public string ProjectKey;
        public string Proxy;
        public string ProxyPass;
        public string ProxyUser;
        public string RemainingTimeEstimate;
        public bool RepeatSchedule;
        public string Responders;


        public TimeSpan ScheduleInterval;
        public LogEventLevel ScheduleLogLevel;
        public string StartFormat = "H:mm:ss";

        public string[] Tags = Array.Empty<string>();
        public string TestDate;
        public DateTime TestOverrideTime = DateTime.Now;

        public bool UseHolidays;
        public bool UseProxy;
        public bool UseTestOverrideTime;

        public string AppName { get; set; }
        public bool UseHandlebars { get; set; }
    }
}