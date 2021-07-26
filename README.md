# Seq.Apps.EventSchedule - Event Schedule for Seq

[![Version](https://img.shields.io/nuget/v/Seq.App.EventSchedule?style=plastic)](https://www.nuget.org/packages/Seq.App.EventSchedule)
[![Downloads](https://img.shields.io/nuget/dt/Seq.App.EventSchedule?style=plastic)](https://www.nuget.org/packages/Seq.App.EventSchedule)
[![License](https://img.shields.io/github/license/MattMofDoom/Seq.App.EventSchedule?style=plastic)](https://github.com/MattMofDoom/Seq.App.EventSchedule/blob/dev/LICENSE)

This app provides an event scheduling function for [Seq](https://datalust.co/seq). It will log the specified message (and optionally description) at configured schedule time. Optionally, this can repeat at intervals over a 24 hour period. You can configure it to only log on certain days of week or month, using powerful config options including date expressions for inclusions and exclusions.

The resulting log entries can be used to fire alerts and actions from other Seq apps that monitor a signal for these events, such as Seq.App.OpsGenie or Seq.App.Atlassian.Jira!

Date/time is converted to UTC time internally, so that the scheduled times are always handled correctly when considering local timezone and daylight savings. 

Event Schedule includes the optional ability to retrieve public holidays using [AbstractApi's Public Holidays API](https://www.abstractapi.com/holidays-api) which can retrieve your local and national public holidays. 

* You can configure Event Schedule to look for holiday types and locales, so that only (for example) National and Local holidays in Australia or New South Wales will be effective. 
* Events with "Bank Holiday" in the name are excluded by default, but can be enabled
* Weekends are excluded by default, but can be enabled
* Retrieval of holidays occurs once per instance per day, at 12am (local time). If an event monitoring period ("Showtime") is in progress, it will only occur after an event monitoring period has ended. If one is scheduled, it will be delayed until holidays are retrieved.
* The Holidays API free tier limits requests to one per second, so a 10 second retry is configured for up to 10 attempts per instance
* This allows even the free Holidays API pricing tier to be used for most cases. 
* Proxy configuration is included for Seq instances that do not have outbound internet access

Event Schedule shares many common features with [Event Timeout for Seq](https://github.com/MattMofDoom/Seq.App.EventTimeout). You can check my [Blog of Doom](https://MattMofDoom.com) for the latest information!
