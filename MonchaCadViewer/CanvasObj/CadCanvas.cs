﻿using MonchaSDK;
using MonchaSDK.Device;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MonchaSDK.Object;
using System.ComponentModel;
using MonchaCadViewer.Interface;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using MonchaCadViewer.Panels;
using System.Windows.Data;


namespace MonchaCadViewer.CanvasObj
{
    public class CadCanvas : Canvas, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        #endregion

        public event EventHandler<CadObject> SelectedObject;
        public event EventHandler UpdateProjection;
        //public event EventHandler<string> ErrorMessageEvent;

        private object MouseOnObject = null;
        private bool _wasmove = false;
        private int _status = 0;
        
        private bool _nofreecursor = true;
        private Point StartMovePoint;
        private Point StartMousePoint;

        public CadAnchor UnderAnchor;

        public List<LSize3D> Masks = new List<LSize3D>();

        public bool MainCanvas { get; }

        public bool HorizontalMesh { get; set; } = false;

        public Point LastMouseDownPosition = new Point();

        public MouseAction MouseAction
        {
            get => this.mouseAction;
            set
            {
                mouseAction = value;
                switch (value)
                {
                    case MouseAction.NoAction:
                        this.Cursor = Cursors.Arrow;
                        this.ReleaseMouseCapture();
                        break;
                    case MouseAction.MoveCanvas:
                        this.Cursor = Cursors.SizeAll;
                        break;
                    case MouseAction.Line:
                    case MouseAction.Rectangle:
                    case MouseAction.Mask:
                        this.Cursor = Cursors.Cross;
                        break;
                };
                OnPropertyChanged("MouseAction");
            }
        }
        private MouseAction mouseAction = MouseAction.NoAction;

        public CadCanvas()
        {
            this.MainCanvas = true;
            LoadSetting();
        }

        public CadCanvas(LSize3D Size, bool MainCanvas)
        {
            this.MainCanvas = MainCanvas;
            LoadSetting();
            this.Loaded += CadCanvas_Loaded;
        }

