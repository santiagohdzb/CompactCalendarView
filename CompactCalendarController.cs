using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Util;
using Android.Util;
using Java.Lang;
using Android.Graphics;
using Java.Text;
using Android.Content.Res;

namespace CompactCalendarView
{
    public class CompactCalendarController
    {
        private static int VELOCITY_UNIT_PIXELS_PER_SECOND = 1000;
        private static float ANIMATION_SCREEN_SET_DURATION_MILLIS = 700;
        private static int LAST_FLING_THRESHOLD_MILLIS = 300;
        private int paddingWidth = 40;
        private int paddingHeight = 40;
        private Paint dayPaint = new Paint();
        private Paint background = new Paint();
        private Rect rect;
        private int textHeight;
        private int textWidth;
        private static int DAYS_IN_WEEK = 7;
        private int widthPerDay;
        private string[] dayColumnNames;
        private float distanceX;
        private PointF accumulatedScrollOffset = new PointF();
        private OverScroller scroller;
        private int monthsScrolledSoFar;
        private Date currentDate = new Date();
		private Java.Util.Locale locale = Java.Util.Locale.Default;
        private Calendar currentCalender;
        private Calendar todayCalender;
        private Calendar calendarWithFirstDayOfMonth;
        private Calendar eventsCalendar;
        private Direction currentDirection = Direction.NONE;
        private int heightPerDay;
        private int currentDayBackgroundColor;
        private int calenderTextColor;
        private int currentSelectedDayBackgroundColor;
        private int calenderBackgroundColor = Color.White;
        private int textSize = 30;
        private int width;
        private int height;
        private int paddingRight;
        private int paddingLeft;
        private bool shouldDrawDaysHeader = true;
        private Dictionary<string, List<CalendarDayEvent>> events = new Dictionary<string, List<CalendarDayEvent>>();
        private bool _showSmallIndicator;
        private float bigCircleIndicatorRadius;
        private float smallIndicatorRadius;
        private bool shouldShowMondayAsFirstDay = true;
        private bool useThreeLetterAbbreviation = false;
        private float growFactor = 0f;
        private bool isAnimatingIndicator = false;
        private float screenDensity = 1;
        private float growfactorIndicator;
        VelocityTracker velocityTracker = null;
        private int maximumVelocity;
        private float SNAP_VELOCITY_DIP_PER_SECOND = 400;
        private int densityAdjustedSnapVelocity;
        private bool isSmoothScrolling;
        private CompactCalendarView.CompactCalendarViewListener listener;
        private bool isScrolling;
        private int distanceThresholdForAutoScroll;
        private long lastAutoScrollFromFling;
        private bool isAnimatingHeight = false;
        private int targetHeight;

        private enum Direction
        {
            NONE, HORIZONTAL, VERTICAL
        }

        public CompactCalendarController(Paint dayPaint, OverScroller scroller, Rect rect, IAttributeSet attrs,
                                  Context context, int currentDayBackgroundColor, int calenderTextColor,
                                  int currentSelectedDayBackgroundColor, VelocityTracker velocityTracker)
        {
            this.dayPaint = dayPaint;
            this.scroller = scroller;
            this.rect = rect;
            this.currentDayBackgroundColor = currentDayBackgroundColor;
            this.calenderTextColor = calenderTextColor;
            this.currentSelectedDayBackgroundColor = currentSelectedDayBackgroundColor;
            this.velocityTracker = velocityTracker;
            this.currentCalender = Calendar.GetInstance(locale);
            this.todayCalender = Calendar.GetInstance(locale);
            this.calendarWithFirstDayOfMonth = Calendar.GetInstance(locale);
            this.eventsCalendar = Calendar.GetInstance(locale);

            loadAttributes(attrs, context);
            init(context);
        }

