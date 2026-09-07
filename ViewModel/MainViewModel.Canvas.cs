using System;
using System.Collections.Generic;
using System.Windows;
using BarcodePrint.Model;

namespace BarcodePrint.ViewModel
{
    public partial class MainViewModel
    {
        public void UpdatePreview()
        {
            try
            {
                RequestPreviewRedraw?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePreview Exception: {ex.Message}");
            }
        }

        public void SetItemOffsetDirect(int itemIndex, int xMm, int yMm, bool triggerPreview = false)
        {
            int effW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 70;
            int effH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 30;
            int clampedX = Math.Max(0, Math.Min(effW - 2, xMm));
            int clampedY = Math.Max(0, Math.Min(effH - 2, yMm));

            switch (itemIndex)
            {
                case 0: Col1OffsetX = clampedX; Col1OffsetY = clampedY; break;
                case 1: Col2OffsetX = clampedX; Col2OffsetY = clampedY; break;
                case 2: Col3OffsetX = clampedX; Col3OffsetY = clampedY; break;
                case 3: Col4OffsetX = clampedX; Col4OffsetY = clampedY; break;
                case 4: Col5OffsetX = clampedX; Col5OffsetY = clampedY; break;
                case 5: Col6OffsetX = clampedX; Col6OffsetY = clampedY; break;
                case 6: Col7OffsetX = clampedX; Col7OffsetY = clampedY; break;
                case 7: Col8OffsetX = clampedX; Col8OffsetY = clampedY; break;
                case 99: DateTimeOffsetX = clampedX; DateTimeOffsetY = clampedY; break;
                case 100: TableTitleOffsetX = clampedX; TableTitleOffsetY = clampedY; break;
                case 101: SerialOffsetX = clampedX; SerialOffsetY = clampedY; break;
            }

            if (triggerPreview)
            {
                UpdatePreview();
            }
        }

        public void UpdateItemPositionByDrag(int itemIndex, int deltaXMm, int deltaYMm)
        {
            int effW = EffectivePaperWidth > 0 ? EffectivePaperWidth : 70;
            int effH = EffectivePaperHeight > 0 ? EffectivePaperHeight : 30;
            switch (itemIndex)
            {
                case 0: Col1OffsetX = Math.Max(0, Math.Min(effW - 2, Col1OffsetX + deltaXMm)); Col1OffsetY = Math.Max(0, Math.Min(effH - 2, Col1OffsetY + deltaYMm)); break;
                case 1: Col2OffsetX = Math.Max(0, Math.Min(effW - 2, Col2OffsetX + deltaXMm)); Col2OffsetY = Math.Max(0, Math.Min(effH - 2, Col2OffsetY + deltaYMm)); break;
                case 2: Col3OffsetX = Math.Max(0, Math.Min(effW - 2, Col3OffsetX + deltaXMm)); Col3OffsetY = Math.Max(0, Math.Min(effH - 2, Col3OffsetY + deltaYMm)); break;
                case 3: Col4OffsetX = Math.Max(0, Math.Min(effW - 2, Col4OffsetX + deltaXMm)); Col4OffsetY = Math.Max(0, Math.Min(effH - 2, Col4OffsetY + deltaYMm)); break;
                case 4: Col5OffsetX = Math.Max(0, Math.Min(effW - 2, Col5OffsetX + deltaXMm)); Col5OffsetY = Math.Max(0, Math.Min(effH - 2, Col5OffsetY + deltaYMm)); break;
                case 5: Col6OffsetX = Math.Max(0, Math.Min(effW - 2, Col6OffsetX + deltaXMm)); Col6OffsetY = Math.Max(0, Math.Min(effH - 2, Col6OffsetY + deltaYMm)); break;
                case 6: Col7OffsetX = Math.Max(0, Math.Min(effW - 2, Col7OffsetX + deltaXMm)); Col7OffsetY = Math.Max(0, Math.Min(effH - 2, Col7OffsetY + deltaYMm)); break;
                case 7: Col8OffsetX = Math.Max(0, Math.Min(effW - 2, Col8OffsetX + deltaXMm)); Col8OffsetY = Math.Max(0, Math.Min(effH - 2, Col8OffsetY + deltaYMm)); break;
                case 99: DateTimeOffsetX = Math.Max(0, Math.Min(effW - 2, DateTimeOffsetX + deltaXMm)); DateTimeOffsetY = Math.Max(0, Math.Min(effH - 2, DateTimeOffsetY + deltaYMm)); break;
                case 100: TableTitleOffsetX = Math.Max(-20, Math.Min(effW - 2, TableTitleOffsetX + deltaXMm)); TableTitleOffsetY = Math.Max(0, Math.Min(effH - 2, TableTitleOffsetY + deltaYMm)); break;
                case 101: SerialOffsetX = Math.Max(0, Math.Min(effW - 2, SerialOffsetX + deltaXMm)); SerialOffsetY = Math.Max(0, Math.Min(effH - 2, SerialOffsetY + deltaYMm)); break;
            }
        }

