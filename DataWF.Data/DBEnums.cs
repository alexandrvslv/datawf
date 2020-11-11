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

namespace DataWF.Data
{
    [Flags]
    public enum DBViewKeys
    {
        None = 0,
        Static = 2,
        Empty = 4,
        Access = 8,
        NoAttach = 16,
        Synch = 32,
        Lock = 64
    }

    public enum DDLType
    {
        Alter,
        Create,
        Drop,
        Default
    }

    public enum DBExecuteType
    {
        Scalar,
        Reader,
        NoReader
    }

    public enum DBConstraintType
    {
        Primary,
        Foreign,
        Unique,
        Default,
        Check
    }

    public enum DBTableType
    {
        Table,
        View,
        Query
    }

    [Flags()]
    public enum DBLoadParam
    {
        None = 0,
        Load = 2,
        Synchronize = 4,
        GetCount = 8,
        CheckDeleted = 16,
        Reference = 32,
        Referencing = 64,
        NoAttach = 128
    }

    public enum DBDataType
    {
        None,
        String,
        Clob,
        Date,
        DateTime,
        TimeStamp,
        TimeSpan,
        Blob,
        ByteArray,
        Decimal,
        Double,
        Float,
        BigInt,
        Int,
        ShortInt,
        TinyInt,
        Bool,
        UInt,
        Object,
        ByteSerializable,
        LargeObject
    }

    public enum DBCommandTypes
    {
        Insert,
        InsertSequence,
        Update,
        Delete,
        Query
    }

    public enum DBColumnTypes
    {
        Default,
        Query,
        Internal,
        Expression,
        Code
    }

    [Flags()]
    public enum DBUpdateState
    {
        Default = 0,
        Commit = 1,
        Insert = 2,
        Update = 4,
        Delete = 8,
        InsertCommit = 3,
        UpdateCommit = 5,
        DeleteCommit = 9
    }

    [Flags()]
    public enum DBItemState
    {
        New = 0,
        Attached = 1,
        Check = 2,
        Expand = 4
    }

    public enum DBQueryTarget
    {
        UserDefined,
        TableSearch,
        Other
    }

    public enum QParcerState
    {
        Where,
        Select,
        From,
        OrderBy,
        GroupBy
    }

    public enum QFunctionType
    {
        none,
        avg,
        concat,
        coalesce,
        initcap,
        trim,
        rtrim,
        ltrim,
        convert,
        cast,
        sum,
        distinct,
        upper,
        lower,
        group,
        to_char,
        to_date,
        getdate,
        datename,
        format,
        parse,
        max,
        min,
        count

    }

    public enum DBRowBinarySeparator
    {
        None,
        ColumnsStart,
        ColumnsEnd,
        RowStart,
        RowEnd,
        End,
        Null
    }

    public enum DBLogType
    {
        None,
        Insert,
        Update,
        Delete
    }

    public enum DBSetValueMode
    {
        Default,
        Loading
    }
}