        private void loadAttributes(IAttributeSet attrs, Context context)
        {
            if (attrs != null && context != null) {
                TypedArray typedArray = context.Theme.ObtainStyledAttributes(attrs, Resource.Styleable.CompactCalendarView, 0, 0);
                try {
					currentDayBackgroundColor = typedArray.GetColor(Resource.Styleable.CompactCalendarView_compactCalendarCurrentDayBackgroundColor, currentDayBackgroundColor);
					calenderTextColor = typedArray.GetColor(Resource.Styleable.CompactCalendarView_compactCalendarTextColor, calenderTextColor);
					currentSelectedDayBackgroundColor = typedArray.GetColor(Resource.Styleable.CompactCalendarView_compactCalendarCurrentSelectedDayBackgroundColor, currentSelectedDayBackgroundColor);
					calenderBackgroundColor = typedArray.GetColor(Resource.Styleable.CompactCalendarView_compactCalendarBackgroundColor, calenderBackgroundColor);
					textSize = typedArray.GetDimensionPixelSize(Resource.Styleable.CompactCalendarView_compactCalendarTextSize,
					                                            (int)TypedValue.ApplyDimension(ComplexUnitType.Sp, textSize, context.Resources.DisplayMetrics));
					targetHeight = typedArray.GetDimensionPixelSize(Resource.Styleable.CompactCalendarView_compactCalendarTargetHeight,
					                                                (int)TypedValue.ApplyDimension(ComplexUnitType.Sp, targetHeight, context.Resources.DisplayMetrics));
                }
                finally {
					typedArray.Recycle();
                }
            }
        }

        private void init(Context context)
        {
            setUseWeekDayAbbreviation(false);
            dayPaint.TextAlign = Paint.Align.Center;
            dayPaint.SetStyle(Paint.Style.Stroke);
            dayPaint.Flags = PaintFlags.AntiAlias;
            dayPaint.SetTypeface(Typeface.SansSerif);
            dayPaint.TextSize = textSize;
            dayPaint.Color = new Color(calenderTextColor);
            dayPaint.GetTextBounds("31", 0, "31".Length, rect);
            textHeight = rect.Height() * 3;
            textWidth = rect.Width() * 2;

            todayCalender.Time = currentDate;
            setToMidnight(todayCalender);

            currentCalender.Time = currentDate;
            setCalenderToFirstDayOfMonth(calendarWithFirstDayOfMonth, currentDate, -monthsScrolledSoFar, 0);

            eventsCalendar.FirstDayOfWeek = Calendar.Monday;

            if (context != null) {
                screenDensity = context.Resources.DisplayMetrics.Density;
                ViewConfiguration configuration = ViewConfiguration.Get(context);
                densityAdjustedSnapVelocity = (int)(screenDensity * SNAP_VELOCITY_DIP_PER_SECOND);
                maximumVelocity = configuration.ScaledMaximumFlingVelocity;
            }

            //just set a default growFactor to draw full calendar when initialised
            growFactor = Integer.MaxValue;
        }

        private void setCalenderToFirstDayOfMonth(Calendar calendarWithFirstDayOfMonth, Date currentDate, int scrollOffset, int monthOffset)
        {
            setMonthOffset(calendarWithFirstDayOfMonth, currentDate, scrollOffset, monthOffset);
            calendarWithFirstDayOfMonth.Set(CalendarField.DayOfMonth, 1);
        }

        private void setMonthOffset(Calendar calendarWithFirstDayOfMonth, Date currentDate, int scrollOffset, int monthOffset)
        {
            calendarWithFirstDayOfMonth.Time = currentDate;
            calendarWithFirstDayOfMonth.Add(CalendarField.Month, scrollOffset + monthOffset);
            calendarWithFirstDayOfMonth.Set(CalendarField.HourOfDay, 0);
            calendarWithFirstDayOfMonth.Set(CalendarField.Minute, 0);
            calendarWithFirstDayOfMonth.Set(CalendarField.Second, 0);
            calendarWithFirstDayOfMonth.Set(CalendarField.Millisecond, 0);
        }

        public void setGrowFactorIndicator(float growfactorIndicator)
        {
            this.growfactorIndicator = growfactorIndicator;
        }

        public float getGrowFactorIndicator()
        {
            return growfactorIndicator;
        }

        public void setAnimatingHeight(bool animatingHeight)
        {
            this.isAnimatingHeight = animatingHeight;
        }

        public int getTargetHeight()
        {
            return targetHeight;
        }

        public void setListener(CompactCalendarView.CompactCalendarViewListener listener)
        {
            this.listener = listener;
        }

