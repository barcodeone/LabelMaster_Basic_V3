using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 입력된 데이터 및 인쇄 이력 레코드 모델 (DataGrid 바인딩 및 엑셀 저장용)
    /// INotifyPropertyChanged 구현으로 삭제 후 순번(Index) 재부여 및 상태 갱신이 UI에 실시간 반영됨
    /// </summary>
    public class PrintRecordModel : INotifyPropertyChanged
    {
        private bool _isChecked = false;
        private int _index;
        private string _timestamp = "";
        private string _col1 = "";
        private string _col2 = "";
        private string _col3 = "";
        private string _col4 = "";
        private string _col5 = "";
        private string _col6 = "";
        private string _col7 = "";
        private string _col8 = "";
        private string _status = "";
        private System.Collections.Generic.Dictionary<string, string> _cellValues = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public System.Collections.Generic.Dictionary<string, string> CellValues
        {
            get => _cellValues;
            set => _cellValues = value ?? new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string GetValueByColumn(string colIdentifier)
        {
            if (string.IsNullOrWhiteSpace(colIdentifier)) return "";
            string key = colIdentifier.Trim();

            if (_cellValues != null && _cellValues.TryGetValue(key, out string val) && !string.IsNullOrEmpty(val))
            {
                return val;
            }

            if (int.TryParse(key, out int num))
            {
                switch (num)
                {
                    case 1: return Col1 ?? "";
                    case 2: return Col2 ?? "";
                    case 3: return Col3 ?? "";
                    case 4: return Col4 ?? "";
                    case 5: return Col5 ?? "";
                    case 6: return Col6 ?? "";
                    case 7: return Col7 ?? "";
                    case 8: return Col8 ?? "";
                    default:
                        if (_cellValues != null && _cellValues.TryGetValue(key, out string extraVal))
                        {
                            return extraVal ?? "";
                        }
                        return "";
                }
            }
            return "";
        }

        public void SetValueByColumn(string colIdentifier, string value)
        {
            if (string.IsNullOrWhiteSpace(colIdentifier)) return;
            string key = colIdentifier.Trim();
            string val = value ?? "";

            if (int.TryParse(key, out int num))
            {
                switch (num)
                {
                    case 1: Col1 = val; break;
                    case 2: Col2 = val; break;
                    case 3: Col3 = val; break;
                    case 4: Col4 = val; break;
                    case 5: Col5 = val; break;
                    case 6: Col6 = val; break;
                    case 7: Col7 = val; break;
                    case 8: Col8 = val; break;
                    default:
                        if (_cellValues == null) _cellValues = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        _cellValues[key] = val;
                        OnPropertyChanged("Item[]");
                        OnPropertyChanged(nameof(CellValues));
                        break;
                }
            }
            else
            {
                if (_cellValues == null) _cellValues = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _cellValues[key] = val;
                OnPropertyChanged("Item[]");
                OnPropertyChanged(nameof(CellValues));
            }
        }

        public string this[int colIndex]
        {
            get => GetValueByColumn(colIndex.ToString());
            set => SetValueByColumn(colIndex.ToString(), value);
        }

        public string this[string colName]
        {
            get => GetValueByColumn(colName);
            set => SetValueByColumn(colName, value);
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Index
        {
            get => _index;
            set
            {
                if (_index != value)
                {
                    _index = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Timestamp
        {
            get => _timestamp;
            set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Col1
        {
            get => _col1;
            set
            {
                if (_col1 != value)
                {
                    _col1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Col2
        {
            get => _col2;
            set
            {
                if (_col2 != value)
                {
                    _col2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Col3
        {
            get => _col3;
            set
            {
                if (_col3 != value)
                {
                    _col3 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Col4
        {
            get => _col4;
            set
            {
                if (_col4 != value)
                {
                    _col4 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Col5
        {
            get => _col5;
            set
            {
                if (_col5 != value)
                {
                    _col5 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Col6
        {
            get => _col6;
            set
            {
                if (_col6 != value)
                {
                    _col6 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Col7
        {
            get => _col7;
            set
            {
                if (_col7 != value)
                {
                    _col7 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Col8
        {
            get => _col8;
            set
            {
                if (_col8 != value)
                {
                    _col8 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
