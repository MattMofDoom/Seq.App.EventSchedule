﻿using Lurgle.Dates;
using Lurgle.Dates.Classes;
using Seq.App.EventSchedule.Classes;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Timers;

// ReSharper disable MemberCanBePrivate.Global

namespace Seq.App.EventSchedule
{
    [SeqApp("Event Schedule", AllowReprocessing = false,
        Description =
            "Super-powered Seq app to schedule logging an event at given times, with optional repeating log intervals, day of week and day of month inclusion/exclusion, and optional holiday API!")]
    // ReSharper disable once UnusedType.Global
    public class EventScheduleReactor : SeqApp
    {
        private static readonly Dictionary<string, string> LogTokenLookup = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> ResponderLookup = new Dictionary<string, string>();
        private string _alertDescription;
        private string _alertMessage;
        private string _apiKey;
        private bool _bypassLocal;
        private string _country;
        private List<DayOfWeek> _daysOfWeek;
        private bool _diagnostics;
        private string _dueDate;
        private DateTime _endTime;
        private int _errorCount;
        private List<string> _holidayMatch;
        private bool _includeApp;
        private bool _includeBank;
        private bool _includeDescription;
        private bool _includeWeekends;
        private string _initialTimeEstimate;

        private bool _isTags;
        private bool _isUpdating;
        private DateTime _lastDay;
        private DateTime _lastError;
        private DateTime _lastLog;
        private DateTime _lastUpdate;
        private string[] _localAddresses;
        private List<string> _localeMatch;

        private string _priority;
        private string _projectKey;
        private string _proxy;
        private string _proxyPass;
        private string _proxyUser;
        private string _remainingTimeEstimate;
        private bool _repeatSchedule;
        private string _responders;
        private int _retryCount;

        private TimeSpan _scheduleInterval;
        private bool _skippedShowtime;
        private string _startFormat = "H:mm:ss";
        private DateTime _startTime;
        private string[] _tags;
        private string _testDate;
        private LogEventLevel _thresholdLogLevel;
        private Timer _timer;
        private bool _useHolidays;
        private bool _useProxy;
        public bool EventLogged;
        public List<DateTime> ExcludeDays;
        public List<AbstractApiHolidays> Holidays;
        public List<DateTime> IncludeDays;
        public bool IsShowtime;
        public int LogCount;
        public DateTime TestOverrideTime = DateTime.Now;
        public bool UseTestOverrideTime; // ReSharper disable MemberCanBePrivate.Global

        // ReSharper disable UnusedAutoPropertyAccessor.Global
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
        [SeqAppSetting(
            DisplayName = "Diagnostic logging",
            HelpText = "Send extra diagnostic logging to the stream. Recommended to enable.")]
        public bool Diagnostics { get; set; } = true;

        [SeqAppSetting(
            DisplayName = "Schedule time",
            HelpText = "The time (H:mm:ss, 24 hour format) to log an event.")]
        public string ScheduleTime { get; set; }

        [SeqAppSetting(
            DisplayName = "Repeat schedule every X seconds",
            HelpText =
                "By default, schedules are once per day with any day/day of week/day of month modifiers applied. Check this box to DateTokens.Handle logging an event every X seconds.",
            InputType = SettingInputType.Checkbox)]
        // ReSharper disable once RedundantDefaultMemberInitializer
        public bool RepeatSchedule { get; set; } = false;

        [SeqAppSetting(
            DisplayName = "Schedule repeat interval (seconds)",
            HelpText =
                "Time interval for repeating a scheduled event, up to a maximum of 86,400 seconds (24 hours). A log will be created at the scheduled intervals.",
            InputType = SettingInputType.Integer,
            IsOptional = true)]
        public int ScheduleInterval { get; set; } = 60;

        [SeqAppSetting(DisplayName = "Multi-log Token",
            HelpText =
                "Optional comma-delimited list of values (Value, or Value=Long Value) that can be referenced as {LogToken} and {LogTokenLong} in Message, Description, and Tags. If specified, this will create log entries for each value.",
            IsOptional = true, InputType = SettingInputType.LongText)]
        public string MultiLogToken { get; set; }

        [SeqAppSetting(DisplayName = "Log level for scheduled logs",
            HelpText = "Verbose, Debug, Information, Warning, Error, Fatal. Defaults to Information.",
            IsOptional = true)]
        public string ScheduleLogLevel { get; set; }

