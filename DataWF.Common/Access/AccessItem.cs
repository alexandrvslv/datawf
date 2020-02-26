using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public struct AccessItem : IAccessItem
    {
        public readonly static AccessItem Empty = new AccessItem();

        public AccessItem(IAccessGroup group, AccessType data = AccessType.None) : this()
        {
            Group = group;
            Access = data;
        }

        public override string ToString()
        {
            return $"{Group?.Name}({Access})";
        }

        [XmlIgnore, JsonIgnore]
        public IAccessGroup Group { get; private set; }

        public int GroupId
        {
            get { return Group?.Id ?? -1; }
            set
            {
                if (value >= 0)
                {
                    Group = AccessValue.Groups.FirstOrDefault(p => p.Id == value);
                }
            }
        }

        public AccessType Access { get; set; }

        [JsonIgnore]
        public bool IsEmpty
        {
            get { return Group == null; }
        }

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

        internal void BinaryWrite(BinaryWriter writer)
        {
            writer.Write(GroupId);
            writer.Write((int)Access);
        }

        internal void BinaryRead(BinaryReader reader)
        {
            GroupId = reader.ReadInt32();
            Access = (AccessType)reader.ReadInt32();
        }
    }
}
