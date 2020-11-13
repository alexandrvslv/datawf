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
using System.Globalization;
using System.IO;
using System.Text.Json.Serialization;

namespace DataWF.Geometry
{
    [JsonConverter(typeof(SystemJsonPoint3DConverter)), Newtonsoft.Json.JsonConverter(typeof(NewtonJsonPoint3DConverter))]
    public struct Point3D : IBinarySerializable, IComparable<Point3D>, IEquatable<Point3D>
    {
        public static readonly Point3D Empty = new Point3D();
        public static bool TryParse(string text, out Point3D point)
        {
            point = new Point3D();
            text = text.Trim(Point2D.TrimArray);
            var split = text.Split(',');

            if (split.Length > 2
                && double.TryParse(split[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var x)
                && double.TryParse(split[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var y)
                && double.TryParse(split[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var z))
            {
                point.X = x;
                point.Y = y;
                point.Z = z;
                return true;
            }
            return false;
        }

        public static bool operator ==(Point3D a, Point3D b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Point3D a, Point3D b)
        {
            return !a.Equals(b);
        }

        public Point3D(byte[] data)
        {
            X = BitConverter.ToDouble(data, 0);
            Y = BitConverter.ToDouble(data, 8);
            Z = BitConverter.ToDouble(data, 16);
        }

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
            if (obj == null)
                return false;
            return Equals((Point3D)obj);
        }

        public override string ToString()
        {
            return FormattableString.Invariant($"({X}, {Y}, {Z})");
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
