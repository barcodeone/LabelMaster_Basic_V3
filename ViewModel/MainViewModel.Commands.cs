using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BarcodePrint.Model;

namespace BarcodePrint.ViewModel
{
    public partial class MainViewModel
    {
        #region Commands Declarations

        public ICommand PrintAndRecordCommand { get; private set; }
        public ICommand PrintOnlyCommand { get; private set; }
        public ICommand RecordOnlyCommand { get; private set; }
        public ICommand ExportToExcelCommand { get; private set; }
        public ICommand ImportFromExcelCommand { get; private set; }
        public ICommand ImportBackupCommand { get; private set; }
        public ICommand ClearRecordsCommand { get; private set; }
        public ICommand AddNewBlankRecordCommand { get; private set; }
        public ICommand DeleteSelectedRecordCommand { get; private set; }
        public ICommand ReprintSelectedRecordsCommand { get; private set; }
        public ICommand SinglePrintCommand { get; private set; }
        public ICommand PrintAllRecordsCommand { get; private set; }
        public ICommand PrintUnprintedRecordsCommand { get; private set; }
        public ICommand DuplicateSelectedRecordsCommand { get; private set; }
        public ICommand DuplicateSingleRecordCommand { get; private set; }
        public ICommand IncreaseCopyCountCommand { get; private set; }
        public ICommand DecreaseCopyCountCommand { get; private set; }
        public ICommand IncreasePrintCopiesCommand { get; private set; }
        public ICommand DecreasePrintCopiesCommand { get; private set; }
        public ICommand ToggleAllRecordsCheckedCommand { get; private set; }
        public ICommand SaveSettingsCommand { get; private set; }
        public ICommand ClearInputsCommand { get; private set; }
        public ICommand RefreshPrintersCommand { get; private set; }
        public ICommand ZoomInCommand { get; private set; }
        public ICommand ZoomOutCommand { get; private set; }
        public ICommand ResetZoomCommand { get; private set; }

        public ICommand IncreaseItemSizeCommand { get; private set; }
        public ICommand DecreaseItemSizeCommand { get; private set; }

        // 컬럼 개수 추가 / 삭제 및 지정 삭제 커맨드
        public ICommand AddColumnCommand { get; private set; }
        public ICommand RemoveColumnCommand { get; private set; }
        public ICommand RemoveColumnAtCommand { get; private set; }
        public ICommand MoveColumnUpCommand { get; private set; }
        public ICommand MoveColumnDownCommand { get; private set; }

        // 레이아웃 초기화 및 정렬 커맨드
        public ICommand ResetLayoutCommand { get; private set; }
        public ICommand AlignLeftCommand { get; private set; }
        public ICommand AlignCenterCommand { get; private set; }
        public ICommand AlignRightCommand { get; private set; }
        public ICommand DistributeVerticalCommand { get; private set; }
        public ICommand DistributeHorizontalCommand { get; private set; }
        public ICommand AutoFitTableLayoutCommand { get; private set; }

        // 표 제목 및 헤더 제어 커맨드
        public ICommand IncreaseTableTitleSizeCommand { get; private set; }
        public ICommand DecreaseTableTitleSizeCommand { get; private set; }
        public ICommand SetTableTitleAlignCommand { get; private set; }
        public ICommand IncreaseTableHeaderHeightCommand { get; private set; }
        public ICommand DecreaseTableHeaderHeightCommand { get; private set; }

        // 프린터 하드웨어 유틸리티 커맨드
        public ICommand AutoDetectCommand { get; private set; }
        public ICommand HomeCommand { get; private set; }
        public ICommand FeedCommand { get; private set; }
        public ICommand TestPrintCommand { get; private set; }
        public ICommand SelfTestCommand { get; private set; }
        public ICommand ResetPrinterCommand { get; private set; }
        public ICommand RebootPrinterCommand { get; private set; }
        public ICommand FactoryResetPrinterCommand { get; private set; }
        public ICommand SetConnectionTypeCommand { get; private set; }
        public ICommand ScanNetworkPrintersCommand { get; private set; }
        public ICommand ScanBluetoothPrintersCommand { get; private set; }
        public ICommand TestTcpConnectionCommand { get; private set; }
        public ICommand TestBluetoothConnectionCommand { get; private set; }

        // 라벨 템플릿 파일 (*.lbm) 내보내기 및 불러오기 커맨드
        public ICommand ExportTemplateCommand { get; private set; }
        public ICommand ImportTemplateCommand { get; private set; }
        public ICommand SaveTemplateCommand { get; private set; }
        public ICommand LoadTemplateCommand { get; private set; }

        // 미리보기 페이지 네비게이션 커맨드
        public ICommand PreviewFirstPageCommand { get; private set; }
        public ICommand PreviewPreviousPageCommand { get; private set; }
        public ICommand PreviewNextPageCommand { get; private set; }
        public ICommand PreviewLastPageCommand { get; private set; }
        public ICommand PreviewLiveInputCommand { get; private set; }

        // 구간 인쇄 커맨드
        public ICommand PrintRangeCommand { get; private set; }

        // 특정 컬럼 삭제 및 순번 커맨드
        public ICommand RemoveSpecificColumnCommand { get; private set; }
        public ICommand IncreaseSerialStartNumberCommand { get; private set; }
        public ICommand DecreaseSerialStartNumberCommand { get; private set; }

        // 회전 제어 커맨드
        public ICommand Rotate90Command { get; private set; }
        public ICommand SetRotation0Command { get; private set; }
        public ICommand SetRotation90Command { get; private set; }
        public ICommand SetRotation180Command { get; private set; }
        public ICommand SetRotation270Command { get; private set; }

        // 전용 팝업 윈도우 열기 커맨드
        public ICommand OpenDesignEditorCommand { get; private set; }
        public ICommand OpenPrinterSettingsCommand { get; private set; }
        public ICommand OpenAboutCommand { get; private set; }
        public ICommand OpenColumnFormatCommand { get; private set; }

        #endregion

