using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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
        private string _pathFolder = string.Empty;
        private string _description;

        public event PropertyChangedEventHandler? PropertyChanged;

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
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    PathFolder = dialog.SelectedPath;
                }
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void StartClean(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_pathFolder))
            {
                InfoBox.AppendText("Ошибка: Не выбран путь для очистки!\n");
                return;
            }

            try
            {
                InfoBox.Clear();
                InfoBox.AppendText($"Начинаем очистку папки: {_pathFolder}\n");
                InfoBox.AppendText("=====================================\n");

                var cleanerService = new CleanerService();
                var directories = cleanerService.CleanFolder(_pathFolder, false, null, null);
                
                int processedCount = 0;
                foreach (var directory in directories)
                {
                    InfoBox.AppendText($"Обработано: {directory}\n");
                    InfoBox.ScrollToEnd();
                    processedCount++;
                }
                
                InfoBox.AppendText($"\nОчистка завершена! Обработано папок: {processedCount}\n");
            }
            catch (Exception ex)
            {
                InfoBox.AppendText($"\nОшибка при очистке: {ex.Message}\n");
            }
        }
    }
}