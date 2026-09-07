using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace BarcodePrint.Model
{
    /// <summary>
    /// ClosedXML을 이용해 DataGrid 레코드를 엑셀 파일로 저장 및 불러오는 헬퍼 클래스
    /// </summary>
    public static class ExcelExportHelper
    {
        public static bool ExportToExcel(IEnumerable<PrintRecordModel> records, AppSettings settings, string filePath, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("인쇄이력목록");

                    int activeColCount = Math.Max(1, Math.Min(8, settings.ActiveColumnCount));

                    // 1. 헤더 생성: 순번, 컬럼1..N, 인쇄상태, 날짜시간 (가장 마지막 열)
                    worksheet.Cell(1, 1).Value = "순번";

                    string[] headers = new string[]
                    {
                        string.IsNullOrEmpty(settings.Column1Name) ? "항목1" : settings.Column1Name,
                        string.IsNullOrEmpty(settings.Column2Name) ? "항목2" : settings.Column2Name,
                        string.IsNullOrEmpty(settings.Column3Name) ? "항목3" : settings.Column3Name,
                        string.IsNullOrEmpty(settings.Column4Name) ? "항목4" : settings.Column4Name,
                        string.IsNullOrEmpty(settings.Column5Name) ? "항목5" : settings.Column5Name,
                        string.IsNullOrEmpty(settings.Column6Name) ? "항목6" : settings.Column6Name,
                        string.IsNullOrEmpty(settings.Column7Name) ? "항목7" : settings.Column7Name,
                        string.IsNullOrEmpty(settings.Column8Name) ? "항목8" : settings.Column8Name
                    };

                    for (int c = 0; c < activeColCount; c++)
                    {
                        worksheet.Cell(1, 2 + c).Value = headers[c];
                    }

                    int statusColIdx = 2 + activeColCount;
                    int timeColIdx = statusColIdx + 1;

                    worksheet.Cell(1, statusColIdx).Value = "인쇄상태";
                    worksheet.Cell(1, timeColIdx).Value = "날짜시간";

                    // 헤더 스타일 적용
                    var headerRange = worksheet.Range(1, 1, 1, timeColIdx);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3F51B5"); // Indigo Accent
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // 2. 데이터 행 작성
                    int row = 2;
                    if (records != null)
                    {
                        foreach (var rec in records)
                        {
                            worksheet.Cell(row, 1).Value = rec.Index;

                            string[] colVals = new string[] { rec.Col1, rec.Col2, rec.Col3, rec.Col4, rec.Col5, rec.Col6, rec.Col7, rec.Col8 };
                            for (int c = 0; c < activeColCount; c++)
                            {
                                worksheet.Cell(row, 2 + c).Value = colVals[c];
                            }
                            worksheet.Cell(row, statusColIdx).Value = rec.Status;
                            worksheet.Cell(row, timeColIdx).Value = rec.Timestamp;
                            row++;
                        }
                    }

                    // 3. 테두리 및 기본 정렬 스타일 적용
                    int lastDataRow = Math.Max(2, row - 1);
                    var dataRange = worksheet.Range(1, 1, lastDataRow, timeColIdx);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // 순번, 인쇄상태, 날짜시간 열 가운데 정렬
                    worksheet.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Column(statusColIdx).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Column(timeColIdx).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // 4. 열 너비 텍스트 길이에 맞춘 정밀 자동 간격 계산 (Auto-Fit + 글자 잘림 방지 여유 패딩)
                    worksheet.Columns(1, timeColIdx).AdjustToContents();

                    for (int col = 1; col <= timeColIdx; col++)
                    {
                        var column = worksheet.Column(col);
                        double currentWidth = column.Width;

                        if (col == 1) // 순번
                        {
                            column.Width = Math.Max(currentWidth + 3, 8);
                        }
                        else if (col == statusColIdx) // 인쇄상태
                        {
                            column.Width = Math.Max(currentWidth + 4, 12);
                        }
                        else if (col == timeColIdx) // 날짜시간
                        {
                            column.Width = Math.Max(currentWidth + 4, 22);
                        }
                        else // 각 입력 컬럼 (항목 1~8)
                        {
                            column.Width = Math.Max(currentWidth + 4, 14);
                        }
                    }

                    // 행 높이 쾌적하게 지정
                    worksheet.Row(1).Height = 26; // 헤더 행
                    for (int r = 2; r <= lastDataRow; r++)
                    {
                        worksheet.Row(r).Height = 20; // 데이터 행
                    }

                    // 5. 저장
                    workbook.SaveAs(filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 엑셀 파일(.xlsx)을 열어서 컬럼명 목록 및 데이터 레코드들을 추출
        /// </summary>
        public static bool ImportFromExcel(string filePath, out List<string> headers, out List<PrintRecordModel> records, out string errorMessage)
        {
            headers = new List<string>();
            records = new List<PrintRecordModel>();
            errorMessage = string.Empty;

            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        errorMessage = "엑셀 파일에 시트가 존재하지 않습니다.";
                        return false;
                    }

                    var firstRow = worksheet.Row(1);
                    int lastColUsed = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
                    if (lastColUsed == 0)
                    {
                        errorMessage = "엑셀 파일에 데이터가 없습니다.";
                        return false;
                    }

                    // 헤더 컬럼 구조 파악
                    int startDataCol = 1;
                    string c1Text = firstRow.Cell(1).GetString().Trim();
                    bool hasIndexCol = c1Text.IndexOf("순번", StringComparison.OrdinalIgnoreCase) >= 0 || c1Text.Equals("No", StringComparison.OrdinalIgnoreCase);
                    if (hasIndexCol)
                    {
                        startDataCol = 2;
                    }

                    string c2Text = firstRow.Cell(2).GetString().Trim();
                    bool isLegacySecondColTime = c2Text.IndexOf("시각", StringComparison.OrdinalIgnoreCase) >= 0 || c2Text.IndexOf("시간", StringComparison.OrdinalIgnoreCase) >= 0 || c2Text.IndexOf("Time", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (hasIndexCol && isLegacySecondColTime)
                    {
                        startDataCol = 3; // 구버전 포맷 (순번, 입력시각, 컬럼1...)
                    }

                    // 마지막 열들 검사 (인쇄상태, 날짜시간 위치)
                    int endDataCol = lastColUsed;
                    int foundTimeCol = -1;
                    int foundStatusCol = -1;

                    for (int c = lastColUsed; c >= startDataCol; c--)
                    {
                        string headerText = firstRow.Cell(c).GetString().Trim();
                        if (foundTimeCol == -1 && (headerText.IndexOf("시간", StringComparison.OrdinalIgnoreCase) >= 0 || headerText.IndexOf("시각", StringComparison.OrdinalIgnoreCase) >= 0 || headerText.IndexOf("Date", StringComparison.OrdinalIgnoreCase) >= 0 || headerText.IndexOf("Time", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            foundTimeCol = c;
                            if (c == endDataCol) endDataCol--;
                        }
                        else if (foundStatusCol == -1 && (headerText.IndexOf("상태", StringComparison.OrdinalIgnoreCase) >= 0 || headerText.Equals("Status", StringComparison.OrdinalIgnoreCase)))
                        {
                            foundStatusCol = c;
                            if (c == endDataCol) endDataCol--;
                        }
                    }

                    for (int c = startDataCol; c <= endDataCol && headers.Count < 8; c++)
                    {
                        string headerName = firstRow.Cell(c).GetString().Trim();
                        if (string.IsNullOrEmpty(headerName)) headerName = $"항목 {headers.Count + 1}";
                        headers.Add(headerName);
                    }

                    // 2행부터 데이터 읽기
                    int lastRowUsed = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                    int indexCounter = 1;

                    for (int r = 2; r <= lastRowUsed; r++)
                    {
                        var row = worksheet.Row(r);
                        if (row.IsEmpty()) continue;

                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        if (foundTimeCol > 0)
                        {
                            string t = row.Cell(foundTimeCol).GetString().Trim();
                            if (!string.IsNullOrEmpty(t)) timestamp = t;
                        }
                        else if (isLegacySecondColTime)
                        {
                            string t = row.Cell(2).GetString().Trim();
                            if (!string.IsNullOrEmpty(t)) timestamp = t;
                        }

                        string status = "완료";
                        if (foundStatusCol > 0)
                        {
                            string s = row.Cell(foundStatusCol).GetString().Trim();
                            if (!string.IsNullOrEmpty(s)) status = s;
                        }

                        var record = new PrintRecordModel
                        {
                            Index = indexCounter++,
                            Timestamp = timestamp,
                            Status = status
                        };

                        int hCount = headers.Count;
                        if (hCount >= 1) record.Col1 = row.Cell(startDataCol + 0).GetString().Trim();
                        if (hCount >= 2) record.Col2 = row.Cell(startDataCol + 1).GetString().Trim();
                        if (hCount >= 3) record.Col3 = row.Cell(startDataCol + 2).GetString().Trim();
                        if (hCount >= 4) record.Col4 = row.Cell(startDataCol + 3).GetString().Trim();
                        if (hCount >= 5) record.Col5 = row.Cell(startDataCol + 4).GetString().Trim();
                        if (hCount >= 6) record.Col6 = row.Cell(startDataCol + 5).GetString().Trim();
                        if (hCount >= 7) record.Col7 = row.Cell(startDataCol + 6).GetString().Trim();
                        if (hCount >= 8) record.Col8 = row.Cell(startDataCol + 7).GetString().Trim();

                        records.Add(record);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
