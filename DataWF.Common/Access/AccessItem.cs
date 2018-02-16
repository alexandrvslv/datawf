using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    //public class AccessItemEventArg : StringEventArg
    //{
    //    public AccessItem Item;
    //}

    public struct AccessItem
    {
        public readonly static AccessItem Empty = new AccessItem();
        //public static event EventHandler<AccessItemEventArg> GetGroup;
        public static IEnumerable<IAccessGroup> Groups;
        public static bool Default = true;

        private IAccessGroup group;
        private AccessType data;

        public override string ToString()
        {
            return $"{group.Name}({data})";
        }

        public IAccessGroup Group
        {
            get { return group; }
            set { group = value; }
        }

        public AccessType Data { get { return data; } }

        public bool IsEmpty
        {
            get { return data == 0; }
        }

        public bool View
        {
            get { return (data & AccessType.View) == AccessType.View; }
            set
            {
                if (View != value)
                {
                    if (value)
                        data |= AccessType.View;
                    else
                        data &= ~AccessType.View;
                }
            }
        }

        public bool Create
        {
            get { return (data & AccessType.Create) == AccessType.Create; }
            set
            {
                if (Create != value)
                {
                    if (value)
                        data |= AccessType.Create;
                    else
                        data &= ~AccessType.Create;
                }
            }
        }

        public bool Edit
        {
            get { return (data & AccessType.Edit) == AccessType.Edit; }
            set
            {
                if (Edit != value)
                {
                    if (value)
                        data |= AccessType.Edit;
                    else
                        data &= ~AccessType.Edit;
                }
            }
        }

        public bool Delete
        {
            get { return (data & AccessType.Delete) == AccessType.Delete; }
            set
            {
                if (Delete != value)
                {
                    if (value)
                        data |= AccessType.Delete;
                    else
                        data &= ~AccessType.Delete;
                }
            }
        }

        public bool Admin
        {
            get { return (data & AccessType.Admin) == AccessType.Admin; }
            set
            {
                if (Admin != value)
                {
                    if (value)
                        data |= AccessType.Admin;
                    else
                        data &= ~AccessType.Admin;
                }
            }
        }

        public bool Accept
        {
            get { return (data & AccessType.Accept) == AccessType.Accept; }
            set
            {
                if (Accept != value)
                {
                    if (value)
                        data |= AccessType.Accept;
                    else
                        data &= ~AccessType.Accept;
                }
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(group.Id);
            writer.Write((int)data);
        }

        public void Read(BinaryReader readre, int size)
        {
            var groupId = readre.ReadInt32();
            if (Groups != null)
            {
                foreach (IAccessGroup item in Groups)
                {
                    if (item.Id == groupId)
                    {
                        group = item;
                        break;
                    }
                }
            }
            //if (GetGroup != null)
            //{
            //    var arg = new AccessItemEventArg() { Item = this };
            //    GetGroup(this, arg);
            //}
            if (size == 9)
            {
                View = readre.ReadBoolean();
                Create = readre.ReadBoolean();
                Edit = readre.ReadBoolean();
                Delete = readre.ReadBoolean();
                Admin = readre.ReadBoolean();
            }
            else
            {
                data = (AccessType)readre.ReadInt32();
            }
        }
    }
}
