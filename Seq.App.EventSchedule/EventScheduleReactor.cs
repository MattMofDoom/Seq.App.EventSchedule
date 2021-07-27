using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using Seq.App.EventSchedule.Classes;
using Seq.Apps;
using Seq.Apps.LogEvents;

// ReSharper disable MemberCanBePrivate.Global

namespace Seq.App.EventSchedule
{
    [SeqApp("Event Schedule", AllowReprocessing = false,
        Description =
            "Super-powered Seq app to schedule logging an event at given times, with optional repeating log intervals, day of week and day of month inclusion/exclusion, and optional holiday API!")]
    // ReSharper disable once UnusedType.Global
    public class EventScheduleReactor : SeqApp
    {
        private string _alertDescription;
        private string _alertMessage;
        private string _apiKey;
        private bool _bypassLocal;
        private string _country;
        private List<DayOfWeek> _daysOfWeek;
        private bool _diagnostics;
        private DateTime _endTime;
        private int _errorCount;
        private List<int> _excludeDays;
        private List<string> _holidayMatch;
        private bool _includeApp;
        private bool _includeBank;
        private List<int> _includeDays;
        private bool _includeDescription;
        private bool _includeWeekends;

        private bool _isTags;
        private bool _isUpdating;
        private DateTime _lastDay;
        private DateTime _lastError;
        private DateTime _lastLog;
        private DateTime _lastUpdate;
        private string[] _localAddresses;

        private List<string> _localeMatch;

        private string _priority;
        private string _proxy;
        private string _proxyPass;
        private string _proxyUser;
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
        public List<AbstractApiHolidays> Holidays;
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
                "By default, schedules are once per day with any day/day of week/day of month modifiers applied. Check this box to handle logging an event every X seconds.",
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

        [SeqAppSetting(DisplayName = "Log level for scheduled logs",
            HelpText = "Verbose, Debug, Information, Warning, Error, Fatal. Defaults to Information.",
            IsOptional = true)]
        public string ScheduleLogLevel { get; set; }

        [SeqAppSetting(DisplayName = "Priority for scheduled logs",
            HelpText = "Optional Priority property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string Priority { get; set; }

        [SeqAppSetting(DisplayName = "Responders for scheduled logs",
            HelpText = "Optional Responders property to pass for scheduled logs, for use with other apps.",
            IsOptional = true)]
        public string Responders { get; set; }

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
                "If selected, the configured description will be part of the log message. Otherwise it will only show as a log property, which can be used by other Seq apps.")]
        public bool IncludeDescription { get; set; } = false;

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

            LogEvent(LogEventLevel.Debug,
                "Use Holidays API {UseHolidays}, Country {Country}, Has API key {IsEmpty} ...", UseHolidays, Country,
                !string.IsNullOrEmpty(ApiKey));
            SetHolidays();
            RetrieveHolidays(DateTime.Today, DateTime.UtcNow);

            if (!_useHolidays || _isUpdating) UtcRollover(DateTime.UtcNow);

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Convert Days of Week {DaysOfWeek} to UTC Days of Week ...", DaysOfWeek);
            _daysOfWeek = Dates.GetDaysOfWeek(DaysOfWeek, ScheduleTime, _startFormat);

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "UTC Days of Week {DaysOfWeek} will be used ...", _daysOfWeek.ToArray());

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Include Days of Month {IncludeDays} ...", IncludeDaysOfMonth);

