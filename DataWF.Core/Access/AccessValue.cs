using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using DataWF.Invokers;
using DataWF.Serialization;

namespace DataWF.Access;

[Invoker]
public partial class AccessValue : IAccessValue, IBinarySerializable//, IEnumerable<AccessItem>
{

    public static readonly AccessValue Empty = new AccessValue((Dictionary<IdentityHash, AccessItem>)null!);
    //public static implicit operator AccessValue(byte[] value)
    //{
    //    return new AccessValue(value);
    //}

    public static AccessValue operator &(AccessValue a, AccessValue b)
    {
        var c = new AccessValue();
        foreach (var aItem in a.Items)
        {
            var bItem = b.Get(aItem.IdentityHash);
            if (bItem != AccessItem.Empty)
            {
                c.Add(aItem.IdentityHash, aItem.Access & bItem.Access);
            }
        }

        foreach (var bItem in b.Items)
        {
            var aItem = a.Get(bItem.IdentityHash);
            if (aItem != AccessItem.Empty)
            {
                c.Add(bItem.IdentityHash, aItem.Access & bItem.Access);
            }
        }
        return c;
    }

    public static AccessValue operator |(AccessValue a, AccessValue b)
    {
        var c = new AccessValue();
        foreach (var aItem in a.Items)
        {
            if (b.items.TryGetValue(aItem.IdentityHash, out var bItem))
            {
                c.Add(aItem.IdentityHash, aItem.Access | bItem.Access);
            }
            else
            {
                c.Add(aItem.IdentityHash, aItem.Access);
            }
        }
        foreach (var bItem in b.Items)
        {
            if (!a.items.TryGetValue(bItem.IdentityHash, out _))
            {
                c.Add(bItem.IdentityHash, bItem.Access);
            }
        }
        return c;
    }

    private readonly Dictionary<IdentityHash, AccessItem> items;
    private IAccessable? owner;
    private string? ownerName;
    public AccessValue()
    {
        items = new(1);
    }
    private AccessValue(Dictionary<IdentityHash, AccessItem> items)
    {
        this.items = items;
    }

    public AccessValue(IEnumerable<IAccessIdentity> identities, AccessType access = AccessType.Read)
    {
        items = new(1);
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
        this.items = new(1);
        foreach (var item in items)
        {
            this.items[item.IdentityHash] = item;
        }
    }

    public AccessValue(byte[] buffer)
    {
        items = new(1);
        if (buffer != null)
        {
            Deserialize(buffer);
        }
    }

    [XmlIgnore, JsonIgnore]
    public IAccessable? Owner
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

    public IIdentity? Identity => throw new NotImplementedException();

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

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        Serialize(writer);
        return stream.ToArray();
    }

    public void Serialize(BinaryWriter writer)
    {
        writer.Write(items.Values.Where(p => !p.IsEmpty).Count());

        foreach (AccessItem item in items.Values.Where(p => !p.IsEmpty))
        {
            item.Serialize(writer);
        }
    }

    public static T Deserialize<T>(byte[] buffer) where T : IBinarySerializable
    {
        using var stream = new MemoryStream(buffer);
        using var reader = new BinaryReader(stream);
        return Deserialize<T>(reader);
    }

    public static T Deserialize<T>(BinaryReader reader) where T : IBinarySerializable
    {
        var items = new Dictionary<IdentityHash, AccessItem>();
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
                    items[item.IdentityHash] = item;
                }
                index++;
            }
        }
        return (T)new AccessValue(items);
    }

    public bool DeleteGroupDublicat(IAccessProvider provider)
    {
        var flag = false;
        foreach (var item in items.Values.ToList())
        {
            if (item.GetIdentity(provider) is IGroup group && group.Group is IAccessIdentity topGroup
                && items.TryGetValue(topGroup.IdentityHash, out var value)
                && (value.Access & item.Access) == item.Access)
            {
                items.Remove(item.IdentityHash);
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
                items.Remove(item.IdentityHash);
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

    public AccessItem Get(IAccessIdentity? identity, bool hierarchy = true)
    {
        var item = AccessItem.Empty;
        while (identity != null && !items.TryGetValue(identity.IdentityHash, out item) && hierarchy)
        {
            identity = identity is IGroup groupped ? (IAccessIdentity?)groupped.Group : null;
        }
        return item;
    }

    public AccessItem Get(IdentityHash identityHash)
    {
        return items.TryGetValue(identityHash, out var item) ? item : AccessItem.Empty;
    }

    public AccessItem GetOrAdd(IAccessIdentity identity)
    {
        if (items.TryGetValue(identity.IdentityHash, out var item))
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
        items[item.IdentityHash] = item;
    }

    public void Add(IdentityHash identityHash, AccessType type)
    {
        Add(new AccessItem(identityHash, type));
    }

    public void Add(IAccessIdentity identity, AccessType type)
    {
        Add(new AccessItem(identity, type));
    }

    public IEnumerable<IAccessIdentity> GetGroups(AccessType type, IAccessProvider provider)
    {
        foreach (var item in items.Values)
        {
            if ((type & item.Access) == type
            && item.GetIdentity(provider) is { } identity)
                yield return identity;
        }
    }

    public override string ToString()
    {
        return string.Join(";", items.Select(p => p.ToString()));
    }

    public void Fill(IAccessProvider provider)
    {
        foreach (IAccessIdentity group in provider.GetGroups())
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
            if (!items.TryGetValue(item.IdentityHash, out var thisItem) || thisItem.Access != item.Access)
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