        public void ChangeItemFontSize(int itemIndex, int delta)
        {
            switch (itemIndex)
            {
                case 0:
                case 1: Col1Size = Math.Max(6, Math.Min(40, Col1Size + delta)); break;
                case 2: Col2Size = Math.Max(6, Math.Min(40, Col2Size + delta)); break;
                case 3: Col3Size = Math.Max(6, Math.Min(40, Col3Size + delta)); break;
                case 4: Col4Size = Math.Max(6, Math.Min(40, Col4Size + delta)); break;
                case 5: Col5Size = Math.Max(6, Math.Min(40, Col5Size + delta)); break;
                case 6: Col6Size = Math.Max(6, Math.Min(40, Col6Size + delta)); break;
                case 7: Col7Size = Math.Max(6, Math.Min(40, Col7Size + delta)); break;
                case 8: Col8Size = Math.Max(6, Math.Min(40, Col8Size + delta)); break;
                case 99: DateTimeSize = Math.Max(6, Math.Min(40, DateTimeSize + delta)); break;
                case 100: TableTitleSize = Math.Max(6, Math.Min(36, TableTitleSize + delta)); break;
                case 101: SerialSize = Math.Max(6, Math.Min(40, SerialSize + delta)); break;
            }
        }

        private void ChangeSizeByParam(object param, int delta)
        {
            if (param != null && int.TryParse(param.ToString(), out int idx))
            {
                ChangeItemFontSize(idx, delta);
            }
        }

        public void ResetLayout()
        {
            Col1OffsetX = 2; Col1OffsetY = 2;
            Col2OffsetX = 2; Col2OffsetY = 12;
            Col3OffsetX = 2; Col3OffsetY = 22;
            Col4OffsetX = 2; Col4OffsetY = 32;
            Col5OffsetX = 2; Col5OffsetY = 42;
            Col6OffsetX = 2; Col6OffsetY = 52;
            Col7OffsetX = 2; Col7OffsetY = 62;
            Col8OffsetX = 2; Col8OffsetY = 72;
            DateTimeOffsetX = 2; DateTimeOffsetY = Math.Max(2, (EffectivePaperHeight > 0 ? EffectivePaperHeight : PaperHeight) - 8);

            UpdatePreview();
            StatusMessage = "라벨 레이아웃 위치가 기본값으로 초기화되었습니다.";
        }

        #region Preview Pagination Navigation Methods

        public void NavigatePreviewFirst()
        {
            if (Records != null && Records.Count > 0)
            {
                CurrentPreviewIndex = 1;
                SelectedRecord = Records[0];
            }
        }

        public void NavigatePreviewPrev()
        {
            if (Records == null || Records.Count == 0) return;
            
            int cur = CurrentPreviewIndex <= 0 ? 1 : CurrentPreviewIndex;
            if (cur > 1)
            {
                CurrentPreviewIndex = cur - 1;
            }
            else
            {
                CurrentPreviewIndex = Records.Count; // 순환
            }
            SelectedRecord = Records[CurrentPreviewIndex - 1];
        }

