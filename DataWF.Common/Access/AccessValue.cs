using System;
using System.Collections.Generic;
using System.IO;

namespace DataWF.Common
{
    public class AccessValue
    {
        public List<AccessItem> Items = new List<AccessItem>(1);

        public AccessValue()
        { }

        public AccessValue(IEnumerable<AccessItem> items)
        {
            Items.AddRange(items);
        }

        public AccessValue(byte[] buffer)
        {
            Read(buffer);
        }

        public bool View
        {
            get { return GetFlag(AccessType.View); }
        }

        public bool Edit
        {
            get { return GetFlag(AccessType.Edit); }
        }

        public bool Create
        {
            get { return GetFlag(AccessType.Create); }
        }

        public bool Delete
        {
            get { return GetFlag(AccessType.Delete); }
        }

        public bool Admin
        {
            get { return GetFlag(AccessType.Admin); }
        }

        public bool Accept
        {
            get { return GetFlag(AccessType.Accept); }
        }

        public bool GetFlag(AccessType type)
        {
            foreach (AccessItem item in Items)
            {
                if (item.Group != null && item.Group.IsCurrent)
                {
                    if ((item.Data & type) == type)
                        return true;
                }
            }
            return AccessItem.Default;
        }

        public byte[] Write()
        {
            byte[] buffer = null;
            using (MemoryStream stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Write(writer);
                buffer = stream.ToArray();
            }
            return buffer;
        }

        public void Write(BinaryWriter writer)
        {
            int c = 0;
            foreach (AccessItem item in Items)
                if (!item.IsEmpty)
                    c++;
            writer.Write(c);

            foreach (AccessItem item in Items)
                if (!item.IsEmpty)
                    item.Write(writer);
        }

        public void Read(byte[] buffer)
        {
            if (buffer != null)
                using (var stream = new MemoryStream(buffer))
                using (var reader = new BinaryReader(stream))
                {
                    Read(reader, buffer);
                }
        }

        public void Read(BinaryReader reader, byte[] buffer)
        {
            Items.Clear();
            Items.Capacity = reader.ReadInt32();
            if (Items.Capacity > 0)
            {
                int size = (buffer.Length - 4) / Items.Capacity;
                for (int i = 0; i < Items.Capacity; i++)
                    if (reader.BaseStream.Position < (reader.BaseStream.Length))
                    {
                        AccessItem item = new AccessItem();
                        item.Read(reader, size);
                        if (item.Group != null)
                            Items.Add(item);
                    }
            }
        }

        public int GetIndex(IAccessGroup group)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Group == group)
                    return i;
            }
            return -1;
        }

        public AccessItem Get(IAccessGroup group)
        {
            foreach (var item in Items)
                if (item.Group == group)
                    return item;
            var n = new AccessItem { Group = group };
            Items.Add(n);
            return n;
        }

        public void Add(AccessItem item)
        {
            int index = GetIndex(item.Group);
            if (index < 0)
                Items.Add(item);
            else
                Items[index] = item;
        }

        public List<IAccessGroup> GetGroups(AccessType type)
        {
            var groups = new List<IAccessGroup>();
            foreach (var item in Items)
                if (type == AccessType.View && item.View)
                    groups.Add(item.Group);
                else if (type == AccessType.Edit && item.Edit)
                    groups.Add(item.Group);
                else if (type == AccessType.Create && item.Create)
                    groups.Add(item.Group);
                else if (type == AccessType.Delete && item.Delete)
                    groups.Add(item.Group);
                else if (type == AccessType.Admin && item.Admin)
                    groups.Add(item.Group);
                else if (type == AccessType.Accept && item.Accept)
                    groups.Add(item.Group);
            return groups;
        }

        public bool GetCreate(IAccessGroup group)
        {
            foreach (AccessItem item in Items)
                if (item.Group == group)
                    return item.Create;

            return false;
        }

        public override string ToString()
        {
            return string.Format("<{0}>{1}{2}{3}{4}{5}{6}",
                Items.Count,
                View ? "View " : string.Empty,
                Edit ? "Edit " : string.Empty,
                Create ? "Create " : string.Empty,
                Delete ? "Delete " : string.Empty,
                Admin ? "Admin " : string.Empty,
                Accept ? "Accept " : string.Empty);
        }

        public void Fill()
        {
            if (AccessItem.Groups != null)
                foreach (IAccessGroup group in AccessItem.Groups)
                    Get(group);
        }

        public AccessValue Clone()
        {
            var cache = new AccessValue();
            foreach (var item in Items)
                cache.Add(item);
            return cache;
        }

        public bool IsEqual(AccessValue accessCache)
        {
            throw new NotImplementedException();
        }
    }
}
