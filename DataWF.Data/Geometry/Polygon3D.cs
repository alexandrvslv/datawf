using DataWF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data.Geometry
{
    public class Polygon3D : IByteSerializable, IComparable<Polygon3D>, IEquatable<Polygon3D>
    {
        private Rectangle3D bounds = new Rectangle3D();

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
