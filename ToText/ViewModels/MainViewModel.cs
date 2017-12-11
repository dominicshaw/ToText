using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using log4net;
using Microsoft.Win32;
using ToText.Models;
using ToText.Properties;

namespace ToText.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainViewModel));

        private string _fileLocation;
        private string _text;
        private bool _working;

        public string FileLocation
        {
            get { return _fileLocation; }
            set
            {
                if (value == _fileLocation) return;
                _fileLocation = value;
                OnPropertyChanged();
            }
        }

        public string Text
        {
            get { return _text; }
            set
            {
                if (value == _text) return;
                _text = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowResult));
            }
        }

        public bool ShowResult => !string.IsNullOrEmpty(Text);

        public bool Working
        {
            get { return _working; }
            set
            {
                if (value == _working) return;
                _working = value;
                OnPropertyChanged();
            }
        }

        public ICommand PickFileCommand => new AsyncCommand(PickFile);
        public ICommand DownloadCommand => new DelegateCommand(Download);

        private async Task PickFile()
        {
            Working = true;

            try
            {
                var dialog = new OpenFileDialog() { Multiselect = false, DefaultExt = "*.pdf", Filter = "PDF Documents (.pdf)|*.pdf"};
                var result = dialog.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    FileLocation = dialog.FileName;

                    using (var pdf = new Pdf(FileLocation))
                    {
                        Text = await pdf.GetText();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                MessageBox.Show("Something went wrong :(", "Errorz", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Working = false;
            }
        }

        private void Download()
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetTempFileName(), "txt"));

                File.WriteAllText(path, Text);
                Process.Start(path);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                MessageBox.Show("Something went wrong :(", "Errorz", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}