        public void removeAllEvents()
        {
            events.Clear();
        }

        public void setShouldShowMondayAsFirstDay(bool shouldShowMondayAsFirstDay)
        {
            this.shouldShowMondayAsFirstDay = shouldShowMondayAsFirstDay;
            setUseWeekDayAbbreviation(useThreeLetterAbbreviation);
            if (shouldShowMondayAsFirstDay) {
                eventsCalendar.FirstDayOfWeek = Calendar.Monday;
            }
            else {
                eventsCalendar.FirstDayOfWeek = Calendar.Sunday;
            }
        }

        public void setCurrentSelectedDayBackgroundColor(int currentSelectedDayBackgroundColor)
        {
            this.currentSelectedDayBackgroundColor = currentSelectedDayBackgroundColor;
        }

        public void setCalenderBackgroundColor(int calenderBackgroundColor)
        {
            this.calenderBackgroundColor = calenderBackgroundColor;
        }

        public void setCurrentDayBackgroundColor(int currentDayBackgroundColor)
        {
            this.currentDayBackgroundColor = currentDayBackgroundColor;
        }

        public void showNextMonth()
        {
            setCalenderToFirstDayOfMonth(calendarWithFirstDayOfMonth, currentCalender.Time, 0, 1);
            setCurrentDate(calendarWithFirstDayOfMonth.Time);
            performMonthScrollCallback();
        }

        public void showPreviousMonth()
        {
            setCalenderToFirstDayOfMonth(calendarWithFirstDayOfMonth, currentCalender.Time, 0, -1);
            setCurrentDate(calendarWithFirstDayOfMonth.Time);
            performMonthScrollCallback();
        }

		public void setLocale(Java.Util.Locale locale)
        {
            if (locale == null) {
                throw new IllegalArgumentException("Locale cannot be null");
            }
            this.locale = locale;
        }

        public void setUseWeekDayAbbreviation(bool useThreeLetterAbbreviation)
        {
            this.useThreeLetterAbbreviation = useThreeLetterAbbreviation;
            DateFormatSymbols dateFormatSymbols = new DateFormatSymbols(locale);
            string[] dayNames = dateFormatSymbols.GetShortWeekdays();
            if (dayNames == null) {
                throw new IllegalStateException("Unable to determine weekday names from default locale");
            }
            if (dayNames.Length != 8) {
                throw new IllegalStateException("Expected weekday names from default locale to be of size 7 but: "
                        + "'" + string.Join("'", dayNames) + "', with size " + dayNames.Length + " was returned.");
            }

            if (useThreeLetterAbbreviation) {
                if (!shouldShowMondayAsFirstDay) {
                    this.dayColumnNames = new string[] { dayNames[1], dayNames[2], dayNames[3], dayNames[4], dayNames[5], dayNames[6], dayNames[7] };
                }
                else {
                    this.dayColumnNames = new string[] { dayNames[2], dayNames[3], dayNames[4], dayNames[5], dayNames[6], dayNames[7], dayNames[1] };
                }
            }
            else {
                if (!shouldShowMondayAsFirstDay) {
                    this.dayColumnNames = new string[]{dayNames[1].Substring(0, 1), dayNames[2].Substring(0, 1),
                            dayNames[3].Substring(0, 1), dayNames[4].Substring(0, 1), dayNames[5].Substring(0, 1), dayNames[6].Substring(0, 1), dayNames[7].Substring(0, 1)};
                }
                else {
                    this.dayColumnNames = new string[]{dayNames[2].Substring(0, 1), dayNames[3].Substring(0, 1),
                            dayNames[4].Substring(0, 1), dayNames[5].Substring(0, 1), dayNames[6].Substring(0, 1), dayNames[7].Substring(0, 1), dayNames[1].Substring(0, 1)};
                }
            }
        }

        public void setDayColumnNames(string[] dayColumnNames)
        {
            if (dayColumnNames == null || dayColumnNames.Length != 7) {
                throw new IllegalArgumentException("Column names cannot be null and must contain a value for each day of the week");
            }
            this.dayColumnNames = dayColumnNames;
        }


