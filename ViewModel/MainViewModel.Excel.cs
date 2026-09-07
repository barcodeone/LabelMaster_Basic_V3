using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Argon;
using BarcodePrint.Model;

namespace BarcodePrint.ViewModel
{
    public partial class MainViewModel
    {
        public void ExecuteExportToExcel()
        {
            if (Records == null || Records.Count == 0)
            {
                _dialogService.ShowMessage("엑셀로 저장할 인쇄 기록이 없습니다.", "알림", MessageBoxImage.Information);
                return;
            }

            string defaultName = $"온팔로우_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            if (_dialogService.ShowSaveFileDialog("Excel 워크시트 (*.xlsx)|*.xlsx", ".xlsx", defaultName, out string filePath))
            {
                bool success = _excelService.ExportToExcel(Records, _settings, filePath, out string err);
                if (success)
                {
                    StatusMessage = $"엑셀 파일 저장 완료: {filePath}";
                    _dialogService.ShowMessage("엑셀 파일이 성공적으로 저장되었습니다.", "완료", MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = $"엑셀 파일 저장 실패: {err}";
                    _dialogService.ShowError($"엑셀 저장 실패: {err}", "오류");
                }
            }
        }

        public void ExecuteImportFromExcel(string specifiedFilePath = null)
        {
            string filePath = specifiedFilePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                if (!_dialogService.ShowOpenFileDialog("Excel 워크시트 (*.xlsx;*.xls)|*.xlsx;*.xls", ".xlsx", out filePath))
                {
                    return;
                }
            }

            if (!File.Exists(filePath))
            {
                _dialogService.ShowError($"지정된 엑셀 파일을 찾을 수 없습니다:\n{filePath}", "파일 없음");
                return;
            }

            // 엑셀 모드(탭 2)로 자동 전환
            SelectedWorkMode = 1;

            // 시트 및 컬럼 선택 팝업 다이얼로그 표시
            var dlg = new BarcodePrint.WpfView.ExcelImportDialog(filePath, _excelService, _settings);
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.MainWindow != null)
            {
                dlg.Owner = System.Windows.Application.Current.MainWindow;
            }

            if (dlg.ShowDialog() != true)
            {
                return;
            }

            string sheetName = dlg.SelectedSheetName;
            var selectedColIndices = dlg.SelectedColumnIndices;
            var customHeaders = dlg.SelectedCustomHeaders;

            bool success = _excelService.ImportFromExcelWithMapping(filePath, sheetName, selectedColIndices, out List<string> headers, out List<PrintRecordModel> importedRecords, out string err);
            if (success)
            {
                // 1. 헤더 자동 매핑 (사용자가 콤보박스로 매핑한 컬럼명 및 활성 컬럼 수 설정)
                var finalHeaders = (customHeaders != null && customHeaders.Count > 0) ? customHeaders : headers;
                if (finalHeaders != null && finalHeaders.Count > 0)
                {
                    int lastMappedIndex = 0;
                    if (selectedColIndices != null)
                    {
                        for (int i = 0; i < selectedColIndices.Count && i < 8; i++)
                        {
                            if (selectedColIndices[i] > 0)
                            {
                                lastMappedIndex = i + 1;
                            }
                        }
                    }
                    int activeCount = Math.Max(1, Math.Min(8, lastMappedIndex > 0 ? lastMappedIndex : finalHeaders.Count));

                    _isBatchUpdating = true;
                    try
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            if (i < finalHeaders.Count && !string.IsNullOrWhiteSpace(finalHeaders[i]))
                            {
                                SetColumnHeaderByIndex(i + 1, finalHeaders[i]);
                            }
                        }
                        ActiveColumnCount = activeCount;
                    }
                    finally
                    {
                        _isBatchUpdating = false;
                    }
                    _settings.Save();
                    
                    OnPropertyChanged(nameof(Column1Header));
                    OnPropertyChanged(nameof(Column2Header));
                    OnPropertyChanged(nameof(Column3Header));
                    OnPropertyChanged(nameof(Column4Header));
                    OnPropertyChanged(nameof(Column5Header));
                    OnPropertyChanged(nameof(Column6Header));
                    OnPropertyChanged(nameof(Column7Header));
                    OnPropertyChanged(nameof(Column8Header));
                    ActiveColumnCountChanged?.Invoke(ActiveColumnCount);
                    UpdatePreview();
                }

