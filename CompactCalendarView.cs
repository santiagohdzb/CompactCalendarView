using Android.Content;
using Android.Graphics;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Util;
using System.Collections.Generic;

namespace CompactCalendarView
{
    public class CompactCalendarView : View
    {
        protected AnimationHandler animationHandler;
        protected CompactCalendarController compactCalendarController;
        protected GestureDetectorCompat gestureDetector;
        protected bool shouldScroll = true;
        protected CalendarGestureListener gestureListener;

        public interface CompactCalendarViewListener
        {
            void onDayClick(Date dateClicked);
            void onMonthScroll(Date firstDayOfNewMonth);
        }

        public class CalendarGestureListener : GestureDetector.SimpleOnGestureListener
        {
            private CompactCalendarView _context;
            public CalendarGestureListener(CompactCalendarView context)
            {
                _context = context;
            }

            public override void OnLongPress(MotionEvent e)
            {
            }

            public override bool OnSingleTapConfirmed(MotionEvent e)
            {
                _context.compactCalendarController.onSingleTapConfirmed(e);
                _context.Invalidate();

                return base.OnSingleTapConfirmed(e);
            }

            public override bool OnDown(MotionEvent e)
            {
                return true;
            }

            public override bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
            {
                return true;
            }

            public override bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
            {
                if (_context.shouldScroll) {
                    _context.compactCalendarController.onScroll(e1, e2, distanceX, distanceY);
                    _context.Invalidate();
                }
                return true;
            }
        }

        public CompactCalendarView(Context context) : base(context, null)
        {
            Setup(context, null, 0);
        }

        public CompactCalendarView(Context context, IAttributeSet attrs) : base(context, attrs, 0)
        {
            Setup(context, attrs, 0);
        }

        public CompactCalendarView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Setup(context, attrs, defStyleAttr);
        }

        private void Setup(Context context, IAttributeSet attrs, int defStyleAttr)
        {
            compactCalendarController = new CompactCalendarController(new Paint(), new OverScroller(context),
                                                                      new Rect(), attrs, context, Color.Argb(255, 233, 84, 81),
                                                                      Color.Argb(255, 64, 64, 64), Color.Argb(255, 219, 219, 219), VelocityTracker.Obtain());
            gestureListener = new CalendarGestureListener(this);
            gestureDetector = new GestureDetectorCompat(context, gestureListener);
            animationHandler = new AnimationHandler(compactCalendarController, this);
        }

        public void setLocale(Java.Util.Locale locale)
        {
            compactCalendarController.setLocale(locale);
            Invalidate();
        }

        /*
        Compact calendar will use the locale to determine the abbreviation to use as the day column names.
        The default is to use the default locale and to abbreviate the day names to one character.
        Setting this to true will displace the short weekday string provided by java.
         */
        public void setUseThreeLetterAbbreviation(bool useThreeLetterAbbreviation)
        {
            compactCalendarController.setUseWeekDayAbbreviation(useThreeLetterAbbreviation);
            Invalidate();
        }

        public void setCalendarBackgroundColor(int calenderBackgroundColor)
        {
            compactCalendarController.setCalenderBackgroundColor(calenderBackgroundColor);
            Invalidate();
        }

        /*
        Will draw the indicator for events as a small dot under the day rather than a circle behind the day.
         */
        public void drawSmallIndicatorForEvents(bool shouldDrawDaysHeader)
        {
            compactCalendarController.showSmallIndicator(shouldDrawDaysHeader);
        }

        /*
        Sets the name for each day of the week. No attempt is made to adjust width or text size based on the length of each day name.
        Works best with 3-4 characters for each day.
         */
        public void setDayColumnNames(string[] dayColumnNames)
        {
            compactCalendarController.setDayColumnNames(dayColumnNames);
        }

        public void setShouldShowMondayAsFirstDay(bool shouldShowMondayAsFirstDay)
        {
            compactCalendarController.setShouldShowMondayAsFirstDay(shouldShowMondayAsFirstDay);
            Invalidate();
        }

        public void setCurrentSelectedDayBackgroundColor(int currentSelectedDayBackgroundColor)
        {
            compactCalendarController.setCurrentSelectedDayBackgroundColor(currentSelectedDayBackgroundColor);
            Invalidate();
        }

        public void setCurrentDayBackgroundColor(int currentDayBackgroundColor)
        {
            compactCalendarController.setCurrentDayBackgroundColor(currentDayBackgroundColor);
            Invalidate();
        }

        public int getHeightPerDay()
        {
            return compactCalendarController.getHeightPerDay();
        }

        public void setListener(CompactCalendarViewListener listener)
        {
            compactCalendarController.setListener(listener);
        }

