using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public struct AccessItem
    {
        public readonly static AccessItem Empty = new AccessItem();
        public static bool Default = false;

        public AccessItem(IAccessGroup group, AccessType data = AccessType.None) : this()
        {
            Group = group;
            Data = data;
        }

        public override string ToString()
        {
            return $"{Group?.Name}({Data})";
        }

        [XmlIgnore, JsonIgnore]
        public IAccessGroup Group { get; private set; }

        public int GroupId
        {
            get { return Group?.Id ?? -1; }
            internal set
            {
                if (value >= 0)
                {
                    Group = AccessValue.Groups.FirstOrDefault(p => p.Id == value);
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public AccessType Data { get; set; }

        [JsonIgnore]
        public bool IsEmpty
        {
            get { return Data == 0; }
        }

        public bool View
        {
            get { return (Data & AccessType.View) == AccessType.View; }
            set
            {
                if (View != value)
                {
                    if (value)
                        Data |= AccessType.View;
                    else
                        Data &= ~AccessType.View;
                }
            }
        }

        public bool Create
        {
            get { return (Data & AccessType.Create) == AccessType.Create; }
            set
            {
                if (Create != value)
                {
                    if (value)
                        Data |= AccessType.Create;
                    else
                        Data &= ~AccessType.Create;
                }
            }
        }

        public bool Edit
        {
            get { return (Data & AccessType.Edit) == AccessType.Edit; }
            set
            {
                if (Edit != value)
                {
                    if (value)
                        Data |= AccessType.Edit;
                    else
                        Data &= ~AccessType.Edit;
                }
            }
        }

        public bool Delete
        {
            get { return (Data & AccessType.Delete) == AccessType.Delete; }
            set
            {
                if (Delete != value)
                {
                    if (value)
                        Data |= AccessType.Delete;
                    else
                        Data &= ~AccessType.Delete;
                }
            }
        }

        public bool Admin
        {
            get { return (Data & AccessType.Admin) == AccessType.Admin; }
            set
            {
                if (Admin != value)
                {
                    if (value)
                        Data |= AccessType.Admin;
                    else
                        Data &= ~AccessType.Admin;
                }
            }
        }

        public bool Accept
        {
            get { return (Data & AccessType.Accept) == AccessType.Accept; }
            set
            {
                if (Accept != value)
                {
                    if (value)
                        Data |= AccessType.Accept;
                    else
                        Data &= ~AccessType.Accept;
                }
            }
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(GroupId);
            writer.Write((int)Data);
        }

        internal void Read(BinaryReader reader)
        {
            GroupId = reader.ReadInt32();
            Data = (AccessType)reader.ReadInt32();
        }
    }
}
