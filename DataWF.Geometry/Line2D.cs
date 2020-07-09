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

namespace DataWF.Geometry
{
    public class Line2D : IByteSerializable, IComparable<Line2D>, IEquatable<Line2D>
    {
        public Line2D()
        {
            Point1 = new Point2D();
            Point2 = new Point2D();
        }

        public Line2D(double x1, double y1, double x2, double y2)
        {
            Point1 = new Point2D(x1, y1);
            Point2 = new Point2D(x2, y2);
        }

        public Point2D Point1 { get; set; }

        public Point2D Point2 { get; set; }

        public int CompareTo(Line2D other)
        {
            if (other == null)
                return 1;
            var result = Point1.CompareTo(Point2);
            return result == 0 ? Point2.CompareTo(Point2) : result;
        }

        public bool Equals(Line2D other)
        {
            if (other == null)
                return false;
            return Point1.Equals(other.Point1) && Point2.Equals(other.Point2);
        }

        public void Deserialize(byte[] data)
        {
            Point1.X = BitConverter.ToDouble(data, 0);
            Point1.Y = BitConverter.ToDouble(data, 8);
            Point2.X = BitConverter.ToDouble(data, 16);
            Point2.Y = BitConverter.ToDouble(data, 24);
        }

        public byte[] Serialize()
        {
            var buffer = new byte[32];
            Array.Copy(BitConverter.GetBytes(Point1.X), 0, buffer, 0, 8);
            Array.Copy(BitConverter.GetBytes(Point1.Y), 0, buffer, 8, 8);
            Array.Copy(BitConverter.GetBytes(Point2.X), 0, buffer, 16, 8);
            Array.Copy(BitConverter.GetBytes(Point2.Y), 0, buffer, 24, 8);
            return buffer;
        }

        public override string ToString()
        {
            return $"{Point1} - {Point2}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Line2D);
        }

        public override int GetHashCode()
        {
            int hashCode = 363529913;
            hashCode = hashCode * -1521134295 + EqualityComparer<Point2D>.Default.GetHashCode(Point1);
            hashCode = hashCode * -1521134295 + EqualityComparer<Point2D>.Default.GetHashCode(Point2);
            return hashCode;
        }
    }
}
