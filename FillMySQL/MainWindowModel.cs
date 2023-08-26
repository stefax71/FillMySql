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
        public bool CanBrowseToNextRecord { get; set; }
        public bool CanBrowseToPreviousRecord { get; set; }
        
        public int CurrentQueryIndex
        {
            get => _currentQueryIndex;
            set
            {
                if (value < 0) _currentQueryIndex = 0;
                else if (value > SqlProcessor.NumberOfQueries) _currentQueryIndex = SqlProcessor.NumberOfQueries;
                else _currentQueryIndex = value;
                CanBrowseToNextRecord = (SqlProcessor.NumberOfQueries > 0 && _currentQueryIndex < SqlProcessor.NumberOfQueries);
                CanBrowseToPreviousRecord = (SqlProcessor.NumberOfQueries > 0 && _currentQueryIndex > 1);
                OnPropertyChanged(nameof(CanBrowseToNextRecord));
                OnPropertyChanged(nameof(CanBrowseToPreviousRecord));
            }
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

