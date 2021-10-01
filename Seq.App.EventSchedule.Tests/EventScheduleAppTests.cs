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
        public void MultiLog()
        {
            var start = DateTime.Now.AddSeconds(1);
            var app = Some.Reactor(start.ToString("H:mm:ss"),
                0);
            app.MultiLogToken =
                "ITKC1=Code Reviews,ITKC2=IT Release Management,ITKC4=Approval of IT change requirements,ITKC7=Adequate protection against malware - external attacks and intrusion attempts,ITKC13=IT Testing,ITKC14=User Acceptance Testing,ITKC21=IT incident management,ITKC22=Scheduled process monitoring and resolution,ITKC23=IT Problem management,ITKC24=IT change logging and acceptance,ITKC25=IT change approval,ITKC26=IT change verification and back-out planning,ITKC27=Emergency change requests,ITKC29=Back-up execution and storage,ITKC30=IT Capacity Management,ITKC38=IT Security Logging - Monitoring and Alerting,ITKC39=Security Patch Management,ITKC40=Security testing,ITKC41=IT access approval - On-boarding,ITKC42=IT authentication,ITKC43=IT access maintenance,ITKC44=Accountability for system/ privileged accounts";
            app.Responders =
                "ITKC1=Test1,ITKC2=Test2,ITKC4=Test3,ITKC7=Test3,ITKC13=Test4,ITKC14=Test5,ITKC21=Test2,ITKC22=Test6,ITKC23=Test2,ITKC24=Test6,ITKC25=Test6,ITKC26=Test7,ITKC27=Test7,ITKC29=Test8,ITKC30=Test9,ITKC38=Test3,ITKC39=Test9,ITKC40=Test3,ITKC41=Test10,ITKC42=Test11,ITKC43=Test11,ITKC44=Test11";
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
            _testOutputHelper.WriteLine("Events: {0}, Configured: {1}", app.Counters.LogCount,
                app.Config.LogTokenLookup.Count);
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
                app.Config.Holidays = new List<AbstractApiHolidays> { holiday };

                app.UtcRollover(app.Config.TestOverrideTime.ToUniversalTime(), true);
                var showTime = app.GetShowtime();
                _testOutputHelper.WriteLine(
                    "Local: {0:dd-MMM H:mm:ss}, UTC: {1:dd-MMM H:mm:ss} / Start {2:dd-MMM H:mm:ss}, Next UTC ShowTime: {3:dd-MMM H:mm:ss} - {4:dd-MMM H:mm:ss}, Matches {5}",
                    app.Config.TestOverrideTime, app.Config.TestOverrideTime.ToUniversalTime(),
                    start.AddDays(1).ToUniversalTime(), showTime.Start.ToUniversalTime(),
                    showTime.End.ToUniversalTime(),
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

                //Only once per day will the start and test override times coincide
                if (start < app.Config.TestOverrideTime)
                {
                    _testOutputHelper.WriteLine(
                        "Local: {0:dd-MMM H:mm:ss}, UTC: {1:dd-MMM H:mm:ss} / Start {2:dd-MMM H:mm:ss}, Next UTC ShowTime: {3:dd-MMM H:mm:ss} - {4:dd-MMM H:mm:ss}, Matches {5}",
                        app.Config.TestOverrideTime, app.Config.TestOverrideTime.ToUniversalTime(),
                        start.AddDays(1).ToUniversalTime(), showTime.Start.ToUniversalTime(),
                        showTime.End.ToUniversalTime(),
                        showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
                    Assert.True(showTime.Start.ToString("F") == start.AddDays(1).ToUniversalTime().ToString("F"));
                    Assert.True(showTime.End.ToString("F") ==
                                start.AddDays(1).AddHours(1).ToUniversalTime().ToString("F"));
                }
                else
                {
                    _testOutputHelper.WriteLine(
                        "Local: {0:dd-MMM H:mm:ss}, UTC: {1:dd-MMM H:mm:ss} / Start {2:dd-MMM H:mm:ss}, Next UTC ShowTime: {3:dd-MMM H:mm:ss} - {4:dd-MMM H:mm:ss}, Matches {5}",
                        app.Config.TestOverrideTime, app.Config.TestOverrideTime.ToUniversalTime(),
                        start.AddDays(1).ToUniversalTime(), showTime.Start.ToUniversalTime(),
                        showTime.End.ToUniversalTime(),
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

            Assert.True(Holidays.ValidateHolidays(new List<AbstractApiHolidays> { holiday },
                new List<string> { "National", "Local" }, new List<string> { "Australia", "New South Wales" }, false,
                true).Count > 0);
        }

        [Fact]
        public void RenderTemplate()
        {
            var app = Some.Reactor(DateTime.Now.AddSeconds(1).ToString("H:mm:ss"),
                0);
            app.UseHandlebars = true;
            app.AlertMessage =
                "{{AppName}}  {{TimeNow}} - {dd-MM-yyyy+10m}";
            app.Attach(TestAppHost.Instance);
            var output = DateTokens.HandleTokens(app.MessageTemplate.Render(app.Config, app.Counters));
            _testOutputHelper.WriteLine("Template: {0}\nOutput: {1}", app.Message, output);

            Assert.DoesNotContain("{", output);
        }
    }
}