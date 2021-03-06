﻿using MahApps.Metro.Controls;
using MonchaCadViewer.CanvasObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MonchaCadViewer.Panels.ObjectPanel
{
    /// <summary>
    /// Логика взаимодействия для CadRectangleSizePanel.xaml
    /// </summary>
    public partial class CadRectangleSizePanel : Window
    {
        public CadRectangleSizePanel(CadRectangle cadRectangle)
        {
            InitializeComponent();
            this.Title = cadRectangle.ToolTip.ToString();
            WidthUpDn.DataContext = cadRectangle.LRect;
            WidthUpDn.SetBinding(NumericUpDown.ValueProperty, "X");
            HeightUpDn.DataContext = cadRectangle.LRect;
            HeightUpDn.SetBinding(NumericUpDown.ValueProperty, "Y");
        }

    }
}