                // 2. 데이터그리드 레코드 로드
                Records.Clear();
                int idx = 0;
                foreach (var rec in importedRecords)
                {
                    idx++;
                    rec.Index = idx;
                    Records.Add(rec);
                }
                _recordCounter = idx;

                SaveHistoryRecords();

                // 첫 번째 행 데이터를 입력창으로 로드
                if (Records.Count > 0)
                {
                    var first = Records[0];
                    InputCol1 = first.Col1 ?? "";
                    InputCol2 = first.Col2 ?? "";
                    InputCol3 = first.Col3 ?? "";
                    InputCol4 = first.Col4 ?? "";
                    InputCol5 = first.Col5 ?? "";
                    InputCol6 = first.Col6 ?? "";
                    InputCol7 = first.Col7 ?? "";
                    InputCol8 = first.Col8 ?? "";
                    SelectedRecord = first;
                }

                LoadedExcelFileName = Path.GetFileName(filePath);
                StatusMessage = $"엑셀 로드 완료: {LoadedExcelFileName} ({Records.Count}건)";
                _dialogService.ShowMessage($"엑셀 파일에서 총 {Records.Count}건의 데이터를 성공적으로 불러왔습니다.", "엑셀 가져오기 완료", MessageBoxImage.Information);
                UpdatePreview();
            }
            else
            {
                StatusMessage = $"엑셀 로드 실패: {err}";
                _dialogService.ShowError($"엑셀 파일 열기 실패: {err}", "오류");
            }
        }

        public void ExecuteImportBackup()
        {
            try
            {
                string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup");
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);

                if (_dialogService.ShowOpenFileDialog("온팔로우 백업 파일 (*.bak)|*.bak|텍스트 백업 파일 (*.txt)|*.txt|모든 파일 (*.*)|*.*", ".bak", out string filePath))
                {
                    var recoveredList = _excelService.ImportBackup(filePath, out string err);
                    if (recoveredList == null || recoveredList.Count == 0)
                    {
                        _dialogService.ShowWarning(err ?? "선택한 백업 파일에서 유효한 데이터를 찾을 수 없습니다.", "복구 실패");
                        return;
                    }

                    Records.Clear();
                    int idx = 0;
                    foreach (var item in recoveredList)
                    {
                        idx++;
                        item.Index = idx;
                        Records.Add(item);
                    }
                    _recordCounter = idx;

                    SaveHistoryRecords();

                    StatusMessage = $"백업 복구 완료: {Path.GetFileName(filePath)} ({Records.Count}건)";
                    _dialogService.ShowMessage($"백업 파일({Path.GetFileName(filePath)})에서 총 {Records.Count}건의 데이터가 성공적으로 복구되었습니다.", 
                                               "백업 복구 완료", MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"백업 복구 실패: {ex.Message}";
                _dialogService.ShowError($"백업 복구 중 오류가 발생했습니다:\n{ex.Message}", "오류");
            }
        }

        public void SaveHistoryRecords()
        {
            try
            {
                var recordList = Records.ToList();

                // 1. Info 폴더 생성 및 JSON 파일 저장
                string infoDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info");
                if (!Directory.Exists(infoDir))
                {
                    Directory.CreateDirectory(infoDir);
                }

                string jsonPath = Path.Combine(infoDir, "print_history.json");
                string json = JsonConvert.SerializeObject(recordList, Formatting.Indented);
                File.WriteAllText(jsonPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveHistoryRecords Error: {ex.Message}");
            }
        }

        public void LoadHistoryRecords()
        {
            try
            {
                string infoDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info");
                string jsonPath = Path.Combine(infoDir, "print_history.json");
                string rootJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "print_history.json");
                
                List<PrintRecordModel> loadedList = null;

                if (File.Exists(jsonPath))
                {
                    string json = File.ReadAllText(jsonPath, Encoding.UTF8);
                    loadedList = JsonConvert.DeserializeObject<List<PrintRecordModel>>(json);
                }
                else if (File.Exists(rootJsonPath))
                {
                    string json = File.ReadAllText(rootJsonPath, Encoding.UTF8);
                    loadedList = JsonConvert.DeserializeObject<List<PrintRecordModel>>(json);
                }

                if (loadedList != null)
                {
                    Records.Clear();
                    int idx = 0;
                    foreach (var item in loadedList)
                    {
                        bool isItemEmpty = string.IsNullOrWhiteSpace(item.Col1) &&
                                           string.IsNullOrWhiteSpace(item.Col2) &&
                                           string.IsNullOrWhiteSpace(item.Col3) &&
                                           string.IsNullOrWhiteSpace(item.Col4) &&
                                           string.IsNullOrWhiteSpace(item.Col5) &&
                                           string.IsNullOrWhiteSpace(item.Col6) &&
                                           string.IsNullOrWhiteSpace(item.Col7) &&
                                           string.IsNullOrWhiteSpace(item.Col8);
                        if (!isItemEmpty)
                        {
                            idx++;
                            item.Index = idx;
                            Records.Add(item);
                        }
                    }
                    _recordCounter = idx;

                    if (Records.Count > 0)
                    {
                        var last = Records.Last();
                        var lastSummary = new List<string>();
                        if (!string.IsNullOrWhiteSpace(last.Col1)) lastSummary.Add(last.Col1);
                        if (!string.IsNullOrWhiteSpace(last.Col2)) lastSummary.Add(last.Col2);
                        if (!string.IsNullOrWhiteSpace(last.Col3)) lastSummary.Add(last.Col3);
                        if (!string.IsNullOrWhiteSpace(last.Col4)) lastSummary.Add(last.Col4);
                        if (!string.IsNullOrWhiteSpace(last.Col5)) lastSummary.Add(last.Col5);
                        if (!string.IsNullOrWhiteSpace(last.Col6)) lastSummary.Add(last.Col6);
                        if (!string.IsNullOrWhiteSpace(last.Col7)) lastSummary.Add(last.Col7);
                        if (!string.IsNullOrWhiteSpace(last.Col8)) lastSummary.Add(last.Col8);
                        StatusMessage = $"마지막 인쇄 기록 [No.{last.Index}]: {string.Join(" | ", lastSummary)} ({last.Timestamp})";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadHistoryRecords Error: {ex.Message}");
            }
        }

        public void ExecuteDuplicateRecords(object parameter = null)
        {
            var targets = new List<PrintRecordModel>();

            var checkedRecords = Records.Where(r => r.IsChecked).ToList();
            if (checkedRecords.Count > 0)
            {
                targets.AddRange(checkedRecords);
            }
            else if (parameter is System.Collections.IList list && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (item is PrintRecordModel rec)
                    {
                        targets.Add(rec);
                    }
                }
            }
            else if (parameter is PrintRecordModel singleTarget)
            {
                targets.Add(singleTarget);
            }
            else if (SelectedRecord != null)
            {
                targets.Add(SelectedRecord);
            }

            if (targets.Count == 0)
            {
                _dialogService.ShowMessage("복사하여 추가할 기록 행을 체크박스로 선택하거나 클릭해 주세요.", "알림", MessageBoxImage.Information);
                return;
            }

            int repeatCount = Math.Max(1, Math.Min(999, CopyCount));
            int totalAdded = 0;
            PrintRecordModel lastAddedItem = null;

            var orderedTargets = targets.OrderByDescending(t => Records.IndexOf(t)).ToList();

            foreach (var src in orderedTargets)
            {
                int srcIdx = Records.IndexOf(src);
                if (srcIdx < 0) srcIdx = Records.Count - 1;

                int insertPos = srcIdx + 1;

                for (int i = 0; i < repeatCount; i++)
                {
                    var newRec = new PrintRecordModel
                    {
                        Index = 0,
                        Timestamp = IsDateTimeEnabled ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : "",
                        Col1 = src.Col1,
                        Col2 = src.Col2,
                        Col3 = src.Col3,
                        Col4 = src.Col4,
                        Col5 = src.Col5,
                        Col6 = src.Col6,
                        Col7 = src.Col7,
                        Col8 = src.Col8,
                        Status = "",
                        IsChecked = false
                    };

                    if (insertPos <= Records.Count)
                    {
                        Records.Insert(insertPos + i, newRec);
                    }
                    else
                    {
                        Records.Add(newRec);
                    }

                    lastAddedItem = newRec;
                    totalAdded++;
                }
            }

            int seq = 0;
            foreach (var r in Records)
            {
                seq++;
                r.Index = seq;
            }
            _recordCounter = seq;

            SaveHistoryRecords();

            if (lastAddedItem != null)
            {
                SelectedRecord = lastAddedItem;
            }

            StatusMessage = $"📋 선택한 행 데이터가 원본 행 바로 아래에 총 {totalAdded}건 복사 추가되었습니다. (수량: {repeatCount}개씩)";
        }

        public void ExecuteDuplicateSingleRecord(object parameter)
        {
            PrintRecordModel target = parameter as PrintRecordModel ?? SelectedRecord;
            if (target == null) return;

            int srcIdx = Records.IndexOf(target);
            if (srcIdx < 0) srcIdx = Records.Count - 1;

            int insertPos = srcIdx + 1;

            var newRec = new PrintRecordModel
            {
                Index = 0,
                Timestamp = IsDateTimeEnabled ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : "",
                Col1 = target.Col1,
                Col2 = target.Col2,
                Col3 = target.Col3,
                Col4 = target.Col4,
                Col5 = target.Col5,
                Col6 = target.Col6,
                Col7 = target.Col7,
                Col8 = target.Col8,
                Status = "",
                IsChecked = false
            };

            if (insertPos <= Records.Count)
            {
                Records.Insert(insertPos, newRec);
            }
            else
            {
                Records.Add(newRec);
            }

            int seq = 0;
            foreach (var r in Records)
            {
                seq++;
                r.Index = seq;
            }
            _recordCounter = seq;

            SaveHistoryRecords();
            SelectedRecord = newRec;
            StatusMessage = $"📋 [No.{target.Index}] 행 데이터가 바로 아래에 1개 복사 추가되었습니다.";
        }

        public void ExecuteDeleteSelectedRecord(object parameter = null)
        {
            var targets = new List<PrintRecordModel>();

            if (parameter is System.Collections.IList list && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (item is PrintRecordModel rec)
                    {
                        targets.Add(rec);
                    }
                }
            }
            else if (parameter is PrintRecordModel singleTarget)
            {
                targets.Add(singleTarget);
            }
            else if (SelectedRecord != null)
            {
                targets.Add(SelectedRecord);
            }

            if (targets.Count == 0)
            {
                var checkedItems = Records.Where(r => r.IsChecked).ToList();
                if (checkedItems.Count > 0)
                {
                    targets.AddRange(checkedItems);
                }
            }
            else
            {
                var checkedItems = Records.Where(r => r.IsChecked).ToList();
                foreach (var c in checkedItems)
                {
                    if (!targets.Contains(c)) targets.Add(c);
                }
            }

            if (targets.Count == 0)
            {
                _dialogService.ShowMessage("삭제할 기록 행을 먼저 선택하거나 체크해 주세요.", "알림", MessageBoxImage.Information);
                return;
            }

            string confirmMsg = targets.Count == 1
                ? $"[No.{targets[0].Index}] 기록 데이터를 삭제하시겠습니까?"
                : $"선택한 {targets.Count}개의 기록 데이터를 모두 삭제하시겠습니까?";

            if (!_dialogService.ShowQuestion(confirmMsg, "기록 삭제 확인"))
            {
                return;
            }

            foreach (var item in targets)
            {
                Records.Remove(item);
            }

            int idx = 0;
            foreach (var r in Records)
            {
                idx++;
                r.Index = idx;
            }
            _recordCounter = idx;

            SaveHistoryRecords();
            StatusMessage = targets.Count == 1 
                ? $"선택한 기록 [No.{targets[0].Index}]이(가) 삭제되었습니다. (남은 기록: {Records.Count}건)"
                : $"선택한 {targets.Count}건의 기록이 삭제되었습니다. (남은 기록: {Records.Count}건)";
        }

        public void ExecuteClearRecords()
        {
            if (Records == null || Records.Count == 0)
            {
                _dialogService.ShowMessage("초기화할 기록이 없습니다.", "알림", MessageBoxImage.Information);
                return;
            }

            if (_dialogService.ShowQuestion("DataGrid의 모든 인쇄 및 입력 기록을 삭제하시겠습니까?\n(저장된 이력 파일도 함께 초기화됩니다)", "기록 전체 초기화"))
            {
                Records.Clear();
                _recordCounter = 0;
                LoadedExcelFileName = "";
                CurrentPreviewIndex = 0;
                SelectedRecord = null;
                SaveHistoryRecords();
                StatusMessage = "모든 기록이 초기화되었습니다.";
                UpdatePreview();
            }
        }
    }
}
