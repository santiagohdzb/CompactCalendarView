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
using Android.Views.Animations;

namespace CompactCalendarView
{
    public class CollapsingAnimation : Animation
    {
        private int targetHeight;
        private CompactCalendarView view;
        private bool down;
        private float currentGrow;
        private CompactCalendarController compactCalendarController;

        public CollapsingAnimation(CompactCalendarView view, CompactCalendarController compactCalendarController, int targetHeight, bool down)
        {
            this.view = view;
            this.compactCalendarController = compactCalendarController;
            this.targetHeight = targetHeight;
            this.down = down;
        }

        protected override void ApplyTransformation(float interpolatedTime, Transformation t)
        {
            currentGrow += 2.4f;
            float grow = 0;
            int newHeight;
            if (down) {
                newHeight = (int)(targetHeight * interpolatedTime);
                grow = (float)(interpolatedTime * (targetHeight * 2));
            }
            else {
                float progress = 1 - interpolatedTime;
                newHeight = (int)(targetHeight * progress);
                grow = (float)(progress * (targetHeight * 2));
            }
            compactCalendarController.setGrowProgress(grow);
            view.LayoutParameters.Height = newHeight;
            view.RequestLayout();
        }

        public override bool WillChangeBounds()
        {
            return true;
        }
    }
}