        [SeqAppSetting(DisplayName = "Priority for scheduled logs",
            HelpText = "Optional Priority property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string Priority { get; set; }

        [SeqAppSetting(DisplayName = "Responders for scheduled logs",
            HelpText =
                "Optional Responders property to pass for scheduled logs, for use with other apps. This can be specified as a comma-delimited key pair to match responders to multi-log tokens, in the format LogToken=Responder.",
            IsOptional = true,
            InputType = SettingInputType.LongText)]
        public string Responders { get; set; }

        [SeqAppSetting(DisplayName = "Project Key for scheduled logs",
            HelpText = "Optional Project Key property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string ProjectKey { get; set; }

        [SeqAppSetting(DisplayName = "Initial Time Estimate for scheduled logs",
            HelpText = "Optional Initial Time Estimate property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string InitialTimeEstimate { get; set; }

        [SeqAppSetting(DisplayName = "Remaining Time Estimate for scheduled logs",
            HelpText = "Optional Remaining Time Estimate property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string RemainingTimeEstimate { get; set; }

        [SeqAppSetting(DisplayName = "Due Date for scheduled logs",
            HelpText = "Optional Due Date property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string DueDate { get; set; }

        [SeqAppSetting(
            DisplayName = "Days of week",
            HelpText = "Comma-delimited - Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday.",
            IsOptional = true)]
        public string DaysOfWeek { get; set; }

        [SeqAppSetting(
            DisplayName = "Include days of month",
            HelpText =
                "Only run on these days. Comma-delimited - first,last,first weekday,last weekday,first-fourth sunday-saturday,1-31.",
            IsOptional = true)]
        public string IncludeDaysOfMonth { get; set; }

        [SeqAppSetting(
            DisplayName = "Exclude days of month",
            HelpText = "Never run on these days. Comma-delimited - first,last,1-31.",
            IsOptional = true)]
        public string ExcludeDaysOfMonth { get; set; }

        [SeqAppSetting(
            DisplayName = "Scheduled log message.",
            HelpText =
                "Event message to raise. Allows tokens for date parts: Day: {d}/{dd}/{ddd}/{dddd}, Month: {M}/{MM}/{MMM}/{MMMM}, Year: {yy}/{yyyy}. These are not case sensitive.")]
        public string AlertMessage { get; set; }

        [SeqAppSetting(
            IsOptional = true,
            DisplayName = "Scheduled log description.",
            HelpText =
                "Optional description associated with the event raised. Allows tokens for date parts: Day: {d}/{dd}/{ddd}/{dddd}, Month: {M}/{MM}/{MMM}/{MMMM}, Year: {yy}/{yyyy}. These are not case sensitive.")]
        public string AlertDescription { get; set; }

        [SeqAppSetting(
            DisplayName = "Include description with log message",
            HelpText =
                "If selected, the configured description will be part of the log message. Otherwise it will only show as a log property, which can be used by other Seq apps.",
            IsOptional = true)]
        public bool? IncludeDescription { get; set; } = false;


        [SeqAppSetting(
            IsOptional = true,
            DisplayName = "Scheduled log tags",
            HelpText =
                "Tags for the event, separated by commas. Allows tokens for date parts: Day: {d}/{dd}/{ddd}/{dddd}, Month: {M}/{MM}/{MMM}/{MMMM}, Year: {yy}/{yyyy}. These are not case sensitive.")]
        public string Tags { get; set; }

        [SeqAppSetting(
            DisplayName = "Include instance name in scheduled log message",
            HelpText = "Prepend the instance name to the scheduled log message.")]
        public bool IncludeApp { get; set; }


        [SeqAppSetting(
            DisplayName = "Holidays - use Holidays API for public holiday detection",
            HelpText = "Connect to the AbstractApi Holidays service to detect public holidays.")]
        public bool UseHolidays { get; set; } = false;

        [SeqAppSetting(
            DisplayName = "Holidays - Retry count",
            HelpText = "Retry count for retrieving the Holidays API. Default 10, minimum 0, maximum 100.",
            InputType = SettingInputType.Integer,
            IsOptional = true)]
        public int RetryCount { get; set; } = 10;

        [SeqAppSetting(
            DisplayName = "Holidays - Country code",
            HelpText = "Two letter country code (eg. AU).",
            IsOptional = true)]
        public string Country { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - API key",
            HelpText = "Sign up for an API key at https://www.abstractapi.com/holidays-api and enter it here.",
            IsOptional = true,
            InputType = SettingInputType.Password)]
        public string ApiKey { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - match these holiday types",
            HelpText =
                "Comma-delimited list of holiday types (eg. National, Local) - case insensitive, partial match okay.",
            IsOptional = true)]
        public string HolidayMatch { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - match these locales",
            HelpText =
                "Holidays are valid if the location matches one of these comma separated values (eg. Australia,New South Wales) - case insensitive, must be a full match.",
            IsOptional = true)]
        public string LocaleMatch { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - include weekends",
            HelpText = "Include public holidays that are returned for weekends.")]
        public bool IncludeWeekends { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - include Bank Holidays.",
            HelpText = "Include bank holidays")]
        public bool IncludeBank { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - test date",
            HelpText = "yyyy-M-d format. Used only for diagnostics - should normally be empty.",
            IsOptional = true)]
        public string TestDate { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - proxy address",
            HelpText = "Proxy address for Holidays API.",
            IsOptional = true)]
        public string Proxy { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - proxy bypass local addresses",
            HelpText = "Bypass local addresses for proxy.")]
        public bool BypassLocal { get; set; } = true;

        [SeqAppSetting(
            DisplayName = "Holidays - local addresses for proxy bypass",
            HelpText = "Local addresses to bypass, comma separated.",
            IsOptional = true)]
        public string LocalAddresses { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - proxy username",
            HelpText = "Username for proxy authentication.",
            IsOptional = true)]
        public string ProxyUser { get; set; }

        [SeqAppSetting(
            DisplayName = "Holidays - proxy password",
            HelpText = "Username for proxy authentication.",
            IsOptional = true,
            InputType = SettingInputType.Password)]


        public string ProxyPass { get; set; }

        protected override void OnAttached()
        {
            LogEvent(LogEventLevel.Debug, "Check {AppName} diagnostic level ({Diagnostics}) ...", App.Title,
                Diagnostics);
            _diagnostics = Diagnostics;

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Check include {AppName} ({IncludeApp}) ...", App.Title, IncludeApp);

            _includeApp = IncludeApp;
            if (!_includeApp && _diagnostics)
                LogEvent(LogEventLevel.Debug, "App name {AppName} will not be included in alert message ...",
                    App.Title);
            else if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "App name {AppName} will be included in alert message ...", App.Title);

            if (!DateTime.TryParseExact(ScheduleTime, "H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out _))
            {
                if (DateTime.TryParseExact(ScheduleTime, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out _))
                    _startFormat = "H:mm";
                else
                    LogEvent(LogEventLevel.Debug,
                        "Start Time {ScheduleTime} does  not parse to a valid DateTime - app will exit ...",
                        ScheduleTime);
            }

            _repeatSchedule = RepeatSchedule;
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Repeat every X seconds: {RepeatSchedule}",
                    _repeatSchedule);

            if (_repeatSchedule)
            {
                if (ScheduleInterval <= 0)
                    ScheduleInterval = 1;
                if (ScheduleInterval > 86400)
                    ScheduleInterval = 60;
                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Convert Repeat Schedule Interval {ScheduleInterval} to TimeSpan ...",
                        ScheduleInterval);
                _scheduleInterval = TimeSpan.FromSeconds(ScheduleInterval);

                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Parsed Repeat Schedule Interval is {Interval} ...",
                        _scheduleInterval.TotalSeconds);
            }

