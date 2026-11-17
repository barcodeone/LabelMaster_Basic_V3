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
        /// <summary>
        /// Bitmap 이미지를 TSPL 1-Bit 흑백 라스터 바이너리 데이터로 직접 변환 (안드로이드/Mac 표준 엔진과 100% 동일)
        /// TSPL BITMAP 형식: 0 = 검은색(인쇄 도트), 1 = 흰색(공백 도트)
        /// 알파 채널(투명도)을 정확하게 처리하여 특정 PC에서 투명 픽셀이 검정/흰색으로 꼬이는 버그를 원천 차단
        /// </summary>
        public static byte[] ConvertBitmapToTsplRaster(Bitmap bitmap, out int widthBytes, out int heightDots)
        {
            if (bitmap == null)
            {
                widthBytes = 0;
                heightDots = 0;
                return new byte[0];
            }

            int originalWidth = bitmap.Width;
            int height = bitmap.Height;
            widthBytes = (originalWidth + 7) / 8; // 8픽셀당 1바이트
            heightDots = height;

            byte[] rasterData = new byte[widthBytes * height];
            // 기본 전체를 0xFF (1 = 흰색/미인쇄)로 초기화하여 우측 여백 패딩을 깨끗한 공백으로 보장
            for (int i = 0; i < rasterData.Length; i++)
            {
                rasterData[i] = 0xFF;
            }

            // 32bpp ARGB로 잠그면 GDI+가 내부 포맷(24bpp, 32bpp 등)에 상관없이 32비트 BGRA로 일관되게 제공
            BitmapData srcData = bitmap.LockBits(
                new Rectangle(0, 0, originalWidth, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb
            );

            try
            {
                int stride = srcData.Stride;
                IntPtr ptr = srcData.Scan0;

                unsafe
                {
                    byte* p = (byte*)ptr;

                    for (int y = 0; y < height; y++)
                    {
                        byte* row = p + (y * stride);
                        for (int x = 0; x < originalWidth; x++)
                        {
                            int pixelOffset = x * 4;
                            byte b = row[pixelOffset];
                            byte g = row[pixelOffset + 1];
                            byte r = row[pixelOffset + 2];
                            byte a = row[pixelOffset + 3];

                            // 투명 픽셀(Alpha < 128)은 라벨 용지 배경(흰색)이므로 무조건 도트 오프(흰색 유지)
                            if (a < 128)
                            {
                                continue;
                            }

                            // 표준 ITU-R BT.601 흑백 휘도 계산
                            int luminance = (int)(r * 0.299 + g * 0.587 + b * 0.114);

                            // 휘도 128 미만이면 검은색 도트 (TSPL: 0=검정, 1=흰색)
                            if (luminance < 128)
                            {
                                int byteIndex = y * widthBytes + (x / 8);
                                int bitIndex = 7 - (x % 8); // MSB First
                                rasterData[byteIndex] &= (byte)~(1 << bitIndex);
                            }
                        }
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(srcData);
            }

            return rasterData;
        }

        // 비트맵을 1bppIndexed로 변환 (기존 레거시 호출 호환성 유지)
        public static Bitmap ConvertTo1Bpp(Bitmap bitmap)
        {
            int originalWidth = bitmap.Width;
            int width = (bitmap.Width + 7) / 8 * 8; // 너비를 8의 배수로 조정
            int height = bitmap.Height;

            Bitmap result = new Bitmap(width, height, PixelFormat.Format1bppIndexed);

            byte[] rasterData = ConvertBitmapToTsplRaster(bitmap, out int widthBytes, out int heightDots);

            BitmapData resultData = result.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format1bppIndexed
            );

            try
            {
                int resultStride = resultData.Stride;
                byte[] resultPixels = new byte[resultStride * height];

                // 1bppIndexed 기본 팔레트에서 1=검정, 0=흰색이므로 rasterData(0=검정, 1=흰색)를 비트 반전하여 복사
                for (int y = 0; y < height; y++)
                {
                    for (int bx = 0; bx < widthBytes; bx++)
                    {
                        byte val = rasterData[y * widthBytes + bx];
                        resultPixels[y * resultStride + bx] = (byte)~val;
                    }
                }

                Marshal.Copy(resultPixels, 0, resultData.Scan0, resultPixels.Length);
            }
            finally
            {
                result.UnlockBits(resultData);
            }

            return result;
        }

        public static byte[] ConvertBitmapToTSPL_(Bitmap originBitmap, int copy = 1, string postPrintCommands = null)
        {
            List<byte> tsplCommandBytes = new List<byte>();

            // 용지 크기 결정 (0 이하 기본값 방어 및 InvariantCulture 포맷팅)
            float paperW = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Width;
            float paperH = (float)PRINT_INFO_CLASS.PrinterInfo.Paper_Height;
            if (paperW <= 0) paperW = 80f;
            if (paperH <= 0) paperH = 60f;

            string SIZE = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.#} mm,{1:0.#} mm", paperW, paperH);
            int SPEED = Math.Max(2, Math.Min(8, PRINT_INFO_CLASS.PrinterInfo.nPrintSpeed));
            int DENSITY = Math.Max(1, Math.Min(15, PRINT_INFO_CLASS.PrinterInfo.nDarkness));
            bool ribbon = PRINT_INFO_CLASS.PrinterInfo.bRibbon;
            int GAP = Math.Max(0, PRINT_INFO_CLASS.PrinterInfo.nGAP);
            int offX = PRINT_INFO_CLASS.PrinterInfo.offset_X;
            int offY = PRINT_INFO_CLASS.PrinterInfo.offset_Y;
            int hiddenX = PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_X;
            int hiddenY = PRINT_INFO_CLASS.PrinterInfo.hidden_Offset_Y;

            // TSPL 명령어 헤더 추가 (SIZE, SPEED, DENSITY, DIRECTION 설정)
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

            // TSPL 매뉴얼 20p 규격: SIZE 및 DIRECTION 설정 후 CLS로 버퍼를 완벽하게 재할당 및 초기화
            AddToByteList(tsplCommandBytes, ConvertToHexadecimal("CLS\r\n"));
            // 한글 및 다국어 인쇄 지원을 위한 코드페이지 설정
            AddToByteList(tsplCommandBytes, ConvertToHexadecimal("CODEPAGE 949\r\n"));

            try
            {
                // 비트맵에서 직접 1-Bit TSPL 라스터 바이트 추출 (GDI+ 1bpp 변환 생략으로 특정 PC 렌더링/투명도 결함 원천 방지)
                byte[] rasterData = ConvertBitmapToTsplRaster(originBitmap, out int widthInBytes, out int heightInPixels);

                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"BITMAP 0,0,{widthInBytes},{heightInPixels},0,"));
                tsplCommandBytes.AddRange(rasterData);

                // TSPL 매뉴얼 공식 규격: PRINT m,n (m세트, 세트당 n매)
                // 일부 프린터 모델/펌웨어에서 PRINT 1 단독 전달 시 매수 생략으로 빈 페이지만 배출(Feed)하는 결함 원천 방지
                int safeCopy = Math.Max(1, copy);
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"\r\nPRINT {safeCopy},1\r\n"));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to convert bitmap to TSPL commands.", ex);
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

            // TSPL 명령어 헤더 추가 (SIZE, SPEED, DIRECTION 설정)
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

            // TSPL 매뉴얼 20p 규격: SIZE 및 DIRECTION 설정 후 CLS로 버퍼 재할당 및 초기화
            AddToByteList(tsplCommandBytes, ConvertToHexadecimal("CLS\r\n"));

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

            AddToByteList(tsplCommandBytes, ConvertToHexadecimal("CLS\r\n"));

            // [열전사 리본 연동] IsRibbon 설정에 따라 리본 모터 동기화 및 센서 오작동 방지 명령 주입
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
            else
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
            bool ribbon = PRINT_INFO_CLASS.PrinterInfo.bRibbon;
            int GAP = PRINT_INFO_CLASS.PrinterInfo.nGAP;

            if (PRINT_INFO_CLASS.PrinterInfo.bPaperRotated)
            {
                SIZE = PRINT_INFO_CLASS.PrinterInfo.Paper_Height.ToString() + " mm," + PRINT_INFO_CLASS.PrinterInfo.Paper_Width.ToString() + " mm";
            }

            AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"SIZE {SIZE}\r\n"));

            if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.BLINE)
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"BLINE {GAP} mm,0 mm\r\n"));
            }
            else if (PRINT_INFO_CLASS.PrinterInfo.PaperType == PAPER_TYPE.CONTINEUS)
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("GAP 0,0\r\n"));
            }
            else
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal($"GAP {GAP} mm,0 mm\r\n"));
            }

            // [열전사 리본 연동]
            if (ribbon)
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBON ON\r\n"));
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET ENCODER OFF\r\n"));
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBONEND OFF\r\n"));
            }
            else
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBON OFF\r\n"));
            }

            AddToByteList(tsplCommandBytes, ConvertToHexadecimal("HOME\r\nCLS\r\n"));

            return tsplCommandBytes.ToArray();
        }

        public static byte[] Command_FEED()
        {
            List<byte> tsplCommandBytes = new List<byte>();
            bool ribbon = PRINT_INFO_CLASS.PrinterInfo.bRibbon;

            AddToByteList(tsplCommandBytes, ConvertToHexadecimal("CLS\r\n"));

            if (ribbon)
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBON ON\r\n"));
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET ENCODER OFF\r\n"));
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBONEND OFF\r\n"));
            }
            else
            {
                AddToByteList(tsplCommandBytes, ConvertToHexadecimal("SET RIBBON OFF\r\n"));
            }

            AddToByteList(tsplCommandBytes, ConvertToHexadecimal("FORMFEED\r\n"));

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
