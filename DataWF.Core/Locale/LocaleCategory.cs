using DataWF.Collections;
using DataWF.Collections.Indexes;
using DataWF.Invokers;
using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF;

[Invoker(Instance = true)]
public sealed partial class LocaleCategory : SelectableList<LocaleItem>, ICloneable, IEntryNotifyPropertyChanged
{
    private string name = "";

    public LocaleCategory()
    {
        Indexes.Add(LocaleItem.NameInvokerInstance.Name,
            new InvokerIndex<LocaleItem, string>(LocaleItem.NameInvoker.Instance, IndexFactory.GetNullKey<string>(), StringComparer.Ordinal));
    }

    [DefaultValue("")]
    public string Name
    {
        get { return name; }
        set
        {
            if (name != value)
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public bool Contains(string name)
    {
        return SelectOne(nameof(LocaleItem.Name), name) != null;
    }

    public LocaleItem GetByName(string name)
    {
        var item = SelectOne(nameof(LocaleItem.Name), name);
        if (item == null)
        {
            item = new LocaleItem(name);
            Add(item);
            Locale.IsChanged = true;
        }
        return item;
    }

    public override string ToString()
    {
        return Name;
    }

    public object Clone()
    {
        var item = new LocaleCategory() { Name = name };
        item.AddRange(items);
        return item;
    }
}

