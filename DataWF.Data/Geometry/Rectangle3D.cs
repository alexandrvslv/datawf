using DataWF.Common;
using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data.Geometry
{
    public class Rectangle3D : IByteSerializable, IComparable<Rectangle3D>, IEquatable<Rectangle3D>
    {
        public Rectangle3D()
        { }

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
            if (other == null)
                return false;

            return Bottom.Equals(other.Bottom)
                && Left.Equals(other.Left)
                && Near.Equals(other.Near)
                && Top.Equals(other.Top)
                && Right.Equals(other.Right)
                && Far.Equals(other.Far);
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
            return Equals(obj as Rectangle3D);
        }

        public override string ToString()
        {
            return $"({Left}, {Top}, {Near}), ({Right}, {Bottom}, {Far})";
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