        public void setShouldDrawDaysHeader(bool shouldDrawDaysHeader)
        {
            this.shouldDrawDaysHeader = shouldDrawDaysHeader;
        }

        public void showSmallIndicator(bool showSmallIndicator)
        {
            this._showSmallIndicator = showSmallIndicator;
        }

        public void onMeasure(int width, int height, int paddingRight, int paddingLeft)
        {
            widthPerDay = (width) / DAYS_IN_WEEK;
            heightPerDay = targetHeight > 0 ? targetHeight / 7 : height / 7;
            this.width = width;
            this.distanceThresholdForAutoScroll = (int)(width * 0.50);
            this.height = height;
            this.paddingRight = paddingRight;
            this.paddingLeft = paddingLeft;

            //scale small indicator by screen density
            smallIndicatorRadius = 2.5f * screenDensity;

            //assume square around each day of width and height = heightPerDay and get diagonal line length
            //makes easier to find radius
            double radiusAroundDay = 0.5 * System.Math.Sqrt((heightPerDay * heightPerDay) + (heightPerDay * heightPerDay));
            //make radius based on screen density
            bigCircleIndicatorRadius = (float)radiusAroundDay / ((1.8f) - 0.5f / screenDensity);
        }

        public void onDraw(Canvas canvas)
        {
            paddingWidth = widthPerDay / 2;
            paddingHeight = heightPerDay / 2;
            calculateXPositionOffset();

            if (isAnimatingHeight) {
                background.Color = new Color(calenderBackgroundColor);
                background.SetStyle(Paint.Style.Fill);
                canvas.DrawCircle(0, 0, growFactor, background);
                dayPaint.SetStyle(Paint.Style.Stroke);
                dayPaint.Color = new Color(Color.White);

                drawScrollableCalender(canvas);
            }
            else if (isAnimatingIndicator) {

                dayPaint.Color = new Color(calenderBackgroundColor);
                dayPaint.SetStyle(Paint.Style.Fill);
                canvas.DrawCircle(0, 0, growFactor, dayPaint);
                dayPaint.SetStyle(Paint.Style.Stroke);
                dayPaint.Color = new Color(Color.White);

                drawScrollableCalender(canvas);
            }
            else {
                drawCalenderBackground(canvas);
                drawScrollableCalender(canvas);
            }
        }

        public void onSingleTapConfirmed(MotionEvent e)
        {
            //Don't handle single tap the calendar is scrolling and is not stationary
            if (Java.Lang.Math.Abs(accumulatedScrollOffset.X) != Java.Lang.Math.Abs(width * monthsScrolledSoFar)) {
                return;
            }

            int dayColumn = Java.Lang.Math.Round((paddingLeft + e.XPrecision - paddingWidth - paddingRight) / widthPerDay);
            int dayRow = Java.Lang.Math.Round((e.YPrecision - paddingHeight) / heightPerDay);

            setCalenderToFirstDayOfMonth(calendarWithFirstDayOfMonth, currentDate, -monthsScrolledSoFar, 0);

            //Start Monday as day 1 and Sunday as day 7. Not Sunday as day 1 and Monday as day 2
            int firstDayOfMonth = getDayOfWeek(calendarWithFirstDayOfMonth);

            int dayOfMonth = ((dayRow - 1) * 7 + dayColumn + 1) - firstDayOfMonth;

            if (dayOfMonth < calendarWithFirstDayOfMonth.GetActualMaximum(CalendarField.DayOfMonth)
                    && dayOfMonth >= 0) {
                calendarWithFirstDayOfMonth.Add(CalendarField.Date, dayOfMonth);

                currentCalender.TimeInMillis = calendarWithFirstDayOfMonth.TimeInMillis;
                performOnDayClickCallback(currentCalender.Time);
            }
        }

        private void performOnDayClickCallback(Date date)
        {
            if (listener != null) {
                listener.onDayClick(date);
            }
        }

        public bool onScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        {
            //ignore scrolling callback if already smooth scrolling
            if (isSmoothScrolling) {
                return true;
            }

            if (currentDirection == Direction.NONE) {
                if (System.Math.Abs(distanceX) > System.Math.Abs(distanceY)) {
                    currentDirection = Direction.HORIZONTAL;
                }
                else {
                    currentDirection = Direction.VERTICAL;
                }
            }

            isScrolling = true;
            this.distanceX = distanceX;
            return true;
        }

