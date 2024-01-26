using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace WRLang.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly ObservableCollection<Translation> _translations = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private ICollectionView? _translationsView;
        public ICollectionView TranslationsView
        {
            get => _translationsView ??= new CollectionViewSource()
            {
                Source = _translations
            }.View;
        }

        private string? _currentFile;
        public string? CurrentFile
        {
            get => _currentFile;
            set
            {
                _currentFile = value;
                PropertyChanged?.Invoke(this, new(nameof(CurrentFile)));
            }
        }

        public string? Filter
        {
            set
            {
                TranslationsView.Filter = GetFilter(value!);
            }
        }

        private void OpenCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var fileBrowser = new OpenFileDialog()
            {
                Filter = ".btf file|*.btf"
            };

            if (fileBrowser.ShowDialog() == true)
            {
                var mergeResult = _translations.Count > 0 ? MessageBox.Show(
                    owner: this,
                    messageBoxText: "Merge with the existing entries?",
                    caption: "Merge",
                    button: MessageBoxButton.YesNoCancel,
                    icon: MessageBoxImage.Question
                ) : MessageBoxResult.No;

                if (mergeResult != MessageBoxResult.Cancel)
                {
                    Open(fileBrowser.FileName, mergeResult == MessageBoxResult.Yes);
                }
            }
        }

        private void SaveCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Save(CurrentFile!);
        }

        private void SaveAsCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog()
            {
                Filter = ".btf file|*.btf"
            };

            if (saveDialog.ShowDialog() == true)
            {
                Save(saveDialog.FileName);
            }
        }

        private void Save_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrEmpty(CurrentFile);
        }

        private void Open(string path, bool merge = true)
        {
            Translation[] translations = [];

            try
            {
                if (merge)
                {
                    translations = BTF.LoadBtf(path).UnionBy(_translations, t => t.Id).ToArray();
                }
                else
                {
                    translations = BTF.LoadBtf(path);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            _translations.Clear();
            _translations.AddRange(translations);            

            CurrentFile = path;
        }

        private void Save(string path)
        {
            try
            {
                BTF.SaveBtf(path, _translations.ToArray());
                CurrentFile = path;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private Predicate<object> GetFilter(string filter)
        {
            return (obj) =>
            {
                var translation = (obj as Translation)!;

                return translation.Text.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
                       (uint.TryParse(filter, out uint filterId) && filterId == translation.Id);
            };
        }
    }
}