using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Lurgle.Dates;
using Lurgle.Dates.Classes;
using Seq.App.EventSchedule.Tests.Support;
using Xunit;
using Xunit.Abstractions;

namespace Seq.App.EventSchedule.Tests
{
    public class EventScheduleAppTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public EventScheduleAppTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void AppIncludeDay()
        {
            var start = DateTime.Now.AddSeconds(2);
            var app = Some.Reactor(start.ToString("H:mm:ss"),
                0, dayOfMonth: start.Day.ToString());
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " +
                                        showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.ToUniversalTime().ToString("F") + " to " +
                                        start.AddHours(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == start.AddHours(1).ToUniversalTime().ToString("F"));
            //Wait for showtime
            Thread.Sleep(3000);
            showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " +
                                        showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Event Logged: {0}, Include Day: {1}", app.Counters.EventLogged,
                string.Join(",", app.Config.IncludeDays));
            Assert.True(app.Counters.EventLogged);
        }

        [Fact]
        public void AppNotInclude()
        {
            var start = DateTime.Now.AddSeconds(1);
            var app = Some.Reactor(start.ToString("H:mm:ss"),
                0, dayOfMonth: start.AddDays(-1).Day.ToString());
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " +
                                        showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.ToUniversalTime().ToString("F") + " to " +
                                        start.AddHours(1).ToUniversalTime().ToString("F"));
            Thread.Sleep(2000);
            showTime = app.GetShowtime();
            Assert.False(showTime.Start.ToString("F") == start.ToUniversalTime().ToString("F"));
            Assert.False(showTime.End.ToString("F") == start.AddHours(1).ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("New ShowTime: " + showTime.Start.ToString("F") + " to " +
                                        showTime.End.ToString("F"));
            Assert.False(app.Counters.EventLogged);
        }

        [Fact]
        public void AppSchedule()
        {
            var start = DateTime.Now.AddSeconds(1);
            var app = Some.Reactor(start.ToString("H:mm:ss"),
                0);
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " +
                                        showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.ToUniversalTime().ToString("F") + " to " +
                                        start.AddHours(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == start.AddHours(1).ToUniversalTime().ToString("F"));
            //Wait for showtime
            Thread.Sleep(2000);
            _testOutputHelper.WriteLine("Event Logged: {0}", app.Counters.EventLogged);
            Assert.True(app.Counters.EventLogged);
        }

        [Fact]
        public void AppRepeatSchedule()
        {
            var start = DateTime.Now.AddSeconds(1);
            var app = Some.Reactor(start.ToString("H:mm:ss"),
                1, true);
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " +
                                        showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.ToUniversalTime().ToString("F") + " to " +
                                        start.AddDays(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
            //Wait for showtime
            Thread.Sleep(2000);
            _testOutputHelper.WriteLine("Log count: {0}", app.Counters.LogCount);
            Assert.True(app.Counters.LogCount >= 1);
            for (var i = 1; i < 6; i++)
            {
                Thread.Sleep(2000);
                _testOutputHelper.WriteLine("Log count after {0} seconds: {1}", i * 2, app.Counters.LogCount);
                Assert.True(app.Counters.LogCount >= i + 1 && app.Counters.LogCount <= i + 5);
            }
        }

        [Fact]
        public void AppStartsDuringShowTime()
        {
            var start = DateTime.Now.AddHours(-1);

            var app = Some.Reactor(start.ToString("H:mm:ss"), 0);
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " +
                                        showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.AddDays(1).ToUniversalTime().ToString("F") + " to " +
                                        start.AddDays(1).AddHours(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == start.AddDays(1).AddHours(1).ToUniversalTime().ToString("F"));
        }

        [Fact]
        public void AppStartsBeforeShowTime()
        {
            var start = DateTime.Now.AddHours(1);

            var app = Some.Reactor(start.ToString("H:mm:ss"), 0);
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " +
                                        showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.ToUniversalTime().ToString("F") + " to " +
                                        start.AddHours(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == start.AddHours(1).ToUniversalTime().ToString("F"));
        }

        [Fact]
        public void AppStartsAfterShowTime()
        {
            var start = DateTime.Now.AddHours(-2);

            var app = Some.Reactor(start.ToString("H:mm:ss"), 0);
            app.Attach(TestAppHost.Instance);
            var showTime = app.GetShowtime();
            _testOutputHelper.WriteLine("Current UTC: " + DateTime.Now.ToUniversalTime().ToString("F"));
            _testOutputHelper.WriteLine("ShowTime: " + showTime.Start.ToString("F") + " to " +
                                        showTime.End.ToString("F"));
            _testOutputHelper.WriteLine("Expect Start: " + start.AddDays(1).ToUniversalTime().ToString("F") + " to " +
                                        start.AddDays(1).AddHours(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
            Assert.True(showTime.End.ToString("F") == start.AddDays(1).AddHours(1).ToUniversalTime().ToString("F"));
        }

        [Fact]
        public void RolloverWithHoliday()
        {
            var start = DateTime.ParseExact(DateTime.Today.ToString("yyyy-MM-dd") + " 00:01:00", "yyyy-MM-dd H:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None);

            var app = Some.Reactor(start.ToString("H:mm:ss"), 0);
            app.Config.TestOverrideTime = start;
            app.Config.UseTestOverrideTime = true;
            app.Attach(TestAppHost.Instance);

            for (var i = 0; i < 169; i++)
            {
                if (i > 0)
                {
                    app.Config.TestOverrideTime = app.Config.TestOverrideTime.AddHours(1);

                    if (i % 24 == 0) start = start.AddDays(1);
                }

                var holiday = new AbstractApiHolidays("Threshold Day", "", "AU", "", "AU",
                    "Australia - New South Wales",
                    "Local holiday", start.ToString("MM/dd/yyyy"), start.Year.ToString(),
                    start.Month.ToString(), start.Day.ToString(), start.DayOfWeek.ToString());
                app.Config.Holidays = new List<AbstractApiHolidays> {holiday};

                app.UtcRollover(app.Config.TestOverrideTime.ToUniversalTime(), true);
                var showTime = app.GetShowtime();
                _testOutputHelper.WriteLine("Time: {0:F}, Next ShowTime: {1:F}, Matches {2}",
                    app.Config.TestOverrideTime.ToUniversalTime(), showTime.Start.ToUniversalTime(),
                    showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));

                Assert.True(showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
                Assert.True(showTime.End.ToString("F") == start.AddDays(1).AddHours(1).ToUniversalTime().ToString("F"));
            }
        }

        [Fact]
        public void RolloverWithoutHoliday()
        {
            var start = DateTime.ParseExact(DateTime.Today.ToString("yyyy-MM-dd") + " 00:01:00", "yyyy-MM-dd H:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None);

            var app = Some.Reactor(start.ToString("H:mm:ss"), 0);
            app.Config.TestOverrideTime = start;
            app.Config.UseTestOverrideTime = true;
            app.Attach(TestAppHost.Instance);

            for (var i = 0; i < 169; i++)
            {
                if (i > 0)
                {
                    app.Config.TestOverrideTime = app.Config.TestOverrideTime.AddHours(1);

                    if (i % 24 == 0) start = start.AddDays(1);
                }

                app.Config.Holidays = new List<AbstractApiHolidays>();

                app.UtcRollover(app.Config.TestOverrideTime.ToUniversalTime());
                var showTime = app.GetShowtime();

                if (start < app.Config.TestOverrideTime)
                {
                    _testOutputHelper.WriteLine("Time: {0:F}, Next ShowTime: {1:F}, Matches {2}",
                        app.Config.TestOverrideTime.ToUniversalTime(), showTime.Start.ToUniversalTime(),
                        showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
                    Assert.True(showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
                    Assert.True(showTime.End.ToString("F") ==
                                start.AddDays(1).AddHours(1).ToUniversalTime().ToString("F"));
                }
                else
                {
                    _testOutputHelper.WriteLine("Time: {0:F}, Next ShowTime: {1:F}, Matches {2}",
                        app.Config.TestOverrideTime.ToUniversalTime(), showTime.Start.ToUniversalTime(),
                        showTime.Start.ToString("F") == start.ToUniversalTime().ToString("F"));
                    Assert.True(showTime.Start.ToString("F") == start.ToUniversalTime().ToString("F"));
                    Assert.True(showTime.End.ToString("F") == start.AddHours(1).ToUniversalTime().ToString("F"));
                }
            }
        }

        [Fact]
        public void HolidaysMatch()
        {
            var holiday = new AbstractApiHolidays("Threshold Day", "", "AU", "", "AU", "Australia - New South Wales",
                "Local holiday", DateTime.Today.ToString("MM/dd/yyyy"), DateTime.Today.Year.ToString(),
                DateTime.Today.Month.ToString(), DateTime.Today.Day.ToString(), DateTime.Today.DayOfWeek.ToString());

            Assert.True(Holidays.ValidateHolidays(new List<AbstractApiHolidays> {holiday},
                new List<string> {"National", "Local"}, new List<string> {"Australia", "New South Wales"}, false,
                true).Count > 0);
        }

        [Fact]
        public void RenderTemplate()
        {
            var app = Some.Reactor(DateTime.Now.AddSeconds(1).ToString("H:mm:ss"),
                 0);
            app.UseHandlebars = true;
            app.AlertMessage =
                "{{AppName}}  {{TimeNow}} - {{Timeout}} Seconds - {{TimeoutMins}} Mins - {{TimeoutHours}} Hours - {dd-MM-yyyy+10m}";
            app.Attach(TestAppHost.Instance);
            var output = DateTokens.HandleTokens(app.MessageTemplate.Render(app.Config, app.Counters));
            _testOutputHelper.WriteLine("Template: {0}\nOutput: {1}", app.Message, output);

            Assert.DoesNotContain("{", output);
        }
    }
}