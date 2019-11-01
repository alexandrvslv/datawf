using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataWF.Common
{
    public class AccessValue : IAccessValue//, IEnumerable<AccessItem>
    {
        public static IEnumerable<IAccessGroup> Groups = new List<IAccessGroup>();

        public static implicit operator AccessValue(byte[] value)
        {
            return new AccessValue(value);
        }

        private readonly Dictionary<IAccessGroup, AccessItem> items = new Dictionary<IAccessGroup, AccessItem>(1);

        public AccessValue()
        { }

        public AccessValue(IEnumerable<IAccessGroup> groups, AccessType access = AccessType.Read)
        {
            foreach (IAccessGroup group in groups)
            {
                if (group != null)
                {
                    Add(new AccessItem(group, access));
                }
            }
        }

        public AccessValue(IEnumerable<AccessItem> items)
        {
            foreach (var item in items)
            {
                this.items[item.Group] = item;
            }
        }

        public AccessValue(byte[] buffer)
        {
            if (buffer != null)
            {
                Read(buffer);
            }
        }

        public IEnumerable<AccessItem> Items
        {
            get => items.Values;
            set => Add(value);
        }

        public AccessType GetFlags(IUserIdentity user)
        {
            var data = AccessType.None;
            if (user != null)
            {
                foreach (IAccessGroup group in user.Groups)
                {
                    var item = Get(group);
                    data |= item.Access;
                }
            }
            return data;
        }

        public bool GetFlag(AccessType type, IUserIdentity user)
        {
            foreach (IAccessGroup group in user.Groups)
            {
                var item = Get(group);
                if ((item.Access & type) == type)
                    return true;
            }
            return false;
        }

        public void Add(IAccessGroup group, AccessType type)
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
            writer.Write(items.Values.Where(p => !p.IsEmpty).Count());

            foreach (AccessItem item in items.Values.Where(p => !p.IsEmpty))
            {
                item.BinaryWrite(writer);
            }
        }

        public void Read(byte[] buffer)
        {
            var stream = new MemoryStream(buffer);
            using (var reader = new BinaryReader(stream))
            {
                Read(reader);
            }
        }

        public void Read(BinaryReader reader)
        {
            items.Clear();
            var capacity = reader.ReadInt32();
            if (capacity > 0)
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var item = new AccessItem();
                    item.BinaryRead(reader);
                    if (!item.IsEmpty)
                    {
                        items[item.Group] = item;
                    }
                }
            }
        }

        public bool DeleteGroupDublicat()
        {
            var flag = false;
            foreach (var item in items.Values.ToList())
            {
                if (item.Group?.Group != null
                    && items.TryGetValue((IAccessGroup)item.Group.Group, out var value)
                    && (value.Access & item.Access) == item.Access)
                {
                    items.Remove(item.Group);
                    flag = true;
                }
            }
            return flag;
        }

        public bool DeleteNoneAccess()
        {
            var flag = false;
            foreach (var item in items.Values.ToList())
            {
                if (item.Access == AccessType.None)
                {
                    items.Remove(item.Group);
                    flag = true;
                }
            }
            return flag;
        }

        public AccessItem Get(IAccessGroup group, bool hierarchy = true)
        {
            var item = AccessItem.Empty;
            while (group != null && !items.TryGetValue(group, out item))
            {
                if (!hierarchy)
                    break;
                group = (IAccessGroup)group.Group;
            }
            return item;
        }

        public AccessItem GetOrAdd(IAccessGroup group)
        {
            if (items.TryGetValue(group, out var item))
            {
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
            items[item.Group] = item;
        }

        public IEnumerable<IAccessGroup> GetGroups(AccessType type)
        {
            foreach (var item in items.Values)
            {
                if ((type & item.Access) == type)
                    yield return item.Group;
            }
        }

        public override string ToString()
        {
            return string.Join(";", items.Select(p => p.ToString()));
        }

        public void Fill()
        {
            foreach (IAccessGroup group in Groups)
            {
                GetOrAdd(group);
            }
        }

        public AccessValue Clone()
        {
            var cache = new AccessValue();
            foreach (var item in items.Values)
                cache.Add(item);
            return cache;
        }

        public bool IsEqual(AccessValue accessValue)
        {
            if (items.Count != accessValue.items.Count)
                return false;
            foreach (var item in accessValue.Items)
            {
                if (!items.TryGetValue(item.Group, out var thisItem) || thisItem.Access != item.Access)
                {
                    return false;
                }
            }
            return true;
        }

        public IEnumerator<AccessItem> GetEnumerator()
        {
            return items.Values.GetEnumerator();
        }

    }
}
