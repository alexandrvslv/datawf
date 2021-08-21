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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Geometry
{
    [JsonConverter(typeof(SystemJsonPoint2DConverter)), Newtonsoft.Json.JsonConverter(typeof(NewtonJsonPoint2DConverter))]
    public struct Point2D : IBinarySerializable, IComparable<Point2D>, IEquatable<Point2D>
    {
        internal static readonly char[] TrimArray = new char[] { ' ', ',', '(', ')' };

        public static readonly Point2D Empty = new Point2D();

        public static bool TryParse(string text, out Point2D point)
        {
            point = new Point2D();
            text = text.Trim(TrimArray);
            var split = text.Split(',');
            if (split.Length > 1
                && double.TryParse(split[0], NumberStyles.Number, CultureInfo.InvariantCulture, out var x)
                && double.TryParse(split[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var y))
            {
                point.X = x;
                point.Y = y;
                return true;
            }
            return false;
        }

        public static bool operator ==(Point2D a, Point2D b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Point2D a, Point2D b)
        {
            return !a.Equals(b);
        }

        public Point2D(byte[] data)
        {
            X = BitConverter.ToDouble(data, 0);
            Y = BitConverter.ToDouble(data, 8);
        }

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
            if (obj == null)
                return false;
            return Equals((Point2D)obj);
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
            return FormattableString.Invariant($"({X}, {Y})");
        }


    }
}
