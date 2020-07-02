using DataWF.Common;
using System;
using System.IO;

namespace DataWF.Data.Geometry
{
    public class Point3D : IByteSerializable, IComparable<Point3D>, IEquatable<Point3D>
    {
        public Point3D()
        { }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }

        public int CompareTo(Point3D other)
        {
            if (other == null)
                return 1;
            var result = Y.CompareTo(other.Y);
            if (result == 0)
                result = X.CompareTo(other.X);
            if (result == 0)
                result = Z.CompareTo(other.Z);
            return result;
        }

        public bool Equals(Point3D other)
        {
            if (other == null)
                return false;
            return X.Equals(other.X)
                && Y.Equals(other.Y)
                && Z.Equals(other.Z);
        }

        public void Deserialize(byte[] data)
        {
            X = BitConverter.ToDouble(data, 0);
            Y = BitConverter.ToDouble(data, 8);
            Z = BitConverter.ToDouble(data, 16);
        }

        public void Deserialize(BinaryReader reader)
        {
            X = reader.ReadDouble();
            Y = reader.ReadDouble();
            Z = reader.ReadDouble();
        }

        public byte[] Serialize()
        {
            var buffer = new byte[24];
            Array.Copy(BitConverter.GetBytes(X), 0, buffer, 0, 8);
            Array.Copy(BitConverter.GetBytes(Y), 0, buffer, 8, 8);
            Array.Copy(BitConverter.GetBytes(Z), 0, buffer, 16, 8);
            return buffer;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Z);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Point3D);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public override int GetHashCode()
        {
            int hashCode = -307843816;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Z.GetHashCode();
            return hashCode;
        }
    }
}
