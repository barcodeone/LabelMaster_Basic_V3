using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using BarcodePrint.Model;

namespace BarcodePrint.WpfView
{
    public partial class PresetSelectDialog : Window
    {
        public PresetLabelItem SelectedPreset { get; private set; }
        private List<PresetLabelItem> _presets;

        public PresetSelectDialog()
        {
            InitializeComponent();
            LoadPresets();
        }

        private void LoadPresets()
        {
            var settings = new AppSettings();
            settings.Load();
            _presets = settings.FavoritePresets ?? AppSettings.GetDefaultPresets();
            if (_presets.Count == 0)
            {
                _presets = AppSettings.GetDefaultPresets();
            }

            LstPresets.ItemsSource = null;
            LstPresets.ItemsSource = _presets;
            if (LstPresets.Items.Count > 0)
            {
                LstPresets.SelectedIndex = 0;
            }
        }

        private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LstPresets.SelectedItem is PresetLabelItem item)
            {
                SelectedPreset = item;
                DialogResult = true;
                Close();
            }
        }

        private void OnConfirmClick(object sender, RoutedEventArgs e)
        {
            if (LstPresets.SelectedItem is PresetLabelItem item)
            {
                SelectedPreset = item;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("시작할 라벨 규격을 목록에서 선택하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnEditSelectedClick(object sender, RoutedEventArgs e)
        {
            if (LstPresets.SelectedItem is PresetLabelItem item)
            {
                int idx = _presets.IndexOf(item);
                var dlg = new PresetEditDialog(item)
                {
                    Owner = this
                };

                if (dlg.ShowDialog() == true && dlg.PresetItem != null)
                {
                    item.Title = dlg.PresetItem.Title;
                    item.WidthMm = dlg.PresetItem.WidthMm;
                    item.HeightMm = dlg.PresetItem.HeightMm;
                    item.Description = dlg.PresetItem.Description;

                    var settings = new AppSettings();
                    settings.Load();
                    settings.FavoritePresets = _presets;
                    settings.Save();

                    LoadPresets();
                    if (idx >= 0 && idx < _presets.Count)
                    {
                        LstPresets.SelectedIndex = idx;
                    }
                }
            }
            else
            {
                MessageBox.Show("수정할 규격을 목록에서 선택하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnManageClick(object sender, RoutedEventArgs e)
        {
            var dlg = new PresetManagerDialog(_presets)
            {
                Owner = this
            };

            if (dlg.ShowDialog() == true && dlg.ResultPresets != null)
            {
                _presets = dlg.ResultPresets;
                var settings = new AppSettings();
                settings.Load();
                settings.FavoritePresets = _presets;
                settings.Save();

                LoadPresets();
            }
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(
                "자주 사용하는 4가지 라벨 규격을 초기 기본값으로 복원하시겠습니까?",
                "기본 규격 복원",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                _presets = AppSettings.GetDefaultPresets();
                var settings = new AppSettings();
                settings.Load();
                settings.FavoritePresets = _presets;
                settings.Save();

                LoadPresets();
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
