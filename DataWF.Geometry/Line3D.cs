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

namespace DataWF.Geometry
{
    public class Line3D : IBinarySerializable, IComparable<Line3D>, IEquatable<Line3D>
    {
        private Point3D point2;
        private Point3D point1;

        public static bool operator ==(Line3D a, Line3D b)
        {
            return a?.Equals(b) ?? b == null;
        }

        public static bool operator !=(Line3D a, Line3D b)
        {
            return !(a?.Equals(b) ?? b == null);
        }

        public Line3D(byte[] data)
        {
            Deserialize(data);
        }

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

        public Point3D Point1 { get => point1; set => point1 = value; }

        public Point3D Point2 { get => point2; set => point2 = value; }

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

        public void Deserialize(BinaryReader reader)
        {
            point1.X = reader.ReadDouble();
            point1.Y = reader.ReadDouble();
            point1.Z = reader.ReadDouble();
            point2.X = reader.ReadDouble();
            point2.Y = reader.ReadDouble();
            point2.Z = reader.ReadDouble();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(point1.X);
            writer.Write(point1.Y);
            writer.Write(point1.Z);
            writer.Write(point2.X);
            writer.Write(point2.Y);
            writer.Write(point2.Z);
        }

        public void Deserialize(byte[] data)
        {
            point1.X = BitConverter.ToDouble(data, 0);
            point1.Y = BitConverter.ToDouble(data, 8);
            point1.Z = BitConverter.ToDouble(data, 16);
            point2.X = BitConverter.ToDouble(data, 24);
            point2.Y = BitConverter.ToDouble(data, 32);
            point2.Z = BitConverter.ToDouble(data, 40);
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

        public override bool Equals(object obj)
        {
            return Equals(obj as Line3D);
        }

        public override int GetHashCode()
        {
            int hashCode = 363529913;
            hashCode = hashCode * -1521134295 + EqualityComparer<Point3D>.Default.GetHashCode(Point1);
            hashCode = hashCode * -1521134295 + EqualityComparer<Point3D>.Default.GetHashCode(Point2);
            return hashCode;
        }
    }
}
