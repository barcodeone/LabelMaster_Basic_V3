using BarcodePrint.Model;



using BarcodePrint.Model.PDFReader;



using BarcodePrint.View;



using DocumentFormat.OpenXml.Drawing;



using DocumentFormat.OpenXml.Drawing.Charts;



using System;



using System.Collections;



using System.Collections.Generic;



using System.Data;



using System.Drawing;



using System.Drawing.Drawing2D;



using System.Drawing.Imaging;



using System.Drawing.Text;



using System.IO;



using System.Linq;



using System.Runtime.InteropServices;



using System.Text;



using System.Threading;



using System.Threading.Tasks;



using System.Windows.Forms;



using UglyToad.PdfPig;



using UglyToad.PdfPig.Content;



using UglyToad.PdfPig.Images;



using ZXing;



using static BarcodePrint.Model.PrinterHelper;



using DataTable = System.Data.DataTable;











namespace BarcodePrint.Controll



{



    public class BarcodePrintManager



    {



        // QR 코드 비트맵 확대/축소용 변수



        private int qrDisplayWidth = 0;



        private int qrDisplayHeight = 0;



        private readonly BarcodeExtractor _barcodeExtractor;



        public Queue<BarcodeData> printQueue_Barcode; //인쇄할때만 사용



        public bool TransferData { get; set; } = false; // 데이터 전송중인 상태



        private MainForm mainForm; // MainForm 참조 추가







        private static object lockObj = new object(); // ✅ 동기화 객체



        private static PrintStatusForm printStatusForm;



        private static Thread printThread;











        public BarcodePrintManager()



        {



            _barcodeExtractor = new BarcodeExtractor();



            printQueue_Barcode = new Queue<BarcodeData>(); // 큐 초기화



        }







        public BarcodePrintManager(MainForm main)



        {



            _barcodeExtractor = new BarcodeExtractor();



            mainForm = main;



            printQueue_Barcode = new Queue<BarcodeData>(); // 큐 초기화



        }







        public void Barcode_PrintGo()



