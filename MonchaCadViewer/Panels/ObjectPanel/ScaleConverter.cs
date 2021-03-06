﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MonchaCadViewer.Panels
{
    public class ScaleConverter : IValueConverter
    {
        private double percent;
        private double invert;

        public ScaleConverter(bool Percent, bool Invert)
        {
            this.percent = Percent == true ? 100 : 1;
            this.invert = Invert == true ? (Percent == true ? 100 : 1) : 0;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Round(Math.Abs(this.invert - Math.Abs((double)value) * this.percent), 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null) return Math.Abs((this.invert - (double)value) / this.percent);
            else return 0;
        }
    }
}
