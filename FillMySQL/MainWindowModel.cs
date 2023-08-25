using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Documents;

namespace FillMySQL
{
    public class MainWindowModel: INotifyPropertyChanged
    {
        public readonly SqlProcessor SqlProcessor;
        public TextRange CurrentQueryRange = null;
        public int _currentQueryIndex = -1;


        public int CurrentQueryIndex
        {
            get => _currentQueryIndex;
            set => _currentQueryIndex = value;
        }

        public MainWindowModel()
        {
            SqlProcessor = new SqlProcessor();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public SqlProcessor Processor => SqlProcessor;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }    
}

