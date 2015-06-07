package com.github.sundeepk.compactcalendarview;

import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.Rect;
import android.view.MotionEvent;
import android.widget.OverScroller;

import com.github.sundeepk.compactcalendarview.domain.CalendarDayEvent;

import junit.framework.Assert;

import org.junit.Before;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.mockito.InOrder;
import org.mockito.Mock;
import org.mockito.runners.MockitoJUnitRunner;

import java.util.ArrayList;
import java.util.Calendar;
import java.util.Date;
import java.util.List;
import java.util.Locale;

import static org.junit.Assert.assertEquals;
import static org.mockito.Matchers.anyFloat;
import static org.mockito.Matchers.anyInt;
import static org.mockito.Matchers.eq;
import static org.mockito.Mockito.inOrder;
import static org.mockito.Mockito.times;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;

@RunWith(MockitoJUnitRunner.class)
public class CompactCalendarControllerTest {

    @Mock private Paint paint;
    @Mock private OverScroller overScroller;
    @Mock private Canvas canvas;
    @Mock private Rect rect;
    @Mock private Calendar calendar;
    @Mock private CompactCalendarView.CompactCalendarViewListener listener;
    @Mock private MotionEvent motionEvent;

    private static final String[] dayColumnNames = {"M", "T", "W", "T", "F", "S", "S"};

    CompactCalendarController underTest;

    @Before
    public void setUp(){
        underTest = new CompactCalendarController(paint, overScroller, rect, null, null, 0, 0, 0);
        underTest.setListener(listener);
    }

    @Test
    public void testListenerIsCalledOnMonthScroll(){
        //Sun, 01 Mar 2015 00:00:00 GMT
        Date expectedDateOnScroll = new Date(1425168000000L);

        when(motionEvent.getAction()).thenReturn(MotionEvent.ACTION_UP);

        //Set width of view so that scrolling will return a correct value
        underTest.onMeasure(720, 1080, 0, 0);

        //Sun, 08 Feb 2015 00:00:00 GMT
        underTest.setMonth(new Date(1423353600000L));

        //Scroll enough to push calender to next month
        underTest.onScroll(motionEvent, motionEvent, 600, 0);
        underTest.onDraw(canvas);
        underTest.onTouch(motionEvent);
        verify(listener).onMonthScroll(expectedDateOnScroll);
    }

    @Test
    public void testItReturnsFirstDayOfMonthAfterDateHasBeenSet(){
        //Sun, 01 Feb 2015 00:00:00 GMT
        Date expectedDate = new Date(1422748800000L);

        //Sun, 08 Feb 2015 00:00:00 GMT
        underTest.setMonth(new Date(1423353600000L));

        Date actualDate = underTest.getFirstDayOfCurrentMonth();
        assertEquals(expectedDate, actualDate);
    }

    @Test
    public void testItReturnsFirstDayOfMonth(){
        Calendar currentCalender =  Calendar.getInstance();
        currentCalender.set(Calendar.DAY_OF_MONTH, 1);
        currentCalender.set(Calendar.HOUR_OF_DAY, 0);
        currentCalender.set(Calendar.MINUTE, 0);
        currentCalender.set(Calendar.SECOND, 0);
        currentCalender.set(Calendar.MILLISECOND, 0);
        Date expectFirstDayOfMonth = currentCalender.getTime();

        Date actualDate = underTest.getFirstDayOfCurrentMonth();

        assertEquals(expectFirstDayOfMonth, actualDate);
    }

    @Test
    public void testItDrawsFirstLetterOfEachDay(){
        //simulate Feb month
        when(calendar.get(Calendar.DAY_OF_WEEK)).thenReturn(1);
        when(calendar.get(Calendar.MONTH)).thenReturn(1);
        when(calendar.getActualMaximum(Calendar.DAY_OF_MONTH)).thenReturn(28);

        underTest.drawMonth(canvas, calendar, 0);

        InOrder inOrder = inOrder(canvas);
        inOrder.verify(canvas).drawText(eq("M"), anyInt(), anyInt(), eq(paint));
        inOrder.verify(canvas).drawText(eq("T"), anyInt(), anyInt(), eq(paint));
        inOrder.verify(canvas).drawText(eq("W"), anyInt(), anyInt(), eq(paint));
        inOrder.verify(canvas).drawText(eq("T"), anyInt(), anyInt(), eq(paint));
        inOrder.verify(canvas).drawText(eq("F"), anyInt(), anyInt(), eq(paint));
        inOrder.verify(canvas).drawText(eq("S"), anyInt(), anyInt(), eq(paint));
        inOrder.verify(canvas).drawText(eq("S"), anyInt(), anyInt(), eq(paint));
    }

