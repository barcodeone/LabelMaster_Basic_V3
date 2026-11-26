using System;
using System.Windows;

namespace BarcodePrint.WpfView
{
    public enum EmptyTabsActionChoice
    {
        None,
        NewDocument,
        OpenDocument
    }

    public partial class EmptyTabsActionDialog : Window
    {
        public EmptyTabsActionChoice Choice { get; private set; } = EmptyTabsActionChoice.None;

        public EmptyTabsActionDialog()
        {
            InitializeComponentSafely();
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster;component/wpfview/emptytabsactiondialog.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EmptyTabsActionDialog Load Exception: {ex.Message}");
            }
        }

        private void NewDoc_Click(object sender, RoutedEventArgs e)
        {
            Choice = EmptyTabsActionChoice.NewDocument;
            DialogResult = true;
            Close();
        }

        private void OpenDoc_Click(object sender, RoutedEventArgs e)
        {
            Choice = EmptyTabsActionChoice.OpenDocument;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Choice = EmptyTabsActionChoice.None;
            DialogResult = false;
            Close();
        }
    }
}
