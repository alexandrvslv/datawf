using DataWF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data.Geometry
{
    public class Polygon2D : IByteSerializable, IComparable<Polygon2D>, IEquatable<Polygon2D>
    {
        private Rectangle2D bounds = new Rectangle2D();

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
                    var point = new Point2D();
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
            return Equals(obj as Polygon2D);
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
