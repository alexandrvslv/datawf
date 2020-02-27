using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataWF.Common
{
    public class AccessValue : IAccessValue//, IEnumerable<AccessItem>
    {
        public static IIdCollection<IGroupIdentity> Groups = new IdCollection<IGroupIdentity>();
        public static IIdCollection<IUserIdentity> Users = new IdCollection<IUserIdentity>();

        public static implicit operator AccessValue(byte[] value)
        {
            return new AccessValue(value);
        }

        private readonly Dictionary<IAccessIdentity, AccessItem> items = new Dictionary<IAccessIdentity, AccessItem>(1);

        public AccessValue()
        { }

        public AccessValue(IEnumerable<IAccessIdentity> identities, AccessType access = AccessType.Read)
        {
            foreach (var identity in identities)
            {
                if (identity != null)
                {
                    Add(new AccessItem(identity, access));
                }
            }
        }

        public AccessValue(IEnumerable<AccessItem> items)
        {
            foreach (var item in items)
            {
                this.items[item.Identity] = item;
            }
        }

        public AccessValue(byte[] buffer)
        {
            if (buffer != null)
            {
                Deserialize(buffer);
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
                foreach (IAccessIdentity identity in user.Groups)
                {
                    var item = Get(identity);
                    data |= item.Access;
                }
            }
            return data;
        }

        public bool GetFlag(AccessType type, IUserIdentity user)
        {
            foreach (IAccessIdentity group in user.Groups)
            {
                var item = Get(group);
                if ((item.Access & type) == type)
                    return true;
            }
            return false;
        }

        public void Add(IAccessIdentity group, AccessType type)
        {
            Add(new AccessItem(group, type));
        }

        public byte[] Write()
        {
            byte[] buffer = null;
            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream))
            {
                Serialize(writer);
                buffer = stream.ToArray();
            }
            return buffer;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(items.Values.Where(p => !p.IsEmpty).Count());

            foreach (AccessItem item in items.Values.Where(p => !p.IsEmpty))
            {
                item.Serialize(writer);
            }
        }

        public void Deserialize(byte[] buffer)
        {
            var stream = new MemoryStream(buffer);
            using (var reader = new BinaryReader(stream))
            {
                Deserialize(reader);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            items.Clear();
            var capacity = reader.ReadInt32();
            if (capacity > 0)
            {
                var itemSize = (reader.BaseStream.Length - 4) / capacity;
                var IsUser = itemSize > 8;
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    var item = AccessItem.Deserialize(reader, IsUser);
                    if (!item.IsEmpty)
                    {
                        items[item.Identity] = item;
                    }
                }
            }
        }

        public bool DeleteGroupDublicat()
        {
            var flag = false;
            foreach (var item in items.Values.ToList())
            {
                if (item.Identity is IGroup group && group.Group != null
                    && items.TryGetValue((IAccessIdentity)group.Group, out var value)
                    && (value.Access & item.Access) == item.Access)
                {
                    items.Remove(item.Identity);
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
                    items.Remove(item.Identity);
                    flag = true;
                }
            }
            return flag;
        }

        public AccessItem Get(IAccessIdentity identity, bool hierarchy = true)
        {
            var item = AccessItem.Empty;
            while (identity != null && !items.TryGetValue(identity, out item) && hierarchy)
            {
                identity = identity is IGroup groupped ? (IAccessIdentity)groupped.Group : null;
            }
            return item;
        }

        public AccessItem GetOrAdd(IAccessIdentity identity)
        {
            if (items.TryGetValue(identity, out var item))
            {
                return item;
            }
            var newItem = new AccessItem(identity);
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
            items[item.Identity] = item;
        }

        public IEnumerable<IAccessIdentity> GetGroups(AccessType type)
        {
            foreach (var item in items.Values)
            {
                if ((type & item.Access) == type)
                    yield return item.Identity;
            }
        }

        public override string ToString()
        {
            return string.Join(";", items.Select(p => p.ToString()));
        }

        public void Fill()
        {
            foreach (IAccessIdentity group in Groups)
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
                if (!items.TryGetValue(item.Identity, out var thisItem) || thisItem.Access != item.Access)
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