        public bool onTouch(MotionEvent ev) {
            if (velocityTracker == null) {
                velocityTracker = VelocityTracker.Obtain();
            }

            velocityTracker.AddMovement(ev);

            if (ev.Action == MotionEventActions.Down) {
                if (!scroller.IsFinished) {
                    scroller.AbortAnimation();
                }

                isSmoothScrolling = false;
            } else if(ev.Action == MotionEventActions.Move) {
                velocityTracker.AddMovement(ev);
                velocityTracker.ComputeCurrentVelocity(500);
            } else if (ev.Action == MotionEventActions.Up) {
                handleHorizontalScrolling();
                velocityTracker.Recycle();
                velocityTracker.Clear();
                velocityTracker = null;
                isScrolling = false;
            }

            return false;
        }

        private void snapBackScroller()
        {
            float remainingScrollAfterFingerLifted1 = (accumulatedScrollOffset.X - (monthsScrolledSoFar * width));
            scroller.StartScroll((int)accumulatedScrollOffset.X, 0, (int)-remainingScrollAfterFingerLifted1, 0);
        }

        private void handleHorizontalScrolling()
        {
            int velocityX = computeVelocity();
            handleSmoothScrolling(velocityX);

            currentDirection = Direction.NONE;
            setCalenderToFirstDayOfMonth(calendarWithFirstDayOfMonth, currentDate, -monthsScrolledSoFar, 0);

            if (calendarWithFirstDayOfMonth.Get(CalendarField.Month) != currentCalender.Get(CalendarField.Month)) {
                setCalenderToFirstDayOfMonth(currentCalender, currentDate, -monthsScrolledSoFar, 0);
            }
        }

        private int computeVelocity()
        {
            velocityTracker.ComputeCurrentVelocity(VELOCITY_UNIT_PIXELS_PER_SECOND, maximumVelocity);
            return (int)velocityTracker.XVelocity;
        }

        private void handleSmoothScrolling(int velocityX)
        {
            int distanceScrolled = (int)(accumulatedScrollOffset.X - (width * monthsScrolledSoFar));
            bool isEnoughTimeElapsedSinceLastSmoothScroll = JavaSystem.CurrentTimeMillis() - lastAutoScrollFromFling > LAST_FLING_THRESHOLD_MILLIS;
            if (velocityX > densityAdjustedSnapVelocity && isEnoughTimeElapsedSinceLastSmoothScroll) {
                scrollPreviousMonth();
            }
            else if (velocityX < -densityAdjustedSnapVelocity && isEnoughTimeElapsedSinceLastSmoothScroll) {
                scrollNextMonth();
            }
            else if (isScrolling && distanceScrolled > distanceThresholdForAutoScroll) {
                scrollPreviousMonth();
            }
            else if (isScrolling && distanceScrolled < -distanceThresholdForAutoScroll) {
                scrollNextMonth();
            }
            else {
                isSmoothScrolling = false;
                snapBackScroller();
            }
        }

        private void scrollNextMonth()
        {
            lastAutoScrollFromFling = JavaSystem.CurrentTimeMillis();
            monthsScrolledSoFar = monthsScrolledSoFar - 1;
            performScroll();
            isSmoothScrolling = true;
            performMonthScrollCallback();
        }

        private void scrollPreviousMonth()
        {
            lastAutoScrollFromFling = JavaSystem.CurrentTimeMillis();
            monthsScrolledSoFar = monthsScrolledSoFar + 1;
            performScroll();
            isSmoothScrolling = true;
            performMonthScrollCallback();
        }

        private void performMonthScrollCallback()
        {
            if (listener != null) {
                listener.onMonthScroll(getFirstDayOfCurrentMonth());
            }
        }

        private void performScroll()
        {
            int targetScroll = monthsScrolledSoFar * width;
            float remainingScrollAfterFingerLifted = targetScroll - accumulatedScrollOffset.X;
            scroller.StartScroll((int)accumulatedScrollOffset.X, 0, (int)(remainingScrollAfterFingerLifted), 0,
                    (int)(Java.Lang.Math.Abs((int)(remainingScrollAfterFingerLifted)) / (float)width * ANIMATION_SCREEN_SET_DURATION_MILLIS));
        }

