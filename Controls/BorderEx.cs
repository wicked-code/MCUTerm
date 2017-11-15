using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MCUTerm.Controls
{
    public class BorderEx : Border
    {
        public bool IgnoreContentSize
        {
            get => (bool)base.GetValue(IgnoreContentSizeProperty);
            set => base.SetValue(IgnoreContentSizeProperty, value);
        }

        public static readonly DependencyProperty IgnoreContentSizeProperty =
          DependencyProperty.Register("IgnoreContentSize", typeof(bool), typeof(BorderEx), new FrameworkPropertyMetadata(false));

        protected override Size MeasureOverride(Size availableSize)
        {
            if (IgnoreContentSize)
                return new Size(BorderThickness.Left + BorderThickness.Right + MinWidth, BorderThickness.Top + BorderThickness.Bottom + MinHeight);
            else
                return base.MeasureOverride(availableSize);
        }
    }

    public class ShadowBorder : Decorator
    {
        public double ShadowThickness
        {
            get { return (double)GetValue(ShadowThicknessProperty); }
            set { SetValue(ShadowThicknessProperty, value); }
        }

        public Color ShadowColor
        {
            get { return (Color)GetValue(ShadowColorProperty); }
            set { SetValue(ShadowColorProperty, value); }
        }

        public static readonly DependencyProperty ShadowThicknessProperty
            = DependencyProperty.Register("ShadowThickness", typeof(double), typeof(ShadowBorder),
                                          new FrameworkPropertyMetadata(defaultThickness,
                                              FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShadowColorProperty
            = DependencyProperty.Register("ShadowColor", typeof(Color), typeof(ShadowBorder),
                                          new FrameworkPropertyMetadata(Colors.Transparent,
                                              FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        const double defaultThickness = 7;
        const double minOpacity = 2;
        const double growFactor = 1.2;

        protected override void OnRender(DrawingContext dc)
        {
            double dpiScaleX = VisualTreeHelper.GetDpi(this).DpiScaleX;
            double thicknessInPixels = Math.Round(ShadowThickness * dpiScaleX);

            Color baseColor = ShadowColor;
            double opacityStep = thicknessInPixels / 7;
            for(double indent = 0; indent <= thicknessInPixels; indent += dpiScaleX)
            {
                double indent2x = indent + indent;
                Rect rect = new Rect(indent, indent,
                                     Math.Max(0.0, RenderSize.Width - indent2x),
                                     Math.Max(0.0, RenderSize.Height - indent2x));

                double opacityPos = indent / opacityStep;
                baseColor.A = (byte)Math.Round(minOpacity + (opacityPos * opacityPos) / growFactor);

                Pen pen = new Pen(new SolidColorBrush(baseColor), dpiScaleX);
                dc.DrawRectangle(null, pen, rect);
            }
        }

        protected double CalculateRoundedThickness()
        {
            double dpiScaleX = VisualTreeHelper.GetDpi(this).DpiScaleX;
            return Math.Round(ShadowThickness * dpiScaleX) / dpiScaleX;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            double thickness = CalculateRoundedThickness();

            if (hitTestParameters.HitPoint.X <= thickness || hitTestParameters.HitPoint.X >= RenderSize.Width - thickness ||
                hitTestParameters.HitPoint.Y <= thickness || hitTestParameters.HitPoint.Y >= RenderSize.Height - thickness)
            {
                return null;
            }

            return base.HitTestCore(hitTestParameters);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            double thickness = CalculateRoundedThickness();
            double thickness2x = thickness + thickness;

            UIElement child = Child;
            Size mySize = new Size(thickness2x, thickness2x);
            if (child != null)
            {
                Size childConstraint = new Size(Math.Max(0.0, constraint.Width - mySize.Width),
                                                Math.Max(0.0, constraint.Height - mySize.Height));

                child.Measure(childConstraint);
                Size childSize = child.DesiredSize;

                mySize.Width += childSize.Width;
                mySize.Height += childSize.Height;
            }

            return mySize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            //  arrange child
            UIElement child = Child;
            if (child != null)
            {
                double thickness = CalculateRoundedThickness();
                double thickness2x = thickness + thickness;

                Rect childRect = new Rect(thickness, thickness,
                                          Math.Max(0.0, finalSize.Width - thickness2x),
                                          Math.Max(0.0, finalSize.Height - thickness2x));
                child.Arrange(childRect);
            }

            return finalSize;
        }
    }
}

