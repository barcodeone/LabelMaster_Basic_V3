using System;
using System.Windows;
using BarcodePrint.ViewModel;

namespace BarcodePrint.WpfView
{
    public partial class DesignEditorWindow : Window
    {
        private MainViewModel ViewModel => DataContext as MainViewModel;

        public DesignEditorWindow()
        {
            InitializeComponentSafely();
        }

        private void InitializeComponentSafely()
        {
            try
            {
                var resourceLocater = new Uri("/LabelMaster_Basic_v2;component/wpfview/designeditorwindow.xaml", UriKind.Relative);
                System.Windows.Application.LoadComponent(this, resourceLocater);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"XAML Load Component Exception: {ex.Message}");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.DialogResult = true;
            }
            catch { }
            this.Close();
        }
    }
}
