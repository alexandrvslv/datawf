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
using Portable.Xaml.Markup;
using System;

namespace DataWF.Geometry
{
    public class Point2DSerializer : BytesSerializer<Point2D>
    {
        public override string ToString(Point2D value) => value.ToString();

        public override Point2D FromString(string value) => Point2D.TryParse(value, out var point) ? point : Point2D.Empty;
    }

    public class Point3DSerializer : BytesSerializer<Point3D>
    {
        public override string ToString(Point3D value) => value.ToString();
        public override Point3D FromString(string value) => Point3D.TryParse(value, out var point) ? point : Point3D.Empty;
    }

    public class Rectangle2DSerializer : BytesSerializer<Rectangle2D>
    {
        public override string ToString(Rectangle2D value) => value.ToString();
        public override Rectangle2D FromString(string value) => Rectangle2D.TryParse(value, out var rect) ? rect : Rectangle2D.Empty;
    }

    public class Rectangle3DSerializer : BytesSerializer<Rectangle3D>
    {
        public override string ToString(Rectangle3D value) => value.ToString();
        public override Rectangle3D FromString(string value) => Rectangle3D.TryParse(value, out var rect) ? rect : Rectangle3D.Empty;
    }
}