using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
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
        public ICommand ImportFromExcelForElementCommand { get; private set; }
        public ICommand ImportBackupCommand { get; private set; }
        public ICommand ClearRecordsCommand { get; private set; }
        public ICommand AddNewBlankRecordCommand { get; private set; }
        public ICommand DeleteSelectedRecordCommand { get; private set; }
        public ICommand ReprintSelectedRecordsCommand { get; private set; }
        public ICommand SinglePrintCommand { get; private set; }
        public ICommand PrintCommand => SinglePrintCommand;
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
        public ICommand AlignTopCommand { get; private set; }
        public ICommand AlignMiddleCommand { get; private set; }
        public ICommand AlignBottomCommand { get; private set; }
        public ICommand DistributeVerticalCommand { get; private set; }
        public ICommand DistributeHorizontalCommand { get; private set; }
        public ICommand AutoFitTableLayoutCommand { get; private set; }

        // 안드로이드 호환 자유 편집 요소 추가 / 삭제 커맨드
        public ICommand AddTextElementCommand { get; private set; }
        public ICommand AddBarcodeElementCommand { get; private set; }
        public ICommand AddQrElementCommand { get; private set; }
        public ICommand AddLineElementCommand { get; private set; }
        public ICommand AddShapeElementCommand { get; private set; }
        public ICommand AddImageElementCommand { get; private set; }
        public ICommand AddTableElementCommand { get; private set; }
        public ICommand AddDateTimeElementCommand { get; private set; }
        public ICommand ImportPdfCommand { get; private set; }
        public ICommand DeleteSelectedElementCommand { get; private set; }
        public ICommand CopySelectedCommand { get; private set; }
        public ICommand DeselectElementCommand { get; private set; }
        public ICommand StepXCommand { get; private set; }
        public ICommand StepYCommand { get; private set; }
        public ICommand StepWidthCommand { get; private set; }
        public ICommand StepHeightCommand { get; private set; }
        public ICommand StepFontSizeCommand { get; private set; }
        public ICommand StepLineWidthCommand { get; private set; }
        public ICommand StepLetterSpacingCommand { get; private set; }
        public ICommand StepFontScaleXCommand { get; private set; }
        public ICommand StepLineSpacingCommand { get; private set; }
        public ICommand ResetTextSpacingCommand { get; private set; }
        public ICommand StepTableRowsCommand { get; private set; }
        public ICommand StepTableColumnsCommand { get; private set; }
        public ICommand StepTableLineWidthCommand { get; private set; }
        public ICommand EvenTableColumnsCommand { get; private set; }
        public ICommand EvenTableRowsCommand { get; private set; }
        public ICommand UnlinkExcelCommand { get; private set; }

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
        public ICommand OpenBluetoothSettingsCommand { get; private set; }
        public ICommand OpenBluetoothComPortSettingsCommand { get; private set; }
        public ICommand OpenCommLogCommand { get; private set; }

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
        public ICommand UnlinkColumnExcelCommand { get; private set; }
        public ICommand UnlinkSelectedElementExcelCommand { get; private set; }
        public ICommand UnlinkTableCellCommand { get; private set; }
        public ICommand ApplyTableRowFontCommand { get; private set; }
        public ICommand ApplyTableColumnFontCommand { get; private set; }
        public ICommand ResetTableFontsCommand { get; private set; }
        public ICommand StepTableTargetFontSizeCommand { get; private set; }
        public ICommand StepTableCellFontSizeCommand { get; private set; }
        public ICommand ToggleTableCellBoldCommand { get; private set; }

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
            ImportFromExcelForElementCommand = new RelayCommand(_ => ExecuteImportExcelForSelectedElement());
            UnlinkSelectedElementExcelCommand = new RelayCommand(_ => UnlinkSelectedElementExcel());
            UnlinkTableCellCommand = new RelayCommand(param => {
                if (param is TableCellItem cell) cell.SelectedExcelColumn = "0";
            });
            ApplyTableRowFontCommand = new RelayCommand(_ => ApplyTableRowFont());
            ApplyTableColumnFontCommand = new RelayCommand(_ => ApplyTableColumnFont());
            ResetTableFontsCommand = new RelayCommand(_ => ResetTableFonts());
            StepTableTargetFontSizeCommand = new RelayCommand(p => StepTableTargetFontSize(ParseStepDelta(p)));
            StepTableCellFontSizeCommand = new RelayCommand(p =>
            {
                if (p is TableCellItem item) StepTableCellFontSize(item, 1.0);
            });
            ToggleTableCellBoldCommand = new RelayCommand(p =>
            {
                if (p is TableCellItem item) ToggleTableCellBold(item);
            });
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
            AlignTopCommand = new RelayCommand(_ => ExecuteAlignTop());
            AlignMiddleCommand = new RelayCommand(_ => ExecuteAlignMiddle());
            AlignBottomCommand = new RelayCommand(_ => ExecuteAlignBottom());
            DistributeVerticalCommand = new RelayCommand(_ => ExecuteDistributeVertical());
            DistributeHorizontalCommand = new RelayCommand(_ => ExecuteDistributeHorizontal());
            AutoFitTableLayoutCommand = new RelayCommand(_ => ExecuteAutoFitTableLayout());

            AddTextElementCommand = new RelayCommand(_ => AddNewElement(ElementType.TEXT));
            AddBarcodeElementCommand = new RelayCommand(_ => AddNewElement(ElementType.BARCODE));
            AddQrElementCommand = new RelayCommand(_ => AddNewElement(ElementType.QR_CODE));
            AddLineElementCommand = new RelayCommand(p => AddNewElement(ElementType.LINE, p?.ToString()));
            AddShapeElementCommand = new RelayCommand(p => AddNewElement(ElementType.SHAPE, p?.ToString()));
            AddImageElementCommand = new RelayCommand(_ => AddImageElement());
            AddTableElementCommand = new RelayCommand(_ => AddNewElement(ElementType.TABLE));
            AddDateTimeElementCommand = new RelayCommand(_ => AddNewElement(ElementType.DATE_TIME));
            ImportPdfCommand = new RelayCommand(_ => OpenPdfPrintStudio());
            DeleteSelectedElementCommand = new RelayCommand(_ => DeleteSelectedElement());
            CopySelectedCommand = new RelayCommand(_ => DuplicateSelectedElement());
            DeselectElementCommand = new RelayCommand(_ => DeselectElement());

            StepXCommand = new RelayCommand(p => StepSelectedElementX(ParseStepDelta(p)));
            StepYCommand = new RelayCommand(p => StepSelectedElementY(ParseStepDelta(p)));
            StepWidthCommand = new RelayCommand(p => StepSelectedElementWidth(ParseStepDelta(p)));
            StepHeightCommand = new RelayCommand(p => StepSelectedElementHeight(ParseStepDelta(p)));
            StepFontSizeCommand = new RelayCommand(p => StepSelectedElementFontSize(ParseStepDelta(p)));
            StepLineWidthCommand = new RelayCommand(p => StepSelectedElementLineWidth(ParseStepDelta(p)));
            StepLetterSpacingCommand = new RelayCommand(p => StepSelectedElementLetterSpacing(ParseStepDelta(p)));
            StepFontScaleXCommand = new RelayCommand(p => StepSelectedElementFontScaleX(ParseStepDelta(p)));
            StepLineSpacingCommand = new RelayCommand(p => StepSelectedElementLineSpacing(ParseStepDelta(p)));
            ResetTextSpacingCommand = new RelayCommand(_ => ResetSelectedElementTextSpacing());
            StepTableRowsCommand = new RelayCommand(p => StepTableRows((int)ParseStepDelta(p)));
            StepTableColumnsCommand = new RelayCommand(p => StepTableColumns((int)ParseStepDelta(p)));
            StepTableLineWidthCommand = new RelayCommand(p => StepTableLineWidth((float)ParseStepDelta(p)));
            EvenTableColumnsCommand = new RelayCommand(_ => EvenTableColumns());
            EvenTableRowsCommand = new RelayCommand(_ => EvenTableRows());
            UnlinkExcelCommand = new RelayCommand(_ => ExecuteUnlinkExcel());
            UnlinkColumnExcelCommand = new RelayCommand(p => { if (p != null && int.TryParse(p.ToString(), out int col)) UnlinkColumnExcel(col); });

            IncreaseTableTitleSizeCommand = new RelayCommand(_ => { TableTitleSize = Math.Min(200, TableTitleSize + 1); });
            DecreaseTableTitleSizeCommand = new RelayCommand(_ => { TableTitleSize = Math.Max(4, TableTitleSize - 1); });
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
            OpenBluetoothSettingsCommand = new RelayCommand(_ => ExecuteOpenBluetoothSettings());
            OpenBluetoothComPortSettingsCommand = new RelayCommand(_ => ExecuteOpenBluetoothComPortSettings());
            OpenCommLogCommand = new RelayCommand(_ => PrinterSerialLogger.OpenCommLogFile());

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
            IncreaseSerialSizeCommand = new RelayCommand(_ => SerialSize = Math.Min(200, SerialSize + 1));
            DecreaseSerialSizeCommand = new RelayCommand(_ => SerialSize = Math.Max(4, SerialSize - 1));
            IncreaseSerialOffsetXCommand = new RelayCommand(_ => SerialOffsetX = Math.Min(200, SerialOffsetX + 1));
            DecreaseSerialOffsetXCommand = new RelayCommand(_ => SerialOffsetX = Math.Max(0, SerialOffsetX - 1));
            IncreaseSerialOffsetYCommand = new RelayCommand(_ => SerialOffsetY = Math.Min(200, SerialOffsetY + 1));
            DecreaseSerialOffsetYCommand = new RelayCommand(_ => SerialOffsetY = Math.Max(0, SerialOffsetY - 1));

            IncreaseDateTimeSizeCommand = new RelayCommand(_ => DateTimeSize = Math.Min(200, DateTimeSize + 1));
            DecreaseDateTimeSizeCommand = new RelayCommand(_ => DateTimeSize = Math.Max(4, DateTimeSize - 1));
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

            ConnectPrinterCommand = new RelayCommand(_ => ExecuteConnectPrinter());
            ReadPrinterHardwareInfoCommand = new RelayCommand(_ => ExecuteConnectPrinter());
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
        public ICommand ConnectPrinterCommand { get; private set; }

        public async void ExecuteConnectPrinter()
        {
            if (string.IsNullOrEmpty(TargetPrinterName))
            {
                _dialogService?.ShowWarning("연결할 대상 프린터를 선택해주세요.", "프린터 선택 필요");
                return;
            }

            var targetInfo = _cachedPrinters?.FirstOrDefault(p => p.Name.Equals(TargetPrinterName, StringComparison.OrdinalIgnoreCase));

            // 미페어링 또는 가상 포트 미연결 블루투스 장치 선택 시 자동 페어링/포트 생성 안내 및 실행
            if (targetInfo != null && (!targetInfo.IsPaired || targetInfo.Name.Contains("(페어링 필요)") || targetInfo.Name.Contains("(가상포트 연결 필요)")))
            {
                bool isJustPort = targetInfo.Name.Contains("(가상포트 연결 필요)");
                string promptMsg = isJustPort
                    ? $"선택하신 [{targetInfo.FriendlyName}] 블루투스 프린터는 Windows에 등록되어 있으나 가상 직렬(COM) 포트가 아직 생성되지 않았습니다.\n\n지금 자동으로 직렬 포트(SPP) 서비스를 활성화하고 COM 포트를 연결하시겠습니까?"
                    : $"선택하신 [{targetInfo.FriendlyName}] 블루투스 프린터는 아직 Windows에 페어링되지 않았습니다.\n\n지금 자동으로 블루투스 페어링(기본 PIN: 0000 / 1234) 및 가상 직렬(COM) 포트 생성을 진행하시겠습니까?";

                bool confirmed = _dialogService != null && _dialogService.ShowQuestion(promptMsg, isJustPort ? "가상 COM 포트 연결" : "블루투스 페어링 진행");

                if (confirmed)
                {
                    StatusMessage = $"⏳ [{targetInfo.FriendlyName}] 블루투스 연결 및 가상 COM 포트 생성 중...";

                    // 고유 MAC 주소(Address/Serial) 또는 장치명 정확히 추출 (MAC 주소 최우선)
                    string devId = "";
                    if (!string.IsNullOrEmpty(targetInfo.Address) && !targetInfo.Address.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                    {
                        devId = targetInfo.Address;
                    }
                    else if (!string.IsNullOrEmpty(targetInfo.Serial) && targetInfo.Serial != "-")
                    {
                        devId = targetInfo.Serial;
                    }
                    else
                    {
                        devId = targetInfo.FriendlyName;
                    }

                    var pairRes = await UsbPrinterHelper.PairAndSetupBluetoothPrinterAsync(devId);

                    if (pairRes.success)
                    {
                        targetInfo.IsPaired = true;

                        if (!string.IsNullOrEmpty(pairRes.portName))
                        {
                            targetInfo.PortName = pairRes.portName;
                            targetInfo.Name = $"[블루투스] {targetInfo.FriendlyName}";
                            targetInfo.Address = pairRes.portName;

                            ApplyPrinterFilter();
                            TargetPrinterName = targetInfo.Name;
                            SelectedPrinterName = targetInfo.Name;
                            BluetoothPortName = pairRes.portName;

                            _dialogService?.ShowMessage(pairRes.message + "\n\n연결 점검 및 하드웨어 정보 조회를 시작합니다.", "연결 완료", System.Windows.MessageBoxImage.Information);
                            ExecuteReadPrinterHardwareInfo(showLoadingPopup: true);
                        }
                        else
                        {
                            targetInfo.PortName = "";
                            targetInfo.Name = $"[블루투스] {targetInfo.FriendlyName} (가상포트 연결 필요)";
                            targetInfo.Address = "";

                            ApplyPrinterFilter();
                            TargetPrinterName = targetInfo.Name;
                            SelectedPrinterName = targetInfo.Name;
                            BluetoothPortName = "가상포트 연결 필요";

                            _dialogService?.ShowMessage(pairRes.message, "페어링 완료", System.Windows.MessageBoxImage.Information);
                        }
                        return;
                    }
                    else
                    {
                        _dialogService?.ShowError(pairRes.message, "연결 실패");
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            // 블루투스 프린터의 경우 유효한 COM 포트 확인
            bool isBt = TargetPrinterName.StartsWith("[블루투스]", StringComparison.OrdinalIgnoreCase) || TargetPrinterName.Contains("블루투스") || IsFilterBt;
            if (isBt)
            {
                var cached = _cachedPrinters?.FirstOrDefault(p => p.Name.Equals(TargetPrinterName, StringComparison.OrdinalIgnoreCase));
                string devName = cached?.FriendlyName ?? TargetPrinterName;
                string cleanDevName = devName.Replace("[블루투스]", "").Trim();
                string realHardwarePort = UsbPrinterHelper.GetBluetoothPortForDeviceName(cleanDevName);
                if (string.IsNullOrEmpty(realHardwarePort) && cleanDevName != devName)
                {
                    realHardwarePort = UsbPrinterHelper.GetBluetoothPortForDeviceName(devName);
                }
                if (string.IsNullOrEmpty(realHardwarePort) && cached != null && !string.IsNullOrEmpty(cached.Address) && !cached.Address.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                {
                    realHardwarePort = UsbPrinterHelper.FindComPortForBluetoothMac(cached.Address);
                }

                // 시스템에 실제 할당된 포트가 있다면 그 포트를 최우선 사용하고 UI와 캐시를 일치시킴
                string targetPort = !string.IsNullOrEmpty(realHardwarePort) ? realHardwarePort : BluetoothPortName;
                if (string.IsNullOrEmpty(targetPort) || !Regex.IsMatch(targetPort, @"COM\d+", RegexOptions.IgnoreCase))
                {
                    if (!string.IsNullOrEmpty(_settings.BluetoothPortName) && Regex.IsMatch(_settings.BluetoothPortName, @"COM\d+", RegexOptions.IgnoreCase))
                    {
                        targetPort = _settings.BluetoothPortName;
                    }
                }

                if (string.IsNullOrEmpty(targetPort) || !Regex.IsMatch(targetPort, @"COM\d+", RegexOptions.IgnoreCase))
                {
                    _dialogService?.ShowWarning($"선택하신 [{TargetPrinterName}] 블루투스 프린터에 할당된 유효한 직렬(COM) 포트가 없습니다.\n\n먼저 [블루투스 장치 검색]을 눌러 포트를 갱신해주세요.", "COM 포트 미할당");
                    return;
                }

                BluetoothPortName = targetPort;
                if (cached != null)
                {
                    cached.PortName = targetPort;
                    cached.Address = targetPort;
                }
                _settings.BluetoothPortName = targetPort;
                _settings.Save();
            }

            SelectedPrinterName = TargetPrinterName;
            ExecuteReadPrinterHardwareInfo(showLoadingPopup: true);
        }

        private bool _isReadingHardwareInfo = false;
        private System.Threading.CancellationTokenSource _hardwareInfoCts;

        public async void ExecuteReadPrinterHardwareInfo(bool showLoadingPopup = true)
        {
            if (_isReadingHardwareInfo) return;
            _isReadingHardwareInfo = true;

            _hardwareInfoCts?.Cancel();
            _hardwareInfoCts = new System.Threading.CancellationTokenSource();
            var token = _hardwareInfoCts.Token;

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
                        progressWin.Cancelled += () =>
                        {
                            try { _hardwareInfoCts?.Cancel(); } catch { }
                        };
                        progressWin.Show();
                    }
                    catch { }
                }

                string currentPrinter = SelectedPrinterName;
                // 최대 4초 타임아웃을 적용하여 무한 대기 팝업 프리징 원천 방지
                var queryTask = System.Threading.Tasks.Task.Run(() => _printerService.GetPrinterHardwareDetails(currentPrinter, _cachedPrinters), token);
                var completedTask = await System.Threading.Tasks.Task.WhenAny(queryTask, System.Threading.Tasks.Task.Delay(4000, token));

                // 비동기 조회 완료 즉시 안내 팝업을 확실하게 닫아 메인 윈도우 프리징 방지
                if (progressWin != null)
                {
                    try { progressWin.Close(); } catch { }
                    progressWin = null;
                }

                if (token.IsCancellationRequested)
                {
                    PrinterHardwareSummaryText = $"⏹ [{currentPrinter}] 프린터 연결 시도가 사용자에 의해 취소되었습니다.";
                    PrinterStatusBadge = "⏹ 연결 취소됨";
                    PrinterStatusColor = "#64748B";
                    IsCurrentPrinterAuthorized = false;
                    return;
                }

                Dictionary<string, string> dict;
                if (completedTask == queryTask)
                {
                    dict = await queryTask;
                }
                else
                {
                    dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    dict["상태"] = "⚠️ 통신 응답 시간 초과 (오프라인)";
                    dict["STATUS"] = "⚠️ 프린터 통신 응답 시간이 초과되었습니다. 전원 및 블루투스 연결을 확인하세요.";
                }

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

                // 네트워크 프린터의 경우 수신된 모델명을 영구 저장하고 표시 이름 자동 승격
                string modelName = null;
                if (dict.TryGetValue("모델명", out string mdl) && !string.IsNullOrWhiteSpace(mdl) && mdl != "-")
                {
                    modelName = mdl.Trim();
                    if (_settings != null && (currentPrinter.StartsWith("[네트워크]", StringComparison.OrdinalIgnoreCase) || IsFilterLan))
                    {
                        _settings.LastTcpPrinterModel = modelName;
                        _settings.Save();
                    }
                }

                // 캐시된 프린터 정보에 실제 수신된 하드웨어 시리얼 번호 및 모델명 반영
                var cachedInfo = _cachedPrinters?.FirstOrDefault(p => p.Name.Equals(currentPrinter, StringComparison.OrdinalIgnoreCase));
                if (cachedInfo != null)
                {
                    if (!string.IsNullOrEmpty(serial) && serial != "-") cachedInfo.Serial = serial;
                    if (!string.IsNullOrEmpty(modelName))
                    {
                        cachedInfo.FriendlyName = modelName;
                        if (cachedInfo.Type.Equals("LAN", StringComparison.OrdinalIgnoreCase))
                        {
                            string newDisplayName = $"[네트워크] {modelName}";
                            if (!cachedInfo.Name.Equals(newDisplayName, StringComparison.OrdinalIgnoreCase))
                            {
                                cachedInfo.Name = newDisplayName;
                                // 콤보박스 목록 갱신 반영
                                ApplyPrinterFilter();
                            }
                        }
                    }
                }

                bool isSuccess = dict.TryGetValue("IS_SUCCESS", out string succ) && succ == "true";
                if (!isSuccess)
                {
                    IsCurrentPrinterAuthorized = false;
                    PrinterHardwareSummaryText = $"🔴 [{currentPrinter}] 통신 응답 없음 (전원 꺼짐 / 케이블 또는 페어링 확인)";
                    PrinterStatusBadge = "🔴 오프라인 (통신 응답 없음 / 전원 꺼짐)";
                    PrinterStatusColor = "#DC2626";
                    PrinterSerialLogger.WriteLog($"[프린터 하드웨어 점검] 대상: {currentPrinter}, 통신 응답 없음 (오프라인)");
                }
                else
                {
                    // 하드웨어 시리얼 번호 정품 라이선스 검증
                    bool isAuth = ValidatePrinterAuthorization(serial);
                    var licenseMode = TrialLicenseManager.EvaluateLicense(isAuth, serial, currentPrinter);
                    UpdateTrialLicenseState();

                    PrinterSerialLogger.WriteLog($"[프린터 시리얼 검증] 대상: {currentPrinter}, 수신 시리얼: {serial}, 결과: {(isAuth ? "✅ 매칭 성공" : "❌ 매칭 실패 (미등록 장치)")}, 라이선스 상태: {licenseMode}");

                    IsCurrentPrinterAuthorized = true;
                    PrinterHardwareSummaryText = $"✅ [{currentPrinter}] 정상 연결됨 (시리얼: {serial})";

                    // UI 스레드 프리징을 방지하기 위해 백그라운드 태스크로 상태 조회 실행
                    CheckRealtimePrinterStatus();
                }

                // UI 인쇄 버튼 상태 강제 즉시 갱신
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
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
                _hardwareInfoCts = null;
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
                TargetPrinterName = SelectedPrinterName;
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

        private async void ExecuteTestBluetoothConnection()
        {
            try
            {
                if (string.IsNullOrEmpty(BluetoothPortName))
                {
                    _dialogService?.ShowWarning("테스트할 블루투스 COM 포트를 선택해주세요.", "COM 포트 선택 필요");
                    return;
                }

                StatusMessage = $"📶 [{BluetoothPortName}] 블루투스 통신 점검 중...";
                var prevCursor = System.Windows.Input.Mouse.OverrideCursor;
                System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                string res = null;
                try
                {
                    res = await Task.Run(() => UsbPrinterHelper.QueryTsplSerialViaCom(BluetoothPortName, 9600, 2500));
                }
                finally
                {
                    System.Windows.Input.Mouse.OverrideCursor = prevCursor;
                }

                if (!string.IsNullOrEmpty(res))
                {
                    string realDevName = UsbPrinterHelper.GetBluetoothDeviceNameForPort(BluetoothPortName);
                    if (string.IsNullOrEmpty(realDevName) || realDevName == BluetoothPortName)
                    {
                        realDevName = (res.StartsWith("XP-") || res.StartsWith("Xprinter")) ? res : "Xprinter (Bluetooth)";
                    }
                    string btDeviceName = !string.IsNullOrEmpty(realDevName) ? $"[블루투스] {realDevName}" : "[블루투스] 블루투스 라벨 프린터";
                    StatusMessage = $"🟢 [{realDevName}] 블루투스 통신 성공! (상태: {res})";

                    // 통신 성공한 블루투스 장치를 즉시 캐시 및 인쇄 대상 프린터로 자동 등록 및 선택
                    var existing = _cachedPrinters.FirstOrDefault(p => p.Type == "Bluetooth" && (p.PortName == BluetoothPortName || p.FriendlyName == realDevName));
                    if (existing != null)
                    {
                        existing.Name = btDeviceName;
                        existing.FriendlyName = realDevName;
                        existing.Serial = res;
                        existing.PortName = BluetoothPortName;
                    }
                    else
                    {
                        _cachedPrinters.Add(new UsbPrinterInfo
                        {
                            Name = btDeviceName,
                            FriendlyName = realDevName,
                            PortName = BluetoothPortName,
                            Serial = res,
                            Type = "Bluetooth",
                            Address = BluetoothPortName
                        });
                    }

                    ApplyPrinterFilter();
                    SelectedPrinterName = btDeviceName;
                    TargetPrinterName = btDeviceName;

                    _dialogService?.ShowMessage($"블루투스 프린터 통신 점검 성공!\n\n• 장치명: {realDevName}\n• COM 포트: {BluetoothPortName}\n• 상태/응답: {res}\n\n해당 프린터가 현재 인쇄 대상으로 자동 선택되었습니다.", "블루투스 연결 확인", System.Windows.MessageBoxImage.Information);

                    // 우측 하드웨어 정보 실시간 자동 로드
                    ExecuteReadPrinterHardwareInfo();
                }
                else
                {
                    StatusMessage = $"🔴 [{BluetoothPortName}] 응답 없음";
                    _dialogService?.ShowWarning($"[{BluetoothPortName}] 연결 점검에 실패했습니다.\n\n확인 사항:\n1. 프린터 전원이 켜져 있는지 확인\n2. Windows 설정에서 블루투스 페어링 상태 확인\n3. 올바른 발신 가상 COM 포트 번호인지 확인", "블루투스 연결 실패");
                }
            }
            catch (Exception ex)
            {
                _dialogService?.ShowError($"통신 점검 오류: {ex.Message}", "오류");
            }
        }

        private void ExecuteOpenBluetoothSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true });
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo("control", "bthprops.cpl") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    _dialogService?.ShowError($"Windows 블루투스 설정을 열 수 없습니다: {ex.Message}", "오류");
                }
            }
        }

        private void ExecuteOpenBluetoothComPortSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo("control", "bthprops.cpl") { UseShellExecute = true });
                _dialogService?.ShowMessage(
                    "Windows 블루투스 COM 포트 설정 창이 열렸습니다.\n\n" +
                    "■ 장치별 COM 포트 추가 방법:\n" +
                    "1. 상단 [COM 포트] 탭으로 이동합니다.\n" +
                    "2. [추가...] 버튼을 클릭합니다.\n" +
                    "3. '발신(이 컴퓨터가 연결 시작)'을 선택하고 원하는 블루투스 프린터를 선택합니다.\n" +
                    "4. [확인]을 누르면 해당 프린터 전용 COM 포트(예: COM3, COM4 등)가 생성됩니다.\n\n" +
                    "생성된 포트 번호를 프로그램의 [블루투스 COM]에서 선택하여 연결 점검을 진행하세요.",
                    "Windows 블루투스 COM 포트 추가 안내",
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _dialogService?.ShowError($"Windows 블루투스 COM 설정을 열 수 없습니다: {ex.Message}", "오류");
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
                    Func<string, string> normKey = s => (s ?? "").Replace(":", "").Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

                    foreach (var p in found)
                    {
                        string pMac = normKey(p.Address);
                        string pSerial = normKey(p.Serial);
                        string pName = normKey(p.FriendlyName);

                        // 블루투스 장치 동일성 판단: 고유 MAC 주소 일치 우선 -> 유효 포트 일치 -> 이름 일치
                        // (주의: PortName이 둘 다 빈 문자열 ""인 미페어링 장치들을 동일 장치로 오인해 덮어쓰지 않음)
                        var existing = _cachedPrinters.FirstOrDefault(x =>
                            x.Type == "Bluetooth" && (
                                (!string.IsNullOrEmpty(pMac) && normKey(x.Address) == pMac) ||
                                (!string.IsNullOrEmpty(pSerial) && normKey(x.Serial) == pSerial && pSerial != "-") ||
                                (!string.IsNullOrEmpty(p.PortName) && !string.IsNullOrEmpty(x.PortName) && x.PortName.Equals(p.PortName, StringComparison.OrdinalIgnoreCase)) ||
                                (string.IsNullOrEmpty(p.PortName) && string.IsNullOrEmpty(x.PortName) && normKey(x.FriendlyName) == pName)
                            )
                        );

                        if (existing != null)
                        {
                            existing.Name = p.Name;
                            existing.FriendlyName = p.FriendlyName;
                            existing.PortName = p.PortName;
                            existing.Serial = p.Serial;
                            existing.Address = p.Address;
                            existing.IsPaired = p.IsPaired;
                        }
                        else
                        {
                            _cachedPrinters.Add(p);
                        }
                    }
                    ApplyPrinterFilter();

                    // 자동 연결 및 강제 선택 방지: 기존 선택 유지 및 사용자 직접 연결 유도
                    if (string.IsNullOrEmpty(SelectedPrinterName) || !AvailablePrinters.Contains(SelectedPrinterName))
                    {
                        TargetPrinterName = found[0].Name;
                    }

                    StatusMessage = $"📶 블루투스 드라이버 프린터 {found.Count}개 검색 완료. 목록에서 프린터를 선택하세요.";

                    var devSummary = string.Join("\n", found.Select(f => $"• {f.Name} (포트: {f.PortName})"));
                    _dialogService?.ShowMessage($"Windows에 등록된 블루투스 라벨 프린터 {found.Count}개를 검색했습니다!\n\n{devSummary}\n\n목록에서 프린터를 선택하여 사용하세요.", "블루투스 검색 완료", System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "⚠️ Windows에 설치된 블루투스 프린터 드라이버가 없습니다.";
                    _dialogService?.ShowWarning("Windows에 설치된 블루투스 라벨 프린터 드라이버를 찾지 못했습니다.\n\n블루투스 라벨 프린터를 사용하시려면 제조사(Xprinter) 드라이버를 먼저 설치하고, 포트를 해당 블루투스 COM 포트로 연결해주세요.", "블루투스 드라이버 검색 결과");
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