        {



            System.Diagnostics.Debug.WriteLine("=== Barcode_PrintGo 시작 ===");







            try



            {



                System.Diagnostics.Debug.WriteLine($"현재 프린터 정보: Name={PRINT_INFO_CLASS.PrinterInfo.printerName}, TargetPrint={PRINT_INFO_CLASS.PrinterInfo.TargetPrint}");



                System.Diagnostics.Debug.WriteLine($"프린터 포트: {PRINT_INFO_CLASS.PrinterInfo.printerPortName}");



                System.Diagnostics.Debug.WriteLine($"프린터 DPI: {PRINT_INFO_CLASS.PrinterInfo.printer_DPI}");







                //AddToPrintQueue();







                if (!PRINT_INFO_CLASS.PrinterInfo.printerName.Contains(PRINT_INFO_CLASS.PrinterInfo.TargetPrint))



                {



                    System.Diagnostics.Debug.WriteLine("프린터 상태 확인 실패 - TargetPrint와 일치하지 않음");



                    MessageBox.Show("프린터 상태를 확인해 주세요.");



                    return;



                }







                // 큐가 null이거나 비어있으면 종료



                if (printQueue_Barcode == null || printQueue_Barcode.Count == 0)



                {



                    System.Diagnostics.Debug.WriteLine("인쇄 큐가 비어있거나 null입니다.");



                    return;



                }







                System.Diagnostics.Debug.WriteLine($"인쇄 큐 크기: {printQueue_Barcode.Count}");



                System.Diagnostics.Debug.WriteLine("PrintBitmapsFromQueue 호출 시작");







                PrintBitmapsFromQueue(PRINT_INFO_CLASS.PrinterInfo.printerName, printQueue_Barcode);







                System.Diagnostics.Debug.WriteLine("PrintBitmapsFromQueue 호출 완료");



            }



            catch (AccessViolationException avEx)



            {



                System.Diagnostics.Debug.WriteLine($"Barcode_PrintGo에서 AccessViolationException 발생: {avEx.Message}");



                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {avEx.StackTrace}");



                MessageBox.Show($"인쇄 중 메모리 접근 오류가 발생했습니다: {avEx.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);



            }



            catch (Exception ex)



            {



                System.Diagnostics.Debug.WriteLine($"Barcode_PrintGo에서 예외 발생: {ex.GetType().Name} - {ex.Message}");



                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");



                MessageBox.Show($"인쇄 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);



            }



            finally



            {



                System.Diagnostics.Debug.WriteLine("=== Barcode_PrintGo 종료 ===");



            }



        }







        // 인쇄할 페이지를 큐에 추가



        public void AddToPrintQueue(string data, int barcodeSize = 50, int barcodeX = 0, int barcodeY = 0, int barcodeHeight = 50, bool showBarcodeText = true, int barcodeTextSize = 8)



        {



            BarcodeData barcodeData = new BarcodeData();



            barcodeData.BarcodeText = data;



            barcodeData.BarcodeType = GetCurrentBarcodeType(); // 현재 선택된 바코드 타입 사용



            barcodeData.BarcodeSize = barcodeSize; // 바코드 크기 저장



            barcodeData.BarcodeHeight = barcodeHeight; // 바코드 세로 크기 저장



            barcodeData.BarcodeX = barcodeX; // 바코드 X 위치 저장



            barcodeData.BarcodeY = barcodeY; // 바코드 Y 위치 저장



            barcodeData.ShowBarcodeText = showBarcodeText; // 바코드 하단 텍스트 표시 여부



            barcodeData.BarcodeTextSize = barcodeTextSize; // 바코드 하단 텍스트 크기







            // 엑셀 연동 시 텍스트 정보 추가



            if (mainForm != null && !string.IsNullOrEmpty(mainForm.currentBarcodeText))



            {



                barcodeData.ItemText = mainForm.currentBarcodeText;



            }







            // 디버그 메시지



            System.Diagnostics.Debug.WriteLine($"AddToPrintQueue - 설정된 바코드 타입: {barcodeData.BarcodeType}");



            System.Diagnostics.Debug.WriteLine($"AddToPrintQueue - 설정된 바코드 크기: {barcodeData.BarcodeSize}");



            System.Diagnostics.Debug.WriteLine($"AddToPrintQueue - 설정된 바코드 X 위치: {barcodeData.BarcodeX}");



            System.Diagnostics.Debug.WriteLine($"AddToPrintQueue - 설정된 바코드 Y 위치: {barcodeData.BarcodeY}");







            // 큐 초기화 (첫 번째 항목인 경우에만)



            if (printQueue_Barcode == null)



            {



                printQueue_Barcode = new Queue<BarcodeData>();



            }



            else



            {



                // 엑셀 전체 출력 모드가 아닌 경우에만 큐 초기화



                // (단일 항목 출력 시 이전 데이터와 섞이지 않도록)



                if (!IsExcelBatchMode())



                {



                    printQueue_Barcode.Clear();



                }



            }



            printQueue_Barcode.Enqueue(barcodeData);



        }







        private string GetCurrentBarcodeType()



        {



            // MainForm의 GetCurrentBarcodeType 메서드 사용 (다중 컬럼 모드 지원)



            if (mainForm != null)



            {



                string barcodeType = mainForm.GetCurrentBarcodeType();



                System.Diagnostics.Debug.WriteLine($"GetCurrentBarcodeType - MainForm에서 가져온 바코드 타입: {barcodeType}");



                System.Diagnostics.Debug.WriteLine($"인쇄 - MainForm에서 가져온 바코드 타입 사용: {barcodeType}");



                return barcodeType;



            }







            // MainForm이 없는 경우 기본값 사용



            string defaultBarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE";



            System.Diagnostics.Debug.WriteLine($"GetCurrentBarcodeType - 기본 바코드 타입 사용: {defaultBarcodeType}");



            return defaultBarcodeType;



        }







        /// <summary>



        /// 엑셀 배치 출력 모드인지 확인



        /// </summary>



        private bool IsExcelBatchMode()



        {



            // MainForm의 IsExcelBatchMode() 메서드 호출



            return mainForm != null && mainForm.IsExcelBatchMode();



        }







        /// <summary>



        /// 인쇄 큐에 데이터가 있는지 확인



        /// </summary>



        public bool HasPrintQueueData()



        {



            return printQueue_Barcode != null && printQueue_Barcode.Count > 0;



        }







        /// <summary>



        /// 다중 입력 데이터를 인쇄 큐에 추가 (한 용지에 모든 컬럼 출력)



        /// </summary>



        public void AddMultiInputToPrintQueue(List<BarcodePrint.View.MultiColumnData> multiColumnData)



        {



            try



            {



                if (multiColumnData == null || multiColumnData.Count == 0)



                {



                    System.Diagnostics.Debug.WriteLine($"[경고] AddMultiInputToPrintQueue: 데이터가 없습니다.");



                    return;



                }







                // 한 용지에 모든 컬럼을 포함하는 BarcodeData 생성



                BarcodeData barcodeData = new BarcodeData



                {



                    MultiColumnData = multiColumnData



                };







                // 큐에 추가



                if (printQueue_Barcode == null)



                    printQueue_Barcode = new Queue<BarcodeData>();







                printQueue_Barcode.Enqueue(barcodeData);







                System.Diagnostics.Debug.WriteLine($"========================================");



                System.Diagnostics.Debug.WriteLine($"[다중 입력 인쇄 큐 추가] {multiColumnData.Count}개 컬럼");



                foreach (var data in multiColumnData)



                {



                    System.Diagnostics.Debug.WriteLine($"  - {data.ColumnName}: 값='{data.Value}', IsBarcode={data.IsBarcode}, 크기={data.Size}, X={data.OffsetX}, Y={data.OffsetY}, ShowText={data.ShowBarcodeText}");



                }



                System.Diagnostics.Debug.WriteLine($"========================================");



            }



            catch (Exception ex)



            {



                System.Diagnostics.Debug.WriteLine($"다중 입력 데이터 처리 오류: {ex.Message}");



            }



        }







        /// <summary>



        /// 다중 컬럼 데이터를 인쇄 큐에 추가 (한 용지에 모든 컬럼 출력)



        /// </summary>



        /// <returns>실제로 큐에 추가된 경우 true, 추가되지 않은 경우 false</returns>



        public bool AddMultiColumnToPrintQueue(DataTable excelData, List<ExcelMultiColumnSelectionForm.ColumnSetting> settings, int rowIndex)



        {



            try



            {



                if (excelData == null || settings == null || rowIndex >= excelData.Rows.Count)



                {



                    System.Diagnostics.Debug.WriteLine($"[AddMultiColumnToPrintQueue] 검증 실패: excelData={excelData != null}, settings={settings != null}, rowIndex={rowIndex}");



                    return false;



                }







                DataRow row = excelData.Rows[rowIndex];







                System.Diagnostics.Debug.WriteLine($"========================================");



                System.Diagnostics.Debug.WriteLine($"[AddMultiColumnToPrintQueue] 행 {rowIndex} 처리 시작");



                System.Diagnostics.Debug.WriteLine($"  총 settings 수: {settings.Count}");







                // MainForm에서 컬럼 위치 설정 가져오기



                var columnPositionSettings = mainForm?.GetColumnPositionSettings();



                System.Diagnostics.Debug.WriteLine($"  columnPositionSettings 수: {columnPositionSettings?.Count ?? 0}");







                // 다중 컬럼 데이터 생성 (가격표용: 품명, 가격, 바코드)



                var multiColumnData = new List<BarcodePrint.View.MultiColumnData>();







                // 가격표용 컬럼 매핑



                // multiColumnSettings의 ColumnIndex는 0, 1, 2 (품명, 가격, 바코드)



                // columnPositionSettings도 동일한 인덱스 사용







                // 명찰용 5개 컬럼 처리 (Index 0~4) - 미리보기와 100% 동일한 방식으로 데이터 조립



                for (int excelColIndex = 0; excelColIndex < 5; excelColIndex++)
                {
                    // columnPositionSettings에서 위치 정보 가져오기 (물리 인덱스 대신 ColumnIndex 매칭으로 조회)
                    var positionSetting = columnPositionSettings.FirstOrDefault(s => s.ColumnIndex == excelColIndex);
                    if (positionSetting == null) continue;







                    // 인덱스 4(공통문구)는 엑셀 설정이 없을 수 있으므로 직접 조회하거나 처리



                    var excelSetting = settings.FirstOrDefault(s => s.ColumnIndex == excelColIndex && s.IsEnabled);







                    // 공통문구(Index 4)는 엑셀 설정이 없어도 positionSetting이 있으면 처리 (수동 입력값 사용)



                    if (excelSetting == null && excelColIndex != 4)



                        continue;







                    // 바코드 여부 확인 (UsageType 설정을 그대로 따름)



                    bool isBarcode = positionSetting.UsageType == "바코드";







                    // 엑셀 데이터에서 값 가져오기



                    string columnName = null;



                    string value = "";







                    if (isBarcode)



                    {



                        // 바코드 컬럼: 먼저 Barcode_ 형식으로 찾기



                        string barcodeColumnName = excelSetting != null ? $"Barcode_{excelSetting.ColumnIndex}_{positionSetting.BarcodeType ?? "QR_CODE"}" : "";



                        if (!string.IsNullOrEmpty(barcodeColumnName) && excelData.Columns.Contains(barcodeColumnName))



                        {



                            columnName = barcodeColumnName;



                            value = row[columnName]?.ToString() ?? "";



                            System.Diagnostics.Debug.WriteLine($"    → 바코드 컬럼 '{barcodeColumnName}' 찾음, 값='{value}'");



                        }



                        else if (excelSetting != null)



                        {



                            // Barcode_ 형식이 없으면 Text_ 형식으로 찾기



                            string textColumnName = $"Text_{excelSetting.ColumnIndex}";



                            if (excelData.Columns.Contains(textColumnName))



                            {



                                columnName = textColumnName;



                                value = row[columnName]?.ToString() ?? "";



                                System.Diagnostics.Debug.WriteLine($"    → 바코드 컬럼 '{barcodeColumnName}' 없음, Text_ 형식 '{textColumnName}' 사용, 값='{value}'");



                            }



                        }



                    }



                    else if (excelColIndex == 4)



                    {



                        // 공통문구: 수동 입력값 사용



                        columnName = "공통문구";



                        value = mainForm?.GetSenderReceiverLabel() ?? "";







                        // "표시없음"이면 제외



                        if (value == "표시없음")



                        {



                            System.Diagnostics.Debug.WriteLine($"[공통문구 제외] 인덱스 4: 표시없음이므로 인쇄에서 제외");



                            continue;



                        }



                    }



                    else if (excelSetting != null)



                    {



                        // 텍스트 컬럼: Text_ 형식으로 찾기



                        columnName = $"Text_{excelSetting.ColumnIndex}";



                        if (excelData.Columns.Contains(columnName))



                        {



                            string rawValue = row[columnName]?.ToString() ?? "";
                            string combined = "";
                            string prefixSeparator = excelSetting.UsePrefixNewLine ? "\n" : " ";
                            string suffixSeparator = excelSetting.UseSuffixNewLine ? "\n" : " ";
                            if (!string.IsNullOrWhiteSpace(excelSetting.Prefix))
                            {
                                combined += excelSetting.Prefix.Trim() + prefixSeparator;
                            }
                            combined += rawValue;
                            if (!string.IsNullOrWhiteSpace(excelSetting.Suffix))
                            {
                                combined += suffixSeparator + excelSetting.Suffix.Trim();
                            }
                            value = combined;



                        }



                        else



                        {



                            // 텍스트 모드지만 Text_ 컬럼이 없는 경우 (예: 바코드 컬럼을 텍스트로 전환 시) Barcode_ 컬럼 시도



                            string barcodeColumnNameCheck = $"Barcode_{excelSetting.ColumnIndex}_{positionSetting.BarcodeType ?? "QR_CODE"}";



                            if (excelData.Columns.Contains(barcodeColumnNameCheck))



                            {



                                columnName = barcodeColumnNameCheck;



                                string rawValue = row[columnName]?.ToString() ?? "";
                                string combined = "";
                                string prefixSeparator = excelSetting.UsePrefixNewLine ? "\n" : " ";
                                string suffixSeparator = excelSetting.UseSuffixNewLine ? "\n" : " ";
                                if (!string.IsNullOrWhiteSpace(excelSetting.Prefix))
                                {
                                    combined += excelSetting.Prefix.Trim() + prefixSeparator;
                                }
                                combined += rawValue;
                                if (!string.IsNullOrWhiteSpace(excelSetting.Suffix))
                                {
                                    combined += suffixSeparator + excelSetting.Suffix.Trim();
                                }
                                value = combined;



                                System.Diagnostics.Debug.WriteLine($"    → Text 컬럼 '{columnName}' 없음, Barcode 컬럼 '{barcodeColumnNameCheck}' 사용 (텍스트 모드), 값='{value}'");



                            }



                        }



                    }







                    if (string.IsNullOrEmpty(columnName) || (excelColIndex != 4 && !excelData.Columns.Contains(columnName)))



                    {



                        System.Diagnostics.Debug.WriteLine($"    → 엑셀 컬럼 '{columnName ?? "null"}' 없음");



                        continue;



                    }







                    if (string.IsNullOrEmpty(value) && excelColIndex != 4)



                    {



                        System.Diagnostics.Debug.WriteLine($"    → 엑셀 컬럼 {excelColIndex} 값이 비어있음");



                        continue;



                    }







                    // 바코드 하단 텍스트 표시 여부 (설정값 사용)



                    bool showBarcodeText = positionSetting.ShowBarcodeText;







                    // 컬럼명 결정 (0=컬럼1, 1=컬럼2, 2=컬럼3, 3=컬럼4, 4=공통문구)



                    string finalColumnName = excelColIndex == 0 ? "컬럼1" :



                                            excelColIndex == 1 ? "컬럼2" :



                                            excelColIndex == 2 ? "컬럼3" :



                                            excelColIndex == 3 ? "컬럼4" : "공통문구";







                    multiColumnData.Add(new BarcodePrint.View.MultiColumnData



                    {



                        Value = value,



                        IsBarcode = isBarcode,



                        BarcodeType = isBarcode ? (positionSetting.BarcodeType ?? BarcodePrint.Model.PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "QR_CODE") : null,



                        ColumnName = finalColumnName, // 명확한 컬럼명 사용



                        Size = positionSetting.Size,



                        BarcodeHeight = positionSetting.BarcodeHeight,



                        OffsetX = positionSetting.OffsetX,



                        OffsetY = positionSetting.OffsetY,



                        ShowBarcodeText = showBarcodeText,



                        BarcodeTextOffsetX = positionSetting.BarcodeTextOffsetX,



                        BarcodeTextOffsetY = positionSetting.BarcodeTextOffsetY,



                        BarcodeTextSize = positionSetting.BarcodeTextSize,



                        FontFamily = positionSetting.FontFamily,



                        IsBold = positionSetting.IsBold,



                        CustomWidth = positionSetting.CustomWidth,



                        RotateAngle = positionSetting.RotateAngle,



                        Alignment = positionSetting.Alignment



                    });







                    System.Diagnostics.Debug.WriteLine($"[엑셀 인쇄] 컬럼 {excelColIndex} ({finalColumnName}): 값='{value}', IsBarcode={isBarcode}, 크기={positionSetting.Size}, X={positionSetting.OffsetX}, Y={positionSetting.OffsetY}");



                }







                // 한 용지에 모든 컬럼을 포함하는 BarcodeData 생성



                if (multiColumnData.Count > 0)



                {



                    BarcodeData barcodeData = new BarcodeData



                    {



                        MultiColumnData = multiColumnData



                    };







                    // 큐에 추가



                    if (printQueue_Barcode == null)



                        printQueue_Barcode = new Queue<BarcodeData>();







                    printQueue_Barcode.Enqueue(barcodeData);







                    System.Diagnostics.Debug.WriteLine($"========================================");



                    System.Diagnostics.Debug.WriteLine($"[인쇄 큐 추가] 행 {rowIndex}: {multiColumnData.Count}개 컬럼");



                    foreach (var data in multiColumnData)



                    {



                        System.Diagnostics.Debug.WriteLine($"  - {data.ColumnName}: 값='{data.Value}', 타입={data.IsBarcode}, 크기={data.Size}, X={data.OffsetX}, Y={data.OffsetY}, ShowText={data.ShowBarcodeText}");



                    }



                    System.Diagnostics.Debug.WriteLine($"========================================");







                    return true; // 성공적으로 추가됨



                }



                else



                {



                    System.Diagnostics.Debug.WriteLine($"========================================");



                    System.Diagnostics.Debug.WriteLine($"[경고] 행 {rowIndex}: 인쇄할 컬럼 데이터가 없습니다.");



                    System.Diagnostics.Debug.WriteLine($"  multiColumnData.Count = 0");



                    System.Diagnostics.Debug.WriteLine($"  활성화된 컬럼 수: {settings.Count(s => s.IsEnabled)}");



                    System.Diagnostics.Debug.WriteLine($"========================================");



                    return false; // 추가되지 않음



                }



            }



            catch (Exception ex)



            {



                System.Diagnostics.Debug.WriteLine($"다중 컬럼 데이터 처리 오류: {ex.Message}");



                return false;



            }



        }







        /// <summary>



        /// 현재 바코드 크기 가져오기



        /// </summary>



        private int GetCurrentBarcodeSize()



        {



            return PRINT_INFO_CLASS.PrinterInfo.BarcodeSize;



        }







        /// <summary>



        /// 현재 바코드 X 위치 가져오기



        /// </summary>



        private int GetCurrentBarcodeX()



        {



            return PRINT_INFO_CLASS.PrinterInfo.BarcodeX;



        }







        /// <summary>



        /// 현재 바코드 Y 위치 가져오기



        /// </summary>



        private int GetCurrentBarcodeY()



        {



            return PRINT_INFO_CLASS.PrinterInfo.BarcodeY;



        }











        // 원래코드



        public void PrintBitmapsFromQueue(string printerName, Queue<BarcodeData> printQueue)



        {



            if (printQueue.Count == 0)



                return;







            lock (lockObj) // ✅ 동시 실행 방지를 위한 lock



            {



                // 이미 인쇄 중인 스레드가 있으면 대기 또는 건너뜀



                if (printThread != null && printThread.IsAlive)



                {



                    System.Diagnostics.Debug.WriteLine("이미 인쇄 중인 작업이 있으므로 건너뜀");



                    return;



                }







                if (printStatusForm == null || printStatusForm.IsDisposed)



                {



                    /*



                    // UI 스레드에서 폼 실행



                    Application.OpenForms[0]?.Invoke(new Action(() =>



                    {



                        printStatusForm = new PrintStatusForm();



                        printStatusForm.Show();



                    }));



                    */



                }







                printThread = new Thread(() => ProcessPrintQueue(printerName, printQueue))



                {



                    IsBackground = true,



                    Name = "ProcessPrintThread"



                };



                printThread.Start();



            }



        }















        private void ProcessPrintQueue(string printerName, Queue<BarcodeData> printQueue)



        {



            System.Diagnostics.Debug.WriteLine("=== ProcessPrintQueue 시작 ===");



            System.Diagnostics.Debug.WriteLine($"프린터 이름: {printerName}");



            System.Diagnostics.Debug.WriteLine($"인쇄 큐 크기: {printQueue?.Count ?? -1}");



            System.Diagnostics.Debug.WriteLine($"스레드 ID: {Thread.CurrentThread.ManagedThreadId}");







            IntPtr hPrinter_;



            System.Diagnostics.Debug.WriteLine("OpenPrinter 호출 시작");







            try



            {



                if (!OpenPrinter(printerName, out hPrinter_, IntPtr.Zero))



                {



                    System.Diagnostics.Debug.WriteLine("OpenPrinter 실패");



                    Console.WriteLine("프린터를 열 수 없습니다.");



                    ClosePrintStatusForm();



                    return;



                }







                System.Diagnostics.Debug.WriteLine($"OpenPrinter 성공 - 핸들: {hPrinter_}");



            }



            catch (AccessViolationException avEx)



            {



                System.Diagnostics.Debug.WriteLine($"OpenPrinter에서 AccessViolationException 발생: {avEx.Message}");



                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {avEx.StackTrace}");



                ClosePrintStatusForm();



                return;



            }



            catch (Exception ex)



            {



                System.Diagnostics.Debug.WriteLine($"OpenPrinter에서 예외 발생: {ex.GetType().Name} - {ex.Message}");



                System.Diagnostics.Debug.WriteLine($"스택 트레이스: {ex.StackTrace}");



                ClosePrintStatusForm();



                return;



            }







            try



            {



                System.Diagnostics.Debug.WriteLine("DOCINFO 구조체 생성 시작");



                DOCINFO docInfo = new DOCINFO



                {



                    pDocName = "User Print",



                    pOutputFile = null,



                    pDataType = "RAW"



                };







                System.Diagnostics.Debug.WriteLine("DOCINFO 메모리 할당 시작");



                IntPtr docInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(docInfo));



                Marshal.StructureToPtr(docInfo, docInfoPtr, false);



                System.Diagnostics.Debug.WriteLine($"DOCINFO 메모리 할당 완료 - 포인터: {docInfoPtr}");







                System.Diagnostics.Debug.WriteLine("StartDocPrinter 호출 시작");



                if (!StartDocPrinter(hPrinter_, 1, docInfoPtr))



                {



                    System.Diagnostics.Debug.WriteLine("StartDocPrinter 실패");



                    Console.WriteLine("StartDocPrinter 실패");



                    ClosePrintStatusForm();



                    return;



                }



                System.Diagnostics.Debug.WriteLine("StartDocPrinter 성공");







                int totalCount = printQueue.Count;



                System.Diagnostics.Debug.WriteLine($"총 인쇄 항목 수: {totalCount}");



                int processedCount = 0;



                TransferData = true;







                while (printQueue.Count > 0)



                {



                    BarcodeData row = printQueue.Dequeue();



                    // MainForm에서 설정한 인쇄매수 가져오기 (우선순위), 없으면 설정값 사용



                    int copyCount = (mainForm != null) ? mainForm.GetPrintCopies() : PRINT_INFO_CLASS.PrinterInfo.nPrintCopies;







                    try



                    {



                        // 바코드 이미지 생성 (row 데이터 직접 사용)



                        Bitmap barcodeImage = GenerateBarcodeImageForPrint(row);



                        if (barcodeImage != null)



                        {



                            // 마지막 출력 이미지를 파일로 저장



                            SaveLastPrintImage(barcodeImage, row);







                            byte[] tsplCommandBytes = PrintManager.ConvertBitmapToTSPL_(barcodeImage, copyCount);



                            if (tsplCommandBytes != null)



                            {



                                IntPtr commandPtr = Marshal.AllocHGlobal(tsplCommandBytes.Length);



                                Marshal.Copy(tsplCommandBytes, 0, commandPtr, tsplCommandBytes.Length);







                                int bytesWritten;



                                if (!WritePrinter(hPrinter_, commandPtr, tsplCommandBytes.Length, out bytesWritten))



                                {



                                    Console.WriteLine("WritePrinter 실패");



                                }



                                Marshal.FreeHGlobal(commandPtr);



                            }



                            barcodeImage.Dispose();



                        }



                    }



                    catch (Exception ex)



                    {



                        TransferData = false;



                        Console.WriteLine($"Error: {ex.Message}");



                    }



                    finally



                    {







                    }







                    // ✅ 진행률 업데이트



                    processedCount++;



                    int progressPercentage = (processedCount * 100) / totalCount;







                    // 안전한 폼 상태 체크 후 진행률 업데이트



                    if (printStatusForm != null && !printStatusForm.IsDisposed)



                    {



                        //printStatusForm.UpdateProgress(progressPercentage);



                    }







                    if (progressPercentage == 100)



                    {



                        Thread.Sleep(100); // 0.1초 후 닫기



                        ClosePrintStatusForm();



                    }



                }







                TransferData = false;



                EndDocPrinter(hPrinter_);



                Marshal.FreeHGlobal(docInfoPtr);



            }



            finally



            {



                TransferData = false;



                ClosePrinter(hPrinter_);



                ClosePrintStatusForm();



            }



        }



        private static void ClosePrintStatusForm()



        {



            lock (lockObj)



            {



                if (printStatusForm != null && !printStatusForm.IsDisposed)



                {



                    try



                    {



                        // 폼이 여전히 유효한지 확인 후 닫기



                        if (printStatusForm.InvokeRequired)



                        {



                            printStatusForm.Invoke(new Action(() =>



                            {



                                if (!printStatusForm.IsDisposed)



                                {



                                    printStatusForm.Close();



                                }



                            }));



                        }



                        else



                        {



                            printStatusForm.Close();



                        }



                    }



                    catch (ObjectDisposedException)



                    {



                        // 폼이 이미 해제된 경우 무시



                    }



                    catch (InvalidOperationException)



                    {



                        // 핸들이 생성되지 않았거나 폼이 이미 닫힌 경우 무시



                    }



                    finally



                    {



                        printStatusForm = null;



                    }



                }



            }



        }







        /// <summary>



        /// 실제 인쇄할 이미지 생성



        /// 미리보기랑 차이점 :



        /// 1. 미리보기는 리스트를 받아서 출력



        /// 2. 인쇄는 큐를 받아서 출력



        /// 3. 인쇄시에만 hidden_Offset_X, hidden_Offset_Y를 적용



        /// 4. 인쇄시에는 1bpp로 변환



        /// </summary>



        /// <returns></returns>



        private Bitmap GenerateBarcodeImageForPrint(BarcodeData barcodeData)



        {



            try



            {



                // 프린터 DPI 가져오기



                int dpi = (int)PRINT_INFO_CLASS.PrinterInfo.printer_DPI;



                float mmToPixel = dpi / 25.4f; // mm를 픽셀로 변환







                System.Diagnostics.Debug.WriteLine($"[인쇄 이미지 생성] 프린터 DPI: {dpi}, mmToPixel: {mmToPixel}");







                // 용지 크기 계산



                float paperWidthMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width;



                float paperHeightMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height;







                if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)



                {



                    paperWidthMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height;



                    paperHeightMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width;



                }







                // 용지 크기를 픽셀로 변환



                int paperWidthPixels = (int)(paperWidthMm * mmToPixel);



                int paperHeightPixels = (int)(paperHeightMm * mmToPixel);







                // 다중 컬럼 모드인 경우



                if (barcodeData.IsMultiColumn)



                {



                    return GenerateMultiColumnImageForPrint(barcodeData.MultiColumnData, mmToPixel, paperWidthPixels, paperHeightPixels);



                }







                // 바코드 크기 계산 (BarcodeData에서 가져온 값 사용)



                int barcodeSize = barcodeData.BarcodeSize;







                // QR코드와 바코드에 따른 크기 계산 개선



                int barcodeWidth, barcodeHeight;







                if (barcodeData.BarcodeType == "QR_CODE")



                {



                    // QR 코드 크기를 용지 크기에 비례하게 계산 (정수 기반 정확한 계산)



                    // 엑셀 모드와 동일하게 물리 포인트 단위 크기 계산 (에디터 화면과 100% 동기화)
                    int displaySize = (int)(barcodeSize * (mmToPixel * 25.4f) / 72f);
                    displaySize = Math.Max(displaySize, 10); // 최소 크기 보장

                    barcodeWidth = displaySize;
                    barcodeHeight = displaySize;
                    qrDisplayWidth = displaySize;
                    qrDisplayHeight = displaySize;
                }
                else



                {



                    // 일반 바코드: Width는 크기에 비례, Height는 설정된 BarcodeHeight 사용



                    barcodeWidth = (int)(barcodeSize * mmToPixel * 1.5);



                    barcodeHeight = (int)(barcodeData.BarcodeHeight * mmToPixel);



                }











                // 바코드 생성



                var writer = new ZXing.BarcodeWriterPixelData



                {



                    Format = GetBarcodeFormat(barcodeData.BarcodeType),



                    Options = new ZXing.Common.EncodingOptions



                    {



                        Height = barcodeHeight,



                        Width = barcodeWidth,



                        Margin = barcodeData.BarcodeType == "QR_CODE" ? 0 : 5, // QR 코드는 여백 최소화, 일반 바코드는 기존 유지



                        PureBarcode = true // 바코드만 생성하고 텍스트는 제외



                    }



                };











                var pixelData = writer.Write(barcodeData.BarcodeText);



                using (var originalBarcodeBitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))



                {



                    var bitmapData = originalBarcodeBitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height),



                        System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);