        private void InitializeCommands()
        {
            Rotate90Command = new RelayCommand(_ => LabelRotationAngle = (LabelRotationAngle + 90) % 360);
            SetRotation0Command = new RelayCommand(_ => LabelRotationAngle = 0);
            SetRotation90Command = new RelayCommand(_ => LabelRotationAngle = 90);
            SetRotation180Command = new RelayCommand(_ => LabelRotationAngle = 180);
            SetRotation270Command = new RelayCommand(_ => LabelRotationAngle = 270);

            OpenDesignEditorCommand = new RelayCommand(_ => ExecuteOpenDesignEditor());
            OpenPrinterSettingsCommand = new RelayCommand(_ => ExecuteOpenPrinterSettings());
            OpenAboutCommand = new RelayCommand(_ => ExecuteOpenAbout());
            OpenColumnFormatCommand = new RelayCommand(param => ExecuteOpenColumnFormat(param));
            ExportTemplateCommand = new RelayCommand(_ => ExecuteExportTemplate());
            ImportTemplateCommand = new RelayCommand(_ => ExecuteImportTemplate());
            SaveTemplateCommand = ExportTemplateCommand;
            LoadTemplateCommand = ImportTemplateCommand;
            PrintAndRecordCommand = new RelayCommand(_ => ExecutePrintAndRecord(), _ => IsCurrentPrinterAuthorized);
            PrintOnlyCommand = new RelayCommand(_ => ExecutePrintOnly(), _ => IsCurrentPrinterAuthorized);
            RecordOnlyCommand = new RelayCommand(_ => ExecuteRecordOnly());
            SinglePrintCommand = new RelayCommand(_ => ExecutePrintOnly(), _ => IsCurrentPrinterAuthorized);
            ExportToExcelCommand = new RelayCommand(_ => ExecuteExportToExcel());
            ImportFromExcelCommand = new RelayCommand(_ => ExecuteImportFromExcel());
            ImportBackupCommand = new RelayCommand(_ => ExecuteImportBackup());
            ClearRecordsCommand = new RelayCommand(_ => ExecuteClearRecords());
            AddNewBlankRecordCommand = new RelayCommand(_ => AddNewBlankRecord());
            DeleteSelectedRecordCommand = new RelayCommand(param => ExecuteDeleteSelectedRecord(param));
            ReprintSelectedRecordsCommand = new RelayCommand(param => ExecuteReprintSelectedRecords(param), _ => IsCurrentPrinterAuthorized);
            PrintAllRecordsCommand = new RelayCommand(_ => ExecutePrintAllRecords(), _ => IsCurrentPrinterAuthorized);
            PrintUnprintedRecordsCommand = new RelayCommand(_ => ExecutePrintUnprintedRecords(), _ => IsCurrentPrinterAuthorized);
            PrintRangeCommand = new RelayCommand(_ => ExecutePrintRange(), _ => IsCurrentPrinterAuthorized);
            DuplicateSelectedRecordsCommand = new RelayCommand(param => ExecuteDuplicateRecords(param));
            DuplicateSingleRecordCommand = new RelayCommand(param => ExecuteDuplicateSingleRecord(param));
            IncreaseCopyCountCommand = new RelayCommand(_ => CopyCount = Math.Min(999, CopyCount + 1));
            DecreaseCopyCountCommand = new RelayCommand(_ => CopyCount = Math.Max(1, CopyCount - 1));
            IncreasePrintCopiesCommand = new RelayCommand(_ => PrintCopies = Math.Min(999, PrintCopies + 1));
            DecreasePrintCopiesCommand = new RelayCommand(_ => PrintCopies = Math.Max(1, PrintCopies - 1));

            RemoveSpecificColumnCommand = new RelayCommand(param => {
                if (int.TryParse(param?.ToString(), out int colIdx))
                {
                    RemoveColumnAt(colIdx);
                }
            });
            IncreaseSerialStartNumberCommand = new RelayCommand(_ => SerialStartNumber += Math.Max(1, Math.Abs(SerialStep)));
            DecreaseSerialStartNumberCommand = new RelayCommand(_ => SerialStartNumber = Math.Max(0, SerialStartNumber - Math.Max(1, Math.Abs(SerialStep))));
            ToggleAllRecordsCheckedCommand = new RelayCommand(param => {
                if (param is bool b)
                {
                    IsAllRecordsChecked = b;
                }
                else
                {
                    bool target = !(IsAllRecordsChecked == true);
                    IsAllRecordsChecked = target;
                }
            });
            SaveSettingsCommand = new RelayCommand(_ => ExecuteSaveSettings());
            ClearInputsCommand = new RelayCommand(_ => 
            { 
                ClearInputs(forceAll: false); 
                FocusFirstInputRequested?.Invoke(); 
            });
            RefreshPrintersCommand = new RelayCommand(_ => RefreshPrinters());

            ZoomInCommand = new RelayCommand(_ => ZoomRatio += 0.15);
            ZoomOutCommand = new RelayCommand(_ => ZoomRatio -= 0.15);
            ResetZoomCommand = new RelayCommand(_ => ZoomRatio = 1.4);

            IncreaseItemSizeCommand = new RelayCommand(param => ChangeSizeByParam(param, 1));
            DecreaseItemSizeCommand = new RelayCommand(param => ChangeSizeByParam(param, -1));

            AddColumnCommand = new RelayCommand(_ => AddNewColumn());
            RemoveColumnCommand = new RelayCommand(_ => { if (ActiveColumnCount > 1) RemoveColumnAt(ActiveColumnCount); });
            RemoveColumnAtCommand = new RelayCommand(param => {
                if (param != null && int.TryParse(param.ToString(), out int idx))
                {
                    RemoveColumnAt(idx);
                }
            });
            MoveColumnUpCommand = new RelayCommand(param => {
                if (param != null && int.TryParse(param.ToString(), out int idx))
                {
                    MoveColumnUp(idx);
                }
            });
            MoveColumnDownCommand = new RelayCommand(param => {
                if (param != null && int.TryParse(param.ToString(), out int idx))
                {
                    MoveColumnDown(idx);
                }
            });

            ResetLayoutCommand = new RelayCommand(_ => ExecuteResetLayout());
            AlignLeftCommand = new RelayCommand(_ => ExecuteAlignLeft());
            AlignCenterCommand = new RelayCommand(_ => ExecuteAlignCenter());
            AlignRightCommand = new RelayCommand(_ => ExecuteAlignRight());
            DistributeVerticalCommand = new RelayCommand(_ => ExecuteDistributeVertical());
            DistributeHorizontalCommand = new RelayCommand(_ => ExecuteDistributeHorizontal());
            AutoFitTableLayoutCommand = new RelayCommand(_ => ExecuteAutoFitTableLayout());

            IncreaseTableTitleSizeCommand = new RelayCommand(_ => { TableTitleSize = Math.Min(36, TableTitleSize + 1); });
            DecreaseTableTitleSizeCommand = new RelayCommand(_ => { TableTitleSize = Math.Max(6, TableTitleSize - 1); });
            SetTableTitleAlignCommand = new RelayCommand(param => { if (param != null) TableTitleAlign = param.ToString(); });

            AutoDetectCommand = new RelayCommand(_ => ExecuteAutoDetectUsbPrinters());
            HomeCommand = new RelayCommand(_ => ExecutePrinterAction("HOME"));
            FeedCommand = new RelayCommand(_ => ExecutePrinterAction("FEED"));
            TestPrintCommand = new RelayCommand(_ => ExecuteTestPrint());
            SelfTestCommand = new RelayCommand(_ => ExecutePrinterAction("SELFTEST"));
            ResetPrinterCommand = new RelayCommand(_ => ExecutePrinterAction("RESET"));
            RebootPrinterCommand = ResetPrinterCommand;
            FactoryResetPrinterCommand = new RelayCommand(_ => ExecutePrinterAction("FACTORY_RESET"));
            SetConnectionTypeCommand = new RelayCommand(param => { if (param != null) SelectedConnectionType = param.ToString(); });
            ScanNetworkPrintersCommand = new RelayCommand(async _ => await ExecuteScanNetworkPrintersAsync());
            TestTcpConnectionCommand = new RelayCommand(_ => ExecuteTestTcpConnection());
            TestBluetoothConnectionCommand = new RelayCommand(_ => ExecuteTestBluetoothConnection());

            IncreaseSinglePrintCopiesCommand = new RelayCommand(_ => SinglePrintCopies = Math.Min(999, SinglePrintCopies + 1));
            DecreaseSinglePrintCopiesCommand = new RelayCommand(_ => SinglePrintCopies = Math.Max(1, SinglePrintCopies - 1));
            IncreaseExcelPrintCopiesCommand = new RelayCommand(_ => ExcelPrintCopies = Math.Min(999, ExcelPrintCopies + 1));
            DecreaseExcelPrintCopiesCommand = new RelayCommand(_ => ExcelPrintCopies = Math.Max(1, ExcelPrintCopies - 1));
            ToggleBoldCommand = new RelayCommand(param => {
                if (param != null && int.TryParse(param.ToString(), out int idx))
                {
                    switch (idx)
                    {
                        case 1: Col1Bold = !Col1Bold; break;
                        case 2: Col2Bold = !Col2Bold; break;
                        case 3: Col3Bold = !Col3Bold; break;
                        case 4: Col4Bold = !Col4Bold; break;
                        case 5: Col5Bold = !Col5Bold; break;
                        case 6: Col6Bold = !Col6Bold; break;
                        case 7: Col7Bold = !Col7Bold; break;
                        case 8: Col8Bold = !Col8Bold; break;
                        case 100: TableTitleBold = !TableTitleBold; break;
                        case 101: SerialBold = !SerialBold; break;
                        case 99: DateTimeBold = !DateTimeBold; break;
                    }
                }
            });

            ToggleItalicCommand = new RelayCommand(param => {
                if (param != null && int.TryParse(param.ToString(), out int idx))
                {
                    switch (idx)
                    {
                        case 1: Col1Italic = !Col1Italic; break;
                        case 2: Col2Italic = !Col2Italic; break;
                        case 3: Col3Italic = !Col3Italic; break;
                        case 4: Col4Italic = !Col4Italic; break;
                        case 5: Col5Italic = !Col5Italic; break;
                        case 6: Col6Italic = !Col6Italic; break;
                        case 7: Col7Italic = !Col7Italic; break;
                        case 8: Col8Italic = !Col8Italic; break;
                        case 100: TableTitleItalic = !TableTitleItalic; break;
                        case 101: SerialItalic = !SerialItalic; break;
                        case 99: DateTimeItalic = !DateTimeItalic; break;
                    }
                }
            });

            IncreaseColRowHeightCommand = new RelayCommand(param => {
                if (param != null && int.TryParse(param.ToString(), out int idx))
                {
                    int current = GetRowHeightByIndex(idx);
                    SetRowHeightByIndex(idx, Math.Min(100, current + 1));
                }
            });

            DecreaseColRowHeightCommand = new RelayCommand(param => {
                if (param != null && int.TryParse(param.ToString(), out int idx))
                {
                    int current = GetRowHeightByIndex(idx);
                    SetRowHeightByIndex(idx, Math.Max(0, current - 1));
                }
            });

            IncreasePaperWidthCommand = new RelayCommand(_ => PaperWidth = Math.Min(300, PaperWidth + 1));
            DecreasePaperWidthCommand = new RelayCommand(_ => PaperWidth = Math.Max(10, PaperWidth - 1));
            IncreasePaperHeightCommand = new RelayCommand(_ => PaperHeight = Math.Min(300, PaperHeight + 1));
            DecreasePaperHeightCommand = new RelayCommand(_ => PaperHeight = Math.Max(10, PaperHeight - 1));
            IncreaseGapHeightCommand = new RelayCommand(_ => GapHeight = Math.Min(50, GapHeight + 1));
            DecreaseGapHeightCommand = new RelayCommand(_ => GapHeight = Math.Max(0, GapHeight - 1));

            IncreasePrintDarknessCommand = new RelayCommand(_ => PrintDarkness = Math.Min(15, PrintDarkness + 1));
            DecreasePrintDarknessCommand = new RelayCommand(_ => PrintDarkness = Math.Max(1, PrintDarkness - 1));
            IncreasePrintSpeedCommand = new RelayCommand(_ => PrintSpeed = Math.Min(6, PrintSpeed + 1));
            DecreasePrintSpeedCommand = new RelayCommand(_ => PrintSpeed = Math.Max(2, PrintSpeed - 1));

            IncreasePrinterOffsetXCommand = new RelayCommand(_ => PrinterOffsetX = Math.Min(50, PrinterOffsetX + 1));
            DecreasePrinterOffsetXCommand = new RelayCommand(_ => PrinterOffsetX = Math.Max(-50, PrinterOffsetX - 1));
            IncreasePrinterOffsetYCommand = new RelayCommand(_ => PrinterOffsetY = Math.Min(50, PrinterOffsetY + 1));
            DecreasePrinterOffsetYCommand = new RelayCommand(_ => PrinterOffsetY = Math.Max(-50, PrinterOffsetY - 1));
            ResetPrinterOffsetCommand = new RelayCommand(_ => { PrinterOffsetX = 0; PrinterOffsetY = 0; });
            IncreaseTearOffOffsetCommand = new RelayCommand(_ => TearOffOffset = Math.Min(50, TearOffOffset + 1));
            DecreaseTearOffOffsetCommand = new RelayCommand(_ => TearOffOffset = Math.Max(-50, TearOffOffset - 1));
            IncreaseBlineOffsetCommand = new RelayCommand(_ => BlineOffset = Math.Min(30.0, Math.Round(BlineOffset + 0.5, 1)));
            DecreaseBlineOffsetCommand = new RelayCommand(_ => BlineOffset = Math.Max(-30.0, Math.Round(BlineOffset - 0.5, 1)));

            IncreasePostPrintOffsetCommand = new RelayCommand(_ => PostPrintOffset = Math.Min(50, PostPrintOffset + 1));
            DecreasePostPrintOffsetCommand = new RelayCommand(_ => PostPrintOffset = Math.Max(-50, PostPrintOffset - 1));
            IncreasePostPrintCutIntervalCommand = new RelayCommand(_ => PostPrintCutInterval = Math.Min(999, PostPrintCutInterval + 1));
            DecreasePostPrintCutIntervalCommand = new RelayCommand(_ => PostPrintCutInterval = Math.Max(1, PostPrintCutInterval - 1));

            IncreaseTableTitleSpacingCommand = new RelayCommand(_ => TableTitleSpacing = Math.Min(50, TableTitleSpacing + 1));
            DecreaseTableTitleSpacingCommand = new RelayCommand(_ => TableTitleSpacing = Math.Max(0, TableTitleSpacing - 1));
            IncreaseTableHeaderWidthCommand = new RelayCommand(_ => TableHeaderWidthMm = Math.Min(200, TableHeaderWidthMm + 1));
            DecreaseTableHeaderWidthCommand = new RelayCommand(_ => TableHeaderWidthMm = Math.Max(5, TableHeaderWidthMm - 1));
            IncreaseTableHeaderHeightCommand = new RelayCommand(_ => TableHeaderHeightMm = Math.Min(50, TableHeaderHeightMm + 1));
            DecreaseTableHeaderHeightCommand = new RelayCommand(_ => TableHeaderHeightMm = Math.Max(3, TableHeaderHeightMm - 1));

            IncreaseTableOffsetXCommand = new RelayCommand(_ => TableOffsetX = Math.Min(200, TableOffsetX + 1));
            DecreaseTableOffsetXCommand = new RelayCommand(_ => TableOffsetX = Math.Max(0, TableOffsetX - 1));
            IncreaseTableOffsetYCommand = new RelayCommand(_ => TableOffsetY = Math.Min(200, TableOffsetY + 1));
            DecreaseTableOffsetYCommand = new RelayCommand(_ => TableOffsetY = Math.Max(0, TableOffsetY - 1));
            IncreaseTableWidthCommand = new RelayCommand(_ => TableWidth = Math.Min(300, TableWidth + 1));
            DecreaseTableWidthCommand = new RelayCommand(_ => TableWidth = Math.Max(0, TableWidth - 1));
            IncreaseTableHeightCommand = new RelayCommand(_ => TableHeight = Math.Min(300, TableHeight + 1));
            IncreaseTableTitleOffsetXCommand = new RelayCommand(_ => TableTitleOffsetX = Math.Min(100, TableTitleOffsetX + 1));
            DecreaseTableTitleOffsetXCommand = new RelayCommand(_ => TableTitleOffsetX = Math.Max(-100, TableTitleOffsetX - 1));
            IncreaseTableTitleOffsetYCommand = new RelayCommand(_ => TableTitleOffsetY = Math.Min(100, TableTitleOffsetY + 1));
            DecreaseTableTitleOffsetYCommand = new RelayCommand(_ => TableTitleOffsetY = Math.Max(-100, TableTitleOffsetY - 1));

            MakeRowHeightsEqualCommand = new RelayCommand(_ =>
            {
                Col1RowHeight = 0;
                Col2RowHeight = 0;
                Col3RowHeight = 0;
                Col4RowHeight = 0;
                Col5RowHeight = 0;
                Col6RowHeight = 0;
                Col7RowHeight = 0;
                Col8RowHeight = 0;
                UpdatePreview();
            });

            IncreaseSerialStepCommand = new RelayCommand(_ => SerialStep = Math.Min(100, SerialStep + 1));
            DecreaseSerialStepCommand = new RelayCommand(_ => SerialStep = Math.Max(-100, SerialStep - 1));
            IncreaseSerialSizeCommand = new RelayCommand(_ => SerialSize = Math.Min(40, SerialSize + 1));
            DecreaseSerialSizeCommand = new RelayCommand(_ => SerialSize = Math.Max(6, SerialSize - 1));
            IncreaseSerialOffsetXCommand = new RelayCommand(_ => SerialOffsetX = Math.Min(200, SerialOffsetX + 1));
            DecreaseSerialOffsetXCommand = new RelayCommand(_ => SerialOffsetX = Math.Max(0, SerialOffsetX - 1));
            IncreaseSerialOffsetYCommand = new RelayCommand(_ => SerialOffsetY = Math.Min(200, SerialOffsetY + 1));
            DecreaseSerialOffsetYCommand = new RelayCommand(_ => SerialOffsetY = Math.Max(0, SerialOffsetY - 1));

            IncreaseDateTimeSizeCommand = new RelayCommand(_ => DateTimeSize = Math.Min(40, DateTimeSize + 1));
            DecreaseDateTimeSizeCommand = new RelayCommand(_ => DateTimeSize = Math.Max(6, DateTimeSize - 1));
            IncreaseDateTimeOffsetXCommand = new RelayCommand(_ => DateTimeOffsetX = Math.Min(200, DateTimeOffsetX + 1));
            DecreaseDateTimeOffsetXCommand = new RelayCommand(_ => DateTimeOffsetX = Math.Max(0, DateTimeOffsetX - 1));
            IncreaseDateTimeOffsetYCommand = new RelayCommand(_ => DateTimeOffsetY = Math.Min(200, DateTimeOffsetY + 1));
            DecreaseDateTimeOffsetYCommand = new RelayCommand(_ => DateTimeOffsetY = Math.Max(0, DateTimeOffsetY - 1));

            IncreasePrintRangeStartCommand = new RelayCommand(_ => PrintRangeStart = Math.Min(9999, PrintRangeStart + 1));
            DecreasePrintRangeStartCommand = new RelayCommand(_ => PrintRangeStart = Math.Max(1, PrintRangeStart - 1));
            IncreasePrintRangeEndCommand = new RelayCommand(_ => PrintRangeEnd = Math.Min(9999, PrintRangeEnd + 1));
            DecreasePrintRangeEndCommand = new RelayCommand(_ => PrintRangeEnd = Math.Max(1, PrintRangeEnd - 1));

            PreviewFirstPageCommand = new RelayCommand(_ => NavigatePreviewFirst());
            PreviewPreviousPageCommand = new RelayCommand(_ => NavigatePreviewPrev());
            PreviewNextPageCommand = new RelayCommand(_ => NavigatePreviewNext());
            PreviewLastPageCommand = new RelayCommand(_ => NavigatePreviewLast());
            PreviewLiveInputCommand = new RelayCommand(_ => NavigatePreviewLiveInput());

            ReadPrinterHardwareInfoCommand = new RelayCommand(_ => ExecuteReadPrinterHardwareInfo());
            ScanBluetoothPrintersCommand = new RelayCommand(async _ => await ExecuteScanBluetoothPrintersAsync());
            CalibrateCommand = new RelayCommand(_ => ExecutePrinterAction("GAPDETECT"));
        }