            _includeDays = Dates.GetDaysOfMonth(IncludeDaysOfMonth, ScheduleTime, _startFormat);
            if (_includeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: {IncludeDays} ...", _includeDays.ToArray());
            else
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: ALL ...");

            if (_diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Exclude Days of Month {ExcludeDays} ...", ExcludeDaysOfMonth);

            _excludeDays = Dates.GetDaysOfMonth(ExcludeDaysOfMonth, ScheduleTime, _startFormat);
            if (_excludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: {ExcludeDays} ...", _excludeDays.ToArray());
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

            _includeDescription = IncludeDescription;
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
                _responders = Responders;

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
                                    _includeDays.Count > 0 && !_includeDays.Contains(_startTime.Day) ||
                                    _excludeDays.Contains(_startTime.Day)))
                {
                    //Log that we have skipped a day due to an exclusion
                    if (!_skippedShowtime)
                        LogEvent(LogEventLevel.Debug,
                            "Threshold checking will not be performed due to exclusions - Day of Week Excluded {DayOfWeek}, Day Of Month Not Included {IncludeDay}, Day of Month Excluded {ExcludeDay} ...",
                            !_daysOfWeek.Contains(_startTime.DayOfWeek),
                            _includeDays.Count > 0 && !_includeDays.Contains(_startTime.Day),
                            _excludeDays.Count > 0 && _excludeDays.Contains(_startTime.Day));

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
                        var message = HandleTokens(_alertMessage);
                        var description = HandleTokens(_alertDescription);

                        //Log event
                        ScheduledLogEvent(_thresholdLogLevel, message, description);

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
            _includeDays = Dates.GetDaysOfMonth(IncludeDaysOfMonth, ScheduleTime, _startFormat);
            if (_includeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: {includedays} ...",
                    _includeDays.ToArray());

            _excludeDays = Dates.GetDaysOfMonth(ExcludeDaysOfMonth, ScheduleTime, _startFormat);
            if (_excludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: {excludedays} ...",
                    _excludeDays.ToArray());
        }

        private static IEnumerable<string> HandleTokens(IEnumerable<string> values)
        {
            return values.Select(HandleTokens).ToList();
        }

        public static string HandleTokens(string value)
        {
            var replaceValue = GetDateExpressionToken(value);
            var replaceParams = new List<string>
            {
                "{d}",
                "{dd}",
                "{ddd}",
                "{dddd}",
                "{M}",
                "{MM}",
                "{MMM}",
                "{MMMM}",
                "{yy}",
                "{yyyy}"
            };

            foreach (var token in replaceParams)
            {
                var tokenMatch = Regex.Match(token, "\\{([dmy]+)\\}", RegexOptions.IgnoreCase);
                replaceValue = GetDateToken(replaceValue, tokenMatch, token);
            }

            return replaceValue;
        }

        private static string GetDateToken(string value, Match tokenMatch, string token)
        {
            var replaceValue = value;


            var matches = Regex.Matches(value, "(\\{(" + tokenMatch.Groups[1].Value + ")(\\+|\\-)?(\\d+)?\\})",
                RegexOptions.IgnoreCase);
            if (matches.Count > 0)
                for (var i = 0; i < matches.Count; i++)
                {
                    var dateAdd = 0;
                    switch (matches[i].Groups[3].Value)
                    {
                        case "+" when !string.IsNullOrEmpty(matches[i].Groups[4].Value):
                            dateAdd = int.Parse(matches[i].Groups[4].Value);
                            break;
                        case "-" when !string.IsNullOrEmpty(matches[i].Groups[4].Value):
                            dateAdd = -int.Parse(matches[i].Groups[4].Value);
                            break;
                    }

                    replaceValue = Regex.Replace(replaceValue,
                        matches[i].Groups[1].Value.Replace("{", "\\{").Replace("}", "\\}").Replace("+", "\\+")
                            .Replace("-", "\\-"),
                        GetDateValue(token, dateAdd), RegexOptions.IgnoreCase);
                }
            else
                replaceValue = Regex.Replace(replaceValue, token, GetDateValue(token));

            return replaceValue;
        }

        private static string GetDateExpressionToken(string value)
        {
            var replaceValue = value;
            const string matchString =
                "(\\{(d{3}|d{4})?(\\s+)?(d{2})?(\\s+|\\/|\\-)?(M{1,4})(\\s+|\\/|\\-)?(Y{2}|Y{4})(\\+|\\-)?(\\d+)?(d|m|y)?\\})";
            var matches = Regex.Matches(replaceValue,
                matchString, RegexOptions.IgnoreCase);

            for (var matchCount = 0; matchCount < matches.Count; matchCount++)
            {
                var s = new StringBuilder();
                for (var group = 2; group < 9; group++)
                    if (!string.IsNullOrEmpty(matches[matchCount].Groups[group].Value))
                        s.Append(matches[matchCount].Groups[group].Value.Replace("D", "d").Replace("m", "M")
                            .Replace("Y", "y"));
                var dateAdd = 0;

                switch (matches[matchCount].Groups[9].Value)
                {
                    case "+" when !string.IsNullOrEmpty(matches[matchCount].Groups[10].Value):
                        dateAdd = int.Parse(matches[matchCount].Groups[10].Value);
                        break;
                    case "-" when !string.IsNullOrEmpty(matches[matchCount].Groups[10].Value):
                        dateAdd = -int.Parse(matches[matchCount].Groups[10].Value);
                        break;
                }

                var date = DateTime.Today;
                if (!string.IsNullOrEmpty(matches[matchCount].Groups[11].Value))
                    switch (matches[matchCount].Groups[11].Value.ToLower()[0])
                    {
                        case 'd':
                            date = date.AddDays(dateAdd);
                            break;
                        case 'm':
                            date = date.AddMonths(dateAdd);
                            break;
                        case 'y':
                            date = date.AddYears(dateAdd);
                            break;
                    }

                replaceValue = Regex.Replace(replaceValue,
                    matches[0].Groups[1].Value.Replace("{", "\\{").Replace("+", "\\+").Replace("-", "\\-")
                        .Replace("}", "\\}"), date.ToString(s.ToString()), RegexOptions.IgnoreCase);
            }

            return replaceValue;
        }

        private static string GetDateValue(string matchType, int dateAdd = 0)
        {
            var dateValue = string.Empty;
            switch (matchType.ToLower())
            {
                case "{d}":
                    dateValue = DateTime.Today.AddDays(dateAdd).Day.ToString();
                    break;
                case "{dd}":
                    dateValue = DateTime.Today.AddDays(dateAdd).ToString("dd");
                    break;
                case "{ddd}":
                    dateValue = DateTime.Today.AddDays(dateAdd).ToString("ddd");
                    break;
                case "{dddd}":
                    dateValue = DateTime.Today.AddDays(dateAdd).ToString("dddd");
                    break;
                case "{m}":
                    dateValue = DateTime.Today.AddMonths(dateAdd).Month.ToString();
                    break;
                case "{mm}":
                    dateValue = DateTime.Today.AddMonths(dateAdd).ToString("MM");
                    break;
                case "{mmm}":
                    dateValue = DateTime.Today.AddMonths(dateAdd).ToString("MMM");
                    break;
                case "{mmmm}":
                    dateValue = DateTime.Today.AddMonths(dateAdd).ToString("MMMM");
                    break;
                case "{yy}":
                    dateValue = DateTime.Today.AddYears(dateAdd).ToString("yy");
                    break;
                case "{yyyy}":
                    dateValue = DateTime.Today.AddYears(dateAdd).ToString("yyyy");
                    break;
            }

            return dateValue;
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

                    if (Classes.Holidays.ValidateCountry(Country))
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

                        WebClient.SetFlurlConfig(App.Title, _useProxy, _proxy, _proxyUser, _proxyPass, _bypassLocal,
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
                    Holidays = Classes.Holidays.ValidateHolidays(result, _holidayMatch, _localeMatch, _includeBank,
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

            //Detect a repeating schedule and handle it - otherwise end time is 1 hour after start
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
        private void ScheduledLogEvent(LogEventLevel logLevel, string message, string description)
        {
            if (_includeApp) message = "[{AppName}] -" + message;


            if (_isTags)
                Log.ForContext(nameof(Tags), HandleTokens(_tags)).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), _priority).ForContext(nameof(Responders), _responders)
                    .ForContext(nameof(LogCount), LogCount).ForContext("Message", message)
                    .ForContext("Description", description)
                    .Write((Serilog.Events.LogEventLevel) logLevel,
                        string.IsNullOrEmpty(description) || !_includeDescription
                            ? "{Message}"
                            : "{Message} : {Description}");
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), _priority)
                    .ForContext(nameof(Responders), _responders).ForContext(nameof(LogCount), LogCount)
                    .ForContext("Message", message).ForContext("Description", description)
                    .Write((Serilog.Events.LogEventLevel) logLevel,
                        string.IsNullOrEmpty(description) || !_includeDescription
                            ? "{Message}"
                            : "{Message} : {Description}");
        }