        public int getHeightPerDay()
        {
            return heightPerDay;
        }

        public int getWeekNumberForCurrentMonth()
        {
            Calendar calendar = Calendar.GetInstance(locale);
            calendar.Time = currentDate;
            return calendar.Get(CalendarField.WeekOfMonth);
        }

        public Date getFirstDayOfCurrentMonth()
        {
            Calendar calendar = Calendar.GetInstance(locale);
            calendar.Time = currentDate;
            calendar.Add(CalendarField.Month, -monthsScrolledSoFar);
            calendar.Set(CalendarField.DayOfMonth, 1);
            setToMidnight(calendar);
            return calendar.Time;
        }

        public void setCurrentDate(Date dateTimeMonth)
        {
            distanceX = 0;
            monthsScrolledSoFar = 0;
            accumulatedScrollOffset.X = 0;
            scroller.StartScroll(0, 0, 0, 0);
            currentDate = new Date(dateTimeMonth.Time);
            currentCalender.Time = currentDate;
            setToMidnight(currentCalender);
        }

        private void setToMidnight(Calendar calendar)
        {
            calendar.Set(CalendarField.HourOfDay, 0);
            calendar.Set(CalendarField.Minute, 0);
            calendar.Set(CalendarField.Second, 0);
            calendar.Set(CalendarField.Millisecond, 0);
        }

        public void addEvent(CalendarDayEvent ev) {
            eventsCalendar.TimeInMillis = ev.getTimeInMillis();
            string key = getKeyForCalendarEvent(eventsCalendar);
            List<CalendarDayEvent> uniqCalendarDayEvents = events[key];

            if (uniqCalendarDayEvents == null) {
                uniqCalendarDayEvents = new List<CalendarDayEvent>();
            }

            if (!uniqCalendarDayEvents.Contains(ev)) {
                uniqCalendarDayEvents.Add(ev);
            }

            events.Add(key, uniqCalendarDayEvents);
        }

        public void addEvents(List<CalendarDayEvent> events)
        {
            int count = events.Count;
            for (int i = 0; i < count; i++) {
                addEvent(events.ElementAt(i));
            }
        }

        public void removeEvent(CalendarDayEvent ev)
        {
            eventsCalendar.TimeInMillis = ev.getTimeInMillis();
            string key = getKeyForCalendarEvent(eventsCalendar);
            List<CalendarDayEvent> uniqCalendarDayEvents = events[key];
            if (uniqCalendarDayEvents != null) {
                uniqCalendarDayEvents.Remove(ev);
            }
        }

        public void removeEvents(List<CalendarDayEvent> events)
        {
            int count = events.Count;
            for (int i = 0; i < count; i++) {
                removeEvent(events.ElementAt(i));
            }
        }

        public List<CalendarDayEvent> getEvents(Date date)
        {
            eventsCalendar.TimeInMillis = date.Time;
            string key = getKeyForCalendarEvent(eventsCalendar);
            List<CalendarDayEvent> uniqEvents = events[key];

            if (uniqEvents != null) {
                return uniqEvents;
            }
            else {
                return new List<CalendarDayEvent>();
            }
        }

        //E.g. 4 2016 becomes 2016_4
        private string getKeyForCalendarEvent(Calendar cal)
        {
            return cal.Get(CalendarField.Year) + "_" + cal.Get(CalendarField.Month);
        }

        public void setGrowProgress(float grow)
        {
            growFactor = grow;
        }

        public float getGrowFactor()
        {
            return growFactor;
        }

        public void setAnimatingIndicators(bool isAnimating)
        {
            isAnimatingIndicator = isAnimating;
        }

        public bool onDown(MotionEvent e)
        {
            scroller.ForceFinished(true);
            return true;
        }

        public bool onFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        {
            scroller.ForceFinished(true);
            return true;
        }

        public bool computeScroll()
        {
            if (scroller.ComputeScrollOffset()) {
                accumulatedScrollOffset.X = scroller.CurrX;
                return true;
            }
            return false;
        }

