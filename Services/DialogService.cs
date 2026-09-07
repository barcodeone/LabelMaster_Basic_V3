using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace BarcodePrint.Services
{
    public class DialogService : IDialogService
    {
        private Window GetOwnerWindow()
        {
            try
            {
                if (Application.Current != null)
                {
                    return Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w.IsVisible)
                           ?? (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible ? Application.Current.MainWindow : null);
                }
            }
            catch { }
            return null;
        }

        public void ShowMessage(string message, string title = "알림", MessageBoxImage icon = MessageBoxImage.Information)
        {
            var owner = GetOwnerWindow();
            if (owner != null)
                MessageBox.Show(owner, message, title, MessageBoxButton.OK, icon);
            else
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        public void ShowWarning(string message, string title = "경고")
        {
            var owner = GetOwnerWindow();
            if (owner != null)
                MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            else
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowError(string message, string title = "오류")
        {
            var owner = GetOwnerWindow();
            if (owner != null)
                MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            else
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool ShowQuestion(string message, string title = "확인")
        {
            var owner = GetOwnerWindow();
            var result = owner != null 
                ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public bool ShowOpenFileDialog(string filter, string defaultExt, out string filePath)
        {
            var ofd = new OpenFileDialog
            {
                Filter = filter,
                DefaultExt = defaultExt
            };

            if (ofd.ShowDialog() == true)
            {
                filePath = ofd.FileName;
                return true;
            }

            filePath = null;
            return false;
        }

        public bool ShowSaveFileDialog(string filter, string defaultExt, string defaultFileName, out string filePath)
        {
            var sfd = new SaveFileDialog
            {
                Filter = filter,
                DefaultExt = defaultExt,
                FileName = defaultFileName
            };

            if (sfd.ShowDialog() == true)
            {
                filePath = sfd.FileName;
                return true;
            }

            filePath = null;
            return false;
        }
    }
}
