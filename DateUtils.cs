using System;
using System.Globalization;

namespace CompactCalendarView
{
    public static class DateUtils
    {
        private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetTimeInMillis(this DateTime time)
        {
            return (long)((time - Jan1St1970).TotalMilliseconds);
        }
    }
}