                    try



                    {



                        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);



                    }



                    finally



                    {



                        originalBarcodeBitmap.UnlockBits(bitmapData);



                    }







                    // QR 코드인 경우 비트맵으로 확대/축소 처리



                    Bitmap barcodeBitmap = originalBarcodeBitmap;



                    if (barcodeData.BarcodeType == "QR_CODE" && qrDisplayWidth > 0 && qrDisplayHeight > 0)



                    {



                        // 원본 QR 코드를 표시 크기로 확대/축소 (픽셀 단위 정확한 확대)



                        var scaledBitmap = new System.Drawing.Bitmap(qrDisplayWidth, qrDisplayHeight);



                        using (var g = System.Drawing.Graphics.FromImage(scaledBitmap))



                        {



                            // QR 코드는 픽셀 단위 정확한 확대를 위해 NearestNeighbor 사용



                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;



                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;



                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;



                            g.DrawImage(originalBarcodeBitmap, 0, 0, qrDisplayWidth, qrDisplayHeight);



                        }



                        barcodeBitmap = scaledBitmap; // 확대/축소된 비트맵 사용







                        // 표시 크기를 실제 크기로 업데이트



                        barcodeWidth = qrDisplayWidth;



                        barcodeHeight = qrDisplayHeight;







                    }
                    else if (barcodeWidth > 0 && barcodeHeight > 0)
                    {
                        var scaledBitmap = new System.Drawing.Bitmap(barcodeWidth, barcodeHeight);
                        using (var g = System.Drawing.Graphics.FromImage(scaledBitmap))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                            g.DrawImage(originalBarcodeBitmap, 0, 0, barcodeWidth, barcodeHeight);
                        }
                        if (barcodeBitmap != originalBarcodeBitmap)
                        {
                            barcodeBitmap.Dispose();
                        }
                        barcodeBitmap = scaledBitmap;
                    }







                    // 바코드의 실제 크기 측정 (생성된 비트맵 기준)



                    int measuredBarcodeWidth = barcodeBitmap.Width;



                    int measuredBarcodeHeight = barcodeBitmap.Height;







                    // 실제 크기와 계산된 크기가 다른 경우 실제 크기로 업데이트



                    if (measuredBarcodeWidth != barcodeWidth || measuredBarcodeHeight != barcodeHeight)



                    {



                        barcodeWidth = measuredBarcodeWidth;



                        barcodeHeight = measuredBarcodeHeight;



                    }







                    // 전체 용지 이미지 생성



                    Bitmap fullPageBitmap = new Bitmap(paperWidthPixels, paperHeightPixels);



                    using (Graphics g = Graphics.FromImage(fullPageBitmap))



                    {



                        g.Clear(Color.White);







                        // 상대좌표 계산을 위한 변수들



                        float currentY = 0; // 현재 Y 위치 (상대좌표 누적)







                        // 보내는 분/받는 분 라벨 그리기 - "표시없음"이 아닌 경우만



                        // 이름 필드 위치와 가까운 곳에 배치



                        if (mainForm != null)



                        {



                            string senderReceiverLabel = mainForm.GetSenderReceiverLabel();



                            if (senderReceiverLabel != "표시없음")



                            {



                                // Title 크기와 위치를 appSettings에서 가져오기



                                int titleSize = mainForm.GetTitleSize(); // mm 단위



                                int titleOffsetY = mainForm.GetTitleOffsetY(); // mm 단위 (최상단 기준)







                                // Title 폰트 크기 계산 (미리보기와 동일)



                                float textSizeInPixels = titleSize * mmToPixel;



                                float labelFontSize = Math.Max(8, textSizeInPixels * 0.6f); // 최소 8pt (미리보기와 동일한 계수 0.6f)







                                // Title Y 위치 계산 (최상단 기준 절대좌표 - 다른 컬럼과 동일, 미리보기와 동일)



                                float titleTopY = titleOffsetY * mmToPixel; // 용지 전체 영역 기준 절대 좌표







                                // 폰트 높이 계산 (미리보기와 동일)



                                float actualLabelHeight = labelFontSize * mmToPixel * 25.4f / 72f; // pt를 픽셀로 변환







                                // Title 하단 위치 계산 (다음 요소 위치 계산용)



                                currentY = titleTopY + actualLabelHeight;







                                System.Diagnostics.Debug.WriteLine($"[Title 위치-단일바코드인쇄] titleSize={titleSize}mm, titleOffsetY={titleOffsetY}mm, titleTopY={titleTopY}px, currentY={currentY}px");







                                using (Font labelFont = new Font("맑은 고딕", labelFontSize, FontStyle.Bold))



                                using (SolidBrush labelBrush = new SolidBrush(Color.Black))



                                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, FormatFlags = StringFormatFlags.LineLimit, Trimming = StringTrimming.Word })



                                {



                                    // 타이틀 텍스트 자동 줄바꿈 적용 (미리보기와 동일)



                                    float maxTitleWidth = paperWidthPixels; // paperWidthPixels는 labelWidthPx와 동일한 값



                                    float maxTitleHeight = actualLabelHeight * 3; // 최대 3줄



                                    RectangleF titleRect = new RectangleF(0, titleTopY, maxTitleWidth, maxTitleHeight);



                                    g.DrawString(senderReceiverLabel, labelFont, labelBrush, titleRect, sf);



                                }



                            }



                            else



                            {



                                // 타이틀이 없으면 최상단부터 시작



                                currentY = 10; // 최상단 여백



                            }



                        }



                        else



                        {



                            // mainForm이 없으면 최상단부터 시작



                            currentY = 10; // 최상단 여백



                        }







                        // X축: 센터 정렬 + 사용자 설정 OffsetX, Y축: Title 아래 상대좌표 + 사용자 설정 OffsetY



                        int paperCenterX = paperWidthPixels / 2;



                        int centerX = paperCenterX - (barcodeWidth / 2) + (int)(barcodeData.BarcodeX * mmToPixel);



                        // Y축: 상대좌표 사용 (Title 아래) + 사용자 설정 OffsetY



                        int barcodeY = (int)currentY + (int)(20 * mmToPixel) + (int)(barcodeData.BarcodeY * mmToPixel);







                        // 바코드 하단 텍스트를 배려한 Y축 클램핑



                        int maxBarcodeY = paperHeightPixels - barcodeHeight;



                        if (barcodeData.ShowBarcodeText)



                        {



                            float fontSizeInPixels = Math.Max(8f, barcodeData.BarcodeTextSize * dpi / 72f);



                            using (Font font = new Font("Arial", fontSizeInPixels, FontStyle.Bold, GraphicsUnit.Pixel))



                            {



                                int textHeight = (int)(font.GetHeight(g) + 5);



                                maxBarcodeY -= textHeight;



                            }



                        }



                        int centerY = Math.Max(0, Math.Min(maxBarcodeY, barcodeY));







                        // 바코드 그리기 (해상도 차이로 인한 크기 변형 방지를 위해 명시적 크기 지정)



                        g.DrawImage(barcodeBitmap, centerX, centerY, barcodeWidth, barcodeHeight);







                        // 바코드 하단 텍스트 그리기



                        if (barcodeData.ShowBarcodeText && !string.IsNullOrEmpty(barcodeData.BarcodeText))



                        {



                            float fontSizeInPixels = Math.Max(8f, barcodeData.BarcodeTextSize * dpi / 72f);



                            using (Font font = new Font("Arial", fontSizeInPixels, FontStyle.Bold, GraphicsUnit.Pixel))



                            using (SolidBrush brush = new SolidBrush(Color.Black))



                            {



                                float textY = centerY + barcodeHeight + 5;



                                SizeF textSize = g.MeasureString(barcodeData.BarcodeText, font);



                                float textX = centerX + (barcodeWidth / 2.0f) - (textSize.Width / 2.0f);







                                g.DrawString(barcodeData.BarcodeText, font, brush, textX, textY);



                            }



                        }



                    }











                    if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)



                        fullPageBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);







                    return fullPageBitmap;



                }



            }



            catch (Exception ex)



            {



                System.Diagnostics.Debug.WriteLine($"바코드 이미지 생성 실패: {ex.Message}");



                return null;



            }



        }







        private Bitmap GenerateMultiColumnImageForPrint(List<BarcodePrint.View.MultiColumnData> multiColumnData, float mmToPixel, int labelWidthPx, int labelHeightPx)



        {



            try



            {



                // 프린터 DPI 가져오기 (로깅용)



                float printerDpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI;



                float paperWidthMm = labelWidthPx / mmToPixel;



                float paperHeightMm = labelHeightPx / mmToPixel;







                System.Diagnostics.Debug.WriteLine($"========================================");



                System.Diagnostics.Debug.WriteLine($"[인쇄 이미지 생성 시작]");



                System.Diagnostics.Debug.WriteLine($"[인쇄 DPI 정보] 프린터DPI={printerDpi}, 용지크기={paperWidthMm:F2}x{paperHeightMm:F2}mm, 픽셀={labelWidthPx}x{labelHeightPx}px, mmToPixel={mmToPixel}");



                System.Diagnostics.Debug.WriteLine($"[인쇄 이미지 크기] {labelWidthPx}x{labelHeightPx}px, 비율={labelWidthPx / (float)labelHeightPx:F2}");



                System.Diagnostics.Debug.WriteLine($"[인쇄 입력 데이터] 컬럼 수={multiColumnData.Count}");



                foreach (var data in multiColumnData)



                {



                    System.Diagnostics.Debug.WriteLine($"  - ColumnName='{data.ColumnName}', Value='{data.Value}', IsBarcode={data.IsBarcode}, Size={data.Size}, OffsetX={data.OffsetX}mm, OffsetY={data.OffsetY}mm, ShowBarcodeText={data.ShowBarcodeText}, BarcodeTextSize={data.BarcodeTextSize}");



                }



                System.Diagnostics.Debug.WriteLine($"========================================");







                Bitmap finalBitmap = new Bitmap(labelWidthPx, labelHeightPx);







                using (Graphics g = Graphics.FromImage(finalBitmap))



                {



                    g.Clear(Color.White);



                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;



                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;







                    // 다중 컬럼 인쇄에서는 타이틀(공통문구)을 별도로 고정 출력하지 않고,



                    // 컬럼 목록에 포함된 공통문구 데이터를 설정된 좌표에 맞추어 출력합니다.







                    // 컬럼 위치 설정을 순서대로 가져오기 (가격표용: 품명→가격→바코드)



                    var columnPositionSettings = mainForm?.GetColumnPositionSettings();



                    var orderedSettings = columnPositionSettings?.OrderBy(s => s.ColumnIndex).ToList() ?? new List<ColumnPositionSetting>();







                    System.Diagnostics.Debug.WriteLine($"[인쇄 컬럼 렌더링] multiColumnData.Count={multiColumnData.Count}, orderedSettings.Count={orderedSettings.Count}");







                    // orderedSettings가 비어있으면 multiColumnData를 직접 순회 (미리보기와 동일)



                    if (orderedSettings.Count == 0)



                    {



                        System.Diagnostics.Debug.WriteLine($"[인쇄 컬럼 렌더링] orderedSettings가 비어있음. multiColumnData를 직접 순회합니다. (총 {multiColumnData.Count}개)");



                        foreach (var columnData in multiColumnData)



                        {



                            // 절대좌표로 Y 위치 계산 (용지 최상단 기준 OffsetY)



                            float columnY = columnData.OffsetY * mmToPixel;







                            System.Diagnostics.Debug.WriteLine($"========================================");



                            System.Diagnostics.Debug.WriteLine($"[인쇄 컬럼 렌더링 시작] ColumnName='{columnData.ColumnName}'");



                            System.Diagnostics.Debug.WriteLine($"[인쇄 컬럼 위치] OffsetY={columnData.OffsetY}mm → currentY={columnY}px, OffsetX={columnData.OffsetX}mm");



                            System.Diagnostics.Debug.WriteLine($"[인쇄 컬럼 데이터] 값='{columnData.Value}', IsBarcode={columnData.IsBarcode}, Size={columnData.Size}mm, ShowBarcodeText={columnData.ShowBarcodeText}");







                            RenderColumnData(g, columnData, columnY);



                        }



                    }



                    else



                    {



                        // orderedSettings 순서대로 렌더링 (가격표용: 타이틀→품명→가격→바코드)



                        foreach (var setting in orderedSettings)



                        {



                            var columnData = multiColumnData.FirstOrDefault(d =>



                                (setting.ColumnIndex == 0 && (d.ColumnName == "컬럼1" || d.ColumnName == "품명" || d.ColumnName.Contains("품명"))) ||



                                (setting.ColumnIndex == 1 && (d.ColumnName == "컬럼2" || d.ColumnName == "가격" || d.ColumnName.Contains("가격"))) ||



                                (setting.ColumnIndex == 2 && (d.ColumnName == "컬럼3" || d.ColumnName == "옵션" || d.ColumnName.Contains("옵션") || d.ColumnName == "비고" || d.ColumnName.Contains("비고"))) ||



                                (setting.ColumnIndex == 3 && (d.ColumnName == "컬럼4" || d.ColumnName == "바코드" || d.ColumnName.Contains("바코드"))) ||



                                (setting.ColumnIndex == 4 && (d.ColumnName == "공통문구" || d.ColumnName.Contains("공통문구"))));







                            if (columnData == null) continue;







                            float columnY = columnData.OffsetY * mmToPixel;







                            System.Diagnostics.Debug.WriteLine($"========================================");



                            System.Diagnostics.Debug.WriteLine($"[인쇄 컬럼 렌더링 시작] ColumnIndex={setting.ColumnIndex}, ColumnName='{columnData.ColumnName}'");



                            System.Diagnostics.Debug.WriteLine($"[인쇄 컬럼 위치] OffsetY={columnData.OffsetY}mm → currentY={columnY}px, OffsetX={columnData.OffsetX}mm");







                            RenderColumnData(g, columnData, columnY);



                        }



                    }



                }







                // 로컬 함수 정의



                void RenderColumnData(Graphics graphics, BarcodePrint.View.MultiColumnData columnData, float yPos)



                {



                    if (columnData.IsBarcode)



                        RenderBarcodeColumnInPrint(graphics, columnData, yPos);



                    else



                        RenderTextColumnInPrint(graphics, columnData, yPos);



                }







                void RenderBarcodeColumnInPrint(Graphics graphics, BarcodePrint.View.MultiColumnData columnData, float yPos)



                {



                    int barcodeSize = columnData.Size;



                    int barcodeWidth, barcodeHeight;







                    if (columnData.BarcodeType == "QR_CODE")



                    {



                        barcodeWidth = (int)(barcodeSize * (mmToPixel * 25.4f) / 72f);
                        barcodeHeight = barcodeWidth;



                    }



                    else



                    {



                        barcodeWidth = columnData.CustomWidth > 0 



                            ? (int)(columnData.CustomWidth * mmToPixel) 



                            : (int)(barcodeSize * mmToPixel * 1.5);



                        barcodeHeight = (int)(columnData.BarcodeHeight * mmToPixel);



                    }







                    var barcodeWriter = new ZXing.BarcodeWriter



                    {



                        Format = GetBarcodeFormat(columnData.BarcodeType),



                        Options = new ZXing.Common.EncodingOptions



                        {



                            Width = barcodeWidth,



                            Height = barcodeHeight,



                            Margin = columnData.BarcodeType == "QR_CODE" ? 0 : 5,



                            PureBarcode = true



                        }



                    };







                    try



                    {



                        string processedBarcodeText = ProcessBarcodeText(columnData.Value, columnData.BarcodeType);



                        var barcodeBitmap = barcodeWriter.Write(processedBarcodeText);

                        if (barcodeBitmap != null && barcodeWidth > 0 && barcodeHeight > 0)

                        {

                            var scaledBitmap = new System.Drawing.Bitmap(barcodeWidth, barcodeHeight);

                            using (var g = System.Drawing.Graphics.FromImage(scaledBitmap))

                            {

                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

                                g.DrawImage(barcodeBitmap, 0, 0, barcodeWidth, barcodeHeight);

                            }

                            barcodeBitmap.Dispose();

                            barcodeBitmap = scaledBitmap;

                        }







                        if (barcodeBitmap == null) return;







                        int labelCenterX = labelWidthPx / 2;



                        int barcodeX = labelCenterX - (barcodeBitmap.Width / 2) + (int)(columnData.OffsetX * mmToPixel);



                        int barcodeY = (int)yPos;







                        int hiddenOffsetXPixels = (int)(PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X * mmToPixel);



                        int hiddenOffsetYPixels = (int)(PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y * mmToPixel);







                        if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)



                        {



                            int temp = hiddenOffsetXPixels;



                            hiddenOffsetXPixels = hiddenOffsetYPixels;



                            hiddenOffsetYPixels = temp;



                        }







                        barcodeX += hiddenOffsetXPixels;



                        barcodeY += hiddenOffsetYPixels;







                        int finalBarcodeX = Math.Max(0, barcodeX);



                        // 바코드 하단 텍스트를 배려한 Y축 클램핑 (가장 중요: 인쇄 시 누락 방지)



                        int maxBarcodeY = labelHeightPx - barcodeBitmap.Height;



                        if (columnData.ShowBarcodeText)
                        {
                            float fontSizeInPixels = Math.Max(8f, columnData.BarcodeTextSize * printerDpi / 72f);
                            using (Font font = new Font("Arial", fontSizeInPixels, FontStyle.Bold, GraphicsUnit.Pixel))
                            {
                                int textHeight = (int)(font.GetHeight(graphics) + 5);
                                maxBarcodeY -= textHeight;
                            }
                        }



                        int finalBarcodeY = Math.Max(0, Math.Min(maxBarcodeY, barcodeY));







                        graphics.DrawImage(barcodeBitmap, finalBarcodeX, finalBarcodeY, barcodeBitmap.Width, barcodeBitmap.Height);







                        if (columnData.ShowBarcodeText && !string.IsNullOrEmpty(columnData.Value))
                        {
                            float fontSizeInPixels = Math.Max(8f, columnData.BarcodeTextSize * printerDpi / 72f);
                            using (Font font = new Font("Arial", fontSizeInPixels, FontStyle.Bold, GraphicsUnit.Pixel))
                            using (SolidBrush brush = new SolidBrush(Color.Black))
                            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, Trimming = StringTrimming.Word })
                            {
                                float textY = finalBarcodeY + barcodeBitmap.Height + 5 + (columnData.BarcodeTextOffsetY * mmToPixel);
                                SizeF textSize = graphics.MeasureString(columnData.Value, font);
                                float textWidth = Math.Max(barcodeBitmap.Width, textSize.Width + 10);
                                float textX = finalBarcodeX + (barcodeBitmap.Width / 2.0f) + (columnData.BarcodeTextOffsetX * mmToPixel);
                                RectangleF textRect = new RectangleF(textX - textWidth / 2.0f, textY, textWidth, fontSizeInPixels * 5);

                                graphics.DrawString(columnData.Value, font, brush, textRect, sf);
                            }
                        }



                        barcodeBitmap.Dispose();



                    }



                    catch (Exception ex)



                    {



                        System.Diagnostics.Debug.WriteLine($"[인쇄 바코드 생성 오류] {ex.Message}");



                    }



                }







                void RenderTextColumnInPrint(Graphics graphics, BarcodePrint.View.MultiColumnData columnData, float yPos)



                {



                    // 텍스트 크기 계산 (에디터와 100% 동일하게 pt -> pixel 변환)



                    float fontSizeInPixels = Math.Max(8f, columnData.Size * printerDpi / 72f);







                    StringAlignment alignVal = StringAlignment.Near;



                    if (columnData.Alignment == "Center") alignVal = StringAlignment.Center;



                    else if (columnData.Alignment == "Right") alignVal = StringAlignment.Far;







                    Font font = null;
                    string familyName = string.IsNullOrEmpty(columnData.FontFamily) ? "맑은 고딕" : columnData.FontFamily;
                    FontStyle style = columnData.IsBold ? FontStyle.Bold : FontStyle.Regular;
                    try
                    {
                        var family = new FontFamily(familyName);
                        if (!family.IsStyleAvailable(style))
                        {
                            if (family.IsStyleAvailable(FontStyle.Regular)) style = FontStyle.Regular;
                            else if (family.IsStyleAvailable(FontStyle.Bold)) style = FontStyle.Bold;
                            else style = FontStyle.Regular;
                        }
                        font = new Font(family, fontSizeInPixels, style, GraphicsUnit.Pixel);
                    }
                    catch
                    {
                        font = new Font("맑은 고딕", fontSizeInPixels, style, GraphicsUnit.Pixel);
                    }
                    using (font)



                    using (SolidBrush brush = new SolidBrush(Color.Black))



                    using (StringFormat textSf = new StringFormat { Alignment = alignVal, LineAlignment = StringAlignment.Near, FormatFlags = 0, Trimming = StringTrimming.Word })



                    {



                        float textY = yPos;



                        int hiddenOffsetXPixels = (int)(PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X * mmToPixel);



                        int hiddenOffsetYPixels = (int)(PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y * mmToPixel);







                        if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)



                        {



                            int temp = hiddenOffsetXPixels;



                            hiddenOffsetXPixels = hiddenOffsetYPixels;



                            hiddenOffsetYPixels = temp;



                        }







                        // 텍스트 영역 및 줄바꿈 폭 계산



                        SizeF size = graphics.MeasureString(columnData.Value, font);



                        float wrapWidth = columnData.CustomWidth > 0 ? columnData.CustomWidth * mmToPixel : size.Width;



                        if (columnData.CustomWidth > 0)



                        {



                            size = graphics.MeasureString(columnData.Value, font, (int)wrapWidth, textSf);



                        }







                        float textHeight = Math.Max(size.Height, 20);



                        



                        // 위치 기준을 왼쪽 상단으로 조절



                        // 위치 기준을 센터로 조절 (에디터와 100% 동일화)
                        float pxX = (labelWidthPx / 2f + columnData.OffsetX * mmToPixel) + hiddenOffsetXPixels;
                        float pxY = (columnData.OffsetY * mmToPixel) + hiddenOffsetYPixels;
                        float centerY = pxY + textHeight / 2f;
                        RectangleF rect = new RectangleF(-wrapWidth / 2f, -textHeight / 2f, wrapWidth, textHeight);

                        var state = graphics.Save();
                        graphics.TranslateTransform(pxX, centerY);
                        graphics.RotateTransform(columnData.RotateAngle);
                        graphics.DrawString(columnData.Value, font, brush, rect, textSf);
                        graphics.Restore(state);



                    }



                }







                // 최종 반환 전 회전 처리



                if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)



                    finalBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);







                System.Diagnostics.Debug.WriteLine($"[인쇄 이미지 생성 완료] 최종 크기={finalBitmap.Width}x{finalBitmap.Height}px");



                return finalBitmap;



            }



            catch (Exception ex)



            {



                System.Diagnostics.Debug.WriteLine($"다중 컬럼 인쇄 이미지 생성 오류: {ex.Message}");



                return new Bitmap(100, 100);



            }



        }











        /// <summary>



        /// 바코드 텍스트 처리 (한글 문자 처리)



        /// </summary>



        private string ProcessBarcodeText(string data, string barcodeType)



        {



            try



            {



                if (string.IsNullOrEmpty(data))



                    return data;







                // CODE_128의 경우 영문자와 숫자만 지원



                if (barcodeType == "CODE_128")



                {



                    var allowedChars = new List<char>();



                    foreach (char c in data)



                    {



                        if ((c >= 'A' && c <= 'Z') ||



                            (c >= 'a' && c <= 'z') ||



                            (c >= '0' && c <= '9') ||



                            c == '-' || c == '_')



                        {



                            allowedChars.Add(c);



                        }



                    }







                    string processedData = new string(allowedChars.ToArray()).Trim();







                    if (string.IsNullOrEmpty(processedData))



                    {



                        processedData = "BARCODE";



                    }







                    return processedData;



                }







                // QR_CODE의 경우 모든 문자 지원



                if (barcodeType == "QR_CODE")



                {



                    return data;



                }







                return data;



            }



            catch (Exception ex)



            {



                System.Diagnostics.Debug.WriteLine($"바코드 텍스트 처리 오류: {ex.Message}");



                return "BARCODE";



            }



        }







        private BarcodeFormat GetBarcodeFormat(string barcodeType)



        {



            switch (barcodeType)



            {



                // 1D 바코드 (선형 바코드)



                case "CODE_128":



                case "CODE_128_A":



                case "CODE_128_B":



                case "CODE_128_C":



                    return BarcodeFormat.CODE_128;



                case "CODE_39":



                    return BarcodeFormat.CODE_39;



                case "CODE_93":



                    return BarcodeFormat.CODE_93;



                case "EAN_13":



                    return BarcodeFormat.EAN_13;



                case "EAN_8":



                    return BarcodeFormat.EAN_8;



                case "UPC_A":



                    return BarcodeFormat.UPC_A;



                case "UPC_E":



                    return BarcodeFormat.UPC_E;



                case "CODABAR":



                    return BarcodeFormat.CODABAR;



                case "ITF":



                    return BarcodeFormat.ITF;



                case "RSS_14":



                    return BarcodeFormat.RSS_14;



                case "RSS_EXPANDED":



                    return BarcodeFormat.RSS_EXPANDED;







                // 2D 바코드 (매트릭스 바코드)



                case "QR_CODE":



                    return BarcodeFormat.QR_CODE;



                case "DATA_MATRIX":



                    return BarcodeFormat.DATA_MATRIX;



                case "PDF_417":



                    return BarcodeFormat.PDF_417;



                case "AZTEC":



                    return BarcodeFormat.AZTEC;



                case "MAXICODE":



                    return BarcodeFormat.MAXICODE;







                default:



                    return BarcodeFormat.CODE_128; // 기본값을 CODE_128로 변경



            }



        }







        private GraphicsPath CreateRoundedRectanglePath(int x, int y, int width, int height, int cornerRadius)



        {



            GraphicsPath path = new GraphicsPath();







            // 모서리가 둥근 사각형 경로를 생성



            path.AddArc(x, y, cornerRadius, cornerRadius, 180, 90); // 좌상단



            path.AddArc(x + width - cornerRadius, y, cornerRadius, cornerRadius, 270, 90); // 우상단



            path.AddArc(x + width - cornerRadius, y + height - cornerRadius, cornerRadius, cornerRadius, 0, 90); // 우하단



            path.AddArc(x, y + height - cornerRadius, cornerRadius, cornerRadius, 90, 90); // 좌하단



            path.CloseFigure(); // 경로 닫기







            return path;



        }







        /// <summary>



        /// 마지막 출력 이미지를 파일로 저장하는 메서드



        /// </summary>



        /// <param name="barcodeImage">저장할 바코드 이미지</param>



        /// <param name="barcodeData">바코드 데이터 정보</param>



        private void SaveLastPrintImage(Bitmap barcodeImage, BarcodeData barcodeData)



        {



            try



            {



                // 저장할 폴더 경로 설정



                string saveFolderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image");







                // 폴더가 존재하지 않으면 생성



                if (!Directory.Exists(saveFolderPath))



                {



                    Directory.CreateDirectory(saveFolderPath);



                }







                // 파일명 생성 (타임스탬프 + 바코드 값)



                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");



                string barcodeValue = string.IsNullOrEmpty(barcodeData.BarcodeText) ? "Unknown" :



                    barcodeData.BarcodeText.Replace("\\", "_").Replace("/", "_").Replace(":", "_").Replace("*", "_")



                    .Replace("?", "_").Replace("\"", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_");







                // 파일명 길이 제한 (최대 50자)



                if (barcodeValue.Length > 50)



                {



                    barcodeValue = barcodeValue.Substring(0, 50);



                }







                string fileName = $"preview_{timestamp}_{barcodeValue}.png";



                string filePath = System.IO.Path.Combine(saveFolderPath, fileName);







                // PNG 형식으로 저장 (고품질)



                barcodeImage.Save(filePath, ImageFormat.Png);







                // 디버그 메시지



                System.Diagnostics.Debug.WriteLine($"마지막 출력 이미지 저장 완료: {filePath}");







                // 기존 파일들 정리 (최대 10개까지만 유지)



                CleanupOldPrintImages(saveFolderPath);



            }



            catch (Exception ex)



            {



                System.Diagnostics.Debug.WriteLine($"마지막 출력 이미지 저장 실패: {ex.Message}");



            }



        }







        /// <summary>



        /// 오래된 출력 이미지 파일들을 정리하는 메서드 (최대 10개까지만 유지)



        /// </summary>



        /// <param name="folderPath">이미지가 저장된 폴더 경로</param>



        private void CleanupOldPrintImages(string folderPath)



        {



            try



            {



                var imageFiles = Directory.GetFiles(folderPath, "LastImage_*.png")



                    .Select(f => new FileInfo(f))



                    .OrderByDescending(f => f.CreationTime)



                    .ToList();







                // 10개를 초과하는 파일들 삭제



                if (imageFiles.Count > 10)



                {



                    var filesToDelete = imageFiles.Skip(10);



                    foreach (var file in filesToDelete)



                    {



                        try



                        {



                            file.Delete();



                            System.Diagnostics.Debug.WriteLine($"오래된 출력 이미지 삭제: {file.Name}");



                        }



                        catch (Exception ex)



                        {



                            System.Diagnostics.Debug.WriteLine($"파일 삭제 실패: {file.Name}, 오류: {ex.Message}");



                        }



                    }



                }



            }



            catch (Exception ex)



            {



                System.Diagnostics.Debug.WriteLine($"이미지 파일 정리 실패: {ex.Message}");



            }



        }











        public void ClearPrintQueue(string printerName)



        {



            IntPtr hPrinter;



            if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))



            {



                try



                {



                    if (!SetPrinter(hPrinter, 0, IntPtr.Zero, PRINTER_CONTROL_PURGE))



                    {



                        //MessageBox.Show("프린터 대기열을 비우는 중 오류가 발생했습니다.");



                    }



                    else



                    {



                        //MessageBox.Show("프린터 대기열이 성공적으로 비워졌습니다.");



                    }



                }



                finally



                {



                    ClosePrinter(hPrinter);



                }



            }



            else



            {



                //MessageBox.Show("프린터를 열 수 없습니다.");



            }



        }







        public void SendPrintCommand(string printerName, string command)



        {



            if (string.IsNullOrEmpty(printerName))



            {



                Console.WriteLine("프린터 이름이 비어있습니다.");



                return;



            }







            IntPtr hPrinter_;



            if (!OpenPrinter(printerName, out hPrinter_, IntPtr.Zero))



            {



                Console.WriteLine($"프린터 '{printerName}'를 열 수 없습니다.");



                return;



            }







            try



            {



                DOCINFO docInfo = new DOCINFO



                {



                    pDocName = "Calibration Command",



                    pOutputFile = null,



                    pDataType = "RAW"



                };







                IntPtr docInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(docInfo));



                Marshal.StructureToPtr(docInfo, docInfoPtr, false);







                if (!StartDocPrinter(hPrinter_, 1, docInfoPtr))



                {



                    Console.WriteLine("StartDocPrinter 실패");



                    return;



                }







                try



                {



                    byte[] tsplCommandBytes = null;



                    string commandType = "";







                    if (command.Contains("CALIBRATION"))



                    {



                        tsplCommandBytes = PrintManager.Command_CALIBRATION();



                        commandType = "CALIBRATION";



                    }



                    else if (command.Contains("HOME"))



                    {



                        tsplCommandBytes = PrintManager.Command_HOME();



                        commandType = "HOME";



                    }



                    else if (command.Contains("FEED"))
                    {
                        tsplCommandBytes = PrintManager.Command_FEED();
                        commandType = "FEED";
                    }
                    else if (command.Contains("INIT"))



                    {



                        tsplCommandBytes = PrintManager.Command_INITIALPRINT();



                        commandType = "INITIALPRINT";



                    }







                    if (tsplCommandBytes != null)



                    {



                        Console.WriteLine($"명령 전송: {commandType}");



                        Console.WriteLine($"명령 데이터: {BitConverter.ToString(tsplCommandBytes)}");







                        IntPtr commandPtr = Marshal.AllocHGlobal(tsplCommandBytes.Length);



                        Marshal.Copy(tsplCommandBytes, 0, commandPtr, tsplCommandBytes.Length);







                        int bytesWritten;



                        if (!WritePrinter(hPrinter_, commandPtr, tsplCommandBytes.Length, out bytesWritten))



                        {



                            Console.WriteLine($"WritePrinter 실패: {Marshal.GetLastWin32Error()}");



                        }



                        else



                        {



                            Console.WriteLine($"명령 전송 성공: {bytesWritten} 바이트 전송됨");



                        }



                        Marshal.FreeHGlobal(commandPtr);



                    }



                    else



                    {



                        Console.WriteLine($"알 수 없는 명령: {command}");



                    }



                }



                finally



                {



                    EndDocPrinter(hPrinter_);



                    Marshal.FreeHGlobal(docInfoPtr);



                }



            }



            finally



            {



                ClosePrinter(hPrinter_);



            }



        }















    }



}