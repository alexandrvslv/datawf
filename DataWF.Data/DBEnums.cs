﻿//  The MIT License (MIT)
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
    public enum DBViewKeys : byte
    {
        None = 0,
        Static = 2,
        Empty = 4,
        Access = 8,
        NoAttach = 16,
        Synch = 32,
        Lock = 64
    }

    public enum DDLType : byte
    {
        None,
        Alter,
        Create,
        Drop
    }

    public enum DBExecuteType : byte
    {
        Scalar,
        Reader,
        NoReader
    }

    public enum DBConstraintType : byte
    {
        Primary,
        Foreign,
        Unique,
        Default,
        Check
    }

    public enum DBTableType : byte
    {
        Table,
        View,
        Query
    }

    [Flags()]
    public enum DBLoadParam : short
    {
        None = 0,
        Load = 2,
        GetCount = 8,
        CheckDeleted = 16,
        Reference = 32,
        Referencing = 64,
        NoAttach = 128,
        NoCache = 256,
        DownloadFiles = 512

    }

    public enum DBDataType : byte
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
        Object,
        ByteSerializable,
    }

    public enum DBCommandTypes : byte
    {
        Insert,
        InsertSequence,
        Update,
        Delete,
        Query
    }

    public enum DBColumnTypes : byte
    {
        Default,
        Query,
        Internal,
        Expression,
        Code
    }

    [Flags()]
    public enum DBUpdateState : byte
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
    public enum DBItemState : byte
    {
        New = 0,
        Attached = 1,
        Check = 2,
        Expand = 4,
        Load = 8,
    }

    public enum QParserState : byte
    {
        Where,
        Select,
        From,
        OrderBy,
        GroupBy
    }

    public enum QFunctionType : byte
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

    public enum DBLogType : byte
    {
        None,
        Insert,
        Update,
        Delete
    }

    public enum DBSetValueMode : byte
    {
        Default,
        Loading
    }
}