        private void CadCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= CadCanvas_Loaded;
        }

        private void LoadSetting()
        {
            this.Name = "CCanvas";
            this.Background = Brushes.Transparent; //backBrush;

            this.DataContext = MonchaHub.Size;

            this.SetBinding(Canvas.WidthProperty, "X");
            this.SetBinding(Canvas.HeightProperty, "Y");
     
            if (this.MainCanvas == true)
            {
                this.ContextMenuClosing += CadCanvas_ContextMenuClosing;
                this.ContextMenu = new ContextMenu();
                ContextMenuLib.CanvasMenu(this.ContextMenu);

                this.MouseLeftButtonDown += Canvas_MouseLeftDown;
                //this.MouseMove += CadCanvas_MouseMove;
                this.MouseUp += CadCanvas_MouseUp;
                this.MouseLeftButtonDown += CadCanvas_MouseLeftButtonDown;
                this.MouseMove += CadCanvas_MouseMove1;
                this.MouseWheel += CadCanvas_MouseWheel;
                this.KeyDown += CadCanvas_KeyDown;
                this.KeyUp += CadCanvas_KeyUp;
                this.MouseLeave += CadCanvas_MouseLeave;
            }
            ResetTransform();
        }

        private void CadCanvas_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.MouseAction == MouseAction.MoveCanvas) this.MouseAction = MouseAction.NoAction;
        }

        private void CadCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftCtrl: 
                case Key.RightCtrl:
                    this.MouseAction = MouseAction.MoveCanvas;
                    break;
            }
        }

        #region MouseAction
        private void CadCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                Point centre = e.GetPosition(this);
                this.Scale.CenterX = centre.X;
                this.Scale.CenterY = centre.Y;
                if (this.Scale.ScaleY + (double)e.Delta / 1000 > 1)
                {
                    this.Scale.ScaleX += (double)e.Delta / 1000;
                    this.Scale.ScaleY += (double)e.Delta / 1000;
                }
                else
                {
                    this.Scale.ScaleX = 1;
                    this.Scale.ScaleY = 1;
                    this.X = 0;
                    this.Y = 0;
                }
            }
        }

        private void CadCanvas_MouseMove1(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.MouseAction = MouseAction.MoveCanvas;

                if (this.IsFix == false)
                {
                    this.WasMove = true;

                    Point tPoint = e.GetPosition(this.Parent as System.Windows.IInputElement);

                    this.X = (this.StartMovePoint.X + (tPoint.X - this.StartMousePoint.X)) / this.Scale.ScaleX;
                    this.Y = (this.StartMovePoint.Y + (tPoint.Y - this.StartMousePoint.Y)) / this.Scale.ScaleY;

                    this.CaptureMouse();
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                this.MouseAction = MouseAction.MoveCanvas;
            }
            else if (this.MouseAction == MouseAction.MoveCanvas)
            {
                this.MouseAction = MouseAction.NoAction;
            }
        }

        private void CadCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.MouseAction == MouseAction.MoveCanvas)
            {
                this.StartMousePoint = e.GetPosition(this.Parent as System.Windows.IInputElement);
                this.StartMovePoint = new Point(this.Translate.X, this.Translate.Y);
            }
            else if (this.MouseAction == MouseAction.Rectangle)
            {
                Point point = e.GetPosition(this);
                CadRectangle cadRectangle = new CadRectangle(new LPoint3D(point, MonchaHub.Size), new LPoint3D(point, MonchaHub.Size), string.Empty,true);
                this.Add(cadRectangle);

            }
            else if (this.mouseAction == MouseAction.Mask)
            {
                LSize3D lRect = new LSize3D(new LPoint3D(e.GetPosition(this), MonchaHub.Size, true), new LPoint3D(e.GetPosition(this), MonchaHub.Size, true));
                CadRectangle Maskrectangle = new CadRectangle(lRect, $"Mask_{this.Masks.Count}", true);
                this.Add(Maskrectangle);
                this.Masks.Add(lRect);
            }
            else if (this.mouseAction == MouseAction.Line)
            {
                CadLine line = new CadLine(new LPoint3D(e.GetPosition(this)), new LPoint3D(e.GetPosition(this)), true);
                this.Add(line);
            }
        }

        private void CadCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) this.ReleaseMouseCapture();
        }
        #endregion


        /// <summary>
        /// Draw object on canvas.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <param name="maincanvas">property for main canvas attributes</param>
        /// <param name="add">Add contour for already view</param>
        /// <param name="mousemove">add mouse event</param>
        public void DrawContour(CadObject obj, bool Clear)
        {
            this.Dispatcher.BeginInvoke(new Action(() => { 
                if (Clear == true) this.Clear();

                if (obj is CadObject cadObject)
                {
                    cadObject.MouseMove += CadObject_MouseMove;
                    cadObject.MouseLeave += CadObject_MouseLeave;
                }

                this.Add(obj);
            }));
        }

        private void CadObject_MouseLeave(object sender, MouseEventArgs e)
        {
            if (this.MouseOnObject == sender)
            {
                this.MouseOnObject = null;
            }
        }

        private void CadObject_MouseMove(object sender, MouseEventArgs e)
        {
            this.MouseOnObject = sender;
        }


        private void Canvas_MouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            if (this._nofreecursor == false)
            {
                switch (_status)
                {
                    case 1:

                        break;
                    default:
                        if (this.MouseOnObject == null)
                        {
                            this.ClearSelectedObject(null);
                            LastMouseDownPosition = e.GetPosition(this);
                        }
                        break;
                }
            }
        }

        private void CadCanvas_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (sender is CadCanvas canvas)
            {
                if (canvas.ContextMenu.DataContext is MenuItem cmindex)
                {
                    switch (cmindex.Tag)
                    {
                        case "canvas_freezall":
                            foreach (object obj in canvas.Children)
                            {
                                if (obj is CadObject cadObject)
                                {
                                    cadObject.IsFix = true;
                                }
                            }
                            break;
                        case "canvas_unselectall":
                            ClearSelectedObject(null);
                            break;
                    }
                }
            }
        }

        public void ClearSelectedObject(object noclearobj)
        {
            foreach (object obj in this.Children)
            {
                if (obj != noclearobj && obj is CadObject cadObject)
                {
                    cadObject.IsSelected = false;
                }
                else
                {

                }

            }
            SelectedObject?.Invoke(this, null);
           
        }

        private void CadCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this._wasmove = false;
        }


        public void RemoveSelectObject()
        {
            for (int i = 0; i < this.Children.Count; i++)
            {
                if (this.Children[i] is CadObject cadObject && cadObject.IsSelected == true)
                {
                    this.Children.Remove(cadObject);
                    i--;
                }
            }
            SelectedObject?.Invoke(this, null);
            UpdateProjection?.Invoke(this, null);
        }

        public CadAnchor[,] GetMeshDot(int Height, int Width)
        {
            CadAnchor[,] mesh = new CadAnchor[Height, Width];
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    mesh[i, j] = this.Children[i * Width + j] as CadAnchor;
                }
            return mesh;
        }

        //Рисуем квадраты в поле согласно схеме
       

        public void Clear()
        {
            for (int i = 0; i < this.Children.Count; i ++)
            {
                if (this.Children[i] is CadObject cadObject)
                {
                    cadObject.Remove();
                    i--;
                }
            }
            this.Masks.Clear();
        }

        public void RemoveChildren(FrameworkElement Object)
        {
            if (Object is CadObject cadObject)
            {
                cadObject.Selected -= CadObject_Selected;
                cadObject.OnObject -= CadObject_OnObject1;
                cadObject.Updated -= CadObject_Updated;
            }
            this.Children.Remove(Object);
            
            if (Object is CadRectangle cadRectangle) this.Masks.Remove(cadRectangle.LRect);
            UpdateProjection?.Invoke(this, null);
        }

        /// <summary>
        /// Add object in Children Canvas
        /// </summary>
        /// <param name="obj"></param>
        public void Add(FrameworkElement obj)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (this.Children.Contains(obj) == false)
                {
                    if (obj is CadObject cadObject)
                    {
                        cadObject.Selected += CadObject_Selected;
                        cadObject.OnObject += CadObject_OnObject1;
                        cadObject.Updated += CadObject_Updated;
                        cadObject.Removed += CadObject_Removed;
                    }

                    this.Children.Add(obj);
                }
                UpdateProjection?.Invoke(this, null);
            }));
        }

        public async void AddRange(List<FrameworkElement> frameworkElements, bool Clear)
        {
            this.Dispatcher.BeginInvoke(new Action(()=> { 
            if (Clear == true) this.Clear();

            ProgressPanel.SetProgressBar(0, frameworkElements.Count, "Добавляем");
            for (int i = 0; i < frameworkElements.Count; i += 1)
            {
                this.Add(frameworkElements[i]);
                ProgressPanel.SetProgressBar(i, frameworkElements.Count, $"{i}/{frameworkElements.Count}");
            }
            ProgressPanel.End();
            }));
        }

        private void CadObject_Removed(object sender, CadObject e) => RemoveChildren((FrameworkElement)sender);

        private void CadObject_Updated(object sender, string e) => UpdateProjection?.Invoke(this, null);

        private void CadObject_OnObject1(object sender, bool e)
        {
            if (sender is CadAnchor cadAnchor)
            {
                this.UnderAnchor = e == true ? cadAnchor : (this.UnderAnchor == cadAnchor ? null : this.UnderAnchor);
            }
            this._nofreecursor = e;
        }


        private void CadObject_Selected(object sender, bool e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Shift)
            {
                this.ClearSelectedObject((CadObject)sender);
            }
            if (e == true)
            SelectedObject?.Invoke(this, (CadObject)sender);
        }

        public static List<FrameworkElement> GetMesh(LDeviceMesh mesh, double AnchorSize, bool Render, MeshType meshType)
        {
            List<FrameworkElement> objects = new List<FrameworkElement>();

            for (int i = 0; i < mesh.GetLength(0); i++)
            {
                for (int j = 0; j < mesh.GetLength(1); j++)
                {
                    mesh[i, j].M = MonchaHub.Size;

                    objects.Add(new CadAnchor(mesh[i, j], true)
                    {
                        IsFix = false,// !mesh.OnlyEdge;
                        Uid = i.ToString() + ":" + j.ToString(),
                        ToolTip = "Позиция: " + i + ":" + j + "\nX: " + mesh[i, j].X + "\n" + "Y: " + mesh[i, j].Y,
                        DataContext = mesh,
                        Render = Render,
                        MeshType = meshType
                    });
                }
            }
            return objects;

            return null;
        }

        #region TransfromObject
        public TransformGroup TransformGroup { get; set; }
        public ScaleTransform Scale { get; set; }
        public RotateTransform Rotate { get; set; }
        public TranslateTransform Translate { get; set; }

        public double X 
        { 
            get => this.Translate.X; 
            set => this.Translate.X = value;
        }
        public double Y 
        { 
            get => this.Translate.Y;
            set => this.Translate.Y = value;
        }
        public bool IsFix { get; set; }
        public bool Mirror { get; set; } = false;

        public bool WasMove { get; set; } = false;

        public void UpdateTransform(TransformGroup transformGroup, bool ResetPosition)
        {
            if (transformGroup != null)
            {
                this.RenderTransform = TransformGroup;
                this.Scale = this.TransformGroup.Children[0] != null ? (ScaleTransform)this.TransformGroup.Children[0] : new ScaleTransform();
                this.Rotate = this.TransformGroup.Children[1] != null ? (RotateTransform)this.TransformGroup.Children[1] : new RotateTransform();
                this.Translate = this.TransformGroup.Children[2] != null ? (TranslateTransform)this.TransformGroup.Children[2] : new TranslateTransform();
            }
            else ResetTransform();


            if (this.Scale.ScaleX < 0) this.Mirror = true;
        }

        public void ResetTransform()
        {
            this.TransformGroup = new TransformGroup()
            {
                Children = new TransformCollection()
                    {
                        new ScaleTransform(),
                        new RotateTransform(),
                        new TranslateTransform()
                    }
            };
            this.Scale = (ScaleTransform)this.TransformGroup.Children[0];
            this.Rotate = (RotateTransform)this.TransformGroup.Children[1];
            this.Translate = (TranslateTransform)this.TransformGroup.Children[2];
            this.RenderTransform = this.TransformGroup;
        }
        #endregion
    }

    public enum MouseAction
    {
        NoAction,
        Rectangle,
        Line,
        Circle,
        MoveCanvas,
        Mask
    }
}
