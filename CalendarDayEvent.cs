using System;

namespace CompactCalendarView
{
    public class CalendarDayEvent
    {
        private long timeInMillis;
        private int color;

        public CalendarDayEvent(DateTime dateTime, int color)
        {
            this.color = color;
            this.timeInMillis = dateTime.GetTimeInMillis();
        }

        public CalendarDayEvent(long timeInMillis, int color)
        {
            this.timeInMillis = timeInMillis;
            this.color = color;
        }

        public long getTimeInMillis()
        {
            return timeInMillis;
        }

        public int getColor()
        {
            return color;
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || o.GetType() != typeof(CalendarDayEvent)) return false;

            var ev = (CalendarDayEvent)o;

            if (color != ev.color) return false;
            if (timeInMillis != ev.timeInMillis) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = (int)(timeInMillis ^ (timeInMillis >> 32));
            result = 31 * result + color;
            return result;
        }

        public override string ToString()
        {
            return "CalendarDayEvent{" +
                    "timeInMillis=" + timeInMillis +
                    ", color=" + color +
                    '}';
        }
    }
}