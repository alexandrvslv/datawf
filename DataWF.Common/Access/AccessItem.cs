using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public struct AccessItem : IAccessItem, IByteSerializable, IEquatable<AccessItem>
    {
        public static readonly AccessItem Empty = new AccessItem(null);
        public static readonly Func<IAccessIdentity> IdentityFunc;
        private IAccessIdentity identity;
        private int identityId;

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
            Access = data;
            this.identityId = identityId;
            this.identity = null;
        }

        public AccessItem(IAccessIdentity identity, AccessType data = AccessType.None) : this()
        {
            Identity = identity;
            Access = data;
        }

        [XmlIgnore, JsonIgnore]
        public IAccessIdentity Identity
        {
            get
            {
                if (AccessValue.GetAccessIdentityFunc == null)
                {
                    AccessValue.GetAccessIdentityFunc = AccessValue.GetAccessIdentity;
                }
                return AccessValue.GetAccessIdentityFunc(IdentityId, IdentityType);
            }
            set
            {
                identityId = value?.Id ?? -1;
                identity = value;
                if (value is IProjectIdentity)
                {
                    IdentityType = IdentityType.Project;
                }
                else
                if (value is ICompanyIdentity)
                {
                    IdentityType = IdentityType.Company;
                }
                else
                if (value is IGroupIdentity)
                {
                    IdentityType = IdentityType.Group;
                }
                else
                if (value is IUserIdentity)
                {
                    IdentityType = IdentityType.User;
                }
            }
        }

        public int IdentityId
        {
            get => identityId;
            set
            {
                identityId = value;
                identity = null;
            }
        }

        public IdentityType IdentityType { get; set; }

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

        public override bool Equals(object obj)
        {
            return obj is AccessItem item ? Equals(item) : false;
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
            Array.Copy(BitConverter.GetBytes((byte)IdentityType), 0, buffer, 0, 1);
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
            return new AccessItem(user ? (IdentityType)reader.ReadByte() : IdentityType.Group, reader.ReadInt32(), (AccessType)reader.ReadInt32());
        }

        public override int GetHashCode()
        {
            int hashCode = 1380532211;
            hashCode = hashCode * -1521134295 + IdentityId.GetHashCode();
            hashCode = hashCode * -1521134295 + IdentityType.GetHashCode();
            hashCode = hashCode * -1521134295 + Access.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"{Identity?.Name}({Access})";
        }


    }
}