        public ICommand CalibrateCommand { get; private set; }

        public ICommand IncreasePaperWidthCommand { get; private set; }
        public ICommand DecreasePaperWidthCommand { get; private set; }
        public ICommand IncreasePaperHeightCommand { get; private set; }
        public ICommand DecreasePaperHeightCommand { get; private set; }
        public ICommand IncreaseGapHeightCommand { get; private set; }
        public ICommand DecreaseGapHeightCommand { get; private set; }
        public ICommand IncreasePrintDarknessCommand { get; private set; }
        public ICommand DecreasePrintDarknessCommand { get; private set; }
        public ICommand IncreasePrintSpeedCommand { get; private set; }
        public ICommand DecreasePrintSpeedCommand { get; private set; }
        public ICommand IncreasePrinterOffsetXCommand { get; private set; }
        public ICommand DecreasePrinterOffsetXCommand { get; private set; }
        public ICommand IncreasePrinterOffsetYCommand { get; private set; }
        public ICommand DecreasePrinterOffsetYCommand { get; private set; }
        public ICommand ResetPrinterOffsetCommand { get; private set; }
        public ICommand IncreaseTearOffOffsetCommand { get; private set; }
        public ICommand DecreaseTearOffOffsetCommand { get; private set; }
        public ICommand IncreaseBlineOffsetCommand { get; private set; }
        public ICommand DecreaseBlineOffsetCommand { get; private set; }
        public ICommand IncreasePostPrintOffsetCommand { get; private set; }
        public ICommand DecreasePostPrintOffsetCommand { get; private set; }
        public ICommand IncreasePostPrintCutIntervalCommand { get; private set; }
        public ICommand DecreasePostPrintCutIntervalCommand { get; private set; }

