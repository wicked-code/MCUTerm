using System;

namespace MCUTerm.Controls
{
    class DPIHelper
    {
        public static double RoundByPixelBound(double value, double dpiScaleX)
        {
            return Math.Round(value * dpiScaleX, MidpointRounding.ToEven) / dpiScaleX;
        }

    }
}
