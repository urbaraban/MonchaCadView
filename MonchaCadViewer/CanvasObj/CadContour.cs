﻿using MonchaCadViewer.CanvasObj.DimObj;
using MonchaSDK.Object;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MonchaCadViewer.CanvasObj
{
    class CadContour : CadObject
    {
        private StreamGeometry _path;
        private LObjectList _points;
        private bool wasmove = false;

        protected override Geometry DefiningGeometry => this._path;

        AdornerContourFrame myAdorner;

        public bool Mirror { get; set; } = false;
        public double Angle { get; set; } = 0;

        public Size Size => _path.Bounds.Size;

        public CadContour(LObjectList point3Ds, MonchaPoint3D Center, bool maincanvas, bool Capturemouse) : base (Capturemouse, Center, false)
        {
            this._points = point3Ds;

            this.ClipToBounds = false;
            this._path = new StreamGeometry();
            this.UpdateGeometry();

            if (maincanvas)
            {
                ContextMenuLib.ViewContourMenu(this.ContextMenu);
                this.Cursor = Cursors.Hand;
                this.MouseLeave += Contour_MouseLeave;
                this.MouseLeftButtonUp += Contour_MouseLeftUp;
                this.Loaded += ViewContour_Loaded;
                this.ContextMenuClosing += ViewContour_ContextMenuClosing;
                this.MouseWheel += CadContour_MouseWheel;

                this.BaseContextPoint.ChangePoint += ViewContour_MoveBasePoint;
            }

           
            this.StrokeThickness = _path.Bounds.Width * 0.002;
            this.Stroke = Brushes.Red;


        }

        private void CadContour_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Angle += Math.Abs(e.Delta)/e.Delta * (Keyboard.Modifiers == ModifierKeys.Shift ? 5 : 1);
            UpdateGeometry();
            myAdorner.Rotate(this.Angle);
        }

        private void CadContour_MouseDown(object sender, MouseButtonEventArgs e)
        {
           
        }

        private void ViewContour_MoveBasePoint(object sender, MonchaPoint3D e)
        {
            this.UpdateGeometry();
        }

        private void ViewContour_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (this.ContextMenu.DataContext is MenuItem menuItem)
            {
                switch (menuItem.Header)
                {
                    case "Mirror":
                        this.Mirror = !this.Mirror;
                        break;

                    case "Fix":
                        this.IsFix = !this.IsFix;
                        break;

                    case "Remove":
                        if (this.Parent is CadCanvas canvas)
                        {
                            canvas.Children.Remove(this);
                        }
                        break;

                    case "Render":
                        this.Render = !this.Render;
                        break;
                }

                
            }
            this.UpdateGeometry();

        }

        private void ViewContour_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent is CadCanvas canvas)
            {
                canvas.SubsObj(this);
                this.adornerLayer = AdornerLayer.GetAdornerLayer(canvas);
                this.adornerLayer.ClipToBounds = true;

                this.Width = canvas.Width;
                this.Height = canvas.Height;
                this.myAdorner = new AdornerContourFrame(this, canvas);
                myAdorner.DataContext = this;
                adornerLayer.Add(myAdorner);
                myAdorner.AngleChange += MyAdorner_AngleChange;
            }
        }

        private void MyAdorner_AngleChange(object sender, double e)
        {
            this.Angle = e;
            this.UpdateGeometry(true);
        }

        private void Contour_MouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            if (wasmove)
            {
                this.ReleaseMouseCapture();
                this.wasmove = false;
            }
            else
            {

            }
            
        }

        private void Contour_MouseLeave(object sender, MouseEventArgs e)
        {
            this.ReleaseMouseCapture();
        }

        public List<List<MonchaPoint3D>> GiveModPoint()
        {
            List<List<MonchaPoint3D>> ListPoints = new List<List<MonchaPoint3D>>();

            for (int i = 0; i < _points.Count; i++)
            {
                List<MonchaPoint3D> Points = new List<MonchaPoint3D>();
                for (int j = 0; j < _points[i].Count; j++)
                {
                    MonchaPoint3D modPoint = _points[i][j];
                    if (this.Mirror)
                        modPoint = new MonchaPoint3D(- modPoint.X, modPoint.Y, modPoint.Z, modPoint.M);

                    if (this.Angle != 0)
                        modPoint = RotatePoint(modPoint, new MonchaPoint3D(0, 0, 0));

                    Points.Add(modPoint);
                }
                ListPoints.Add(Points);
            }

            return ListPoints;

            MonchaPoint3D RotatePoint(MonchaPoint3D pointToRotate, MonchaPoint3D centerPoint)
            {
                this.Angle = (360 + this.Angle) % 360;
                double angleInRadians = this.Angle * (Math.PI / 180);
                double cosTheta = (float)Math.Cos(angleInRadians);
                double sinTheta = (float)Math.Sin(angleInRadians);
                return new MonchaPoint3D
                (
                        //X
                        (cosTheta * (pointToRotate.X - centerPoint.X) -
                        sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                        //Y
                        (sinTheta * (pointToRotate.X - centerPoint.X) +
                        cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y),
                     //Z
                     pointToRotate.Z,
                    //M
                    pointToRotate.M
                );
            }
        }

        public void UpdateGeometry(bool FromAdorner = false)
        {
            PathFigureCollection _pathFigures = new PathFigureCollection();

            List<List<MonchaPoint3D>> workPoint = GiveModPoint();

            this._path.Clear();

            using (StreamGeometryContext ctx = this._path.Open())
            {
                for (int i = 0; i < workPoint.Count; i++)
                {
                    ctx.BeginFigure(
                    workPoint[i][0].GetPoint,
                    true,    // is NOT filled
                    true);   // is NOT closed

                    for (int j = 0; j < _points[i].Count; j++)
                    {
                        ctx.LineTo(
                        workPoint[i][j].GetPoint,
                        true,     // is stroked (line visible)
                        false);   // is not smoothly joined w/other segments
                    }
                }
            }


            if (adornerLayer != null && !FromAdorner)
                adornerLayer.Update();
            this.Fill = Brushes.Transparent;

            Canvas.SetLeft(this, this.BaseContextPoint.GetMPoint.X);
            Canvas.SetTop(this, this.BaseContextPoint.GetMPoint.Y);

            this.intEvent();
        }
    }
}

