using DataWF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data.Geometry
{
    public class Polygon2DArray : IByteSerializable, IEquatable<Polygon2DArray>
    {
        private Rectangle2D bounds = new Rectangle2D();

        public Polygon2DArray()
        { }

        public Polygon2DArray(IEnumerable<Polygon2D> polygons)
        {
            Polygons.AddRange(polygons);
        }

        public List<Polygon2D> Polygons { get; set; } = new List<Polygon2D>();

        [XmlIgnore, JsonIgnore]
        public Rectangle2D Bounds => RefreshBounds();

        private Rectangle2D RefreshBounds()
        {
            bounds.Left =
                bounds.Top =
                bounds.Right =
                bounds.Bottom = 0;
            if (Polygons.Count > 0)
            {
                var firstpoint = Polygons.First().Points.First();
                bounds.Reset(firstpoint);
                foreach (var polygon in Polygons)
                {
                    foreach (var point in polygon.Points)
                    {
                        bounds.Append(point);
                    }
                }
            }
            return bounds;
        }

        public int CompareTo(Polygon2DArray other)
        {
            if (other == null)
                return 1;
            return Bounds.CompareTo(other.Bounds);
        }

        public bool Equals(Polygon2DArray other)
        {
            if (other == null)
                return false;
            return Polygons.SequenceEqual(other.Polygons);
        }

        public void Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                Polygons.Clear();
                Polygons.Capacity = reader.ReadInt32();
                while (Polygons.Count < Polygons.Capacity)
                {
                    var polygon = new Polygon2D();
                    polygon.Deserialize(reader);
                    Polygons.Add(polygon);
                }
            }
        }

        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Polygons.Count);
                foreach (var polygon in Polygons)
                {
                    polygon.Serialize(writer);
                }
                writer.Flush();
                return stream.ToArray();
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Polygon2DArray);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Join(", ", Polygons.Select(p => p.ToString()));
        }
    }
}
