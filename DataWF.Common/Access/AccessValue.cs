﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class AccessValue : IAccessValue, IByteSerializable//, IEnumerable<AccessItem>
    {
        public static IAccessProvider Provider = new AccessProviderStub { Groups = new IdCollection<IGroupIdentity>() };

        public static implicit operator AccessValue(byte[] value)
        {
            return new AccessValue(value);
        }

        public static AccessValue operator &(AccessValue a, AccessValue b)
        {
            AccessValue c = new AccessValue();
            foreach (var aItem in a.Items)
            {
                var bItem = b.Get(aItem.Identity);
                if (bItem != AccessItem.Empty)
                {
                    c.Add(aItem.Identity, aItem.Access & bItem.Access);
                }
            }

            foreach (var bItem in b.Items)
            {
                var aItem = a.Get(bItem.Identity);
                if (aItem != AccessItem.Empty)
                {
                    c.Add(bItem.Identity, aItem.Access & bItem.Access);
                }
            }
            return c;
        }

        public static AccessValue operator |(AccessValue a, AccessValue b)
        {
            AccessValue c = new AccessValue();
            foreach (var aItem in a.Items)
            {
                if (b.items.TryGetValue(aItem.Identity, out var bItem))
                {
                    c.Add(aItem.Identity, aItem.Access | bItem.Access);
                }
                else
                {
                    c.Add(aItem.Identity, aItem.Access);
                }
            }
            foreach (var bItem in b.Items)
            {
                if (!a.items.TryGetValue(bItem.Identity, out var aItem))
                {
                    c.Add(bItem.Identity, bItem.Access);
                }
            }
            return c;
        }

        private readonly Dictionary<IAccessIdentity, AccessItem> items = new Dictionary<IAccessIdentity, AccessItem>(1);
        private IAccessable owner;
        private string ownerName;


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

        [XmlIgnore, JsonIgnore]
        public IAccessable Owner
        {
            get => owner;
            set => owner = value;
        }

        public string OwnerName
        {
            get => ownerName ?? Owner?.AccessorName ?? "Catalog";
            set => ownerName = value;
        }

        public IEnumerable<AccessItem> Items
        {
            get => items.Values;
            set => Add(value);
        }

        private bool IsFailRequired(IUserIdentity user)
        {
            return Owner is IProjectItem projectItem
                && (projectItem.ProjectIdentity?.Required).GetValueOrDefault()
                && !user.Groups.Contains(projectItem.ProjectIdentity);
        }

        public AccessType GetFlags(IUserIdentity user)
        {
            var roles = AccessType.None;
            if (user != null &&
                !IsFailRequired(user))
            {
                foreach (IAccessIdentity identity in user.Groups)
                {
                    var item = Get(identity);
                    roles |= item.Access;
                }
            }
            return roles;
        }

        public bool GetFlag(AccessType type, IUserIdentity user)
        {
            if (!IsFailRequired(user))
            {
                foreach (IAccessIdentity group in user.Groups)
                {
                    var item = Get(group);
                    if ((item.Access & type) == type)
                        return true;
                }
            }
            return false;
        }

        public void Add(IAccessIdentity group, AccessType type)
        {
            Add(new AccessItem(group, type));
        }

        public byte[] Serialize()
        {
            byte[] buffer = null;
            using (var stream = new MemoryStream())
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
            using (var stream = new MemoryStream(buffer))
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
                var isTyped = true;
                if (reader.BaseStream.CanSeek)
                {
                    var itemSize = (reader.BaseStream.Length - 4) / capacity;
                    isTyped = itemSize > 8;
                }
                int index = 0;
                while (index < capacity)
                {
                    var item = AccessItem.Deserialize(reader, isTyped);
                    if (!item.IsEmpty)
                    {
                        items[item.Identity] = item;
                    }
                    index++;
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

        public bool DeleteVirtualGroup()
        {
            var flag = false;
            foreach (var identity in items.Keys.ToList())
            {
                if (identity is IProjectIdentity
                    || identity is ICompanyIdentity)
                {
                    items.Remove(identity);
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
            foreach (IAccessIdentity group in Provider.GetGroups())
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
