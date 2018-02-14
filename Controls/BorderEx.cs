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

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (SnapsToDevicePixels == false)
                return base.ArrangeOverride(finalSize);

            //  arrange child
            UIElement child = Child;
            if (child != null)
            {
                double dpiScaleX = VisualTreeHelper.GetDpi(this).DpiScaleX;
                Thickness roundedThickness = new Thickness(
                    DPIHelper.RoundByPixelBound(BorderThickness.Left, dpiScaleX),
                    DPIHelper.RoundByPixelBound(BorderThickness.Top, dpiScaleX),
                    DPIHelper.RoundByPixelBound(BorderThickness.Right, dpiScaleX),
                    DPIHelper.RoundByPixelBound(BorderThickness.Bottom, dpiScaleX)
                );

                Rect childRect = new Rect(roundedThickness.Left, roundedThickness.Top,
                                          Math.Max(0.0, finalSize.Width - roundedThickness.Left - roundedThickness.Right),
                                          Math.Max(0.0, finalSize.Height - roundedThickness.Top - roundedThickness.Bottom));
                child.Arrange(childRect);
            }

            return finalSize;
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
        const double growFactor = 2.7;

        protected override void OnRender(DrawingContext dc)
        {
            double dpiScaleX = VisualTreeHelper.GetDpi(this).DpiScaleX;

            double thickness = DPIHelper.RoundByPixelBound(ShadowThickness, dpiScaleX);
            double thickness2x = thickness + thickness;
            Rect rect = new Rect(thickness, thickness, RenderSize.Width - thickness2x, RenderSize.Height - thickness2x);
            dc.DrawRectangle(new SolidColorBrush(Colors.White), null, rect);

            Color baseColor = ShadowColor;
            double step = 1.0 / dpiScaleX;
            int thicknessInPixels = (int)Math.Round(ShadowThickness * dpiScaleX);
            for (int i = 0; i <= thicknessInPixels; i++)
            {
                double indent = i / dpiScaleX;
                double indent2x = indent + indent;
                rect = new Rect(indent, indent,
                                Math.Max(0.0, RenderSize.Width - indent2x),
                                Math.Max(0.0, RenderSize.Height - indent2x));

                baseColor.A = (byte)Math.Round(minOpacity + (i * i) / growFactor);

                Pen pen = new Pen(new SolidColorBrush(baseColor), 1.0 / dpiScaleX);
                dc.DrawRectangle(null, pen, rect);
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            double dpiScaleX = VisualTreeHelper.GetDpi(this).DpiScaleX;
            double thickness = DPIHelper.RoundByPixelBound(ShadowThickness, dpiScaleX);

            if (hitTestParameters.HitPoint.X <= thickness || hitTestParameters.HitPoint.X >= RenderSize.Width - thickness ||
                hitTestParameters.HitPoint.Y <= thickness || hitTestParameters.HitPoint.Y >= RenderSize.Height - thickness)
            {
                return null;
            }

            return base.HitTestCore(hitTestParameters);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            double dpiScaleX = VisualTreeHelper.GetDpi(this).DpiScaleX;
            double thickness = DPIHelper.RoundByPixelBound(ShadowThickness, dpiScaleX);
            double thickness2x = thickness + thickness;

            UIElement child = Child;
            Size mySize = new Size(thickness2x, thickness2x);
            if (child != null)
            {
                Size childConstraint = new Size(Math.Max(0.0, constraint.Width - mySize.Width),
                                                Math.Max(0.0, constraint.Height - mySize.Height));

                child.Measure(childConstraint);
                Size childSize = child.DesiredSize;

                mySize.Width += DPIHelper.RoundByPixelBound(childSize.Width, dpiScaleX);
                mySize.Height += DPIHelper.RoundByPixelBound(childSize.Height, dpiScaleX);
            }

            return mySize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            //  arrange child
            UIElement child = Child;
            if (child != null)
            {
                double dpiScaleX = VisualTreeHelper.GetDpi(this).DpiScaleX;
                double thickness = DPIHelper.RoundByPixelBound(ShadowThickness, dpiScaleX);
                double thickness2x = thickness + thickness;

                double width = DPIHelper.RoundByPixelBound(finalSize.Width, dpiScaleX);
                double height = DPIHelper.RoundByPixelBound(finalSize.Height, dpiScaleX);
                Rect childRect = new Rect(thickness, thickness,
                                          Math.Max(0.0, width - thickness2x),
                                          Math.Max(0.0, height - thickness2x));
                child.Arrange(childRect);
            }

            return finalSize;
        }
    }
}

