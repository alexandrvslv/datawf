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
using System.IO;

namespace DataWF.Geometry
{
    //Skia concept
    public class Matrix2D : IByteSerializable, IEquatable<Matrix2D>
    {
        public static bool operator ==(Matrix2D a, Matrix2D b)
        {
            return a?.Equals(b) ?? b?.Equals(a) ?? true;
        }

        public static bool operator !=(Matrix2D a, Matrix2D b)
        {
            return !(a?.Equals(b) ?? b?.Equals(a) ?? true);
        }

        public static Matrix2D CreateIdentity() => new Matrix2D { ScaleX = 1, ScaleY = 1, Persp2 = 1 };

        public Matrix2D(byte[] data)
        {
            Deserialize(data);
        }

        public Matrix2D()
        { }

        public Matrix2D(double scaleX, double skewX, double transX,
            double skewY, double scaleY, double transY,
            double persp0, double persp1, double persp2)
        {
            ScaleX = scaleX;
            SkewX = skewX;
            TransX = transX;

            SkewY = skewY;
            ScaleY = scaleY;
            TransY = transY;

            Persp0 = persp0;
            Persp1 = persp1;
            Persp2 = persp2;
        }

        public double ScaleX { get; set; }
        public double SkewX { get; set; }
        public double TransX { get; set; }

        public double SkewY { get; set; }
        public double ScaleY { get; set; }
        public double TransY { get; set; }

        public double Persp0 { get; set; }
        public double Persp1 { get; set; }
        public double Persp2 { get; set; }

        public bool Equals(Matrix2D other)
        {
            if (other == null)
                return false;
            return ScaleX.Equals(other.ScaleX)
                && SkewX.Equals(other.SkewX)
                && TransX.Equals(other.TransX)
                && SkewY.Equals(other.SkewY)
                && ScaleY.Equals(other.ScaleY)
                && TransY.Equals(other.TransY)
                && Persp0.Equals(other.Persp0)
                && Persp1.Equals(other.Persp1)
                && Persp2.Equals(other.Persp2);
        }

        public void Deserialize(BinaryReader reader)
        {
            ScaleX = reader.ReadDouble();
            SkewX = reader.ReadDouble();
            TransX = reader.ReadDouble();

            SkewY = reader.ReadDouble();
            ScaleY = reader.ReadDouble();
            TransY = reader.ReadDouble();

            Persp0 = reader.ReadDouble();
            Persp1 = reader.ReadDouble();
            Persp2 = reader.ReadDouble();
        }

        public void Deserialize(byte[] data)
        {
            ScaleX = BitConverter.ToDouble(data, 0);
            SkewX = BitConverter.ToDouble(data, 8);
            TransX = BitConverter.ToDouble(data, 16);

            SkewY = BitConverter.ToDouble(data, 24);
            ScaleY = BitConverter.ToDouble(data, 32);
            TransY = BitConverter.ToDouble(data, 40);

            Persp0 = BitConverter.ToDouble(data, 48);
            Persp1 = BitConverter.ToDouble(data, 56);
            Persp2 = BitConverter.ToDouble(data, 64);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ScaleX);
            writer.Write(SkewX);
            writer.Write(TransX);

            writer.Write(SkewY);
            writer.Write(ScaleY);
            writer.Write(TransY);

            writer.Write(Persp0);
            writer.Write(Persp1);
            writer.Write(Persp2);
        }

        public byte[] Serialize()
        {
            var buffer = new byte[72];
            Array.Copy(BitConverter.GetBytes(ScaleX), 0, buffer, 0, 8);
            Array.Copy(BitConverter.GetBytes(SkewX), 0, buffer, 8, 8);
            Array.Copy(BitConverter.GetBytes(TransX), 0, buffer, 16, 8);

            Array.Copy(BitConverter.GetBytes(SkewY), 0, buffer, 24, 8);
            Array.Copy(BitConverter.GetBytes(ScaleY), 0, buffer, 32, 8);
            Array.Copy(BitConverter.GetBytes(TransY), 0, buffer, 40, 8);

            Array.Copy(BitConverter.GetBytes(Persp0), 0, buffer, 48, 8);
            Array.Copy(BitConverter.GetBytes(Persp1), 0, buffer, 56, 8);
            Array.Copy(BitConverter.GetBytes(Persp2), 0, buffer, 64, 8);
            return buffer;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Matrix2D);
        }

        public override string ToString()
        {
            return FormattableString.Invariant($"({ScaleX}, {SkewX}, {TransX}), ({SkewY}, {ScaleY}, {TransY}), ({Persp0}, {Persp1}, {Persp2})");
        }

        public override int GetHashCode()
        {
            int hashCode = -62375069;
            hashCode = hashCode * -1521134295 + ScaleX.GetHashCode();
            hashCode = hashCode * -1521134295 + SkewX.GetHashCode();
            hashCode = hashCode * -1521134295 + TransX.GetHashCode();
            hashCode = hashCode * -1521134295 + SkewY.GetHashCode();
            hashCode = hashCode * -1521134295 + ScaleY.GetHashCode();
            hashCode = hashCode * -1521134295 + TransY.GetHashCode();
            hashCode = hashCode * -1521134295 + Persp0.GetHashCode();
            hashCode = hashCode * -1521134295 + Persp1.GetHashCode();
            hashCode = hashCode * -1521134295 + Persp2.GetHashCode();
            return hashCode;
        }
    }


}
