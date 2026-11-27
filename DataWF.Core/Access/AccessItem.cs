using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Access;


public struct AccessItem : IAccessItem, IEquatable<AccessItem>//IBinarySerializable, 
{
    public static readonly AccessItem Empty = new();

    public static bool operator ==(AccessItem a, AccessItem b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(AccessItem a, AccessItem b)
    {
        return !a.Equals(b);
    }

    public AccessItem(IdentityType identityType, int identityId, AccessType data)
    {
        IdentityType = identityType;
        IdentityId = identityId;
        Access = data;
    }

    public AccessItem(IdentityHash identityHash, AccessType data)
    {
        IdentityType = identityHash.Type;
        IdentityId = identityHash.Id;
        Access = data;
    }

    public AccessItem(IAccessIdentity identity, AccessType data = AccessType.None) : this()
    {
        SetIdentity(identity);
        Access = data;
    }

    public IAccessIdentity? GetIdentity(IAccessProvider accessProvider)
        => accessProvider.GetAccessIdentity(IdentityId, IdentityType);

    public void SetIdentity(IAccessIdentity value)
    {
        IdentityId = value?.Id ?? -1;
        switch (value)
        {
            case IProjectIdentity:
                IdentityType = IdentityType.Project;
                break;
            case ICompanyIdentity:
                IdentityType = IdentityType.Company;
                break;
            case IGroupIdentity:
                IdentityType = IdentityType.Group;
                break;
            case IUserIdentity:
                IdentityType = IdentityType.User;
                break;
        }
    }

    public int IdentityId { get; set; }

    public IdentityType IdentityType { get; set; }

    [XmlIgnore, JsonIgnore]
    public IdentityHash IdentityHash => new(IdentityId, IdentityType);

    [DefaultValue(AccessType.None)]
    public AccessType Access { get; set; }

    [XmlIgnore, JsonIgnore]
    public bool IsEmpty => IdentityId < 0;

    [XmlIgnore, JsonIgnore, DefaultValue(false)]
    public bool Read
    {
        get { return (Access & AccessType.Read) == AccessType.Read; }
        set
        {
            if (Read != value)
            {
                if (value)
                    Access |= AccessType.Read;
                else
                    Access &= ~AccessType.Read;
            }
        }
    }

    [XmlIgnore, JsonIgnore, DefaultValue(false)]
    public bool Create
    {
        get { return (Access & AccessType.Create) == AccessType.Create; }
        set
        {
            if (Create != value)
            {
                if (value)
                    Access |= AccessType.Create;
                else
                    Access &= ~AccessType.Create;
            }
        }
    }

    [XmlIgnore, JsonIgnore, DefaultValue(false)]
    public bool Update
    {
        get { return (Access & AccessType.Update) == AccessType.Update; }
        set
        {
            if (Update != value)
            {
                if (value)
                    Access |= AccessType.Update;
                else
                    Access &= ~AccessType.Update;
            }
        }
    }

    [XmlIgnore, JsonIgnore, DefaultValue(false)]
    public bool Delete
    {
        get { return (Access & AccessType.Delete) == AccessType.Delete; }
        set
        {
            if (Delete != value)
            {
                if (value)
                    Access |= AccessType.Delete;
                else
                    Access &= ~AccessType.Delete;
            }
        }
    }

    [XmlIgnore, JsonIgnore, DefaultValue(false)]
    public bool Admin
    {
        get { return (Access & AccessType.Admin) == AccessType.Admin; }
        set
        {
            if (Admin != value)
            {
                if (value)
                    Access |= AccessType.Admin;
                else
                    Access &= ~AccessType.Admin;
            }
        }
    }

    [XmlIgnore, JsonIgnore, DefaultValue(false)]
    public bool Accept
    {
        get { return (Access & AccessType.Accept) == AccessType.Accept; }
        set
        {
            if (Accept != value)
            {
                if (value)
                    Access |= AccessType.Accept;
                else
                    Access &= ~AccessType.Accept;
            }
        }
    }

    [XmlIgnore, JsonIgnore, DefaultValue(false)]
    public bool Download
    {
        get { return (Access & AccessType.Download) == AccessType.Download; }
        set
        {
            if (Accept != value)
            {
                if (value)
                    Access |= AccessType.Download;
                else
                    Access &= ~AccessType.Download;
            }
        }
    }

    [XmlIgnore, JsonIgnore, DefaultValue(false)]
    public bool Full
    {
        get { return (Access & AccessType.Full) == AccessType.Full; }
        set
        {
            if (Accept != value)
            {
                if (value)
                    Access |= AccessType.Full;
                else
                    Access &= ~AccessType.Full;
            }
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is AccessItem item && Equals(item);
    }

    public bool Equals(AccessItem item)
    {
        return IdentityType == item.IdentityType
               && IdentityId == item.IdentityId
               && Access == item.Access;
    }

    public byte[] Serialize()
    {
        var buffer = new byte[9];
        buffer[0] = (byte)IdentityType;
        Array.Copy(BitConverter.GetBytes(IdentityId), 0, buffer, 1, 4);
        Array.Copy(BitConverter.GetBytes((int)Access), 0, buffer, 5, 4);
        return buffer;
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write((byte)IdentityType);
        writer.Write(IdentityId);
        writer.Write((int)Access);
    }

    public void Deserialize(byte[] buffer)
    {
        IdentityType = (IdentityType)buffer[0];
        IdentityId = BitConverter.ToInt32(buffer, 1);
        Access = (AccessType)BitConverter.ToInt32(buffer, 5);
    }

    public void Deserialize(BinaryReader reader)
    {
        IdentityType = (IdentityType)reader.ReadByte();
        IdentityId = reader.ReadInt32();
        Access = (AccessType)reader.ReadInt32();
    }

    public static AccessItem Deserialize(BinaryReader reader, bool user)
    {
        var identityType = user ? (IdentityType)reader.ReadByte() : IdentityType.Group;
        var identityId = reader.ReadInt32();
        return new AccessItem(identityType, identityId, (AccessType)reader.ReadInt32());
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IdentityType, IdentityId, Access);
    }

    public override string ToString()
    {
        return $"{GetIdentity(AccessProviderStub.Current)?.Name}({Access})";
    }
}
