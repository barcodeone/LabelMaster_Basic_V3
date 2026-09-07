using System.Windows;

namespace BarcodePrint.Services
{
    public interface IDialogService
    {
        void ShowMessage(string message, string title = "알림", MessageBoxImage icon = MessageBoxImage.Information);
        void ShowWarning(string message, string title = "경고");
        void ShowError(string message, string title = "오류");
        bool ShowQuestion(string message, string title = "확인");
        bool ShowOpenFileDialog(string filter, string defaultExt, out string filePath);
        bool ShowSaveFileDialog(string filter, string defaultExt, string defaultFileName, out string filePath);
    }
}
