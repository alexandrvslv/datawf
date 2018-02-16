using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt.Drawing;
using System.Xml.Serialization;

namespace DataWF.Gui
{
    public class Node : IGroup, IContainerNotifyPropertyChanged, IComparable, IGlyph, ICheck
    {       
        protected bool expand;
        protected bool check;
        protected internal int order = -1;
        protected internal string groupName;
        protected internal string categoryName;
        protected string name;
        protected string text;
        protected bool visible = true;
        protected bool complex;       
        private GlyphType glyph = GlyphType.None;
        protected Dictionary<string, object> attributes = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        protected List<Node> _childs = new List<Node>();
        protected internal Node group;
        protected internal Category category;
        protected Image image;

        public Node()
        {

        }

        public Node(string name)
        {
            this.name = name;
        }

        public Node(string name, string header, bool check, object tag)
            : this(name)
        {
            this.text = header;
            this.check = check;
            Tag = tag;
        }

        public object this[string key]
        {
            get
            {
                object value;
                if (!attributes.TryGetValue(key, out value))
                    attributes[key] = null;
                return value;
            }
            set
            {
                attributes[key] = value;
                OnPropertyChanged(key);
            }
        }

        public override string ToString()
        {
            return text ?? name;
        }

        [DefaultValue(true)]
        public bool Visible
        {
            get { return visible; }
            set
            {
                if (visible != value)
                {
                    visible = value;
                    OnPropertyChanged(nameof(Visible));
                }
            }
        }

        public string CategoryName
        {
            get { return categoryName; }
            set
            {
                if (categoryName != value)
                {
                    categoryName = value;
                    OnPropertyChanged(nameof(CategoryName));
                }
            }
        }

        public string GroupName
        {
            get { return groupName; }
            set
            {
                if (groupName != value)
                {
                    groupName = value;
                    OnPropertyChanged(nameof(GroupName));
                }
            }
        }

        public int Order
        {
            get { return order; }
            set
            {
                if (order != value)
                {
                    order = value;
                    OnPropertyChanged(nameof(Order));
                }
            }
        }

        [XmlIgnore]
        public Category Category
        {
            get { return category; }
            set
            {
                if (category == value)
                    return;
                category = value;
                categoryName = category?.Name;
                OnPropertyChanged(nameof(Category));
            }
        }

        [XmlIgnore]
        public List<Node> Childs
        {
            get { return _childs; }
        }

        [DefaultValue(false)]
        public bool Check
        {
            get { return check; }
            set
            {
                check = value;
                foreach (Node node in _childs)
                    node.Check = value;
                OnPropertyChanged(nameof(Check));
            }
        }

        [XmlIgnore]
        public object Tag { get; set; }

        public string Text
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }

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

        public string FullPath
        {
            get
            {
                string rez = string.Empty;
                Node g = this;
                while (g != null)
                {
                    rez = g.Text + " " + rez;
                    g = g.Group;
                }
                return rez;
            }
        }

        #region IGroup implementation
        IGroup IGroup.Group
        {
            get { return Group; }
            set { Group = value as Node; }
        }

        public Node TopGroup
        {
            get { return (Node)GroupHelper.TopGroup(this); }
        }

        [XmlIgnore]
        public Node Group
        {
            get { return group; }
            set
            {
                if (group == value)
                    return;
                if (value == null || (value.Group != this && value != this))
                {
                    if (group != null)
                        group._childs.Remove(this);
                    group = value;
                    groupName = group?.Name;
                    if (group != null)
                    {
                        this.categoryName = group.categoryName;
                        //this.order = _group._childs.Count;
                        group._childs.Add(this);
                    }
                    OnPropertyChanged(nameof(Group));
                }
            }
        }

        public bool IsExpanded
        {
            get { return IsParentActive(this); }
        }

        public static bool IsParentActive(Node item)
        {
            if (!item.visible)
                return false;
            else if (item.Group == null)
                return item.visible;
            else if (!item.Group.expand || !item.Group.visible)
                return false;
            else
                return IsParentActive(item.Group);
        }

        [DefaultValue(false)]
        public virtual bool Expand
        {
            get { return expand; }
            set
            {
                if (expand != value)
                {
                    expand = value;
                    OnPropertyChanged(nameof(Expand));
                }
            }
        }

        [DefaultValue(false)]
        public bool IsCompaund
        {
            get { return complex || _childs.Count > 0; }
            set
            {
                if (complex != value)
                {
                    complex = value;
                    OnPropertyChanged(nameof(IsCompaund));
                }
            }
        }

        [XmlIgnore]
        public Image Image
        {
            get { return image; }
            set
            {
                if (image != value)
                {
                    image = value;
                    OnPropertyChanged(nameof(Image));
                }
            }
        }

        [DefaultValue(GlyphType.None)]
        public GlyphType Glyph
        {
            get { return glyph; }
            set
            {
                if (glyph != value)
                {
                    glyph = value;
                    OnPropertyChanged(nameof(Glyph));
                }
            }
        }

        [XmlIgnore]
        public INotifyListChanged Container { get; set; }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string property)
        {
            var args = new PropertyChangedEventArgs(property);
            PropertyChanged?.Invoke(this, args);
            if (Container != null)
                Container.OnPropertyChanged(this, args);
        }

        public int CompareTo(object obj)
        {
            return order.CompareTo(((Node)obj).order);
        }

        public void Hide()
        {
            Visible = false;
        }

        public void HideItems()
        {
            Node[] items = _childs.ToArray();
            foreach (Node item in items)
                item.Visible = false;
        }
    }
}

