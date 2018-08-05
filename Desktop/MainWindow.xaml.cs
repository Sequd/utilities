using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using CleanBin;
using Desktop.Annotations;

namespace Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private string _pathFolder = String.Empty;
        private string _description;

        public string PathFolder
        {
            get => _pathFolder;
            set
            {
                _pathFolder = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                OnPropertyChanged();
                _description = value;
            }
        }


        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenDialog(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    PathFolder = dialog.SelectedPath;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void StartClean(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_pathFolder))
                return;
            
            CleanerService cleanerService = new CleanerService();
            var directories = cleanerService.CleanFolder(PathFolder, false, null, null);
            foreach (var directory in directories)
            {
                InfoBox.AppendText(directory);
                InfoBox.AppendText("\n");
                InfoBox.ScrollToEnd();
            }
        }
    }
}
