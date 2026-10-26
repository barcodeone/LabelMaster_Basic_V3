using System;
using System.Collections.Generic;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 안드로이드(Label_Master) TemplateElement와 100% 호환되는 자유 배치 라벨 요소 모델
    /// </summary>
    [Serializable]
    public class TemplateElement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ElementType Type { get; set; } = ElementType.TEXT;

        // 좌표 및 크기 (mm 단위)
        public double X { get; set; } = 2.0;
        public double Y { get; set; } = 2.0;
        public double Width { get; set; } = 30.0;
        public double Height { get; set; } = 8.0;
        public int Rotation { get; set; } = 0; // 0, 90, 180, 270

        // 텍스트/내용 속성
        public string Content { get; set; } = "";
        public string FontName { get; set; } = "맑은 고딕";
        public double FontSize { get; set; } = 10.0; // mm 또는 pt 기준 크기
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;
        public bool IsUnderline { get; set; } = false;
        public string Alignment { get; set; } = "LEFT"; // LEFT, CENTER, RIGHT
        public double LetterSpacing { get; set; } = 0.0; // 자간 (em 단위, 예: 0.0, 0.05, 0.1, -0.05)
        public double FontScaleX { get; set; } = 1.0;    // 장평 (가로 비율, 1.0 = 100%, 0.8 = 80%, 1.2 = 120%)
        public double LineSpacing { get; set; } = 1.0;   // 줄간격 (배수, 1.0 = 100%, 1.2 = 120%, 1.5 = 150%)

        // 바코드 속성
        public string BarcodeType { get; set; } = "CODE128"; // CODE128, QR_CODE, EAN13, CODE39 등
        public string BarcodeTextPosition { get; set; } = "BELOW"; // BELOW, ABOVE, NONE
        public bool ShowBarcodeText { get; set; } = true;

        // [★ 기존 텍스트 입력창 연동 핵심 속성]
        public bool IsVariable { get; set; } = true;    // 우측 직접 입력 패널에 표시할 가변 항목인지 여부
        public int InputOrder { get; set; } = 1;        // 우측 입력창 탭 순서 (1열, 2열, ...)
        public string HeaderName { get; set; } = "항목"; // 입력창 상단에 표시될 라벨명
        public bool IsKeep { get; set; } = false;       // 인쇄 후 내용 유지 여부
        public bool IncludeHeaderOnLabel { get; set; } = false; // "항목: 내용" 형태로 라벨에 함께 출력 여부
        public bool IsPriceFormat { get; set; } = false; // 금액 콤마 서식(₩) 적용 여부

        // [일시(DATE_TIME) 속성]
        public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm"; // 날짜/시간 서식 (예: yyyy-MM-dd, yyyy.MM.dd, yyyy-MM-dd HH:mm 등)
        public bool AutoUpdateDate { get; set; } = true;                  // 인쇄 및 렌더링 시 현재 시각 자동 업데이트 여부

        // 도형 / 선 속성
        public string ShapeType { get; set; } = "RECTANGLE"; // RECTANGLE, ROUNDED_RECT, CIRCLE, TRIANGLE, DIAMOND, etc.
        public string LineStyle { get; set; } = "SOLID";     // SOLID, DASHED, DOTTED, DASH_DOT, DOUBLE, WAVY, ZIGZAG
        public bool IsFilled { get; set; } = false;
        public float LineWidth { get; set; } = 1.0f;

        // 표(Table) 속성
        public int Rows { get; set; } = 2;
        public int Columns { get; set; } = 2;
        public string[][] TableData { get; set; }
        public double[] RowHeights { get; set; }
        public double[] ColumnWidths { get; set; }
        public double CellPadding { get; set; } = 1.0;

        // 엑셀 연동 속성
        public string DataSourceColumn { get; set; } // 엑셀 매핑 컬럼명
        public Dictionary<int, string> TableColumnMapping { get; set; } = new Dictionary<int, string>();
        public Dictionary<string, string> TableCellMapping { get; set; } = new Dictionary<string, string>(); // 표 개별 셀 엑셀 매핑 ("{r}_{c}" -> 컬럼번호)
        public Dictionary<string, string> TableCellStyles { get; set; } = new Dictionary<string, string>();  // 표 행/열/셀별 서식 ("{r}_{c}", "row_{r}", "col_{c}" -> "FontName|FontSize|IsBold|Alignment")

        public string GetCellMapping(int r, int c)
        {
            if (TableCellMapping == null) TableCellMapping = new Dictionary<string, string>();
            string key = $"{r}_{c}";
            return TableCellMapping.TryGetValue(key, out string col) ? col : "0";
        }

        public void SetCellMapping(int r, int c, string col)
        {
            if (TableCellMapping == null) TableCellMapping = new Dictionary<string, string>();
            string key = $"{r}_{c}";
            if (string.IsNullOrEmpty(col) || col == "0")
            {
                TableCellMapping.Remove(key);
            }
            else
            {
                TableCellMapping[key] = col;
            }
        }

        /// <summary>
        /// 표의 특정 셀(r, c)에 적용될 최종 글꼴, 크기, 굵게, 정렬 상태를 계산합니다. (셀 개별 -> 행 -> 열 -> 기본 서식 순)
        /// </summary>
        public TableCellStyleInfo GetCellResolvedStyle(int r, int c)
        {
            string fName = string.IsNullOrWhiteSpace(this.FontName) ? "맑은 고딕" : this.FontName;
            double fSize = this.FontSize > 0 ? this.FontSize : 9.0;
            bool fBold = this.IsBold || (r == 0 && this.IsFilled);
            string fAlign = this.Alignment ?? "CENTER";

            if (TableCellStyles != null)
            {
                // 1순위: 셀 개별 서식
                if (TableCellStyles.TryGetValue($"{r}_{c}", out string cellStyle))
                {
                    ApplyStyleString(cellStyle, ref fName, ref fSize, ref fBold, ref fAlign);
                }
                // 2순위: 행 서식
                else if (TableCellStyles.TryGetValue($"row_{r}", out string rowStyle))
                {
                    ApplyStyleString(rowStyle, ref fName, ref fSize, ref fBold, ref fAlign);
                }
                // 3순위: 열 서식
                else if (TableCellStyles.TryGetValue($"col_{c}", out string colStyle))
                {
                    ApplyStyleString(colStyle, ref fName, ref fSize, ref fBold, ref fAlign);
                }
            }

            return new TableCellStyleInfo(fName, fSize, fBold, fAlign);
        }

        private static void ApplyStyleString(string styleStr, ref string fName, ref double fSize, ref bool fBold, ref string fAlign)
        {
            if (string.IsNullOrWhiteSpace(styleStr)) return;
            string[] parts = styleStr.Split('|');
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])) fName = parts[0];
            if (parts.Length > 1 && double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double sz) && sz > 0) fSize = sz;
            if (parts.Length > 2 && bool.TryParse(parts[2], out bool bld)) fBold = bld;
            if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3])) fAlign = parts[3];
        }

        public void SetCellStyle(int r, int c, string fontName, double? fontSize, bool? isBold, string alignment)
        {
            if (TableCellStyles == null) TableCellStyles = new Dictionary<string, string>();
            string key = $"{r}_{c}";
            TableCellStyles[key] = BuildStyleString(fontName, fontSize, isBold, alignment);
        }

        public void SetRowStyle(int r, string fontName, double? fontSize, bool? isBold, string alignment)
        {
            if (TableCellStyles == null) TableCellStyles = new Dictionary<string, string>();
            string style = BuildStyleString(fontName, fontSize, isBold, alignment);
            TableCellStyles[$"row_{r}"] = style;
            // 해당 행의 개별 셀 오버라이드도 통일
            for (int c = 0; c < Columns; c++)
            {
                TableCellStyles[$"{r}_{c}"] = style;
            }
        }

        public void SetColumnStyle(int c, string fontName, double? fontSize, bool? isBold, string alignment)
        {
            if (TableCellStyles == null) TableCellStyles = new Dictionary<string, string>();
            string style = BuildStyleString(fontName, fontSize, isBold, alignment);
            TableCellStyles[$"col_{c}"] = style;
            // 해당 열의 개별 셀 오버라이드도 통일
            for (int r = 0; r < Rows; r++)
            {
                TableCellStyles[$"{r}_{c}"] = style;
            }
        }

        public void ClearAllCellStyles()
        {
            TableCellStyles?.Clear();
        }

        private static string BuildStyleString(string fontName, double? fontSize, bool? isBold, string alignment)
        {
            string f = fontName ?? "";
            string s = fontSize.HasValue ? fontSize.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "";
            string b = isBold.HasValue ? isBold.Value.ToString() : "";
            string a = alignment ?? "";
            return $"{f}|{s}|{b}|{a}";
        }

        public void EnsureTableData()
        {
            if (Type != ElementType.TABLE) return;
            if (Rows < 1) Rows = 2;
            if (Columns < 1) Columns = 2;
            if (TableData == null || TableData.Length != Rows)
            {
                var oldData = TableData;
                TableData = new string[Rows][];
                for (int r = 0; r < Rows; r++)
                {
                    TableData[r] = new string[Columns];
                    for (int c = 0; c < Columns; c++)
                    {
                        if (oldData != null && r < oldData.Length && c < oldData[r].Length)
                            TableData[r][c] = oldData[r][c] ?? "";
                        else
                            TableData[r][c] = (r == 0) ? $"항목{c + 1}" : "";
                    }
                }
            }
            else
            {
                for (int r = 0; r < Rows; r++)
                {
                    if (TableData[r] == null || TableData[r].Length != Columns)
                    {
                        var oldRow = TableData[r];
                        TableData[r] = new string[Columns];
                        for (int c = 0; c < Columns; c++)
                        {
                            if (oldRow != null && c < oldRow.Length)
                                TableData[r][c] = oldRow[c] ?? "";
                            else
                                TableData[r][c] = (r == 0) ? $"항목{c + 1}" : "";
                        }
                    }
                }
            }
        }

        // UI 선택 상태
        [NonSerialized]
        public bool IsSelected = false;

        public TemplateElement Clone()
        {
            var clone = (TemplateElement)this.MemberwiseClone();
            clone.Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            if (TableData != null)
            {
                clone.TableData = new string[TableData.Length][];
                for (int r = 0; r < TableData.Length; r++)
                {
                    if (TableData[r] != null)
                    {
                        clone.TableData[r] = (string[])TableData[r].Clone();
                    }
                }
            }
            if (RowHeights != null) clone.RowHeights = (double[])RowHeights.Clone();
            if (ColumnWidths != null) clone.ColumnWidths = (double[])ColumnWidths.Clone();
            if (TableColumnMapping != null) clone.TableColumnMapping = new Dictionary<int, string>(TableColumnMapping);
            if (TableCellMapping != null) clone.TableCellMapping = new Dictionary<string, string>(TableCellMapping);
            if (TableCellStyles != null) clone.TableCellStyles = new Dictionary<string, string>(TableCellStyles);
            return clone;
        }
    }

    /// <summary>
    /// 표 셀의 최종 해석된 글꼴 스타일 정보
    /// </summary>
    public class TableCellStyleInfo
    {
        public string FontName { get; set; } = "맑은 고딕";
        public double FontSize { get; set; } = 9.0;
        public bool IsBold { get; set; } = false;
        public string Alignment { get; set; } = "CENTER";

        public TableCellStyleInfo() { }

        public TableCellStyleInfo(string fontName, double fontSize, bool isBold, string alignment)
        {
            FontName = string.IsNullOrWhiteSpace(fontName) ? "맑은 고딕" : fontName;
            FontSize = fontSize > 0 ? fontSize : 9.0;
            IsBold = isBold;
            Alignment = alignment ?? "CENTER";
        }

        public void Deconstruct(out string fontName, out double fontSize, out bool isBold, out string alignment)
        {
            fontName = FontName;
            fontSize = FontSize;
            isBold = IsBold;
            alignment = Alignment;
        }
    }
}
