//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
using DataWF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Geometry
{
    public class Polygon3D : IByteSerializable, IComparable<Polygon3D>, IEquatable<Polygon3D>
    {
        public static bool operator ==(Polygon3D a, Polygon3D b)
        {
            return a?.Equals(b) ?? b?.Equals(a) ?? true;
        }

        public static bool operator !=(Polygon3D a, Polygon3D b)
        {
            return !(a?.Equals(b) ?? b?.Equals(a) ?? true);
        }

        private Rectangle3D bounds = new Rectangle3D();

        public Polygon3D(byte[] data)
        {
            Deserialize(data);
        }

        public Polygon3D()
        { }

        public Polygon3D(IEnumerable<Point3D> points)
        {
            Points.AddRange(points);
        }

        public List<Point3D> Points { get; set; } = new List<Point3D>();

        [XmlIgnore, JsonIgnore]
        public Rectangle3D Bounds => RefreshBounds();

        private Rectangle3D RefreshBounds()
        {
            bounds.Left =
                bounds.Bottom =
                bounds.Near =
                bounds.Right =
                bounds.Top =
                bounds.Far = 0;
            if (Points.Count > 0)
            {
                var firstpoint = Points.First();
                bounds.Reset(firstpoint);
                foreach (var point in Points)
                {
                    bounds.Append(point);
                }
            }
            return bounds;
        }

        public int CompareTo(Polygon3D other)
        {
            if (other == null)
                return 1;
            return Bounds.CompareTo(other.Bounds);
        }

        public bool Equals(Polygon3D other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Points.SequenceEqual(other.Points);
        }

        public void Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                Points.Clear();
                Points.Capacity = reader.ReadInt32();
                while (Points.Count < Points.Capacity)
                {
                    var point = new Point3D();
                    point.Deserialize(reader);
                    Points.Add(point);
                }
            }
        }

        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Points.Count);
                foreach (var point in Points)
                {
                    point.Serialize(writer);
                }
                writer.Flush();
                return stream.ToArray();
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Polygon3D);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Join(", ", Points.Select(p => p.ToString()));
        }
    }
}