            if (!string.IsNullOrEmpty(MultiLogToken))
            {
                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Convert Multi-Log Tokens to dictionary ...");
                var tokens = (MultiLogToken ?? "")
                    .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();
                foreach (var x in from token in tokens where token.Contains("=") select token.Split('='))
                {
                    LogTokenLookup.Add(x[0], x[1]);
                    if (_diagnostics)
                        LogEvent(LogEventLevel.Debug, "Add mapping for {LogToken} to {LogTokenLong}", x[0], x[1]);
                }
            }

            LogEvent(LogEventLevel.Debug,
                "Use Holidays API {UseHolidays}, Country {Country}, Has API key {IsEmpty} ...", UseHolidays, Country,
                !string.IsNullOrEmpty(ApiKey));
            SetHolidays();
            RetrieveHolidays(DateTime.Today, DateTime.UtcNow);

            if (!_useHolidays || _isUpdating) UtcRollover(DateTime.UtcNow);

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Convert Days of Week {DaysOfWeek} to UTC Days of Week ...", DaysOfWeek);
            _daysOfWeek = Dates.GetUtcDaysOfWeek(DaysOfWeek, ScheduleTime, _startFormat);

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "UTC Days of Week {DaysOfWeek} will be used ...", _daysOfWeek.ToArray());

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Include Days of Month {IncludeDays} ...", IncludeDaysOfMonth);