        public ICommand IncreaseTableTitleSpacingCommand { get; private set; }
        public ICommand DecreaseTableTitleSpacingCommand { get; private set; }
        public ICommand IncreaseTableHeaderWidthCommand { get; private set; }
        public ICommand DecreaseTableHeaderWidthCommand { get; private set; }
        public ICommand IncreaseTableOffsetXCommand { get; private set; }
        public ICommand DecreaseTableOffsetXCommand { get; private set; }
        public ICommand IncreaseTableOffsetYCommand { get; private set; }
        public ICommand DecreaseTableOffsetYCommand { get; private set; }
        public ICommand IncreaseTableWidthCommand { get; private set; }
        public ICommand DecreaseTableWidthCommand { get; private set; }
        public ICommand IncreaseTableHeightCommand { get; private set; }
        public ICommand DecreaseTableHeightCommand { get; private set; }
        public ICommand IncreaseTableTitleOffsetXCommand { get; private set; }
        public ICommand DecreaseTableTitleOffsetXCommand { get; private set; }
        public ICommand IncreaseTableTitleOffsetYCommand { get; private set; }
        public ICommand DecreaseTableTitleOffsetYCommand { get; private set; }
        public ICommand IncreaseColRowHeightCommand { get; private set; }
        public ICommand DecreaseColRowHeightCommand { get; private set; }
        public ICommand MakeRowHeightsEqualCommand { get; private set; }

