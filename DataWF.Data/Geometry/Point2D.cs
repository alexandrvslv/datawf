using DataWF.Common;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data.Geometry
{
    public class Point2D : IByteSerializable, IComparable<Point2D>, IEquatable<Point2D>
    {
        public Point2D()
        { }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }

        public double Y { get; set; }

        public int CompareTo(Point2D other)
        {
            if (other == null)
                return 1;
            var result = Y.CompareTo(other.Y);
            return result == 0 ? X.CompareTo(other.X) : result;
        }

        public bool Equals(Point2D other)
        {
            if (other == null)
                return false;
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public void Deserialize(BinaryReader reader)
        {
            X = reader.ReadDouble();
            Y = reader.ReadDouble();
        }

        public void Deserialize(byte[] data)
        {
            X = BitConverter.ToDouble(data, 0);
            Y = BitConverter.ToDouble(data, 8);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(X);
            writer.Write(Y);
        }

        public byte[] Serialize()
        {
            var buffer = new byte[16];
            Array.Copy(BitConverter.GetBytes(X), 0, buffer, 0, 8);
            Array.Copy(BitConverter.GetBytes(Y), 0, buffer, 8, 8);
            return buffer;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Point2D);
        }

        public override int GetHashCode()
        {
            int hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