        /// <summary>
        ///     Output a log event to Seq stream
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void LogEvent(LogEventLevel logLevel, string message, params object[] args)
        {
            var logArgsList = args.ToList();

            if (_includeApp)
            {
                message = "[{AppName}] -" + message;
                logArgsList.Insert(0, App.Title);
            }

            var logArgs = logArgsList.ToArray();


            if (_isTags)
                Log.ForContext(nameof(Tags), HandleTokens(_tags)).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), _priority).ForContext(nameof(Responders), _responders)
                    .ForContext(nameof(LogCount), LogCount)
                    .Write((Serilog.Events.LogEventLevel) logLevel, message, logArgs);
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), _priority)
                    .ForContext(nameof(Responders), _responders).ForContext(nameof(LogCount), LogCount)
                    .Write((Serilog.Events.LogEventLevel) logLevel, message, logArgs);
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
            var logArgsList = args.ToList();

            if (_includeApp)
            {
                message = "[{AppName}] -" + message;
                logArgsList.Insert(0, App.Title);
            }

            var logArgs = logArgsList.ToArray();

            if (_isTags)
                Log.ForContext(nameof(Tags), HandleTokens(_tags)).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), _priority).ForContext(nameof(Responders), _responders)
                    .ForContext(nameof(LogCount), LogCount)
                    .Write((Serilog.Events.LogEventLevel) logLevel, exception, message, logArgs);
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), _priority)
                    .ForContext(nameof(Responders), _responders).ForContext(nameof(LogCount), LogCount)
                    .Write((Serilog.Events.LogEventLevel) logLevel, exception, message, logArgs);
        }
    }
}