        public Date getFirstDayOfCurrentMonth()
        {
            return compactCalendarController.getFirstDayOfCurrentMonth();
        }

        public void setCurrentDate(Date dateTimeMonth)
        {
            compactCalendarController.setCurrentDate(dateTimeMonth);
            Invalidate();
        }

        public int getWeekNumberForCurrentMonth()
        {
            return compactCalendarController.getWeekNumberForCurrentMonth();
        }

        public void setShouldDrawDaysHeader(bool shouldDrawDaysHeader)
        {
            compactCalendarController.setShouldDrawDaysHeader(shouldDrawDaysHeader);
        }

        /**
         * see {@link #addEvent(com.github.sundeepk.compactcalendarview.domain.CalendarDayEvent, boolean)} when adding single events
         * or {@link #addEvents(java.util.List)}  when adding multiple events
         * @param event
         */
        public void addEvent(CalendarDayEvent ev)
        {
            addEvent(ev, false);
        }

        /**
         *  Adds an event to be drawn as an indicator in the calendar.
         *  If adding multiple events see {@link #addEvents(List)}} method.
         * @param event to be added to the calendar
         * @param shouldInvalidate true if the view should invalidate
         */
        public void addEvent(CalendarDayEvent ev, bool shouldInvalidate)
        {
            compactCalendarController.addEvent(ev);
            if (shouldInvalidate) {
                Invalidate();
            }
        }

        /**
         * Adds multiple events to the calendar and invalidates the view once all events are added.
         */
        public void addEvents(List<CalendarDayEvent> events)
        {
            compactCalendarController.addEvents(events);
            Invalidate();
        }

        /**
         * see {@link #removeEvent(com.github.sundeepk.compactcalendarview.domain.CalendarDayEvent, boolean)} when removing single events
         * or {@link #removeEvents(java.util.List)} (java.util.List)}  when removing multiple events
         * @param event
         */
        public void removeEvent(CalendarDayEvent ev)
        {
            removeEvent(ev, false);
        }

        /**
         * Removes an event from the calendar.
         * If removing multiple events see {@link #removeEvents(List)}
         *
         * @param event event to remove from the calendar
         * @param shouldInvalidate true if the view should invalidate
         */
        public void removeEvent(CalendarDayEvent ev, bool shouldInvalidate)
        {
            compactCalendarController.removeEvent(ev);
            if (shouldInvalidate) {
                Invalidate();
            }
        }

        /**
         * Adds multiple events to the calendar and invalidates the view once all events are added.
         */
        public void removeEvents(List<CalendarDayEvent> events)
        {
            compactCalendarController.removeEvents(events);
            Invalidate();
        }

        /**
         * Clears all Events from the calendar.
         */
        public void removeAllEvents()
        {
            compactCalendarController.removeAllEvents();
        }

        private void checkTargetHeight()
        {
            if (compactCalendarController.getTargetHeight() <= 0) {
                throw new IllegalStateException("Target height must be set in xml properties in order to expand/collapse CompactCalendar.");
            }
        }

        public void showCalendar()
        {
            checkTargetHeight();
            animationHandler.openCalendar();
        }

        public void hideCalendar()
        {
            checkTargetHeight();
            animationHandler.closeCalendar();
        }

        public void showCalendarWithAnimation()
        {
            checkTargetHeight();
            animationHandler.openCalendarWithAnimation();
        }

        public void hideCalendarWithAnimation()
        {
            checkTargetHeight();
            animationHandler.closeCalendarWithAnimation();
        }

        public void showNextMonth()
        {
            compactCalendarController.showNextMonth();
            Invalidate();
        }

        public void showPreviousMonth()
        {
            compactCalendarController.showPreviousMonth();
            Invalidate();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            int width = MeasureSpec.GetSize(widthMeasureSpec);
            int height = MeasureSpec.GetSize(heightMeasureSpec);
            if (width > 0 && height > 0) {
                compactCalendarController.onMeasure(width, height, PaddingRight, PaddingLeft);
            }

            SetMeasuredDimension(width, height);
        }

        protected override void OnDraw(Canvas canvas)
        {
            compactCalendarController.onDraw(canvas);
        }

        public override void ComputeScroll()
        {
            base.ComputeScroll();
            if (compactCalendarController.computeScroll()) {
                Invalidate();
            }
        }

        public void shouldScrollMonth(bool shouldDisableScroll)
        {
            shouldScroll = shouldDisableScroll;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            compactCalendarController.onTouch(e);
            Invalidate();
            // always allow gestureDetector to detect onSingleTap and scroll events
            return gestureDetector.OnTouchEvent(e);
        }
    }
}