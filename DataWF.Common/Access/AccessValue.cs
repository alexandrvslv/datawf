using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataWF.Common
{
    public class AccessValue : IAccessValue
    {
        public static IEnumerable<IAccessGroup> Groups = new List<IAccessGroup>();

        public static implicit operator AccessValue(byte[] value)
        {
            return new AccessValue(value);
        }

        public List<AccessItem> Items = new List<AccessItem>(1);

        public AccessValue()
        {
            foreach (IAccessGroup group in Groups)
            {
                if (group != null)
                {
                    Add(new AccessItem(group, AccessType.Read));
                }
            }
        }

        public AccessValue(IEnumerable<AccessItem> items)
        {
            Items.AddRange(items);
        }

        public AccessValue(byte[] buffer)
        {
            if (buffer != null)
            {
                Read(buffer);
            }
        }

        public AccessType GetFlags(IUserIdentity user)
        {
            var data = AccessType.None;
            foreach (AccessItem item in Items)
            {
                if (item.Group?.IsCurrentUser(user) ?? false)
                {
                    data |= item.Access;
                }
            }
            return data;
        }


        public bool GetFlag(AccessType type, IUserIdentity user)
        {
            foreach (AccessItem item in Items)
            {
                if (item.Group?.IsCurrentUser(user) ?? false)
                {
                    if ((item.Access & type) == type)
                        return true;
                }
            }
            return false;
        }

        public void SetFlag(IAccessGroup group, AccessType type)
        {
            Add(new AccessItem(group, type));
        }

        public byte[] Write()
        {
            byte[] buffer = null;
            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream))
            {
                Write(writer);
                buffer = stream.ToArray();
            }
            return buffer;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(Items.Where(p => !p.IsEmpty).Distinct().Count());

            foreach (AccessItem item in Items.Where(p => !p.IsEmpty).Distinct())
            {
                item.BinaryWrite(writer);
            }
        }

        public void Read(byte[] buffer)
        {
            var stream = new MemoryStream(buffer);
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
                for (int i = 0; i < Items.Capacity; i++)
                    if (reader.BaseStream.Position < (reader.BaseStream.Length))
                    {
                        var item = new AccessItem();
                        item.BinaryRead(reader);
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
            {
                if (item.Group == group)
                    return item;
            }
            var newItem = new AccessItem(group);
            Add(newItem);
            return newItem;
        }

        public void Add(IEnumerable<AccessItem> accessItems)
        {
            foreach (var item in accessItems)
            {
                Add(item);
            }
        }

        public void Add(AccessItem item)
        {
            int index = GetIndex(item.Group);
            if (index < 0)
            {
                Items.Add(item);
            }
            else
            {
                Items[index] = item;
            }
        }

        public IEnumerable<IAccessGroup> GetGroups(AccessType type)
        {
            foreach (var item in Items)
            {
                if ((type & item.Access) == type)
                    yield return item.Group;
            }
        }

        public override string ToString()
        {
            return string.Join(";", Items.Select(p => p.ToString()));
        }

        public void Fill()
        {
            foreach (IAccessGroup group in Groups)
            {
                Get(group);
            }
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