        public ICommand IncreaseSerialStepCommand { get; private set; }
        public ICommand DecreaseSerialStepCommand { get; private set; }
        public ICommand IncreaseSerialSizeCommand { get; private set; }
        public ICommand DecreaseSerialSizeCommand { get; private set; }
        public ICommand IncreaseSerialOffsetXCommand { get; private set; }
        public ICommand DecreaseSerialOffsetXCommand { get; private set; }
        public ICommand IncreaseSerialOffsetYCommand { get; private set; }
        public ICommand DecreaseSerialOffsetYCommand { get; private set; }

        public ICommand IncreaseDateTimeSizeCommand { get; private set; }
        public ICommand DecreaseDateTimeSizeCommand { get; private set; }
        public ICommand IncreaseDateTimeOffsetXCommand { get; private set; }
        public ICommand DecreaseDateTimeOffsetXCommand { get; private set; }
        public ICommand IncreaseDateTimeOffsetYCommand { get; private set; }
        public ICommand DecreaseDateTimeOffsetYCommand { get; private set; }

        public ICommand IncreasePrintRangeStartCommand { get; private set; }
        public ICommand DecreasePrintRangeStartCommand { get; private set; }
        public ICommand IncreasePrintRangeEndCommand { get; private set; }
        public ICommand DecreasePrintRangeEndCommand { get; private set; }

