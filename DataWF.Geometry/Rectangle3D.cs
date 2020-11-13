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
    [JsonConverter(typeof(SystemJsonRectangle3DConverter)), Newtonsoft.Json.JsonConverter(typeof(NewtonJsonRectangle3DConverter))]
    public struct Rectangle3D : IBinarySerializable, IComparable<Rectangle3D>, IEquatable<Rectangle3D>
    {
        public static readonly Rectangle3D Empty = new Rectangle3D();

        public static bool TryParse(string text, out Rectangle3D rect)
        {
            rect = new Rectangle3D();
            text = text.Trim(Point2D.TrimArray);
            var split = text.Split(',');

            if (split.Length > 5
                && double.TryParse(split[0].Trim(Point2D.TrimArray), NumberStyles.Number, CultureInfo.InvariantCulture, out var left)
                && double.TryParse(split[1].Trim(Point2D.TrimArray), NumberStyles.Number, CultureInfo.InvariantCulture, out var bottom)
                && double.TryParse(split[2].Trim(Point2D.TrimArray), NumberStyles.Number, CultureInfo.InvariantCulture, out var near)
                && double.TryParse(split[3].Trim(Point2D.TrimArray), NumberStyles.Number, CultureInfo.InvariantCulture, out var right)
                && double.TryParse(split[4].Trim(Point2D.TrimArray), NumberStyles.Number, CultureInfo.InvariantCulture, out var top)
                && double.TryParse(split[5].Trim(Point2D.TrimArray), NumberStyles.Number, CultureInfo.InvariantCulture, out var far))
            {
                rect.Left = left;
                rect.Bottom = bottom;
                rect.Near = near;
                rect.Right = right;
                rect.Top = top;
                rect.Far = far;
                return true;
            }
            return false;
        }

        public static bool operator ==(Rectangle3D a, Rectangle3D b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Rectangle3D a, Rectangle3D b)
        {
            return !a.Equals(b);
        }

        public Rectangle3D(byte[] data)
        {
            Left = BitConverter.ToDouble(data, 0);
            Bottom = BitConverter.ToDouble(data, 8);
            Near = BitConverter.ToDouble(data, 16);
            Right = BitConverter.ToDouble(data, 24);
            Top = BitConverter.ToDouble(data, 32);
            Far = BitConverter.ToDouble(data, 40);
        }

        public Rectangle3D(double left, double bottom, double near, double right, double top, double far)
        {
            Left = left;
            Bottom = bottom;
            Near = near;
            Right = right;
            Top = top;
            Far = far;
        }

        public double Left { get; set; }

        public double Bottom { get; set; }

        public double Near { get; set; }

        public double Right { get; set; }

        public double Top { get; set; }

        public double Far { get; set; }

        [XmlIgnore, JsonIgnore]
        public double Width => Right - Left;

        [XmlIgnore, JsonIgnore]
        public double Height => Top - Bottom;

        [XmlIgnore, JsonIgnore]
        public double Deep => Far - Near;

        public int CompareTo(Rectangle3D other)
        {
            if (other == null)
                return 1;
            var result = Bottom.CompareTo(other.Bottom);
            if (result == 0)
                result = Left.CompareTo(other.Left);
            if (result == 0)
                result = Near.CompareTo(other.Near);
            if (result == 0)
                result = Top.CompareTo(other.Top);
            if (result == 0)
                result = Right.CompareTo(other.Right);
            if (result == 0)
                result = Far.CompareTo(other.Far);
            return result;
        }

        public bool Equals(Rectangle3D other)
        {
            return Bottom.Equals(other.Bottom)
                && Left.Equals(other.Left)
                && Near.Equals(other.Near)
                && Top.Equals(other.Top)
                && Right.Equals(other.Right)
                && Far.Equals(other.Far);
        }

        public void Deserialize(BinaryReader reader)
        {
            Left = reader.ReadDouble();
            Bottom = reader.ReadDouble();
            Near = reader.ReadDouble();
            Right = reader.ReadDouble();
            Top = reader.ReadDouble();
            Far = reader.ReadDouble();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Left);
            writer.Write(Bottom);
            writer.Write(Near);
            writer.Write(Right);
            writer.Write(Top);
            writer.Write(Far);
        }

        public void Deserialize(byte[] data)
        {
            Left = BitConverter.ToDouble(data, 0);
            Bottom = BitConverter.ToDouble(data, 8);
            Near = BitConverter.ToDouble(data, 16);
            Right = BitConverter.ToDouble(data, 24);
            Top = BitConverter.ToDouble(data, 32);
            Far = BitConverter.ToDouble(data, 40);
        }

        public byte[] Serialize()
        {
            var buffer = new byte[48];
            Array.Copy(BitConverter.GetBytes(Left), 0, buffer, 0, 8);
            Array.Copy(BitConverter.GetBytes(Bottom), 0, buffer, 8, 8);
            Array.Copy(BitConverter.GetBytes(Near), 0, buffer, 16, 8);
            Array.Copy(BitConverter.GetBytes(Right), 0, buffer, 24, 8);
            Array.Copy(BitConverter.GetBytes(Top), 0, buffer, 32, 8);
            Array.Copy(BitConverter.GetBytes(Far), 0, buffer, 40, 8);
            return buffer;
        }

        public void Append(Point3D point)
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

            if (Near > point.Z)
            {
                Near = point.Z;
            }
            if (Far < point.Z)
            {
                Far = point.Z;
            }
        }

        public void Reset(Point3D point)
        {
            Left =
                Right = point.X;
            Top =
                Bottom = point.Y;
            Near =
                Far = point.Z;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return Equals((Rectangle3D)obj);
        }

        public override string ToString()
        {
            return FormattableString.Invariant($"({Left}, {Top}, {Near}), ({Right}, {Bottom}, {Far})");
        }

        public override int GetHashCode()
        {
            int hashCode = 544882480;
            hashCode = hashCode * -1521134295 + Left.GetHashCode();
            hashCode = hashCode * -1521134295 + Bottom.GetHashCode();
            hashCode = hashCode * -1521134295 + Near.GetHashCode();
            hashCode = hashCode * -1521134295 + Right.GetHashCode();
            hashCode = hashCode * -1521134295 + Top.GetHashCode();
            hashCode = hashCode * -1521134295 + Far.GetHashCode();
            return hashCode;
        }
    }
}