    @Test
    public void testItDrawsDaysOnCalender(){
        //simulate Feb month
        when(calendar.get(Calendar.DAY_OF_WEEK)).thenReturn(1);
        when(calendar.get(Calendar.MONTH)).thenReturn(1);
        when(calendar.getActualMaximum(Calendar.DAY_OF_MONTH)).thenReturn(28);

        underTest.drawMonth(canvas, calendar, 0);

        for(int dayColumn = 0, dayRow = 0; dayColumn <= 6; dayRow++){
            if(dayRow == 7){
                dayRow = 0;
                if(dayColumn <= 6){
                    dayColumn++;
                }
            }
            if(dayColumn == dayColumnNames.length){
                break;
            }
            if(dayColumn == 0){
                verify(canvas).drawText(eq(dayColumnNames[dayColumn]), anyInt(), anyInt(), eq(paint));
            }else{
                int day = ((dayRow - 1) * 7 + dayColumn + 1) - 6;
                if( day > 0 && day <= 28){
                    verify(canvas).drawText(eq(String.valueOf(day)), anyInt(), anyInt(), eq(paint));
                }
            }
        }
    }

    @Test
    public void testItAddsEvents(){
        //Sun, 01 Feb 2015 00:00:00 GMT
        List<CalendarDayEvent> events = getEvents(0, 30, 1422748800000L);
        for(CalendarDayEvent event : events){
            underTest.addEvent(event);
        }

        events = getEvents(0, 28, 1422748800000L);

        List<CalendarDayEvent> actualEvents = underTest.getEvents(new Date(1422748800000L));
        Assert.assertEquals(events, actualEvents);
    }


    @Test
    public void testItRemovesEvents(){
        //Sun, 01 Feb 2015 00:00:00 GMT
        List<CalendarDayEvent> events = getEvents(0, 30, 1422748800000L);
        for(CalendarDayEvent event : events){
            underTest.addEvent(event);
        }

        underTest.removeEvent(events.get(0));
        underTest.removeEvent(events.get(1));
        underTest.removeEvent(events.get(5));
        underTest.removeEvent(events.get(20));

        List<CalendarDayEvent> expectedEvents = getEvents(0, 28, 1422748800000L);
        expectedEvents.remove(events.get(0));
        expectedEvents.remove(events.get(1));
        expectedEvents.remove(events.get(5));
        expectedEvents.remove(events.get(20));

        List<CalendarDayEvent> actualEvents = underTest.getEvents(new Date(1422748800000L));
        Assert.assertEquals(expectedEvents, actualEvents);
    }

    @Test
    public void testItDrawsEventDaysOnCalendar(){
        //Sun, 07 Jun 2015 18:20:51 GMT
        List<CalendarDayEvent> events = getEvents(0, 30, 1433701251000L);
        for(CalendarDayEvent event : events){
            underTest.addEvent(event);
        }

        when(calendar.get(Calendar.MONTH)).thenReturn(5);
        when(calendar.get(Calendar.YEAR)).thenReturn(2015);

        underTest.drawEvents(canvas, calendar, 0);

        //should draw 2 less days because we don't draw events for the 1st day of month and the current day even if they exist
        verify(canvas, times(28)).drawCircle(anyFloat(), anyFloat(), anyFloat(), eq(paint));
    }


    private List<CalendarDayEvent> getEvents(int start, int days, long timeStamp) {
        Calendar currentCalender = Calendar.getInstance(Locale.getDefault());
        List<CalendarDayEvent> eventList = new ArrayList<>();
        for(int i = start; i < days; i++){
            currentCalender.setTimeInMillis(timeStamp);
            currentCalender.set(Calendar.DATE, 1);
            currentCalender.set(Calendar.HOUR_OF_DAY, 0);
            currentCalender.set(Calendar.MINUTE, 0);
            currentCalender.set(Calendar.SECOND, 0);
            currentCalender.set(Calendar.MILLISECOND, 0);
            currentCalender.add(Calendar.DATE, i);
            eventList.add(new CalendarDayEvent(currentCalender.getTimeInMillis(), Color.BLUE));
        }
        return eventList;
    }

}