            IncludeDays = Dates.GetUtcDaysOfMonth(IncludeDaysOfMonth, ScheduleTime, _startFormat, DateTime.Now);
            if (IncludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: {IncludeDays} ...", IncludeDays.ToArray());
            else
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: ALL ...");

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Exclude Days of Month {ExcludeDays} ...", ExcludeDaysOfMonth);

            ExcludeDays = Dates.GetUtcDaysOfMonth(ExcludeDaysOfMonth, ScheduleTime, _startFormat, DateTime.Now);
            if (ExcludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: {ExcludeDays} ...", ExcludeDays.ToArray());
            else
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: NONE ...");

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Alert Message '{AlertMessage}' ...", AlertMessage);

            _alertMessage = string.IsNullOrWhiteSpace(AlertMessage)
                ? "Scheduled event log"
                : AlertMessage.Trim();
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Alert Message '{AlertMessage}' will be used ...", _alertMessage);

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Alert Description '{AlertDescription}' ...", AlertDescription);

            _alertDescription = string.IsNullOrWhiteSpace(AlertDescription)
                ? ""
                : AlertDescription.Trim();
            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Alert Description '{AlertDescription}' will be used ...",
                    _alertDescription);

            if (IncludeDescription != null)
                _includeDescription = (bool) IncludeDescription;

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Include Description in Log Message: '{IncludeDescription}' ...",
                    _includeDescription);

            if (_diagnostics) LogEvent(LogEventLevel.Debug, "Convert Tags '{Tags}' to array ...", Tags);

            _tags = (Tags ?? "")
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToArray();
            if (_tags.Length > 0) _isTags = true;

            if (string.IsNullOrWhiteSpace(ScheduleLogLevel)) ScheduleLogLevel = "Information";
            if (!Enum.TryParse(ScheduleLogLevel, out _thresholdLogLevel))
                _thresholdLogLevel = LogEventLevel.Information;

            if (!string.IsNullOrEmpty(Priority))
                _priority = Priority;

            if (!string.IsNullOrEmpty(Responders))
            {
                if (Responders.Contains('='))
                {
                    LogEvent(LogEventLevel.Debug, "Convert Responders to dictionary ...");
                    var responderList = (Responders ?? "")
                        .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .ToList();
                    foreach (var x in from responder in responderList
                        where responder.Contains("=")
                        select responder.Split('='))
                    {
                        ResponderLookup.Add(x[0], x[1]);
                        if (_diagnostics)
                            LogEvent(LogEventLevel.Debug, "Add mapping for {LogToken} to {Responder}", x[0], x[1]);
                    }
                }
                else
                {
                    _responders = Responders;
                    if (_diagnostics)
                        LogEvent(LogEventLevel.Debug, "Set responder to {Responder}", _responders);
                }
            }

            if (!string.IsNullOrEmpty(ProjectKey))
            {
                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Project Key to {Value}", ProjectKey);
                _projectKey = ProjectKey;
            }

