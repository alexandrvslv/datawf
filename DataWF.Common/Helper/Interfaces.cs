using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

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
        DBNull = 18
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
        Using
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

    [Flags]
    public enum DBStatus
    {
        Empty = 0,
        Actual = 1,
        New = 2,
        Edit = 4,
        Error = 8,
        Current = 16,
        Delete = 32,
        Accept = 64,
        Archive = 128,
        Old = 256
    }


    public interface IStatus
    {
        DBStatus Status { get; set; }
    }

    public interface IComparerList : IComparer
    {
        void Add(IComparer comparer);
    }

    public interface IComparerList<T> : IComparer<T>
    {
        void Add(IComparer<T> comparer);
    }

    public interface IStringConverter
    {
        string FormatObject(object val);
        object ParceObjcet(string val, Type type);
    }

    public interface IEditable
    {
        void Refresh();

        void Save();

        void Reject();

        void Accept();

        bool IsChanged { get; }
    }

    public interface ICheck
    {
        bool Check { get; set; }
    }

    public interface IGroup : IComparable
    {
        bool IsExpanded { get; }

        IGroup Group { get; set; }

        bool Expand { get; set; }

        bool IsCompaund { get; }
    }

    public interface IReadOnly
    {
        bool ReadOnly { get; set; }
    }

    public interface IFileSerialize
    {
        void Save(string file);

        void Save();

        void Load(string file);

        void Load();

        string FileName
        { get; set; }
    }

    public interface ILocalizable
    {
        void Localize();
    }

    public interface IData
    {
        byte[] Data { get; set; }
        string DataName { get; set; }
    }

    public interface INamed
    {
        string Name { get; set; }
    }

    public interface INamedList : IList
    {
        INamed Get(string name);
        void Set(INamed value);
    }
}
