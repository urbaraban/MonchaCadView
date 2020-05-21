﻿using MahApps.Metro.Controls;
using Microsoft.Win32;
using MonchaCadViewer.Calibration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using AppSt = MonchaCadViewer.Properties.Settings;
using System.Linq;
using System.Diagnostics;
using KompasLib.Tools;
using MonchaCadViewer.CanvasObj;
using MonchaCadViewer.Format;
using System.Globalization;
using MonchaSDK;
using MonchaSDK.Device;
using MonchaSDK.Object;
using System.Runtime.InteropServices;
using System.Windows.Data;

namespace MonchaCadViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static Dispatcher dispatcher = System.Windows.Application.Current.Dispatcher;

        private string qrpath = string.Empty;

        //private DotShape[,] BaseMeshRectangles;

        public MainWindow()
        {
            InitializeComponent();
            LoadMoncha();
            // MainCanvas = dot_canvas;

            RedToggle.Resources["ToggleSwitchFillOn"] = Brushes.Red;
            RedToggle.UpdateLayout();

            CanvasBoxBorder.BorderThickness = new Thickness(Math.Min(AppSt.Default.base_width, AppSt.Default.base_height) * 0.01);
            CadCanvas cadCanvas = new CadCanvas(MonchaHub.Size);
            cadCanvas.Focusable = true;
            cadCanvas.LayoutUpdated += dot_canvas_LayoutUpdated;

            CanvasBox.Child = cadCanvas;

            ResizeCanvasBox(CanvasBox);



            AppSt.Default.PropertyChanged += Default_PropertyChanged;


        }



        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "cl_width":
                    MonchaHub.Size.X = AppSt.Default.base_width / AppSt.Default.cl_mash_multiplier;
                    break;
                case "cl_height":
                    MonchaHub.Size.Y = AppSt.Default.base_height / AppSt.Default.cl_mash_multiplier;
                    break;
                case "cl_deep":
                    MonchaHub.Size.Z = AppSt.Default.base_deep / AppSt.Default.cl_mash_multiplier;
                    break;
            }
        }

        private void LoadMoncha()
        {
            MonchaHub.Size.X = AppSt.Default.base_width;
            MonchaHub.Size.Y = AppSt.Default.base_height;
            MonchaHub.Size.Z = AppSt.Default.base_deep;
            MonchaHub.Size.M = new MonchaPoint3D(
                1 / AppSt.Default.cl_mash_multiplier,
                1 / AppSt.Default.cl_mash_multiplier,
                1 / AppSt.Default.cl_mash_multiplier);

            //check path to setting file
            if (AppSt.Default.cl_moncha_path == string.Empty)
                BrowseMoncha(); //select if not

            //recheck
            if (File.Exists(AppSt.Default.cl_moncha_path))
            {
                //send path to hub class
                MonchaHub.Load(AppSt.Default.cl_moncha_path);

                WidthUpDn.DataContext = MonchaHub.Size;
                WidthUpDn.SetBinding(NumericUpDown.ValueProperty, "X");

                HeightUpD.DataContext = MonchaHub.Size;
                HeightUpD.SetBinding(NumericUpDown.ValueProperty, "Y");

                DeepUpDn.DataContext = MonchaHub.Size;
                DeepUpDn.SetBinding(NumericUpDown.ValueProperty, "Z");

                MashMultiplierUpDn.DataContext = MonchaHub.Size.M;
                MashMultiplierUpDn.SetBinding(NumericUpDown.ValueProperty, "X");

                if (MonchaHub.Devices != null)
                {
                    DeviceTree.Items.Clear();

                    DeviceCombo.DisplayMemberPath = "HWIdentifier";
                    DeviceCombo.SelectedValuePath = "HWIdentifier";
                    DeviceCombo.ItemsSource = MonchaHub.Devices;
                    DeviceCombo.DataContext = MonchaHub.Devices;

                    LaserMetersCombo.DisplayMemberPath = "Adress";
                    LaserMetersCombo.SelectedValuePath = "Adress";
                    LaserMetersCombo.ItemsSource = MonchaHub.LMeters;
                    LaserMetersCombo.DataContext = MonchaHub.LMeters;

                    foreach (MonchaDevice device in MonchaHub.Devices)
                    {
                        TreeViewItem treeViewDevice = new TreeViewItem();
                        treeViewDevice.Header = device.HWIdentifier;
                        treeViewDevice.DataContext = device;
                        treeViewDevice.ContextMenu = new ContextMenu();
                        ContextMenuLib.DeviceTreeMenu(treeViewDevice.ContextMenu);
                        treeViewDevice.MouseLeftButtonUp += LoadDeviceSetting;
                        treeViewDevice.ContextMenuClosing += TreeViewDevice_ContextMenuClosing;

                        TreeViewItem treeViewBaseMesh = new TreeViewItem();
                        treeViewBaseMesh.Header = "BaseMesh";
                        treeViewBaseMesh.DataContext = device.BaseMesh;
                        treeViewBaseMesh.MouseDoubleClick += TreeBaseMesh;

                        TreeViewItem treeViewVirtualMesh = new TreeViewItem();
                        treeViewVirtualMesh.Header = "VirtualMesh";
                        treeViewVirtualMesh.DataContext = device.VirtualMesh;
                        treeViewVirtualMesh.MouseDoubleClick += TreeViewVirtualMesh_MouseDoubleClick;

      
                        TreeViewItem treeViewCalculationMesh = new TreeViewItem();
                        treeViewCalculationMesh.Header = "CalculateMesh";
                        treeViewCalculationMesh.DataContext = device.CalculateMesh;
                        treeViewCalculationMesh.MouseDoubleClick += TreeViewCalculationMesh_MouseDoubleClick;
                        
                        if (treeViewCalculationMesh.ContextMenu == null) treeViewCalculationMesh.ContextMenu = new System.Windows.Controls.ContextMenu();
                        ContextMenuLib.MeshMenu(treeViewCalculationMesh.ContextMenu);
                        treeViewCalculationMesh.ContextMenuClosing += TreeViewBaseMesh_ContextMenuClosing;

                         treeViewDevice.Items.Add(treeViewCalculationMesh);
  

                        if (treeViewBaseMesh.ContextMenu == null) treeViewBaseMesh.ContextMenu = new System.Windows.Controls.ContextMenu();
                        ContextMenuLib.MeshMenu(treeViewBaseMesh.ContextMenu);
                        treeViewBaseMesh.ContextMenuClosing += TreeViewBaseMesh_ContextMenuClosing;

                        if (treeViewVirtualMesh.ContextMenu == null) treeViewVirtualMesh.ContextMenu = new System.Windows.Controls.ContextMenu();
                        ContextMenuLib.MeshMenu(treeViewVirtualMesh.ContextMenu);
                        treeViewVirtualMesh.ContextMenuClosing += TreeViewBaseMesh_ContextMenuClosing;

                        treeViewDevice.Items.Add(treeViewBaseMesh);
                        treeViewDevice.Items.Add(treeViewVirtualMesh);
                       
                        DeviceTree.Items.Add(treeViewDevice);
                        DeviceTree.ExpandSubtree();
                    }


                }
            }
        }

        private void DeviceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceCombo.SelectedItem is MonchaDevice tempdevice)
            {
                //LaserMeter
                LaserMetersCombo.SelectedValue = tempdevice.LMeter;

                LaserMeterToggle.DataContext = tempdevice.LMeter;
                LaserMeterToggle.SetBinding(ToggleSwitch.IsOnProperty, "IsTurn");

                DistanceUpDn.DataContext = tempdevice.LMeter;
                DistanceUpDn.SetBinding(NumericUpDown.ValueProperty, "Distance");

                //Device
                ScanRateRealSlider.DataContext = tempdevice;
                ScanRateRealSlider.SetBinding(Slider.ValueProperty, "ScanRateReal");

                ScanRateCalc.DataContext = tempdevice;
                ScanRateCalc.SetBinding(Slider.ValueProperty, "ScanRateCalc");

                RedUpDn.DataContext = tempdevice;
                RedUpDn.SetBinding(NumericUpDown.ValueProperty, "Red");

                GreenUpDn.DataContext = tempdevice;
                GreenUpDn.SetBinding(NumericUpDown.ValueProperty, "Green");

                BlueUpDn.DataContext = tempdevice;
                BlueUpDn.SetBinding(NumericUpDown.ValueProperty, "Blue");

                AlphaSlider.DataContext = tempdevice;
                AlphaSlider.SetBinding(Slider.ValueProperty, "Alpha");

                FPSUpDn.DataContext = tempdevice;
                FPSUpDn.SetBinding(NumericUpDown.ValueProperty, "FPS");

                AppSt.Default.cl_crs = (double)tempdevice.CRS;

                AngleWaitSlider.DataContext = tempdevice;
                AngleWaitSlider.SetBinding(Slider.ValueProperty, "StartLineWait");

                EndBlankSlider.DataContext = tempdevice;
                EndBlankSlider.SetBinding(Slider.ValueProperty, "EndBlanckWait");

                StartBlankSlider.DataContext = tempdevice;
                StartBlankSlider.SetBinding(Slider.ValueProperty, "StartBlankWait");

                InvertXtoggle.DataContext = tempdevice;
                InvertXtoggle.SetBinding(ToggleSwitch.IsOnProperty, "InvertedX");

                InvertYtoggle.DataContext = tempdevice;
                InvertYtoggle.SetBinding(ToggleSwitch.IsOnProperty, "InvertedY");


            }
        }


        private void TreeViewCalculationMesh_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem BaseMeshItem && BaseMeshItem.Parent is TreeViewItem DeviceTree)
            {
                if (DeviceTree.DataContext is MonchaDevice device && BaseMeshItem.DataContext is MonchaDeviceMesh mesh && CanvasBox.Child is CadCanvas canvas)
                {
                    canvas.DrawMesh(mesh, device, false, false, false);
                }
            }
        }

        private void TreeViewVirtualMesh_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem BaseMeshItem && BaseMeshItem.Parent is TreeViewItem DeviceTree)
            {
                if (DeviceTree.DataContext is MonchaDevice device && BaseMeshItem.DataContext is MonchaDeviceMesh mesh && CanvasBox.Child is CadCanvas canvas)
                {
                    canvas.DrawMesh(device.VirtualMesh, device, false, true, false);
                }
            }
        }

        private void TreeViewDevice_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                if (viewItem.ContextMenu.DataContext is MenuItem cmindex)
                    switch (cmindex.Header)
                    {
                        case "Rectangle":
                            if (sender is TreeViewItem treeView)
                            {
                                SendProcessor.DrawZone(viewItem.DataContext as MonchaDevice);
                            }
                            break;
                    }
            }
        }

        private void TreeViewBaseMesh_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                if (viewItem.ContextMenu.DataContext is MenuItem cmindex)
                {
                    switch (cmindex.Header)
                    {
                        case "Create":
                            if (sender is TreeViewItem meshTree &&
                                meshTree.Parent is TreeViewItem DeviceTree)
                            {
                                CreateGridWindow createGridWindow = new CreateGridWindow(DeviceTree.DataContext as MonchaDevice, meshTree.DataContext as MonchaDeviceMesh );
                                createGridWindow.ShowDialog();
                            }
                            break;
                        case "Refresh":
                            if (sender is TreeViewItem meshTree2 &&
                                meshTree2.Parent is TreeViewItem DeviceTree2)
                                if (DeviceTree2.DataContext is MonchaDevice device2 && meshTree2.DataContext is MonchaDeviceMesh deviceMesh)
                                {
                                    deviceMesh = mws.GetMeshDot(device2.HWIdentifier, deviceMesh.Name);
                                }
                            break;
                    }
                }
            }
        }

        private void LoadDeviceSetting(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem treeViewItem)
            DeviceCombo.SelectedValue = ((MonchaDevice)treeViewItem.DataContext).HWIdentifier;
        }

        private void TreeBaseMesh(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem BaseMeshItem && BaseMeshItem.Parent is TreeViewItem DeviceTree)
            {
                if (DeviceTree.DataContext is MonchaDevice device && BaseMeshItem.DataContext is MonchaDeviceMesh mesh && CanvasBox.Child is CadCanvas canvas)
                {
                    canvas.DrawMesh(device.BaseMesh, device, true, false, true);
                }
            }
        }

        private void BrowseMonchaBtn_Click(object sender, RoutedEventArgs e)
        {
            BrowseMoncha();
        }

        private void BrowseMoncha()
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Moncha (.mws)|*.mws|All Files (*.*)|*.*";
            if (fileDialog.ShowDialog() == true)
            {
                AppSt.Default.cl_moncha_path = fileDialog.FileName;
                AppSt.Default.Save();
                MonchaPathBox.Text = fileDialog.FileName;
                LoadMoncha();
            }
        }





        private void ResizeCanvasBox(Viewbox canvasbox)
        {
            if (canvasbox.Child is CadCanvas canvas)
            {
                CanvasBoxBorder.BorderThickness = new Thickness(Math.Min(canvas.Width, canvas.Height) * 0.001);
                canvasbox.StretchDirection = StretchDirection.UpOnly;
                canvasbox.UpdateLayout();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show("Сохранить настройки?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            switch (messageBoxResult)
            {
                case MessageBoxResult.Yes:
                    AppSt.Default.Save();
                    MonchaHub.Save();

                    break;
                case MessageBoxResult.No:
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }

        private void SplitLineUpDn_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (this.IsLoaded)
            {
                MonchaHub.Size.M.Update(1 / e.NewValue.Value, false);
            }
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            MonchaHub.CanPlay = false;
            MonchaHub.Disconnect();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            this.Close();
        }


        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            MonchaHub.CanPlay = !MonchaHub.CanPlay;
            if (MonchaHub.CanPlay)
            {
                MonchaHub.Play();
                PlayBtn.Background = Brushes.YellowGreen;
            }
            else
            {
                PlayBtn.Background = Brushes.Yellow;
            }

        }


        private void SaveMeshBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCombo.SelectedItem is MonchaDevice device)
            {
                device.CalculatedMesh();
                MonchaHub.Save();
            }
        }





        private void OpenBtn_Click(object sender, EventArgs e)
        {
            WinForms.OpenFileDialog openFile = new WinForms.OpenFileDialog();
            openFile.Filter = "(*.frw; *.cdw; *.svg; *.dxf; *.stp)|*.frw; *.cdw; *.svg; *.dxf, *.stp| All Files (*.*)|*.*";

            if (AppSt.Default.save_work_folder == string.Empty)
            {
                WinForms.FolderBrowserDialog folderDialog = new WinForms.FolderBrowserDialog();

                if (folderDialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    AppSt.Default.save_work_folder = folderDialog.SelectedPath;
                    AppSt.Default.Save();
                }
            }

            openFile.InitialDirectory = AppSt.Default.save_work_folder;
            openFile.FileName = null;
            if (openFile.ShowDialog() == WinForms.DialogResult.OK)
                OpenFile(openFile.FileName);
        }

        private void OpenFile(string filename)
        {
            List<LObjectList> _actualFrames = new List<LObjectList>();

            if (filename.Split('.').Last() == "svg")
                _actualFrames.Add(SVG.Get(filename, AppSt.Default.svg_separator1, AppSt.Default.svg_separator2));

            else if (filename.Split('.').Last() == "dxf")
                _actualFrames.Add(DXF.Get(filename));
            else if (filename.Split('.').Last() == "stl")
                _actualFrames.Add(STL.Get(filename));
            else if ((filename.Split('.').Last() == "frw") || (filename.Split('.').Last() == "cdw"))
            {
                if (KmpsAppl.KompasAPI == null)
                {
                    Process.Start(filename);
                }
                else
                {
                    KmpsAppl.OpenFile(filename);
                }
            }


            /*
            else if (filename.Split('.').Last() == "ild")
                MonchaWrt(filename);
            */

            if (_actualFrames.Count > 0)
                AppSt.Default.stg_last_file_path = filename;
            else return;

            AppSt.Default.Save();

            ContourProcessor(false, _actualFrames);
        }

        private void ContourProcessor(bool remove, List<LObjectList> _frames, bool show = true)
        {
            if (remove)
            {
                FrameTree.Items.Clear();
                FrameStack.Children.Clear();
            }

            foreach (LObjectList contoursList in _frames)
            {
                Border _viewborder = new Border();
                _viewborder.BorderThickness = new Thickness(1, 0, 1, 0);
                _viewborder.BorderBrush = Brushes.Gray;
                _viewborder.Width = FrameStack.ActualHeight;
                _viewborder.Background = Brushes.Gray;

                Viewbox _viewbox = new Viewbox();
                _viewbox.Stretch = Stretch.UniformToFill;
                _viewbox.MaxHeight = FrameStack.ActualHeight;
                _viewbox.MaxWidth = FrameStack.ActualHeight;
                _viewbox.Margin = new Thickness(0);
                _viewbox.DataContext = contoursList;
                _viewbox.ClipToBounds = true;
                _viewbox.MouseLeftButtonUp += DrawTreeContour;

                Viewbox _canvasviewbox = new Viewbox();
                _canvasviewbox.Stretch = Stretch.Uniform;
                _canvasviewbox.StretchDirection = StretchDirection.UpOnly;
                _canvasviewbox.MaxHeight = AppSt.Default.base_height / AppSt.Default.cl_mash_multiplier;
                _canvasviewbox.MaxWidth = AppSt.Default.base_width / AppSt.Default.cl_mash_multiplier;
                _canvasviewbox.Margin = new Thickness(0);

                CadCanvas _canvas = new CadCanvas(MonchaHub.Size);
                _canvas.Background = Brushes.White;

                _canvasviewbox.Child = _canvas;
                _viewbox.Child = _canvasviewbox;
                _viewborder.Child = _viewbox;
                FrameStack.Children.Add(_viewborder);

                _canvas.DrawOnCanvas(contoursList, false, false, true);

                TreeViewItem contourTree = new TreeViewItem();
                contourTree.Header = contoursList.DisplayName == string.Empty ? "Frame " + FrameTree.Items.Count : "Frame:" + contoursList.DisplayName;
                contourTree.DataContext = contoursList;
                contourTree.MouseLeftButtonUp += DrawTreeContour;

                FrameTree.Items.Add(contourTree);

                if (show && CanvasBox.Child is CadCanvas canvas) canvas.DrawOnCanvas(contoursList, true, false, true);
            }
        }

        private void DrawTreeContour(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem viewItem)
            {
                //_selectedindex = FrameTree.Items.IndexOf(viewItem);
                if (viewItem.DataContext is LObjectList tempList && CanvasBox.Child is CadCanvas canvas)
                    canvas.DrawOnCanvas(tempList, true, Keyboard.Modifiers == ModifierKeys.Shift, true);
            }

            if (sender is Viewbox viewbox)
            {
                //_selectedindex = FrameStack.Children.IndexOf(viewbox.Parent as Border);
                if (viewbox.DataContext is LObjectList tempList && CanvasBox.Child is CadCanvas canvas)
                    canvas.DrawOnCanvas(tempList, true, Keyboard.Modifiers == ModifierKeys.Shift, true);
            }
        }





        private void OpenBtn_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                foreach (string fileLoc in filePaths)
                    if (File.Exists(fileLoc))
                        OpenFile(fileLoc);
            }
            OpenBtn.Background = Brushes.Gainsboro;
        }

        private void OpenBtn_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
            OpenBtn.Background = Brushes.Yellow;
            string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
            foreach (string fileLoc in filePaths) //переберает всю инфу пока не найдет строку адреса
                if (File.Exists(fileLoc))
                    OpenBtn.Content = "Открыть " + fileLoc.Split('\\').Last();
        }

        private void OpenBtn_DragLeave(object sender, EventArgs e)
        {
            OpenBtn.Content = "Открыть";
            OpenBtn.Background = Brushes.Gainsboro;
        }

        private void OpenBtn_DragOver(object sender, DragEventArgs e)
        {
            OpenBtn.Background = Brushes.Gainsboro;
        }

        private void ReloadBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AppSt.Default.stg_last_file_path != string.Empty)
                if (File.Exists(AppSt.Default.stg_last_file_path))
                    OpenFile(AppSt.Default.stg_last_file_path);
        }

        private void MashMultiplierUpDn_ValueIncremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            args.Interval = 0;
            MashMultiplierUpDn.Value = AppSt.Default.cl_mash_multiplier * 10;
            AppSt.Default.cl_mash_multiplier = MashMultiplierUpDn.Value.Value;
            AppSt.Default.Save();
        }

        private void MashMultiplierUpDn_ValueDecremented(object sender, NumericUpDownChangedRoutedEventArgs args)
        {
            args.Interval = 0;
            MashMultiplierUpDn.Value = AppSt.Default.cl_mash_multiplier / 10;
            AppSt.Default.cl_mash_multiplier = MashMultiplierUpDn.Value.Value;
            AppSt.Default.Save();
        }

        private void ScanRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.IsLoaded)
            {
                if (ScanRateAnchor.IsChecked.Value) ScanRateCalc.Value = ScanRateRealSlider.Value;
            }
        }

        private void RefreshLaser_Click_1(object sender, RoutedEventArgs e)
        {
            LoadMoncha();
            treeView.UpdateLayout();
        }


        private void dot_canvas_LayoutUpdated(object sender, EventArgs e)
        {
            if (RealTimeToggle.IsOn)
                SendProcessor.Worker(CanvasBox);
        }

        private void LineBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CanvasBox.Child is CadCanvas canvas)
            {
                if (canvas.Status != 1)
                    canvas.Status = 1;
                else 
                    canvas.Status = 0;
                if (canvas.Status == 1) 
                    (sender as Button).Background = Brushes.Green;
                else
                    (sender as Button).Background = Brushes.White;
            }
        }


        private void DeviceCombo_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DeviceCombo.SelectedIndex == -1) DeviceCombo.SelectedIndex = 0;
        }

        private void MorphToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
                if (CanvasBox.Child is CadCanvas cadCanvas)
                    cadCanvas.MorphMesh = MorphToggle.IsOn;
        }

        private void HorizontalToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
                if (CanvasBox.Child is CadCanvas cadCanvas)
                    cadCanvas.HorizontalMesh = HorizontalToggle.IsOn;
        }

        private void RefreshFrameBtn_Click(object sender, RoutedEventArgs e)
        {
            SendProcessor.Worker(CanvasBox);
        }

        private void RealTimeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            AppSt.Default.int_realtime = RealTimeToggle.IsOn;
        }


        private void ScanRateRealSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                Point point = Mouse.GetPosition(slider);
                slider.Value = slider.Minimum + (point.X / slider.ActualWidth) * (slider.Maximum - slider.Minimum);
            }
        }

        private void kmpsConnectToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI == null)
            {
                KmpsAppl.Connect();
                KmpsAppl.ChangeDoc += KmpsAppl_ChangeDoc;
                KmpsAppl.ConnectBoolEvent += KmpsAppl_ConnectBoolEvent;

                kmpsConnectToggle.IsOn = KmpsAppl.KompasAPI != null;

                KmpsAppl.SelectDoc();

            }
        }

        private void KmpsAppl_ChangeDoc(object sender, KmpsDoc e)
        {
            KmpsNameLbl.Invoke(new Action(() => {
                if (e.D7 != null)
                    KmpsNameLbl.Content = e.D7.Name;
            }));
        }

        private void KmpsAppl_ConnectBoolEvent(object sender, bool e)
        {
            kmpsConnectToggle.IsOn = e;
        }

        private void stlSeparatorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (svgSeparatorBox1.Text != string.Empty)
                AppSt.Default.svg_separator1 =  svgSeparatorBox1.Text.Last();
            if(svgSeparatorBox2.Text != string.Empty)
                AppSt.Default.svg_separator2 = svgSeparatorBox2.Text.Last();
        }

        private void kmpsSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                LObjectList lObjectList = KMPS.GetContour(AppSt.Default.cl_crs, MashMultiplierUpDn.Value.Value, false, true);
                ContourProcessor(false, new List<LObjectList>() { lObjectList });
            }

        }

        private void kmpsAddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (KmpsAppl.KompasAPI != null)
            {
                LObjectList lObjectList = KMPS.GetContour(AppSt.Default.cl_crs, MashMultiplierUpDn.Value.Value, true, true);
                ContourProcessor(false, new List<LObjectList>() { lObjectList });
            }
        }

      
        private void ScanRateAnchor_Click(object sender, RoutedEventArgs e)
        {
            AppSt.Default.int_scananchor = ScanRateAnchor.IsChecked.Value;
        }

        private void MainWindow1_KeyUp(object sender, KeyEventArgs e)
        {
            double _mult = Keyboard.Modifiers == ModifierKeys.Shift ? 10 : 1;
            double step = PointStepUpDn.Value.Value;

            switch (e.Key)
            {
                case Key.Q:
                    SelectNext();
                    break;
                case Key.W:
                    MoveCanvasSet(0, -step * _mult);
                    break;
                case Key.S:
                    MoveCanvasSet(0, step * _mult);
                    break;
                case Key.A:
                    MoveCanvasSet(-step * _mult, 0);
                    break;
                case Key.D:
                    MoveCanvasSet(step * _mult, 0);
                    break;
                case Key.F:
                    FixSelect();
                    break;

            }

            void SelectNext()
            {
                if (CanvasBox.Child is CadCanvas canvas)
                {
                    for (int i = 0; i < canvas.Children.Count; i++)
                    {
                        if (canvas.Children[i] is CadObject cadObject)
                        {
                            if (cadObject.IsSelected)
                            {
                                cadObject.IsSelected = false;
                                if (i + 1 < canvas.Children.Count)
                                    if (canvas.Children[i + 1] is CadObject cadObject2)
                                    {
                                        cadObject2.IsSelected = true;
                                            cadObject2.UpdateLayout();
                                    }
                                break;
                            }
                        }
                    }
                }
            }

            void MoveCanvasSet(double left, double top)
            {
                if (CanvasBox.Child is CadCanvas canvas)
                {
                    for (int i = 0; i < canvas.Children.Count; i++)
                    {
                        if (canvas.Children[i] is CadObject cadObject)
                        {
                            if (cadObject.IsSelected && cadObject.BaseContextPoint is MonchaPoint3D point && !point.IsFix)
                            {
                                point.Update(point.X + (canvas.ActualWidth * left) / canvas.ActualWidth, 
                                point.Y + (canvas.ActualHeight * top) / canvas.ActualHeight);


                                if (cadObject.DataContext is MonchaDeviceMesh mesh)
                                {
                                    if (mesh.OnlyEdge)
                                        mesh.OnEdge();
                                    else
                                        mesh.MorphMesh(point);
                                }
                            }
                        }
                    }
                }
            }

            void FixSelect()
            {
                if (CanvasBox.Child is CadCanvas canvas)
                {
                    for (int i = 0; i < canvas.Children.Count; i++)
                    {
                        if (canvas.Children[i] is CadObject cadObject)
                        {
                            if (cadObject.IsSelected && cadObject.BaseContextPoint is MonchaPoint3D point)
                            {
                                point.IsFix = !point.IsFix;
                            }
                        }
                    }
                }
            }
        }

        private void MainWindow1_TextInput(object sender, TextCompositionEventArgs e)
        {
            string keyChar = e.Text;
            if (QRBtn.IsChecked.Value)
            {
                qrpath += keyChar;
                LogBox.Items.Add((AppSt.Default.save_qr_path ? AppSt.Default.save_base_folder : AppSt.Default.save_work_folder) + @qrpath);
                if (File.Exists((AppSt.Default.save_qr_path ? AppSt.Default.save_base_folder : AppSt.Default.save_work_folder) + @qrpath))
                    {
                    Progressinfo.Content = "Файл найден!";
                    OpenFile((AppSt.Default.save_qr_path ? AppSt.Default.save_base_folder : AppSt.Default.save_work_folder) + @qrpath);
                    }
                else
                {
                    Progressinfo.Content = "Файл не найден!";
                }
            }
        }

        private void MainWindow1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (QRBtn.IsChecked.Value)
                InputLanguageManager.SetInputLanguage(this, new CultureInfo("ru-RU"));

            int Temp = KeyInterop.VirtualKeyFromKey(e.Key);
            //Debug.WriteLine(Temp);
            // KeyInterop.KeyFromVirtualKey(e.Key);
            if (qrpath.Contains("#"))
            {
                qrpath = string.Empty;
                AppSt.Default.save_qr_path = true;
                
            }
            else if (qrpath.Contains("&"))
            {
                qrpath = string.Empty;
                AppSt.Default.save_qr_path = false;

            }
            QrToggler.IsOn = AppSt.Default.save_qr_path;
        }

        private void QRBtn_Click(object sender, RoutedEventArgs e)
        {


        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CanvasBox.Child is CadCanvas canvas)
            {
                canvas.Children.Clear();
            }
        }

        private void QrToggler_Toggled(object sender, RoutedEventArgs e)
        {
            AppSt.Default.save_qr_path = QrToggler.IsOn;
            AppSt.Default.Save();
        }

        private void BaseFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog WorkFolderSlct = new System.Windows.Forms.FolderBrowserDialog();

            if (WorkFolderSlct.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (WorkFolderSlct.SelectedPath != string.Empty)
                {
                    AppSt.Default.save_base_folder = WorkFolderSlct.SelectedPath;
                    AppSt.Default.Save();
                }
            }

            BasePathBox.Text = AppSt.Default.save_base_folder;
        }

        private void WorkFolderSelect_Click(object sender, RoutedEventArgs e)
        {
            DateTime date = DateTime.Now;

            System.Windows.Forms.FolderBrowserDialog WorkFolderSlct = new System.Windows.Forms.FolderBrowserDialog();

            if (WorkFolderSlct.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (WorkFolderSlct.SelectedPath != string.Empty)
                {
                    AppSt.Default.save_work_folder = WorkFolderSlct.SelectedPath;
                    AppSt.Default.lastDate = date.Month;
                    AppSt.Default.Save();
                }
            }

            AlreadyPathBox.Text = AppSt.Default.save_work_folder;
        }

        private void MainViewBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
            OpenBtn.Background = Brushes.Yellow;
            string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
            if (filePaths != null)
            foreach (string fileLoc in filePaths) //переберает всю инфу пока не найдет строку адреса
                if (File.Exists(fileLoc))
                    OpenBtn.Content = "Открыть " + fileLoc.Split('\\').Last();
            CanvasBorder.Background = Brushes.LightYellow;
        }

        private void MainViewBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])(e.Data.GetData(DataFormats.FileDrop));
                foreach (string fileLoc in filePaths)
                    if (File.Exists(fileLoc))
                        OpenFile(fileLoc);
            }
            OpenBtn.Background = Brushes.Gainsboro;
            CanvasBorder.Background = Brushes.LightGray;
        }

        private void ClearCalcMeshBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceCombo.SelectedItem is MonchaDevice device)
            {
                device.CalculateMesh.Points = null;
            }
        }


        private void DistanceUpDn_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
           /* if (e.OldValue.Value > 0)
                double prop = e.NewValue.Value / e.OldValue.Value;*/
        }
    }
    
}