            if (!string.IsNullOrEmpty(InitialTimeEstimate) && DateTokens.ValidDateExpression(InitialTimeEstimate))
            {
                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Initial Time Estimate to {Value}",
                        DateTokens.SetValidExpression(InitialTimeEstimate));
                _initialTimeEstimate = DateTokens.SetValidExpression(InitialTimeEstimate);
            }

            if (!string.IsNullOrEmpty(RemainingTimeEstimate) && DateTokens.ValidDateExpression(RemainingTimeEstimate))
            {
                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Remaining Time Estimate to {Value}",
                        DateTokens.SetValidExpression(RemainingTimeEstimate));
                _remainingTimeEstimate = DateTokens.SetValidExpression(RemainingTimeEstimate);
            }

            if (!string.IsNullOrEmpty(DueDate) &&
                (DateTokens.ValidDateExpression(DueDate) || DateTokens.ValidDate(DueDate)))
            {
                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Due Date to {Value}",
                        DateTokens.ValidDate(DueDate) ? DueDate : DateTokens.SetValidExpression(DueDate));
                _dueDate = DateTokens.ValidDate(DueDate) ? DueDate : DateTokens.SetValidExpression(DueDate);
            }

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug,
                    "Log level {LogLevel} will be used for threshold violations on {Instance} ...",
                    _thresholdLogLevel, App.Title);

            if (_diagnostics) LogEvent(LogEventLevel.Debug, "Starting timer ...");

            _timer = new Timer(1000)
            {
                AutoReset = true
            };
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            if (_diagnostics) LogEvent(LogEventLevel.Debug, "Timer started ...");
        }


        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var timeNow = DateTime.UtcNow;
            var localDate = DateTime.Today;
            if (!string.IsNullOrEmpty(_testDate))
                localDate = DateTime.ParseExact(_testDate, "yyyy-M-d", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);

            if (_lastDay < localDate) RetrieveHolidays(localDate, timeNow);

            //We can only enter showtime if we're not currently retrying holidays, but existing showtimes will continue to monitor
            if ((!_useHolidays || IsShowtime || !IsShowtime && !_isUpdating) && timeNow >= _startTime &&
                timeNow < _endTime)
            {
                if (!IsShowtime && (!_daysOfWeek.Contains(_startTime.DayOfWeek) ||
                                    IncludeDays.Count > 0 && !IncludeDays.Contains(_startTime) ||
                                    ExcludeDays.Contains(_startTime)))
                {
                    //Log that we have skipped a day due to an exclusion
                    if (!_skippedShowtime)
                        LogEvent(LogEventLevel.Debug,
                            "Threshold checking will not be performed due to exclusions - Day of Week Excluded {DayOfWeek}, Day Of Month Not Included {IncludeDay}, Day of Month Excluded {ExcludeDay} ...",
                            !_daysOfWeek.Contains(_startTime.DayOfWeek),
                            IncludeDays.Count > 0 && !IncludeDays.Contains(_startTime),
                            ExcludeDays.Count > 0 && ExcludeDays.Contains(_startTime));

                    _skippedShowtime = true;
                }
                else
                {
                    //Showtime! - Log one or more events as scheduled
                    if (!IsShowtime)
                    {
                        LogEvent(LogEventLevel.Debug,
                            "UTC Start Time {Time} ({DayOfWeek}), logging events as scheduled, until UTC End time {EndTime} ({EndDayOfWeek}) ...",
                            _startTime.ToShortTimeString(), _startTime.DayOfWeek,
                            _endTime.ToShortTimeString(), _endTime.DayOfWeek);
                        IsShowtime = true;
                        _lastLog = timeNow;
                    }

                    var difference = timeNow - _lastLog;
                    //Check the interval time versus threshold count
                    if (!EventLogged || _repeatSchedule && difference.TotalSeconds > _scheduleInterval.TotalSeconds)
                    {
                        if (LogTokenLookup.Any())
                        {
                            //Log multiple events
                            foreach (var token in LogTokenLookup)
                            {
                                var message = DateTokens.HandleTokens(_alertMessage, token);
                                var description = DateTokens.HandleTokens(_alertDescription, token);

                                //Log event
                                ScheduledLogEvent(_thresholdLogLevel, message, description, token);
                            }
                        }
                        else
                        {
                            var message = DateTokens.HandleTokens(_alertMessage);
                            var description = DateTokens.HandleTokens(_alertDescription);

                            //Log event
                            ScheduledLogEvent(_thresholdLogLevel, message, description);
                        }

                        _lastLog = timeNow;
                        EventLogged = true;
                        LogCount++;
                    }
                }
            }
            else if (timeNow < _startTime || timeNow >= _endTime)
            {
                //Showtime can end even if we're retrieving holidays
                if (IsShowtime)
                    LogEvent(LogEventLevel.Debug,
                        "UTC End Time {Time} ({DayOfWeek}), no longer logging scheduled events, total logged {LogCount}  ...",
                        _endTime.ToShortTimeString(), _endTime.DayOfWeek, LogCount);

                //Reset the match counters
                _lastLog = timeNow;
                IsShowtime = false;
                _skippedShowtime = false;
                EventLogged = false;
            }

            //We can only do UTC rollover if we're not currently retrying holidays and it's not during showtime
            if (IsShowtime || _useHolidays && _isUpdating || _startTime > timeNow ||
                !string.IsNullOrEmpty(_testDate)) return;
            UtcRollover(timeNow);
            //Take the opportunity to refresh include/exclude days to allow for month rollover
            IncludeDays = Dates.GetUtcDaysOfMonth(IncludeDaysOfMonth, ScheduleTime, _startFormat, DateTime.Now);
            if (IncludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: {includedays} ...",
                    IncludeDays.ToArray());

            ExcludeDays = Dates.GetUtcDaysOfMonth(ExcludeDaysOfMonth, ScheduleTime, _startFormat, DateTime.Now);
            if (ExcludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: {excludedays} ...",
                    ExcludeDays.ToArray());
        }


        /// <summary>
        ///     Configure Abstract API Holidays for this instance
        /// </summary>
        private void SetHolidays()
        {
            switch (UseHolidays)
            {
                case true when !string.IsNullOrEmpty(Country) && !string.IsNullOrEmpty(ApiKey):
                {
                    if (_diagnostics) LogEvent(LogEventLevel.Debug, "Validate Country {Country}", Country);

                    if (Lurgle.Dates.Holidays.ValidateCountry(Country))
                    {
                        _useHolidays = true;
                        _retryCount = 10;
                        if (RetryCount >= 0 && RetryCount <= 100)
                            _retryCount = RetryCount;
                        _country = Country;
                        _apiKey = ApiKey;
                        _includeWeekends = IncludeWeekends;
                        _includeBank = IncludeBank;

                        if (string.IsNullOrEmpty(HolidayMatch))
                            _holidayMatch = new List<string>();
                        else
                            _holidayMatch = HolidayMatch.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim()).ToList();

                        if (string.IsNullOrEmpty(LocaleMatch))
                            _localeMatch = new List<string>();
                        else
                            _localeMatch = LocaleMatch.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim()).ToList();

                        if (!string.IsNullOrEmpty(Proxy))
                        {
                            _useProxy = true;
                            _proxy = Proxy;
                            _bypassLocal = BypassLocal;
                            _localAddresses = LocalAddresses.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim()).ToArray();
                            _proxyUser = ProxyUser;
                            _proxyPass = ProxyPass;
                        }

                        if (_diagnostics)
                            LogEvent(LogEventLevel.Debug,
                                "Holidays API Enabled: {UseHolidays}, Country {Country}, Use Proxy {UseProxy}, Proxy Address {Proxy}, BypassLocal {BypassLocal}, Authentication {Authentication} ...",
                                _useHolidays, _country,
                                _useProxy, _proxy, _bypassLocal,
                                !string.IsNullOrEmpty(ProxyUser) && !string.IsNullOrEmpty(ProxyPass));

                        WebClient.SetConfig(App.Title, _useProxy, _proxy, _proxyUser, _proxyPass, _bypassLocal,
                            _localAddresses);
                    }
                    else
                    {
                        _useHolidays = false;
                        LogEvent(LogEventLevel.Debug,
                            "Holidays API Enabled: {UseHolidays}, Could not parse country {CountryCode} to valid region ...",
                            _useHolidays, _country);
                    }

                    break;
                }
                case true:
                    _useHolidays = false;
                    LogEvent(LogEventLevel.Debug, "Holidays API Enabled: {UseHolidays}, One or more parameters not set",
                        _useHolidays);
                    break;
            }

            _lastDay = DateTime.Today.AddDays(-1);
            _lastError = DateTime.Now.AddDays(-1);
            _lastUpdate = DateTime.Now.AddDays(-1);
            _errorCount = 0;
            _testDate = TestDate;
            Holidays = new List<AbstractApiHolidays>();
        }

        /// <summary>
        ///     Update AbstractAPI Holidays for this instance, given local and UTC date
        /// </summary>
        /// <param name="localDate"></param>
        /// <param name="utcDate"></param>
        private void RetrieveHolidays(DateTime localDate, DateTime utcDate)
        {
            if (_useHolidays && (!_isUpdating || _isUpdating && (DateTime.Now - _lastUpdate).TotalSeconds > 10 &&
                (DateTime.Now - _lastError).TotalSeconds > 10 && _errorCount < _retryCount))
            {
                _isUpdating = true;
                if (!string.IsNullOrEmpty(_testDate))
                    localDate = DateTime.ParseExact(_testDate, "yyyy-M-d", CultureInfo.InvariantCulture,
                        DateTimeStyles.None);

                if (_diagnostics)
                    LogEvent(LogEventLevel.Debug,
                        "Retrieve holidays for {Date}, Last Update {lastUpdateDate} {lastUpdateTime} ...",
                        localDate.ToShortDateString(), _lastUpdate.ToShortDateString(),
                        _lastUpdate.ToShortTimeString());

                var holidayUrl = WebClient.GetUrl(_apiKey, _country, localDate);
                if (_diagnostics) LogEvent(LogEventLevel.Debug, "URL used is {url} ...", holidayUrl);

                try
                {
                    _lastUpdate = DateTime.Now;
                    var result = WebClient.GetHolidays(_apiKey, _country, localDate).Result;
                    Holidays = Lurgle.Dates.Holidays.ValidateHolidays(result, _holidayMatch, _localeMatch, _includeBank,
                        _includeWeekends);
                    _lastDay = localDate;
                    _errorCount = 0;

                    if (_diagnostics && !string.IsNullOrEmpty(_testDate))
                    {
                        LogEvent(LogEventLevel.Debug,
                            "Test date {testDate} used, raw holidays retrieved {testCount} ...", _testDate,
                            result.Count);
                        foreach (var holiday in result)
                            LogEvent(LogEventLevel.Debug,
                                "Holiday Name: {Name}, Local Name {LocalName}, Start {LocalStart}, Start UTC {Start}, End UTC {End}, Type {Type}, Location string {Location}, Locations parsed {Locations} ...",
                                holiday.Name, holiday.Name_Local, holiday.LocalStart, holiday.UtcStart, holiday.UtcEnd,
                                holiday.Type, holiday.Location, holiday.Locations.ToArray());
                    }

                    LogEvent(LogEventLevel.Debug, "Holidays retrieved and validated {holidayCount} ...",
                        Holidays.Count);
                    foreach (var holiday in Holidays)
                        LogEvent(LogEventLevel.Debug,
                            "Holiday Name: {Name}, Local Name {LocalName}, Start {LocalStart}, Start UTC {Start}, End UTC {End}, Type {Type}, Location string {Location}, Locations parsed {Locations} ...",
                            holiday.Name, holiday.Name_Local, holiday.LocalStart, holiday.UtcStart, holiday.UtcEnd,
                            holiday.Type, holiday.Location, holiday.Locations.ToArray());

                    _isUpdating = false;
                    if (!IsShowtime) UtcRollover(utcDate, true);
                }
                catch (Exception ex)
                {
                    _errorCount++;
                    LogEvent(LogEventLevel.Debug, ex,
                        "Error {Error} retrieving holidays, public holidays cannot be evaluated (Try {Count} of {retryCount})...",
                        ex.Message, _errorCount, _retryCount);
                    _lastError = DateTime.Now;
                }
            }
            else if (!_useHolidays || _isUpdating && _errorCount >= 10)
            {
                _isUpdating = false;
                _lastDay = localDate;
                _errorCount = 0;
                Holidays = new List<AbstractApiHolidays>();
                if (_useHolidays && !IsShowtime) UtcRollover(utcDate, true);
            }
        }

        /// <summary>
        ///     Day rollover based on UTC date
        /// </summary>
        /// <param name="utcDate"></param>
        /// <param name="isUpdateHolidays"></param>
        public void UtcRollover(DateTime utcDate, bool isUpdateHolidays = false)
        {
            LogEvent(LogEventLevel.Debug, "UTC Time is currently {UtcTime} ...",
                UseTestOverrideTime
                    ? TestOverrideTime.ToUniversalTime().ToShortTimeString()
                    : DateTime.Now.ToUniversalTime().ToShortTimeString());

            //Day rollover, we need to ensure the next start and end is in the future
            if (!string.IsNullOrEmpty(_testDate))
                _startTime = DateTime.ParseExact(_testDate + " " + ScheduleTime, "yyyy-M-d " + _startFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
            else if (UseTestOverrideTime)
                _startTime = DateTime
                    .ParseExact(TestOverrideTime.ToString("yyyy-M-d") + " " + ScheduleTime, "yyyy-M-d " + _startFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None)
                    .ToUniversalTime();
            else
                _startTime = DateTime
                    .ParseExact(ScheduleTime, _startFormat, CultureInfo.InvariantCulture, DateTimeStyles.None)
                    .ToUniversalTime();

            //Detect a repeating schedule and DateTokens.Handle it - otherwise end time is 1 hour after start
            _endTime = _repeatSchedule ? _startTime.AddDays(1) : _startTime.AddHours(1);

            //If there are holidays, account for them
            if (Holidays.Any(holiday => _startTime >= holiday.UtcStart && _startTime < holiday.UtcEnd))
            {
                _startTime = _startTime.AddDays(Holidays.Any(holiday =>
                    _startTime.AddDays(1) >= holiday.UtcStart && _startTime.AddDays(1) < holiday.UtcEnd)
                    ? 2
                    : 1);
                _endTime = _endTime.AddDays(_endTime.AddDays(1) < _startTime ? 2 : 1);
            }

            //If we updated holidays or this is a repeating schedule, don't automatically put start time to the future
            if (!RepeatSchedule && (!UseTestOverrideTime && _startTime < utcDate ||
                                    UseTestOverrideTime && _startTime < TestOverrideTime.ToUniversalTime()) &&
                !isUpdateHolidays) _startTime = _startTime.AddDays(1);

            if (_endTime < _startTime)
                _endTime = _endTime.AddDays(_endTime.AddDays(1) < _startTime ? 2 : 1);

            LogEvent(LogEventLevel.Debug,
                isUpdateHolidays
                    ? "UTC Day Rollover (Holidays Updated), Parse {LocalStart} To Next UTC Schedule Time {ScheduleTime} ({StartDayOfWeek}), UTC End Time {EndTime} ({EndDayOfWeek})..."
                    : "UTC Day Rollover, Parse {LocalStart} To Next UTC Start Time {ScheduleTime} ({StartDayOfWeek}), UTC End Time {EndTime} ({EndDayOfWeek})...",
                ScheduleTime, _startTime.ToShortTimeString(), _startTime.DayOfWeek, _endTime.ToShortTimeString(),
                _endTime.DayOfWeek);
        }

        public Showtime GetShowtime()
        {
            return new Showtime(_startTime, _endTime);
        }

        /// <summary>
        ///     Output a scheduled log event that always defines the Message and Description tags for use with other apps
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        /// <param name="description"></param>
        /// <param name="token"></param>
        private void ScheduledLogEvent(LogEventLevel logLevel, string message, string description,
            KeyValuePair<string, string>? token = null)
        {
            string include = "{AppName} - ";
            if (!_includeApp) include = string.Empty;

            var responder = string.Empty;
            if (ResponderLookup.Count > 0)
            {
                if (token != null)
                    foreach (var responderPair in from responderPair in ResponderLookup
                        let tokenPair = (KeyValuePair<string, string>) token
                        where responderPair.Key.Equals(tokenPair.Key, StringComparison.OrdinalIgnoreCase)
                        select responderPair)
                    {
                        responder = responderPair.Value;
                        break;
                    }
            }
            else
            {
                responder = _responders;
            }


            if (_isTags)
                Log.ForContext(nameof(Tags), DateTokens.HandleTokens(_tags, token)).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), _priority).ForContext(nameof(Responders), responder)
                    .ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .ForContext(nameof(LogCount), LogCount).ForContext("Message", message)
                    .ForContext("Description", description).ForContext("MultiLogTokens", LogTokenLookup)
                    .Write((Serilog.Events.LogEventLevel) logLevel,
                        string.IsNullOrEmpty(description) || !_includeDescription
                            ? include + "{Message}"
                            : include + "{Message} : {Description}");
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), _priority)
                    .ForContext(nameof(Responders), responder).ForContext(nameof(LogCount), LogCount)
                    .ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .ForContext("Message", message).ForContext("Description", description)
                    .ForContext("MultiLogTokens", LogTokenLookup)
                    .Write((Serilog.Events.LogEventLevel) logLevel,
                        string.IsNullOrEmpty(description) || !_includeDescription
                            ? include + "{Message}"
                            : include + "{Message} : {Description}");
        }

        /// <summary>
        ///     Output a log event to Seq stream
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void LogEvent(LogEventLevel logLevel, string message, params object[] args)
        {
            string include = "{AppName} - ";
            if (!_includeApp) include = string.Empty;


            if (_isTags)
                Log.ForContext(nameof(Tags), DateTokens.HandleTokens(_tags)).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), _priority).ForContext(nameof(Responders), _responders)
                    .ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .ForContext(nameof(LogCount), LogCount).ForContext("MultiLogTokens", LogTokenLookup)
                    .Write((Serilog.Events.LogEventLevel) logLevel, $"{include}{message}", args);
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), _priority)
                    .ForContext(nameof(Responders), _responders).ForContext(nameof(LogCount), LogCount)
                    .ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .ForContext("MultiLogTokens", LogTokenLookup)
                    .Write((Serilog.Events.LogEventLevel) logLevel, $"{include}{message}", args);
        }

        /// <summary>
        ///     Output an exception log event to Seq stream
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void LogEvent(LogEventLevel logLevel, Exception exception, string message, params object[] args)
        {
            string include = "{AppName} - ";
            if (!_includeApp) include = string.Empty;


            if (_isTags)
                Log.ForContext(nameof(Tags), DateTokens.HandleTokens(_tags)).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), _priority).ForContext(nameof(Responders), _responders)
                    .ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .ForContext(nameof(LogCount), LogCount).ForContext("MultiLogTokens", LogTokenLookup)
                    .Write((Serilog.Events.LogEventLevel) logLevel, exception, $"{include}{message}", args);
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), _priority)
                    .ForContext(nameof(Responders), _responders).ForContext(nameof(LogCount), LogCount)
                    .ForContext(nameof(InitialTimeEstimate), _initialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), _remainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), _projectKey).ForContext(nameof(DueDate), _dueDate)
                    .ForContext("MultiLogTokens", LogTokenLookup)
                    .Write((Serilog.Events.LogEventLevel) logLevel, exception, $"{include}{message}", args);
        }
    }
}