        public void NavigatePreviewNext()
        {
            if (Records == null || Records.Count == 0) return;

            int cur = CurrentPreviewIndex <= 0 ? 1 : CurrentPreviewIndex;
            if (cur < Records.Count)
            {
                CurrentPreviewIndex = cur + 1;
            }
            else
            {
                CurrentPreviewIndex = 1; // 순환
            }
            SelectedRecord = Records[CurrentPreviewIndex - 1];
        }

        public void NavigatePreviewLast()
        {
            if (Records != null && Records.Count > 0)
            {
                CurrentPreviewIndex = Records.Count;
                SelectedRecord = Records[Records.Count - 1];
            }
        }

        public void NavigatePreviewLiveInput()
        {
            CurrentPreviewIndex = 0;
            SelectedRecord = null;
        }

        /// <summary>
        /// 현재 미리보기 또는 인쇄 시 사용할 1~8번 컬럼의 실제 텍스트 값들을 배열로 반환
        /// </summary>
        public string[] GetEffectivePreviewColumnValues()
        {
            string[] values = new string[8];

            // 단건 직접 입력 탭(SelectedWorkMode == 0)인 경우 항상 사용자가 직접 입력한 값을 반환
            if (SelectedWorkMode == 0)
            {
                values[0] = InputCol1 ?? "";
                values[1] = InputCol2 ?? "";
                values[2] = InputCol3 ?? "";
                values[3] = InputCol4 ?? "";
                values[4] = InputCol5 ?? "";
                values[5] = InputCol6 ?? "";
                values[6] = InputCol7 ?? "";
                values[7] = InputCol8 ?? "";
            }
            // 엑셀/목록 관리 탭(SelectedWorkMode == 1)인 경우 현재 선택/지정된 레코드 값 반환
            else if (CurrentPreviewIndex > 0 && Records != null && CurrentPreviewIndex <= Records.Count)
            {
                var rec = Records[CurrentPreviewIndex - 1];
                values[0] = rec.Col1 ?? "";
                values[1] = rec.Col2 ?? "";
                values[2] = rec.Col3 ?? "";
                values[3] = rec.Col4 ?? "";
                values[4] = rec.Col5 ?? "";
                values[5] = rec.Col6 ?? "";
                values[6] = rec.Col7 ?? "";
                values[7] = rec.Col8 ?? "";
            }
            else if (SelectedRecord != null)
            {
                values[0] = SelectedRecord.Col1 ?? "";
                values[1] = SelectedRecord.Col2 ?? "";
                values[2] = SelectedRecord.Col3 ?? "";
                values[3] = SelectedRecord.Col4 ?? "";
                values[4] = SelectedRecord.Col5 ?? "";
                values[5] = SelectedRecord.Col6 ?? "";
                values[6] = SelectedRecord.Col7 ?? "";
                values[7] = SelectedRecord.Col8 ?? "";
            }
            else
            {
                values[0] = InputCol1 ?? "";
                values[1] = InputCol2 ?? "";
                values[2] = InputCol3 ?? "";
                values[3] = InputCol4 ?? "";
                values[4] = InputCol5 ?? "";
                values[5] = InputCol6 ?? "";
                values[6] = InputCol7 ?? "";
                values[7] = InputCol8 ?? "";
            }

            return values;
        }

        #endregion

        private void ExecuteResetLayout()
        {
            IsTableBorderEnabled = false;
            IsTableTitleEnabled = false;
            Col1OffsetX = 2; Col1OffsetY = 2;
            Col2OffsetX = 2; Col2OffsetY = 6;
            Col3OffsetX = 2; Col3OffsetY = 10;
            Col4OffsetX = 2; Col4OffsetY = 14;
            Col5OffsetX = 2; Col5OffsetY = 18;
            Col6OffsetX = 2; Col6OffsetY = 22;
            Col7OffsetX = 2; Col7OffsetY = 26;
            Col8OffsetX = 2; Col8OffsetY = 30;
            DateTimeOffsetX = 2; DateTimeOffsetY = Math.Max(24, PaperHeight - 5);
            StatusMessage = $"표 맞춤이 해제되고 라벨 기본 위치로 초기화되었습니다 (활성 컬럼: {ActiveColumnCount}개).";
            UpdatePreview();
        }

