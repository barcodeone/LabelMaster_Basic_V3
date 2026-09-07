using BarcodePrint.Model;







using BarcodePrint.View;







using System;







using System.Collections.Generic;







using System.Drawing;







using System.Drawing.Drawing2D;







using System.IO;







using System.Linq;







using System.Runtime.InteropServices;







using System.Text;







using System.Threading;







using System.Threading.Tasks;







using ZXing;







using static BarcodePrint.Model.PrinterHelper;







using static System.Windows.Forms.AxHost;















namespace BarcodePrint.Controll







{















    internal class GetPrintState { }















    // 프린터 상태 및 미리보기 관리







    public partial class PreviewManager







    {







        MainForm mainForm;







        BarcodePrintManager barcodePrintManager;







        public EventHandler<string> PrintStateUpdateEvent;







        public event EventHandler PaintPreviewEvent; // 메인뷰로 전달할 이벤트







        public static IntPtr hPrinter;







        public static int openStatus = 100;







        private static string lastPrinterName = ""; // 마지막 프린터 이름 추적







        private CancellationTokenSource cancellationTokenSource; // 스레드 취소 토큰







        private static string lastPrinterPort = ""; // 마지막 프린터 포트 추적















        // QR 코드 비트맵 확대/축소용 변수















        private int qrDisplayWidth = 0;







        private int qrDisplayHeight = 0;























        public PreviewManager(MainForm main)







        {















            mainForm = main;







            barcodePrintManager = new BarcodePrintManager();







        }























        public PreviewManager() { }















        private bool _shouldStop = false;















        public void OnStateCheck()







        {







            _shouldStop = false;







            try







            {







                hPrinter = TSPLPrint.InitPrinter("");







                // 포트 오픈 (USB,USB031) 







                openStatus = TSPLPrint.OpenPort(hPrinter, "USB," + PRINT_INFO_CLASS.PrinterInfo.printerPortName);







                int ret = 0;







                while (!_shouldStop)







                {







                    try







                    {







                        if (PRINT_INFO_CLASS.PrinterInfo.printerName == "")







                        {







                            return;







                        }







                        if (PRINT_INFO_CLASS.PrinterInfo.printerName.Contains(PRINT_INFO_CLASS.PrinterInfo.TargetPrint))







                        {







                            if (openStatus != 0 || ret != 0)







                            {







                                openStatus = TSPLPrint.OpenPort(hPrinter, "USB," + PRINT_INFO_CLASS.PrinterInfo.printerPortName);







                                Thread.Sleep(500);







                            }















                            PrintStateUpdateEvent?.Invoke(this, USB_status.getTSPLPrintState(hPrinter, openStatus, out ret));







                        }















                        Thread.Sleep(1000);







                    }







                    catch (ThreadInterruptedException)







                    {







                        break;







                    }







                    catch (Exception ex)







                    {







                        System.Diagnostics.Debug.WriteLine($"OnStateCheck 루프 오류: {ex.Message}");







                        Thread.Sleep(2000);







                    }







                }







            }







            catch (Exception ex)







            {







                System.Diagnostics.Debug.WriteLine($"OnStateCheck 초기화 오류: {ex.Message}");







            }







            finally







            {







                if (hPrinter != IntPtr.Zero)







                {







                    TSPLPrint.ClosePort(hPrinter);







                    hPrinter = IntPtr.Zero;







                    System.Diagnostics.Debug.WriteLine("OnStateCheck: 프린터 리소스 해제 완료");







                }







            }







        }















        /// <summary>







        /// 스레드를 안전하게 종료하기 위한 메서드







        /// </summary>







        public void RequestStop()







        {







            try







            {







                cancellationTokenSource?.Cancel();







                System.Diagnostics.Debug.WriteLine("스레드 종료 요청 완료");







            }







            catch (Exception ex)







            {







                System.Diagnostics.Debug.WriteLine($"스레드 종료 요청 중 오류: {ex.Message}");







            }







        }















        /// ////////////////////////////////////////////////







        /// 미리보기 이벤트 수신







        public void PreviewEventSet()







        {







            mainForm.previewContentEvent += receiveContent;







        }















        public void PreviewEventClose()







        {







            mainForm.previewContentEvent -= receiveContent;







        }















        private Thread previewThread; // 현재 실행 중인 미리보기 스레드







        private readonly object previewLock = new object(); // 스레드 생성 잠금























        public void receiveContent(object sender, EventArgs e)







        {















            lock (previewLock)







            {







                // 이미 실행 중인 스레드가 있으면 새로 생성하지 않음







                if (previewThread != null && previewThread.IsAlive)







                {







                    System.Diagnostics.Debug.WriteLine("이미 미리보기 스레드가 실행 중이므로 건너뜀");







                    return;







                }















                // Task.Run 대신 Thread 방식 사용 (X-QuickLabel_최종 방식)







                // Task.Run은 스레드 풀을 사용하여 메모리 충돌 가능성이 있음







                // Thread는 독립된 스레드를 생성하여 프린터 드라이버와의 충돌 방지















                previewThread = new Thread(new ParameterizedThreadStart(PreviewWork));







                previewThread.IsBackground = true; // 백그라운드 스레드로 설정







                previewThread.Start();























                System.Diagnostics.Debug.WriteLine($"미리보기 스레드 생성됨 - 스레드 ID: {previewThread.ManagedThreadId}");







            }







        }















        private void PreviewWork(object obj)







        {







            try







            {







                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))







                {







                    // 미리보기도 프린터 DPI 사용 (인쇄와 완전히 동일)







                    float dpiX = PRINT_INFO_CLASS.PrinterInfo.printer_DPI; // 프린터 DPI







                    float dpiY = PRINT_INFO_CLASS.PrinterInfo.printer_DPI;















                    int DPI = (int)dpiX;























                    System.Diagnostics.Debug.WriteLine($"[미리보기 DPI] 프린터 DPI 사용: {dpiX} (인쇄와 동일)");















                    // 다중 컬럼 모드 확인 (엑셀) - 우선순위 1







                    // 엑셀 데이터가 있으면 직접 입력 데이터보다 우선 표시







                    if (mainForm.IsMultiColumnMode())







                    {







                        var multiColumnData = mainForm.GetMultiColumnData();























                        if (multiColumnData != null && multiColumnData.Count > 0)







                        {







                            // 다중 컬럼 이미지 생성







                            System.Drawing.Bitmap bitmap = GenerateMultiColumnBarcodeImage(multiColumnData, dpiX);







                            OnPaintPreviewEvent(bitmap);







                            return;







                        }







                    }















                    // 다중 입력 모드 확인 (수동 입력) - 우선순위 2















                    if (mainForm.IsMultiInputMode())







                    {







                        var multiInputData = mainForm.GetMultiInputData();















                        // 타이틀만 입력되어 있어도 미리보기 생성 (multiInputData가 비어있어도 타이틀 표시)







                        // 다중 입력 이미지 생성 (타이틀 포함)















                        System.Drawing.Bitmap bitmap = GenerateMultiInputBarcodeImage(multiInputData, dpiX);







                        OnPaintPreviewEvent(bitmap);







                        return;







                    }















                    // 단일 컬럼 모드 (기존 로직) - 우선순위 3















                    {







                        string data = mainForm.GetPrintData();















                        // 타이틀만 입력되어 있어도 미리보기 생성







                        // 타이틀이 있으면 빈 바코드라도 미리보기 생성















                        string senderReceiverLabel = mainForm.GetSenderReceiverLabel();







                        bool hasTitle = senderReceiverLabel != "표시없음";























                        if (string.IsNullOrEmpty(data) && !hasTitle)







                        {







                            // 바코드도 없고 타이틀도 없으면 미리보기 생성하지 않음







                            return;







                        }















                        // 바코드가 있으면 큐에 추가















                        if (!string.IsNullOrEmpty(data))







                        {







                            int barcodeSize = mainForm.GetBarcodeSize();







                            int barcodeX = mainForm.GetBarcodeX();







                            int barcodeY = mainForm.GetBarcodeY();







                            int barcodeHeight = mainForm.GetBarcodeHeight();







                            barcodePrintManager.AddToPrintQueue(data, barcodeSize, barcodeX, barcodeY, barcodeHeight);







                        }















                        // 바코드 이미지 생성 (타이틀 포함)















                        System.Drawing.Bitmap bitmap = GenerateBarcodeImage(data, dpiX);







                        OnPaintPreviewEvent(bitmap);







                    }







                }







            }







            catch (Exception ex)







