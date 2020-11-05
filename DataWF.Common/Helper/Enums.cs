using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public enum GridOrientation
    {
        Horizontal,
        Vertical
    }

    public enum SizingAligment
    {
        None,
        Bootom,
        BootomLeft,
        BootomRight,
        Top,
        TopLeft,
        TopRight,
        Left,
        Right
    }

    public enum CheckedState
    {
        Unchecked = 0,
        Checked = 1,
        Indeterminate = 2
    }

    public enum ToolShowMode
    {
        Default,
        Dialog,
        AutoHide,
        ToolTip,
        Modal
    }

    public enum LogicTypes
    {
        Undefined,
        And,
        Or
    }

    public enum BinaryTypeIndex : byte
    {
        Boolean = 1,
        Byte = 2,
        SByte = 3,
        Char = 4,
        Short = 5,
        UShort = 6,
        Int = 7,
        UInt = 8,
        Long = 9,
        ULong = 10,
        Float = 11,
        Double = 12,
        Decimal = 13,
        ByteArray = 14,
        CharArray = 15,
        DateTime = 16,
        String = 17,
        Null = 18,
        Array = 19,        
    }

    public enum CompareTypes
    {
        Undefined,
        Equal,
        In,
        Like,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Between,
        Is,
        As,
        Using,
        Contains,
        Intersect,
        Distinct
    }

    [Flags]
    public enum JoinTypes
    {
        Undefined = 0,
        Join = 1,
        Left = 2,
        Right = 4,
        Outer = 8
    }

    public enum EditListState
    {
        ReadOnly,
        Edit,
        EditAny
    }

    public enum CellDisplayState
    {
        Default,
        Alternate,
        Hover,
        Selected,
        Pressed
    }

    public enum StatusType
    {
        Information,
        Warning,
        Error,
    }

    public enum EditModes
    {
        None,
        ByClick,
        ByF2
    }

    //[Flags]
    public enum DBStatus
    {
        Empty = 0,
        Actual = 1,
        New = 2,
        Edit = 4,
        Error = 8,
        Delete = 16,
        Archive = 32
    }
}

