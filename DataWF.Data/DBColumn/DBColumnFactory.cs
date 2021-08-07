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
using DataWF.Data;
using System;

namespace DataWF.Data
{
    public static class DBColumnFactory
    {
        public static DBColumn Create(Type dataType)
        {
            var column = (DBColumn)null;
            if (dataType == typeof(string))
                column = new DBColumnString();
            else if (dataType == typeof(byte[]))
                column = new DBColumnByteArray();
            else if (TypeHelper.IsNullable(dataType))
            {
                dataType = TypeHelper.CheckNullable(dataType);

                if (dataType == typeof(bool))
                    column = new DBColumnNBool();
                else if (dataType == typeof(sbyte))
                    column = new DBColumnNInt8();
                else if (dataType == typeof(byte))
                    column = new DBColumnNUInt8();
                else if (dataType == typeof(short))
                    column = new DBColumnNInt16();
                else if (dataType == typeof(ushort))
                    column = new DBColumnNUInt16();
                else if (dataType == typeof(int))
                    column = new DBColumnNInt32();
                else if (dataType == typeof(uint))
                    column = new DBColumnNUInt32();
                else if (dataType == typeof(long))
                    column = new DBColumnNInt64();
                else if (dataType == typeof(ulong))
                    throw new Exception("Unsupported type unsigned long");
                else if (dataType == typeof(float))
                    column = new DBColumnNFloat();
                else if (dataType == typeof(double))
                    column = new DBColumnNDouble();
                else if (dataType == typeof(decimal))
                    column = new DBColumnNDecimal();
                else if (dataType == typeof(DateTime))
                    column = new DBColumnNDateTime();
                else if (dataType == typeof(TimeSpan))
                    column = new DBColumnNTimeSpan();
                else if (dataType.IsEnum)
                {
                    var enumType = Enum.GetUnderlyingType(dataType);
                    if (enumType == typeof(sbyte))
                        column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnNEnumInt8<>).MakeGenericType(dataType));
                    else if (enumType == typeof(byte))
                        column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnNEnumUInt8<>).MakeGenericType(dataType));
                    else if (enumType == typeof(short))
                        column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnNEnumInt16<>).MakeGenericType(dataType));
                    else if (enumType == typeof(ushort))
                        column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnNEnumUInt16<>).MakeGenericType(dataType));
                    else if (enumType == typeof(int))
                        column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnNEnumInt32<>).MakeGenericType(dataType));
                    else if (enumType == typeof(uint))
                        column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnNEnumUInt32<>).MakeGenericType(dataType));
                    else if (enumType == typeof(long))
                        column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnNEnumInt64<>).MakeGenericType(dataType));
                    else if (enumType == typeof(ulong))
                        throw new Exception("Unsupported type unsigned long");
                }
                else if (TypeHelper.IsInterface(dataType, typeof(IBinarySerializable)))
                    column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnNBinarySerializable<>).MakeGenericType(dataType));
                else
                {
                    column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnNullable<>).MakeGenericType(dataType));
                }
            }
            else if (dataType == typeof(bool))
                column = new DBColumnBool();
            else if (dataType == typeof(sbyte))
                column = new DBColumnInt8();
            else if (dataType == typeof(byte))
                column = new DBColumnUInt8();
            else if (dataType == typeof(short))
                column = new DBColumnInt16();
            else if (dataType == typeof(ushort))
                column = new DBColumnUInt16();
            else if (dataType == typeof(int))
                column = new DBColumnInt32();
            else if (dataType == typeof(uint))
                column = new DBColumnUInt32();
            else if (dataType == typeof(long))
                column = new DBColumnInt64();
            else if (dataType == typeof(ulong))
                throw new Exception("Unsupported type unsigned long");
            else if (dataType == typeof(float))
                column = new DBColumnFloat();
            else if (dataType == typeof(double))
                column = new DBColumnDouble();
            else if (dataType == typeof(decimal))
                column = new DBColumnDecimal();
            else if (dataType == typeof(DateTime))
                column = new DBColumnDateTime();
            else if (dataType == typeof(TimeSpan))
                column = new DBColumnTimeSpan();
            else if (dataType.IsEnum)
            {
                var enumType = Enum.GetUnderlyingType(dataType);
                if (enumType == typeof(sbyte))
                    column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnEnumInt8<>).MakeGenericType(dataType));
                else if (enumType == typeof(byte))
                    column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnEnumUInt8<>).MakeGenericType(dataType));
                else if (enumType == typeof(short))
                    column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnEnumInt16<>).MakeGenericType(dataType));
                else if (enumType == typeof(ushort))
                    column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnEnumUInt16<>).MakeGenericType(dataType));
                else if (enumType == typeof(int))
                    column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnEnumInt32<>).MakeGenericType(dataType));
                else if (enumType == typeof(uint))
                    column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnEnumUInt32<>).MakeGenericType(dataType));
                else if (enumType == typeof(long))
                    column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnEnumInt64<>).MakeGenericType(dataType));
                else if (enumType == typeof(ulong))
                    throw new Exception("Unsupported type unsigned long");
            }
            else if (TypeHelper.IsInterface(dataType, typeof(IBinarySerializable)))
                column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumnBinarySerializable<>).MakeGenericType(dataType));
            else
                column = (DBColumn)EmitInvoker.CreateObject(typeof(DBColumn<>).MakeGenericType(dataType));
            return column;
        }

        public static DBColumn Create(Type dataType, string name, DBColumnKeys keys = DBColumnKeys.None, int size = 0, DBTable table = null)
        {
            var column = Create(dataType);
            column.Name = name;
            column.Keys = keys;
            column.Size = size;
            column.Table = table;
            return column;
        }

        public static DBColumn CreateLog(DBColumn baseColumn, IDBLogTable table)
        {
            var column = (DBColumn)EmitInvoker.CreateObject(baseColumn.GetType());
            column.Table = (DBTable)table;
            column.RefreshLogColumn(baseColumn);
            return column;
        }

        public static DBColumn CreateVirtual(DBColumn baseColumn, DBTable table)
        {
            var column = (DBColumn)EmitInvoker.CreateObject(baseColumn.GetType());
            column.Table = table;
            column.RefreshVirtualColumn(baseColumn);
            return column;
        }

        public static DBColumnGroup CreateGroup(DBColumnGroup baseColumnGroup)
        {
            var columnGroup = new DBColumnGroup()
            {
                Name = baseColumnGroup.Name,
                Order = baseColumnGroup.Order,
                DisplayName = baseColumnGroup.DisplayName,
            };
            return columnGroup;
        }
    }
}