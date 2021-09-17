using Lurgle.Dates;
using Lurgle.Dates.Classes;
using Seq.App.EventSchedule.Classes;
using Seq.Apps;
using Seq.Apps.LogEvents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Timers;
using Lurgle.Dates.Enums;

// ReSharper disable MemberCanBePrivate.Global

namespace Seq.App.EventSchedule
{
    [SeqApp("Event Schedule", AllowReprocessing = false,
        Description =
            "Super-powered Seq app to schedule logging an event at given times, with optional repeating log intervals, day of week and day of month inclusion/exclusion, and optional holiday API!")]
    // ReSharper disable once UnusedType.Global
    public class EventScheduleReactor : SeqApp
    {
        public readonly ScheduleConfig Config = new ScheduleConfig();
        public readonly ScheduleCounters Counters = new ScheduleCounters();
        private Timer _timer;
        public string Description;
        public HandlebarsTemplate DescriptionTemplate;
        public string Message;
        public HandlebarsTemplate MessageTemplate;

// ReSharper disable MemberCanBePrivate.Global

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
            HelpText = "Optional Initial Time Estimate property to pass for scheduled logs, for use with other apps. Jira-type date expression, eg. Ww (weeks) Xd (days) Yh (hours) Zm (minutes).",
            IsOptional = true)]
        public string InitialTimeEstimate { get; set; }

        [SeqAppSetting(DisplayName = "Remaining Time Estimate for scheduled logs",
            HelpText = "Optional Remaining Time Estimate property to pass for scheduled logs, for use with other apps. Jira-type date expression, eg. Ww (weeks) Xd (days) Yh (hours) Zm (minutes).",
            IsOptional = true)]
        public string RemainingTimeEstimate { get; set; }

        [SeqAppSetting(DisplayName = "Due Date for scheduled logs",
            HelpText = "Optional Due Date property to pass for scheduled logs, for use with other apps. Date in yyyy-MM-dd format, or Jira-type date expression, eg. Ww (weeks) Xd (days) Yh (hours) Zm (minutes).",
            IsOptional = true)]
        public string DueDate { get; set; }

        [SeqAppSetting(
            DisplayName = "Days of week",
            HelpText = "Comma-delimited - Monday, Tue, Wednesday, Thursday, Friday, Saturday, Sunday.",
            IsOptional = true)]
        public string DaysOfWeek { get; set; }

        [SeqAppSetting(
            DisplayName = "Months of year",
            HelpText = "Comma-delimited - January,Feb,February.March,April,May,etc.",
            IsOptional = true)]
        public string MonthsOfYear { get; set; }

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
                "Event message to raise. Allows tokens for date parts: Day: {d}/{dd}/{ddd}/{dddd}, Month: {M}/{MM}/{MMM}/{MMMM}, Year: {yy}/{yyyy}, or date expressions. These are not case sensitive.")]
        public string AlertMessage { get; set; }

        [SeqAppSetting(
            IsOptional = true,
            DisplayName = "Scheduled log description.",
            InputType = SettingInputType.LongText,
            HelpText =
                "Optional description associated with the event raised. Allows tokens for date parts: Day: {d}/{dd}/{ddd}/{dddd}, Month: {M}/{MM}/{MMM}/{MMMM}, Year: {yy}/{yyyy}, or date expressions. These are not case sensitive.")]
        public string AlertDescription { get; set; }

        [SeqAppSetting(
            DisplayName = "Include description with log message",
            HelpText =
                "If selected, the configured description will be part of the log message. Otherwise it will only show as a log property, which can be used by other Seq apps.",
            IsOptional = true)]
        public bool? IncludeDescription { get; set; } = false;

        [SeqAppSetting(
            DisplayName = "Use Handlebars templates in message and description",
            HelpText =
                "if selected, the configured message and description will be rendered using Handlebars. Don't select this if you want to render in another app.",
            IsOptional = true)]
        public bool? UseHandlebars { get; set; } = false;

        [SeqAppSetting(
            IsOptional = true,
            DisplayName = "Scheduled log tags",
            HelpText =
                "Tags for the event, separated by commas. Allows tokens for date parts: Day: {d}/{dd}/{ddd}/{dddd}, Month: {M}/{MM}/{MMM}/{MMMM}, Year: {yy}/{yyyy}, or date expressions. These are not case sensitive.")]
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
            Config.AppName = App.Title;
            LogEvent(LogEventLevel.Debug, "Check {AppName} diagnostic level ({Diagnostics}) ...", App.Title,
                Diagnostics);
            Config.Diagnostics = Diagnostics;

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Check include {AppName} ({IncludeApp}) ...", App.Title, IncludeApp);

            Config.IncludeApp = IncludeApp;
            if (!Config.IncludeApp && Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "App name {AppName} will not be included in alert message ...",
                    App.Title);
            else if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "App name {AppName} will be included in alert message ...", App.Title);

            if (!DateTime.TryParseExact(ScheduleTime, "H:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out _))
            {
                if (DateTime.TryParseExact(ScheduleTime, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out _))
                    Config.StartFormat = "H:mm";
                else
                    LogEvent(LogEventLevel.Debug,
                        "Start Time {ScheduleTime} does  not parse to a valid DateTime - app will exit ...",
                        ScheduleTime);
            }

            Config.RepeatSchedule = RepeatSchedule;
            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Repeat every X seconds: {RepeatSchedule}",
                    Config.RepeatSchedule);

            if (Config.RepeatSchedule)
            {
                if (ScheduleInterval <= 0)
                    ScheduleInterval = 1;
                if (ScheduleInterval > 86400)
                    ScheduleInterval = 60;
                if (Config.Diagnostics)
                    LogEvent(LogEventLevel.Debug, "Convert Repeat Schedule Interval {ScheduleInterval} to TimeSpan ...",
                        ScheduleInterval);
                Config.ScheduleInterval = TimeSpan.FromSeconds(ScheduleInterval);

                if (Config.Diagnostics)
                    LogEvent(LogEventLevel.Debug, "Parsed Repeat Schedule Interval is {Interval} ...",
                        Config.ScheduleInterval.TotalSeconds);
            }

            if (!string.IsNullOrEmpty(MultiLogToken))
            {
                if (Config.Diagnostics)
                    LogEvent(LogEventLevel.Debug, "Convert Multi-Log Tokens to dictionary ...");
                var tokens = (MultiLogToken ?? "")
                    .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();
                foreach (var x in from token in tokens where token.Contains("=") select token.Split('='))
                {
                    Config.LogTokenLookup.Add(x[0], x[1]);
                    if (Config.Diagnostics)
                        LogEvent(LogEventLevel.Debug, "Add mapping for {LogToken} to {LogTokenLong}", x[0], x[1]);
                }
            }

            LogEvent(LogEventLevel.Debug,
                "Use Holidays API {UseHolidays}, Country {Country}, Has API key {IsEmpty} ...", UseHolidays, Country,
                !string.IsNullOrEmpty(ApiKey));
            SetHolidays();
            RetrieveHolidays(DateTime.Today, DateTime.UtcNow);

            if (!Config.UseHolidays || Counters.IsUpdating) UtcRollover(DateTime.UtcNow);

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Convert Days of Week {DaysOfWeek} to UTC Days of Week ...", DaysOfWeek);
            Config.DaysOfWeek = Dates.GetUtcDaysOfWeek(DaysOfWeek, ScheduleTime, Config.StartFormat);


            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "UTC Days of Week {DaysOfWeek} will be used ...", Config.DaysOfWeek.ToArray());

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Parse Months of Year {MonthsOfYear} ...", MonthsOfYear);
            Config.MonthsOfYear = Dates.GetMonthsOfYear(MonthsOfYear);

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Months of Year {MonthsOfYear} will be used ...", Config.MonthsOfYear.ToArray());


            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Include Days of Month {Config.IncludeDays} ...", IncludeDaysOfMonth);

            Config.IncludeDays = Dates.GetUtcDaysOfMonth(IncludeDaysOfMonth, ScheduleTime, Config.StartFormat, DateTime.Now);
            if (Config.IncludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: {Config.IncludeDays} ...", Config.IncludeDays.ToArray());
            else
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: ALL ...");

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Exclude Days of Month {Config.ExcludeDays} ...", ExcludeDaysOfMonth);

            Config.ExcludeDays = Dates.GetUtcDaysOfMonth(ExcludeDaysOfMonth, ScheduleTime, Config.StartFormat, DateTime.Now);
            if (Config.ExcludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: {Config.ExcludeDays} ...", Config.ExcludeDays.ToArray());
            else
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: NONE ...");

            if (UseHandlebars != null) Config.UseHandlebars = (bool)UseHandlebars;
            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug,
                    "Use Handlebars to render Log Message and Description: '{UseHandlebars}' ...",
                    Config.UseHandlebars);

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Alert Message '{AlertMessage}' ...", AlertMessage);

            Message = !string.IsNullOrWhiteSpace(AlertMessage)
                ? AlertMessage.Trim()
                : "Scheduled event log";
            MessageTemplate = Config.UseHandlebars ? new HandlebarsTemplate(Message) : new HandlebarsTemplate("");

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Alert Message '{AlertMessage}' will be used ...", Message);

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Validate Alert Description '{AlertDescription}' ...", AlertDescription);

            Description = !string.IsNullOrWhiteSpace(AlertDescription)
                ? AlertDescription.Trim()
                : "";
            DescriptionTemplate =
                Config.UseHandlebars ? new HandlebarsTemplate(Description) : new HandlebarsTemplate("");

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Alert Description '{AlertDescription}' will be used ...",
                    Description);

            if (IncludeDescription != null)
                Config.IncludeDescription = (bool) IncludeDescription;

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug, "Include Description in Log Message: '{IncludeDescription}' ...",
                    Config.IncludeDescription);

            if (Config.Diagnostics) LogEvent(LogEventLevel.Debug, "Convert Tags '{Tags}' to array ...", Tags);

            Config.Tags = (Tags ?? "")
                .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToArray();
            if (Config.Tags.Length > 0) Config.IsTags = true;

            if (string.IsNullOrWhiteSpace(ScheduleLogLevel)) ScheduleLogLevel = "Information";
            if (!Enum.TryParse(ScheduleLogLevel, out Config.ThresholdLogLevel))
                Config.ThresholdLogLevel = LogEventLevel.Information;

            if (!string.IsNullOrEmpty(Priority))
                Config.Priority = Priority;

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
                        Config.ResponderLookup.Add(x[0], x[1]);
                        if (Config.Diagnostics)
                            LogEvent(LogEventLevel.Debug, "Add mapping for {LogToken} to {Responder}", x[0], x[1]);
                    }
                }
                else
                {
                    Config.Responders = Responders;
                    if (Config.Diagnostics)
                        LogEvent(LogEventLevel.Debug, "Set responder to {Responder}", Config.Responders);
                }
            }

            if (!string.IsNullOrEmpty(ProjectKey))
            {
                if (Config.Diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Project Key to {Value}", ProjectKey);
                Config.ProjectKey = ProjectKey;
            }

            if (!string.IsNullOrEmpty(InitialTimeEstimate) && DateTokens.ValidDateExpression(InitialTimeEstimate))
            {
                if (Config.Diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Initial Time Estimate to {Value}",
                        DateTokens.SetValidExpression(InitialTimeEstimate));
                Config.InitialTimeEstimate = DateTokens.SetValidExpression(InitialTimeEstimate);
            }

            if (!string.IsNullOrEmpty(RemainingTimeEstimate) && DateTokens.ValidDateExpression(RemainingTimeEstimate))
            {
                if (Config.Diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Remaining Time Estimate to {Value}",
                        DateTokens.SetValidExpression(RemainingTimeEstimate));
                Config.RemainingTimeEstimate = DateTokens.SetValidExpression(RemainingTimeEstimate);
            }

            if (!string.IsNullOrEmpty(DueDate) &&
                (DateTokens.ValidDateExpression(DueDate) || DateTokens.ValidDate(DueDate)))
            {
                if (Config.Diagnostics)
                    LogEvent(LogEventLevel.Debug, "Set Due Date to {Value}",
                        DateTokens.ValidDate(DueDate) ? DueDate : DateTokens.SetValidExpression(DueDate));
                Config.DueDate = DateTokens.ValidDate(DueDate) ? DueDate : DateTokens.SetValidExpression(DueDate);
            }

            if (Config.Diagnostics)
                LogEvent(LogEventLevel.Debug,
                    "Log level {LogLevel} will be used for threshold violations on {Instance} ...",
                    Config.ThresholdLogLevel, App.Title);

            if (Config.Diagnostics) LogEvent(LogEventLevel.Debug, "Starting timer ...");

            _timer = new Timer(1000)
            {
                AutoReset = true
            };
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            if (Config.Diagnostics) LogEvent(LogEventLevel.Debug, "Timer started ...");
        }


        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var timeNow = DateTime.UtcNow;
            var localDate = DateTime.Today;
            if (!string.IsNullOrEmpty(Config.TestDate))
                localDate = DateTime.ParseExact(Config.TestDate, "yyyy-M-d", CultureInfo.InvariantCulture,
                    DateTimeStyles.None);

            if (Counters.LastDay < localDate) RetrieveHolidays(localDate, timeNow);

            //We can only enter showtime if we're not currently retrying holidays, but existing showtimes will continue to monitor
            if ((!Config.UseHolidays || Counters.IsShowtime || !Counters.IsShowtime && !Counters.IsUpdating) && timeNow >= Counters.StartTime &&
                timeNow < Counters.EndTime)
            {
                if (!Counters.IsShowtime && (!Config.DaysOfWeek.Contains(Counters.StartTime.DayOfWeek) ||
                                    Config.IncludeDays.Count > 0 && !Config.IncludeDays.Contains(Counters.StartTime) ||
                                    Config.ExcludeDays.Contains(Counters.StartTime) || !Config.MonthsOfYear.Contains((MonthOfYear)localDate.Month)))
                {
                    //Log that we have skipped a day due to an exclusion
                    if (!Counters.SkippedShowtime)
                        LogEvent(LogEventLevel.Debug,
                            "Threshold checking will not be performed due to exclusions - Day of Week Excluded {DayOfWeek}, Day Of Month Not Included {IncludeDay}, Day of Month Excluded {ExcludeDay}, Month Excluded {MonthOfYear} ...",
                            !Config.DaysOfWeek.Contains(Counters.StartTime.DayOfWeek),
                            Config.IncludeDays.Count > 0 && !Config.IncludeDays.Contains(Counters.StartTime),
                            Config.ExcludeDays.Count > 0 && Config.ExcludeDays.Contains(Counters.StartTime), 
                            !Config.MonthsOfYear.Contains((MonthOfYear)localDate.Month));

                    Counters.SkippedShowtime = true;
                }
                else
                {
                    //Showtime! - Log one or more events as scheduled
                    if (!Counters.IsShowtime)
                    {
                        LogEvent(LogEventLevel.Debug,
                            "UTC Start Time {Time} ({DayOfWeek}), logging events as scheduled, until UTC End time {EndTime} ({EndDayOfWeek}) ...",
                            Counters.StartTime.ToShortTimeString(), Counters.StartTime.DayOfWeek,
                            Counters.EndTime.ToShortTimeString(), Counters.EndTime.DayOfWeek);
                        Counters.IsShowtime = true;
                        Counters.LastLog = timeNow;
                    }

                    var difference = timeNow - Counters.LastLog;
                    //Check the interval time versus threshold count
                    if (!Counters.EventLogged || Config.RepeatSchedule && difference.TotalSeconds > Config.ScheduleInterval.TotalSeconds)
                    {
                        if (Config.LogTokenLookup.Any())
                        {
                            //Log multiple events
                            foreach (var token in Config.LogTokenLookup)
                            {
                                //Log event
                                ScheduledLogEvent(Config.ThresholdLogLevel,
                                    Config.UseHandlebars ? MessageTemplate.Render(Config, Counters) : Message,
                                    Config.UseHandlebars ? DescriptionTemplate.Render(Config, Counters) : Description, token);
                            }
                        }
                        else
                        {
                            //Log event
                            ScheduledLogEvent(Config.ThresholdLogLevel,
                                Config.UseHandlebars ? MessageTemplate.Render(Config, Counters) : Message,
                                Config.UseHandlebars ? DescriptionTemplate.Render(Config, Counters) : Description);
                        }

                        Counters.LastLog = timeNow;
                        Counters.EventLogged = true;
                        Counters.LogCount++;
                    }
                }
            }
            else if (timeNow < Counters.StartTime || timeNow >= Counters.EndTime)
            {
                //Showtime can end even if we're retrieving holidays
                if (Counters.IsShowtime)
                    LogEvent(LogEventLevel.Debug,
                        "UTC End Time {Time} ({DayOfWeek}), no longer logging scheduled events, total logged {Counters.LogCount}  ...",
                        Counters.EndTime.ToShortTimeString(), Counters.EndTime.DayOfWeek, Counters.LogCount);

                //Reset the match counters
                Counters.LastLog = timeNow;
                Counters.IsShowtime = false;
                Counters.SkippedShowtime = false;
                Counters.EventLogged = false;
            }

            //We can only do UTC rollover if we're not currently retrying holidays and it's not during showtime
            if (Counters.IsShowtime || Config.UseHolidays && Counters.IsUpdating || Counters.StartTime > timeNow ||
                !string.IsNullOrEmpty(Config.TestDate)) return;
            UtcRollover(timeNow);
            //Take the opportunity to refresh include/exclude days to allow for month rollover
            Config.IncludeDays = Dates.GetUtcDaysOfMonth(IncludeDaysOfMonth, ScheduleTime, Config.StartFormat, DateTime.Now);
            if (Config.IncludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Include UTC Days of Month: {Config.IncludeDays} ...",
                    Config.IncludeDays.ToArray());

            Config.ExcludeDays = Dates.GetUtcDaysOfMonth(ExcludeDaysOfMonth, ScheduleTime, Config.StartFormat, DateTime.Now);
            if (Config.ExcludeDays.Count > 0)
                LogEvent(LogEventLevel.Debug, "Exclude UTC Days of Month: {Config.ExcludeDays} ...",
                    Config.ExcludeDays.ToArray());
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
                    if (Config.Diagnostics) LogEvent(LogEventLevel.Debug, "Validate Country {Country}", Country);

                    if (Holidays.ValidateCountry(Country))
                    {
                        Config.UseHolidays = true;
                        Counters.RetryCount = 10;
                        if (RetryCount >= 0 && RetryCount <= 100)
                            Counters.RetryCount = RetryCount;
                        Config.Country = Country;
                        Config.ApiKey = ApiKey;
                        Config.IncludeWeekends = IncludeWeekends;
                        Config.IncludeBank = IncludeBank;

                        if (string.IsNullOrEmpty(HolidayMatch))
                            Config.HolidayMatch = new List<string>();
                        else
                            Config.HolidayMatch = HolidayMatch.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim()).ToList();

                        if (string.IsNullOrEmpty(LocaleMatch))
                            Config.LocaleMatch = new List<string>();
                        else
                            Config.LocaleMatch = LocaleMatch.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim()).ToList();

                        if (!string.IsNullOrEmpty(Proxy))
                        {
                            Config.UseProxy = true;
                            Config.Proxy = Proxy;
                            Config.BypassLocal = BypassLocal;
                            Config.LocalAddresses = LocalAddresses.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim()).ToArray();
                            Config.ProxyUser = ProxyUser;
                            Config.ProxyPass = ProxyPass;
                        }

                        if (Config.Diagnostics)
                            LogEvent(LogEventLevel.Debug,
                                "Holidays API Enabled: {UseHolidays}, Country {Country}, Use Proxy {UseProxy}, Proxy Address {Proxy}, BypassLocal {BypassLocal}, Authentication {Authentication} ...",
                                Config.UseHolidays, Config.Country,
                                Config.UseProxy, Config.Proxy, Config.BypassLocal,
                                !string.IsNullOrEmpty(ProxyUser) && !string.IsNullOrEmpty(ProxyPass));

                        WebClient.SetConfig(App.Title, Config.UseProxy, Config.Proxy, Config.ProxyUser, Config.ProxyPass, Config.BypassLocal,
                            Config.LocalAddresses);
                    }
                    else
                    {
                        Config.UseHolidays = false;
                        LogEvent(LogEventLevel.Debug,
                            "Holidays API Enabled: {UseHolidays}, Could not parse country {CountryCode} to valid region ...",
                            Config.UseHolidays, Config.Country);
                    }

                    break;
                }
                case true:
                    Config.UseHolidays = false;
                    LogEvent(LogEventLevel.Debug, "Holidays API Enabled: {UseHolidays}, One or more parameters not set",
                        Config.UseHolidays);
                    break;
            }

            Counters.LastDay = DateTime.Today.AddDays(-1);
            Counters.LastError = DateTime.Now.AddDays(-1);
            Counters.LastUpdate = DateTime.Now.AddDays(-1);
            Counters.ErrorCount = 0;
            Config.TestDate = TestDate;
            Config.Holidays = new List<AbstractApiHolidays>();
        }

        /// <summary>
        ///     Update AbstractAPI Holidays for this instance, given local and UTC date
        /// </summary>
        /// <param name="localDate"></param>
        /// <param name="utcDate"></param>
        private void RetrieveHolidays(DateTime localDate, DateTime utcDate)
        {
            if (Config.UseHolidays && (!Counters.IsUpdating || Counters.IsUpdating && (DateTime.Now - Counters.LastUpdate).TotalSeconds > 10 &&
                (DateTime.Now - Counters.LastError).TotalSeconds > 10 && Counters.ErrorCount < Counters.RetryCount))
            {
                Counters.IsUpdating = true;
                if (!string.IsNullOrEmpty(Config.TestDate))
                    localDate = DateTime.ParseExact(Config.TestDate, "yyyy-M-d", CultureInfo.InvariantCulture,
                        DateTimeStyles.None);

                if (Config.Diagnostics)
                    LogEvent(LogEventLevel.Debug,
                        "Retrieve holidays for {Date}, Last Update {lastUpdateDate} {lastUpdateTime} ...",
                        localDate.ToShortDateString(), Counters.LastUpdate.ToShortDateString(),
                        Counters.LastUpdate.ToShortTimeString());

                var holidayUrl = WebClient.GetUrl(Config.ApiKey, Config.Country, localDate);
                if (Config.Diagnostics) LogEvent(LogEventLevel.Debug, "URL used is {url} ...", holidayUrl);

                try
                {
                    Counters.LastUpdate = DateTime.Now;
                    var result = WebClient.GetHolidays(Config.ApiKey, Config.Country, localDate).Result;
                    Config.Holidays = Holidays.ValidateHolidays(result, Config.HolidayMatch, Config.LocaleMatch, Config.IncludeBank,
                        Config.IncludeWeekends);
                    Counters.LastDay = localDate;
                    Counters.ErrorCount = 0;

                    if (Config.Diagnostics && !string.IsNullOrEmpty(Config.TestDate))
                    {
                        LogEvent(LogEventLevel.Debug,
                            "Test date {testDate} used, raw holidays retrieved {testCount} ...", Config.TestDate,
                            result.Count);
                        foreach (var holiday in result)
                            LogEvent(LogEventLevel.Debug,
                                "Holiday Name: {Name}, Local Name {LocalName}, Start {LocalStart}, Start UTC {Start}, End UTC {End}, Type {Type}, Location string {Location}, Locations parsed {Locations} ...",
                                holiday.Name, holiday.Name_Local, holiday.LocalStart, holiday.UtcStart, holiday.UtcEnd,
                                holiday.Type, holiday.Location, holiday.Locations.ToArray());
                    }

                    LogEvent(LogEventLevel.Debug, "Holidays retrieved and validated {holidayCount} ...",
                        Config.Holidays.Count);
                    foreach (var holiday in Config.Holidays)
                        LogEvent(LogEventLevel.Debug,
                            "Holiday Name: {Name}, Local Name {LocalName}, Start {LocalStart}, Start UTC {Start}, End UTC {End}, Type {Type}, Location string {Location}, Locations parsed {Locations} ...",
                            holiday.Name, holiday.Name_Local, holiday.LocalStart, holiday.UtcStart, holiday.UtcEnd,
                            holiday.Type, holiday.Location, holiday.Locations.ToArray());

                    Counters.IsUpdating = false;
                    if (!Counters.IsShowtime) UtcRollover(utcDate, true);
                }
                catch (Exception ex)
                {
                    Counters.ErrorCount++;
                    LogEvent(LogEventLevel.Debug, ex,
                        "Error {Error} retrieving holidays, public holidays cannot be evaluated (Try {Count} of {retryCount})...",
                        ex.Message, Counters.ErrorCount, Counters.RetryCount);
                    Counters.LastError = DateTime.Now;
                }
            }
            else if (!Config.UseHolidays || Counters.IsUpdating && Counters.ErrorCount >= 10)
            {
                Counters.IsUpdating = false;
                Counters.LastDay = localDate;
                Counters.ErrorCount = 0;
                Config.Holidays = new List<AbstractApiHolidays>();
                if (Config.UseHolidays && !Counters.IsShowtime) UtcRollover(utcDate, true);
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
                Config.UseTestOverrideTime
                    ? Config.TestOverrideTime.ToUniversalTime().ToShortTimeString()
                    : DateTime.Now.ToUniversalTime().ToShortTimeString());

            //Day rollover, we need to ensure the next start and end is in the future
            if (!string.IsNullOrEmpty(Config.TestDate))
                Counters.StartTime = DateTime.ParseExact(Config.TestDate + " " + ScheduleTime, "yyyy-M-d " + Config.StartFormat,
                    CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
            else if (Config.UseTestOverrideTime)
                Counters.StartTime = DateTime
                    .ParseExact(Config.TestOverrideTime.ToString("yyyy-M-d") + " " + ScheduleTime, "yyyy-M-d " + Config.StartFormat,
                        CultureInfo.InvariantCulture, DateTimeStyles.None)
                    .ToUniversalTime();
            else
                Counters.StartTime = DateTime
                    .ParseExact(ScheduleTime, Config.StartFormat, CultureInfo.InvariantCulture, DateTimeStyles.None)
                    .ToUniversalTime();

            //Detect a repeating schedule and DateTokens.Handle it - otherwise end time is 1 hour after start
            Counters.EndTime = Config.RepeatSchedule ? Counters.StartTime.AddDays(1) : Counters.StartTime.AddHours(1);

            //If there are holidays, account for them
            if (Config.Holidays.Any(holiday => Counters.StartTime >= holiday.UtcStart && Counters.StartTime < holiday.UtcEnd))
            {
                Counters.StartTime = Counters.StartTime.AddDays(Config.Holidays.Any(holiday =>
                    Counters.StartTime.AddDays(1) >= holiday.UtcStart && Counters.StartTime.AddDays(1) < holiday.UtcEnd)
                    ? 2
                    : 1);
                Counters.EndTime = Counters.EndTime.AddDays(Counters.EndTime.AddDays(1) < Counters.StartTime ? 2 : 1);
            }

            //If we updated holidays or this is a repeating schedule, don't automatically put start time to the future
            if (!RepeatSchedule && (!Config.UseTestOverrideTime && Counters.StartTime < utcDate ||
                                    Config.UseTestOverrideTime && Counters.StartTime < Config.TestOverrideTime.ToUniversalTime()) &&
                !isUpdateHolidays) Counters.StartTime = Counters.StartTime.AddDays(1);

            if (Counters.EndTime < Counters.StartTime)
                Counters.EndTime = Counters.EndTime.AddDays(Counters.EndTime.AddDays(1) < Counters.StartTime ? 2 : 1);

            LogEvent(LogEventLevel.Debug,
                isUpdateHolidays
                    ? "UTC Day Rollover (Holidays Updated), Parse {LocalStart} To Next UTC Schedule Time {ScheduleTime} ({StartDayOfWeek}), UTC End Time {EndTime} ({EndDayOfWeek})..."
                    : "UTC Day Rollover, Parse {LocalStart} To Next UTC Start Time {ScheduleTime} ({StartDayOfWeek}), UTC End Time {EndTime} ({EndDayOfWeek})...",
                ScheduleTime, Counters.StartTime.ToShortTimeString(), Counters.StartTime.DayOfWeek, Counters.EndTime.ToShortTimeString(),
                Counters.EndTime.DayOfWeek);
        }

        public Showtime GetShowtime()
        {
            return new Showtime(Counters.StartTime, Counters.EndTime);
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
            var include = "{AppName} - ";
            if (!Config.IncludeApp) include = string.Empty;

            var responder = string.Empty;
            if (Config.ResponderLookup.Count > 0)
            {
                if (token != null)
                    foreach (var responderPair in from responderPair in Config.ResponderLookup
                        let tokenPair = (KeyValuePair<string, string>)token
                        where responderPair.Key.Equals(tokenPair.Key, StringComparison.OrdinalIgnoreCase)
                        select responderPair)
                    {
                        responder = responderPair.Value;
                        break;
                    }
            }
            else
            {
                responder = Config.Responders;
            }

            Log.ForContext(nameof(Tags),
                    Config.IsTags ? DateTokens.HandleTokens(Config.Tags, token) : new List<string>())
                .ForContext("AppName", App.Title)
                .ForContext(nameof(Priority), Config.Priority).ForContext(nameof(Responders), responder)
                .ForContext(nameof(InitialTimeEstimate), Config.InitialTimeEstimate)
                .ForContext(nameof(RemainingTimeEstimate), Config.RemainingTimeEstimate)
                .ForContext(nameof(ProjectKey), Config.ProjectKey).ForContext(nameof(DueDate), Config.DueDate)
                .ForContext(nameof(Counters.LogCount), Counters.LogCount).ForContext("Message",
                    token == null ? DateTokens.HandleTokens(message) : DateTokens.HandleTokens(message, token))
                .ForContext("Description",
                    token == null ? DateTokens.HandleTokens(description) : DateTokens.HandleTokens(description, token))
                .ForContext("MultiLogTokens", Config.LogTokenLookup)
                .Write((Serilog.Events.LogEventLevel)logLevel,
                    string.IsNullOrEmpty(description) || !Config.IncludeDescription
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
            var logArgsList = args.ToList();

            if (Config.IncludeApp)
            {
                logArgsList.Insert(0, App.Title);
            }

            var logArgs = logArgsList.ToArray();


            if (Config.IsTags)
                Log.ForContext(nameof(Tags), DateTokens.HandleTokens(Config.Tags)).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), Config.Priority).ForContext(nameof(Responders), Config.Responders)
                    .ForContext(nameof(InitialTimeEstimate), Config.InitialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), Config.RemainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), Config.ProjectKey).ForContext(nameof(DueDate), Config.DueDate)
                    .ForContext(nameof(Counters.LogCount), Counters.LogCount).ForContext("MultiLogTokens", Config.LogTokenLookup)
                    .Write((Serilog.Events.LogEventLevel) logLevel, Config.IncludeApp ? "[{AppName}] - " + message : message, logArgs);
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), Config.Priority)
                    .ForContext(nameof(Responders), Config.Responders).ForContext(nameof(Counters.LogCount), Counters.LogCount)
                    .ForContext(nameof(InitialTimeEstimate), Config.InitialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), Config.RemainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), Config.ProjectKey).ForContext(nameof(DueDate), Config.DueDate)
                    .ForContext("MultiLogTokens", Config.LogTokenLookup)
                    .Write((Serilog.Events.LogEventLevel) logLevel, Config.IncludeApp ? "[{AppName}] - " + message : message, logArgs);
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

            if (Config.IncludeApp)
            {
                logArgsList.Insert(0, App.Title);
            }

            var logArgs = logArgsList.ToArray();


            if (Config.IsTags)
                Log.ForContext(nameof(Tags), DateTokens.HandleTokens(Config.Tags)).ForContext("AppName", App.Title)
                    .ForContext(nameof(Priority), Config.Priority).ForContext(nameof(Responders), Config.Responders)
                    .ForContext(nameof(InitialTimeEstimate), Config.InitialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), Config.RemainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), Config.ProjectKey).ForContext(nameof(DueDate), Config.DueDate)
                    .ForContext(nameof(Counters.LogCount), Counters.LogCount).ForContext("MultiLogTokens", Config.LogTokenLookup)
                    .Write((Serilog.Events.LogEventLevel) logLevel, exception, Config.IncludeApp ? "[{AppName}] - " + message : message, logArgs);
            else
                Log.ForContext("AppName", App.Title).ForContext(nameof(Priority), Config.Priority)
                    .ForContext(nameof(Responders), Config.Responders).ForContext(nameof(Counters.LogCount), Counters.LogCount)
                    .ForContext(nameof(InitialTimeEstimate), Config.InitialTimeEstimate)
                    .ForContext(nameof(RemainingTimeEstimate), Config.RemainingTimeEstimate)
                    .ForContext(nameof(ProjectKey), Config.ProjectKey).ForContext(nameof(DueDate), Config.DueDate)
                    .ForContext("MultiLogTokens", Config.LogTokenLookup)
                    .Write((Serilog.Events.LogEventLevel) logLevel, exception, Config.IncludeApp ? "[{AppName}] - " + message : message, logArgs);
        }
    }
}