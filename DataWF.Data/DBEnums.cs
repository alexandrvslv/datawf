/*
 DBEnums.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>  

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
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
        LargeObject,
        Object
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
        min
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

}