        private void drawScrollableCalender(Canvas canvas)
        {
            drawPreviousMonth(canvas);
            drawCurrentMonth(canvas);
            drawNextMonth(canvas);
        }

        private void drawNextMonth(Canvas canvas)
        {
            setCalenderToFirstDayOfMonth(calendarWithFirstDayOfMonth, currentDate, -monthsScrolledSoFar, 1);
            drawMonth(canvas, calendarWithFirstDayOfMonth, (width * (-monthsScrolledSoFar + 1)));
        }

        private void drawCurrentMonth(Canvas canvas)
        {
            setCalenderToFirstDayOfMonth(calendarWithFirstDayOfMonth, currentDate, -monthsScrolledSoFar, 0);
            drawMonth(canvas, calendarWithFirstDayOfMonth, width * -monthsScrolledSoFar);
        }

        private void drawPreviousMonth(Canvas canvas)
        {
            setCalenderToFirstDayOfMonth(calendarWithFirstDayOfMonth, currentDate, -monthsScrolledSoFar, -1);
            drawMonth(canvas, calendarWithFirstDayOfMonth, (width * (-monthsScrolledSoFar - 1)));
        }

        private void calculateXPositionOffset()
        {
            if (currentDirection == Direction.HORIZONTAL) {
                accumulatedScrollOffset.X -= distanceX;
            }
        }

        private void drawCalenderBackground(Canvas canvas)
        {
            dayPaint.Color = new Color(calenderBackgroundColor);
            dayPaint.SetStyle(Paint.Style.Fill);
            canvas.DrawRect(0, 0, width, height, dayPaint);
            dayPaint.SetStyle(Paint.Style.Stroke);
            dayPaint.Color = new Color(calenderTextColor);
        }

        public void drawEvents(Canvas canvas, Calendar currentMonthToDrawCalender, int offset)
        {
			var key = getKeyForCalendarEvent(currentMonthToDrawCalender);
			List<CalendarDayEvent> uniqCalendarDayEvents = events.ContainsKey(key) ? events[key] : null;

            bool shouldDrawCurrentDayCircle = currentMonthToDrawCalender.Get(CalendarField.Month) == todayCalender.Get(CalendarField.Month);
            int todayDayOfMonth = todayCalender.Get(CalendarField.DayOfMonth);

            if (uniqCalendarDayEvents != null) {
                for (int i = 0; i < uniqCalendarDayEvents.Count; i++) {
                    CalendarDayEvent ev = uniqCalendarDayEvents.ElementAt(i);
                    long timeMillis = ev.getTimeInMillis();
                    eventsCalendar.TimeInMillis = timeMillis;
                    int dayOfWeek = getDayOfWeek(eventsCalendar) - 1;

                    int weekNumberForMonth = eventsCalendar.Get(CalendarField.WeekOfMonth);
                    float xPosition = widthPerDay * dayOfWeek + paddingWidth + paddingLeft + accumulatedScrollOffset.X + offset - paddingRight;
                    float yPosition = weekNumberForMonth * heightPerDay + paddingHeight;

                    if (xPosition >= growFactor || yPosition >= growFactor) {
                        continue;
                    }

                    int dayOfMonth = eventsCalendar.Get(CalendarField.DayOfMonth);
                    bool isSameDayAsCurrentDay = (todayDayOfMonth == dayOfMonth && shouldDrawCurrentDayCircle);
                    if (!isSameDayAsCurrentDay) {
                        if (_showSmallIndicator) {
                            //draw small indicators below the day in the calendar
                            drawSmallIndicatorCircle(canvas, xPosition, yPosition + 15, ev.getColor());
                        }
                        else {
                            drawCircle(canvas, xPosition, yPosition, ev.getColor());
        }
                    }

                }
            }
        }

        private int getDayOfWeek(Calendar calendar)
        {
            int dayOfWeek;
            if (!shouldShowMondayAsFirstDay) {
                return calendar.Get(CalendarField.DayOfWeek);
            }
            else {
                dayOfWeek = calendar.Get(CalendarField.DayOfWeek) - 1;
                dayOfWeek = dayOfWeek <= 0 ? 7 : dayOfWeek;
            }
            return dayOfWeek;
        }

