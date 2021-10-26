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
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.Geometry
{
    public class SystemJsonPoint2DConverter : JsonConverter<Point2D>
    {
        public override Point2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Point2D.TryParse(reader.GetString(), out var point) ? point : Point2D.Empty;
        }

        public override void Write(Utf8JsonWriter writer, Point2D value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class SystemJsonPoint3DConverter : JsonConverter<Point3D>
    {
        public override Point3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Point3D.TryParse(reader.GetString(), out var point) ? point : Point3D.Empty;
        }

        public override void Write(Utf8JsonWriter writer, Point3D value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class SystemJsonRectangle2DConverter : JsonConverter<Rectangle2D>
    {
        public override Rectangle2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Rectangle2D.TryParse(reader.GetString(), out var rect) ? rect : Rectangle2D.Empty;
        }

        public override void Write(Utf8JsonWriter writer, Rectangle2D value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    public class SystemJsonRectangle3DConverter : JsonConverter<Rectangle3D>
    {
        public override Rectangle3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Rectangle3D.TryParse(reader.GetString(), out var rect) ? rect : Rectangle3D.Empty;
        }

        public override void Write(Utf8JsonWriter writer, Rectangle3D value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}