        #region Text Width Calculation & Alignment Logic

        public double CalculateExactTextWidthMm(string text, string fontName, int fontSizePt, bool isBold, bool isItalic)
        {
            if (string.IsNullOrEmpty(text)) return 0.0;
            try
            {
                string family = string.IsNullOrWhiteSpace(fontName) ? "맑은 고딕" : fontName;
                var style = isItalic ? FontStyles.Italic : FontStyles.Normal;
                var weight = isBold ? FontWeights.Bold : FontWeights.Normal;
                double targetPxSize = fontSizePt * 1.35; // 캔버스 화면 렌더링 폰트 크기와 100% 동일

                var ft = new System.Windows.Media.FormattedText(
                    text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new System.Windows.Media.Typeface(new System.Windows.Media.FontFamily(family), style, weight, FontStretches.Normal),
                    Math.Max(8, targetPxSize),
                    System.Windows.Media.Brushes.Black,
                    1.0);

                return ft.Width / (96.0 / 25.4); // 픽셀 -> mm 정밀 변환
            }
            catch
            {
                return CalculateTextWidthMm(text, fontSizePt);
            }
        }

        public double CalculateTextWidthMm(string text, int fontSizePt)
        {
            if (string.IsNullOrEmpty(text)) return 0.0;
            double totalWidthMm = 0.0;
            double ptToMm = 0.352778; // 1 pt = 0.352778 mm
            
            foreach (char c in text)
            {
                if (c >= 0xAC00 && c <= 0xD7A3 || c >= 0x1100 && c <= 0x11FF || c >= 0x3130 && c <= 0x318F)
                {
                    // 한글 음절 및 자모
                    totalWidthMm += fontSizePt * ptToMm * 0.95;
                }
                else if (char.IsUpper(c))
                {
                    // 영문 대문자
                    totalWidthMm += fontSizePt * ptToMm * 0.68;
                }
                else if (char.IsDigit(c) || char.IsLower(c))
                {
                    // 영문 소문자 및 숫자
                    totalWidthMm += fontSizePt * ptToMm * 0.55;
                }
                else if (char.IsWhiteSpace(c))
                {
                    // 공백
                    totalWidthMm += fontSizePt * ptToMm * 0.35;
                }
                else
                {
                    // 특수문자, 괄호 등
                    totalWidthMm += fontSizePt * ptToMm * 0.6;
                }
            }
            return Math.Max(2.0, totalWidthMm);
        }

        public string GetPreviewDisplayTextForColumn(int colNum)
        {
            if (colNum == 99)
            {
                return $"🕒 {GetFormattedDateTimeString()}";
            }
            if (colNum == 101)
            {
                return $"🔢 {FormattedSerialPreview}";
            }
            string[] values = GetEffectivePreviewColumnValues();
            int i = colNum - 1;
            string val = (i >= 0 && i < values.Length) ? values[i] : "";
            string header = GetHeaderByIndex(colNum);
            bool isPrice = GetPriceByIndex(colNum);
            bool isIncludeHeader = GetIncludeHeaderByIndex(colNum);
            bool isBarcode = GetBarcodeByIndex(colNum);

            string displayVal = isPrice ? PriceFormatHelper.FormatShortPriceInput(val) : val;
            if (string.IsNullOrEmpty(displayVal))
            {
                displayVal = (isIncludeHeader && !isBarcode) ? $"{header}: [{header}]" : $"[{header}]";
            }
            else if (isIncludeHeader && !isBarcode)
            {
                displayVal = $"{header}: {displayVal}";
            }
            return displayVal;
        }

