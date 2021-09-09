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
        Inner = 1,
        Left = 2,
        Right = 4,        
        Outer = 8,
        Join = 16
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