        public ICommand ToggleBoldCommand { get; private set; }
        public ICommand ToggleItalicCommand { get; private set; }

        public ICommand IncreaseSinglePrintCopiesCommand { get; private set; }
        public ICommand DecreaseSinglePrintCopiesCommand { get; private set; }
        public ICommand IncreaseExcelPrintCopiesCommand { get; private set; }
        public ICommand DecreaseExcelPrintCopiesCommand { get; private set; }
        public ICommand PrintSelectedRecordsCommand => ReprintSelectedRecordsCommand;
        public ICommand ReadPrinterHardwareInfoCommand { get; private set; }

        private bool _isReadingHardwareInfo = false;

        public async void ExecuteReadPrinterHardwareInfo(bool showLoadingPopup = true)
        {
            if (_isReadingHardwareInfo) return;
            _isReadingHardwareInfo = true;

            WpfView.ConnectionProgressWindow progressWin = null;
            try
            {
                if (string.IsNullOrEmpty(SelectedPrinterName))
                {
                    PrinterHardwareSummaryText = "⚠️ 선택된 프린터가 없습니다. 프린터를 먼저 선택해주세요.";
                    IsCurrentPrinterAuthorized = false;
                    return;
                }

                if (_settings != null)
                {
                    _settings.LastSelectedPrinterName = SelectedPrinterName;
                    _settings.Save();
                }

                PrinterHardwareSummaryText = $"⏳ [{SelectedPrinterName}] 프린터 하드웨어 정보 실시간 통신 중...";

                // 연결 시도 안내 팝업 표시
                if (showLoadingPopup && Application.Current != null)
                {
                    try
                    {
                        var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
                        progressWin = new WpfView.ConnectionProgressWindow(SelectedPrinterName, owner);
                        progressWin.Show();
                    }
                    catch { }
                }

                string currentPrinter = SelectedPrinterName;
                var dict = await System.Threading.Tasks.Task.Run(() => _printerService.GetPrinterHardwareDetails(currentPrinter, _cachedPrinters));

                PrinterHardwareDetailsList.Clear();
                string serial = "-";
                foreach (var kvp in dict)
                {
                    PrinterHardwareDetailsList.Add(kvp);
                    if (kvp.Key.Equals("시리얼", StringComparison.OrdinalIgnoreCase))
                    {
                        serial = kvp.Value;
                    }
                }

                // 캐시된 프린터 정보에 실제 수신된 하드웨어 시리얼 번호 반영
                var cachedInfo = _cachedPrinters?.FirstOrDefault(p => p.Name.Equals(currentPrinter, StringComparison.OrdinalIgnoreCase));
                if (cachedInfo != null && !string.IsNullOrEmpty(serial) && serial != "-")
                {
                    cachedInfo.Serial = serial;
                }

                // 하드웨어 시리얼 번호 정품 라이선스 검증
                bool isAuth = ValidatePrinterAuthorization(serial);
                IsCurrentPrinterAuthorized = isAuth;

                PrinterSerialLogger.WriteLog($"[프린터 시리얼 검증] 대상: {currentPrinter}, 수신 시리얼: {serial}, 결과: {(isAuth ? "✅ 매칭 성공" : "❌ 매칭 실패 (미등록 장치)")}");

                // UI 인쇄 버튼 상태 강제 즉시 갱신
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();

                if (isAuth)
                {
                    PrinterStatusBadge = "🟢 정상 준비 (인증 완료)";
                    PrinterStatusColor = "#22C55E";
                    PrinterHardwareSummaryText = $"✅ [{currentPrinter}] 정품 인증 완료 (시리얼: {serial})";
                }
                else
                {
                    IsCurrentPrinterAuthorized = false;
                    PrinterStatusBadge = "🔴 미등록 프린터 (기기 등록 필요)";
                    PrinterStatusColor = "#DC2626";
                    PrinterHardwareSummaryText = $"❌ [{currentPrinter}] 미등록 프린터 (시리얼: {serial}) - 기기 등록이 필요합니다.";

                    // 안내 팝업을 먼저 닫은 후 경고창 표시
                    if (progressWin != null)
                    {
                        try { progressWin.Close(); } catch { }
                        progressWin = null;
                    }

                    _dialogService.ShowWarning($"사용 권한이 등록되지 않은 프린터입니다. (시리얼: {serial})\n\n프로그램 사용을 위해 기기 등록이 필요합니다. 관리자에게 문의해 주세요.", "기기 등록 필요");
                }
            }
            catch (Exception ex)
            {
                PrinterHardwareSummaryText = $"❌ 정보 조회 실패: {ex.Message}";
                IsCurrentPrinterAuthorized = false;
            }
            finally
            {
                if (progressWin != null)
                {
                    try { progressWin.Close(); } catch { }
                    progressWin = null;
                }
                _isReadingHardwareInfo = false;
            }
        }