        public void AlignSingleColumn(int colNum, string align)
        {
            SetAlignByIndex(colNum, align);
            if (!IsTableBorderEnabled)
            {
                int paperW = Math.Max(15, EffectivePaperWidth > 0 ? EffectivePaperWidth : PaperWidth);
                double itemWidthMm = 10.0;

                if (GetBarcodeByIndex(colNum))
                {
                    string[] values = GetEffectivePreviewColumnValues();
                    int i = colNum - 1;
                    string val = (i >= 0 && i < values.Length) ? values[i] : "12345678";
                    string bType = GetBarcodeTypeByIndex(colNum);
                    int size = GetSizeByIndex(colNum);

                    if (bType?.ToUpper() == "QR_CODE")
                    {
                        itemWidthMm = Math.Max(52, size * 4.2) / (96.0 / 25.4);
                    }
                    else if (bType?.ToUpper() == "DATAMATRIX" || bType?.ToUpper() == "DATA_MATRIX")
                    {
                        itemWidthMm = Math.Max(48, size * 3.6) / (96.0 / 25.4);
                    }
                    else
                    {
                        string safeContent = string.IsNullOrWhiteSpace(val) ? "12345678" : val;
                        itemWidthMm = Math.Max(80, safeContent.Length * 11) / (96.0 / 25.4);
                    }
                }
                else
                {
                    int userWMm = GetWidthByIndex(colNum);
                    if (userWMm > 0)
                    {
                        itemWidthMm = userWMm;
                    }
                    else
                    {
                        string displayText = GetPreviewDisplayTextForColumn(colNum);
                        string fontName = GetFontByIndex(colNum);
                        int size = GetSizeByIndex(colNum);
                        bool isBold = GetBoldByIndex(colNum);
                        bool isItalic = GetItalicByIndex(colNum);
                        itemWidthMm = CalculateExactTextWidthMm(displayText, fontName, size, isBold, isItalic);
                    }
                }

                int targetX = 2;
                if (align == "CENTER")
                {
                    targetX = Math.Max(0, (int)Math.Round((paperW - itemWidthMm) / 2.0));
                }
                else if (align == "RIGHT")
                {
                    targetX = Math.Max(0, (int)Math.Round(paperW - itemWidthMm - 2.0));
                }

                SetOffsetXByIndex(colNum, targetX);
            }
        }

