using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BarcodePrint.Model;

namespace BarcodePrint.ViewModel
{
    public partial class MainViewModel
    {
        private LabelTemplateModel _currentTemplate;
        public LabelTemplateModel CurrentTemplate
        {
            get
            {
                if (_currentTemplate == null)
                {
                    InitializeDefaultTemplateFromCurrentSettings();
                }
                return _currentTemplate;
            }
            set
            {
                if (_currentTemplate != value)
                {
                    _currentTemplate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TemplateElements));
                    UpdatePreview();
                }
            }
        }

        public ObservableCollection<TemplateElement> TemplateElements => 
            CurrentTemplate != null ? new ObservableCollection<TemplateElement>(CurrentTemplate.Elements) : new ObservableCollection<TemplateElement>();

        private TemplateElement _selectedElement;
        public TemplateElement SelectedElement
        {
            get => _selectedElement;
            set
            {
                if (_selectedElement != value || (_selectedElement != null && _selectedElements.Count != 1))
                {
                    // 단일 선택 모드로 변경 시 기존 선택 전체 해제 후 해당 요소만 선택
                    if (CurrentTemplate?.Elements != null)
                    {
                        foreach (var el in CurrentTemplate.Elements) el.IsSelected = (el == value);
                    }
                    _selectedElements.Clear();
                    _selectedElement = value;
                    if (_selectedElement != null)
                    {
                        _selectedElement.IsSelected = true;
                        _selectedElements.Add(_selectedElement);
                        if (_selectedElement.Type == ElementType.DATE_TIME && string.IsNullOrWhiteSpace(_selectedElement.DateTimeFormat))
                        {
                            _selectedElement.DateTimeFormat = "yyyy-MM-dd HH:mm";
                        }
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedElementsCount));
                    OnPropertyChanged(nameof(HasMultipleSelectedElements));
                    NotifySelectedElementProperties();
                    UpdatePreview();
                }
            }
        }

        public void NotifySelectedElementProperties()
        {
            OnPropertyChanged(nameof(HasSelectedElement));
            OnPropertyChanged(nameof(HasNoSelectedElement));
            OnPropertyChanged(nameof(SelectedElementTypeText));
            OnPropertyChanged(nameof(IsSelectedElementText));
            OnPropertyChanged(nameof(IsSelectedElementBarcode));
            OnPropertyChanged(nameof(IsSelectedElementShapeOrLine));
            OnPropertyChanged(nameof(IsSelectedElementContentEditable));
            OnPropertyChanged(nameof(SelectedElementContent));
            OnPropertyChanged(nameof(SelectedElementEffectivePreviewText));
            OnPropertyChanged(nameof(SelectedElementFontSize));
            OnPropertyChanged(nameof(SelectedElementFontName));
            OnPropertyChanged(nameof(SelectedElementIsBold));
            OnPropertyChanged(nameof(SelectedElementIsItalic));
            OnPropertyChanged(nameof(SelectedElementIsUnderline));
            OnPropertyChanged(nameof(SelectedElementLetterSpacing));
            OnPropertyChanged(nameof(SelectedElementFontScaleX));
            OnPropertyChanged(nameof(SelectedElementLineSpacing));
            OnPropertyChanged(nameof(SelectedElementAlignment));
            OnPropertyChanged(nameof(SelectedElementBarcodeType));
            OnPropertyChanged(nameof(SelectedElementX));
            OnPropertyChanged(nameof(SelectedElementY));
            OnPropertyChanged(nameof(SelectedElementWidth));
            OnPropertyChanged(nameof(SelectedElementHeight));
            OnPropertyChanged(nameof(SelectedElementShowBarcodeText));
            OnPropertyChanged(nameof(SelectedElementBarcodeTextPosition));
            OnPropertyChanged(nameof(SelectedElementIsFilled));
            OnPropertyChanged(nameof(SelectedElementLineWidth));
            OnPropertyChanged(nameof(IsSelectedElementShape));
            OnPropertyChanged(nameof(IsSelectedElementLine));
            OnPropertyChanged(nameof(IsSelectedElementTable));
            OnPropertyChanged(nameof(SelectedElementTableRows));
            OnPropertyChanged(nameof(SelectedElementTableColumns));
            OnPropertyChanged(nameof(SelectedElementTableLineWidth));
            OnPropertyChanged(nameof(SelectedElementTableIsFilled));
            RefreshTableCells();
            OnPropertyChanged(nameof(SelectedElementShapeType));
            OnPropertyChanged(nameof(SelectedElementShapeName));
            OnPropertyChanged(nameof(SelectedElementLineStyle));
            OnPropertyChanged(nameof(SelectedElementLineName));
            OnPropertyChanged(nameof(AvailableExcelColumnOptions));
            OnPropertyChanged(nameof(SelectedElementExcelColumn));
            OnPropertyChanged(nameof(IsSelectedElementExcelLinked));
            OnPropertyChanged(nameof(IsSelectedElementExcelPending));
            OnPropertyChanged(nameof(IsSelectedElementDateTime));
            OnPropertyChanged(nameof(SelectedElementDateTimeFormat));
            OnPropertyChanged(nameof(SelectedElementAutoUpdateDate));
            OnPropertyChanged(nameof(DateTimeFormatOptions));
            OnPropertyChanged(nameof(SelectedElementIsKeep));
        }

        public bool HasSelectedElement => SelectedElement != null;
        public bool HasNoSelectedElement => SelectedElement == null;

        public string SelectedElementTypeText
        {
            get
            {
                if (SelectedElement == null) return "🏷️ 라벨 인쇄 & 엑셀 연동";
                switch (SelectedElement.Type)
                {
                    case ElementType.TEXT: return "📝 텍스트 서식";
                    case ElementType.BARCODE: return $"🏷️ 1D 바코드 ({SelectedElement.BarcodeType})";
                    case ElementType.QR_CODE: return $"📱 2D 코드 ({SelectedElement.BarcodeType})";
                    case ElementType.LINE:
                        var lineItem = AvailableLineStyles.FirstOrDefault(x => x.Code == SelectedElement.LineStyle);
                        return lineItem != null ? $"{lineItem.Icon} 구분선 ({lineItem.Name})" : $"📏 구분선 ({SelectedElement.LineStyle})";
                    case ElementType.SHAPE:
                        var shapeItem = AvailableShapeTypes.FirstOrDefault(x => x.Code == SelectedElement.ShapeType);
                        return shapeItem != null ? $"{shapeItem.Icon} 도형 ({shapeItem.Name})" : $"📐 도형 ({SelectedElement.ShapeType})";
                    case ElementType.IMAGE: return "🖼️ 이미지";
                    case ElementType.TABLE: return "📊 표";
                    case ElementType.DATE_TIME: return "🕒 날짜/시간";
                    default: return "선택된 요소";
                }
            }
        }

        public bool IsSelectedElementText => SelectedElement != null && (SelectedElement.Type == ElementType.TEXT || SelectedElement.Type == ElementType.DATE_TIME);
        public bool IsSelectedElementDateTime => SelectedElement != null && SelectedElement.Type == ElementType.DATE_TIME;
        public bool IsSelectedElementBarcode => SelectedElement != null && (SelectedElement.Type == ElementType.BARCODE || SelectedElement.Type == ElementType.QR_CODE);
        public bool IsSelectedElementShapeOrLine => SelectedElement != null && (SelectedElement.Type == ElementType.SHAPE || SelectedElement.Type == ElementType.LINE);
        public bool IsSelectedElementShape => SelectedElement != null && SelectedElement.Type == ElementType.SHAPE;
        public bool IsSelectedElementLine => SelectedElement != null && SelectedElement.Type == ElementType.LINE;
        public bool IsSelectedElementTable => SelectedElement != null && SelectedElement.Type == ElementType.TABLE;
        public bool IsSelectedElementContentEditable => SelectedElement != null && 
            (SelectedElement.Type == ElementType.TEXT || SelectedElement.Type == ElementType.BARCODE || SelectedElement.Type == ElementType.QR_CODE || SelectedElement.Type == ElementType.DATE_TIME);

        public string[] AvailableFonts { get; } = new[] { "맑은 고딕", "나눔고딕", "굴림", "돋움", "Arial", "Segoe UI", "Times New Roman" };
        public string[] AvailableBarcodeFormats { get; } = new[] 
        { 
            "CODE128", 
            "EAN13", 
            "EAN8", 
            "CODE39", 
            "CODE93", 
            "UPC-A", 
            "UPC-E", 
            "ITF", 
            "CODABAR", 
            "QR_CODE", 
            "DATA_MATRIX", 
            "PDF417", 
            "AZTEC" 
        };

        public ObservableCollection<NameCodePair> AvailableBarcodeTextPositions { get; } = new ObservableCollection<NameCodePair>
        {
            new NameCodePair("BELOW", "바코드 아래 (기본)"),
            new NameCodePair("ABOVE", "바코드 위"),
            new NameCodePair("NONE", "미표시 (숨김)")
        };

        public ObservableCollection<NameCodePair> AvailableShapeTypes { get; } = new ObservableCollection<NameCodePair>
        {
            new NameCodePair("RECTANGLE", "직사각형", "⬛"),
            new NameCodePair("ROUNDED_RECT", "둥근 사각형", "▢"),
            new NameCodePair("CIRCLE", "원 / 타원", "⚪"),
            new NameCodePair("TRIANGLE", "삼각형", "🔺"),
            new NameCodePair("TRIANGLE_INVERTED", "역삼각형", "🔻"),
            new NameCodePair("DIAMOND", "마름모", "🔶"),
            new NameCodePair("PENTAGON", "오각형", "⬟"),
            new NameCodePair("HEXAGON", "육각형", "⬢"),
            new NameCodePair("OCTAGON", "팔각형", "🛑"),
            new NameCodePair("STAR", "별", "⭐"),
            new NameCodePair("HEART", "하트", "❤️"),
            new NameCodePair("CLOUD", "구름", "☁️"),
            new NameCodePair("SPEECH_BUBBLE", "말풍선", "💬"),
            new NameCodePair("ARROW_RIGHT", "우측 화살표", "➔"),
            new NameCodePair("ARROW_LEFT", "좌측 화살표", "⬅️"),
            new NameCodePair("ARROW_BOTH", "양방향 화살표", "↔️"),
            new NameCodePair("CROSS", "십자가", "➕")
        };

        public ObservableCollection<NameCodePair> AvailableLineStyles { get; } = new ObservableCollection<NameCodePair>
        {
            new NameCodePair("SOLID", "실선", "───"),
            new NameCodePair("DASHED", "파선 (대시)", "- - -"),
            new NameCodePair("DOTTED", "점선", "·······"),
            new NameCodePair("DASH_DOT", "일점쇄선", "─ · ─"),
            new NameCodePair("DOUBLE", "이중선", "═══"),
            new NameCodePair("WAVY", "물결선", "〰️〰️"),
            new NameCodePair("ZIGZAG", "지그재그", "⚡⚡")
        };

        private List<string> _loadedExcelHeaders = new List<string>();
        public List<string> LoadedExcelHeaders
        {
            get => _loadedExcelHeaders;
            set
            {
                _loadedExcelHeaders = value ?? new List<string>();
                OnPropertyChanged();
                OnPropertyChanged(nameof(AvailableExcelColumnOptions));
            }
        }

        public ObservableCollection<NameCodePair> AvailableExcelColumnOptions
        {
            get
            {
                var list = new ObservableCollection<NameCodePair>
                {
                    new NameCodePair("0", "(직접 입력 - 고정값)")
                };

                // 실제 엑셀 파일이 로드되었을 때만 해당 엑셀 시트의 컬럼 옵션 제공
                if (IsExcelFileLoaded && _loadedExcelHeaders != null && _loadedExcelHeaders.Count > 0)
                {
                    for (int i = 0; i < _loadedExcelHeaders.Count; i++)
                    {
                        string header = _loadedExcelHeaders[i];
                        string colLetter = BarcodePrint.Services.ExcelService.GetColumnLetter(i + 1);
                        string name = string.IsNullOrWhiteSpace(header) ? $"[{colLetter}열]" : $"[{colLetter}열] {header}";
                        list.Add(new NameCodePair((i + 1).ToString(), name));
                    }
                }
                return list;
            }
        }

        private string GetNormalizedColumnIndex(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == "0") return "0";

            if (int.TryParse(raw, out int idx) && idx > 0)
            {
                return idx.ToString();
            }

            if (_loadedExcelHeaders != null && _loadedExcelHeaders.Count > 0)
            {
                for (int i = 0; i < _loadedExcelHeaders.Count; i++)
                {
                    string colLetter = BarcodePrint.Services.ExcelService.GetColumnLetter(i + 1);
                    if (string.Equals(raw, colLetter, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(raw, _loadedExcelHeaders[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return (i + 1).ToString();
                    }
                }
            }

            return raw;
        }

        public string SelectedElementExcelColumn
        {
            get
            {
                if (SelectedElement == null) return "0";

                string raw = SelectedElement.DataSourceColumn;
                if (string.IsNullOrWhiteSpace(raw) || raw == "0")
                {
                    return "0";
                }

                return GetNormalizedColumnIndex(raw);
            }
            set
            {
                if (SelectedElement != null)
                {
                    string normalized = (value == "0" || string.IsNullOrWhiteSpace(value)) ? null : value;
                    if ((SelectedElement.DataSourceColumn ?? "") != (normalized ?? ""))
                    {
                        SelectedElement.DataSourceColumn = normalized;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(IsSelectedElementExcelLinked));
                        OnPropertyChanged(nameof(IsSelectedElementExcelNotLinked));
                        OnPropertyChanged(nameof(IsSelectedElementExcelPending));
                        OnPropertyChanged(nameof(SelectedElementContent));
                        OnPropertyChanged(nameof(SelectedElementEffectivePreviewText));
                        if (SelectedElement.IsVariable && SelectedElement.InputOrder > 0)
                        {
                            NotifyColumnExcelChanged(SelectedElement.InputOrder);
                        }
                        UpdatePreview();
                    }
                }
            }
        }

        public void UnlinkSelectedElementExcel()
        {
            if (SelectedElement != null)
            {
                SelectedElement.DataSourceColumn = null;
                OnPropertyChanged(nameof(SelectedElementExcelColumn));
                OnPropertyChanged(nameof(IsSelectedElementExcelLinked));
                OnPropertyChanged(nameof(IsSelectedElementExcelNotLinked));
                OnPropertyChanged(nameof(IsSelectedElementExcelPending));
                OnPropertyChanged(nameof(SelectedElementContent));
                OnPropertyChanged(nameof(SelectedElementEffectivePreviewText));
                if (SelectedElement.IsVariable && SelectedElement.InputOrder > 0)
                {
                    NotifyColumnExcelChanged(SelectedElement.InputOrder);
                }
                UpdatePreview();
                StatusMessage = "선택된 요소의 엑셀 연동이 해제되었습니다. 직접 수정이 가능합니다.";
            }
        }

        public bool IsSelectedElementExcelLinked => IsExcelFileLoaded && SelectedElement != null && !string.IsNullOrEmpty(SelectedElement.DataSourceColumn) && SelectedElement.DataSourceColumn != "0";
        public bool IsSelectedElementExcelNotLinked => !IsSelectedElementExcelLinked;
        public bool IsSelectedElementExcelPending => !IsExcelFileLoaded && SelectedElement != null && !string.IsNullOrEmpty(SelectedElement.DataSourceColumn) && SelectedElement.DataSourceColumn != "0";

        #region 직접 입력 항목 관리 (1~8열) 엑셀 연동 제어

        public bool IsColumnExcelLinked(int colNum)
        {
            if (!IsExcelFileLoaded || CurrentTemplate?.Elements == null) return false;
            var elem = CurrentTemplate.Elements.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
            return elem != null && !string.IsNullOrEmpty(elem.DataSourceColumn) && elem.DataSourceColumn != "0";
        }

        public string GetColumnDataSource(int colNum)
        {
            var elem = CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
            return elem != null ? GetNormalizedColumnIndex(elem.DataSourceColumn) : "0";
        }

        public void SetColumnDataSource(int colNum, string value)
        {
            var elem = CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
            if (elem != null)
            {
                string norm = (value == "0" || string.IsNullOrWhiteSpace(value)) ? null : value;
                elem.DataSourceColumn = norm;
                NotifyColumnExcelChanged(colNum);
                UpdatePreview();
            }
        }

        public void UnlinkColumnExcel(int colNum)
        {
            SetColumnDataSource(colNum, "0");
            StatusMessage = $"{colNum}열의 엑셀 연동이 해제되었습니다. 직접 입력이 가능합니다.";
        }

        public void NotifyColumnExcelChanged(int colNum)
        {
            OnPropertyChanged($"IsCol{colNum}ExcelLinked");
            OnPropertyChanged($"IsCol{colNum}DirectInputEnabled");
            OnPropertyChanged($"Col{colNum}ExcelColumn");
            OnPropertyChanged(nameof(SelectedElementExcelColumn));
            OnPropertyChanged(nameof(IsSelectedElementExcelLinked));
            OnPropertyChanged(nameof(IsSelectedElementExcelNotLinked));
            OnPropertyChanged(nameof(IsSelectedElementExcelPending));
            OnPropertyChanged(nameof(SelectedElementEffectivePreviewText));
            OnPropertyChanged($"Col{colNum}EffectiveText");
        }

        public void NotifyAllColumnsExcelChanged()
        {
            for (int i = 1; i <= 8; i++)
            {
                NotifyColumnExcelChanged(i);
            }
        }

        public void NotifyAllColumnsEffectiveTextChanged()
        {
            for (int i = 1; i <= 8; i++)
            {
                OnPropertyChanged($"Col{i}EffectiveText");
            }
        }

        public string GetColumnEffectiveText(int colNum)
        {
            var elem = CurrentTemplate?.Elements?.FirstOrDefault(el => el.IsVariable && el.InputOrder == colNum);
            if (elem != null && !string.IsNullOrEmpty(elem.DataSourceColumn) && elem.DataSourceColumn != "0")
            {
                return GetElementEffectiveContent(elem);
            }
            return GetInputByIndex(colNum);
        }

        public bool IsCol1ExcelLinked => IsColumnExcelLinked(1);
        public bool IsCol1DirectInputEnabled => !IsColumnExcelLinked(1);
        public string Col1ExcelColumn { get => GetColumnDataSource(1); set => SetColumnDataSource(1, value); }
        public string Col1EffectiveText { get => IsCol1ExcelLinked ? GetColumnEffectiveText(1) : InputCol1; set => InputCol1 = value; }

        public bool IsCol2ExcelLinked => IsColumnExcelLinked(2);
        public bool IsCol2DirectInputEnabled => !IsColumnExcelLinked(2);
        public string Col2ExcelColumn { get => GetColumnDataSource(2); set => SetColumnDataSource(2, value); }
        public string Col2EffectiveText { get => IsCol2ExcelLinked ? GetColumnEffectiveText(2) : InputCol2; set => InputCol2 = value; }

        public bool IsCol3ExcelLinked => IsColumnExcelLinked(3);
        public bool IsCol3DirectInputEnabled => !IsColumnExcelLinked(3);
        public string Col3ExcelColumn { get => GetColumnDataSource(3); set => SetColumnDataSource(3, value); }
        public string Col3EffectiveText { get => IsCol3ExcelLinked ? GetColumnEffectiveText(3) : InputCol3; set => InputCol3 = value; }

        public bool IsCol4ExcelLinked => IsColumnExcelLinked(4);
        public bool IsCol4DirectInputEnabled => !IsColumnExcelLinked(4);
        public string Col4ExcelColumn { get => GetColumnDataSource(4); set => SetColumnDataSource(4, value); }
        public string Col4EffectiveText { get => IsCol4ExcelLinked ? GetColumnEffectiveText(4) : InputCol4; set => InputCol4 = value; }

        public bool IsCol5ExcelLinked => IsColumnExcelLinked(5);
        public bool IsCol5DirectInputEnabled => !IsColumnExcelLinked(5);
        public string Col5ExcelColumn { get => GetColumnDataSource(5); set => SetColumnDataSource(5, value); }
        public string Col5EffectiveText { get => IsCol5ExcelLinked ? GetColumnEffectiveText(5) : InputCol5; set => InputCol5 = value; }

        public bool IsCol6ExcelLinked => IsColumnExcelLinked(6);
        public bool IsCol6DirectInputEnabled => !IsColumnExcelLinked(6);
        public string Col6ExcelColumn { get => GetColumnDataSource(6); set => SetColumnDataSource(6, value); }
        public string Col6EffectiveText { get => IsCol6ExcelLinked ? GetColumnEffectiveText(6) : InputCol6; set => InputCol6 = value; }

        public bool IsCol7ExcelLinked => IsColumnExcelLinked(7);
        public bool IsCol7DirectInputEnabled => !IsColumnExcelLinked(7);
        public string Col7ExcelColumn { get => GetColumnDataSource(7); set => SetColumnDataSource(7, value); }
        public string Col7EffectiveText { get => IsCol7ExcelLinked ? GetColumnEffectiveText(7) : InputCol7; set => InputCol7 = value; }

        public bool IsCol8ExcelLinked => IsColumnExcelLinked(8);
        public bool IsCol8DirectInputEnabled => !IsColumnExcelLinked(8);
        public string Col8ExcelColumn { get => GetColumnDataSource(8); set => SetColumnDataSource(8, value); }
        public string Col8EffectiveText { get => IsCol8ExcelLinked ? GetColumnEffectiveText(8) : InputCol8; set => InputCol8 = value; }

        #endregion

        public double SelectedElementX
        {
            get => SelectedElement?.X ?? 0;
            set
            {
                if (SelectedElement != null && Math.Abs(SelectedElement.X - value) > 0.01)
                {
                    SelectedElement.X = Math.Max(0, value);
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public double SelectedElementY
        {
            get => SelectedElement?.Y ?? 0;
            set
            {
                if (SelectedElement != null && Math.Abs(SelectedElement.Y - value) > 0.01)
                {
                    SelectedElement.Y = Math.Max(0, value);
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public double SelectedElementWidth
        {
            get => SelectedElement?.Width ?? 10;
            set
            {
                if (SelectedElement != null && Math.Abs(SelectedElement.Width - value) > 0.01)
                {
                    SelectedElement.Width = Math.Max(2, value);
                    OnPropertyChanged();
                    SyncElementToLegacyColumn(SelectedElement);
                    UpdatePreview();
                }
            }
        }

        public double SelectedElementHeight
        {
            get => SelectedElement?.Height ?? 10;
            set
            {
                if (SelectedElement != null && Math.Abs(SelectedElement.Height - value) > 0.01)
                {
                    SelectedElement.Height = Math.Max(2, value);
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public int SelectedElementRotation
        {
            get => SelectedElement?.Rotation ?? 0;
            set
            {
                if (SelectedElement != null && SelectedElement.Rotation != value)
                {
                    SelectedElement.Rotation = (value % 360 + 360) % 360;
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public bool SelectedElementShowBarcodeText
        {
            get => SelectedElement?.ShowBarcodeText ?? true;
            set
            {
                if (SelectedElement != null && SelectedElement.ShowBarcodeText != value)
                {
                    SelectedElement.ShowBarcodeText = value;
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public bool SelectedElementIsFilled
        {
            get => SelectedElement?.IsFilled ?? false;
            set
            {
                if (SelectedElement != null && SelectedElement.IsFilled != value)
                {
                    SelectedElement.IsFilled = value;
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public double SelectedElementLineWidth
        {
            get => SelectedElement?.LineWidth ?? 1.0;
            set
            {
                if (SelectedElement != null && Math.Abs(SelectedElement.LineWidth - value) > 0.01)
                {
                    SelectedElement.LineWidth = (float)Math.Max(1.0, value);
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public string SelectedElementBarcodeTextPosition
        {
            get => SelectedElement?.BarcodeTextPosition ?? "BELOW";
            set
            {
                if (SelectedElement != null && SelectedElement.BarcodeTextPosition != value)
                {
                    SelectedElement.BarcodeTextPosition = value;
                    SelectedElement.ShowBarcodeText = !string.Equals(value, "NONE", StringComparison.OrdinalIgnoreCase);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedElementShowBarcodeText));
                    SyncElementToLegacyColumn(SelectedElement);
                    UpdatePreview();
                }
            }
        }

        public string SelectedElementShapeType
        {
            get => SelectedElement?.ShapeType ?? "RECTANGLE";
            set
            {
                if (SelectedElement != null && SelectedElement.ShapeType != value)
                {
                    SelectedElement.ShapeType = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedElementShapeName));
                    OnPropertyChanged(nameof(SelectedElementTypeText));
                    UpdatePreview();
                }
            }
        }

        public string SelectedElementShapeName
        {
            get
            {
                if (SelectedElement == null) return "";
                var item = AvailableShapeTypes.FirstOrDefault(x => x.Code == SelectedElement.ShapeType);
                return item != null ? item.DisplayText : SelectedElement.ShapeType;
            }
        }

        public string SelectedElementLineStyle
        {
            get => SelectedElement?.LineStyle ?? "SOLID";
            set
            {
                if (SelectedElement != null && SelectedElement.LineStyle != value)
                {
                    SelectedElement.LineStyle = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedElementLineName));
                    OnPropertyChanged(nameof(SelectedElementTypeText));
                    UpdatePreview();
                }
            }
        }

        public string SelectedElementLineName
        {
            get
            {
                if (SelectedElement == null) return "";
                var item = AvailableLineStyles.FirstOrDefault(x => x.Code == SelectedElement.LineStyle);
                return item != null ? item.DisplayText : SelectedElement.LineStyle;
            }
        }

        private double ParseStepDelta(object p)
        {
            if (p == null) return 0.5;
            if (p is double d) return d;
            if (double.TryParse(p.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsed))
            {
                return parsed;
            }
            if (double.TryParse(p.ToString(), out double parsedLocal))
            {
                return parsedLocal;
            }
            return 0.5;
        }

        public void StepSelectedElementX(double delta)
        {
            if (SelectedElement == null) return;
            SelectedElementX = Math.Max(0, Math.Round((SelectedElement.X + delta) * 2.0) / 2.0);
        }

        public void StepSelectedElementY(double delta)
        {
            if (SelectedElement == null) return;
            SelectedElementY = Math.Max(0, Math.Round((SelectedElement.Y + delta) * 2.0) / 2.0);
        }

        public void StepSelectedElementWidth(double delta)
        {
            if (SelectedElement == null) return;
            SelectedElementWidth = Math.Max(2.0, Math.Round((SelectedElement.Width + delta) * 2.0) / 2.0);
        }

        public void StepSelectedElementHeight(double delta)
        {
            if (SelectedElement == null) return;
            SelectedElementHeight = Math.Max(2.0, Math.Round((SelectedElement.Height + delta) * 2.0) / 2.0);
        }

        public void StepSelectedElementFontSize(double delta)
        {
            if (SelectedElement == null) return;
            SelectedElementFontSize = Math.Max(4.0, Math.Min(200.0, Math.Round(SelectedElement.FontSize + delta)));
        }

        public void StepSelectedElementLineWidth(double delta)
        {
            if (SelectedElement == null) return;
            SelectedElementLineWidth = Math.Max(1.0, Math.Min(50.0, Math.Round(SelectedElement.LineWidth + delta)));
        }

        public void StepSelectedElementLetterSpacing(double delta)
        {
            if (SelectedElement == null) return;
            SelectedElementLetterSpacing = Math.Max(-0.5, Math.Min(2.0, Math.Round((SelectedElementLetterSpacing + delta) * 100.0) / 100.0));
        }

        public void StepSelectedElementFontScaleX(double delta)
        {
            if (SelectedElement == null) return;
            SelectedElementFontScaleX = Math.Max(30.0, Math.Min(300.0, Math.Round(SelectedElementFontScaleX + delta)));
        }

        public void StepSelectedElementLineSpacing(double delta)
        {
            if (SelectedElement == null) return;
            SelectedElementLineSpacing = Math.Max(0.5, Math.Min(3.0, Math.Round((SelectedElementLineSpacing + delta) * 10.0) / 10.0));
        }

        public void ResetSelectedElementTextSpacing()
        {
            if (SelectedElement == null) return;
            SelectedElementLetterSpacing = 0.0;
            SelectedElementFontScaleX = 100.0;
            SelectedElementLineSpacing = 1.0;
        }

        public void StepTableRows(int delta)
        {
            if (SelectedElement == null || SelectedElement.Type != ElementType.TABLE) return;
            SelectedElementTableRows = Math.Max(1, Math.Min(20, SelectedElement.Rows + delta));
        }

        public void StepTableColumns(int delta)
        {
            if (SelectedElement == null || SelectedElement.Type != ElementType.TABLE) return;
            SelectedElementTableColumns = Math.Max(1, Math.Min(10, SelectedElement.Columns + delta));
        }

        public void StepTableLineWidth(float delta)
        {
            if (SelectedElement == null || SelectedElement.Type != ElementType.TABLE) return;
            SelectedElementTableLineWidth = (float)Math.Max(1.0, Math.Min(20.0, Math.Round(SelectedElement.LineWidth + delta)));
        }

        public void EvenTableColumns()
        {
            if (SelectedElement == null || SelectedElement.Type != ElementType.TABLE) return;
            SelectedElement.ColumnWidths = null;
            UpdatePreview();
            StatusMessage = "표 열 너비를 균등하게 분할했습니다.";
        }

        public void EvenTableRows()
        {
            if (SelectedElement == null || SelectedElement.Type != ElementType.TABLE) return;
            SelectedElement.RowHeights = null;
            UpdatePreview();
            StatusMessage = "표 행 높이를 균등하게 분할했습니다.";
        }

        public int SelectedElementTableRows
        {
            get => SelectedElement?.Rows ?? 2;
            set
            {
                if (SelectedElement != null && SelectedElement.Type == ElementType.TABLE)
                {
                    int val = Math.Max(1, Math.Min(20, value));
                    if (SelectedElement.Rows != val)
                    {
                        SelectedElement.Rows = val;
                        SelectedElement.EnsureTableData();
                        OnPropertyChanged();
                        RefreshTableCells();
                        UpdatePreview();
                    }
                }
            }
        }

        public int SelectedElementTableColumns
        {
            get => SelectedElement?.Columns ?? 2;
            set
            {
                if (SelectedElement != null && SelectedElement.Type == ElementType.TABLE)
                {
                    int val = Math.Max(1, Math.Min(10, value));
                    if (SelectedElement.Columns != val)
                    {
                        SelectedElement.Columns = val;
                        SelectedElement.EnsureTableData();
                        OnPropertyChanged();
                        RefreshTableCells();
                        UpdatePreview();
                    }
                }
            }
        }

        public float SelectedElementTableLineWidth
        {
            get => SelectedElement?.LineWidth ?? 1.0f;
            set
            {
                if (SelectedElement != null && SelectedElement.Type == ElementType.TABLE)
                {
                    float val = Math.Max(1.0f, Math.Min(20.0f, value));
                    if (Math.Abs(SelectedElement.LineWidth - val) > 0.01f)
                    {
                        SelectedElement.LineWidth = val;
                        OnPropertyChanged();
                        UpdatePreview();
                    }
                }
            }
        }

        public bool SelectedElementTableIsFilled
        {
            get => SelectedElement?.IsFilled ?? false;
            set
            {
                if (SelectedElement != null && SelectedElement.Type == ElementType.TABLE)
                {
                    if (SelectedElement.IsFilled != value)
                    {
                        SelectedElement.IsFilled = value;
                        OnPropertyChanged();
                        UpdatePreview();
                    }
                }
            }
        }

        private int _selectedTableRowIndex = 0;
        public int SelectedTableRowIndex
        {
            get => _selectedTableRowIndex;
            set
            {
                if (_selectedTableRowIndex != value)
                {
                    _selectedTableRowIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _selectedTableColumnIndex = 0;
        public int SelectedTableColumnIndex
        {
            get => _selectedTableColumnIndex;
            set
            {
                if (_selectedTableColumnIndex != value)
                {
                    _selectedTableColumnIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _tableTargetFontSize = 9.0;
        public double TableTargetFontSize
        {
            get => _tableTargetFontSize;
            set
            {
                double sz = Math.Max(5.0, Math.Min(40.0, value));
                if (Math.Abs(_tableTargetFontSize - sz) > 0.01)
                {
                    _tableTargetFontSize = sz;
                    OnPropertyChanged();
                }
            }
        }

        private bool _tableTargetIsBold = false;
        public bool TableTargetIsBold
        {
            get => _tableTargetIsBold;
            set
            {
                if (_tableTargetIsBold != value)
                {
                    _tableTargetIsBold = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _tableTargetAlignment = "CENTER";
        public string TableTargetAlignment
        {
            get => _tableTargetAlignment;
            set
            {
                if (_tableTargetAlignment != value)
                {
                    _tableTargetAlignment = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<NameCodePair> _availableTableRows = new ObservableCollection<NameCodePair>();
        public ObservableCollection<NameCodePair> AvailableTableRows => _availableTableRows;

        private ObservableCollection<NameCodePair> _availableTableColumns = new ObservableCollection<NameCodePair>();
        public ObservableCollection<NameCodePair> AvailableTableColumns => _availableTableColumns;

        public void StepTableTargetFontSize(double delta)
        {
            TableTargetFontSize = Math.Max(5.0, Math.Min(40.0, TableTargetFontSize + delta));
        }

        public void ApplyTableRowFont()
        {
            if (SelectedElement == null || SelectedElement.Type != ElementType.TABLE) return;
            SelectedElement.SetRowStyle(SelectedTableRowIndex, SelectedElement.FontName, TableTargetFontSize, TableTargetIsBold, TableTargetAlignment);
            RefreshTableCells();
            UpdatePreview();
            StatusMessage = $"{SelectedTableRowIndex + 1}행 전체 서식을 적용했습니다. (크기: {TableTargetFontSize:0.#}pt, 굵기: {(TableTargetIsBold ? "굵게" : "보통")})";
        }

        public void ApplyTableColumnFont()
        {
            if (SelectedElement == null || SelectedElement.Type != ElementType.TABLE) return;
            SelectedElement.SetColumnStyle(SelectedTableColumnIndex, SelectedElement.FontName, TableTargetFontSize, TableTargetIsBold, TableTargetAlignment);
            RefreshTableCells();
            UpdatePreview();
            StatusMessage = $"{SelectedTableColumnIndex + 1}열 전체 서식을 적용했습니다. (크기: {TableTargetFontSize:0.#}pt, 굵기: {(TableTargetIsBold ? "굵게" : "보통")})";
        }

        public void ResetTableFonts()
        {
            if (SelectedElement == null || SelectedElement.Type != ElementType.TABLE) return;
            SelectedElement.ClearAllCellStyles();
            RefreshTableCells();
            UpdatePreview();
            StatusMessage = "표의 모든 개별 서식을 초기화하고 기본 폰트로 복원했습니다.";
        }

        public void StepTableCellFontSize(TableCellItem item, double delta)
        {
            if (item == null || SelectedElement == null || SelectedElement.Type != ElementType.TABLE) return;
            item.StepFontSize(delta);
        }

        public void ToggleTableCellBold(TableCellItem item)
        {
            if (item == null || SelectedElement == null || SelectedElement.Type != ElementType.TABLE) return;
            item.ToggleBold();
        }

        private ObservableCollection<TableCellItem> _selectedElementTableCells = new ObservableCollection<TableCellItem>();
        public ObservableCollection<TableCellItem> SelectedElementTableCells
        {
            get => _selectedElementTableCells;
            set
            {
                _selectedElementTableCells = value;
                OnPropertyChanged();
            }
        }

        public void RefreshTableCells()
        {
            _selectedElementTableCells.Clear();
            _availableTableRows.Clear();
            _availableTableColumns.Clear();

            if (SelectedElement != null && SelectedElement.Type == ElementType.TABLE)
            {
                SelectedElement.EnsureTableData();
                int rows = SelectedElement.Rows;
                int cols = SelectedElement.Columns;

                for (int r = 0; r < rows; r++)
                {
                    _availableTableRows.Add(new NameCodePair($"{r + 1}행", r.ToString()));
                }
                for (int c = 0; c < cols; c++)
                {
                    _availableTableColumns.Add(new NameCodePair($"{c + 1}열", c.ToString()));
                }
                if (SelectedTableRowIndex >= rows) SelectedTableRowIndex = 0;
                if (SelectedTableColumnIndex >= cols) SelectedTableColumnIndex = 0;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        string val = SelectedElement.TableData[r][c];
                        string mapCol = SelectedElement.GetCellMapping(r, c);
                        string preview = GetTableCellEffectiveContent(SelectedElement, r, c);
                        var style = SelectedElement.GetCellResolvedStyle(r, c);

                        var cellItem = new TableCellItem(
                            r, c, val, mapCol,
                            (cellR, cellC, newText) =>
                            {
                                if (SelectedElement != null && SelectedElement.TableData != null &&
                                    cellR < SelectedElement.TableData.Length && cellC < SelectedElement.TableData[cellR].Length)
                                {
                                    SelectedElement.TableData[cellR][cellC] = newText;
                                    UpdatePreview();
                                }
                            },
                            (cellR, cellC, newCol) =>
                            {
                                if (SelectedElement != null)
                                {
                                    SelectedElement.SetCellMapping(cellR, cellC, newCol);
                                    UpdatePreview();
                                    RefreshTableCellPreviews();
                                }
                            },
                            () => AvailableExcelColumnOptions,
                            preview,
                            style.FontSize,
                            style.IsBold,
                            style.Alignment,
                            (cellR, cellC, newSz, newBold, newAlign) =>
                            {
                                if (SelectedElement != null)
                                {
                                    SelectedElement.SetCellStyle(cellR, cellC, SelectedElement.FontName, newSz, newBold, newAlign);
                                    UpdatePreview();
                                }
                            }
                        );
                        _selectedElementTableCells.Add(cellItem);
                    }
                }
            }
            OnPropertyChanged(nameof(SelectedElementTableCells));
            OnPropertyChanged(nameof(AvailableTableRows));
            OnPropertyChanged(nameof(AvailableTableColumns));
        }

        public void RefreshTableCellPreviews()
        {
            if (SelectedElement != null && SelectedElement.Type == ElementType.TABLE && _selectedElementTableCells != null)
            {
                foreach (var cell in _selectedElementTableCells)
                {
                    string preview = GetTableCellEffectiveContent(SelectedElement, cell.Row, cell.Column);
                    var style = SelectedElement.GetCellResolvedStyle(cell.Row, cell.Column);
                    cell.UpdateStyleSilently(style.FontSize, style.IsBold, style.Alignment);
                    cell.RefreshOptions(preview);
                }
            }
        }

        public string GetTableCellEffectiveContent(TemplateElement elem, int r, int c, PrintRecordModel record = null)
        {
            if (elem == null || elem.Type != ElementType.TABLE) return "";
            elem.EnsureTableData();

            string mapCol = elem.GetCellMapping(r, c);
            if (!string.IsNullOrEmpty(mapCol) && mapCol != "0")
            {
                if (record != null)
                {
                    string recVal = record.GetValueByColumn(mapCol);
                    return recVal ?? "";
                }

                if (IsExcelFileLoaded && Records != null && Records.Count > 0)
                {
                    int recIdx = (CurrentPreviewIndex > 0 && CurrentPreviewIndex <= Records.Count)
                        ? CurrentPreviewIndex - 1
                        : (SelectedRecord != null ? Records.IndexOf(SelectedRecord) : 0);
                    if (recIdx < 0 || recIdx >= Records.Count) recIdx = 0;
                    var targetRecord = Records[recIdx];
                    if (targetRecord != null)
                    {
                        string mappedVal = targetRecord.GetValueByColumn(mapCol);
                        return mappedVal ?? "";
                    }
                }

                string staticText = (elem.TableData != null && r < elem.TableData.Length && c < elem.TableData[r].Length)
                    ? elem.TableData[r][c] : "";
                return !string.IsNullOrEmpty(staticText) ? staticText : $"[엑셀 {mapCol}열]";
            }

            return (elem.TableData != null && r < elem.TableData.Length && c < elem.TableData[r].Length)
                ? (elem.TableData[r][c] ?? "")
                : "";
        }

        public void DeselectElement()
        {
            SelectedElement = null;
        }

        #region Selected Element Property Proxies (UI 양방향 바인딩용)

        public string SelectedElementContent
        {
            get => SelectedElement?.Content ?? "";
            set
            {
                if (SelectedElement != null && SelectedElement.Content != value)
                {
                    SelectedElement.Content = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedElementEffectivePreviewText));
                    SyncElementToLegacyColumn(SelectedElement);
                    UpdatePreview();
                }
            }
        }

        public bool SelectedElementIsKeep
        {
            get => SelectedElement?.IsKeep ?? false;
            set
            {
                if (SelectedElement != null && SelectedElement.IsKeep != value)
                {
                    SelectedElement.IsKeep = value;
                    OnPropertyChanged();
                    SyncElementToLegacyColumn(SelectedElement);
                }
            }
        }

        public List<DateTimeFormatOptionItem> DateTimeFormatOptions
        {
            get
            {
                var now = DateTime.Now;
                return new List<DateTimeFormatOptionItem>
                {
                    new DateTimeFormatOptionItem("yyyy-MM-dd HH:mm", $"{now:yyyy-MM-dd HH:mm} (yyyy-MM-dd HH:mm)"),
                    new DateTimeFormatOptionItem("yyyy-MM-dd HH:mm:ss", $"{now:yyyy-MM-dd HH:mm:ss} (yyyy-MM-dd HH:mm:ss)"),
                    new DateTimeFormatOptionItem("yyyy-MM-dd", $"{now:yyyy-MM-dd} (yyyy-MM-dd)"),
                    new DateTimeFormatOptionItem("yyyy.MM.dd", $"{now:yyyy.MM.dd} (yyyy.MM.dd)"),
                    new DateTimeFormatOptionItem("yyyy/MM/dd", $"{now.ToString("yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture)} (yyyy/MM/dd)"),
                    new DateTimeFormatOptionItem("yyyy년 MM월 dd일", $"{now:yyyy년 MM월 dd일} (yyyy년 MM월 dd일)"),
                    new DateTimeFormatOptionItem("yyyy.MM.dd (ddd)", $"{now.ToString("yyyy.MM.dd (ddd)", System.Globalization.CultureInfo.CurrentCulture)} (yyyy.MM.dd (ddd))"),
                    new DateTimeFormatOptionItem("yyyy-MM-dd tt hh:mm", $"{now:yyyy-MM-dd tt hh:mm} (yyyy-MM-dd tt hh:mm)"),
                    new DateTimeFormatOptionItem("HH:mm:ss", $"{now:HH:mm:ss} (HH:mm:ss)"),
                    new DateTimeFormatOptionItem("HH:mm", $"{now:HH:mm} (HH:mm)"),
                    new DateTimeFormatOptionItem("yyMMdd", $"{now:yyMMdd} (yyMMdd)"),
                    new DateTimeFormatOptionItem("yyyyMMdd", $"{now:yyyyMMdd} (yyyyMMdd)")
                };
            }
        }

        public string SelectedElementDateTimeFormat
        {
            get
            {
                if (SelectedElement == null || SelectedElement.Type != ElementType.DATE_TIME)
                    return "yyyy-MM-dd HH:mm";
                if (string.IsNullOrWhiteSpace(SelectedElement.DateTimeFormat))
                {
                    SelectedElement.DateTimeFormat = "yyyy-MM-dd HH:mm";
                }
                return SelectedElement.DateTimeFormat;
            }
            set
            {
                if (SelectedElement != null && SelectedElement.Type == ElementType.DATE_TIME)
                {
                    string finalVal = string.IsNullOrWhiteSpace(value) ? "yyyy-MM-dd HH:mm" : value;
                    SelectedElement.DateTimeFormat = finalVal;
                    try
                    {
                        SelectedElement.Content = DateTime.Now.ToString(finalVal, System.Globalization.CultureInfo.CurrentCulture);
                    }
                    catch
                    {
                        SelectedElement.Content = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedElementContent));
                    OnPropertyChanged(nameof(SelectedElementEffectivePreviewText));
                    UpdatePreview();
                }
            }
        }

        public bool SelectedElementAutoUpdateDate
        {
            get => SelectedElement?.AutoUpdateDate ?? true;
            set
            {
                if (SelectedElement != null && SelectedElement.Type == ElementType.DATE_TIME && SelectedElement.AutoUpdateDate != value)
                {
                    SelectedElement.AutoUpdateDate = value;
                    if (value)
                    {
                        try
                        {
                            string fmt = string.IsNullOrWhiteSpace(SelectedElement.DateTimeFormat) ? "yyyy-MM-dd HH:mm" : SelectedElement.DateTimeFormat;
                            SelectedElement.Content = DateTime.Now.ToString(fmt, System.Globalization.CultureInfo.CurrentCulture);
                        }
                        catch
                        {
                            SelectedElement.Content = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                        }
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedElementContent));
                    OnPropertyChanged(nameof(SelectedElementEffectivePreviewText));
                    UpdatePreview();
                }
            }
        }

        public double SelectedElementFontSize
        {
            get => SelectedElement?.FontSize ?? 10.0;
            set
            {
                if (SelectedElement != null && SelectedElement.FontSize != value)
                {
                    SelectedElement.FontSize = Math.Max(4, Math.Min(200, value));
                    OnPropertyChanged();
                    SyncElementToLegacyColumn(SelectedElement);
                    UpdatePreview();
                }
            }
        }

        public string SelectedElementFontName
        {
            get => SelectedElement?.FontName ?? "맑은 고딕";
            set
            {
                if (SelectedElement != null && SelectedElement.FontName != value)
                {
                    SelectedElement.FontName = value;
                    OnPropertyChanged();
                    SyncElementToLegacyColumn(SelectedElement);
                    UpdatePreview();
                }
            }
        }

        public bool SelectedElementIsBold
        {
            get => SelectedElement?.IsBold ?? false;
            set
            {
                if (SelectedElement != null && SelectedElement.IsBold != value)
                {
                    SelectedElement.IsBold = value;
                    OnPropertyChanged();
                    SyncElementToLegacyColumn(SelectedElement);
                    UpdatePreview();
                }
            }
        }

        public bool SelectedElementIsItalic
        {
            get => SelectedElement?.IsItalic ?? false;
            set
            {
                if (SelectedElement != null && SelectedElement.IsItalic != value)
                {
                    SelectedElement.IsItalic = value;
                    OnPropertyChanged();
                    SyncElementToLegacyColumn(SelectedElement);
                    UpdatePreview();
                }
            }
        }

        public bool SelectedElementIsUnderline
        {
            get => SelectedElement?.IsUnderline ?? false;
            set
            {
                if (SelectedElement != null && SelectedElement.IsUnderline != value)
                {
                    SelectedElement.IsUnderline = value;
                    OnPropertyChanged();
                    SyncElementToLegacyColumn(SelectedElement);
                    UpdatePreview();
                }
            }
        }

        public double SelectedElementLetterSpacing
        {
            get => SelectedElement?.LetterSpacing ?? 0.0;
            set
            {
                if (SelectedElement != null && Math.Abs(SelectedElement.LetterSpacing - value) > 0.001)
                {
                    SelectedElement.LetterSpacing = Math.Round(value, 2);
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public double SelectedElementFontScaleX
        {
            get => Math.Round((SelectedElement?.FontScaleX ?? 1.0) * 100.0);
            set
            {
                if (SelectedElement != null)
                {
                    double ratio = Math.Max(0.3, Math.Min(3.0, value / 100.0));
                    if (Math.Abs(SelectedElement.FontScaleX - ratio) > 0.001)
                    {
                        SelectedElement.FontScaleX = ratio;
                        OnPropertyChanged();
                        UpdatePreview();
                    }
                }
            }
        }

        public double SelectedElementLineSpacing
        {
            get => SelectedElement?.LineSpacing ?? 1.0;
            set
            {
                if (SelectedElement != null && Math.Abs(SelectedElement.LineSpacing - value) > 0.001)
                {
                    SelectedElement.LineSpacing = Math.Round(value, 2);
                    OnPropertyChanged();
                    UpdatePreview();
                }
            }
        }

        public string SelectedElementAlignment
        {
            get => SelectedElement?.Alignment ?? "LEFT";
            set
            {
                if (SelectedElement != null && SelectedElement.Alignment != value)
                {
                    SelectedElement.Alignment = value;
                    OnPropertyChanged();
                    SyncElementToLegacyColumn(SelectedElement);
                    UpdatePreview();
                }
            }
        }

        public string SelectedElementBarcodeType
        {
            get => SelectedElement?.BarcodeType ?? "CODE128";
            set
            {
                if (SelectedElement != null && SelectedElement.BarcodeType != value)
                {
                    SelectedElement.BarcodeType = value;
                    OnPropertyChanged();
                    SyncElementToLegacyColumn(SelectedElement);
                    UpdatePreview();
                }
            }
        }

        #endregion

        #region 자유 편집 요소 추가 / 삭제 명령

        public void ApplyWizardSetup(double width, double height, string paperType, int rotation)
        {
            // 새 문서 생성 시 기존 엑셀 파일 및 컬럼 연동 정보 완전 초기화
            ExecuteUnlinkExcel();

            PaperWidth = (int)Math.Round(width);
            PaperHeight = (int)Math.Round(height);
            PaperWidthStr = width.ToString("0.#");
            PaperHeightStr = height.ToString("0.#");
            LabelRotationAngle = rotation;

            // 새 깨끗한 템플릿 초기화
            _currentTemplate = new LabelTemplateModel
            {
                TemplateName = "새 라벨 템플릿",
                WidthMm = width,
                HeightMm = height,
                Rotation = rotation,
                PaperType = paperType
            };

            // 기본 텍스트 요소 1개 스마트 중앙 배치 (너비 0 = 텍스트 실제 길이에 100% 자동 일치)
            double initTextWMm = 58.0;
            double initTextHMm = 8.0;
            double centerX = Math.Max(2.0, (width - initTextWMm) / 2.0);
            double centerY = Math.Max(2.0, (height - initTextHMm) / 2.0);

            var defaultElem = TemplateMigrationHelper.CreateElement(ElementType.TEXT, centerX, centerY);
            defaultElem.Content = "상품명 또는 텍스트 입력";
            defaultElem.HeaderName = "1열";
            defaultElem.InputOrder = 1;
            defaultElem.FontSize = 14;
            defaultElem.IsBold = true;
            defaultElem.Width = 0.0;  // 0: 텍스트 실제 크기에 자동 일치
            defaultElem.Height = 0.0;
            _currentTemplate.Elements.Add(defaultElem);
            SelectedElement = defaultElem;

            ActiveColumnCount = 1;
            InputCol1 = defaultElem.Content;
            Column1Header = "1열";
            SetOffsetXByIndex(1, (int)Math.Round(centerX));
            SetOffsetYByIndex(1, (int)Math.Round(centerY));
            SetWidthByIndex(1, 0);
            SetBarcodeByIndex(1, false);
            SyncElementToLegacyColumn(defaultElem);

            OnPropertyChanged(nameof(CurrentTemplate));
            OnPropertyChanged(nameof(TemplateElements));
            StatusMessage = $"새 라벨 규격 ({width}x{height} mm) 템플릿이 생성되었습니다.";
            UpdatePreview();
        }

        public void LoadTemplateFromFile(string filePath)
        {
            var loaded = LabelTemplateModel.LoadFromFile(filePath);
            if (loaded != null)
            {
                CurrentTemplate = loaded;
                PaperWidth = (int)Math.Round(loaded.WidthMm);
                PaperHeight = (int)Math.Round(loaded.HeightMm);
                PaperWidthStr = loaded.WidthMm.ToString("0.#");
                PaperHeightStr = loaded.HeightMm.ToString("0.#");
                LabelRotationAngle = loaded.Rotation;
                StatusMessage = $"템플릿을 불러왔습니다: {System.IO.Path.GetFileName(filePath)}";
                UpdatePreview();
            }
        }

        public void AddImageElement()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "라벨에 삽입할 이미지 선택",
                Filter = "이미지 파일 (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|모든 파일 (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                ImportImageFile(dlg.FileName);
            }
        }

        public void ImportImageFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;

            try
            {
                if (CurrentTemplate == null) InitializeDefaultTemplateFromCurrentSettings();

                double elemW = Math.Min(30.0, EffectivePaperWidth - 4.0);
                double elemH = Math.Min(30.0, EffectivePaperHeight - 4.0);
                double elemX = Math.Max(2.0, (EffectivePaperWidth - elemW) / 2.0);
                double elemY = Math.Max(2.0, (EffectivePaperHeight - elemH) / 2.0);

                var imgElem = new TemplateElement
                {
                    Id = Guid.NewGuid().ToString("N").Substring(0, 8),
                    Type = ElementType.IMAGE,
                    Content = filePath,
                    HeaderName = "이미지",
                    X = elemX,
                    Y = elemY,
                    Width = elemW,
                    Height = elemH,
                    IsVariable = false
                };

                CurrentTemplate.Elements.Add(imgElem);
                SelectedElement = imgElem;
                OnPropertyChanged(nameof(TemplateElements));
                StatusMessage = $"이미지 파일이 라벨에 추가되었습니다: {System.IO.Path.GetFileName(filePath)}";
                UpdatePreview();
            }
            catch (Exception ex)
            {
                StatusMessage = $"이미지 불러오기 실패: {ex.Message}";
            }
        }

        public void ImportPdfOrImageFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath)) return;

            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".pdf")
            {
                // PDF 첫 페이지를 고해상도 PNG로 래스터화하여 라벨 캔버스에 추가
                string tempPng = PdfRenderHelper.SavePageToTempPng(filePath, 0, 203);
                if (!string.IsNullOrEmpty(tempPng) && System.IO.File.Exists(tempPng))
                {
                    ImportImageFile(tempPng);
                    StatusMessage = $"PDF 1페이지 이미지가 라벨에 추가되었습니다: {System.IO.Path.GetFileName(filePath)}";
                }
                else
                {
                    StatusMessage = "PDF 페이지 렌더링에 실패했습니다.";
                }
            }
            else
            {
                ImportImageFile(filePath);
            }
        }

        public void OpenPdfPrintStudio()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "인쇄할 PDF 송장 또는 문서 선택",
                Filter = "PDF 문서 (*.pdf)|*.pdf|모든 파일 (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                var pdfDialog = new WpfView.PdfPrintDialog(dlg.FileName, EffectivePaperWidth, EffectivePaperHeight, (tempPng, w, h) =>
                {
                    ImportImageFile(tempPng);
                });
                pdfDialog.ShowDialog();
            }
        }

        public void AddNewElement(ElementType type, string subType = null)
        {
            if (CurrentTemplate == null) InitializeDefaultTemplateFromCurrentSettings();

            // 스마트 겹침 방지 오프셋 계산
            double defaultX = 5.0;
            double defaultY = 5.0;
            if (CurrentTemplate.Elements.Count > 0)
            {
                var lastElem = CurrentTemplate.Elements.Last();
                defaultY = lastElem.Y + lastElem.Height + 2.0;
                if (defaultY + 10.0 > EffectivePaperHeight)
                {
                    defaultY = 5.0;
                    defaultX = Math.Min(EffectivePaperWidth - 20.0, lastElem.X + 15.0);
                }
            }

            var newElem = TemplateMigrationHelper.CreateElement(type, defaultX, defaultY);
            if (type == ElementType.SHAPE && !string.IsNullOrEmpty(subType))
            {
                newElem.ShapeType = subType;
            }
            else if (type == ElementType.LINE && !string.IsNullOrEmpty(subType))
            {
                newElem.LineStyle = subType;
            }
            newElem.InputOrder = CurrentTemplate.Elements.Count(e => e.IsVariable) + 1;
            newElem.HeaderName = $"{newElem.InputOrder}열 ({type})";

            RecordUndoSnapshot();
            CurrentTemplate.Elements.Add(newElem);
            SelectedElement = newElem;

            // 컬럼 수 동기화 (컬럼 개수 제한 해제)
            if (newElem.IsVariable && ActiveColumnCount < 100)
            {
                ActiveColumnCount = Math.Max(ActiveColumnCount, newElem.InputOrder);
                if (newElem.InputOrder <= 8)
                {
                    SyncElementToLegacyColumn(newElem);
                }
            }

            OnPropertyChanged(nameof(TemplateElements));
            StatusMessage = $"새 {type} 요소가 라벨에 추가되었습니다.";
            UpdatePreview();
        }

        private readonly Stack<List<TemplateElement>> _undoStack = new Stack<List<TemplateElement>>();
        private readonly Stack<List<TemplateElement>> _redoStack = new Stack<List<TemplateElement>>();
        private TemplateElement _clipboardElement = null;

        public void RecordUndoSnapshot()
        {
            if (CurrentTemplate?.Elements == null) return;
            var snapshot = new List<TemplateElement>();
            foreach (var el in CurrentTemplate.Elements)
            {
                snapshot.Add(el.Clone());
            }
            _undoStack.Push(snapshot);
            if (_undoStack.Count > 50)
            {
                var arr = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = arr.Length - 2; i >= 0; i--) _undoStack.Push(arr[i]);
            }
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (_undoStack == null || _undoStack.Count == 0 || CurrentTemplate?.Elements == null)
            {
                StatusMessage = "더 이상 실행 취소할 작업이 없습니다.";
                return;
            }

            var currentSnapshot = new List<TemplateElement>();
            foreach (var el in CurrentTemplate.Elements) currentSnapshot.Add(el.Clone());
            _redoStack.Push(currentSnapshot);

            var previous = _undoStack.Pop();
            CurrentTemplate.Elements.Clear();
            foreach (var el in previous) CurrentTemplate.Elements.Add(el);

            SelectedElement = CurrentTemplate.Elements.LastOrDefault();
            OnPropertyChanged(nameof(TemplateElements));
            StatusMessage = "실행 취소(Undo) 완료 (Ctrl+Z).";
            UpdatePreview();
        }

        public void Redo()
        {
            if (_redoStack == null || _redoStack.Count == 0 || CurrentTemplate?.Elements == null)
            {
                StatusMessage = "더 이상 다시 실행할 작업이 없습니다.";
                return;
            }

            var currentSnapshot = new List<TemplateElement>();
            foreach (var el in CurrentTemplate.Elements) currentSnapshot.Add(el.Clone());
            _undoStack.Push(currentSnapshot);

            var next = _redoStack.Pop();
            CurrentTemplate.Elements.Clear();
            foreach (var el in next) CurrentTemplate.Elements.Add(el);

            SelectedElement = CurrentTemplate.Elements.LastOrDefault();
            OnPropertyChanged(nameof(TemplateElements));
            StatusMessage = "다시 실행(Redo) 완료 (Ctrl+Y).";
            UpdatePreview();
        }

        public void CopySelectedElement()
        {
            if (SelectedElement == null) return;
            _clipboardElement = SelectedElement.Clone();
            StatusMessage = $"'{SelectedElement.HeaderName ?? SelectedElement.Type.ToString()}' 요소가 복사되었습니다 (Ctrl+C).";
        }

        public void PasteElement()
        {
            if (_clipboardElement == null || CurrentTemplate?.Elements == null)
            {
                StatusMessage = "클립보드에 복사된 요소가 없습니다 (Ctrl+C로 먼저 복사하세요).";
                return;
            }

            RecordUndoSnapshot();
            var pasted = _clipboardElement.Clone();
            pasted.X = Math.Min(EffectivePaperWidth - 5.0, pasted.X + 3.0);
            pasted.Y = Math.Min(EffectivePaperHeight - 5.0, pasted.Y + 3.0);
            pasted.HeaderName = pasted.HeaderName + " (복사)";

            CurrentTemplate.Elements.Add(pasted);
            SelectedElement = pasted;
            OnPropertyChanged(nameof(TemplateElements));
            StatusMessage = "요소가 붙여넣기되었습니다 (Ctrl+V).";
            UpdatePreview();
        }

        private readonly List<TemplateElement> _selectedElements = new List<TemplateElement>();
        public IReadOnlyList<TemplateElement> SelectedElements => _selectedElements;
        public int SelectedElementsCount => _selectedElements.Count;
        public bool HasMultipleSelectedElements => _selectedElements.Count > 1;

        public void ClearSelectedElements()
        {
            if (CurrentTemplate?.Elements != null)
            {
                foreach (var el in CurrentTemplate.Elements) el.IsSelected = false;
            }
            _selectedElements.Clear();
            _selectedElement = null;
            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(SelectedElementsCount));
            OnPropertyChanged(nameof(HasMultipleSelectedElements));
            NotifySelectedElementProperties();
            UpdatePreview();
        }

        public void SetSelectedElements(IEnumerable<TemplateElement> elements, bool addToExisting = false)
        {
            if (!addToExisting)
            {
                if (CurrentTemplate?.Elements != null)
                {
                    foreach (var el in CurrentTemplate.Elements) el.IsSelected = false;
                }
                _selectedElements.Clear();
            }

            if (elements != null)
            {
                foreach (var el in elements)
                {
                    if (el != null)
                    {
                        el.IsSelected = true;
                        if (!_selectedElements.Contains(el))
                        {
                            _selectedElements.Add(el);
                        }
                    }
                }
            }

            _selectedElement = _selectedElements.LastOrDefault();
            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(SelectedElementsCount));
            OnPropertyChanged(nameof(HasMultipleSelectedElements));
            NotifySelectedElementProperties();
            UpdatePreview();
        }

        public void ToggleElementSelection(TemplateElement elem)
        {
            if (elem == null) return;

            // 기존 단일 선택 상태에서 처음 Shift/Ctrl 클릭 시 _selectedElements 동기화
            if (_selectedElement != null && !_selectedElements.Contains(_selectedElement))
            {
                _selectedElement.IsSelected = true;
                _selectedElements.Add(_selectedElement);
            }

            if (_selectedElements.Contains(elem))
            {
                elem.IsSelected = false;
                _selectedElements.Remove(elem);
            }
            else
            {
                elem.IsSelected = true;
                _selectedElements.Add(elem);
            }

            _selectedElement = _selectedElements.LastOrDefault();
            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(SelectedElementsCount));
            OnPropertyChanged(nameof(HasMultipleSelectedElements));
            NotifySelectedElementProperties();
            UpdatePreview();
        }

        public void AddElementToSelection(TemplateElement elem)
        {
            if (elem == null) return;
            if (_selectedElement != null && !_selectedElements.Contains(_selectedElement))
            {
                _selectedElement.IsSelected = true;
                _selectedElements.Add(_selectedElement);
            }
            if (!_selectedElements.Contains(elem))
            {
                elem.IsSelected = true;
                _selectedElements.Add(elem);
            }
            _selectedElement = elem;
            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(SelectedElementsCount));
            OnPropertyChanged(nameof(HasMultipleSelectedElements));
            NotifySelectedElementProperties();
            UpdatePreview();
        }

        public void SelectAllElements()
        {
            if (CurrentTemplate == null) InitializeDefaultTemplateFromCurrentSettings();
            if (CurrentTemplate?.Elements == null || CurrentTemplate.Elements.Count == 0) return;
            SetSelectedElements(CurrentTemplate.Elements);
            StatusMessage = $"총 {CurrentTemplate.Elements.Count}개 요소 전체가 선택되었습니다 (Ctrl+A).";
        }

        #region 선택 요소 정렬 (Alignment & Distribution)

        public void AlignSelectedElements(string alignmentType)
        {
            var targets = _selectedElements.Count >= 2 
                ? _selectedElements 
                : (SelectedElement != null ? new List<TemplateElement> { SelectedElement } : new List<TemplateElement>());

            if (targets.Count == 0) return;

            RecordUndoSnapshot();

            // 1개 요소만 선택되었을 때는 라벨 용지 전체 기준 정렬
            if (targets.Count == 1)
            {
                var el = targets[0];
                double paperW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 80.0;
                double paperH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 60.0;

                switch (alignmentType?.ToUpper())
                {
                    case "LEFT":
                        el.X = 2.0;
                        break;
                    case "CENTER":
                        el.X = Math.Max(0, Math.Round(((paperW - el.Width) / 2.0) * 2.0) / 2.0);
                        break;
                    case "RIGHT":
                        el.X = Math.Max(0, Math.Round((paperW - el.Width - 2.0) * 2.0) / 2.0);
                        break;
                    case "TOP":
                        el.Y = 2.0;
                        break;
                    case "MIDDLE":
                        el.Y = Math.Max(0, Math.Round(((paperH - el.Height) / 2.0) * 2.0) / 2.0);
                        break;
                    case "BOTTOM":
                        el.Y = Math.Max(0, Math.Round((paperH - el.Height - 2.0) * 2.0) / 2.0);
                        break;
                }
                SyncElementToLegacyColumn(el);
                StatusMessage = $"선택한 요소가 라벨 기준으로 {GetAlignName(alignmentType)}되었습니다.";
                UpdatePreview();
                return;
            }

            // 2개 이상 다중 선택되었을 때는 선택된 요소 그룹 기준 정렬
            double minX = targets.Min(e => e.X);
            double maxX = targets.Max(e => e.X + e.Width);
            double minY = targets.Min(e => e.Y);
            double maxY = targets.Max(e => e.Y + e.Height);
            double centerX = (minX + maxX) / 2.0;
            double centerY = (minY + maxY) / 2.0;

            switch (alignmentType?.ToUpper())
            {
                case "LEFT":
                    foreach (var el in targets) el.X = minX;
                    break;
                case "CENTER":
                    foreach (var el in targets) el.X = Math.Round((centerX - (el.Width / 2.0)) * 2.0) / 2.0;
                    break;
                case "RIGHT":
                    foreach (var el in targets) el.X = maxX - el.Width;
                    break;
                case "TOP":
                    foreach (var el in targets) el.Y = minY;
                    break;
                case "MIDDLE":
                    foreach (var el in targets) el.Y = Math.Round((centerY - (el.Height / 2.0)) * 2.0) / 2.0;
                    break;
                case "BOTTOM":
                    foreach (var el in targets) el.Y = maxY - el.Height;
                    break;
                case "DISTRIBUTE_HORIZONTALLY":
                    if (targets.Count >= 3)
                    {
                        var sorted = targets.OrderBy(e => e.X).ToList();
                        double totalElemW = sorted.Sum(e => e.Width);
                        double totalSpan = (sorted.Last().X + sorted.Last().Width) - sorted.First().X;
                        double totalGap = totalSpan - totalElemW;
                        double gap = totalGap / (sorted.Count - 1);
                        double curX = sorted.First().X;
                        for (int i = 0; i < sorted.Count; i++)
                        {
                            sorted[i].X = Math.Round(curX * 2.0) / 2.0;
                            curX += sorted[i].Width + gap;
                        }
                    }
                    else
                    {
                        StatusMessage = "가로 등간격 배분은 3개 이상의 요소를 선택해야 합니다.";
                        return;
                    }
                    break;
                case "DISTRIBUTE_VERTICALLY":
                    if (targets.Count >= 3)
                    {
                        var sorted = targets.OrderBy(e => e.Y).ToList();
                        double totalElemH = sorted.Sum(e => e.Height);
                        double totalSpan = (sorted.Last().Y + sorted.Last().Height) - sorted.First().Y;
                        double totalGap = totalSpan - totalElemH;
                        double gap = totalGap / (sorted.Count - 1);
                        double curY = sorted.First().Y;
                        for (int i = 0; i < sorted.Count; i++)
                        {
                            sorted[i].Y = Math.Round(curY * 2.0) / 2.0;
                            curY += sorted[i].Height + gap;
                        }
                    }
                    else
                    {
                        StatusMessage = "세로 등간격 배분은 3개 이상의 요소를 선택해야 합니다.";
                        return;
                    }
                    break;
            }

            foreach (var el in targets) SyncElementToLegacyColumn(el);
            StatusMessage = $"선택한 {targets.Count}개 요소가 {GetAlignName(alignmentType)}되었습니다.";
            UpdatePreview();
        }

        private string GetAlignName(string type)
        {
            switch (type?.ToUpper())
            {
                case "LEFT": return "왼쪽 정렬";
                case "CENTER": return "수평 가운데 정렬";
                case "RIGHT": return "오른쪽 정렬";
                case "TOP": return "위쪽 정렬";
                case "MIDDLE": return "수직 가운데 정렬";
                case "BOTTOM": return "아래쪽 정렬";
                case "DISTRIBUTE_HORIZONTALLY": return "가로 등간격 배분";
                case "DISTRIBUTE_VERTICALLY": return "세로 등간격 배분";
                default: return "정렬";
            }
        }

        #endregion

        public void DuplicateSelectedElement()
        {
            if (SelectedElement == null || CurrentTemplate == null) return;

            RecordUndoSnapshot();
            var copy = SelectedElement.Clone();
            copy.X = Math.Min(EffectivePaperWidth - 5.0, SelectedElement.X + 3.0);
            copy.Y = Math.Min(EffectivePaperHeight - 5.0, SelectedElement.Y + 3.0);
            copy.HeaderName = SelectedElement.HeaderName + " (복사)";

            CurrentTemplate.Elements.Add(copy);
            SelectedElement = copy;
            OnPropertyChanged(nameof(TemplateElements));
            StatusMessage = "선택한 요소가 복제되었습니다 (Ctrl+D).";
            UpdatePreview();
        }

        public void MoveSelectedElement(double deltaX, double deltaY)
        {
            if (SelectedElement == null && _selectedElements.Count == 0) return;
            RecordUndoSnapshot();

            var targets = _selectedElements.Count > 0 ? _selectedElements.ToList() : new List<TemplateElement> { SelectedElement };
            foreach (var elem in targets)
            {
                elem.X = Math.Max(0, Math.Min(EffectivePaperWidth - 1.0, elem.X + deltaX));
                elem.Y = Math.Max(0, Math.Min(EffectivePaperHeight - 1.0, elem.Y + deltaY));
                SyncElementToLegacyColumn(elem);
            }
            UpdatePreview();
        }

        public void DeleteSelectedElement()
        {
            if (CurrentTemplate == null) return;
            var targets = _selectedElements.Count > 0 ? _selectedElements.ToList() : (SelectedElement != null ? new List<TemplateElement> { SelectedElement } : null);
            if (targets == null || targets.Count == 0) return;

            RecordUndoSnapshot();

            bool isDeletingAll = targets.Count >= CurrentTemplate.Elements.Count;

            if (isDeletingAll)
            {
                CurrentTemplate.Elements.Clear();
                ClearAllColumnsData();
                ActiveColumnCount = 1;
                ClearSelectedElements();
                SelectedElement = null;
                SelectedItemIndex = -1;
                StatusMessage = "모든 라벨 요소가 삭제되었습니다 (Del).";
            }
            else
            {
                foreach (var elem in targets)
                {
                    CurrentTemplate.Elements.Remove(elem);
                    if (elem.IsVariable && elem.InputOrder <= 8)
                    {
                        ClearColumnData(elem.InputOrder);
                    }
                }

                // 남은 가변 요소들의 InputOrder 재정렬 및 레거시 컬럼 동기화
                int order = 1;
                var variableElems = CurrentTemplate.Elements.Where(e => e.IsVariable).OrderBy(e => e.InputOrder).ToList();
                foreach (var elem in variableElems)
                {
                    elem.InputOrder = order;
                    if (order <= 8)
                    {
                        SyncElementToLegacyColumn(elem);
                    }
                    order++;
                }

                ActiveColumnCount = Math.Max(1, Math.Min(8, variableElems.Count));

                for (int c = ActiveColumnCount + 1; c <= 8; c++)
                {
                    ClearColumnData(c);
                }

                ClearSelectedElements();
                SelectedElement = CurrentTemplate.Elements.LastOrDefault();
                SelectedItemIndex = (SelectedElement != null && SelectedElement.IsVariable && SelectedElement.InputOrder <= 8)
                    ? SelectedElement.InputOrder - 1
                    : -1;

                StatusMessage = targets.Count > 1 ? $"{targets.Count}개 요소가 삭제되었습니다 (Del)." : "선택한 라벨 요소가 삭제되었습니다 (Del).";
            }

            OnPropertyChanged(nameof(TemplateElements));
            UpdatePreview();
        }

        #endregion

        #region 기존 8열 및 설정과 LabelTemplateModel 상호 동기화

        public bool GetKeepByIndex(int idx)
        {
            switch (idx)
            {
                case 1: return IsCol1Keep;
                case 2: return IsCol2Keep;
                case 3: return IsCol3Keep;
                case 4: return IsCol4Keep;
                case 5: return IsCol5Keep;
                case 6: return IsCol6Keep;
                case 7: return IsCol7Keep;
                case 8: return IsCol8Keep;
                default: return false;
            }
        }

        public void SetKeepByIndex(int idx, bool keep)
        {
            switch (idx)
            {
                case 1: IsCol1Keep = keep; break;
                case 2: IsCol2Keep = keep; break;
                case 3: IsCol3Keep = keep; break;
                case 4: IsCol4Keep = keep; break;
                case 5: IsCol5Keep = keep; break;
                case 6: IsCol6Keep = keep; break;
                case 7: IsCol7Keep = keep; break;
                case 8: IsCol8Keep = keep; break;
            }
        }

        /// <summary>
        /// 기존 AppSettings 및 1~8열 상태로부터 초기 템플릿 모델 구축
        /// </summary>
        public void InitializeDefaultTemplateFromCurrentSettings()
        {
            _currentTemplate = TemplateMigrationHelper.ExportFromViewModel(
                EffectivePaperWidth, EffectivePaperHeight, LabelRotationAngle,
                ActiveColumnCount,
                GetHeaderByIndex,
                GetInputByIndex,
                GetSizeByIndex,
                GetFontByIndex,
                GetBoldByIndex,
                GetItalicByIndex,
                GetAlignByIndex,
                GetBarcodeByIndex,
                GetBarcodeTypeByIndex,
                GetOffsetXByIndex,
                GetOffsetYByIndex,
                GetWidthByIndex,
                GetIncludeHeaderByIndex,
                GetPriceByIndex,
                GetKeepByIndex
            );

            // 표 상단 제목(Title)이 활성화되어 있다면 별도 텍스트 요소로 등록
            if (IsTableTitleEnabled && !string.IsNullOrWhiteSpace(TableTitleText))
            {
                _currentTemplate.Elements.Insert(0, new TemplateElement
                {
                    Id = "TITLE_ELEMENT",
                    Type = ElementType.TEXT,
                    Content = TableTitleText,
                    HeaderName = "상단 제목",
                    FontSize = TableTitleSize,
                    FontName = TableTitleFont,
                    IsBold = TableTitleBold,
                    IsItalic = TableTitleItalic,
                    Alignment = TableTitleAlign,
                    X = TableOffsetX + TableTitleOffsetX,
                    Y = TableOffsetY + TableTitleOffsetY,
                    Width = (TableWidth > 0 ? TableWidth : EffectivePaperWidth - 4),
                    Height = Math.Max(4.0, TableTitleHeight),
                    IsVariable = false
                });
            }

            // 날짜/시간(DateTime)이 활성화되어 있다면 별도 요소로 등록
            if (IsDateTimeEnabled)
            {
                _currentTemplate.Elements.Add(new TemplateElement
                {
                    Id = "DATETIME_ELEMENT",
                    Type = ElementType.DATE_TIME,
                    Content = DateTime.Now.ToString("yyyy-MM-dd"),
                    HeaderName = "일자/시각",
                    FontSize = DateTimeSize,
                    FontName = DateTimeFont,
                    IsBold = DateTimeBold,
                    IsItalic = DateTimeItalic,
                    Alignment = "LEFT",
                    X = DateTimeOffsetX,
                    Y = DateTimeOffsetY,
                    Width = 40.0,
                    Height = 6.0,
                    IsVariable = false
                });
            }
        }

        /// <summary>
        /// 요소의 변경 내용을 기존의 1~8열 변수에 즉시 반영하여 완벽한 하위 호환성 유지
        /// </summary>
        public void SyncElementToLegacyColumn(TemplateElement elem)
        {
            if (elem == null || !elem.IsVariable) return;

            int idx = elem.InputOrder;
            if (idx >= 1 && idx <= 8)
            {
                SetHeaderByIndex(idx, elem.HeaderName);
                SetInputByIndex(idx, elem.Content);
                SetSizeByIndex(idx, (int)Math.Round(elem.FontSize));
                SetFontByIndex(idx, elem.FontName);
                SetBoldByIndex(idx, elem.IsBold);
                SetItalicByIndex(idx, elem.IsItalic);
                SetAlignByIndex(idx, elem.Alignment);
                SetBarcodeByIndex(idx, elem.Type == ElementType.BARCODE || elem.Type == ElementType.QR_CODE);
                SetBarcodeTypeByIndex(idx, elem.BarcodeType);
                SetOffsetXByIndex(idx, (int)Math.Round(elem.X));
                SetOffsetYByIndex(idx, (int)Math.Round(elem.Y));
                SetWidthByIndex(idx, (int)Math.Round(elem.Width));
                SetIncludeHeaderByIndex(idx, elem.IncludeHeaderOnLabel);
                SetPriceByIndex(idx, elem.IsPriceFormat);
                SetKeepByIndex(idx, elem.IsKeep);
            }
        }

        public string SelectedElementEffectivePreviewText
        {
            get
            {
                if (SelectedElement == null) return "";
                return GetElementEffectiveContent(SelectedElement);
            }
        }

        public string GetElementEffectiveContent(TemplateElement elem)
        {
            if (elem == null) return "";

            // 1. 엑셀 연동 컬럼이 지정되어 있는 경우 (인덱스 또는 헤더명)
            if (!string.IsNullOrEmpty(elem.DataSourceColumn) && IsExcelFileLoaded && Records != null && Records.Count > 0)
            {
                PrintRecordModel targetRecord = null;
                if (CurrentPreviewIndex > 0 && CurrentPreviewIndex <= Records.Count)
                {
                    targetRecord = Records[CurrentPreviewIndex - 1];
                }
                else if (SelectedRecord != null)
                {
                    targetRecord = SelectedRecord;
                }
                else if (CurrentPreviewIndex <= 0 && !IsLiveInputMode)
                {
                    targetRecord = Records[0];
                }

                if (targetRecord != null)
                {
                    string mappedVal = targetRecord.GetValueByColumn(elem.DataSourceColumn);
                    if (mappedVal != null)
                    {
                        return mappedVal;
                    }
                }
            }

            // 1-1. 실시간 인쇄 중인 경우 현재 인쇄 레코드에서 값 우선 조회
            if (_currentPrintingRecord != null && elem.IsVariable && elem.InputOrder >= 1 && elem.InputOrder <= ActiveColumnCount)
            {
                string printVal = _currentPrintingRecord.GetValueByColumn(elem.InputOrder.ToString());
                if (!string.IsNullOrEmpty(printVal))
                {
                    return printVal;
                }
            }

            // 2. 가변 컬럼 요소(1~N열 제한 해제)이고 엑셀 레코드 탐색 중(CurrentPreviewIndex > 0)인 경우
            if (CurrentPreviewIndex > 0 && Records != null && CurrentPreviewIndex <= Records.Count && elem.IsVariable && elem.InputOrder >= 1 && elem.InputOrder <= ActiveColumnCount)
            {
                var record = Records[CurrentPreviewIndex - 1];
                if (record != null)
                {
                    string val = record.GetValueByColumn(elem.InputOrder.ToString());
                    if (val != null)
                    {
                        return val;
                    }
                }
            }
            else if (SelectedWorkMode == 1 && SelectedRecord != null && elem.IsVariable && elem.InputOrder >= 1 && elem.InputOrder <= ActiveColumnCount)
            {
                string val = SelectedRecord.GetValueByColumn(elem.InputOrder.ToString());
                if (val != null)
                {
                    return val;
                }
            }

            // 2-1. 단건 직접 입력 모드에서 입력란(InputCol1~8)에 값이 있는 경우 우선 반영
            if (elem.IsVariable && elem.InputOrder >= 1 && elem.InputOrder <= 8)
            {
                string inputVal = GetInputByIndex(elem.InputOrder);
                if (!string.IsNullOrEmpty(inputVal))
                {
                    return inputVal;
                }
            }

            if (elem.Type == ElementType.DATE_TIME)
            {
                if (elem.AutoUpdateDate)
                {
                    string fmt = string.IsNullOrWhiteSpace(elem.DateTimeFormat) ? "yyyy-MM-dd HH:mm" : elem.DateTimeFormat;
                    try
                    {
                        return DateTime.Now.ToString(fmt, System.Globalization.CultureInfo.CurrentCulture);
                    }
                    catch
                    {
                        return DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    }
                }
                else
                {
                    return !string.IsNullOrWhiteSpace(elem.Content) ? elem.Content : DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                }
            }

            return elem.Content ?? "";
        }

        public void ExecuteUnlinkExcel()
        {
            LoadedExcelFileName = "";
            LoadedExcelHeaders = new List<string>();
            Records.Clear();
            CurrentPreviewIndex = 0;
            StatusMessage = "엑셀 연동이 해제되었습니다.";
            OnPropertyChanged(nameof(IsExcelFileLoaded));
            OnPropertyChanged(nameof(IsExcelFileNotLoaded));
            OnPropertyChanged(nameof(IsSelectedElementExcelLinked));
            OnPropertyChanged(nameof(IsSelectedElementExcelPending));
            OnPropertyChanged(nameof(AvailableExcelColumnOptions));
            OnPropertyChanged(nameof(LoadedExcelFileName));
            OnPropertyChanged(nameof(PreviewPageText));
            UpdatePreview();
        }

        #endregion
    }

    /// <summary>
    /// 날짜/시간 서식 드롭다운 항목 모델
    /// </summary>
    public class DateTimeFormatOptionItem
    {
        public string Format { get; set; }
        public string DisplayText { get; set; }

        public DateTimeFormatOptionItem(string format, string displayText)
        {
            Format = format;
            DisplayText = displayText;
        }

        public override string ToString() => DisplayText ?? "";
    }
}
