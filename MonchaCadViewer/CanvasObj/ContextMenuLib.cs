﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MonchaCadViewer.CanvasObj
{
    public static class ContextMenuLib
    {
        public static void LineContextMenu(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
                contextMenu.Items.Add(new Separator());

        }

        public static void DotContextMenu(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
                contextMenu.Items.Add(new Separator());
            AddItem("Edit", contextMenu);

        }

        public static void ViewContourMenu(ContextMenu contextMenu)
        {
            if (contextMenu.Items.Count > 0)
                contextMenu.Items.Add(new Separator());

            AddItem("Mirror", contextMenu);
        }

        public static void CadObjMenu(ContextMenu contextMenu)
        {
            AddItem("Fix", contextMenu);
            AddItem("Remove", contextMenu);
            AddItem("Render", contextMenu);
        }

        public static void MeshMenu(ContextMenu contextMenu)
        {
            AddItem("Create", contextMenu);
            AddItem("Refresh", contextMenu);
        }

        public static void DeviceTreeMenu(ContextMenu contextMenu)
        {
            AddItem("CanvasRectangle", contextMenu);
            AddItem("ZoneRectangle", contextMenu);
        }

        private static void AddItem(string Name, ContextMenu menu)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.Header = Name;
            menuItem.Click += MenuItem_Click;

            menu.Items.Add(menuItem);
        }

        private static void MenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DependencyObject obj = sender as DependencyObject;

            while (obj != null && !(obj is ContextMenu))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            (obj as ContextMenu).DataContext = sender;
        }
    }
}