        public void ExecuteAlign(string align)
        {
            string alignKor = align == "LEFT" ? "좌측" : (align == "CENTER" ? "가운데" : "우측");
            int paperW = Math.Max(15, PaperWidth);

            if (SelectedItemIndex == 100)
            {
                // 표 상단 제목 정렬: 누적 오프셋을 0으로 리셋하여 정확한 기준 위치(좌/중/우)로 정렬
                TableTitleAlign = align;
                TableTitleOffsetX = 0;
                StatusMessage = $"표 상단 제목이 {alignKor} 정렬로 변경되었습니다.";
            }
            else if (SelectedItemIndex == 99)
            {
                // 날짜/시간 정렬
                string dtStr = GetFormattedDateTimeString();
                double dtW = CalculateExactTextWidthMm(dtStr, DateTimeFont, DateTimeSize, DateTimeBold, DateTimeItalic);
                int targetX = 2;
                if (align == "CENTER") targetX = Math.Max(1, (int)Math.Round((paperW - dtW) / 2.0));
                else if (align == "RIGHT") targetX = Math.Max(1, (int)Math.Round(paperW - dtW - 2.0));
                DateTimeOffsetX = targetX;
                StatusMessage = $"날짜/시간 항목이 {alignKor} 정렬되었습니다.";
            }
            else if (SelectedItemIndex == 101)
            {
                // 순번(일련번호) 정렬
                string serialStr = FormattedSerialPreview;
                double serW = CalculateExactTextWidthMm(serialStr, SerialFont, SerialSize, SerialBold, SerialItalic);
                int targetX = 2;
                if (align == "CENTER") targetX = Math.Max(1, (int)Math.Round((paperW - serW) / 2.0));
                else if (align == "RIGHT") targetX = Math.Max(1, (int)Math.Round(paperW - serW - 2.0));
                SerialOffsetX = targetX;
                StatusMessage = $"순번 항목이 {alignKor} 정렬되었습니다.";
            }
            else if (SelectedItemIndex < 0 && SelectedItemIndex >= -8)
            {
                // 특정 컬럼의 좌측 제목(헤더) 정렬 (표 모드)
                int colNum = -SelectedItemIndex;
                SetHeaderAlignByIndex(colNum, align);
                StatusMessage = $"{colNum}번 컬럼의 [제목 셀]이 {alignKor} 정렬로 변경되었습니다.";
            }
            else if (SelectedItemIndex >= 0 && SelectedItemIndex < 8)
            {
                // 특정 단일 컬럼 정렬
                int colNum = SelectedItemIndex + 1;
                AlignSingleColumn(colNum, align);
                StatusMessage = $"{colNum}번 컬럼이 {alignKor} 정렬되었습니다.";
            }
            else
            {
                // 전체 일괄 정렬
                if (IsTableBorderEnabled)
                {
                    TableTitleAlign = align;
                    TableTitleOffsetX = 0;
                    for (int i = 1; i <= ActiveColumnCount; i++)
                    {
                        SetAlignByIndex(i, align);
                    }
                    StatusMessage = $"표의 모든 항목이 {alignKor} 정렬로 변경되었습니다.";
                }
                else
                {
                    // 각 항목별 글자 너비를 개별 계산하여 정확한 좌측/중앙/우측 정렬 적용
                    for (int i = 1; i <= ActiveColumnCount; i++)
                    {
                        AlignSingleColumn(i, align);
                    }

                    // 날짜시간도 함께 정렬
                    if (IsDateTimeEnabled)
                    {
                        string dtStr = GetFormattedDateTimeString();
                        double dtW = CalculateExactTextWidthMm(dtStr, DateTimeFont, DateTimeSize, DateTimeBold, DateTimeItalic);
                        int dtTargetX = 2;
                        if (align == "CENTER") dtTargetX = Math.Max(1, (int)Math.Round((paperW - dtW) / 2.0));
                        else if (align == "RIGHT") dtTargetX = Math.Max(1, (int)Math.Round(paperW - dtW - 2.0));
                        DateTimeOffsetX = dtTargetX;
                    }

                    StatusMessage = $"모든 항목이 항목별 개별 텍스트 기준 {alignKor} 정렬되었습니다.";
                }
            }

            UpdatePreview();
        }

        private void ExecuteAlignLeft() => ExecuteAlign("LEFT");
        private void ExecuteAlignCenter() => ExecuteAlign("CENTER");
        private void ExecuteAlignRight() => ExecuteAlign("RIGHT");

        public double CalculateItemHeightMm(int colNum)
        {
            if (GetBarcodeByIndex(colNum))
            {
                string bType = GetBarcodeTypeByIndex(colNum)?.ToUpper();
                int size = GetSizeByIndex(colNum);
                if (bType == "QR_CODE")
                {
                    return Math.Max(8.0, Math.Max(40, size * 3.5) / (96.0 / 25.4));
                }
                if (bType == "DATAMATRIX" || bType == "DATA_MATRIX")
                {
                    return Math.Max(8.0, Math.Max(36, size * 3.0) / (96.0 / 25.4));
                }
                // 1D 바코드 기본 높이
                return 11.0;
            }
            else
            {
                string displayText = GetPreviewDisplayTextForColumn(colNum);
                int size = GetSizeByIndex(colNum);

                // 1줄 순수 렌더링 높이 (mm 단위: fontSizePt * 0.3528 * 1.25)
                double singleLineHeightMm = Math.Max(3.2, size * 0.352778 * 1.25);

                // 텍스트에 명시적인 줄바꿈(\n)이 포함된 경우 줄 수 반영
                int lineCount = 1;
                if (!string.IsNullOrEmpty(displayText) && displayText.Contains("\n"))
                {
                    lineCount = Math.Max(1, displayText.Split('\n').Length);
                }

                return lineCount * singleLineHeightMm;
            }
        }

