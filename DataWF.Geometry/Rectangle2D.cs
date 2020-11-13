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
using System.Xml.Serialization;

namespace DataWF.Geometry
{
    /// Rectangle in User Space  - Base coordinate: bottom left corner
    [JsonConverter(typeof(SystemJsonRectangle2DConverter)), Newtonsoft.Json.JsonConverter(typeof(NewtonJsonRectangle2DConverter))]
    public struct Rectangle2D : IBinarySerializable, IComparable<Rectangle2D>, IEquatable<Rectangle2D>
    {
        public static readonly Rectangle2D Empty = new Rectangle2D();

        public static bool TryParse(string text, out Rectangle2D rect)
        {
            rect = new Rectangle2D();
            text = text.Trim(Point2D.TrimArray);
            var split = text.Split(',');

            if (split.Length > 3
                && double.TryParse(split[0].Trim(Point2D.TrimArray), NumberStyles.Number, CultureInfo.InvariantCulture, out var left)
                && double.TryParse(split[1].Trim(Point2D.TrimArray), NumberStyles.Number, CultureInfo.InvariantCulture, out var bottom)
                && double.TryParse(split[2].Trim(Point2D.TrimArray), NumberStyles.Number, CultureInfo.InvariantCulture, out var right)
                && double.TryParse(split[3].Trim(Point2D.TrimArray), NumberStyles.Number, CultureInfo.InvariantCulture, out var top))
            {
                rect.left = left;
                rect.bottom = bottom;
                rect.right = right;
                rect.top = top;
                return true;
            }
            return false;
        }

        public static bool operator ==(Rectangle2D a, Rectangle2D b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Rectangle2D a, Rectangle2D b)
        {
            return !a.Equals(b);
        }

        private double left;
        private double bottom;
        private double right;
        private double top;

        public Rectangle2D(byte[] data)
        {
            left = BitConverter.ToDouble(data, 0);
            bottom = BitConverter.ToDouble(data, 8);
            right = BitConverter.ToDouble(data, 16);
            top = BitConverter.ToDouble(data, 24);
        }

        public Rectangle2D(double left, double bottom, double right, double top)
        {
            this.left = left;
            this.bottom = bottom;
            this.right = right;
            this.top = top;
        }

        public double Left
        {
            get => left;
            set => left = value;
        }

        public double Bottom
        {
            get => bottom;
            set => bottom = value;
        }

        public double Right
        {
            get => right;
            set => right = value;
        }

        public double Top
        {
            get => top;
            set => top = value;
        }

        [XmlIgnore, JsonIgnore]
        public double Width => Right - Left;

        [XmlIgnore, JsonIgnore]
        public double Height => Top - Bottom;

        [XmlIgnore, JsonIgnore]
        public Point2D BottomLeft => new Point2D(left, bottom);

        [XmlIgnore, JsonIgnore]
        public Point2D TopRight => new Point2D(right, top);

        public int CompareTo(Rectangle2D other)
        {
            if (other == null)
                return 1;
            var result = Bottom.CompareTo(other.Bottom);
            if (result == 0)
                result = Left.CompareTo(other.Left);
            if (result == 0)
                result = Top.CompareTo(other.Top);
            if (result == 0)
                result = Right.CompareTo(other.Right);
            return result;
        }

        public bool Equals(Rectangle2D other)
        {
            return Bottom.Equals(other.Bottom)
                && Left.Equals(other.Left)
                && Top.Equals(other.Top)
                && Right.Equals(other.Right);
        }

        public void Deserialize(BinaryReader reader)
        {
            Left = reader.ReadDouble();
            Bottom = reader.ReadDouble();
            Right = reader.ReadDouble();
            Top = reader.ReadDouble();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Left);
            writer.Write(Bottom);
            writer.Write(Right);
            writer.Write(Top);
        }

        public void Deserialize(byte[] data)
        {
            Left = BitConverter.ToDouble(data, 0);
            Bottom = BitConverter.ToDouble(data, 8);
            Right = BitConverter.ToDouble(data, 16);
            Top = BitConverter.ToDouble(data, 24);
        }

        public byte[] Serialize()
        {
            var buffer = new byte[32];
            Array.Copy(BitConverter.GetBytes(Left), 0, buffer, 0, 8);
            Array.Copy(BitConverter.GetBytes(Bottom), 0, buffer, 8, 8);
            Array.Copy(BitConverter.GetBytes(Right), 0, buffer, 16, 8);
            Array.Copy(BitConverter.GetBytes(Top), 0, buffer, 24, 8);
            return buffer;
        }

        public void Append(Point2D point)
        {
            if (Left > point.X)
            {
                Left = point.X;
            }
            if (Right < point.X)
            {
                Right = point.X;
            }

            if (Bottom > point.Y)
            {
                Bottom = point.Y;
            }
            if (Top < point.Y)
            {
                Top = point.Y;
            }
        }

        public void Reset(Point2D point)
        {
            Left =
                Right = point.X;
            Top =
                Bottom = point.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            return Equals((Rectangle2D)obj);
        }

        public override string ToString()
        {
            return FormattableString.Invariant($"({Left}, {Bottom}), ({Right}, {Top})");
        }

        public override int GetHashCode()
        {
            int hashCode = -1819631549;
            hashCode = hashCode * -1521134295 + Left.GetHashCode();
            hashCode = hashCode * -1521134295 + Top.GetHashCode();
            hashCode = hashCode * -1521134295 + Right.GetHashCode();
            hashCode = hashCode * -1521134295 + Bottom.GetHashCode();
            return hashCode;
        }
    }
}