        private void ExecuteOpenDesignEditor()
        {
            try
            {
                var win = new WpfView.DesignEditorWindow
                {
                    DataContext = this,
                    Owner = System.Windows.Application.Current?.MainWindow
                };
                win.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"디자인 편집기 창 열기 오류: {ex.Message}");
            }
        }

        private void ExecuteOpenPrinterSettings()
        {
            try
            {
                var win = new WpfView.PrinterSettingsWindow
                {
                    DataContext = this,
                    Owner = System.Windows.Application.Current?.MainWindow
                };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"프린터 설정 창 열기 오류: {ex.Message}");
            }
        }

        private void ExecuteOpenAbout()
        {
            try
            {
                var win = new WpfView.AboutWindow
                {
                    Owner = System.Windows.Application.Current?.MainWindow
                };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"프로그램 정보 창 열기 오류: {ex.Message}");
            }
        }

        public void ExecuteOpenColumnFormat(object param)
        {
            try
            {
                int colIndex = 1;
                if (param != null && int.TryParse(param.ToString(), out int parsed))
                {
                    colIndex = parsed;
                }

                var dlg = new WpfView.ColumnFormatDialog(this, colIndex)
                {
                    Owner = System.Windows.Application.Current?.MainWindow
                };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"컬럼 서식 창 열기 오류: {ex.Message}");
            }
        }

        private void ExecuteAutoDetectUsbPrinters()
        {
            try
            {
                // 1. USB 프린터 하드웨어 스캔 및 목록 갱신 (라벨 배출 명령 없이 장치만 탐색)
                RefreshPrinters();

                if (AvailablePrinters.Count > 0)
                {
                    if (string.IsNullOrEmpty(SelectedPrinterName) || !AvailablePrinters.Contains(SelectedPrinterName))
                    {
                        SelectedPrinterName = AvailablePrinters[0];
                    }

                    StatusMessage = $"🔍 USB 프린터 감지 완료: [{SelectedPrinterName}]";
                    _dialogService?.ShowMessage($"연결된 USB 프린터 [{SelectedPrinterName}]을(를) 자동 감지하여 선택했습니다.", "USB 프린터 자동 감지", System.Windows.MessageBoxImage.Information);

                    // 하드웨어 정보 실시간 조회
                    ExecuteReadPrinterHardwareInfo();
                }
                else
                {
                    StatusMessage = "⚠️ 연결된 USB 프린터를 찾을 수 없습니다.";
                    _dialogService?.ShowWarning("연결된 USB 프린터를 찾을 수 없습니다. 케이블 및 전원을 확인해주세요.", "USB 프린터 감지");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"USB 감지 오류: {ex.Message}";
                _dialogService?.ShowError($"USB 프린터 감지 중 오류가 발생했습니다: {ex.Message}", "오류");
            }
        }

        private async Task ExecuteScanNetworkPrintersAsync()
        {
            try
            {
                IsScanningNetwork = true;
                StatusMessage = "🌐 로컬 네트워크(동일 대역)의 Xprinter를 검색 중입니다...";
                var found = await UsbPrinterHelper.ScanSubnetForXprinterTcpAsync();
                if (found != null && found.Count > 0)
                {
                    foreach (var p in found)
                    {
                        var existing = _cachedPrinters.FirstOrDefault(x => x.Address == p.Address);
                        if (existing != null)
                        {
                            existing.Name = p.Name;
                            existing.FriendlyName = p.FriendlyName;
                            existing.Serial = p.Serial;
                        }
                        else
                        {
                            _cachedPrinters.Add(p);
                        }
                    }
                    ApplyPrinterFilter();
                    SelectedPrinterName = found[0].Name;
                    TcpPrinterIp = found[0].Address;
                    StatusMessage = $"🌐 네트워크 프린터 {found.Count}대 검색 완료! [{found[0].Name}]";
                    _dialogService?.ShowMessage($"네트워크 상에서 {found.Count}대의 Xprinter를 발견했습니다!\n\n첫 번째 검색 장치: {found[0].Name}\n시리얼/MAC: {found[0].Serial}", "네트워크 프린터 검색 완료", System.Windows.MessageBoxImage.Information);
                    
                    // 하드웨어 정보 즉시 실시간 조회
                    ExecuteReadPrinterHardwareInfo();
                }
                else
                {
                    StatusMessage = "⚠️ 검색된 네트워크 Xprinter가 없습니다.";
                    _dialogService?.ShowWarning("현재 네트워크 대역에서 응답하는 Xprinter를 찾을 수 없습니다.\n프린터 전원 및 네트워크 연결 상태를 확인해주세요.", "네트워크 검색 결과");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"네트워크 검색 오류: {ex.Message}";
                _dialogService?.ShowError($"네트워크 검색 오류: {ex.Message}", "오류");
            }
            finally
            {
                IsScanningNetwork = false;
            }
        }

        private void ExecuteTestTcpConnection()
        {
            try
            {
                if (string.IsNullOrEmpty(TcpPrinterIp))
                {
                    _dialogService?.ShowWarning("테스트할 프린터 IP 주소를 입력해주세요.", "IP 주소 입력 필요");
                    return;
                }

                StatusMessage = $"🌐 [{TcpPrinterIp}:{TcpPrinterPort}] 통신 테스트 중...";
                string res = UsbPrinterHelper.QueryTsplSerialViaTcp(TcpPrinterIp, TcpPrinterPort, 1200);
                if (!string.IsNullOrEmpty(res) && res != "-")
                {
                    StatusMessage = $"🟢 네트워크 프린터 통신 성공! (시리얼: {res})";

                    // 통신 성공한 장치를 즉시 캐시 및 인쇄 대상 프린터로 자동 등록 및 선택
                    string netDeviceName = $"[네트워크] {TcpPrinterIp}";
                    var existing = _cachedPrinters.FirstOrDefault(p => p.Type == "LAN" && p.Address == TcpPrinterIp);
                    if (existing != null)
                    {
                        existing.Serial = res;
                        netDeviceName = existing.Name;
                    }
                    else
                    {
                        _cachedPrinters.Add(new UsbPrinterInfo
                        {
                            Name = netDeviceName,
                            FriendlyName = "Network Printer",
                            PortName = $"{TcpPrinterIp}:{TcpPrinterPort}",
                            Serial = res,
                            Type = "LAN",
                            Address = TcpPrinterIp
                        });
                    }

                    ApplyPrinterFilter();
                    SelectedPrinterName = netDeviceName;

                    _dialogService?.ShowMessage($"네트워크 프린터 통신 성공!\n\nIP 주소: {TcpPrinterIp}:{TcpPrinterPort}\n수신 시리얼/정보: {res}\n\n해당 프린터가 현재 인쇄 대상으로 자동 선택되었습니다.", "TCP/IP 통신 테스트 성공", System.Windows.MessageBoxImage.Information);

                    // 우측 하드웨어 정보 실시간 자동 로드
                    ExecuteReadPrinterHardwareInfo();
                }
                else
                {
                    StatusMessage = $"🔴 [{TcpPrinterIp}:{TcpPrinterPort}] 응답 없음";
                    _dialogService?.ShowWarning($"[{TcpPrinterIp}:{TcpPrinterPort}] 연결에 실패했습니다.\nIP 주소 및 프린터 전원/네트워크 상태를 확인해주세요.", "TCP/IP 통신 실패");
                }
            }
            catch (Exception ex)
            {
                _dialogService?.ShowError($"통신 테스트 오류: {ex.Message}", "오류");
            }
        }

        private void ExecuteTestBluetoothConnection()
        {
            try
            {
                if (string.IsNullOrEmpty(BluetoothPortName))
                {
                    _dialogService?.ShowWarning("테스트할 블루투스 COM 포트를 선택해주세요.", "COM 포트 선택 필요");
                    return;
                }

                StatusMessage = $"📶 [{BluetoothPortName}] 통신 테스트 중...";
                string res = UsbPrinterHelper.QueryTsplSerialViaCom(BluetoothPortName, 9600, 1500);
                if (!string.IsNullOrEmpty(res) && res != "-")
                {
                    StatusMessage = $"🟢 블루투스 프린터 통신 성공! (시리얼: {res})";

                    // 통신 성공한 블루투스 장치를 즉시 캐시 및 인쇄 대상 프린터로 자동 등록 및 선택
                    string btDeviceName = $"[블루투스] {BluetoothPortName}";
                    var existing = _cachedPrinters.FirstOrDefault(p => p.Type == "Bluetooth" && p.PortName == BluetoothPortName);
                    if (existing != null)
                    {
                        existing.Serial = res;
                        btDeviceName = existing.Name;
                    }
                    else
                    {
                        _cachedPrinters.Add(new UsbPrinterInfo
                        {
                            Name = btDeviceName,
                            FriendlyName = "Bluetooth Printer",
                            PortName = BluetoothPortName,
                            Serial = res,
                            Type = "Bluetooth",
                            Address = BluetoothPortName
                        });
                    }

                    ApplyPrinterFilter();
                    SelectedPrinterName = btDeviceName;

                    _dialogService?.ShowMessage($"블루투스 프린터 통신 성공!\n\nCOM 포트: {BluetoothPortName}\n수신 시리얼/정보: {res}\n\n해당 프린터가 현재 인쇄 대상으로 자동 선택되었습니다.", "블루투스 통신 테스트 성공", System.Windows.MessageBoxImage.Information);

                    // 우측 하드웨어 정보 실시간 자동 로드
                    ExecuteReadPrinterHardwareInfo();
                }
                else
                {
                    StatusMessage = $"🔴 [{BluetoothPortName}] 응답 없음";
                    _dialogService?.ShowWarning($"[{BluetoothPortName}] 연결에 실패했습니다.\n블루투스 페어링 상태 및 COM 포트 번호를 확인해주세요.", "블루투스 통신 실패");
                }
            }
            catch (Exception ex)
            {
                _dialogService?.ShowError($"통신 테스트 오류: {ex.Message}", "오류");
            }
        }

        private async Task ExecuteScanBluetoothPrintersAsync()
        {
            try
            {
                IsScanningBluetooth = true;
                StatusMessage = "📶 등록된 블루투스 직렬(COM) 포트 및 프린터를 검색 중입니다...";
                var found = await UsbPrinterHelper.ScanBluetoothPrintersAsync();
                if (found != null && found.Count > 0)
                {
                    foreach (var p in found)
                    {
                        var existing = _cachedPrinters.FirstOrDefault(x => x.Type == "Bluetooth" && x.PortName == p.PortName);
                        if (existing != null)
                        {
                            existing.Name = p.Name;
                            existing.FriendlyName = p.FriendlyName;
                        }
                        else
                        {
                            _cachedPrinters.Add(p);
                        }
                    }
                    ApplyPrinterFilter();
                    SelectedPrinterName = found[0].Name;
                    BluetoothPortName = found[0].PortName;
                    StatusMessage = $"📶 블루투스 장치 {found.Count}개 감지 완료! [{found[0].Name}]";
                    _dialogService?.ShowMessage($"블루투스 프린터 {found.Count}개를 감지했습니다!\n\n선택된 장치: {found[0].Name}", "블루투스 장치 검색 완료", System.Windows.MessageBoxImage.Information);

                    // 하드웨어 정보 즉시 실시간 조회
                    ExecuteReadPrinterHardwareInfo();
                }
                else
                {
                    StatusMessage = "⚠️ 검색된 블루투스 COM 포트가 없습니다.";
                    _dialogService?.ShowWarning("사용 가능한 블루투스 직렬(COM) 포트가 없습니다.\nWindows 설정에서 블루투스 프린터 페어링을 확인해주세요.", "블루투스 검색 결과");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"블루투스 검색 오류: {ex.Message}";
                _dialogService?.ShowError($"블루투스 검색 오류: {ex.Message}", "오류");
            }
            finally
            {
                IsScanningBluetooth = false;
            }
        }
    }
}
