using ClosedXML.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BarcodePrint.Model
{
    internal class PrintManager
    {
        // 비트맵을 1bppIndexed로 변환
        public static Bitmap ConvertTo1Bpp(Bitmap bitmap)
        {
            int originalWidth = bitmap.Width;
            int width = (bitmap.Width + 7) / 8 * 8; // 너비를 8의 배수로 조정
            int height = bitmap.Height;

            // 디버깅: 너비 조정 정보 출력

            if (originalWidth != width)
            {
                Console.WriteLine($"ConvertTo1Bpp: 원본 너비 {originalWidth} -> 조정된 너비 {width} (추가된 픽셀: {width - originalWidth})");
            }

            Bitmap result = new Bitmap(width, height, PixelFormat.Format1bppIndexed);

            // 원본 비트맵 데이터를 잠금
            BitmapData sourceData = bitmap.LockBits(
                new Rectangle(0, 0, originalWidth, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb // 24비트 RGB로 가정
            );

            // 결과 비트맵 데이터를 잠금
            BitmapData resultData = result.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format1bppIndexed
            );

            int sourceStride = sourceData.Stride;
            int resultStride = resultData.Stride;

            byte[] resultPixels = new byte[resultStride * height];

            // 초기화: 전체 영역을 흰색(0)으로 설정
            for (int i = 0; i < resultPixels.Length; i++)
            {
                resultPixels[i] = 0x00; // 흰색으로 초기화 (1bpp에서 0이 흰색)
            }

            IntPtr sourcePtr = sourceData.Scan0;
            unsafe
            {
                byte* source = (byte*)sourcePtr;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < originalWidth; x++)
                    {
                        // 원본 픽셀 RGB 데이터를 읽고 그레이스케일로 변환
                        int index = y * sourceStride + x * 3;
                        byte r = source[index];
                        byte g = source[index + 1];
                        byte b = source[index + 2];
                        int grayscale = (r + g + b) / 3;

                        // 1bpp 변환: 흰색(255)은 0, 검은색(0)은 1로 변환
                        if (grayscale < 128) // 검은색 처리 (grayscale < 128)
                        {
                            resultPixels[y * resultStride + (x / 8)] |= (byte)(0x80 >> (x % 8)); // 검정 (1)
                        }
                        else
                        {
                            // 흰색 픽셀의 경우 0으로 설정
                            resultPixels[y * resultStride + (x / 8)] &= (byte)~(0x80 >> (x % 8)); // 흰색 (0)
                        }
                    }

                    // 추가된 영역(원본 너비를 초과하는 부분)을 명시적으로 흰색으로 설정

                    if (originalWidth < width)
                    {
                        for (int x = originalWidth; x < width; x++)
                        {
                            resultPixels[y * resultStride + (x / 8)] &= (byte)~(0x80 >> (x % 8)); // 흰색 (0)
                        }
                    }
                }
            }

            // 결과 데이터를 결과 비트맵에 복사
            Marshal.Copy(resultPixels, 0, resultData.Scan0, resultPixels.Length);

            // 잠금 해제
            bitmap.UnlockBits(sourceData);
            result.UnlockBits(resultData);

            // 디버깅용 콘솔 출력 제거

            return result;
        }

        public static byte[] ConvertBitmapToTSPL_(Bitmap originBitmap, int copy = 1, string postPrintCommands = null)
        {
            List<byte> tsplCommandBytes = new List<byte>();

            // 용지 크기 결정
            string widthStr = PRINT_INFO_CLASS.PrinterInfo.Paper_Width.ToString();
            string heightStr = PRINT_INFO_CLASS.PrinterInfo.Paper_Height.ToString();

            // BarcodePrintManager에서 이미 비트맵을 회전시켜서 보내므로 
            // 여기서는 비트맵을 추가로 회전시키지 않음 (중복 회전 방지)
            // 사용자 요청: 회전 기능 적용 시 이미지만 회전되고 용지 사이즈는 변경되지 않아야 함.

            string SIZE = widthStr + " mm," + heightStr + " mm";
            int SPEED = Math.Max(2, Math.Min(6, PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed));
            int DENSITY = Math.Max(1, Math.Min(15, PRINT_INFO_CLASS.PrinterInfo.nDarkness));
            bool ribbon = PRINT_INFO_CLASS.PrinterInfo.bRibbon;
            int GAP = Math.Max(0, PRINT_INFO_CLASS.PrinterInfo.nGAP);
            int offX = PRINT_INFO_CLASS.PrinterInfo.offset_X;
            int offY = PRINT_INFO_CLASS.PrinterInfo.offset_Y;
            int hiddenX = PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X;
            int hiddenY = PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y;

            // 1bpp 비트맵으로 변환 (인쇄 품질 및 명령 호환성)
            using (Bitmap bitmap = ConvertTo1Bpp(originBitmap))
            {
                // TSPL 명령어 헤더 추가
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("CLS\r\n"));
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"SIZE {SIZE}\r\n"));
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"SPEED {SPEED}\r\n"));
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"DENSITY {DENSITY}\r\n"));
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"DIRECTION 1,0\r\n"));

                if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.GAP)
                {
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"GAP {GAP} mm, 0 mm\r\n"));
                }
                else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.BLINE)
                {
                    double blineOff = PRINT_INFO_CLASS.PrinterInfo.BlineOffset;
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"BLINE {GAP} mm, {blineOff} mm\r\n"));
                }
                else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.CONTINEUS)
                {
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"GAP 0 mm, 0 mm\r\n"));
                }

                if (ribbon)
                {
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBON ON\r\n"));
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET ENCODER OFF\r\n"));   // 리본 인코더 에러 센서 비활성화
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBONEND OFF\r\n")); // 리본 엔드 에러 센서 비활성화
                }
                else
                {
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBON OFF\r\n"));
                }

                // REFERENCE (하드웨어 원점 오프셋: 1mm = 8 dots)
                int totalOffX = (offX + hiddenX) * 8;
                int totalOffY = (offY + hiddenY) * 8;
                if (totalOffX != 0 || totalOffY != 0)
                {
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"REFERENCE {totalOffX},{totalOffY}\r\n"));
                }
                else
                {
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal("REFERENCE 0,0\r\n"));
                }

                // 프린터의 Shift 값 초기화
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SHIFT 0\r\n"));

                // 커터, 오프셋 등 후처리/추가 설정 명령어가 있으면 인쇄 전 버퍼에 주입
                if (!string.IsNullOrEmpty(postPrintCommands))
                {
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal(postPrintCommands));
                }

                try
                {
                    // 1bpp 비트맵의 실제 너비(8의 배수로 조정됨)를 사용
                    int widthInBytes = bitmap.Width / 8; // ConvertTo1Bpp에서 이미 8의 배수로 조정됨
                    int heightInPixels = bitmap.Height;

                    // 디버깅: TSPL 명령 정보 출력
                    Console.WriteLine($"TSPL BITMAP 명령: 너비={widthInBytes}바이트, 높이={heightInPixels}픽셀, 실제비트맵너비={bitmap.Width}픽셀, 인쇄매수={copy}");

                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"BITMAP 0,0,{widthInBytes},{heightInPixels},0,"));

                    BitmapData data = bitmap.LockBits(
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format1bppIndexed
                    );

                    int stride = data.Stride;
                    byte[] rawData = new byte[stride * data.Height];
                    Marshal.Copy(data.Scan0, rawData, 0, rawData.Length);
                    bitmap.UnlockBits(data);

                    for (int y = 0; y < heightInPixels; y++)
                    {
                        for (int x = 0; x < widthInBytes; x++)
                        {
                            int index = y * stride + x;
                            byte value = rawData[index];
                            tsplCommandBytes.Add((byte)~value); // 비트 반전 (1, 0 바뀜 문제 해결)
                        }
                    }

                    int safeCopy = Math.Max(1, copy);
                    AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"\r\nPRINT {safeCopy}\r\n"));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to convert bitmap to TSPL commands.", ex);
                }
            }

            return tsplCommandBytes.ToArray();
        }

        public static byte[] BarcodeToTSPL_(string text, int copy = 1)
        {
            return BarcodeToTSPL_(text, "128", copy); // 기본값으로 CODE_128 사용
        }

        public static byte[] BarcodeToTSPL_(string text, string barcodeType, int copy = 1)
        {
            List<byte> tsplCommandBytes = new List<byte>();

            string SIZE = PRINT_INFO_CLASS.PrinterInfo.Paper_Width.ToString() + " mm," + PRINT_INFO_CLASS.PrinterInfo.Paper_Height.ToString() + " mm";
            int SPEED = PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed;
            bool ribbon = PRINT_INFO_CLASS.PrinterInfo.bRibbon;
            int GAP = PRINT_INFO_CLASS.PrinterInfo.nGAP;
            float paperWidthMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
            float paperHeightMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height;

            if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)
            {
                SIZE = PRINT_INFO_CLASS.PrinterInfo.Paper_Height.ToString() + " mm," + PRINT_INFO_CLASS.PrinterInfo.Paper_Width.ToString() + " mm";
                paperWidthMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
                paperHeightMm = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
            }

            // TSPL 명령어 헤더 추가
            AddToByteList(tsplCommandBytes, ConvertToHexadecimal("CLS\r\n"));
            AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"SIZE {SIZE}\r\n"));
            AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"SPEED {SPEED}\r\n"));
            AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"DIRECTION 1\r\n"));

            if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.GAP)
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"GAP {GAP} mm, 0 mm\r\n"));
            }
            else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.BLINE)
            {
                double blineOff = PRINT_INFO_CLASS.PrinterInfo.BlineOffset;
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"BLINE {GAP} mm, {blineOff} mm\r\n"));
            }
            else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.CONTINEUS)
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"GAP 0 mm, 0 mm\r\n"));
            }

            if (ribbon)
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBON ON\r\n"));
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET ENCODER OFF\r\n"));   // 리본 인코더 에러 센서 비활성화
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBONEND OFF\r\n")); // 리본 엔드 에러 센서 비활성화
            }
            else
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBON OFF\r\n"));
            }

            try
            {
                int dpi = (int)PRINT_INFO_CLASS.PrinterInfo.printer_DPI;  // 프린터 DPI

                // CODE128 바코드에서는 문자당 11개의 모듈을 평균적으로 사용합니다.
                int modulePerChar = 11; // CODE128 바코드 문자당 모듈 수 (예시)
                float barcodeWidthMm = text.Length * modulePerChar * 0.2f;  // 한 문자당 평균 0.2mm 정도로 계산 (모듈 크기 고려)
                float barcodeHeightMm = paperHeightMm * 0.6f; // 예시: 바코드 높이 (mm)

                // 용지 크기 (DPI 기반으로 dot 단위로 계산)
                float paperWidthDot = paperWidthMm / 25.4f * dpi;  // mm → inch 변환 후 DPI 적용
                float paperHeightDot = paperHeightMm / 25.4f * dpi;  // mm → inch 변환 후 DPI 적용

                // 바코드 크기 (dot 단위로 변환)
                int barcodeWidthDots = (int)(barcodeWidthMm * dpi / 25.4);
                int barcodeHeightDots = (int)(barcodeHeightMm * dpi / 25.4);

                // 바코드 중앙 위치 계산 (X, Y좌표 모두 중앙 정렬 + 사용자 오프셋 적용)
                int centerX = (int)((paperWidthDot - barcodeWidthDots) / 2) + PRINT_INFO_CLASS.PrinterInfo.offset_X; // X축 중앙 정렬 + 오프셋
                int centerY = (int)((paperHeightDot - barcodeHeightDots) / 2) + PRINT_INFO_CLASS.PrinterInfo.offset_Y; // Y축 중앙 + 오프셋

                // 바코드 타입을 TSPL 형식으로 변환
                string tsplBarcodeType = ConvertBarcodeTypeToTSPL(barcodeType);

                // 바코드 TSPL 명령어 추가 (중앙에 배치)

                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"BARCODE {centerX},{centerY},\"{tsplBarcodeType}\",{barcodeHeightDots},2,0,2,2,\"{text}\"\r\n"));

                // 인쇄 명령어 추가
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"PRINT {copy}\r\n"));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to convert bitmap to TSPL commands.", ex);
            }

            return tsplCommandBytes.ToArray();
        }






        public static byte[] Command_CALIBRATION()
        {
            List<byte> tsplCommandBytes = new List<byte>();

            string SIZE = PRINT_INFO_CLASS.PrinterInfo.Paper_Width.ToString() + " mm," + PRINT_INFO_CLASS.PrinterInfo.Paper_Height.ToString() + " mm";
            int SPEED = PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed;
            bool ribbon = PRINT_INFO_CLASS.PrinterInfo.bRibbon;
            int GAP = PRINT_INFO_CLASS.PrinterInfo.nGAP;
            string COMMAND = "";

            if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)
            {
                SIZE = PRINT_INFO_CLASS.PrinterInfo.Paper_Height.ToString() + " mm," + PRINT_INFO_CLASS.PrinterInfo.Paper_Width.ToString() + " mm";
            }

            //"GAPDETECT" or "BLINEDETECT" or  "AUTODETECT"
            // TSPL 명령어 헤더 추가

            AddToByteList(tsplCommandBytes, ConvertToHexadecimal("CLS\r\n"));

            if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.GAP)
            {
                COMMAND = "GAPDETECT";
            }
            else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.BLINE)
            {
                COMMAND = "BLINEDETECT";
            }
            else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.CONTINEUS)
            {
                COMMAND = "AUTODETECT";
            }

            AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"{COMMAND}\r\n"));

            return tsplCommandBytes.ToArray();
        }

        public static byte[] Command_HOME()
        {
            List<byte> tsplCommandBytes = new List<byte>();

            string SIZE = PRINT_INFO_CLASS.PrinterInfo.Paper_Width.ToString() + " mm," + PRINT_INFO_CLASS.PrinterInfo.Paper_Height.ToString() + " mm";
            int SPEED = PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed;
            bool ribbon = PRINT_INFO_CLASS.PrinterInfo.bRibbon;
            int GAP = PRINT_INFO_CLASS.PrinterInfo.nGAP;
            string COMMAND = "HOME";


            if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)
            {
                SIZE = PRINT_INFO_CLASS.PrinterInfo.Paper_Height.ToString() + " mm," + PRINT_INFO_CLASS.PrinterInfo.Paper_Width.ToString() + " mm";
            }

            //"GAPDETECT" or "BLINEDETECT" or  "AUTODETECT"
            // TSPL 명령어 헤더 추가

            AddToByteList(tsplCommandBytes, ConvertToHexadecimal("CLS\r\n"));

            if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.GAP)
            {
                COMMAND = "GAPDETECT";
            }
            else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.BLINE)
            {
                COMMAND = "BLINEDETECT";
            }
            else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.CONTINEUS)
            {
                COMMAND = "AUTODETECT";
            }

            AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"{COMMAND}\r\n"));

            return tsplCommandBytes.ToArray();
        }

        // 참고 프로젝트에서 추가된 메서드
        public static byte[] Command_INITIALPRINT()
        {
            string command = "CLS\r\nINITIALPRINT\r\n";
            return Encoding.ASCII.GetBytes(command);
        }




        public static System.Drawing.Font AdjustFontSize(int dpi, System.Drawing.Font originFont)
        {
            // 기본 폰트 크기 설정 (예: 12pt)
            float baseFontSize = originFont.Size;
            string baseFontSame = originFont.Name;
            var style = originFont.Style;


            int monitorDPI = 96; // 모니터 DPI
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = graphics.DpiX; // 가로 DPI
                float dpiY = graphics.DpiY; // 세로 DPI

                monitorDPI = (int)dpiX;
            }
            // DPI에 비례하여 폰트 크기 조정
            float adjustedFontSize = baseFontSize * (dpi / monitorDPI);  // 모니터 96 DPI를 기준으로 조정

            // 폰트 크기 조정된 폰트 생성
            System.Drawing.Font adjustedFont = new System.Drawing.Font(baseFontSame, adjustedFontSize, style);

            return adjustedFont;
        }

        private static void AddToByteList(List<byte> byteList, byte[] bytes)
        {
            byteList.AddRange(bytes);
        }

        private static byte[] ConvertToHexadecimal(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        private static string ConvertBarcodeTypeToTSPL(string barcodeType)
        {
            switch (barcodeType)
            {
                case "QR_CODE":
                    return "QRCODE";
                case "CODE_128":
                    return "128";
                case "CODE_39":
                    return "39";
                case "EAN_13":
                    return "EAN13";
                case "EAN_8":
                    return "EAN8";
                case "DATA_MATRIX":
                    return "DATAMATRIX";
                case "PDF_417":
                    return "PDF417";
                default:
                    return "128"; // 기본값
            }
        }
    }
}
