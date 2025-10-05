using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using CleanBin;
using Desktop.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly ICleanerService _cleanerService;
        private string _pathFolder = string.Empty;
        private string _description = string.Empty;

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


        /// <summary>
        /// Конструктор главного окна
        /// </summary>
        /// <param name="cleanerService">Сервис очистки папок</param>
        public MainWindow(ICleanerService cleanerService)
        {
            _cleanerService = cleanerService ?? throw new ArgumentNullException(nameof(cleanerService));
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

                var directories = _cleanerService.CleanFolder(_pathFolder, false, null, null);
                
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