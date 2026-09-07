using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using BarcodePrint.Model;
using BarcodePrint.Services;

namespace BarcodePrint.ViewModel
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly IExcelService _excelService;
        private readonly ITemplateService _templateService;
        private readonly IPrinterService _printerService;

        private AppSettings _settings;
        private int _recordCounter = 0;
        private List<string> _authenticatedSerials = new List<string>();
        private bool _isBatchUpdating = false;
        private int _selectedWorkMode = 0; // 0: 단건/연속, 1: 엑셀 대량, 2: 목록 직접작성, 3: 디자인, 4: 프린터 설정

        public Action FocusFirstInputRequested;
        public Action RequestPreviewRedraw;
        public Action ScrollToLatestRecordRequested;

        public int SelectedWorkMode
        {
            get => _selectedWorkMode;
            set
            {
                if (_selectedWorkMode != value)
                {
                    _selectedWorkMode = value;
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        private bool _isDesignEditMode = false;
        public bool IsDesignEditMode
        {
            get => _isDesignEditMode;
            set
            {
                if (_isDesignEditMode != value)
                {
                    _isDesignEditMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPrintMode));
                    OnPropertyChanged(nameof(DesignEditModeButtonText));
                    OnPropertyChanged(nameof(DesignEditModeButtonBackground));
                    UpdatePreview();
                }
            }
        }

        public bool IsPrintMode => !IsDesignEditMode;

        public string DesignEditModeButtonText => IsDesignEditMode ? "↩️ 인쇄 모드로 복귀" : "🎨 디자인 서식 편집";
        public string DesignEditModeButtonBackground => IsDesignEditMode ? "#0284C7" : "#4F46E5";

        public void AddNewBlankRecord()
        {
            _recordCounter++;
            var newRecord = new PrintRecordModel
            {
                Index = _recordCounter,
                IsChecked = true,
                Status = "입력 대기",
                Timestamp = IsDateTimeEnabled ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : "",
                Col1 = "",
                Col2 = "",
                Col3 = "",
                Col4 = "",
                Col5 = "",
                Col6 = "",
                Col7 = "",
                Col8 = ""
            };
            Records.Add(newRecord);
            ScrollToLatestRecordRequested?.Invoke();
            StatusMessage = $"새 행 [No.{_recordCounter}]이 추가되었습니다. 그리드에서 바로 입력하세요.";
        }

        #region Properties - Zoom (확대 / 축소)

        public double ZoomRatio
        {
            get => _settings.ZoomRatio;
            set
            {
                double clamped = Math.Max(0.5, Math.Min(3.0, value));
                if (Math.Abs(_settings.ZoomRatio - clamped) > 0.01)
                {
                    _settings.ZoomRatio = clamped;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ZoomPercentageText));
                    UpdatePreview();
                }
            }
        }

        public string ZoomPercentageText => $"{Math.Round(ZoomRatio * 100)}%";

        #endregion

        #region Properties - Date & Time Auto Output (현재 날짜-시간)

        public bool IsDateTimeEnabled
        {
            get => _settings.IsDateTimeEnabled;
            set
            {
                if (_settings.IsDateTimeEnabled != value)
                {
                    _settings.IsDateTimeEnabled = value;
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public int DateTimeOffsetX
        {
            get => _settings.DateTimeOffsetX;
            set
            {
                if (_settings.DateTimeOffsetX != value)
                {
                    _settings.DateTimeOffsetX = value;
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public int DateTimeOffsetY
        {
            get => _settings.DateTimeOffsetY;
            set
            {
                if (_settings.DateTimeOffsetY != value)
                {
                    _settings.DateTimeOffsetY = value;
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public int DateTimeSize
        {
            get => _settings.DateTimeSize;
            set
            {
                if (_settings.DateTimeSize != value)
                {
                    _settings.DateTimeSize = Math.Max(6, Math.Min(40, value));
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int DateTimeWidth
        {
            get => _settings.DateTimeWidth;
            set
            {
                if (_settings.DateTimeWidth != value)
                {
                    _settings.DateTimeWidth = Math.Max(0, value);
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int DateTimeFormatIndex
        {
            get => _settings.DateTimeFormatIndex;
            set
            {
                if (_settings.DateTimeFormatIndex != value)
                {
                    _settings.DateTimeFormatIndex = value;
                    _settings.DateTimeFormatType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DateTimeFormatType));
                    OnPropertyChanged(nameof(FormattedDateTimePreview));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int DateTimeFormatType
        {
            get => _settings.DateTimeFormatIndex;
            set => DateTimeFormatIndex = value;
        }

        public string DateTimePrefix
        {
            get => _settings.DateTimePrefix ?? "";
            set
            {
                if (_settings.DateTimePrefix != value)
                {
                    _settings.DateTimePrefix = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedDateTimePreview));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public string FormattedDateTimePreview => GetFormattedDateTimeString();

        public string GetFormattedDateTimeString()
        {
            string prefix = DateTimePrefix ?? "";
            switch (DateTimeFormatIndex)
            {
                case 0: // YYYY-MM-DD
                    return $"{prefix}{DateTime.Now:yyyy-MM-dd}";
                case 1: // YYYY/MM/DD
                    return $"{prefix}{DateTime.Now:yyyy/MM/dd}";
                case 2: // YYYY.MM.DD
                    return $"{prefix}{DateTime.Now:yyyy.MM.dd}";
                case 3: // YYYY년 MM월 DD일
                    return $"{prefix}{DateTime.Now:yyyy년 MM월 dd일}";
                case 4: // YYYY-MM-DD HH:mm
                    return $"{prefix}{DateTime.Now:yyyy-MM-dd HH:mm}";
                case 5: // YYYY-MM-DD HH:mm:ss
                    return $"{prefix}{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                case 6: // YY.MM.DD
                    return $"{prefix}{DateTime.Now:yy.MM.dd}";
                case 7: // YYYYMMDD (숫자만)
                    return $"{prefix}{DateTime.Now:yyyyMMdd}";
                case 8: // HH:mm:ss (시간만)
                    return $"{prefix}{DateTime.Now:HH:mm:ss}";
                default:
                    return $"{prefix}{DateTime.Now:yyyy-MM-dd}";
            }
        }

        #endregion

        #region Properties - Active Column Count (컬럼 개수 동적 조절 1~8개)

        public int ActiveColumnCount
        {
            get => _settings.ActiveColumnCount > 0 ? _settings.ActiveColumnCount : 5;
            set
            {
                int clamped = Math.Max(1, Math.Min(8, value));
                if (_settings.ActiveColumnCount != clamped)
                {
                    _settings.ActiveColumnCount = clamped;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCol1Visible));
                    OnPropertyChanged(nameof(IsCol2Visible));
                    OnPropertyChanged(nameof(IsCol3Visible));
                    OnPropertyChanged(nameof(IsCol4Visible));
                    OnPropertyChanged(nameof(IsCol5Visible));
                    OnPropertyChanged(nameof(IsCol6Visible));
                    OnPropertyChanged(nameof(IsCol7Visible));
                    OnPropertyChanged(nameof(IsCol8Visible));
                    ActiveColumnCountChanged?.Invoke(clamped);
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public event Action<int> ActiveColumnCountChanged;

        #endregion

        #region Properties - Table Border Settings (표 / 격자 테두리 기능)

        public bool IsTableBorderEnabled
        {
            get => _settings.IsTableBorderEnabled;
            set
            {
                if (_settings.IsTableBorderEnabled != value)
                {
                    _settings.IsTableBorderEnabled = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableBorderStyle
        {
            get => _settings.TableBorderStyle;
            set
            {
                if (_settings.TableBorderStyle != value)
                {
                    _settings.TableBorderStyle = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableBorderThickness
        {
            get => _settings.TableBorderThickness > 0 ? _settings.TableBorderThickness : 1;
            set
            {
                if (_settings.TableBorderThickness != value)
                {
                    _settings.TableBorderThickness = Math.Max(1, Math.Min(5, value));
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TableBorderThicknessIndex));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableBorderThicknessIndex
        {
            get => Math.Max(0, Math.Min(4, TableBorderThickness - 1));
            set
            {
                TableBorderThickness = value + 1;
            }
        }

        public int TableBorderPadding
        {
            get => _settings.TableBorderPadding;
            set
            {
                if (_settings.TableBorderPadding != value)
                {
                    _settings.TableBorderPadding = Math.Max(0, Math.Min(10, value));
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableOffsetX
        {
            get => _settings.TableOffsetX;
            set
            {
                if (_settings.TableOffsetX != value)
                {
                    _settings.TableOffsetX = Math.Max(0, value);
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableOffsetY
        {
            get => _settings.TableOffsetY;
            set
            {
                if (_settings.TableOffsetY != value)
                {
                    _settings.TableOffsetY = Math.Max(0, value);
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableWidth
        {
            get => _settings.TableWidth;
            set
            {
                if (_settings.TableWidth != value)
                {
                    _settings.TableWidth = Math.Max(0, value);
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableHeight
        {
            get => _settings.TableHeight;
            set
            {
                if (_settings.TableHeight != value)
                {
                    _settings.TableHeight = Math.Max(0, value);
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public bool IsTableTitleEnabled
        {
            get => _settings.IsTableTitleEnabled;
            set
            {
                if (_settings.IsTableTitleEnabled != value)
                {
                    _settings.IsTableTitleEnabled = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public string TableTitleText
        {
            get => _settings.TableTitleText ?? "라벨 정보표";
            set
            {
                if (_settings.TableTitleText != value)
                {
                    _settings.TableTitleText = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableTitleSize
        {
            get => _settings.TableTitleSize > 0 ? _settings.TableTitleSize : 13;
            set
            {
                if (_settings.TableTitleSize != value)
                {
                    _settings.TableTitleSize = Math.Max(6, Math.Min(36, value));
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableTitleHeight
        {
            get => _settings.TableTitleHeight > 0 ? _settings.TableTitleHeight : 6;
            set
            {
                if (_settings.TableTitleHeight != value)
                {
                    _settings.TableTitleHeight = Math.Max(3, Math.Min(30, value));
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public string TableTitleAlign
        {
            get => _settings.TableTitleAlign ?? "CENTER";
            set
            {
                if (_settings.TableTitleAlign != value)
                {
                    _settings.TableTitleAlign = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableTitleOffsetX
        {
            get => _settings.TableTitleOffsetX;
            set
            {
                if (_settings.TableTitleOffsetX != value)
                {
                    _settings.TableTitleOffsetX = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableTitleOffsetY
        {
            get => _settings.TableTitleOffsetY;
            set
            {
                if (_settings.TableTitleOffsetY != value)
                {
                    _settings.TableTitleOffsetY = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public bool IsTableTitleUnderline
        {
            get => _settings.IsTableTitleUnderline;
            set
            {
                if (_settings.IsTableTitleUnderline != value)
                {
                    _settings.IsTableTitleUnderline = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsTableTitleLineEnabled));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public bool IsTableTitleLineEnabled
        {
            get => IsTableTitleUnderline;
            set => IsTableTitleUnderline = value;
        }

        public int TableTitleSpacing
        {
            get => _settings.TableTitleSpacing;
            set
            {
                if (_settings.TableTitleSpacing != value)
                {
                    _settings.TableTitleSpacing = Math.Max(0, Math.Min(30, value));
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableOrientation
        {
            get => _settings.TableOrientation;
            set
            {
                if (_settings.TableOrientation != value)
                {
                    _settings.TableOrientation = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsTableHorizontalOrientation));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public bool IsTableHorizontalOrientation => TableOrientation == 1;

        public int TableHeaderWidthMm
        {
            get => _settings.TableHeaderWidthMm > 0 ? _settings.TableHeaderWidthMm : 20;
            set
            {
                if (_settings.TableHeaderWidthMm != value)
                {
                    _settings.TableHeaderWidthMm = Math.Max(5, Math.Min(100, value));
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int TableHeaderHeightMm
        {
            get => _settings.TableHeaderHeightMm > 0 ? _settings.TableHeaderHeightMm : 7;
            set
            {
                if (_settings.TableHeaderHeightMm != value)
                {
                    _settings.TableHeaderHeightMm = Math.Max(3, Math.Min(50, value));
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        #region Properties - Serial (순번 / 일련번호 / 카운터) Settings

        public bool IsSerialEnabled
        {
            get => _settings.IsSerialEnabled;
            set
            {
                if (_settings.IsSerialEnabled != value)
                {
                    _settings.IsSerialEnabled = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int SerialFormatIndex
        {
            get => _settings.SerialFormatIndex;
            set
            {
                if (_settings.SerialFormatIndex != value)
                {
                    _settings.SerialFormatIndex = value;
                    // 양식에 따른 Prefix 및 Digits 자동 설정
                    switch (value)
                    {
                        case 0: // 1, 2, 3...
                            _settings.SerialPrefix = "";
                            _settings.SerialSuffix = "";
                            _settings.SerialDigits = 0;
                            break;
                        case 1: // No. 1, 2...
                            _settings.SerialPrefix = "No. ";
                            _settings.SerialSuffix = "";
                            _settings.SerialDigits = 0;
                            break;
                        case 2: // 001, 002...
                            _settings.SerialPrefix = "";
                            _settings.SerialSuffix = "";
                            _settings.SerialDigits = 3;
                            break;
                        case 3: // 0001, 0002...
                            _settings.SerialPrefix = "";
                            _settings.SerialSuffix = "";
                            _settings.SerialDigits = 4;
                            break;
                        case 4: // No. 001, 002...
                            _settings.SerialPrefix = "No. ";
                            _settings.SerialSuffix = "";
                            _settings.SerialDigits = 3;
                            break;
                        case 5: // No. 0001, 0002...
                            _settings.SerialPrefix = "No. ";
                            _settings.SerialSuffix = "";
                            _settings.SerialDigits = 4;
                            break;
                        case 6: // (1), (2)...
                            _settings.SerialPrefix = "(";
                            _settings.SerialSuffix = ")";
                            _settings.SerialDigits = 0;
                            break;
                        case 7: // [001], [002]...
                            _settings.SerialPrefix = "[";
                            _settings.SerialSuffix = "]";
                            _settings.SerialDigits = 3;
                            break;
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SerialPrefix));
                    OnPropertyChanged(nameof(SerialSuffix));
                    OnPropertyChanged(nameof(SerialDigits));
                    OnPropertyChanged(nameof(FormattedSerialPreview));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int SerialDirectionIndex
        {
            get => _settings.SerialDirectionIndex;
            set
            {
                if (_settings.SerialDirectionIndex != value)
                {
                    _settings.SerialDirectionIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsSerialDecrement));
                    OnPropertyChanged(nameof(FormattedSerialPreview));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public bool IsSerialDecrement
        {
            get => SerialDirectionIndex == 1;
            set => SerialDirectionIndex = value ? 1 : 0;
        }

        public string SerialPrefix
        {
            get => _settings.SerialPrefix ?? "";
            set
            {
                if (_settings.SerialPrefix != value)
                {
                    _settings.SerialPrefix = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedSerialPreview));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public string SerialSuffix
        {
            get => _settings.SerialSuffix ?? "";
            set
            {
                if (_settings.SerialSuffix != value)
                {
                    _settings.SerialSuffix = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedSerialPreview));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int SerialDigits
        {
            get => _settings.SerialDigits;
            set
            {
                if (_settings.SerialDigits != value)
                {
                    _settings.SerialDigits = Math.Max(0, Math.Min(8, value));
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedSerialPreview));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int SerialStartNumber
        {
            get => _settings.SerialStartNumber;
            set
            {
                if (_settings.SerialStartNumber != value)
                {
                    _settings.SerialStartNumber = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FormattedSerialPreview));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int SerialStep
        {
            get => _settings.SerialStep != 0 ? _settings.SerialStep : 1;
            set
            {
                if (_settings.SerialStep != value)
                {
                    _settings.SerialStep = Math.Max(1, Math.Abs(value));
                    OnPropertyChanged();
                    _settings.Save();
                }
            }
        }

        public int SerialSize
        {
            get => _settings.SerialSize > 0 ? _settings.SerialSize : 10;
            set
            {
                if (_settings.SerialSize != value)
                {
                    _settings.SerialSize = Math.Max(5, Math.Min(36, value));
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int SerialOffsetX
        {
            get => _settings.SerialOffsetX;
            set
            {
                if (_settings.SerialOffsetX != value)
                {
                    _settings.SerialOffsetX = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int SerialOffsetY
        {
            get => _settings.SerialOffsetY;
            set
            {
                if (_settings.SerialOffsetY != value)
                {
                    _settings.SerialOffsetY = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int SerialWidth
        {
            get => _settings.SerialWidth;
            set
            {
                if (_settings.SerialWidth != value)
                {
                    _settings.SerialWidth = Math.Max(0, value);
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public string SerialAlign
        {
            get => _settings.SerialAlign ?? "LEFT";
            set
            {
                if (_settings.SerialAlign != value)
                {
                    _settings.SerialAlign = value;
                    OnPropertyChanged();
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public string FormattedSerialPreview
        {
            get
            {
                string numStr = SerialDigits > 0 ? SerialStartNumber.ToString(new string('0', SerialDigits)) : SerialStartNumber.ToString();
                return $"{SerialPrefix}{numStr}{SerialSuffix}";
            }
        }

        #endregion

        // 현재 캔버스에서 마우스로 선택된 요소 인덱스 (100: 표 상단제목, 101: 순번, -1~-8: 좌측 제목셀, 0~7: 내용/바코드, 99: 날짜)
        private int _selectedItemIndex = -999;
        public int SelectedItemIndex
        {
            get => _selectedItemIndex;
            set { _selectedItemIndex = value; OnPropertyChanged(); }
        }

        #endregion

        public bool IsCol1Visible => ActiveColumnCount >= 1;
        public bool IsCol2Visible => ActiveColumnCount >= 2;
        public bool IsCol3Visible => ActiveColumnCount >= 3;
        public bool IsCol4Visible => ActiveColumnCount >= 4;
        public bool IsCol5Visible => ActiveColumnCount >= 5;
        public bool IsCol6Visible => ActiveColumnCount >= 6;
        public bool IsCol7Visible => ActiveColumnCount >= 7;
        public bool IsCol8Visible => ActiveColumnCount >= 8;

        #region Properties - Barcode Options & Barcode Types (바코드/QR/2D 타입 대폭 확장 지원)

        public bool IsColumn1Barcode { get => _settings.IsColumn1Barcode; set { _settings.IsColumn1Barcode = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool IsColumn2Barcode { get => _settings.IsColumn2Barcode; set { _settings.IsColumn2Barcode = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool IsColumn3Barcode { get => _settings.IsColumn3Barcode; set { _settings.IsColumn3Barcode = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool IsColumn4Barcode { get => _settings.IsColumn4Barcode; set { _settings.IsColumn4Barcode = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool IsColumn5Barcode { get => _settings.IsColumn5Barcode; set { _settings.IsColumn5Barcode = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool IsColumn6Barcode { get => _settings.IsColumn6Barcode; set { _settings.IsColumn6Barcode = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool IsColumn7Barcode { get => _settings.IsColumn7Barcode; set { _settings.IsColumn7Barcode = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool IsColumn8Barcode { get => _settings.IsColumn8Barcode; set { _settings.IsColumn8Barcode = value; OnPropertyChanged(); UpdatePreview(); } }

        public string Col1BarcodeType { get => _settings.Col1BarcodeType; set { _settings.Col1BarcodeType = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col2BarcodeType { get => _settings.Col2BarcodeType; set { _settings.Col2BarcodeType = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col3BarcodeType { get => _settings.Col3BarcodeType; set { _settings.Col3BarcodeType = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col4BarcodeType { get => _settings.Col4BarcodeType; set { _settings.Col4BarcodeType = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col5BarcodeType { get => _settings.Col5BarcodeType; set { _settings.Col5BarcodeType = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col6BarcodeType { get => _settings.Col6BarcodeType; set { _settings.Col6BarcodeType = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col7BarcodeType { get => _settings.Col7BarcodeType; set { _settings.Col7BarcodeType = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col8BarcodeType { get => _settings.Col8BarcodeType; set { _settings.Col8BarcodeType = value; OnPropertyChanged(); UpdatePreview(); } }

        public bool Col1ShowBarcodeText { get => _settings.Col1ShowBarcodeText; set { _settings.Col1ShowBarcodeText = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col2ShowBarcodeText { get => _settings.Col2ShowBarcodeText; set { _settings.Col2ShowBarcodeText = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col3ShowBarcodeText { get => _settings.Col3ShowBarcodeText; set { _settings.Col3ShowBarcodeText = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col4ShowBarcodeText { get => _settings.Col4ShowBarcodeText; set { _settings.Col4ShowBarcodeText = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col5ShowBarcodeText { get => _settings.Col5ShowBarcodeText; set { _settings.Col5ShowBarcodeText = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col6ShowBarcodeText { get => _settings.Col6ShowBarcodeText; set { _settings.Col6ShowBarcodeText = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col7ShowBarcodeText { get => _settings.Col7ShowBarcodeText; set { _settings.Col7ShowBarcodeText = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col8ShowBarcodeText { get => _settings.Col8ShowBarcodeText; set { _settings.Col8ShowBarcodeText = value; OnPropertyChanged(); UpdatePreview(); } }

        public List<string> AvailableBarcodeTypes { get; } = new List<string> 
        { 
            "CODE128", 
            "CODE39", 
            "CODE93",
            "EAN13", 
            "EAN8", 
            "UPC-A", 
            "UPC-E",
            "ITF", 
            "CODABAR",
            "QR_CODE", 
            "DATAMATRIX",
            "PDF417"
        };

        #endregion

        #region Properties - Advanced Printer Hardware Settings

        public int PrintDarkness
        {
            get => _settings.PrintDarkness;
            set { _settings.PrintDarkness = Math.Max(1, Math.Min(15, value)); OnPropertyChanged(); _settings.Save(); }
        }

        public int PrintSpeed
        {
            get => _settings.PrintSpeed;
            set { _settings.PrintSpeed = Math.Max(2, Math.Min(6, value)); OnPropertyChanged(); _settings.Save(); }
        }

        public string PaperType
        {
            get => _settings.PaperType ?? "GAP";
            set
            {
                _settings.PaperType = value;
                PRINT_INFO_CLASS.PrinterInfo.PaperType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBlineMode));
                _settings.Save();
            }
        }

        public bool IsBlineMode => PaperType == "BLINE";

        public int GapHeight
        {
            get => _settings.GapHeight;
            set
            {
                _settings.GapHeight = Math.Max(0, value);
                PRINT_INFO_CLASS.PrinterInfo.nGAP = _settings.GapHeight;
                OnPropertyChanged();
                _settings.Save();
            }
        }

        public double BlineOffset
        {
            get => _settings.BlineOffset;
            set
            {
                _settings.BlineOffset = Math.Round(value, 1);
                PRINT_INFO_CLASS.PrinterInfo.BlineOffset = _settings.BlineOffset;
                OnPropertyChanged();
                _settings.Save();
            }
        }

        public bool IsRibbon
        {
            get => _settings.IsRibbon;
            set
            {
                _settings.IsRibbon = value;
                PRINT_INFO_CLASS.PrinterInfo.bRibbon = value;
                OnPropertyChanged();
                _settings.Save();
            }
        }

        public int PrinterOffsetX
        {
            get => _settings.PrinterOffsetX;
            set 
            { 
                _settings.PrinterOffsetX = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(PrinterOffsetXStr));
                _settings.Save(); 
                UpdatePreview(); 
            }
        }

        public string PrinterOffsetXStr
        {
            get => PrinterOffsetX.ToString();
            set
            {
                if (int.TryParse(value, out int v))
                {
                    PrinterOffsetX = Math.Max(-50, Math.Min(50, v));
                }
            }
        }

        public int PrinterOffsetY
        {
            get => _settings.PrinterOffsetY;
            set 
            { 
                _settings.PrinterOffsetY = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(PrinterOffsetYStr));
                _settings.Save(); 
                UpdatePreview(); 
            }
        }

        public string PrinterOffsetYStr
        {
            get => PrinterOffsetY.ToString();
            set
            {
                if (int.TryParse(value, out int v))
                {
                    PrinterOffsetY = Math.Max(-50, Math.Min(50, v));
                }
            }
        }

        public int TearOffOffset
        {
            get => _settings.TearOffOffset;
            set { _settings.TearOffOffset = value; OnPropertyChanged(); _settings.Save(); }
        }

        public int PrintCopies
        {
            get => _settings.PrintCopies;
            set { _settings.PrintCopies = Math.Max(1, value); OnPropertyChanged(); _settings.Save(); }
        }

        public string EnterActionType
        {
            get => _settings.EnterActionType ?? "PRINT_AND_RECORD";
            set
            {
                _settings.EnterActionType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEnterPrintAndRecord));
                OnPropertyChanged(nameof(IsEnterRecordOnly));
                _settings.Save();
            }
        }

        public bool IsEnterPrintAndRecord
        {
            get => EnterActionType == "PRINT_AND_RECORD";
            set { if (value) EnterActionType = "PRINT_AND_RECORD"; }
        }

        public bool IsEnterRecordOnly
        {
            get => EnterActionType == "RECORD_ONLY";
            set { if (value) EnterActionType = "RECORD_ONLY"; }
        }

        public int PrintRangeStart
        {
            get => _settings.PrintRangeStart > 0 ? _settings.PrintRangeStart : 1;
            set { _settings.PrintRangeStart = Math.Max(1, value); OnPropertyChanged(); }
        }

        public int PrintRangeEnd
        {
            get => _settings.PrintRangeEnd > 0 ? _settings.PrintRangeEnd : Math.Max(1, Records.Count);
            set { _settings.PrintRangeEnd = Math.Max(1, value); OnPropertyChanged(); }
        }

        public List<int> SpeedOptions { get; } = new List<int> { 2, 3, 4, 5, 6 };
        public List<int> DarknessOptions { get; } = Enumerable.Range(1, 15).ToList();

        private string _loadedExcelFileName = "";
        public string LoadedExcelFileName
        {
            get => _loadedExcelFileName;
            set
            {
                if (SetProperty(ref _loadedExcelFileName, value))
                {
                    OnPropertyChanged(nameof(IsExcelFileLoaded));
                }
            }
        }
        public bool IsExcelFileLoaded => !string.IsNullOrWhiteSpace(LoadedExcelFileName);

        #endregion

        #region Properties - Preview Page Navigation (실시간 라벨 페이지 탐색)

        private int _currentPreviewIndex = 0; // 0 = Live Input, 1 ~ Records.Count = Record Data
        public int CurrentPreviewIndex
        {
            get => _currentPreviewIndex;
            set
            {
                int maxIdx = Records?.Count ?? 0;
                int clamped = Math.Max(0, Math.Min(maxIdx, value));
                if (SetProperty(ref _currentPreviewIndex, clamped))
                {
                    OnPropertyChanged(nameof(PreviewPageText));
                    OnPropertyChanged(nameof(IsLiveInputMode));
                    OnPropertyChanged(nameof(HasPreviousPreviewPage));
                    OnPropertyChanged(nameof(HasNextPreviewPage));
                    UpdatePreview();
                }
            }
        }

        public bool IsLiveInputMode => CurrentPreviewIndex == 0;
        public bool HasPreviousPreviewPage => CurrentPreviewIndex > 1 || (CurrentPreviewIndex == 0 && Records.Count > 0);
        public bool HasNextPreviewPage => CurrentPreviewIndex > 0 && CurrentPreviewIndex < Records.Count;

        public string PreviewPageText
        {
            get
            {
                if (Records == null || Records.Count == 0)
                {
                    return "0 / 0";
                }
                int cur = CurrentPreviewIndex <= 0 ? 1 : CurrentPreviewIndex;
                return $"{cur} / {Records.Count}";
            }
        }

        #endregion

        #region Properties - Printer & Page Settings

        private string _paperWidthStr;
        public string PaperWidthStr
        {
            get => _paperWidthStr ?? (_paperWidthStr = _settings.PaperWidth.ToString());
            set
            {
                if (_paperWidthStr != value)
                {
                    _paperWidthStr = value;
                    OnPropertyChanged();

                    if (int.TryParse(value, out int parsedVal) && parsedVal > 0)
                    {
                        _settings.PaperWidth = parsedVal;
                        OnPropertyChanged(nameof(PaperWidth));
                        _settings.Save();
                        UpdatePreview();
                    }
                }
            }
        }

        private string _paperHeightStr;
        public string PaperHeightStr
        {
            get => _paperHeightStr ?? (_paperHeightStr = _settings.PaperHeight.ToString());
            set
            {
                if (_paperHeightStr != value)
                {
                    _paperHeightStr = value;
                    OnPropertyChanged();

                    if (int.TryParse(value, out int parsedVal) && parsedVal > 0)
                    {
                        _settings.PaperHeight = parsedVal;
                        OnPropertyChanged(nameof(PaperHeight));
                        _settings.Save();
                        UpdatePreview();
                    }
                }
            }
        }

        public int PaperWidth
        {
            get => _settings.PaperWidth;
            set
            {
                if (_settings.PaperWidth != value && value > 0)
                {
                    _settings.PaperWidth = value;
                    _paperWidthStr = value.ToString();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PaperWidthStr));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int PaperHeight
        {
            get => _settings.PaperHeight;
            set
            {
                if (_settings.PaperHeight != value && value > 0)
                {
                    _settings.PaperHeight = value;
                    _paperHeightStr = value.ToString();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PaperHeightStr));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public int LabelRotationAngle
        {
            get => _settings.LabelRotationAngle;
            set
            {
                int normalized = ((value % 360) + 360) % 360;
                if (_settings.LabelRotationAngle != normalized)
                {
                    _settings.LabelRotationAngle = normalized;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LabelRotationText));
                    OnPropertyChanged(nameof(LabelRotationAngleStr));
                    OnPropertyChanged(nameof(IsRotated));
                    OnPropertyChanged(nameof(IsRotated0));
                    OnPropertyChanged(nameof(IsRotated90));
                    OnPropertyChanged(nameof(IsRotated180));
                    OnPropertyChanged(nameof(IsRotated270));
                    OnPropertyChanged(nameof(EffectivePaperWidth));
                    OnPropertyChanged(nameof(EffectivePaperHeight));
                    _settings.Save();
                    UpdatePreview();
                }
            }
        }

        public string LabelRotationText => $"🔄 {LabelRotationAngle}° 회전";

        public string LabelRotationAngleStr
        {
            get => LabelRotationAngle.ToString();
            set
            {
                if (int.TryParse(value, out int angle))
                {
                    LabelRotationAngle = angle;
                }
            }
        }

        public bool IsRotated0 => LabelRotationAngle == 0;
        public bool IsRotated90 => LabelRotationAngle == 90;
        public bool IsRotated180 => LabelRotationAngle == 180;
        public bool IsRotated270 => LabelRotationAngle == 270;

        public int EffectivePaperWidth => (LabelRotationAngle == 90 || LabelRotationAngle == 270) ? PaperHeight : PaperWidth;
        public int EffectivePaperHeight => (LabelRotationAngle == 90 || LabelRotationAngle == 270) ? PaperWidth : PaperHeight;

        public bool IsRotated
        {
            get => LabelRotationAngle == 180;
            set
            {
                LabelRotationAngle = value ? 180 : 0;
            }
        }

        #endregion

        #region Properties - Column Headers & Formats

        public string Column1Header { get => _settings.Column1Name; set { _settings.Column1Name = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Column2Header { get => _settings.Column2Name; set { _settings.Column2Name = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Column3Header { get => _settings.Column3Name; set { _settings.Column3Name = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Column4Header { get => _settings.Column4Name; set { _settings.Column4Name = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Column5Header { get => _settings.Column5Name; set { _settings.Column5Name = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Column6Header { get => _settings.Column6Name; set { _settings.Column6Name = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Column7Header { get => _settings.Column7Name; set { _settings.Column7Name = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Column8Header { get => _settings.Column8Name; set { _settings.Column8Name = value; OnPropertyChanged(); UpdatePreview(); } }

        public bool IsColumn1Price { get => _settings.IsColumn1PriceFormat; set { _settings.IsColumn1PriceFormat = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn1PriceFormat)); UpdatePreview(); } }
        public bool IsColumn2Price { get => _settings.IsColumn2PriceFormat; set { _settings.IsColumn2PriceFormat = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn2PriceFormat)); UpdatePreview(); } }
        public bool IsColumn3Price { get => _settings.IsColumn3PriceFormat; set { _settings.IsColumn3PriceFormat = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn3PriceFormat)); UpdatePreview(); } }
        public bool IsColumn4Price { get => _settings.IsColumn4PriceFormat; set { _settings.IsColumn4PriceFormat = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn4PriceFormat)); UpdatePreview(); } }
        public bool IsColumn5Price { get => _settings.IsColumn5PriceFormat; set { _settings.IsColumn5PriceFormat = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn5PriceFormat)); UpdatePreview(); } }
        public bool IsColumn6Price { get => _settings.IsColumn6PriceFormat; set { _settings.IsColumn6PriceFormat = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn6PriceFormat)); UpdatePreview(); } }
        public bool IsColumn7Price { get => _settings.IsColumn7PriceFormat; set { _settings.IsColumn7PriceFormat = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn7PriceFormat)); UpdatePreview(); } }
        public bool IsColumn8Price { get => _settings.IsColumn8PriceFormat; set { _settings.IsColumn8PriceFormat = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn8PriceFormat)); UpdatePreview(); } }

        public bool IsColumn1PriceFormat { get => IsColumn1Price; set => IsColumn1Price = value; }
        public bool IsColumn2PriceFormat { get => IsColumn2Price; set => IsColumn2Price = value; }
        public bool IsColumn3PriceFormat { get => IsColumn3Price; set => IsColumn3Price = value; }
        public bool IsColumn4PriceFormat { get => IsColumn4Price; set => IsColumn4Price = value; }
        public bool IsColumn5PriceFormat { get => IsColumn5Price; set => IsColumn5Price = value; }
        public bool IsColumn6PriceFormat { get => IsColumn6Price; set => IsColumn6Price = value; }
        public bool IsColumn7PriceFormat { get => IsColumn7Price; set => IsColumn7Price = value; }
        public bool IsColumn8PriceFormat { get => IsColumn8Price; set => IsColumn8Price = value; }

        public bool IsIncludeHeader1 { get => _settings.IsColumn1IncludeHeader; set { _settings.IsColumn1IncludeHeader = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn1IncludeHeader)); UpdatePreview(); } }
        public bool IsIncludeHeader2 { get => _settings.IsColumn2IncludeHeader; set { _settings.IsColumn2IncludeHeader = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn2IncludeHeader)); UpdatePreview(); } }
        public bool IsIncludeHeader3 { get => _settings.IsColumn3IncludeHeader; set { _settings.IsColumn3IncludeHeader = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn3IncludeHeader)); UpdatePreview(); } }
        public bool IsIncludeHeader4 { get => _settings.IsColumn4IncludeHeader; set { _settings.IsColumn4IncludeHeader = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn4IncludeHeader)); UpdatePreview(); } }
        public bool IsIncludeHeader5 { get => _settings.IsColumn5IncludeHeader; set { _settings.IsColumn5IncludeHeader = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn5IncludeHeader)); UpdatePreview(); } }
        public bool IsIncludeHeader6 { get => _settings.IsColumn6IncludeHeader; set { _settings.IsColumn6IncludeHeader = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn6IncludeHeader)); UpdatePreview(); } }
        public bool IsIncludeHeader7 { get => _settings.IsColumn7IncludeHeader; set { _settings.IsColumn7IncludeHeader = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn7IncludeHeader)); UpdatePreview(); } }
        public bool IsIncludeHeader8 { get => _settings.IsColumn8IncludeHeader; set { _settings.IsColumn8IncludeHeader = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsColumn8IncludeHeader)); UpdatePreview(); } }

        public bool IsColumn1IncludeHeader { get => IsIncludeHeader1; set => IsIncludeHeader1 = value; }
        public bool IsColumn2IncludeHeader { get => IsIncludeHeader2; set => IsIncludeHeader2 = value; }
        public bool IsColumn3IncludeHeader { get => IsIncludeHeader3; set => IsIncludeHeader3 = value; }
        public bool IsColumn4IncludeHeader { get => IsIncludeHeader4; set => IsIncludeHeader4 = value; }
        public bool IsColumn5IncludeHeader { get => IsIncludeHeader5; set => IsIncludeHeader5 = value; }
        public bool IsColumn6IncludeHeader { get => IsIncludeHeader6; set => IsIncludeHeader6 = value; }
        public bool IsColumn7IncludeHeader { get => IsIncludeHeader7; set => IsIncludeHeader7 = value; }
        public bool IsColumn8IncludeHeader { get => IsIncludeHeader8; set => IsIncludeHeader8 = value; }

        #endregion

        #region Properties - Fonts & Styles

        private static List<string> LoadSystemFonts()
        {
            var priorityFonts = new[] { "맑은 고딕", "굴림", "돋움", "바탕", "궁서", "Arial", "Segoe UI", "Tahoma", "Times New Roman", "Courier New", "나눔고딕", "나눔명조", "Pretendard" };
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string>();

            foreach (var p in priorityFonts)
            {
                if (set.Add(p)) list.Add(p);
            }

            try
            {
                var installed = System.Windows.Media.Fonts.SystemFontFamilies
                    .Select(f => f.Source)
                    .Where(name => !string.IsNullOrWhiteSpace(name) && !name.StartsWith("@"))
                    .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase);

                foreach (var fontName in installed)
                {
                    if (set.Add(fontName))
                    {
                        list.Add(fontName);
                    }
                }
            }
            catch { }

            return list;
        }

        public List<string> AvailableFontFamilies { get; } = LoadSystemFonts();

        public string Col1Font { get => _settings.Col1Font ?? "맑은 고딕"; set { _settings.Col1Font = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col2Font { get => _settings.Col2Font ?? "맑은 고딕"; set { _settings.Col2Font = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col3Font { get => _settings.Col3Font ?? "맑은 고딕"; set { _settings.Col3Font = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col4Font { get => _settings.Col4Font ?? "맑은 고딕"; set { _settings.Col4Font = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col5Font { get => _settings.Col5Font ?? "맑은 고딕"; set { _settings.Col5Font = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col6Font { get => _settings.Col6Font ?? "맑은 고딕"; set { _settings.Col6Font = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col7Font { get => _settings.Col7Font ?? "맑은 고딕"; set { _settings.Col7Font = value; OnPropertyChanged(); UpdatePreview(); } }
        public string Col8Font { get => _settings.Col8Font ?? "맑은 고딕"; set { _settings.Col8Font = value; OnPropertyChanged(); UpdatePreview(); } }

        public bool Col1Bold { get => _settings.Col1Bold; set { _settings.Col1Bold = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col2Bold { get => _settings.Col2Bold; set { _settings.Col2Bold = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col3Bold { get => _settings.Col3Bold; set { _settings.Col3Bold = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col4Bold { get => _settings.Col4Bold; set { _settings.Col4Bold = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col5Bold { get => _settings.Col5Bold; set { _settings.Col5Bold = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col6Bold { get => _settings.Col6Bold; set { _settings.Col6Bold = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col7Bold { get => _settings.Col7Bold; set { _settings.Col7Bold = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col8Bold { get => _settings.Col8Bold; set { _settings.Col8Bold = value; OnPropertyChanged(); UpdatePreview(); } }

        public bool Col1Italic { get => _settings.Col1Italic; set { _settings.Col1Italic = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col2Italic { get => _settings.Col2Italic; set { _settings.Col2Italic = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col3Italic { get => _settings.Col3Italic; set { _settings.Col3Italic = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col4Italic { get => _settings.Col4Italic; set { _settings.Col4Italic = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col5Italic { get => _settings.Col5Italic; set { _settings.Col5Italic = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col6Italic { get => _settings.Col6Italic; set { _settings.Col6Italic = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col7Italic { get => _settings.Col7Italic; set { _settings.Col7Italic = value; OnPropertyChanged(); UpdatePreview(); } }
        public bool Col8Italic { get => _settings.Col8Italic; set { _settings.Col8Italic = value; OnPropertyChanged(); UpdatePreview(); } }

        public string TableTitleFont { get => _settings.TableTitleFont ?? "맑은 고딕"; set { _settings.TableTitleFont = value; OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public bool TableTitleBold { get => _settings.TableTitleBold; set { _settings.TableTitleBold = value; OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public bool TableTitleItalic { get => _settings.TableTitleItalic; set { _settings.TableTitleItalic = value; OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }

        public string SerialFont { get => _settings.SerialFont ?? "맑은 고딕"; set { _settings.SerialFont = value; OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public bool SerialBold { get => _settings.SerialBold; set { _settings.SerialBold = value; OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public bool SerialItalic { get => _settings.SerialItalic; set { _settings.SerialItalic = value; OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }

        public string DateTimeFont { get => _settings.DateTimeFont ?? "맑은 고딕"; set { _settings.DateTimeFont = value; OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public bool DateTimeBold { get => _settings.DateTimeBold; set { _settings.DateTimeBold = value; OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public bool DateTimeItalic { get => _settings.DateTimeItalic; set { _settings.DateTimeItalic = value; OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }

        public int Col1Size { get => _settings.Col1Size; set { _settings.Col1Size = Math.Max(6, Math.Min(40, value)); OnPropertyChanged(); UpdatePreview(); } }
        public int Col2Size { get => _settings.Col2Size; set { _settings.Col2Size = Math.Max(6, Math.Min(40, value)); OnPropertyChanged(); UpdatePreview(); } }
        public int Col3Size { get => _settings.Col3Size; set { _settings.Col3Size = Math.Max(6, Math.Min(40, value)); OnPropertyChanged(); UpdatePreview(); } }
        public int Col4Size { get => _settings.Col4Size; set { _settings.Col4Size = Math.Max(6, Math.Min(40, value)); OnPropertyChanged(); UpdatePreview(); } }
        public int Col5Size { get => _settings.Col5Size; set { _settings.Col5Size = Math.Max(6, Math.Min(40, value)); OnPropertyChanged(); UpdatePreview(); } }
        public int Col6Size { get => _settings.Col6Size; set { _settings.Col6Size = Math.Max(6, Math.Min(40, value)); OnPropertyChanged(); UpdatePreview(); } }
        public int Col7Size { get => _settings.Col7Size; set { _settings.Col7Size = Math.Max(6, Math.Min(40, value)); OnPropertyChanged(); UpdatePreview(); } }
        public int Col8Size { get => _settings.Col8Size; set { _settings.Col8Size = Math.Max(6, Math.Min(40, value)); OnPropertyChanged(); UpdatePreview(); } }

        #endregion

        #region Properties - Offsets (X, Y in mm)

        public int Col1OffsetX { get => _settings.Col1OffsetX; set { _settings.Col1OffsetX = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col1OffsetY { get => _settings.Col1OffsetY; set { _settings.Col1OffsetY = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col2OffsetX { get => _settings.Col2OffsetX; set { _settings.Col2OffsetX = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col2OffsetY { get => _settings.Col2OffsetY; set { _settings.Col2OffsetY = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col3OffsetX { get => _settings.Col3OffsetX; set { _settings.Col3OffsetX = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col3OffsetY { get => _settings.Col3OffsetY; set { _settings.Col3OffsetY = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col4OffsetX { get => _settings.Col4OffsetX; set { _settings.Col4OffsetX = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col4OffsetY { get => _settings.Col4OffsetY; set { _settings.Col4OffsetY = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col5OffsetX { get => _settings.Col5OffsetX; set { _settings.Col5OffsetX = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col5OffsetY { get => _settings.Col5OffsetY; set { _settings.Col5OffsetY = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col6OffsetX { get => _settings.Col6OffsetX; set { _settings.Col6OffsetX = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col6OffsetY { get => _settings.Col6OffsetY; set { _settings.Col6OffsetY = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col7OffsetX { get => _settings.Col7OffsetX; set { _settings.Col7OffsetX = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col7OffsetY { get => _settings.Col7OffsetY; set { _settings.Col7OffsetY = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col8OffsetX { get => _settings.Col8OffsetX; set { _settings.Col8OffsetX = value; OnPropertyChanged(); UpdatePreview(); } }
        public int Col8OffsetY { get => _settings.Col8OffsetY; set { _settings.Col8OffsetY = value; OnPropertyChanged(); UpdatePreview(); } }

        public int Col1Width { get => _settings.Col1Width; set { _settings.Col1Width = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col2Width { get => _settings.Col2Width; set { _settings.Col2Width = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col3Width { get => _settings.Col3Width; set { _settings.Col3Width = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col4Width { get => _settings.Col4Width; set { _settings.Col4Width = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col5Width { get => _settings.Col5Width; set { _settings.Col5Width = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col6Width { get => _settings.Col6Width; set { _settings.Col6Width = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col7Width { get => _settings.Col7Width; set { _settings.Col7Width = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col8Width { get => _settings.Col8Width; set { _settings.Col8Width = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }

        public int Col1RowHeight { get => _settings.Col1RowHeight; set { _settings.Col1RowHeight = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col2RowHeight { get => _settings.Col2RowHeight; set { _settings.Col2RowHeight = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col3RowHeight { get => _settings.Col3RowHeight; set { _settings.Col3RowHeight = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col4RowHeight { get => _settings.Col4RowHeight; set { _settings.Col4RowHeight = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col5RowHeight { get => _settings.Col5RowHeight; set { _settings.Col5RowHeight = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col6RowHeight { get => _settings.Col6RowHeight; set { _settings.Col6RowHeight = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col7RowHeight { get => _settings.Col7RowHeight; set { _settings.Col7RowHeight = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }
        public int Col8RowHeight { get => _settings.Col8RowHeight; set { _settings.Col8RowHeight = Math.Max(0, value); OnPropertyChanged(); _settings.Save(); UpdatePreview(); } }

        #endregion

        #region Properties - Inputs & State

        private string _inputCol1 = "";
        public string InputCol1 { get => _inputCol1; set { if (SetProperty(ref _inputCol1, value)) UpdatePreview(); } }

        private string _inputCol2 = "";
        public string InputCol2 { get => _inputCol2; set { if (SetProperty(ref _inputCol2, value)) UpdatePreview(); } }

        private string _inputCol3 = "";
        public string InputCol3 { get => _inputCol3; set { if (SetProperty(ref _inputCol3, value)) UpdatePreview(); } }

        private string _inputCol4 = "";
        public string InputCol4 { get => _inputCol4; set { if (SetProperty(ref _inputCol4, value)) UpdatePreview(); } }

        private string _inputCol5 = "";
        public string InputCol5 { get => _inputCol5; set { if (SetProperty(ref _inputCol5, value)) UpdatePreview(); } }

        private string _inputCol6 = "";
        public string InputCol6 { get => _inputCol6; set { if (SetProperty(ref _inputCol6, value)) UpdatePreview(); } }

        private string _inputCol7 = "";
        public string InputCol7 { get => _inputCol7; set { if (SetProperty(ref _inputCol7, value)) UpdatePreview(); } }

        private string _inputCol8 = "";
        public string InputCol8 { get => _inputCol8; set { if (SetProperty(ref _inputCol8, value)) UpdatePreview(); } }

        public bool IsCol1Keep { get => _settings.IsCol1Keep; set { _settings.IsCol1Keep = value; OnPropertyChanged(); } }
        public bool IsCol2Keep { get => _settings.IsCol2Keep; set { _settings.IsCol2Keep = value; OnPropertyChanged(); } }
        public bool IsCol3Keep { get => _settings.IsCol3Keep; set { _settings.IsCol3Keep = value; OnPropertyChanged(); } }
        public bool IsCol4Keep { get => _settings.IsCol4Keep; set { _settings.IsCol4Keep = value; OnPropertyChanged(); } }
        public bool IsCol5Keep { get => _settings.IsCol5Keep; set { _settings.IsCol5Keep = value; OnPropertyChanged(); } }
        public bool IsCol6Keep { get => _settings.IsCol6Keep; set { _settings.IsCol6Keep = value; OnPropertyChanged(); } }
        public bool IsCol7Keep { get => _settings.IsCol7Keep; set { _settings.IsCol7Keep = value; OnPropertyChanged(); } }
        public bool IsCol8Keep { get => _settings.IsCol8Keep; set { _settings.IsCol8Keep = value; OnPropertyChanged(); } }

        private int _copyCount = 1;
        public int CopyCount { get => _copyCount; set => SetProperty(ref _copyCount, Math.Max(1, Math.Min(999, value))); }

        private bool? _isAllRecordsChecked = false;
        public bool? IsAllRecordsChecked
        {
            get => _isAllRecordsChecked;
            set
            {
                if (SetProperty(ref _isAllRecordsChecked, value) && value.HasValue)
                {
                    foreach (var r in Records)
                    {
                        r.IsChecked = value.Value;
                    }
                }
            }
        }

        public int SinglePrintCopies { get => _settings.SinglePrintCopies > 0 ? _settings.SinglePrintCopies : 1; set { _settings.SinglePrintCopies = Math.Max(1, Math.Min(999, value)); OnPropertyChanged(); _settings.Save(); } }
        public int ExcelPrintCopies { get => _settings.ExcelPrintCopies > 0 ? _settings.ExcelPrintCopies : 1; set { _settings.ExcelPrintCopies = Math.Max(1, Math.Min(999, value)); OnPropertyChanged(); _settings.Save(); } }

        public string PostPrintAction
        {
            get => _settings.PostPrintAction ?? "DEFAULT";
            set
            {
                if (_settings.PostPrintAction != value)
                {
                    _settings.PostPrintAction = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCutOptionsVisible));
                    _settings.Save();
                }
            }
        }

        public bool IsCutOptionsVisible => PostPrintAction == "CUTTER";

        public string PostPrintCutTrigger
        {
            get => _settings.PostPrintCutTrigger ?? "EVERY_PAGE";
            set
            {
                if (_settings.PostPrintCutTrigger != value)
                {
                    _settings.PostPrintCutTrigger = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCutIntervalVisible));
                    _settings.Save();
                }
            }
        }

        public bool IsCutIntervalVisible => IsCutOptionsVisible && PostPrintCutTrigger == "CUSTOM_INTERVAL";
        public bool IsCutIntervalEnabled => PostPrintCutTrigger == "CUSTOM_INTERVAL";

        public int PostPrintCutInterval
        {
            get => _settings.PostPrintCutInterval > 0 ? _settings.PostPrintCutInterval : 1;
            set
            {
                if (_settings.PostPrintCutInterval != value)
                {
                    _settings.PostPrintCutInterval = Math.Max(1, Math.Min(999, value));
                    OnPropertyChanged();
                    _settings.Save();
                }
            }
        }

        public int PostPrintOffset
        {
            get => _settings.PostPrintOffset;
            set
            {
                if (_settings.PostPrintOffset != value)
                {
                    _settings.PostPrintOffset = Math.Max(-30, Math.Min(30, value));
                    OnPropertyChanged();
                    _settings.Save();
                }
            }
        }

        private ObservableCollection<KeyValuePair<string, string>> _printerHardwareDetailsList = new ObservableCollection<KeyValuePair<string, string>>();
        public ObservableCollection<KeyValuePair<string, string>> PrinterHardwareDetailsList
        {
            get => _printerHardwareDetailsList;
            set => SetProperty(ref _printerHardwareDetailsList, value);
        }

        private string _printerHardwareSummaryText = "ℹ️ [프린터 정보 읽기] 버튼을 누르면 상세 하드웨어 스펙이 표시됩니다.";
        public string PrinterHardwareSummaryText
        {
            get => _printerHardwareSummaryText;
            set => SetProperty(ref _printerHardwareSummaryText, value);
        }

        private string _statusMessage = "준비 완료";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private string _printerStatusBadge = "🔴 오프라인";
        public string PrinterStatusBadge { get => _printerStatusBadge; set => SetProperty(ref _printerStatusBadge, value); }

        private bool _isInitializing = false;
        private string _printerStatusColor = "#D32F2F";
        public string PrinterStatusColor { get => _printerStatusColor; set => SetProperty(ref _printerStatusColor, value); }

        private string _selectedPrinterName = "";
        public string SelectedPrinterName
        {
            get => _selectedPrinterName;
            set
            {
                if (SetProperty(ref _selectedPrinterName, value))
                {
                    if (!_isInitializing)
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            ExecuteReadPrinterHardwareInfo(showLoadingPopup: true);
                        }
                        else
                        {
                            CheckRealtimePrinterStatus();
                        }
                    }
                }
            }
        }

        public string PrinterConnectionType
        {
            get => _settings.PrinterConnectionType ?? "USB";
            set
            {
                if (_settings.PrinterConnectionType != value)
                {
                    _settings.PrinterConnectionType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsConnectionUsb));
                    OnPropertyChanged(nameof(IsConnectionLan));
                    OnPropertyChanged(nameof(IsConnectionBluetooth));
                    _settings.Save();
                    RefreshPrinters();
                }
            }
        }

        public bool IsConnectionUsb => PrinterConnectionType == "USB";
        public bool IsConnectionLan => PrinterConnectionType == "LAN";
        public bool IsConnectionBluetooth => PrinterConnectionType == "BLUETOOTH";

        public string TcpPrinterIp
        {
            get => _settings.TcpPrinterIp ?? "192.168.0.50";
            set { _settings.TcpPrinterIp = value; OnPropertyChanged(); _settings.Save(); }
        }

        public int TcpPrinterPort
        {
            get => _settings.TcpPrinterPort > 0 ? _settings.TcpPrinterPort : 9100;
            set { _settings.TcpPrinterPort = value; OnPropertyChanged(); _settings.Save(); }
        }

        public string BluetoothPortName
        {
            get => _settings.BluetoothPortName ?? "COM3";
            set { _settings.BluetoothPortName = value; OnPropertyChanged(); _settings.Save(); }
        }

        public List<string> AvailableComPorts => UsbPrinterHelper.GetAvailableComPorts();

        private bool _isScanningNetwork = false;
        public bool IsScanningNetwork
        {
            get => _isScanningNetwork;
            set => SetProperty(ref _isScanningNetwork, value);
        }

        private bool _isScanningBluetooth = false;
        public bool IsScanningBluetooth
        {
            get => _isScanningBluetooth;
            set => SetProperty(ref _isScanningBluetooth, value);
        }

        private ImageSource _previewImage;
        public ImageSource PreviewImage { get => _previewImage; set => SetProperty(ref _previewImage, value); }

        public ObservableCollection<PrintRecordModel> Records { get; } = new ObservableCollection<PrintRecordModel>();
        public ObservableCollection<string> AvailablePrinters { get; } = new ObservableCollection<string>();

        private PrintRecordModel _selectedRecord;
        public PrintRecordModel SelectedRecord
        {
            get => _selectedRecord;
            set
            {
                if (_selectedRecord != null)
                {
                    _selectedRecord.PropertyChanged -= OnSelectedRecordPropertyChanged;
                }

                if (SetProperty(ref _selectedRecord, value))
                {
                    if (value != null && Records != null)
                    {
                        int idx = Records.IndexOf(value);
                        if (idx >= 0)
                        {
                            _currentPreviewIndex = idx + 1;
                            OnPropertyChanged(nameof(CurrentPreviewIndex));
                            OnPropertyChanged(nameof(PreviewPageText));
                            OnPropertyChanged(nameof(IsLiveInputMode));
                            OnPropertyChanged(nameof(HasPreviousPreviewPage));
                            OnPropertyChanged(nameof(HasNextPreviewPage));
                        }
                        value.PropertyChanged += OnSelectedRecordPropertyChanged;
                    }
                    UpdatePreview();
                }
            }
        }

        private void OnSelectedRecordPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(PrintRecordModel.IsChecked))
            {
                UpdatePreview();
                SaveHistoryRecords();
            }
        }

        #endregion

        public MainViewModel() : this(new DialogService(), new ExcelService(), new TemplateService(), new PrinterService())
        {
        }

        public MainViewModel(IDialogService dialogService, IExcelService excelService, ITemplateService templateService, IPrinterService printerService)
        {
            _isInitializing = true;
            try
            {
                _dialogService = dialogService ?? new DialogService();
                _excelService = excelService ?? new ExcelService();
                _templateService = templateService ?? new TemplateService();
                _printerService = printerService ?? new PrinterService();

                _settings = new AppSettings();
                _settings.Load();

                LoadAuthenticationSerials();
                LoadHistoryRecords();

                InitializeCommands();

                // 저장된 마지막 연결 방식 복원
                if (!string.IsNullOrEmpty(_settings.LastSelectedConnectionType))
                {
                    _selectedConnectionType = _settings.LastSelectedConnectionType;
                }

                RefreshPrinters();

                // 저장된 마지막 프린터 이름 복원
                if (!string.IsNullOrEmpty(_settings.LastSelectedPrinterName) && AvailablePrinters.Contains(_settings.LastSelectedPrinterName))
                {
                    SelectedPrinterName = _settings.LastSelectedPrinterName;
                }

                UpdatePreview();
            }
            finally
            {
                _isInitializing = false;
            }

            // 실시간 프린터 상태 백그라운드 감지 타이머 (3초 주기)
            StartRealtimeStatusMonitoring();
        }

        private System.Windows.Threading.DispatcherTimer _realtimeStatusTimer;

        private void StartRealtimeStatusMonitoring()
        {
            try
            {
                _realtimeStatusTimer = new System.Windows.Threading.DispatcherTimer();
                _realtimeStatusTimer.Interval = TimeSpan.FromSeconds(3);
                _realtimeStatusTimer.Tick += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(SelectedPrinterName) && AvailablePrinters.Contains(SelectedPrinterName))
                    {
                        CheckRealtimePrinterStatus();
                    }
                };
                _realtimeStatusTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"실시간 상태 모니터링 타이머 시작 실패: {ex.Message}");
            }
        }

        private void LoadAuthenticationSerials()
        {
            try
            {
                _authenticatedSerials = SerialCryptoManager.LoadSerials() ?? new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"시리얼 번호 로드 실패: {ex.Message}");
                _authenticatedSerials = new List<string>();
            }
        }

        public void SaveSettingsSilently()
        {
            try
            {
                _settings.Save();
                SaveHistoryRecords();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSettingsSilently Error: {ex.Message}");
            }
        }

        public void ExecuteSaveSettings()
        {
            try
            {
                _settings.Save();
                StatusMessage = "모든 설정 파일이 성공적으로 저장되었습니다.";
                _dialogService.ShowMessage("설정 내용이 저장되었습니다.", "저장 완료", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"설정 저장 실패: {ex.Message}";
            }
        }

        public void ExecuteExportTemplate()
        {
            string defaultName = $"온팔로우_템플릿_{PaperWidth}x{PaperHeight}_{DateTime.Now:yyyyMMdd}.lbm";
            if (_dialogService.ShowSaveFileDialog("LabelMaster 템플릿 (*.lbm)|*.lbm|모든 파일 (*.*)|*.*", ".lbm", defaultName, out string filePath))
            {
                if (_templateService.ExportTemplate(_settings, filePath, out string err))
                {
                    StatusMessage = $"템플릿 저장 완료: {Path.GetFileName(filePath)}";
                    _dialogService.ShowMessage($"라벨 템플릿 파일이 저장되었습니다.\n\n경로: {filePath}", "템플릿 저장 완료", MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"템플릿 저장 실패: {err}";
                    _dialogService.ShowError($"템플릿 저장 실패: {err}", "오류");
                }
            }
        }

        public void ExecuteImportTemplate()
        {
            if (_dialogService.ShowOpenFileDialog("LabelMaster 템플릿 (*.lbm)|*.lbm|모든 파일 (*.*)|*.*", ".lbm", out string filePath))
            {
                var loaded = _templateService.ImportTemplate(filePath, out string err);
                if (loaded != null)
                {
                    _settings = loaded;
                    _settings.Save();

                    OnPropertyChanged("");
                    ActiveColumnCountChanged?.Invoke(ActiveColumnCount);

                    UpdatePreview();
                    RequestPreviewRedraw?.Invoke();

                    StatusMessage = $"템플릿 로드 완료: {Path.GetFileName(filePath)}";
                    _dialogService.ShowMessage($"라벨 템플릿을 성공적으로 불러왔습니다.\n\n파일명: {Path.GetFileName(filePath)}", "템플릿 불러오기 완료", MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"템플릿 불러오기 실패: {err}";
                    _dialogService.ShowError($"템플릿 불러오기 실패: {err}", "오류");
                }
            }
        }

        #region Column Helper Operations

        public void RemoveColumnAt(int colIndex)
        {
            if (ActiveColumnCount <= 1)
            {
                _dialogService.ShowMessage("최소 1개의 컬럼은 유지되어야 합니다.", "알림", MessageBoxImage.Information);
                return;
            }

            if (colIndex < 1 || colIndex > ActiveColumnCount) return;

            string targetName = GetHeaderByIndex(colIndex);
            if (!_dialogService.ShowQuestion($"[{colIndex}번 컬럼 '{targetName}']을(를) 삭제하시겠습니까?\n삭제 시 뒤쪽 컬럼들이 앞으로 자동 정렬됩니다.", "컬럼 지정 삭제"))
            {
                return;
            }

            _isBatchUpdating = true;
            try
            {
                for (int i = colIndex; i < 8; i++)
                {
                    CopyColumnData(from: i + 1, to: i);
                }
                ResetColumnData(8);

                if (Records != null && Records.Count > 0)
                {
                    foreach (var rec in Records)
                    {
                        ShiftRecordColumns(rec, colIndex);
                    }
                    SaveHistoryRecords();
                }

                ActiveColumnCount--;
            }
            finally
            {
                _isBatchUpdating = false;
            }

            _settings.Save();
            StatusMessage = $"[{colIndex}번 컬럼 '{targetName}']이(가) 삭제되었습니다. (남은 컬럼: {ActiveColumnCount}개)";
            UpdatePreview();
        }

        public void MoveColumnUp(int colIndex)
        {
            if (colIndex <= 1 || colIndex > ActiveColumnCount) return;
            SwapColumns(colIndex, colIndex - 1);
            StatusMessage = $"[{colIndex}번 컬럼]을(를) {colIndex - 1}번 위치로 위로 이동했습니다.";
        }

        public void MoveColumnDown(int colIndex)
        {
            if (colIndex < 1 || colIndex >= ActiveColumnCount) return;
            SwapColumns(colIndex, colIndex + 1);
            StatusMessage = $"[{colIndex}번 컬럼]을(를) {colIndex + 1}번 위치로 아래로 이동했습니다.";
        }

        public void SwapColumns(int a, int b)
        {
            if (a < 1 || a > 8 || b < 1 || b > 8 || a == b) return;

            _isBatchUpdating = true;
            try
            {
                string tempHeader = GetHeaderByIndex(a);
                bool tempPrice = GetPriceByIndex(a);
                bool tempIncludeHeader = GetIncludeHeaderByIndex(a);
                bool tempBarcode = GetBarcodeByIndex(a);
                string tempBType = GetBarcodeTypeByIndex(a);
                bool tempBText = GetBarcodeTextByIndex(a);
                int tempSize = GetSizeByIndex(a);
                int tempX = GetOffsetXByIndex(a);
                int tempY = GetOffsetYByIndex(a);
                string tempInput = GetInputByIndex(a);

                SetColumnDataByIndex(a, 
                    GetHeaderByIndex(b), 
                    GetPriceByIndex(b), 
                    GetIncludeHeaderByIndex(b),
                    GetBarcodeByIndex(b), 
                    GetBarcodeTypeByIndex(b), 
                    GetBarcodeTextByIndex(b),
                    GetSizeByIndex(b), 
                    GetOffsetXByIndex(b), 
                    GetOffsetYByIndex(b), 
                    GetInputByIndex(b));

                SetColumnDataByIndex(b, 
                    tempHeader, 
                    tempPrice, 
                    tempIncludeHeader, 
                    tempBarcode, 
                    tempBType, 
                    tempBText, 
                    tempSize, 
                    tempX, 
                    tempY, 
                    tempInput);

                if (Records != null && Records.Count > 0)
                {
                    foreach (var rec in Records)
                    {
                        SwapRecordColumns(rec, a, b);
                    }
                    SaveHistoryRecords();
                }
            }
            finally
            {
                _isBatchUpdating = false;
            }

            _settings.Save();
            UpdatePreview();
        }

        public void AddNewColumn()
        {
            if (ActiveColumnCount >= 8) return;
            int newIndex = ActiveColumnCount + 1;

            // 1. 헤더명을 항목{newIndex}로 확실히 지정
            string currentHeader = GetHeaderByIndex(newIndex);
            if (string.IsNullOrWhiteSpace(currentHeader) || currentHeader.StartsWith("항목"))
            {
                SetColumnHeaderByIndex(newIndex, $"항목{newIndex}");
            }

            // 2. Y 오프셋을 용지 인쇄 영역 내부로 안전하게 배치
            int paperH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 40;
            int lastY = GetOffsetYByIndex(ActiveColumnCount);
            int newY = Math.Min(paperH - 6, lastY + 5);
            if (newY >= paperH - 4 || newY <= 0)
            {
                newY = Math.Max(2, (newIndex - 1) * (paperH / Math.Max(2, newIndex + 1)));
            }
            SetItemOffsetDirect(newIndex - 1, 2, newY, triggerPreview: false);

            ActiveColumnCount = newIndex;

            if (IsTableBorderEnabled)
            {
                ApplyTableAutoFitLayoutInternal();
            }

            StatusMessage = $"[컬럼 {newIndex} ('{GetHeaderByIndex(newIndex)}')]이(가) 추가되었습니다.";
            UpdatePreview();
        }

        private void CopyColumnData(int from, int to)
        {
            SetColumnDataByIndex(to, 
                GetHeaderByIndex(from), 
                GetPriceByIndex(from), 
                GetIncludeHeaderByIndex(from),
                GetBarcodeByIndex(from), 
                GetBarcodeTypeByIndex(from), 
                GetBarcodeTextByIndex(from),
                GetSizeByIndex(from), 
                GetOffsetXByIndex(from), 
                GetOffsetYByIndex(from), 
                GetInputByIndex(from));
        }

        private void ResetColumnData(int idx)
        {
            int paperH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 40;
            int safeY = Math.Max(2, Math.Min(paperH - 6, (idx - 1) * 4 + 2));
            SetColumnDataByIndex(idx, $"항목{idx}", false, false, false, "CODE128", true, 12, 2, safeY, "");
        }

        private void ShiftRecordColumns(PrintRecordModel rec, int fromColIndex)
        {
            for (int i = fromColIndex; i < 8; i++)
            {
                SetRecordCol(rec, i, GetRecordCol(rec, i + 1));
            }
            SetRecordCol(rec, 8, "");
        }

        private void SwapRecordColumns(PrintRecordModel rec, int a, int b)
        {
            string temp = GetRecordCol(rec, a);
            SetRecordCol(rec, a, GetRecordCol(rec, b));
            SetRecordCol(rec, b, temp);
        }

        private string GetRecordCol(PrintRecordModel rec, int idx)
        {
            switch (idx)
            {
                case 1: return rec.Col1;
                case 2: return rec.Col2;
                case 3: return rec.Col3;
                case 4: return rec.Col4;
                case 5: return rec.Col5;
                case 6: return rec.Col6;
                case 7: return rec.Col7;
                case 8: return rec.Col8;
                default: return "";
            }
        }

        private void SetRecordCol(PrintRecordModel rec, int idx, string val)
        {
            switch (idx)
            {
                case 1: rec.Col1 = val; break;
                case 2: rec.Col2 = val; break;
                case 3: rec.Col3 = val; break;
                case 4: rec.Col4 = val; break;
                case 5: rec.Col5 = val; break;
                case 6: rec.Col6 = val; break;
                case 7: rec.Col7 = val; break;
                case 8: rec.Col8 = val; break;
            }
        }

        public void SetColumnHeaderByIndex(int idx, string name)
        {
            switch (idx)
            {
                case 1: Column1Header = name; break;
                case 2: Column2Header = name; break;
                case 3: Column3Header = name; break;
                case 4: Column4Header = name; break;
                case 5: Column5Header = name; break;
                case 6: Column6Header = name; break;
                case 7: Column7Header = name; break;
                case 8: Column8Header = name; break;
            }
            _settings.Save();
            UpdatePreview();
        }

        public void SetColumnDataByIndex(int idx, string header, bool isPrice, bool isIncludeHeader, bool isBarcode, string bType, bool bText, int size, int x, int y, string input)
        {
            switch (idx)
            {
                case 1:
                    _settings.Column1Name = header; _settings.IsColumn1PriceFormat = isPrice; _settings.IsColumn1IncludeHeader = isIncludeHeader;
                    _settings.IsColumn1Barcode = isBarcode; _settings.Col1BarcodeType = bType; _settings.Col1ShowBarcodeText = bText;
                    _settings.Col1Size = size; _settings.Col1OffsetX = x; _settings.Col1OffsetY = y; _inputCol1 = input;
                    break;
                case 2:
                    _settings.Column2Name = header; _settings.IsColumn2PriceFormat = isPrice; _settings.IsColumn2IncludeHeader = isIncludeHeader;
                    _settings.IsColumn2Barcode = isBarcode; _settings.Col2BarcodeType = bType; _settings.Col2ShowBarcodeText = bText;
                    _settings.Col2Size = size; _settings.Col2OffsetX = x; _settings.Col2OffsetY = y; _inputCol2 = input;
                    break;
                case 3:
                    _settings.Column3Name = header; _settings.IsColumn3PriceFormat = isPrice; _settings.IsColumn3IncludeHeader = isIncludeHeader;
                    _settings.IsColumn3Barcode = isBarcode; _settings.Col3BarcodeType = bType; _settings.Col3ShowBarcodeText = bText;
                    _settings.Col3Size = size; _settings.Col3OffsetX = x; _settings.Col3OffsetY = y; _inputCol3 = input;
                    break;
                case 4:
                    _settings.Column4Name = header; _settings.IsColumn4PriceFormat = isPrice; _settings.IsColumn4IncludeHeader = isIncludeHeader;
                    _settings.IsColumn4Barcode = isBarcode; _settings.Col4BarcodeType = bType; _settings.Col4ShowBarcodeText = bText;
                    _settings.Col4Size = size; _settings.Col4OffsetX = x; _settings.Col4OffsetY = y; _inputCol4 = input;
                    break;
                case 5:
                    _settings.Column5Name = header; _settings.IsColumn5PriceFormat = isPrice; _settings.IsColumn5IncludeHeader = isIncludeHeader;
                    _settings.IsColumn5Barcode = isBarcode; _settings.Col5BarcodeType = bType; _settings.Col5ShowBarcodeText = bText;
                    _settings.Col5Size = size; _settings.Col5OffsetX = x; _settings.Col5OffsetY = y; _inputCol5 = input;
                    break;
                case 6:
                    _settings.Column6Name = header; _settings.IsColumn6PriceFormat = isPrice; _settings.IsColumn6IncludeHeader = isIncludeHeader;
                    _settings.IsColumn6Barcode = isBarcode; _settings.Col6BarcodeType = bType; _settings.Col6ShowBarcodeText = bText;
                    _settings.Col6Size = size; _settings.Col6OffsetX = x; _settings.Col6OffsetY = y; _inputCol6 = input;
                    break;
                case 7:
                    _settings.Column7Name = header; _settings.IsColumn7PriceFormat = isPrice; _settings.IsColumn7IncludeHeader = isIncludeHeader;
                    _settings.IsColumn7Barcode = isBarcode; _settings.Col7BarcodeType = bType; _settings.Col7ShowBarcodeText = bText;
                    _settings.Col7Size = size; _settings.Col7OffsetX = x; _settings.Col7OffsetY = y; _inputCol7 = input;
                    break;
                case 8:
                    _settings.Column8Name = header; _settings.IsColumn8PriceFormat = isPrice; _settings.IsColumn8IncludeHeader = isIncludeHeader;
                    _settings.IsColumn8Barcode = isBarcode; _settings.Col8BarcodeType = bType; _settings.Col8ShowBarcodeText = bText;
                    _settings.Col8Size = size; _settings.Col8OffsetX = x; _settings.Col8OffsetY = y; _inputCol8 = input;
                    break;
            }

            if (!_isBatchUpdating)
            {
                OnPropertyChanged("");
                UpdatePreview();
            }
        }

        public string GetHeaderByIndex(int idx)
        {
            switch (idx) { case 1: return Column1Header; case 2: return Column2Header; case 3: return Column3Header; case 4: return Column4Header; case 5: return Column5Header; case 6: return Column6Header; case 7: return Column7Header; case 8: return Column8Header; default: return ""; }
        }
        public bool GetPriceByIndex(int idx)
        {
            switch (idx) { case 1: return IsColumn1Price; case 2: return IsColumn2Price; case 3: return IsColumn3Price; case 4: return IsColumn4Price; case 5: return IsColumn5Price; case 6: return IsColumn6Price; case 7: return IsColumn7Price; case 8: return IsColumn8Price; default: return false; }
        }
        public bool GetIncludeHeaderByIndex(int idx)
        {
            switch (idx) { case 1: return IsIncludeHeader1; case 2: return IsIncludeHeader2; case 3: return IsIncludeHeader3; case 4: return IsIncludeHeader4; case 5: return IsIncludeHeader5; case 6: return IsIncludeHeader6; case 7: return IsIncludeHeader7; case 8: return IsIncludeHeader8; default: return false; }
        }
        public bool GetBarcodeByIndex(int idx)
        {
            switch (idx) { case 1: return IsColumn1Barcode; case 2: return IsColumn2Barcode; case 3: return IsColumn3Barcode; case 4: return IsColumn4Barcode; case 5: return IsColumn5Barcode; case 6: return IsColumn6Barcode; case 7: return IsColumn7Barcode; case 8: return IsColumn8Barcode; default: return false; }
        }
        public string GetBarcodeTypeByIndex(int idx)
        {
            switch (idx) { case 1: return Col1BarcodeType; case 2: return Col2BarcodeType; case 3: return Col3BarcodeType; case 4: return Col4BarcodeType; case 5: return Col5BarcodeType; case 6: return Col6BarcodeType; case 7: return Col7BarcodeType; case 8: return Col8BarcodeType; default: return "CODE128"; }
        }
        public bool GetBarcodeTextByIndex(int idx)
        {
            switch (idx) { case 1: return Col1ShowBarcodeText; case 2: return Col2ShowBarcodeText; case 3: return Col3ShowBarcodeText; case 4: return Col4ShowBarcodeText; case 5: return Col5ShowBarcodeText; case 6: return Col6ShowBarcodeText; case 7: return Col7ShowBarcodeText; case 8: return Col8ShowBarcodeText; default: return true; }
        }
        public string GetFontByIndex(int idx)
        {
            switch (idx)
            {
                case 1: return Col1Font;
                case 2: return Col2Font;
                case 3: return Col3Font;
                case 4: return Col4Font;
                case 5: return Col5Font;
                case 6: return Col6Font;
                case 7: return Col7Font;
                case 8: return Col8Font;
                case 99: return DateTimeFont;
                case 100: return TableTitleFont;
                case 101: return SerialFont;
                default: return "맑은 고딕";
            }
        }
        public bool GetBoldByIndex(int idx)
        {
            switch (idx)
            {
                case 1: return Col1Bold;
                case 2: return Col2Bold;
                case 3: return Col3Bold;
                case 4: return Col4Bold;
                case 5: return Col5Bold;
                case 6: return Col6Bold;
                case 7: return Col7Bold;
                case 8: return Col8Bold;
                case 99: return DateTimeBold;
                case 100: return TableTitleBold;
                case 101: return SerialBold;
                default: return true;
            }
        }
        public bool GetItalicByIndex(int idx)
        {
            switch (idx)
            {
                case 1: return Col1Italic;
                case 2: return Col2Italic;
                case 3: return Col3Italic;
                case 4: return Col4Italic;
                case 5: return Col5Italic;
                case 6: return Col6Italic;
                case 7: return Col7Italic;
                case 8: return Col8Italic;
                case 99: return DateTimeItalic;
                case 100: return TableTitleItalic;
                case 101: return SerialItalic;
                default: return false;
            }
        }
        public int GetSizeByIndex(int idx)
        {
            switch (idx)
            {
                case 1: return Col1Size;
                case 2: return Col2Size;
                case 3: return Col3Size;
                case 4: return Col4Size;
                case 5: return Col5Size;
                case 6: return Col6Size;
                case 7: return Col7Size;
                case 8: return Col8Size;
                case 99: return DateTimeSize;
                case 100: return TableTitleSize;
                case 101: return SerialSize;
                default: return 12;
            }
        }
        public int GetOffsetXByIndex(int idx)
        {
            switch (idx)
            {
                case 1: return Col1OffsetX;
                case 2: return Col2OffsetX;
                case 3: return Col3OffsetX;
                case 4: return Col4OffsetX;
                case 5: return Col5OffsetX;
                case 6: return Col6OffsetX;
                case 7: return Col7OffsetX;
                case 8: return Col8OffsetX;
                case 99: return DateTimeOffsetX;
                case 100: return TableTitleOffsetX;
                case 101: return SerialOffsetX;
                default: return 2;
            }
        }
        public void SetOffsetXByIndex(int idx, int x)
        {
            switch (idx)
            {
                case 1: Col1OffsetX = x; break;
                case 2: Col2OffsetX = x; break;
                case 3: Col3OffsetX = x; break;
                case 4: Col4OffsetX = x; break;
                case 5: Col5OffsetX = x; break;
                case 6: Col6OffsetX = x; break;
                case 7: Col7OffsetX = x; break;
                case 8: Col8OffsetX = x; break;
                case 99: DateTimeOffsetX = x; break;
                case 100: TableTitleOffsetX = x; break;
                case 101: SerialOffsetX = x; break;
            }
        }
        public int GetOffsetYByIndex(int idx)
        {
            switch (idx)
            {
                case 1: return Col1OffsetY;
                case 2: return Col2OffsetY;
                case 3: return Col3OffsetY;
                case 4: return Col4OffsetY;
                case 5: return Col5OffsetY;
                case 6: return Col6OffsetY;
                case 7: return Col7OffsetY;
                case 8: return Col8OffsetY;
                case 99: return DateTimeOffsetY;
                case 100: return TableTitleOffsetY;
                case 101: return SerialOffsetY;
                default: return 2;
            }
        }
        public void SetOffsetYByIndex(int idx, int y)
        {
            switch (idx)
            {
                case 1: Col1OffsetY = y; break;
                case 2: Col2OffsetY = y; break;
                case 3: Col3OffsetY = y; break;
                case 4: Col4OffsetY = y; break;
                case 5: Col5OffsetY = y; break;
                case 6: Col6OffsetY = y; break;
                case 7: Col7OffsetY = y; break;
                case 8: Col8OffsetY = y; break;
                case 99: DateTimeOffsetY = y; break;
                case 100: TableTitleOffsetY = y; break;
                case 101: SerialOffsetY = y; break;
            }
        }
        public int GetRowHeightByIndex(int idx)
        {
            switch (idx) { case 1: return Col1RowHeight; case 2: return Col2RowHeight; case 3: return Col3RowHeight; case 4: return Col4RowHeight; case 5: return Col5RowHeight; case 6: return Col6RowHeight; case 7: return Col7RowHeight; case 8: return Col8RowHeight; default: return 0; }
        }
        public void SetRowHeightByIndex(int idx, int heightMm)
        {
            switch (idx)
            {
                case 1: Col1RowHeight = heightMm; break;
                case 2: Col2RowHeight = heightMm; break;
                case 3: Col3RowHeight = heightMm; break;
                case 4: Col4RowHeight = heightMm; break;
                case 5: Col5RowHeight = heightMm; break;
                case 6: Col6RowHeight = heightMm; break;
                case 7: Col7RowHeight = heightMm; break;
                case 8: Col8RowHeight = heightMm; break;
            }
        }
        public int GetWidthByIndex(int idx)
        {
            switch (idx)
            {
                case 1: return Col1Width;
                case 2: return Col2Width;
                case 3: return Col3Width;
                case 4: return Col4Width;
                case 5: return Col5Width;
                case 6: return Col6Width;
                case 7: return Col7Width;
                case 8: return Col8Width;
                case 99: return DateTimeWidth;
                case 101: return SerialWidth;
                default: return 0;
            }
        }
        public void SetWidthByIndex(int idx, int widthMm)
        {
            switch (idx)
            {
                case 1: Col1Width = widthMm; break;
                case 2: Col2Width = widthMm; break;
                case 3: Col3Width = widthMm; break;
                case 4: Col4Width = widthMm; break;
                case 5: Col5Width = widthMm; break;
                case 6: Col6Width = widthMm; break;
                case 7: Col7Width = widthMm; break;
                case 8: Col8Width = widthMm; break;
                case 99: DateTimeWidth = widthMm; break;
                case 101: SerialWidth = widthMm; break;
            }
        }
        public string GetAlignByIndex(int idx)
        {
            switch (idx)
            {
                case 1: return _settings.Col1Align ?? "LEFT";
                case 2: return _settings.Col2Align ?? "LEFT";
                case 3: return _settings.Col3Align ?? "LEFT";
                case 4: return _settings.Col4Align ?? "LEFT";
                case 5: return _settings.Col5Align ?? "LEFT";
                case 6: return _settings.Col6Align ?? "LEFT";
                case 7: return _settings.Col7Align ?? "LEFT";
                case 8: return _settings.Col8Align ?? "LEFT";
                case 99: return "LEFT";
                case 100: return TableTitleAlign;
                case 101: return SerialAlign;
                default: return "LEFT";
            }
        }
        public void SetAlignByIndex(int idx, string align)
        {
            switch (idx)
            {
                case 1: _settings.Col1Align = align; break;
                case 2: _settings.Col2Align = align; break;
                case 3: _settings.Col3Align = align; break;
                case 4: _settings.Col4Align = align; break;
                case 5: _settings.Col5Align = align; break;
                case 6: _settings.Col6Align = align; break;
                case 7: _settings.Col7Align = align; break;
                case 8: _settings.Col8Align = align; break;
                case 100: TableTitleAlign = align; break;
                case 101: SerialAlign = align; break;
            }
            _settings.Save();
            OnPropertyChanged();
            UpdatePreview();
        }
        public string GetHeaderAlignByIndex(int idx)
        {
            switch (idx) { case 1: return _settings.Col1HeaderAlign ?? "CENTER"; case 2: return _settings.Col2HeaderAlign ?? "CENTER"; case 3: return _settings.Col3HeaderAlign ?? "CENTER"; case 4: return _settings.Col4HeaderAlign ?? "CENTER"; case 5: return _settings.Col5HeaderAlign ?? "CENTER"; case 6: return _settings.Col6HeaderAlign ?? "CENTER"; case 7: return _settings.Col7HeaderAlign ?? "CENTER"; case 8: return _settings.Col8HeaderAlign ?? "CENTER"; default: return "CENTER"; }
        }
        public void SetHeaderAlignByIndex(int idx, string align)
        {
            switch (idx)
            {
                case 1: _settings.Col1HeaderAlign = align; break;
                case 2: _settings.Col2HeaderAlign = align; break;
                case 3: _settings.Col3HeaderAlign = align; break;
                case 4: _settings.Col4HeaderAlign = align; break;
                case 5: _settings.Col5HeaderAlign = align; break;
                case 6: _settings.Col6HeaderAlign = align; break;
                case 7: _settings.Col7HeaderAlign = align; break;
                case 8: _settings.Col8HeaderAlign = align; break;
            }
            _settings.Save();
            OnPropertyChanged();
            UpdatePreview();
        }
        public void SetHeaderByIndex(int idx, string header)
        {
            switch (idx)
            {
                case 1: Column1Header = header; break;
                case 2: Column2Header = header; break;
                case 3: Column3Header = header; break;
                case 4: Column4Header = header; break;
                case 5: Column5Header = header; break;
                case 6: Column6Header = header; break;
                case 7: Column7Header = header; break;
                case 8: Column8Header = header; break;
            }
        }

        public void SetFontByIndex(int idx, string font)
        {
            switch (idx)
            {
                case 1: Col1Font = font; break;
                case 2: Col2Font = font; break;
                case 3: Col3Font = font; break;
                case 4: Col4Font = font; break;
                case 5: Col5Font = font; break;
                case 6: Col6Font = font; break;
                case 7: Col7Font = font; break;
                case 8: Col8Font = font; break;
                case 99: DateTimeFont = font; break;
                case 100: TableTitleFont = font; break;
                case 101: SerialFont = font; break;
            }
        }

        public void SetSizeByIndex(int idx, int size)
        {
            switch (idx)
            {
                case 1: Col1Size = size; break;
                case 2: Col2Size = size; break;
                case 3: Col3Size = size; break;
                case 4: Col4Size = size; break;
                case 5: Col5Size = size; break;
                case 6: Col6Size = size; break;
                case 7: Col7Size = size; break;
                case 8: Col8Size = size; break;
                case 99: DateTimeSize = size; break;
                case 100: TableTitleSize = size; break;
                case 101: SerialSize = size; break;
            }
        }

        public void SetBoldByIndex(int idx, bool bold)
        {
            switch (idx)
            {
                case 1: Col1Bold = bold; break;
                case 2: Col2Bold = bold; break;
                case 3: Col3Bold = bold; break;
                case 4: Col4Bold = bold; break;
                case 5: Col5Bold = bold; break;
                case 6: Col6Bold = bold; break;
                case 7: Col7Bold = bold; break;
                case 8: Col8Bold = bold; break;
                case 99: DateTimeBold = bold; break;
                case 100: TableTitleBold = bold; break;
                case 101: SerialBold = bold; break;
            }
        }

        public void SetItalicByIndex(int idx, bool italic)
        {
            switch (idx)
            {
                case 1: Col1Italic = italic; break;
                case 2: Col2Italic = italic; break;
                case 3: Col3Italic = italic; break;
                case 4: Col4Italic = italic; break;
                case 5: Col5Italic = italic; break;
                case 6: Col6Italic = italic; break;
                case 7: Col7Italic = italic; break;
                case 8: Col8Italic = italic; break;
                case 99: DateTimeItalic = italic; break;
                case 100: TableTitleItalic = italic; break;
                case 101: SerialItalic = italic; break;
            }
        }

        public void SetIncludeHeaderByIndex(int idx, bool include)
        {
            switch (idx)
            {
                case 1: IsIncludeHeader1 = include; break;
                case 2: IsIncludeHeader2 = include; break;
                case 3: IsIncludeHeader3 = include; break;
                case 4: IsIncludeHeader4 = include; break;
                case 5: IsIncludeHeader5 = include; break;
                case 6: IsIncludeHeader6 = include; break;
                case 7: IsIncludeHeader7 = include; break;
                case 8: IsIncludeHeader8 = include; break;
            }
        }

        public void SetPriceByIndex(int idx, bool price)
        {
            switch (idx)
            {
                case 1: IsColumn1Price = price; break;
                case 2: IsColumn2Price = price; break;
                case 3: IsColumn3Price = price; break;
                case 4: IsColumn4Price = price; break;
                case 5: IsColumn5Price = price; break;
                case 6: IsColumn6Price = price; break;
                case 7: IsColumn7Price = price; break;
                case 8: IsColumn8Price = price; break;
            }
        }

        public void SetBarcodeByIndex(int idx, bool barcode)
        {
            switch (idx)
            {
                case 1: IsColumn1Barcode = barcode; break;
                case 2: IsColumn2Barcode = barcode; break;
                case 3: IsColumn3Barcode = barcode; break;
                case 4: IsColumn4Barcode = barcode; break;
                case 5: IsColumn5Barcode = barcode; break;
                case 6: IsColumn6Barcode = barcode; break;
                case 7: IsColumn7Barcode = barcode; break;
                case 8: IsColumn8Barcode = barcode; break;
            }
        }

        public void SetBarcodeTypeByIndex(int idx, string bType)
        {
            switch (idx)
            {
                case 1: Col1BarcodeType = bType; break;
                case 2: Col2BarcodeType = bType; break;
                case 3: Col3BarcodeType = bType; break;
                case 4: Col4BarcodeType = bType; break;
                case 5: Col5BarcodeType = bType; break;
                case 6: Col6BarcodeType = bType; break;
                case 7: Col7BarcodeType = bType; break;
                case 8: Col8BarcodeType = bType; break;
            }
        }

        public void SetBarcodeTextByIndex(int idx, bool showText)
        {
            switch (idx)
            {
                case 1: Col1ShowBarcodeText = showText; break;
                case 2: Col2ShowBarcodeText = showText; break;
                case 3: Col3ShowBarcodeText = showText; break;
                case 4: Col4ShowBarcodeText = showText; break;
                case 5: Col5ShowBarcodeText = showText; break;
                case 6: Col6ShowBarcodeText = showText; break;
                case 7: Col7ShowBarcodeText = showText; break;
                case 8: Col8ShowBarcodeText = showText; break;
            }
        }

        public string GetInputByIndex(int idx)
        {
            switch (idx) { case 1: return InputCol1; case 2: return InputCol2; case 3: return InputCol3; case 4: return InputCol4; case 5: return InputCol5; case 6: return InputCol6; case 7: return InputCol7; case 8: return InputCol8; default: return ""; }
        }

        public void ClearInputs(bool forceAll = false)
        {
            if (forceAll || !IsCol1Keep) InputCol1 = "";
            if (forceAll || !IsCol2Keep) InputCol2 = "";
            if (forceAll || !IsCol3Keep) InputCol3 = "";
            if (forceAll || !IsCol4Keep) InputCol4 = "";
            if (forceAll || !IsCol5Keep) InputCol5 = "";
            if (forceAll || !IsCol6Keep) InputCol6 = "";
            if (forceAll || !IsCol7Keep) InputCol7 = "";
            if (forceAll || !IsCol8Keep) InputCol8 = "";
            UpdatePreview();
        }

        public bool IsAllInputsEmpty()
        {
            int count = ActiveColumnCount;
            for (int i = 1; i <= count; i++)
            {
                if (!string.IsNullOrWhiteSpace(GetInputByIndex(i)))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}
