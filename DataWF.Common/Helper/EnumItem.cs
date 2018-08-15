using System.ComponentModel;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization;

namespace DataWF.Common
{
    public class EnumItem : ICheck, INotifyPropertyChanged
    {
        private bool check;

        public override string ToString()
        {
            if (Name == null)
            {
                var type = Value.GetType();
                var name = Value.ToString();
                var memeberName = type.GetMember(name)?.FirstOrDefault()?.GetCustomAttribute<EnumMemberAttribute>()?.Value;

                Name = Locale.Get(Locale.GetTypeCategory(type), memeberName ?? name);
            }
            return Name;
        }

        public int Index { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }

        public bool Check
        {
            get { return check; }
            set
            {
                if (this.check != value)
                {
                    this.check = value;
                    OnPropertyChanged(nameof(Check));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

}

