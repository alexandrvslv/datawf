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
    public class Polygon2D : IByteSerializable, IComparable<Polygon2D>, IEquatable<Polygon2D>
    {
        public static bool operator ==(Polygon2D a, Polygon2D b)
        {
            return a?.Equals(b) ?? b?.Equals(a) ?? true;
        }

        public static bool operator !=(Polygon2D a, Polygon2D b)
        {
            return !(a?.Equals(b) ?? b?.Equals(a) ?? true);
        }

        private Rectangle2D bounds = new Rectangle2D();

        public Polygon2D(byte[] data)
        {
            Deserialize(data);
        }

        public Polygon2D()
        { }

        public Polygon2D(IEnumerable<Point2D> points)
        {
            Points.AddRange(points);
        }

        public List<Point2D> Points { get; set; } = new List<Point2D>();

        [XmlIgnore, JsonIgnore]
        public Rectangle2D Bounds => RefreshBounds();

        private Rectangle2D RefreshBounds()
        {
            bounds.Left =
                bounds.Top =
                bounds.Right =
                bounds.Bottom = 0;
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

        public int CompareTo(Polygon2D other)
        {
            if (other == null)
                return 1;
            return Bounds.CompareTo(other.Bounds);
        }

        public bool Equals(Polygon2D other)
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
                Deserialize(reader);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            Points.Clear();
            Points.Capacity = reader.ReadInt32();
            while (Points.Count < Points.Capacity)
            {
                var point = new Point2D();
                point.Deserialize(reader);
                Points.Add(point);
            }

        }
        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Serialize(writer);
                return stream.ToArray();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Points.Count);
            foreach (var point in Points)
            {
                point.Serialize(writer);
            }
            writer.Flush();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Polygon2D);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return $"({string.Join(", ", Points.Select(p => p.ToString()))})";
        }
    }
}
