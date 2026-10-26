using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using BarcodePrint.Model;

namespace BarcodePrint.WpfView
{
    public class PresetItemViewModel : INotifyPropertyChanged
    {
        private int _indexNumber;
        private PresetLabelItem _item;

        public event PropertyChangedEventHandler PropertyChanged;

        public PresetLabelItem Item
        {
            get => _item;
            set
            {
                _item = value;
                OnPropertyChanged(nameof(Item));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(DisplaySize));
            }
        }

        public int IndexNumber
        {
            get => _indexNumber;
            set
            {
                if (_indexNumber != value)
                {
                    _indexNumber = value;
                    OnPropertyChanged(nameof(IndexNumber));
                }
            }
        }

        public string Title => _item?.Title ?? "";
        public string Description => _item?.Description ?? "";
        public string DisplaySize => _item?.DisplaySize ?? "";

        public PresetItemViewModel(int index, PresetLabelItem item)
        {
            _indexNumber = index;
            _item = item;
        }

        public void NotifyAllChanged()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(DisplaySize));
        }

        protected void OnPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public partial class PresetManagerDialog : Window
    {
        private readonly ObservableCollection<PresetItemViewModel> _items = new ObservableCollection<PresetItemViewModel>();

        public List<PresetLabelItem> ResultPresets { get; private set; }

        public PresetManagerDialog(List<PresetLabelItem> presets)
        {
            InitializeComponent();

            var sourceList = presets != null && presets.Count > 0 ? presets : AppSettings.GetDefaultPresets();
            int idx = 1;
            foreach (var p in sourceList)
            {
                _items.Add(new PresetItemViewModel(idx++, p.Clone()));
            }

            LstPresets.ItemsSource = _items;
            if (_items.Count > 0)
            {
                LstPresets.SelectedIndex = 0;
            }
        }

        private void ReassignIndexNumbers()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].IndexNumber = i + 1;
            }
        }

        private void OnMoveUpClick(object sender, RoutedEventArgs e)
        {
            int selectedIndex = LstPresets.SelectedIndex;
            if (selectedIndex <= 0) return;

            var item = _items[selectedIndex];
            _items.RemoveAt(selectedIndex);
            _items.Insert(selectedIndex - 1, item);
            ReassignIndexNumbers();

            LstPresets.SelectedIndex = selectedIndex - 1;
            LstPresets.ScrollIntoView(_items[selectedIndex - 1]);
        }

        private void OnMoveDownClick(object sender, RoutedEventArgs e)
        {
            int selectedIndex = LstPresets.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= _items.Count - 1) return;

            var item = _items[selectedIndex];
            _items.RemoveAt(selectedIndex);
            _items.Insert(selectedIndex + 1, item);
            ReassignIndexNumbers();

            LstPresets.SelectedIndex = selectedIndex + 1;
            LstPresets.ScrollIntoView(_items[selectedIndex + 1]);
        }

        private void OnEditSelectedClick(object sender, RoutedEventArgs e)
        {
            if (LstPresets.SelectedItem is PresetItemViewModel selectedVm)
            {
                var dlg = new PresetEditDialog(selectedVm.Item);
                dlg.Owner = this;
                if (dlg.ShowDialog() == true && dlg.PresetItem != null)
                {
                    selectedVm.Item = dlg.PresetItem;
                    selectedVm.NotifyAllChanged();
                }
            }
            else
            {
                MessageBox.Show("수정할 규격 항목을 먼저 선택해주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnResetDefaultsClick(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(
                "자주 사용하는 라벨 규격을 프로그램 초기 기본값(4종)으로 되돌리시겠습니까?",
                "기본 규격 복원",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            var defaultList = AppSettings.GetDefaultPresets();
            _items.Clear();
            int idx = 1;
            foreach (var p in defaultList)
            {
                _items.Add(new PresetItemViewModel(idx++, p.Clone()));
            }

            if (_items.Count > 0)
            {
                LstPresets.SelectedIndex = 0;
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            ResultPresets = _items.Select(vm => vm.Item).ToList();
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
