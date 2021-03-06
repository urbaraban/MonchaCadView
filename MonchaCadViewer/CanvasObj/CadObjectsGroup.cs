﻿using MonchaSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ToGeometryConverter.Object;
using AppSt = MonchaCadViewer.Properties.Settings;

namespace MonchaCadViewer.CanvasObj
{
    public class CadObjectsGroup : CadObject, IList<CadGeometry>
    {
        private List<CadGeometry> cadObjects = new List<CadGeometry>();

        private bool Opened = false;

        public GCCollection gCElements 
        {
            get => elements;
            set
            {
                this.cadObjects.Clear();
                elements = value;

                foreach (IGCObject gC in elements)
                {
                    this.cadObjects.Add(new CadGeometry(gC, true)
                    {
                        TransformGroup = this.TransformGroup,
                        Name = this.Name,
                    });
                }

                geometryGroup.Children.Clear();
                foreach (CadObject cadObject in cadObjects)
                {
                    geometryGroup.Children.Add(cadObject.GetGeometry);
                }

                if (AppSt.Default.stg_show_name == true)
                {
                    this.cadObjects.Add(new CadGeometry(
                        new ToGeometryConverter.Object.Elements.TextElement(Name, MonchaHub.GetThinkess,
                        new Point3D(0, 0, 0)),
                        true)
                    {
                        TransformGroup = this.TransformGroup,
                        Name = this.Name,
                    });
                }
            }
        }
        private GCCollection elements;

        public override Geometry GetGeometry => geometryGroup;
        private GeometryGroup geometryGroup = new GeometryGroup();

        public override Rect Bounds => gCElements.Bounds; 

        public CadObjectsGroup(GCCollection gcCollection, string Name, Transform3DGroup transform3DGroup)
        {
            this.Name = Name;
            this.UpdateTransform(transform3DGroup, transform3DGroup == null, gcCollection.Bounds);
            this.gCElements = gcCollection;
        }

        #region IList<CadGeometry>
        public CadGeometry this[int index] { get => ((IList<CadGeometry>)cadObjects)[index]; set => ((IList<CadGeometry>)cadObjects)[index] = value; }

        public int Count => ((ICollection<CadGeometry>)cadObjects).Count;

        public bool IsReadOnly => ((ICollection<CadGeometry>)cadObjects).IsReadOnly;

        public void Add(CadGeometry item)
        {
            ((ICollection<CadGeometry>)cadObjects).Add(item);
        }

        public void Clear()
        {
            ((ICollection<CadGeometry>)cadObjects).Clear();
        }

        public bool Contains(CadGeometry item)
        {
            return ((ICollection<CadGeometry>)cadObjects).Contains(item);
        }

        public void CopyTo(CadGeometry[] array, int arrayIndex)
        {
            ((ICollection<CadGeometry>)cadObjects).CopyTo(array, arrayIndex);
        }

        public IEnumerator<CadGeometry> GetEnumerator()
        {
            return ((IEnumerable<CadGeometry>)cadObjects).GetEnumerator();
        }

        public int IndexOf(CadGeometry item)
        {
            return ((IList<CadGeometry>)cadObjects).IndexOf(item);
        }

        public void Insert(int index, CadGeometry item)
        {
            ((IList<CadGeometry>)cadObjects).Insert(index, item);
        }

        public bool Remove(CadGeometry item)
        {
            return ((ICollection<CadGeometry>)cadObjects).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<CadGeometry>)cadObjects).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)cadObjects).GetEnumerator();
        }
        #endregion
    }
}
