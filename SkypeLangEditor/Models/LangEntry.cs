using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SkypeLangEditor.Models
{
    public class LangEntry : INotifyPropertyChanged
    {
        private string _key = string.Empty;
        private string _value = string.Empty;

        public LangEntry()
        {
        }

        public LangEntry(string key, string value)
        {
            _key = key ?? string.Empty;
            _value = value ?? string.Empty;
        }

        public string Key
        {
            get => _key;
            set
            {
                if (_key != value)
                {
                    _key = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public LangEntry Clone()
        {
            return new LangEntry(_key, _value);
        }
    }
}