        private void ExecuteDistributeVertical()
        {
            int count = Math.Max(1, Math.Min(8, ActiveColumnCount));
            int paperH = Math.Max(15, EffectivePaperHeight > 0 ? EffectivePaperHeight : PaperHeight);

            if (count <= 2)
            {
                StatusMessage = "세로 균등 정렬은 3개 이상의 활성 컬럼이 있을 때 적용됩니다.";
                return;
            }

            // 1. 첫 번째 컬럼과 마지막 컬럼의 현재 Y 위치를 기준점으로 설정 (사용자가 배치한 위치 100% 보존)
            int firstY = GetOffsetYByIndex(1);
            int lastY = GetOffsetYByIndex(count);

            // 만약 첫 번째 컬럼과 마지막 컬럼의 순서가 역전되었거나 너무 붙어있는 경우 기본 보정
            if (lastY <= firstY)
            {
                lastY = Math.Min(paperH - 6, firstY + ((count - 1) * 6));
                SetOffsetYByIndex(count, lastY);
            }

            // 2. 첫 번째 컬럼(firstY)부터 마지막 컬럼(lastY)까지의 시작 Y 기준 완벽한 등간격(Equal Step) 산출
            double totalSpan = lastY - firstY;
            double step = totalSpan / (count - 1);

            // 3. 중간 컬럼들(2 ~ count-1)을 첫 번째와 마지막 컬럼 사이의 등간격 위치로 재배치
            for (int i = 1; i < count - 1; i++)
            {
                int colNum = i + 1;
                double targetY = firstY + (i * step);
                SetOffsetYByIndex(colNum, Math.Max(0, (int)Math.Round(targetY)));
            }

            StatusMessage = $"1번 컬럼(Y:{firstY}mm)과 {count}번 컬럼(Y:{lastY}mm) 사이 {count - 2}개 항목이 등간격({step:F1}mm)으로 정렬되었습니다.";
            UpdatePreview();
        }

        private void ExecuteDistributeHorizontal()
        {
            int count = Math.Max(1, Math.Min(8, ActiveColumnCount));
            int paperW = Math.Max(15, EffectivePaperWidth > 0 ? EffectivePaperWidth : PaperWidth);

            if (IsTableBorderEnabled && TableOrientation == 1)
            {
                // 가로형 표 모드일 때: 가로 균등 분할
                StatusMessage = "가로형 표의 각 열 너비가 균등 분할되었습니다.";
                UpdatePreview();
                return;
            }

            if (count <= 2)
            {
                StatusMessage = "가로 균등 정렬은 3개 이상의 활성 컬럼이 있을 때 적용됩니다.";
                return;
            }

            // 1. 첫 번째 컬럼과 마지막 컬럼의 현재 X 위치를 기준점으로 설정
            int firstX = GetOffsetXByIndex(1);
            int lastX = GetOffsetXByIndex(count);

            if (lastX <= firstX)
            {
                lastX = Math.Min(paperW - 10, firstX + ((count - 1) * 15));
                SetOffsetXByIndex(count, lastX);
            }

            // 2. 등간격 계산
            double totalSpan = lastX - firstX;
            double step = totalSpan / (count - 1);

            // 3. 중간 컬럼들 재배치
            for (int i = 1; i < count - 1; i++)
            {
                int colNum = i + 1;
                double targetX = firstX + (i * step);
                SetOffsetXByIndex(colNum, Math.Max(0, (int)Math.Round(targetX)));
            }

            StatusMessage = $"1번 컬럼(X:{firstX}mm)과 {count}번 컬럼(X:{lastX}mm) 사이 {count - 2}개 항목이 가로 등간격({step:F1}mm)으로 정렬되었습니다.";
            UpdatePreview();
        }

        public void ExecuteAutoFitTableLayout()
        {
            if (IsTableBorderEnabled)
            {
                // 이미 표 모드가 켜져 있는 경우 -> 표 맞춤 해제 및 기본 배치로 복귀
                IsTableBorderEnabled = false;
                StatusMessage = "표 맞춤(격자)이 해제되었습니다.";
                UpdatePreview();
                return;
            }

            ApplyTableAutoFitLayoutInternal();
        }

