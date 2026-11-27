using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace DataWF.Serialization;

public sealed class XmlInvokeReader : IDisposable
{
    private Stream stream;
    private long startPosition;
    public XmlInvokeReader(Stream stream, XmlInvokerSerializer serializer)
    {
        Serializer = serializer;
        Reader = XmlReader.Create(stream, new XmlReaderSettings { CloseInput = false });
        this.stream = stream;
        startPosition = stream.Position;
    }

    public XmlInvokerSerializer Serializer { get; }

    public XmlReader Reader { get; }

    public string Name => Reader.Name;

    public bool IsEmptyElement => Reader.IsEmptyElement;

    public XmlNodeType NodeType => Reader.NodeType;

    public bool HasAttributes => Reader.HasAttributes;

    public long Consumed => stream.Position - startPosition;

    public static int Level(XmlNode Node)
    {
        XmlNode node = Node;
        int rez = 0;
        while (node.ParentNode != null)
        {
            rez++;
            node = node.ParentNode;
        }
        return rez;
    }

    public void ReadProperties<T>(ref T value, ISerializeTypeInfo<T> info)
    {
        ReadAttributes(ref value, info);
        ReadElements(ref value, info);
    }

    public void ReadAttributes<T>(ref T value, ISerializeTypeInfo<T> info)
    {
        if (!Reader.HasAttributes)
            return;
        while (Reader.MoveToNextAttribute())
        {
            ReadAttribute(ref value, info);
        }

        Reader.MoveToElement();
    }

    public void ReadAttribute<T>(ref T value, ISerializeTypeInfo<T> info)
    {
        var member = info.GetProperty(Reader.Name);
        if (member != null && !member.IsReadOnly)
        {
            member.Read(this, ref value);
        }
    }

    public void ReadElements<T>(ref T value, ISerializeTypeInfo<T> info)
    {
        if (Reader.IsEmptyElement)
            return;
        while (ReadNextElement())
        {
            ReadElement(ref value, info);
        }
    }

    public void ReadElement<T>(ref T value, ISerializeTypeInfo<T> itemInfo)
    {
        var property = itemInfo.GetProperty(Reader.Name);
        if (property != null)
        {
            property.Read(this, ref value);
        }
        else
        {
            Reader.ReadInnerXml();
        }
    }

    public string ReadContent()
    {
        return Reader.ReadInnerXml();
    }

    // public object ReadIFile(object value, ITypeSerializeInfo typeInfo)
    // {
    //     if (value == null || value.GetType() != typeInfo.Type)
    //     {
    //         value = typeInfo.Constructor?.Create();
    //     }
    //     string fileName = Reader.ReadElementContentAsString();
    //     if (fileName != null)
    //     {
    //         var fullName = Path.GetFullPath(fileName);
    //         if (File.Exists(fullName))
    //         {
    //             ((IFileSerialize)value).FileName = fileName;
    //             ((IFileSerialize)value).Load(fullName);
    //         }
    //     }
    //     return value;
    // }

    public bool ReadNextElement()
    {
        while (Reader.Read() && Reader.NodeType != XmlNodeType.EndElement)
        {
            if (Reader.NodeType == XmlNodeType.Element)
            {
                return true;
            }
        }
        return false;
    }

    public T? Read<T>(scoped ref T? value)
    {
        var serializer = Serializer.GetSerializer<T>();
        serializer.Read(this, ref value);
        return value;
    }

    public void Dispose()
    {
        Reader?.Dispose();
    }

    public bool Read() => Reader.Read();

    public string? GetAttribute(string name)
        => Reader.GetAttribute(name);

    public bool MoveToNextAttribute() => Reader.MoveToNextAttribute();

    public string ReadContentAsString()
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadContentAsString();
    }

    public int ReadContentAsInt()
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadContentAsInt();
    }
    public long ReadContentAsLong()
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadContentAsLong();
    }

    public decimal ReadContentAsDecimal()
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadContentAsDecimal();
    }

    public double ReadContentAsDouble()
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadContentAsDouble();
    }

    public float ReadContentAsFloat()
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadContentAsFloat();
    }

    public DateTime ReadContentAsDateTime()
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadContentAsDateTime();
    }

    public DateTimeOffset ReadContentAsDateTimeOffset()
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadContentAsDateTimeOffset();
    }

    public bool ReadContentAsBoolean()
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadContentAsBoolean();
    }

    public int ReadValueChunk(char[] buffer, int index, int count)
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadValueChunk(buffer, index, count);
    }

    public int ReadContentAsBase64(byte[] buffer, int index, int count)
    {
        if (Reader.NodeType == XmlNodeType.Element)
            Reader.Read();
        return Reader.ReadContentAsBase64(buffer, index, count);
    }

    public void MoveToElement() => Reader.MoveToElement();

    internal (bool nullValue, int? length) ReadCollectionAttributes(Action? otherAttributes = null)
    {
        if (!Reader.HasAttributes)
            return (false, null);
        int? length = null;
        try
        {
            while (Reader.MoveToNextAttribute())
            {
                if (Reader.Name == "nil")
                {
                    if (Reader.ReadContentAsBoolean())
                    {
                        return (true, null);
                    }
                }
                else if (Reader.Name == nameof(Array.Length)
                    || Reader.Name == nameof(ICollection<>.Count))
                {
                    length = Reader.ReadContentAsInt();
                }
                else
                {
                    otherAttributes?.Invoke();
                }
            }
        }
        finally
        {
            Reader.MoveToElement();
        }
        return (false, length);

    }
}
