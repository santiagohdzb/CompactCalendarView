using Android.Animation;
using Android.Views.Animations;

namespace CompactCalendarView
{
    public class AnimationHandler
    {
        public static int HEIGHT_ANIM_DURATION_MILLIS = 650;
        public static int INDICATOR_ANIM_DURATION_MILLIS = 700;
        private CompactCalendarController compactCalendarController;
        private CompactCalendarView compactCalendarView;

        public AnimationHandler(CompactCalendarController compactCalendarController, CompactCalendarView compactCalendarView)
        {
            this.compactCalendarController = compactCalendarController;
            this.compactCalendarView = compactCalendarView;
        }

        public void openCalendar()
        {
            var heightAnim = getCollapsingAnimation(true);
            heightAnim.Duration = HEIGHT_ANIM_DURATION_MILLIS;
            heightAnim.Interpolator = new AccelerateDecelerateInterpolator();

            compactCalendarController.setAnimatingHeight(false);
            compactCalendarView.LayoutParameters.Height = 0;
            compactCalendarView.RequestLayout();
            compactCalendarView.StartAnimation(heightAnim);
        }

        public void closeCalendar()
        {
            var heightAnim = getCollapsingAnimation(false);
            heightAnim.Duration = HEIGHT_ANIM_DURATION_MILLIS;
            heightAnim.Interpolator = new AccelerateDecelerateInterpolator();

            compactCalendarController.setAnimatingHeight(false);
            compactCalendarView.LayoutParameters.Height = compactCalendarView.Height;
            compactCalendarView.RequestLayout();

            compactCalendarView.StartAnimation(heightAnim);
        }

        public void openCalendarWithAnimation()
        {
            var indicatorAnim = getIndicatorAnimator(1f, 55f);
            var heightAnim = getCollapsingAnimation(indicatorAnim, true);

            compactCalendarController.setAnimatingHeight(true);
            compactCalendarView.LayoutParameters.Height = 0;
            compactCalendarView.RequestLayout();

            compactCalendarView.StartAnimation(heightAnim);
        }

        public void closeCalendarWithAnimation()
        {
            var indicatorAnim = getIndicatorAnimator(55f, 1f);
            var heightAnim = getCollapsingAnimation(indicatorAnim, false);

            compactCalendarController.setAnimatingHeight(true);
            compactCalendarView.LayoutParameters.Height = compactCalendarView.Height;
            compactCalendarView.RequestLayout();

            compactCalendarView.StartAnimation(heightAnim);
        }

        private Animation getCollapsingAnimation(bool isCollapsing)
        {
            return new CollapsingAnimation(compactCalendarView, compactCalendarController, compactCalendarController.getTargetHeight(), isCollapsing);
        }

        private Animation getCollapsingAnimation(Animator animIndicator, bool isCollapsing)
        {
            var heightAnim = getCollapsingAnimation(isCollapsing);
            heightAnim.Duration = HEIGHT_ANIM_DURATION_MILLIS;
            heightAnim.Interpolator = new AccelerateDecelerateInterpolator();
            heightAnim.SetAnimationListener(new AnimationHandlerAnimationListener(this, animIndicator, isCollapsing));

            return heightAnim;
        }

        public class AnimationHandlerAnimationListener : Java.Lang.Object, Animation.IAnimationListener
        {
            private AnimationHandler _context;
            private Animator _animIndicator;
            private bool _isCollapsing;

            public AnimationHandlerAnimationListener(AnimationHandler context, Animator animIndicator, bool isCollapsing)
            {
                _context = context;
                _animIndicator = animIndicator;
                _isCollapsing = isCollapsing;
            }

            public void OnAnimationRepeat(Animation animation)
            {
            }

            public void OnAnimationEnd(Animation animation)
            {
                if (_isCollapsing) {
                    _context.compactCalendarController.setAnimatingHeight(false);
                    _context.compactCalendarController.setAnimatingIndicators(true);
                    _animIndicator.Start();
                }
                else {
                    _context.compactCalendarController.setAnimatingIndicators(false);
                }
            }

            public void OnAnimationStart(Animation animation)
            {
                if (!_isCollapsing) {
                    _context.compactCalendarController.setAnimatingHeight(false);
                    _context.compactCalendarController.setAnimatingIndicators(true);
                    _animIndicator.Start();
                }
            }
        }

        private Animator getIndicatorAnimator(float from, float to)
        {
            var animIndicator = ValueAnimator.OfFloat(from, to);
            animIndicator.SetDuration(INDICATOR_ANIM_DURATION_MILLIS);
            animIndicator.SetInterpolator(new OvershootInterpolator());
            animIndicator.AddUpdateListener(new CustomUpdateListener(this));
            animIndicator.AddListener(new CustomAnimatorListener(this));

            return animIndicator;
        }

        public class CustomAnimatorListener : Java.Lang.Object, Animator.IAnimatorListener
        {
            private AnimationHandler _context;
            public CustomAnimatorListener(AnimationHandler context)
            {
                _context = context;
            }

            public void OnAnimationEnd(Animator animation)
            {
                _context.compactCalendarController.setAnimatingIndicators(false);
                _context.compactCalendarView.Invalidate();
            }

            public void OnAnimationCancel(Animator animation)
            {
            }
            public void OnAnimationRepeat(Animator animation)
            {
            }
            public void OnAnimationStart(Animator animation)
            {
            }
        }

        public class CustomUpdateListener : Java.Lang.Object, ValueAnimator.IAnimatorUpdateListener
        {
            private AnimationHandler _context;

            public CustomUpdateListener(AnimationHandler context)
            {
                _context = context;
            }

            public void OnAnimationUpdate(ValueAnimator animation)
            {
                _context.compactCalendarController.setGrowFactorIndicator((float)animation.AnimatedValue);
                _context.compactCalendarView.Invalidate();
            }
        }
    }
}