        public void ApplyTableAutoFitLayoutInternal()
        {
            IsTableBorderEnabled = true;

            int count = Math.Max(1, ActiveColumnCount);
            int totalW = Math.Max(20, EffectivePaperWidth > 0 ? EffectivePaperWidth : PaperWidth);
            int totalH = Math.Max(15, EffectivePaperHeight > 0 ? EffectivePaperHeight : PaperHeight);

            int startX = TableOffsetX > 0 ? TableOffsetX : 2;
            int startY = TableOffsetY > 0 ? TableOffsetY : 2;

            if (IsTableTitleEnabled)
            {
                startY += Math.Max(4, TableTitleHeight) + 1 + TableTitleOffsetY;
            }

            // 바코드 컬럼 여부 확인 및 텍스트 컬럼 리스트 추출
            var textColIndices = new List<int>();
            var barcodeColIndices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                int colNum = i + 1;
                if (GetBarcodeByIndex(colNum)) barcodeColIndices.Add(colNum);
                else textColIndices.Add(colNum);
            }

            int textRowCount = Math.Max(1, textColIndices.Count);
            int effectiveW = TableWidth > 0 ? TableWidth : (totalW - (startX * 2));
            int effectiveH = TableHeight > 0 ? TableHeight : (totalH - startY - 2);

            // 바코드가 있을 경우 표 아래 바코드 공간을 적절히 고려 (또는 용지 우측 상단 배치)
            if (barcodeColIndices.Count > 0 && effectiveH > (textRowCount * 8))
            {
                effectiveH = Math.Max(textRowCount * 4, effectiveH - 12);
            }

            if (IsDateTimeEnabled)
            {
                effectiveH = Math.Max(textRowCount * 4, effectiveH - 5);
            }

            int rowH = Math.Max(4, effectiveH / textRowCount);
            int tableBottomY = startY + (rowH * textRowCount);

            // 1. 텍스트 컬럼들만 순서대로 표 내부 각 행의 수직 중앙에 배치
            for (int r = 0; r < textColIndices.Count; r++)
            {
                int colNum = textColIndices[r];
                int size = GetSizeByIndex(colNum);
                int rowTop = startY + (r * rowH);

                double textHeightMm = size * 0.3527;
                int centerY = (int)Math.Round(rowTop + Math.Max(0.5, (rowH - textHeightMm) / 2.0));
                SetColOffsetByIndex(colNum, startX + 2, centerY);
            }

            // 2. 바코드 컬럼들은 표 외부(표 하단 중앙 또는 우측)에 독립 배치
            for (int b = 0; b < barcodeColIndices.Count; b++)
            {
                int colNum = barcodeColIndices[b];
                int bcX = Math.Max(startX + 2, (totalW / 2) - 8);
                int bcY = Math.Min(totalH - 10, tableBottomY + 1 + (b * 12));
                SetColOffsetByIndex(colNum, bcX, bcY);
            }

            if (IsDateTimeEnabled)
            {
                DateTimeOffsetX = startX + 2;
                DateTimeOffsetY = totalH - 4;
            }

            StatusMessage = $"바코드 행을 자동 제거하고 {textRowCount}개 텍스트 항목을 표 수직 중앙으로 정렬했습니다.";
            UpdatePreview();
        }

        private void SetColOffsetByIndex(int colNum, int x, int y)
        {
            switch (colNum)
            {
                case 1: Col1OffsetX = x; Col1OffsetY = y; break;
                case 2: Col2OffsetX = x; Col2OffsetY = y; break;
                case 3: Col3OffsetX = x; Col3OffsetY = y; break;
                case 4: Col4OffsetX = x; Col4OffsetY = y; break;
                case 5: Col5OffsetX = x; Col5OffsetY = y; break;
                case 6: Col6OffsetX = x; Col6OffsetY = y; break;
                case 7: Col7OffsetX = x; Col7OffsetY = y; break;
                case 8: Col8OffsetX = x; Col8OffsetY = y; break;
            }
        }

        #endregion
    }
}
