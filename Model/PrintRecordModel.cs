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
