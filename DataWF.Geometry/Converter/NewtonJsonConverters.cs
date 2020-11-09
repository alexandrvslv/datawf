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
using Newtonsoft.Json;
using System;

namespace DataWF.Geometry
{
    public class NewtonJsonPoint2DConverter : JsonConverter<Point2D>
    {
        public override Point2D ReadJson(JsonReader reader, Type objectType, Point2D existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return Point2D.TryParse((string)reader.Value, out var point) ? point : Point2D.Empty;
        }

        public override void WriteJson(JsonWriter writer, Point2D value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

    public class NewtonJsonPoint3DConverter : JsonConverter<Point3D>
    {
        public override Point3D ReadJson(JsonReader reader, Type objectType, Point3D existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return Point3D.TryParse((string)reader.Value, out var point) ? point : Point3D.Empty;
        }

        public override void WriteJson(JsonWriter writer, Point3D value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

    public class NewtonJsonRectangle2DConverter : JsonConverter<Rectangle2D>
    {
        public override Rectangle2D ReadJson(JsonReader reader, Type objectType, Rectangle2D existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return Rectangle2D.TryParse((string)reader.Value, out var rect) ? rect : Rectangle2D.Empty;
        }

        public override void WriteJson(JsonWriter writer, Rectangle2D value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

    public class NewtonJsonRectangle3DConverter : JsonConverter<Rectangle3D>
    {
        public override Rectangle3D ReadJson(JsonReader reader, Type objectType, Rectangle3D existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return Rectangle3D.TryParse((string)reader.Value, out var rect) ? rect : Rectangle3D.Empty;
        }

        public override void WriteJson(JsonWriter writer, Rectangle3D value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}