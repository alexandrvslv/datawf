﻿using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public struct AccessItem : IAccessItem
    {
        public static readonly AccessItem Empty = new AccessItem();
        private IAccessIdentity identity;
        private int identityId;
        public AccessItem(bool isUser, int identityId, AccessType data)
        {
            IsUser = isUser;
            Access = data;
            this.identityId = identityId;
            this.identity = null;
        }

        public AccessItem(IAccessIdentity identity, AccessType data = AccessType.None) : this()
        {
            Identity = identity;
            Access = data;
        }

        public override string ToString()
        {
            return $"{Identity?.Name}({Access})";
        }

        [XmlIgnore, JsonIgnore]
        public IAccessIdentity Identity
        {
            get => identity ?? (identity = IsUser
                ? (IAccessIdentity)AccessValue.Users.GetById(identityId)
                : (IAccessIdentity)AccessValue.Groups.GetById(identityId));
            private set
            {
                identityId = value?.Id ?? -1;
                identity = value;
                IsUser = value is IUserIdentity;
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

        public bool IsUser { get; set; }

        public AccessType Access { get; set; }

        [XmlIgnore, JsonIgnore]
        public bool IsEmpty => IdentityId <= 0;

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

        internal void Serialize(BinaryWriter writer)
        {
            writer.Write(IsUser);
            writer.Write(IdentityId);
            writer.Write((int)Access);
        }

        public static AccessItem Deserialize(BinaryReader reader, bool user)
        {
            return new AccessItem(user ? reader.ReadBoolean() : false, reader.ReadInt32(), (AccessType)reader.ReadInt32());
        }
    }
}
