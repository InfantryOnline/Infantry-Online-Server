using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DatabaseServerNATS.Extensions
{
    public static class StopwatchExtensions
    {
        public static string ToRoundedString(this Stopwatch stopwatch)
        {
            return FormatRounded(stopwatch.Elapsed);
        }

        public static string FormatRounded(TimeSpan elapsed)
        {
            // Less than 60 seconds -> Round to nearest second
            if (elapsed.TotalSeconds < 60)
            {
                int seconds = (int)Math.Round(elapsed.TotalSeconds, MidpointRounding.AwayFromZero);
                return $"{seconds} second{(seconds == 1 ? "" : "s")}";
            }

            // Less than 60 minutes -> Round to nearest minute
            if (elapsed.TotalMinutes < 60)
            {
                int minutes = (int)Math.Round(elapsed.TotalMinutes, MidpointRounding.AwayFromZero);
                return $"{minutes} minute{(minutes == 1 ? "" : "s")}";
            }

            // Less than 24 hours -> Round to nearest hour
            if (elapsed.TotalHours < 24)
            {
                int hours = (int)Math.Round(elapsed.TotalHours, MidpointRounding.AwayFromZero);
                return $"{hours} hour{(hours == 1 ? "" : "s")}";
            }

            // 24 hours or more -> Round to nearest day
            int days = (int)Math.Round(elapsed.TotalDays, MidpointRounding.AwayFromZero);
            return $"{days} day{(days == 1 ? "" : "s")}";
        }
    }
}
