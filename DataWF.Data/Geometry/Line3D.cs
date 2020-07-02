using DataWF.Common;
using System;

namespace DataWF.Data.Geometry
{
    public class Line3D : IByteSerializable, IComparable<Line3D>, IEquatable<Line3D>
    {
        public Line3D()
        {
            Point1 = new Point3D();
            Point2 = new Point3D();
        }

        public Line3D(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            Point1 = new Point3D(x1, y1, z1);
            Point2 = new Point3D(x2, y2, z2);
        }

        public Point3D Point1 { get; set; }

        public Point3D Point2 { get; set; }

        public int CompareTo(Line3D other)
        {
            if (other == null)
                return 1;
            var result = Point1.CompareTo(Point2);
            return result == 0 ? Point2.CompareTo(Point2) : result;
        }

        public bool Equals(Line3D other)
        {
            if (other == null)
                return false;
            return Point1.Equals(other.Point1) && Point2.Equals(other.Point2);
        }

        public void Deserialize(byte[] data)
        {
            Point1.X = BitConverter.ToDouble(data, 0);
            Point1.Y = BitConverter.ToDouble(data, 8);
            Point1.Z = BitConverter.ToDouble(data, 16);
            Point2.X = BitConverter.ToDouble(data, 24);
            Point2.Y = BitConverter.ToDouble(data, 32);
            Point2.Z = BitConverter.ToDouble(data, 40);
        }

        public byte[] Serialize()
        {
            var buffer = new byte[32];
            Array.Copy(BitConverter.GetBytes(Point1.X), 0, buffer, 0, 8);
            Array.Copy(BitConverter.GetBytes(Point1.Y), 0, buffer, 8, 8);
            Array.Copy(BitConverter.GetBytes(Point1.Z), 0, buffer, 16, 8);
            Array.Copy(BitConverter.GetBytes(Point2.X), 0, buffer, 24, 8);
            Array.Copy(BitConverter.GetBytes(Point2.Y), 0, buffer, 32, 8);
            Array.Copy(BitConverter.GetBytes(Point2.Z), 0, buffer, 40, 8);
            return buffer;
        }

        public override string ToString()
        {
            return $"{Point1} - {Point2}";
        }
    }
}