        public void drawMonth(Canvas canvas, Calendar monthToDrawCalender, int offset)
        {
            drawEvents(canvas, monthToDrawCalender, offset);

            //offset by one because we want to start from Monday
            int firstDayOfMonth = getDayOfWeek(monthToDrawCalender);

            //offset by one because of 0 index based calculations
            firstDayOfMonth = firstDayOfMonth - 1;
            bool isSameMonthAsToday = monthToDrawCalender.Get(CalendarField.Month) == todayCalender.Get(CalendarField.Month);
            bool isSameYearAsToday = monthToDrawCalender.Get(CalendarField.Year) == todayCalender.Get(CalendarField.Year);
            bool isSameMonthAsCurrentCalendar = monthToDrawCalender.Get(CalendarField.Month) == currentCalender.Get(CalendarField.Month);
            int todayDayOfMonth = todayCalender.Get(CalendarField.DayOfMonth);

            for (int dayColumn = 0, dayRow = 0; dayColumn <= 6; dayRow++) {
                if (dayRow == 7) {
                    dayRow = 0;
                    if (dayColumn <= 6) {
                        dayColumn++;
                    }
                }
                if (dayColumn == dayColumnNames.Length) {
                    break;
                }
                float xPosition = widthPerDay * dayColumn + paddingWidth + paddingLeft + accumulatedScrollOffset.X + offset - paddingRight;
                float yPosition = dayRow * heightPerDay + paddingHeight;
                if (xPosition >= growFactor && isAnimatingHeight || yPosition >= growFactor) {
                    continue;
                }
                if (dayRow == 0) {
                    // first row, so draw the first letter of the day
                    if (shouldDrawDaysHeader) {
                        dayPaint.SetTypeface(Typeface.DefaultBold);
                        canvas.DrawText(dayColumnNames[dayColumn], xPosition, paddingHeight, dayPaint);
                        dayPaint.SetTypeface(Typeface.Default);
                    }
                }
                else {
                    int day = ((dayRow - 1) * 7 + dayColumn + 1) - firstDayOfMonth;
                    if (isSameYearAsToday && isSameMonthAsToday && todayDayOfMonth == day && !isAnimatingHeight) {
                        // TODO calculate position of circle in a more reliable way
                        drawCircle(canvas, xPosition, yPosition, currentDayBackgroundColor);
                    }
                    else if (currentCalender.Get(CalendarField.DayOfMonth) == day && isSameMonthAsCurrentCalendar && !isAnimatingHeight) {
                        drawCircle(canvas, xPosition, yPosition, currentSelectedDayBackgroundColor);
                    }
                    else if (day == 1 && !isSameMonthAsCurrentCalendar && !isAnimatingHeight) {
                        drawCircle(canvas, xPosition, yPosition, currentSelectedDayBackgroundColor);
                    }
                    if (day <= monthToDrawCalender.GetActualMaximum(CalendarField.DayOfMonth) && day > 0) {
                        canvas.DrawText(day.ToString(), xPosition, yPosition, dayPaint);
                    }
                }
            }
        }

        // Draw Circle on certain days to highlight them
        private void drawCircle(Canvas canvas, float x, float y, int color)
        {
            dayPaint.Color = new Color(color);

            if (isAnimatingIndicator) {
                drawCircle(canvas, growfactorIndicator, x, y - (textHeight / 6));
            }
            else {
                drawCircle(canvas, bigCircleIndicatorRadius, x, y - (textHeight / 6));
            }
        }

        private void drawSmallIndicatorCircle(Canvas canvas, float x, float y, int color)
        {
            dayPaint.Color = new Color(color);
            drawCircle(canvas, smallIndicatorRadius, x, y);
        }

        private void drawCircle(Canvas canvas, float radius, float x, float y)
        {
            dayPaint.SetStyle(Paint.Style.Fill);
            canvas.DrawCircle(x, y, radius, dayPaint);
            dayPaint.SetStyle(Paint.Style.Stroke);
            dayPaint.Color = new Color(calenderTextColor);
        }
    }
}