using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using BarcodePrint.Model;

namespace BarcodePrint.Services
{
    public class ExcelService : IExcelService
    {
        public bool ExportToExcel(IEnumerable<PrintRecordModel> records, AppSettings settings, string filePath, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("인쇄기록");

                    // 1. 헤더 작성
                    int activeCols = Math.Max(1, settings.ActiveColumnCount);
                    worksheet.Cell(1, 1).Value = "순번";
                    worksheet.Cell(1, 2).Value = "인쇄일시";

                    for (int i = 0; i < activeCols; i++)
                    {
                        string header = "";
                        switch (i + 1)
                        {
                            case 1: header = settings.Column1Name; break;
                            case 2: header = settings.Column2Name; break;
                            case 3: header = settings.Column3Name; break;
                            case 4: header = settings.Column4Name; break;
                            case 5: header = settings.Column5Name; break;
                            case 6: header = settings.Column6Name; break;
                            case 7: header = settings.Column7Name; break;
                            case 8: header = settings.Column8Name; break;
                            default:
                                if (settings.ExtendedColumnHeaders != null && settings.ExtendedColumnHeaders.TryGetValue((i + 1).ToString(), out string extH))
                                {
                                    header = extH;
                                }
                                break;
                        }
                        worksheet.Cell(1, 3 + i).Value = string.IsNullOrWhiteSpace(header) ? $"항목{i + 1}" : header;
                    }
                    worksheet.Cell(1, 3 + activeCols).Value = "상태";

                    // 2. 헤더 스타일링
                    var headerRange = worksheet.Range(1, 1, 1, 3 + activeCols);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976D2");
                    headerRange.Style.Font.FontColor = XLColor.White;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // 3. 데이터 기록
                    int row = 2;
                    foreach (var rec in records)
                    {
                        worksheet.Cell(row, 1).Value = rec.Index;
                        worksheet.Cell(row, 2).Value = rec.Timestamp;

                        for (int c = 0; c < activeCols; c++)
                        {
                            worksheet.Cell(row, 3 + c).Value = rec.GetValueByColumn((c + 1).ToString()) ?? "";
                        }
                        worksheet.Cell(row, 3 + activeCols).Value = rec.Status ?? "";
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
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

        public List<string> GetSheetNames(string filePath)
        {
            var sheetNames = new List<string>();
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    foreach (var ws in workbook.Worksheets)
                    {
                        sheetNames.Add(ws.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSheetNames Error: {ex.Message}");
            }
            return sheetNames;
        }

        public List<string> GetSheetHeaders(string filePath, string sheetName)
        {
            var headers = new List<string>();
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var ws = string.IsNullOrEmpty(sheetName) ? workbook.Worksheet(1) : workbook.Worksheet(sheetName);
                    if (ws != null)
                    {
                        var firstRow = ws.FirstRowUsed();
                        if (firstRow != null)
                        {
                            int lastColNumber = firstRow.LastCellUsed().Address.ColumnNumber;
                            for (int c = 1; c <= lastColNumber; c++)
                            {
                                string text = firstRow.Cell(c).GetString().Trim();
                                headers.Add(string.IsNullOrWhiteSpace(text) ? $"열 {c}" : text);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSheetHeaders Error: {ex.Message}");
            }
            return headers;
        }

        public bool ImportFromExcel(string filePath, out List<string> headers, out List<PrintRecordModel> records, out string errorMessage)
        {
            var sheetHeaders = GetSheetHeaders(filePath, "");
            var defaultIndices = new List<int>();
            for (int i = 1; i <= sheetHeaders.Count; i++) defaultIndices.Add(i);
            return ImportFromExcelWithMapping(filePath, "", defaultIndices, out headers, out records, out errorMessage);
        }

        public bool ImportFromExcelWithMapping(string filePath, string sheetName, List<int> selectedColIndices, out List<string> selectedHeaders, out List<PrintRecordModel> records, out string errorMessage)
        {
            selectedHeaders = new List<string>();
            records = new List<PrintRecordModel>();
            errorMessage = "";

            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = string.IsNullOrEmpty(sheetName) ? workbook.Worksheet(1) : workbook.Worksheet(sheetName);
                    if (worksheet == null)
                    {
                        errorMessage = "유효한 워크시트를 찾을 수 없습니다.";
                        return false;
                    }

                    var firstRow = worksheet.FirstRowUsed();
                    if (firstRow == null)
                    {
                        errorMessage = "엑셀 시트에 데이터가 없습니다.";
                        return false;
                    }

                    int lastColNumber = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
                    var allSheetHeaders = new List<string>();
                    for (int c = 1; c <= lastColNumber; c++)
                    {
                        string headerText = firstRow.Cell(c).GetString().Trim();
                        allSheetHeaders.Add(string.IsNullOrWhiteSpace(headerText) ? $"열 {c}" : headerText);
                    }

                    // 1. 선택된 헤더 추출 (1-based index, 0은 미사용)
                    if (selectedColIndices != null && selectedColIndices.Count > 0)
                    {
                        foreach (int colIdx in selectedColIndices)
                        {
                            if (colIdx > 0 && colIdx <= allSheetHeaders.Count)
                            {
                                selectedHeaders.Add(allSheetHeaders[colIdx - 1]);
                            }
                            else
                            {
                                selectedHeaders.Add("");
                            }
                        }
                    }
                    else
                    {
                        // 선택된 인덱스가 없을 경우 시트 전체 헤더 사용
                        selectedHeaders.AddRange(allSheetHeaders);
                    }

                    // 2. 데이터 레코드 추출
                    var dataRows = worksheet.RowsUsed().Skip(1);
                    int index = 0;
                    foreach (var r in dataRows)
                    {
                        index++;
                        var model = new PrintRecordModel
                        {
                            Index = index,
                            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            Status = "가져옴",
                            IsChecked = false
                        };

                        // 모든 열의 셀 값을 CellValues에 키(컬럼번호, 열문자, 헤더명)별로 보관
                        for (int c = 1; c <= lastColNumber; c++)
                        {
                            string cellVal = r.Cell(c).GetString();
                            string colLetter = GetColumnLetter(c);
                            string header = (c - 1 < allSheetHeaders.Count) ? allSheetHeaders[c - 1] : $"열 {c}";

                            model.CellValues[c.ToString()] = cellVal;
                            model.CellValues[colLetter] = cellVal;
                            if (!string.IsNullOrWhiteSpace(header))
                            {
                                model.CellValues[header] = cellVal;
                            }
                        }

                        // 컬럼 매핑 (선택 인덱스 기준 또는 시트 전체 열 매핑 - 제한 해제)
                        if (selectedColIndices != null && selectedColIndices.Count > 0)
                        {
                            for (int i = 0; i < selectedColIndices.Count; i++)
                            {
                                int excelCol = selectedColIndices[i];
                                string val = excelCol > 0 ? r.Cell(excelCol).GetString() : "";
                                SetModelColumn(model, i + 1, val);
                            }
                        }
                        else
                        {
                            for (int c = 1; c <= lastColNumber; c++)
                            {
                                SetModelColumn(model, c, r.Cell(c).GetString());
                            }
                        }

                        records.Add(model);
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

        private static void SetModelColumn(PrintRecordModel model, int colNum, string val)
        {
            switch (colNum)
            {
                case 1: model.Col1 = val; break;
                case 2: model.Col2 = val; break;
                case 3: model.Col3 = val; break;
                case 4: model.Col4 = val; break;
                case 5: model.Col5 = val; break;
                case 6: model.Col6 = val; break;
                case 7: model.Col7 = val; break;
                case 8: model.Col8 = val; break;
                default:
                    model.SetValueByColumn(colNum.ToString(), val);
                    break;
            }
        }

        public static string GetColumnLetter(int colIndex)
        {
            string colLetter = "";
            while (colIndex > 0)
            {
                int rem = (colIndex - 1) % 26;
                colLetter = (char)('A' + rem) + colLetter;
                colIndex = (colIndex - 1) / 26;
            }
            return string.IsNullOrEmpty(colLetter) ? "A" : colLetter;
        }

        public bool SaveBackup(IEnumerable<PrintRecordModel> records, AppSettings settings, out string todayBakPath, out string lastBakPath, out string txtBakPath)
        {
            todayBakPath = "";
            lastBakPath = "";
            txtBakPath = "";
            return true;
        }

        public List<PrintRecordModel> ImportBackup(string filePath, out string errorMessage)
        {
            errorMessage = "";
            var results = new List<PrintRecordModel>();

            try
            {
                if (!File.Exists(filePath))
                {
                    errorMessage = "지정된 백업 파일이 존재하지 않습니다.";
                    return results;
                }

                string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
                int idx = 0;
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] parts = line.Split('\t');
                    if (parts.Length >= 3)
                    {
                        idx++;
                        var model = new PrintRecordModel
                        {
                            Index = idx,
                            Timestamp = parts.Length > 1 ? parts[1] : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            Col1 = parts.Length > 2 ? parts[2] : "",
                            Col2 = parts.Length > 3 ? parts[3] : "",
                            Col3 = parts.Length > 4 ? parts[4] : "",
                            Col4 = parts.Length > 5 ? parts[5] : "",
                            Col5 = parts.Length > 6 ? parts[6] : "",
                            Col6 = parts.Length > 7 ? parts[7] : "",
                            Col7 = parts.Length > 8 ? parts[8] : "",
                            Col8 = parts.Length > 9 ? parts[9] : "",
                            Status = parts.Length > 10 ? parts[10] : "복구됨",
                            IsChecked = false
                        };
                        results.Add(model);
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return results;
            }
        }
    }
}
