﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class AccessValue : IAccessValue
    {
        public static IEnumerable<IAccessGroup> Groups = new List<IAccessGroup>();

        public static implicit operator AccessValue(byte[] value)
        {
            return new AccessValue(value);
        }

        [JsonIgnore, XmlIgnore]
        public List<AccessItem> Items = new List<AccessItem>(1);

        public AccessValue()
        {
            foreach (IAccessGroup group in Groups)
            {
                if (group != null)
                    Add(new AccessItem(group, AccessType.View));
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
                        item.Read(reader);
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
            return new AccessItem(group);
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
                if ((type & item.Data) == type)
                    yield return item.Group;
            }
        }

        public override string ToString()
        {
            return string.Format("<{0}>{1}{2}{3}{4}{5}{6}",
                Items.Count,
                View ? " View" : string.Empty,
                Edit ? " Edit" : string.Empty,
                Create ? " Create" : string.Empty,
                Delete ? " Delete" : string.Empty,
                Admin ? " Admin" : string.Empty,
                Accept ? " Accept" : string.Empty);
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

    public class AccessValueJson : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(AccessValue);
        }

        public override bool CanRead { get { return false; } }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is AccessValue typeValue))
            {
                throw new JsonSerializationException($"Expected {nameof(AccessValue)} but {nameof(value)} is {value?.GetType().Name}.");
            }
            writer.WriteValue(value.ToString());
        }
    }
}