            {







                Console.WriteLine(ex.Message);







            }















        }















        private System.Drawing.Bitmap GenerateBarcodeImage(string data, float dpiX)







        {







            try







            {







                // 현재 바코드 설정 가져오기







                string barcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType ?? "CODE_128";







                int barcodeSize = mainForm.GetBarcodeSize();







                int barcodeX = mainForm.GetBarcodeX();







                int barcodeY = mainForm.GetBarcodeY();















                // 라벨 크기 정보 가져오기







                int labelWidth = PRINT_INFO_CLASS.PrinterInfo.Paper_Width;







                int labelHeight = PRINT_INFO_CLASS.PrinterInfo.Paper_Height;















                // 라벨이 회전되어 있는지 확인 (에디터의 vertical/horizontal 규격 동기화)







                if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)







                {







                    int temp = labelWidth;







                    labelWidth = labelHeight;







                    labelHeight = temp;







                }















                // 실제 인쇄와 동일한 방식으로 바코드 크기 계산







                // 프린터 DPI를 사용하여 실제 출력과 동일한 크기 계산







                float printerDpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI;







                float mmToPixel = printerDpi / 25.4f; // mm를 픽셀로 변환







                int labelWidthPixels = (int)(labelWidth * mmToPixel);







                int labelHeightPixels = (int)(labelHeight * mmToPixel);















                // 라벨 크기에 맞는 비트맵 생성 (mm를 픽셀로 변환)















                int labelWidthPx = (int)(labelWidth * mmToPixel); // mm를 픽셀로 변환







                int labelHeightPx = (int)(labelHeight * mmToPixel);















                // QR코드와 바코드에 따른 크기 계산 개선







                int barcodeWidth, barcodeHeight;























                if (barcodeType == "QR_CODE")







                {
                    // 엑셀 모드와 동일하게 물리 포인트 단위 크기 계산 (에디터 화면과 100% 동기화)
                    int displaySize = (int)(barcodeSize * (mmToPixel * 25.4f) / 72f);
                    displaySize = Math.Max(displaySize, 10); // 최소 크기 보장

                    barcodeWidth = displaySize;
                    barcodeHeight = displaySize;
                    qrDisplayWidth = displaySize;
                    qrDisplayHeight = displaySize;

                    System.Diagnostics.Debug.WriteLine($"QR 코드 물리 크기 계산: 설정값={barcodeSize}, 용지크기={labelWidthPx}x{labelHeightPx}, 표시크기={displaySize}x{displaySize}");







                }







                else







                {







                    // 일반 바코드는 크기를 더 크게 조정







                    float sizeRatio = barcodeSize / 25.0f; // 분모를 50에서 25로 변경하여 비율 2배 확대







                    int maxBarcodeWidth = (int)(labelWidthPx * 0.9);  // 라벨 너비의 90%를 최대 크기로 (80%에서 증가)























                    barcodeWidth = (int)(maxBarcodeWidth * sizeRatio);  // 최대 크기 * 크기 비율















                    // 세로 크기 설정값 가져와서 적용 (기존에는 용지 높이의 60% 고정이었으나 설정값 반영)















                    int barcodeHeightValue = mainForm.GetBarcodeHeight();







                    barcodeHeight = (int)(barcodeHeightValue * mmToPixel);























                    System.Diagnostics.Debug.WriteLine($"[미리보기 바코드-단일] 가로설정={barcodeSize}, 세로설정={barcodeHeightValue}, 픽셀={barcodeWidth}x{barcodeHeight}");







                }















                // 텍스트 높이 미리 계산 (바코드 크기에 비례)







                // QR 코드의 경우 실제 표시 크기를 기준으로 계산







                int actualBarcodeHeight = barcodeType == "QR_CODE" ? qrDisplayHeight : barcodeHeight;







                int textHeight = (int)(actualBarcodeHeight * 0.25);















                // 바코드 타입에 따른 텍스트 간격 조절 (바코드 크기에 비례)















                int textSpacing;







                if (barcodeType == "QR_CODE")







                {







                    // QR 코드: 실제 표시 높이의 1-2% 범위에서 동적 조절 (간격 최소화)







                    textSpacing = Math.Max(1, (int)(qrDisplayHeight * 0.02));







                }







                else if (barcodeType == "CODE_128" || barcodeType == "CODE_39")







                {







                    // 일반 바코드: 바코드 높이의 8-15% 범위에서 동적 조절







                    textSpacing = Math.Max(8, (int)(barcodeHeight * 0.12));







                }







                else







                {







                    // 기타 바코드: 바코드 높이의 6-12% 범위에서 동적 조절







                    textSpacing = Math.Max(6, (int)(barcodeHeight * 0.10));







                }























                System.Diagnostics.Debug.WriteLine($"동적 텍스트 간격 계산: barcodeType={barcodeType}, 기본크기={barcodeHeight}, 실제크기={actualBarcodeHeight}, textSpacing={textSpacing}");















                // 엑셀 연동 시 텍스트 높이를 더 크게 설정 (한글 등으로 인한 공간 확보)







                if (!string.IsNullOrEmpty(mainForm.currentBarcodeText))







                {







                    textHeight = (int)(actualBarcodeHeight * 0.4); // 엑셀 텍스트는 더 큰 비율 사용 (실제 크기 기준)







                }















                // 바코드와 텍스트가 겹치지 않도록 바코드 크기 미리 조정







                // 텍스트는 바코드 하단 아래에 배치되므로 실제 바코드 높이 + 텍스트 높이 + 간격















                int requiredHeight = actualBarcodeHeight + textHeight + textSpacing;







                int availableHeight = labelHeightPx - 40; // 상하 여백 20px씩 (엑셀 텍스트를 위해 더 넉넉하게)























                if (barcodeType != "QR_CODE")







                {







                    if (requiredHeight > availableHeight)







                    {







                        // 바코드 높이를 줄여서 텍스트 공간 확보







                        int newBarcodeHeight = availableHeight - textHeight - textSpacing;







                        if (newBarcodeHeight > 15) // 최소 바코드 높이 보장







                        {







                            barcodeHeight = newBarcodeHeight;







                            // 엑셀 텍스트인 경우 더 큰 비율 유지







                            if (!string.IsNullOrEmpty(mainForm.currentBarcodeText))







                            {







                                textHeight = (int)(barcodeHeight * 0.4);







                            }







                            else







                            {







                                textHeight = (int)(barcodeHeight * 0.3);







                            }







                        }







                        else







                        {







                            // 바코드가 너무 작아지는 경우 텍스트 높이를 줄임







                            textHeight = Math.Max(15, availableHeight - barcodeHeight - textSpacing); // 최소 높이 증가







                        }







                    }







                }















                // ZXing 바코드 생성기 생성 (조정된 크기로)







                var writer = new ZXing.BarcodeWriter







                {







                    Format = GetBarcodeFormat(barcodeType),







                    Options = new ZXing.Common.EncodingOptions







                    {







                        Width = barcodeWidth,







                        Height = barcodeHeight,







                        Margin = barcodeType == "QR_CODE" ? 0 : 5, // QR 코드는 여백 최소화, 일반 바코드는 기존 유지







                        PureBarcode = false // 바코드만 생성하고 텍스트는 제외







                    }







                };















                // QR 코드 2배 크기 적용 및 여백 최소화 정보















                if (barcodeType == "QR_CODE")







                {







                    System.Diagnostics.Debug.WriteLine($"QR 코드 2배 크기 적용 + 여백 최소화: 기본크기={barcodeWidth}x{barcodeHeight}, 표시크기={qrDisplayWidth}x{qrDisplayHeight}, Margin=0");







                }







                else







                {







                    System.Diagnostics.Debug.WriteLine($"일반 바코드: 크기={barcodeWidth}x{barcodeHeight}, Margin=5");







                }















                // 바코드 생성 전 텍스트 처리 (한글 문자 처리)







                string processedData = ProcessBarcodeText(data, barcodeType);















                // 바코드 이미지 생성















                var barcodeBitmap = writer.Write(processedData);















                // QR 코드인 경우 비트맵으로 확대/축소 처리















                if (barcodeType == "QR_CODE" && qrDisplayWidth > 0 && qrDisplayHeight > 0)







                {







                    // 원본 QR 코드를 표시 크기로 확대/축소 (픽셀 단위 정확한 확대)







                    var scaledBitmap = new System.Drawing.Bitmap(qrDisplayWidth, qrDisplayHeight);







                    using (var g = System.Drawing.Graphics.FromImage(scaledBitmap))







                    {







                        // QR 코드는 픽셀 단위 정확한 확대를 위해 NearestNeighbor 사용







                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;







                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;







                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;







                        g.DrawImage(barcodeBitmap, 0, 0, qrDisplayWidth, qrDisplayHeight);







                    }







                    barcodeBitmap.Dispose(); // 원본 비트맵 해제







                    barcodeBitmap = scaledBitmap; // 확대/축소된 비트맵 사용















                    // 표시 크기를 실제 크기로 업데이트















                    barcodeWidth = qrDisplayWidth;







                    barcodeHeight = qrDisplayHeight;























                    System.Diagnostics.Debug.WriteLine($"QR 코드 비트맵 픽셀 정확 확대/축소: {barcodeWidth}x{barcodeHeight} (NearestNeighbor)");







                }

                else if (barcodeWidth > 0 && barcodeHeight > 0)

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

                    System.Diagnostics.Debug.WriteLine($"일반 바코드 비트맵 픽셀 정확 확대/축소: {barcodeWidth}x{barcodeHeight} (NearestNeighbor)");

                }















                // 바코드의 실제 크기 측정 (생성된 비트맵 기준)















                int measuredBarcodeWidth = barcodeBitmap.Width;







                int measuredBarcodeHeight = barcodeBitmap.Height;















                // 실제 크기와 계산된 크기가 다른 경우 실제 크기로 업데이트















                if (measuredBarcodeWidth != barcodeWidth || measuredBarcodeHeight != barcodeHeight)







                {







                    System.Diagnostics.Debug.WriteLine($"바코드 실제 크기 측정: 계산된 크기={barcodeWidth}x{barcodeHeight}, 실제 크기={measuredBarcodeWidth}x{measuredBarcodeHeight}");







                    barcodeWidth = measuredBarcodeWidth;







                    barcodeHeight = measuredBarcodeHeight;







                }























                System.Drawing.Bitmap finalBitmap = new System.Drawing.Bitmap(labelWidthPx, labelHeightPx);







                finalBitmap.SetResolution(dpiX, dpiX); // 고해상도 DPI 설정으로 선명한 이미지 생성















                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(finalBitmap))







                {







                    // 라벨 배경을 흰색으로 설정







                    g.Clear(System.Drawing.Color.White);















                    // 보내는 분/받는 분 라벨 그리기 - "표시없음"이 아닌 경우만







                    // 이름 필드 위치와 가까운 곳에 배치







                    // 상대좌표 계산을 위한 변수들















                    float currentY = 0; // 현재 Y 위치 (상대좌표 누적)























                    string senderReceiverLabel = mainForm.GetSenderReceiverLabel();







                    System.Diagnostics.Debug.WriteLine($"[Title 렌더링-단일바코드] senderReceiverLabel='{senderReceiverLabel}', 표시여부={senderReceiverLabel != "표시없음"}");







                    if (senderReceiverLabel != "표시없음")







                    {







                        // Title 크기와 위치를 appSettings에서 가져오기







                        int titleSize = mainForm.GetTitleSize(); // mm 단위







                        int titleOffsetY = mainForm.GetTitleOffsetY(); // mm 단위 (최상단 기준)


                        // Title 폰트 크기 계산


                        float textSizeInPixels = titleSize * mmToPixel;


                        float labelFontSize = Math.Max(8, textSizeInPixels * 0.6f); // 최소 8pt















                        // Title Y 위치 계산 (최상단 기준 절대좌표 - 다른 컬럼과 동일)















                        float titleTopY = titleOffsetY * mmToPixel; // 용지 전체 영역 기준 절대 좌표















                        // 폰트 높이 계산















                        float actualLabelHeight = labelFontSize * mmToPixel * 25.4f / 72f; // pt를 픽셀로 변환















                        // Title 하단 위치 계산 (다음 요소 위치 계산용)















                        currentY = titleTopY + actualLabelHeight;























                        System.Diagnostics.Debug.WriteLine($"[Title 위치-단일바코드] titleSize={titleSize}mm, titleOffsetY={titleOffsetY}mm, titleTopY={titleTopY}px, currentY={currentY}px");























                        using (System.Drawing.Font labelFont = new System.Drawing.Font("맑은 고딕", labelFontSize, System.Drawing.FontStyle.Bold))







                        using (System.Drawing.SolidBrush labelBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))







                        using (System.Drawing.StringFormat sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Near, FormatFlags = System.Drawing.StringFormatFlags.LineLimit, Trimming = System.Drawing.StringTrimming.Word })







                        {







                            // 타이틀 텍스트 자동 줄바꿈 적용 (인쇄와 동일)







                            float maxTitleWidth = labelWidthPx;







                            float maxTitleHeight = actualLabelHeight * 3; // 최대 3줄







                            System.Drawing.RectangleF titleRect = new System.Drawing.RectangleF(0, titleTopY, maxTitleWidth, maxTitleHeight);







                            g.DrawString(senderReceiverLabel, labelFont, labelBrush, titleRect, sf);







                        }







                    }







                    else







                    {







                        // 타이틀이 없으면 최상단부터 시작







                        currentY = 10; // 최상단 여백







                    }















                    // 바코드 위치 계산 (센터 정렬 X축, 상대좌표 Y축 - 단일 바코드 모드는 Title 아래 고정)















                    int labelCenterX = labelWidthPx / 2;















                    // X축은 센터 정렬 + 사용자 설정 OffsetX (에디터와 동일)
                    int centerX = labelCenterX - (barcodeWidth / 2) + (int)(barcodeX * mmToPixel);

                    // Y축 절대좌표 적용 (에디터와 100% 동기화) 및 용지 이탈 방지 안전 장치 추가
                    int rawBarcodeY = (int)(barcodeY * mmToPixel);
                    int maxBarcodeY = labelHeightPx - barcodeHeight;
                    
                    // 바코드 하단 텍스트가 표시되면 텍스트 공간 확보를 위해 최대 Y제한 보정
                    if (PRINT_INFO_CLASS.PrinterInfo.ShowBarcodeText)
                    {
                        maxBarcodeY -= textHeight + textSpacing;
                    }

                    int centerY = Math.Max(0, Math.Min(maxBarcodeY, rawBarcodeY));























                    System.Diagnostics.Debug.WriteLine($"바코드 위치 계산: centerX={centerX}, centerY={centerY} (barcodeX={barcodeX}, barcodeY={barcodeY})");















                    // 바코드 위치 제한 제거 - 자유로운 이동 허용















                    System.Diagnostics.Debug.WriteLine($"바코드 자유 위치: centerX={centerX}, centerY={centerY}");















                    // 바코드 그리기 (해상도 차이로 인한 크기 변형 방지를 위해 명시적 크기 지정)















                    g.DrawImage(barcodeBitmap, centerX, centerY, barcodeWidth, barcodeHeight);























                    // 바코드 아래에 텍스트 표시            















                    string textToDisplay = "";















                    // 엑셀 연동 시 currentBarcodeText 우선 사용















                    if (!string.IsNullOrEmpty(mainForm.currentBarcodeText))







                    {







                        textToDisplay = mainForm.currentBarcodeText;







                        System.Diagnostics.Debug.WriteLine($"엑셀 텍스트 표시: '{textToDisplay}'");















                        // 바코드 값 표시가 체크된 경우 바코드 값도 함께 표시















                        if (mainForm.showBarcodeText == true && !string.IsNullOrEmpty(data))







                        {







                            textToDisplay = $"{data}\n{textToDisplay}";







                            System.Diagnostics.Debug.WriteLine($"바코드 값 + 엑셀 텍스트: '{textToDisplay}'");







                        }







                    }







                    // 일반 모드에서 showBarcodeText가 체크된 경우 바코드 값 표시







                    else if (mainForm.showBarcodeText == true && !string.IsNullOrEmpty(data))







                    {







                        textToDisplay = data;







                        System.Diagnostics.Debug.WriteLine($"일반 모드 바코드 값 표시: '{textToDisplay}'");







                    }







                    // showBarcodeText가 false이면 바코드 값 표시하지 않음















                    // 텍스트 표시 조건 확인 (디버깅)















                    if (!string.IsNullOrEmpty(textToDisplay))







                    {







                        System.Diagnostics.Debug.WriteLine($"텍스트 표시: '{textToDisplay}' (barcodeY: {barcodeY})");







                        // 바코드 사이즈와 용지 크기에 비례한 텍스트 크기 계산







                        // 바코드 타입에 따라 텍스트 크기 비율 조정







                        float baseTextSize;







                        if (barcodeType == "QR_CODE")







                        {







                            // QR 코드는 기존 비율 유지







                            baseTextSize = Math.Max(8, barcodeSize * 0.1f);







                        }







                        else







                        {







                            // 일반 바코드는 텍스트 크기 비율 0.3f 적용







                            baseTextSize = Math.Max(8, barcodeSize * 0.3f);







                        }























                        float paperSizeRatio = Math.Min(labelWidthPx, labelHeightPx) / 200.0f; // 기준 크기 200px 대비 비율







                        float textSize = baseTextSize * Math.Max(0.8f, paperSizeRatio); // 최소 0.8배는 보장







                        using (System.Drawing.Font font = new System.Drawing.Font("Arial", textSize, System.Drawing.FontStyle.Bold))







                        using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))







                        using (System.Drawing.StringFormat sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center })























                        {







                            // 줄 간격 조정 (좁게)







                            float lineSpacing = 0.8f; // 폰트 높이의 80%만 사용







                            float lineHeight = font.GetHeight(g) * lineSpacing;















                            // 텍스트 내용에 따라 동적으로 높이 계산















                            System.Drawing.SizeF textSizeF;







                            float dynamicTextHeight;







                            float textWidth;























                            if (barcodeType == "QR_CODE")







                            {







                                // QR 코드일 때는 텍스트를 자연스럽게 표시 (바코드 폭에 제한하지 않음)







                                textSizeF = g.MeasureString(textToDisplay, font);







                                // 줄 수 계산







                                int lineCount = textToDisplay.Split('\n').Length;







                                dynamicTextHeight = lineHeight * lineCount;







                                textWidth = textSizeF.Width; // 텍스트의 실제 너비 사용























                                System.Diagnostics.Debug.WriteLine($"QR 코드 텍스트 자연스러운 표시 - 텍스트 너비: {textWidth}, 바코드 너비: {barcodeWidth}, 줄 수: {lineCount}, 줄 높이: {lineHeight}");







                            }







                            else







                            {







                                // 일반 바코드일 때는 기존 로직 유지 (바코드 폭에 맞춤)







                                textSizeF = g.MeasureString(textToDisplay, font, barcodeWidth, sf);







                                // 줄 수 추정 (바코드 폭에 맞춰 줄바꿈)







                                int estimatedLines = (int)Math.Ceiling(textSizeF.Height / font.GetHeight(g));







                                dynamicTextHeight = lineHeight * estimatedLines;







                                textWidth = barcodeWidth; // 바코드 폭에 맞춤







                            }















                            // 텍스트 위치 계산 - 항상 바코드 아래에만 배치















                            int textY = centerY + barcodeHeight + textSpacing;























                            System.Diagnostics.Debug.WriteLine($"텍스트 위치 계산: centerY={centerY}, barcodeHeight={barcodeHeight}, textSpacing={textSpacing}, textY={textY}");















                            // 텍스트가 라벨 영역을 벗어나는 경우 위치 조정 (하지만 라벨 영역 안의 부분은 보여짐)















                            if (textY < 0) // 상단을 벗어나는 경우







                            {







                                // 텍스트가 라벨 상단을 벗어나는 경우 라벨 상단에 배치







                                textY = 0;







                                System.Diagnostics.Debug.WriteLine($"텍스트 위치 조정 (상단): barcodeY={barcodeY} → textY={textY}");







                            }







                            else if (textY + dynamicTextHeight > labelHeightPx) // 하단을 벗어나는 경우







                            {







                                // 텍스트가 라벨 하단을 벗어나는 경우, 라벨 영역 안의 부분만 표시하도록 높이 조정







                                int visibleHeight = labelHeightPx - textY;







                                if (visibleHeight > 0)







                                {







                                    dynamicTextHeight = visibleHeight;







                                    System.Diagnostics.Debug.WriteLine($"텍스트 높이 조정 (하단): barcodeY={barcodeY} → textHeight={dynamicTextHeight} (라벨 영역 내 표시)");







                                }







                                else







                                {







                                    // 텍스트가 완전히 라벨 영역 밖에 있는 경우 라벨 하단에 배치







                                    textY = labelHeightPx - (int)dynamicTextHeight;







                                    if (textY < 0) textY = 0; // 최소 0으로 제한







                                    System.Diagnostics.Debug.WriteLine($"텍스트 위치 조정 (완전 벗어남): barcodeY={barcodeY} → textY={textY}");







                                }







                            }















                            // 텍스트 중앙 정렬을 위한 X 위치 계산















                            float textX;







                            if (barcodeType == "QR_CODE")







                            {







                                // QR 코드일 때는 QR 코드 중앙에 텍스트 배치







                                textX = centerX + (barcodeWidth - textWidth) / 2;







                                System.Diagnostics.Debug.WriteLine($"QR 코드 텍스트 위치 계산: centerX={centerX}, barcodeWidth={barcodeWidth}, textWidth={textWidth}, textX={textX}");







                            }







                            else







                            {







                                // 일반 바코드일 때는 바코드 중앙에 맞춤







                                textX = centerX;







                            }















                            // 텍스트를 자동 줄바꿈하여 그리기 (줄 간격 제어)















                            string[] lines = textToDisplay.Split(new[] { '\n' }, StringSplitOptions.None);







                            float lineCurrentY = textY; // 줄별 Y 위치 (바깥쪽 currentY와 구분)















                            // sf.LineLimit 제거하여 텍스트가 영역을 벗어나도 그림















                            sf.FormatFlags &= ~StringFormatFlags.LineLimit;























                            foreach (string line in lines)







                            {







                                if (!string.IsNullOrEmpty(line))







                                {







                                    // 텍스트가 너비를 초과하는지 확인







                                    System.Drawing.SizeF lineSize = g.MeasureString(line, font);























                                    if (lineSize.Width <= textWidth)







                                    {







                                        // 한 줄에 들어감







                                        float lineX = textX + (textWidth - lineSize.Width) / 2; // 중앙 정렬







                                        g.DrawString(line, font, brush, lineX, lineCurrentY);







                                        lineCurrentY += lineHeight;







                                    }







                                    else







                                    {







                                        // 여러 줄로 나누어 그리기







                                        System.Drawing.RectangleF textRect = new System.Drawing.RectangleF(textX, lineCurrentY, textWidth, lineHeight * 10); // 최대 10줄







                                        using (System.Drawing.StringFormat wrapSf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, FormatFlags = StringFormatFlags.LineLimit })







                                        {







                                            g.DrawString(line, font, brush, textRect, wrapSf);















                                            // 실제 그려진 줄 수 계산















                                            System.Drawing.SizeF multiLineSize = g.MeasureString(line, font, (int)textWidth, wrapSf);







                                            int linesDrawn = (int)Math.Ceiling(multiLineSize.Height / font.GetHeight(g));







                                            lineCurrentY += lineHeight * linesDrawn;







                                        }







                                    }







                                }







                            }







                        }







                    }







                    else







                    {







                        System.Diagnostics.Debug.WriteLine($"텍스트 미표시 - textToDisplay: '{textToDisplay}', barcodeY: {barcodeY}");







                    }







                } // End of using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(finalBitmap)) for barcode and text drawing















                // 실제 용지 테두리 그리기 (둥근 모서리, 회색 1px)















                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(finalBitmap))







                {







                    using (GraphicsPath path = GetRoundedRectanglePath(new Rectangle(0, 0, finalBitmap.Width - 1, finalBitmap.Height - 1), 8))







                    using (Pen borderPen = new Pen(Color.LightGray, 1))







                    {







                        g.DrawPath(borderPen, path);







                    }







                }















                if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)







                    finalBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);















                return finalBitmap;







            }







            catch (Exception ex)







            {







                Console.WriteLine($"바코드 이미지 생성 중 오류: {ex.Message}");







                // 기본 이미지 반환







                return new System.Drawing.Bitmap(100, 100);







            }







        }















        /// <summary>







        /// 바코드 생성 전 텍스트 처리 (한글 문자 처리)







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







                    // 한글 문자를 제거하고 영문자, 숫자, 일부 특수문자만 유지







                    var allowedChars = new List<char>();







                    foreach (char c in data)







                    {







                        // 영문자, 숫자, 하이픈, 언더스코어만 허용 (공백 제외)







                        if ((c >= 'A' && c <= 'Z') ||















                            (c >= 'a' && c <= 'z') ||















                            (c >= '0' && c <= '9') ||















                            c == '-' || c == '_')







                        {







                            allowedChars.Add(c);







                        }







                    }























                    string processedData = new string(allowedChars.ToArray()).Trim();















                    // 빈 문자열이거나 공백만 있으면 기본값 사용















                    if (string.IsNullOrEmpty(processedData))







                    {







                        processedData = "BARCODE";







                    }























                    System.Diagnostics.Debug.WriteLine($"바코드 텍스트 처리: '{data}' -> '{processedData}'");







                    return processedData;







                }















                // QR_CODE의 경우 모든 문자 지원















                if (barcodeType == "QR_CODE")







                {







                    return data; // QR 코드는 한글도 지원







                }















                // 기타 바코드 타입의 경우 기본 처리















                return data;







            }







            catch (Exception ex)







            {







                System.Diagnostics.Debug.WriteLine($"바코드 텍스트 처리 오류: {ex.Message}");







                return "BARCODE"; // 오류 시 기본값 반환







            }







        }















        /// <summary>







        /// 다중 입력 바코드 이미지 생성 (위치 설정 적용)







        /// </summary>







        private System.Drawing.Bitmap GenerateMultiInputBarcodeImage(List<BarcodePrint.View.MultiColumnData> multiInputData, float dpiX)







        {







            try







            {







                // 용지 크기 계산







                float paperWidthMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width;







                float paperHeightMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height;























                // 라벨이 회전되어 있는지 확인 (에디터의 vertical/horizontal 규격 동기화)







                if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)







                {







                    paperWidthMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height;







                    paperHeightMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width;







                }















                // 미리보기도 프린터 DPI 기준으로 크기 계산 (인쇄와 동일)















                float printerDpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI;







                float mmToPixel = printerDpi / 25.4f; // 프린터 DPI 기준 (인쇄와 동일)























                int labelWidthPx = (int)(paperWidthMm * mmToPixel);







                int labelHeightPx = (int)(paperHeightMm * mmToPixel);























                System.Diagnostics.Debug.WriteLine($"========================================");







                System.Diagnostics.Debug.WriteLine($"[미리보기 이미지 생성 시작]");







                System.Diagnostics.Debug.WriteLine($"[미리보기 DPI 정보] 프린터DPI={printerDpi}, 용지크기={paperWidthMm}x{paperHeightMm}mm, 픽셀={labelWidthPx}x{labelHeightPx}px, mmToPixel={mmToPixel}");







                System.Diagnostics.Debug.WriteLine($"[미리보기 이미지 크기] {labelWidthPx}x{labelHeightPx}px, 비율={labelWidthPx / (float)labelHeightPx:F2}");







                System.Diagnostics.Debug.WriteLine($"[미리보기 입력 데이터] 컬럼 수={multiInputData.Count}");







                foreach (var data in multiInputData)







                {







                    System.Diagnostics.Debug.WriteLine($"  - ColumnName='{data.ColumnName}', Value='{data.Value}', IsBarcode={data.IsBarcode}, Size={data.Size}, OffsetX={data.OffsetX}mm, OffsetY={data.OffsetY}mm, ShowBarcodeText={data.ShowBarcodeText}");







                }







                System.Diagnostics.Debug.WriteLine($"========================================");























                System.Drawing.Bitmap finalBitmap = new System.Drawing.Bitmap(labelWidthPx, labelHeightPx);







                // SetResolution 호출하지 않음 (참조 프로젝트와 동일)















                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(finalBitmap))







                {







                    g.Clear(System.Drawing.Color.White);







                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;







                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;















                    // 상대좌표 계산을 위한 변수들







                    float currentY = 10; // 최상단 여백















                    // 컬럼 위치 설정을 순서대로 가져오기 (소속→직책→이름→바코드)







                    var columnPositionSettings = mainForm.GetColumnPositionSettings();







                    var orderedSettings = columnPositionSettings?.OrderBy(s => s.ColumnIndex).ToList() ?? new List<ColumnPositionSetting>();















                    // 가격표용: 순서대로 렌더링 (타이틀→품명→가격→바코드)















                    foreach (var setting in orderedSettings)







                    {







                        // 인덱스를 기준으로 데이터 매칭 (가격표용 4개 컬럼)







                        int idx = setting.ColumnIndex;







                        var inputData = multiInputData.FirstOrDefault(d =>
                            (setting.ColumnIndex == 0 && (d.ColumnName == "컬럼1" || d.ColumnName == "품명" || d.ColumnName.Contains("품명"))) ||
                            (setting.ColumnIndex == 1 && (d.ColumnName == "컬럼2" || d.ColumnName == "가격" || d.ColumnName.Contains("가격"))) ||
                            (setting.ColumnIndex == 2 && (d.ColumnName == "컬럼3" || d.ColumnName == "옵션" || d.ColumnName.Contains("옵션") || d.ColumnName == "비고" || d.ColumnName.Contains("비고"))) ||
                            (setting.ColumnIndex == 3 && (d.ColumnName == "컬럼4" || d.ColumnName == "바코드" || d.ColumnName.Contains("바코드"))) ||
                            (setting.ColumnIndex == 4 && (d.ColumnName == "공통문구" || d.ColumnName.Contains("공통문구"))));















                        if (inputData == null)







                        {







                            System.Diagnostics.Debug.WriteLine($"[미리보기 렌더링 건너뜀] ColumnIndex={idx}, 데이터 없음");







                            continue;







                        }















                        // 절대좌표로 Y 위치 계산 (용지 최상단 기준 OffsetY)















                        currentY = inputData.OffsetY * mmToPixel;























                        System.Diagnostics.Debug.WriteLine($"========================================");







                        System.Diagnostics.Debug.WriteLine($"[미리보기 컬럼 렌더링 시작] ColumnIndex={setting.ColumnIndex}, ColumnName='{inputData.ColumnName}'");







                        System.Diagnostics.Debug.WriteLine($"[미리보기 컬럼 위치] OffsetY={inputData.OffsetY}mm → currentY={currentY}px, OffsetX={inputData.OffsetX}mm");







                        System.Diagnostics.Debug.WriteLine($"[미리보기 컬럼 데이터] 값='{inputData.Value}', IsBarcode={inputData.IsBarcode}, Size={inputData.Size}mm, ShowBarcodeText={inputData.ShowBarcodeText}");























                        if (inputData.IsBarcode) // Check IsBarcode property







                        {







                            // 바코드 크기 계산 (inputData의 Size 사용)







                            int barcodeSize = inputData.Size;







                            int barcodeWidth, barcodeHeight;























                            if (inputData.BarcodeType == "QR_CODE")







                            {







                                // QR 코드: 정방형 (Width와 Height 동일) - 프린터 DPI 기준 (인쇄와 동일)







                                barcodeWidth = (int)(barcodeSize * (mmToPixel * 25.4f) / 72f);
                                barcodeHeight = barcodeWidth;







                                System.Diagnostics.Debug.WriteLine($"[다중 입력 미리보기 QR] 크기={barcodeSize}mm, 픽셀={barcodeWidth}x{barcodeHeight}, mmToPixel={mmToPixel}");







                            }







                            else







                            {







                                // 일반 바코드: Width는 크기에 비례, Height는 설정된 BarcodeHeight 사용







                                barcodeWidth = inputData.CustomWidth > 0 







                                    ? (int)(inputData.CustomWidth * mmToPixel) 







                                    : (int)(barcodeSize * mmToPixel * 1.5);







                                // BarcodeHeight는 mm 단위에 가깝게 mmToPixel만 적용







                                barcodeHeight = (int)(inputData.BarcodeHeight * mmToPixel);







                                System.Diagnostics.Debug.WriteLine($"[다중 입력 미리보기 바코드] 크기={barcodeSize}mm, 세로크기={inputData.BarcodeHeight}mm, 픽셀={barcodeWidth}x{barcodeHeight}, mmToPixel={mmToPixel}");







                            }















                            // 바코드 생성 (PureBarcode = true로 고정, 텍스트는 별도 그리기)







                            var barcodeWriter = new ZXing.BarcodeWriter







                            {







                                Format = GetBarcodeFormat(inputData.BarcodeType),







                                Options = new ZXing.Common.EncodingOptions







                                {







                                    Width = barcodeWidth,







                                    Height = barcodeHeight,







                                    Margin = inputData.BarcodeType == "QR_CODE" ? 0 : 5,







                                    PureBarcode = true // 항상 바코드만 생성







                                }







                            };















                            try







                            {







                                // 바코드 텍스트 처리







                                string processedBarcodeText = ProcessBarcodeText(inputData.Value, inputData.BarcodeType);







                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 생성] 원본값='{inputData.Value}', 처리값='{processedBarcodeText}', BarcodeType={inputData.BarcodeType}");







                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 크기 계산] Size={barcodeSize}mm, barcodeWidth={barcodeWidth}px, barcodeHeight={barcodeHeight}px, mmToPixel={mmToPixel}");















                                // 바코드 이미지 생성















                                var barcodeBitmap = barcodeWriter.Write(processedBarcodeText);

                                if (barcodeBitmap != null && barcodeWidth > 0 && barcodeHeight > 0)

                                {

                                    var scaledBitmap = new System.Drawing.Bitmap(barcodeWidth, barcodeHeight);

                                    using (var gScale = System.Drawing.Graphics.FromImage(scaledBitmap))

                                    {

                                        gScale.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                                        gScale.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                                        gScale.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

                                        gScale.DrawImage(barcodeBitmap, 0, 0, barcodeWidth, barcodeHeight);

                                    }

                                    barcodeBitmap.Dispose();

                                    barcodeBitmap = scaledBitmap;

                                }























                                if (barcodeBitmap == null)







                                {







                                    System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 생성 실패] barcodeBitmap이 null입니다. 값='{processedBarcodeText}'");







                                    continue;







                                }























                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 비트맵] 실제 크기={barcodeBitmap.Width}x{barcodeBitmap.Height}px");















                                // 바코드 위치 계산 (센터 정렬 X축, 절대좌표 Y축)







                                int labelCenterX = labelWidthPx / 2;















                                // X축은 센터 정렬, Y축은 절대좌표 사용















                                int barcodeX = labelCenterX - (barcodeBitmap.Width / 2) + (int)(inputData.OffsetX * mmToPixel);







                                int barcodeY = (int)currentY; // 절대좌표 Y 위치























                                int finalBarcodeX = Math.Max(0, barcodeX);







                                // 바코드 하단 텍스트를 배려한 Y축 클램핑 (미리보기)







                                int maxBarcodeY = labelHeightPx - barcodeBitmap.Height;







                                if (inputData.ShowBarcodeText)







                                {







                                     float fontSizeInPixels = Math.Max(8f, inputData.BarcodeTextSize * dpiX / 72f);







                                     using (System.Drawing.Font font = new System.Drawing.Font("Arial", fontSizeInPixels, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))







                                     {







                                         int textHeight = (int)(font.GetHeight(g) + 5);







                                         maxBarcodeY -= textHeight;







                                     }







                                }







                                int finalBarcodeY = Math.Max(0, Math.Min(maxBarcodeY, barcodeY));























                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 위치 계산] labelCenterX={labelCenterX}px, labelWidthPx={labelWidthPx}px");




                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 위치 계산] barcodeX={barcodeX}px (센터{labelCenterX} - 반폭{barcodeBitmap.Width / 2} + OffsetX{(int)(inputData.OffsetX * mmToPixel)})");


                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 위치 계산] barcodeY={barcodeY}px (절대좌표), labelHeightPx={labelHeightPx}px");







                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 최종 위치] finalBarcodeX={finalBarcodeX}px, finalBarcodeY={finalBarcodeY}px");







                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 범위 체크] X범위=[{finalBarcodeX} ~ {finalBarcodeX + barcodeBitmap.Width}], Y범위=[{finalBarcodeY} ~ {finalBarcodeY + barcodeBitmap.Height}]");















                                // 바코드 그리기 (해상도 차이로 인한 크기 변형 방지를 위해 명시적 크기 지정)















                                g.DrawImage(barcodeBitmap, finalBarcodeX, finalBarcodeY, barcodeBitmap.Width, barcodeBitmap.Height);







                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 그리기 완료] 위치=({finalBarcodeX}, {finalBarcodeY}), 크기={barcodeBitmap.Width}x{barcodeBitmap.Height}px");















                                 if (inputData.ShowBarcodeText && !string.IsNullOrEmpty(inputData.Value))
                                 {
                                     float fontSizeInPixels = Math.Max(8f, inputData.BarcodeTextSize * dpiX / 72f);
                                     using (System.Drawing.Font font = new System.Drawing.Font("Arial", fontSizeInPixels, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
                                     using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                                     using (System.Drawing.StringFormat sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, Trimming = System.Drawing.StringTrimming.Word })
                                     {
                                          float textY = finalBarcodeY + barcodeHeight + 5 + (inputData.BarcodeTextOffsetY * mmToPixel);
                                          var textSize = g.MeasureString(inputData.Value, font);
                                          float textWidth = Math.Max(barcodeWidth, textSize.Width + 10);
                                          float textX = finalBarcodeX + (barcodeWidth / 2.0f) + (inputData.BarcodeTextOffsetX * mmToPixel);
                                          System.Drawing.RectangleF textRect = new System.Drawing.RectangleF(textX - textWidth / 2.0f, textY, textWidth, fontSizeInPixels * 5);

                                          g.DrawString(inputData.Value, font, brush, textRect, sf);
                                     }
                                 }















                                // 바코드 비트맵 해제















                                barcodeBitmap.Dispose();







                            }







                            catch (Exception ex)







                            {







                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 생성 오류] 값='{inputData.Value}', BarcodeType={inputData.BarcodeType}, 오류메시지={ex.Message}");







                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드 생성 오류] 스택트레이스: {ex.StackTrace}");







                                // 오류 시 텍스트로 표시







                                using (System.Drawing.Font font = new System.Drawing.Font("맑은 고딕", 12F))







                                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Red))







                                {







                                    g.DrawString($"바코드 생성 오류: {inputData.Value}", font, brush, 20, 20);







                                }







                            }







                        }







                        else // Handle as text







                        {







                            // 텍스트 크기 계산 (에디터와 100% 동일하게 pt -> pixel 변환)







                            float fontSizeInPixels = Math.Max(8f, inputData.Size * printerDpi / 72f);















                            System.Diagnostics.Debug.WriteLine($"[미리보기 텍스트 크기] Size={inputData.Size}pt, 픽셀={fontSizeInPixels}px");















                            System.Drawing.StringAlignment alignVal1 = System.Drawing.StringAlignment.Near;







                            if (inputData.Alignment == "Center") alignVal1 = System.Drawing.StringAlignment.Center;







                            else if (inputData.Alignment == "Right") alignVal1 = System.Drawing.StringAlignment.Far;















                            System.Drawing.Font font = null;
                            string familyName = string.IsNullOrEmpty(inputData.FontFamily) ? "맑은 고딕" : inputData.FontFamily;
                            System.Drawing.FontStyle style = inputData.IsBold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular;
                            try
                            {
                                var family = new System.Drawing.FontFamily(familyName);
                                if (!family.IsStyleAvailable(style))
                                {
                                    if (family.IsStyleAvailable(System.Drawing.FontStyle.Regular)) style = System.Drawing.FontStyle.Regular;
                                    else if (family.IsStyleAvailable(System.Drawing.FontStyle.Bold)) style = System.Drawing.FontStyle.Bold;
                                    else style = System.Drawing.FontStyle.Regular;
                                }
                                font = new System.Drawing.Font(family, fontSizeInPixels, style, System.Drawing.GraphicsUnit.Pixel);
                            }
                            catch
                            {
                                font = new System.Drawing.Font("맑은 고딕", fontSizeInPixels, style, System.Drawing.GraphicsUnit.Pixel);
                            }
                            using (font)







                            using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))







                            using (System.Drawing.StringFormat textSf = new System.Drawing.StringFormat { Alignment = alignVal1, LineAlignment = System.Drawing.StringAlignment.Near, FormatFlags = 0, Trimming = StringTrimming.Word })







                            {







                                System.Drawing.SizeF size = g.MeasureString(inputData.Value, font);







                                







                                // 가로 폭 제한 적용 (CustomWidth가 설정되어 있으면 사용, 아니면 측정된 size.Width 사용)







                                float wrapWidth = inputData.CustomWidth > 0 ? inputData.CustomWidth * mmToPixel : size.Width;







                                if (inputData.CustomWidth > 0)







                                {







                                    size = g.MeasureString(inputData.Value, font, (int)wrapWidth, textSf);







                                }







                                







                                float textHeight = Math.Max(size.Height, 20);







                                







                                // 위치 기준을 왼쪽 상단으로 조절







                                float pxX = labelWidthPx / 2f + inputData.OffsetX * mmToPixel;
                                float pxY = inputData.OffsetY * mmToPixel;
                                float centerY = pxY + textHeight / 2f;
                                System.Drawing.RectangleF rect = new System.Drawing.RectangleF(-wrapWidth / 2f, -textHeight / 2f, wrapWidth, textHeight);

                                var state = g.Save();
                                g.TranslateTransform(pxX, centerY);
                                g.RotateTransform(inputData.RotateAngle);
                                g.DrawString(inputData.Value, font, brush, rect, textSf);
                                g.Restore(state);

 















                                System.Diagnostics.Debug.WriteLine($"[미리보기 컬럼 렌더링 완료] ColumnName='{inputData.ColumnName}', OffsetY={inputData.OffsetY}mm (절대좌표)");







                            }







                        }







                    }















                    // 실제 용지 테두리 그리기 (둥근 모서리, 회색 1px)















                    using (GraphicsPath path = GetRoundedRectanglePath(new Rectangle(0, 0, finalBitmap.Width - 1, finalBitmap.Height - 1), 8))







                    using (Pen borderPen = new Pen(Color.LightGray, 1))







                    {







                        g.DrawPath(borderPen, path);







                    }























                    System.Diagnostics.Debug.WriteLine($"[미리보기 이미지 생성 완료] 최종 크기={finalBitmap.Width}x{finalBitmap.Height}px");







                    System.Diagnostics.Debug.WriteLine($"========================================");







                }







                if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)







                    finalBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);















                return finalBitmap;







            }







            catch (Exception ex)







            {







                Console.WriteLine($"다중 입력 바코드 이미지 생성 중 오류: {ex.Message}");







                System.Diagnostics.Debug.WriteLine($"다중 입력 바코드 이미지 생성 오류: {ex.Message}\n{ex.StackTrace}");







                // 기본 이미지 반환







                return new System.Drawing.Bitmap(100, 100);







            }







        }















        /// <summary>







        /// 다중 컬럼 바코드 이미지 생성







        /// </summary>







        private System.Drawing.Bitmap GenerateMultiColumnBarcodeImage(List<BarcodePrint.View.MultiColumnData> multiColumnData, float dpiX)







        {







            try







            {







                // 용지 크기 계산







                float paperWidthMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width;







                float paperHeightMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height;























                // 라벨이 회전되어 있는지 확인 (에디터의 vertical/horizontal 규격 동기화)







                if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)







                {







                    paperWidthMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height;







                    paperHeightMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width;







                }















                // 미리보기도 프린터 DPI 기준으로 크기 계산 (인쇄와 동일)















                float printerDpi = PRINT_INFO_CLASS.PrinterInfo.printer_DPI;







                float mmToPixel = printerDpi / 25.4f; // 프린터 DPI 기준 (인쇄와 동일)























                int labelWidthPx = (int)(paperWidthMm * mmToPixel);







                int labelHeightPx = (int)(paperHeightMm * mmToPixel);























                System.Diagnostics.Debug.WriteLine($"[미리보기 용지] 프린터DPI={printerDpi}, 용지크기={paperWidthMm}x{paperHeightMm}mm, 픽셀={labelWidthPx}x{labelHeightPx}px, mmToPixel={mmToPixel}");























                System.Drawing.Bitmap finalBitmap = new System.Drawing.Bitmap(labelWidthPx, labelHeightPx);







                // SetResolution 호출하지 않음 (참조 프로젝트와 동일)























                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(finalBitmap))







                {







                    g.Clear(System.Drawing.Color.White);







                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;







                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;















                    // 상대좌표 계산을 위한 변수들







                    float currentY = 10; // 최상단 여백















                    // 컬럼 위치 설정을 순서대로 가져오기 (가격표용: 품명→가격→바코드)















                    var columnPositionSettings = mainForm.GetColumnPositionSettings();







                    var orderedSettings = columnPositionSettings?.OrderBy(s => s.ColumnIndex).ToList() ?? new List<ColumnPositionSetting>();















                    // 가격표용: 순서대로 렌더링 (타이틀→품명→가격→바코드)















                    foreach (var setting in orderedSettings)







                    {







                        // 인덱스를 기준으로 데이터 매칭







                        int idx = setting.ColumnIndex;







                        var columnData = multiColumnData.FirstOrDefault(d =>
                            (setting.ColumnIndex == 0 && (d.ColumnName == "컬럼1" || d.ColumnName == "품명" || d.ColumnName.Contains("품명"))) ||
                            (setting.ColumnIndex == 1 && (d.ColumnName == "컬럼2" || d.ColumnName == "가격" || d.ColumnName.Contains("가격"))) ||
                            (setting.ColumnIndex == 2 && (d.ColumnName == "컬럼3" || d.ColumnName == "옵션" || d.ColumnName.Contains("옵션") || d.ColumnName == "비고" || d.ColumnName.Contains("비고"))) ||
                            (setting.ColumnIndex == 3 && (d.ColumnName == "컬럼4" || d.ColumnName == "바코드" || d.ColumnName.Contains("바코드"))) ||
                            (setting.ColumnIndex == 4 && (d.ColumnName == "공통문구" || d.ColumnName.Contains("공통문구"))));















                        if (columnData == null) continue;















                        // 절대좌표로 Y 위치 계산 (용지 최상단 기준 OffsetY)















                        currentY = columnData.OffsetY * mmToPixel;























                        System.Diagnostics.Debug.WriteLine($"[미리보기 렌더링-절대좌표] ColumnIndex={setting.ColumnIndex}, 값='{columnData.Value}', IsBarcode={columnData.IsBarcode}, Size={columnData.Size}, OffsetY={columnData.OffsetY}mm, currentY={currentY}px (절대좌표)");























                        if (columnData.IsBarcode)







                        {







                            // 바코드 크기 계산 (MultiColumnData의 Size 속성 사용)







                            int barcodeSize = columnData.Size;







                            int barcodeWidth, barcodeHeight;























                            if (columnData.BarcodeType == "QR_CODE")







                            {







                                // QR 코드: 정방형 (Width와 Height 동일) - 프린터 DPI 기준 (인쇄와 동일)













                                barcodeWidth = (int)(barcodeSize * (mmToPixel * 25.4f) / 72f);
                                barcodeHeight = barcodeWidth;







                                System.Diagnostics.Debug.WriteLine($"[미리보기 QR] 크기={barcodeSize}mm, 픽셀={barcodeWidth}x{barcodeHeight}, mmToPixel={mmToPixel}");







                            }







                            else







                            {







                                // 일반 바코드: Width는 크기에 비례, Height는 설정된 BarcodeHeight 사용







                                barcodeWidth = columnData.CustomWidth > 0 







                                    ? (int)(columnData.CustomWidth * mmToPixel) 







                                    : (int)(barcodeSize * mmToPixel * 1.5);







                                // BarcodeHeight는 mm 단위에 가깝게 mmToPixel만 적용







                                barcodeHeight = (int)(columnData.BarcodeHeight * mmToPixel);







                                System.Diagnostics.Debug.WriteLine($"[미리보기 바코드] 크기={barcodeSize}mm, 세로크기={columnData.BarcodeHeight}mm, 픽셀={barcodeWidth}x{barcodeHeight}, mmToPixel={mmToPixel}");







                            }















                            // 바코드 생성 (PureBarcode = true로 고정, 텍스트는 별도 그리기)















                            var barcodeWriter = new ZXing.BarcodeWriter







                            {







                                Format = GetBarcodeFormat(columnData.BarcodeType),







                                Options = new ZXing.Common.EncodingOptions







                                {







                                    Width = barcodeWidth,







                                    Height = barcodeHeight,







                                    Margin = columnData.BarcodeType == "QR_CODE" ? 0 : 5,







                                    PureBarcode = true // 항상 바코드만 생성







                                }







                            };























                            try







                            {







                                // 바코드 이미지 생성







                                var barcodeBitmap = barcodeWriter.Write(ProcessBarcodeText(columnData.Value, columnData.BarcodeType));

                                if (barcodeBitmap != null && barcodeWidth > 0 && barcodeHeight > 0)

                                {

                                    var scaledBitmap = new System.Drawing.Bitmap(barcodeWidth, barcodeHeight);

                                    using (var gScale = System.Drawing.Graphics.FromImage(scaledBitmap))

                                    {

                                        gScale.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                                        gScale.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                                        gScale.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

                                        gScale.DrawImage(barcodeBitmap, 0, 0, barcodeWidth, barcodeHeight);

                                    }

                                    barcodeBitmap.Dispose();

                                    barcodeBitmap = scaledBitmap;

                                }















                                // 바코드 위치 계산 (센터 정렬 X축, 절대좌표 Y축)















                                int labelCenterX = labelWidthPx / 2;















                                // X축은 센터 정렬, Y축은 절대좌표 사용















                                int barcodeX = labelCenterX - (barcodeBitmap.Width / 2) + (int)(columnData.OffsetX * mmToPixel);







                                int barcodeY = (int)currentY; // 절대좌표 Y 위치























                                int finalBarcodeX = Math.Max(0, barcodeX);







                                // 바코드 하단 텍스트를 배려한 Y축 클램핑 (미리보기-엑셀)







                                int maxBarcodeY = labelHeightPx - barcodeBitmap.Height;







                                if (columnData.ShowBarcodeText)







                                {







                                    float fontSizeInPixels = Math.Max(8f, columnData.BarcodeTextSize * dpiX / 72f);







                                    using (System.Drawing.Font font = new System.Drawing.Font("Arial", fontSizeInPixels, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))







                                    {







                                        int textHeight = (int)(font.GetHeight(g) + 5);







                                        maxBarcodeY -= textHeight;







                                    }







                                }







                                int finalBarcodeY = Math.Max(0, Math.Min(maxBarcodeY, barcodeY));























                                System.Diagnostics.Debug.WriteLine($"[바코드 위치-절대좌표 (엑셀)] 생성크기={barcodeBitmap.Width}x{barcodeBitmap.Height}, 위치=({finalBarcodeX}, {finalBarcodeY}), OffsetX={columnData.OffsetX}, OffsetY={columnData.OffsetY}mm (절대좌표)");















                                // 바코드 그리기 (해상도 차이로 인한 크기 변형 방지를 위해 명시적 크기 지정)















                                g.DrawImage(barcodeBitmap, finalBarcodeX, finalBarcodeY, barcodeBitmap.Width, barcodeBitmap.Height);















                                // 바코드 값 텍스트 표시 (ShowBarcodeText = true일 때만) - 프린터 DPI 기준 (인쇄와 동일)















                                if (columnData.ShowBarcodeText && !string.IsNullOrEmpty(columnData.Value))
                                {
                                    float barcodeTextSizeInPixels = columnData.BarcodeTextSize * dpiX / 72f;
                                    float fontSize = Math.Max(8, barcodeTextSizeInPixels);

                                    using (System.Drawing.Font font = new System.Drawing.Font("Arial", fontSize, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
                                    using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                                    using (System.Drawing.StringFormat sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, Trimming = System.Drawing.StringTrimming.Word })
                                    {
                                         float textY = finalBarcodeY + barcodeBitmap.Height + 5 + (columnData.BarcodeTextOffsetY * mmToPixel);
                                         var textSize = g.MeasureString(columnData.Value, font);
                                         float textWidth = Math.Max(barcodeBitmap.Width, textSize.Width + 10);
                                         float textX = finalBarcodeX + (barcodeBitmap.Width / 2.0f) + (columnData.BarcodeTextOffsetX * mmToPixel);
                                         System.Drawing.RectangleF textRect = new System.Drawing.RectangleF(textX - textWidth / 2.0f, textY, textWidth, fontSize * 5);

                                         g.DrawString(columnData.Value, font, brush, textRect, sf);
                                    }
                                }
 















                                // 바코드 비트맵 해제















                                barcodeBitmap.Dispose();







                            }







                            catch (Exception ex)







                            {







                                System.Diagnostics.Debug.WriteLine($"바코드 생성 오류: {ex.Message}");







                                // 오류 시 텍스트로 표시







                                using (System.Drawing.Font font = new System.Drawing.Font("맑은 고딕", 12F))







                                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Red))







                                {







                                    g.DrawString($"바코드 생성 오류: {ex.Message}", font, brush, 20, currentY);







                                    currentY += 30;







                                }







                            }







                        }







                        else







                        {







                            // 텍스트 크기 계산 (에디터와 100% 동일하게 pt -> pixel 변환)







                            float fontSizeInPixels = Math.Max(8f, columnData.Size * printerDpi / 72f);















                            System.Diagnostics.Debug.WriteLine($"[미리보기 텍스트 크기 (엑셀)] Size={columnData.Size}pt, 픽셀={fontSizeInPixels}px");















                            System.Drawing.StringAlignment alignVal2 = System.Drawing.StringAlignment.Near;







                            if (columnData.Alignment == "Center") alignVal2 = System.Drawing.StringAlignment.Center;







                            else if (columnData.Alignment == "Right") alignVal2 = System.Drawing.StringAlignment.Far;















                            System.Drawing.Font font = null;
                            string familyName = string.IsNullOrEmpty(columnData.FontFamily) ? "맑은 고딕" : columnData.FontFamily;
                            System.Drawing.FontStyle style = columnData.IsBold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular;
                            try
                            {
                                var family = new System.Drawing.FontFamily(familyName);
                                if (!family.IsStyleAvailable(style))
                                {
                                    if (family.IsStyleAvailable(System.Drawing.FontStyle.Regular)) style = System.Drawing.FontStyle.Regular;
                                    else if (family.IsStyleAvailable(System.Drawing.FontStyle.Bold)) style = System.Drawing.FontStyle.Bold;
                                    else style = System.Drawing.FontStyle.Regular;
                                }
                                font = new System.Drawing.Font(family, fontSizeInPixels, style, System.Drawing.GraphicsUnit.Pixel);
                            }
                            catch
                            {
                                font = new System.Drawing.Font("맑은 고딕", fontSizeInPixels, style, System.Drawing.GraphicsUnit.Pixel);
                            }
                            using (font)







                            using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))







                            using (System.Drawing.StringFormat sf = new System.Drawing.StringFormat { Alignment = alignVal2, LineAlignment = System.Drawing.StringAlignment.Near, FormatFlags = 0, Trimming = StringTrimming.Word })







                            {







                                System.Drawing.SizeF size = g.MeasureString(columnData.Value, font);







                                







                                // 가로 폭 제한 적용 (CustomWidth가 설정되어 있으면 사용, 아니면 측정된 size.Width 사용)







                                float wrapWidth = columnData.CustomWidth > 0 ? columnData.CustomWidth * mmToPixel : size.Width;







                                if (columnData.CustomWidth > 0)







                                {







                                    size = g.MeasureString(columnData.Value, font, (int)wrapWidth, sf);







                                }







                                







                                float textHeight = Math.Max(size.Height, 20);







                                







                                // 위치 기준을 왼쪽 상단으로 조절







                                float pxX = labelWidthPx / 2f + columnData.OffsetX * mmToPixel;
                                float pxY = columnData.OffsetY * mmToPixel;
                                float centerY = pxY + textHeight / 2f;
                                System.Drawing.RectangleF rect = new System.Drawing.RectangleF(-wrapWidth / 2f, -textHeight / 2f, wrapWidth, textHeight);

                                var state = g.Save();
                                g.TranslateTransform(pxX, centerY);
                                g.RotateTransform(columnData.RotateAngle);
                                g.DrawString(columnData.Value, font, brush, rect, sf);
                                g.Restore(state);















                                System.Diagnostics.Debug.WriteLine($"[텍스트 위치-절대좌표 (엑셀)] 값='{columnData.Value}', pxX={pxX}, pxY={pxY}, 높이={textHeight}px, OffsetY={columnData.OffsetY}mm (절대좌표)");







                            }







                        }







                    }















                    // 실제 용지 테두리 그리기 (둥근 모서리, 회색 1px)







                    using (GraphicsPath path = GetRoundedRectanglePath(new Rectangle(0, 0, finalBitmap.Width - 1, finalBitmap.Height - 1), 8))







                    using (Pen borderPen = new Pen(Color.LightGray, 1))







                    {







                        g.DrawPath(borderPen, path);







                    }







                }























                if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)







                    finalBitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);















                return finalBitmap;







            }







            catch (Exception ex)







            {







                Console.WriteLine($"다중 컬럼 바코드 이미지 생성 중 오류: {ex.Message}");







                System.Diagnostics.Debug.WriteLine($"다중 컬럼 바코드 이미지 생성 오류: {ex.Message}\n{ex.StackTrace}");







                // 기본 이미지 반환







                return new System.Drawing.Bitmap(100, 100);







            }







        }















        private ZXing.BarcodeFormat GetBarcodeFormat(string barcodeType)







        {







            switch (barcodeType)







            {







                // 1D 바코드 (선형 바코드)







                case "CODE_128":







                case "CODE_128_A":







                case "CODE_128_B":







                case "CODE_128_C":







                    return ZXing.BarcodeFormat.CODE_128;







                case "CODE_39":







                    return ZXing.BarcodeFormat.CODE_39;







                case "CODE_93":







                    return ZXing.BarcodeFormat.CODE_93;







                case "EAN_13":







                    return ZXing.BarcodeFormat.EAN_13;







                case "EAN_8":







                    return ZXing.BarcodeFormat.EAN_8;







                case "UPC_A":







                    return ZXing.BarcodeFormat.UPC_A;







                case "UPC_E":







                    return ZXing.BarcodeFormat.UPC_E;







                case "CODABAR":







                    return ZXing.BarcodeFormat.CODABAR;







                case "ITF":







                    return ZXing.BarcodeFormat.ITF;







                case "RSS_14":







                    return ZXing.BarcodeFormat.RSS_14;







                case "RSS_EXPANDED":







                    return ZXing.BarcodeFormat.RSS_EXPANDED;















                // 2D 바코드 (매트릭스 바코드)















                case "QR_CODE":







                    return ZXing.BarcodeFormat.QR_CODE;







                case "DATA_MATRIX":







                    return ZXing.BarcodeFormat.DATA_MATRIX;







                case "PDF_417":







                    return ZXing.BarcodeFormat.PDF_417;







                case "AZTEC":







                    return ZXing.BarcodeFormat.AZTEC;







                case "MAXICODE":







                    return ZXing.BarcodeFormat.MAXICODE;























                default:







                    return ZXing.BarcodeFormat.CODE_128;







            }







        }















        /// <summary>







        /// 둥근 사각형 경로 생성







        /// </summary>







        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)







        {







            GraphicsPath path = new GraphicsPath();







            int diameter = radius * 2;















            // 왼쪽 상단







            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);







            // 오른쪽 상단







            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);







            // 오른쪽 하단







            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);







            // 왼쪽 하단







            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);























            path.CloseFigure();







            return path;







        }















        /// ////////////////////////////////////////////////////







        /// 이벤트 발생







        public class PaintEventArgs : EventArgs







        {







            public System.Drawing.Bitmap bit;







        }























        public void OnPaintPreviewEvent(System.Drawing.Bitmap bitmap)







        {







            PaintEventArgs e = new PaintEventArgs();







            e.bit = bitmap;







            PaintPreviewEvent?.Invoke(this, e);







        }







    }







}







