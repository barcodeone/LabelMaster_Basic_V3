using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace BarcodePrint.Model
{
    public static class BarcodeVisualHelper
    {
        /// <summary>
        /// 실제 표준 바코드 및 QR코드 그래픽 UIElement 생성 (ZXing 기반 정밀 렌더링 & 하단 텍스트 중앙정렬)
        /// </summary>
        public static FrameworkElement CreateBarcodeVisual(string content, string barcodeType, double fontSize, bool isSelected, bool showText = true)
        {
            if (string.IsNullOrEmpty(content))
            {
                content = "12345678";
            }

            try
            {
                BarcodeFormat format = MapBarcodeFormat(barcodeType);

                if (format == BarcodeFormat.QR_CODE)
                {
                    return CreateQrCodeVisual(content, fontSize, isSelected, showText);
                }
                else if (format == BarcodeFormat.DATA_MATRIX)
                {
                    return Create2DVisual(content, format, fontSize, isSelected);
                }
                else
                {
                    return Create1DBarcodeVisual(content, format, fontSize, isSelected, showText);
                }
            }
            catch
            {
                // 오류 발생 시 안전한 기본 텍스트 반환
                return new TextBlock
                {
                    Text = $"[{content}]",
                    FontSize = fontSize,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black
                };
            }
        }

        private static BarcodeFormat MapBarcodeFormat(string barcodeType)
        {
            switch (barcodeType?.ToUpper())
            {
                case "QR_CODE": return BarcodeFormat.QR_CODE;
                case "DATAMATRIX":
                case "DATA_MATRIX": return BarcodeFormat.DATA_MATRIX;
                case "PDF417": return BarcodeFormat.PDF_417;
                case "CODE39": return BarcodeFormat.CODE_39;
                case "CODE93": return BarcodeFormat.CODE_93;
                case "EAN13": return BarcodeFormat.EAN_13;
                case "EAN8": return BarcodeFormat.EAN_8;
                case "UPC-A":
                case "UPCA": return BarcodeFormat.UPC_A;
                case "UPC-E":
                case "UPCE": return BarcodeFormat.UPC_E;
                case "ITF": return BarcodeFormat.ITF;
                case "CODABAR": return BarcodeFormat.CODABAR;
                default: return BarcodeFormat.CODE_128;
            }
        }

        private static FrameworkElement CreateQrCodeVisual(string content, double fontSize, bool isSelected, bool showText)
        {
            double displaySize = Math.Max(52, fontSize * 4.2);

            var rootPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = displaySize
            };

            // ZXing QR 코드 비트맵 생성 (여백 0, 표준 오류 복원 레벨 M)
            var qrWriter = new ZXing.Presentation.BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = (int)(displaySize * 2),
                    Height = (int)(displaySize * 2),
                    Margin = 1,
                    ErrorCorrection = ErrorCorrectionLevel.M,
                    CharacterSet = "UTF-8"
                }
            };

            var bitmapSource = qrWriter.Write(content);

            var qrImage = new Image
            {
                Source = bitmapSource,
                Width = displaySize,
                Height = displaySize,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(qrImage, BitmapScalingMode.NearestNeighbor);

            rootPanel.Children.Add(qrImage);

            if (showText)
            {
                var textBlock = new TextBlock
                {
                    Text = content,
                    FontFamily = new FontFamily("Consolas, Malgun Gothic, Segoe UI"),
                    FontSize = Math.Max(8.5, fontSize * 0.75),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = displaySize + 30
                };
                rootPanel.Children.Add(textBlock);
            }

            return rootPanel;
        }

        private static FrameworkElement Create1DBarcodeVisual(string content, BarcodeFormat format, double fontSize, bool isSelected, bool showText)
        {
            string safeContent = GetSafe1DContent(content, format);

            // 1D 바코드 높이 및 너비 계산
            double targetHeight = Math.Max(26, fontSize * 2.2);
            double targetWidth = Math.Max(80, (safeContent?.Length ?? 8) * 11);

            var rootPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = targetWidth
            };

            // ZXing을 통해 바코드 흑백 이미지 생성 (여백 0으로 바코드가 전체 폭을 꽉 채움)
            var writer = new ZXing.Presentation.BarcodeWriter
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Width = (int)(targetWidth * 2),
                    Height = (int)(targetHeight * 2),
                    Margin = 0,
                    PureBarcode = true
                }
            };

            WriteableBitmap bitmapSource = null;
            try
            {
                bitmapSource = writer.Write(safeContent);
            }
            catch
            {
                // 실패 시 가장 안전한 CODE128 + 12345678 로 비트맵 생성
                try
                {
                    writer.Format = BarcodeFormat.CODE_128;
                    bitmapSource = writer.Write("12345678");
                }
                catch { }
            }

            if (bitmapSource != null)
            {
                var barcodeImage = new Image
                {
                    Source = bitmapSource,
                    Width = targetWidth,
                    Height = targetHeight,
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(barcodeImage, BitmapScalingMode.NearestNeighbor);
                rootPanel.Children.Add(barcodeImage);
            }

            // 하단 가독 텍스트: 사용자가 입력한 원래의 content를 정중앙(TextAlignment.Center) 정렬
            if (showText)
            {
                var textBlock = new TextBlock
                {
                    Text = content,
                    FontFamily = new FontFamily("Consolas, Malgun Gothic, Segoe UI"),
                    FontSize = Math.Max(9, fontSize * 0.85),
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                rootPanel.Children.Add(textBlock);
            }

            return rootPanel;
        }

        private static string GetSafe1DContent(string raw, BarcodeFormat format)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "12345678";

            switch (format)
            {
                case BarcodeFormat.EAN_13:
                    string digits13 = new string(raw.Where(char.IsDigit).ToArray());
                    if (digits13.Length == 12 || digits13.Length == 13) return digits13;
                    return "8801234567893";

                case BarcodeFormat.EAN_8:
                    string digits8 = new string(raw.Where(char.IsDigit).ToArray());
                    if (digits8.Length == 7 || digits8.Length == 8) return digits8;
                    return "12345670";

                case BarcodeFormat.UPC_A:
                    string digitsUpc = new string(raw.Where(char.IsDigit).ToArray());
                    if (digitsUpc.Length == 11 || digitsUpc.Length == 12) return digitsUpc;
                    return "012345678905";

                case BarcodeFormat.ITF:
                    string digitsItf = new string(raw.Where(char.IsDigit).ToArray());
                    if (digitsItf.Length > 0 && digitsItf.Length % 2 == 0) return digitsItf;
                    return "12345678";

                case BarcodeFormat.CODE_39:
                    // CODE39 지원 문자: 대문자, 숫자, - . $ / + % 공백
                    string upper = raw.ToUpper();
                    bool allValid39 = upper.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || "-.$/+% ".Contains(c));
                    return allValid39 ? upper : "12345678";

                case BarcodeFormat.CODE_93:
                case BarcodeFormat.CODE_128:
                default:
                    // ASCII 범위(0~127)만 지원
                    bool allAscii = raw.All(c => c < 128);
                    return allAscii ? raw : "12345678";
            }
        }

        private static FrameworkElement Create2DVisual(string content, BarcodeFormat format, double fontSize, bool isSelected)
        {
            double displaySize = Math.Max(48, fontSize * 3.6);

            var rootPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = displaySize
            };

            var writer = new ZXing.Presentation.BarcodeWriter
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Width = (int)(displaySize * 2),
                    Height = (int)(displaySize * 2),
                    Margin = 1
                }
            };

            try
            {
                string safeContent = string.IsNullOrWhiteSpace(content) ? "12345678" : content;
                var bitmapSource = writer.Write(safeContent);
                var image = new Image
                {
                    Source = bitmapSource,
                    Width = displaySize,
                    Height = displaySize,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                rootPanel.Children.Add(image);
            }
            catch { }

            return rootPanel;
        }

        /// <summary>
        /// 203 DPI 등 인쇄용 비트맵 생성 시 DrawingContext에 화면과 100% 동일한 고화질 바코드/QR코드 비트맵 및 가독 텍스트 렌더링 (WYSIWYG)
        /// </summary>
        public static void RenderBarcodeOnDc(DrawingContext dc, string content, string barcodeType, double fontSize, double offsetXMm, double offsetYMm, double dotsPerMm, bool showText = true)
        {
            if (string.IsNullOrEmpty(content)) content = "12345678";

            // 화면 96 DPI(1mm=3.7795px) 기준 수치를 인쇄 해상도(1mm=dotsPerMm px)로 비례 환산
            double scale = dotsPerMm / 3.7795;
            double leftPx = offsetXMm * dotsPerMm;
            double topPx = offsetYMm * dotsPerMm;

            BarcodeFormat format = MapBarcodeFormat(barcodeType);

            if (format == BarcodeFormat.QR_CODE)
            {
                double displaySize = Math.Max(52, fontSize * 4.2) * scale;

                var qrWriter = new ZXing.Presentation.BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Width = (int)displaySize,
                        Height = (int)displaySize,
                        Margin = 1,
                        ErrorCorrection = ErrorCorrectionLevel.M,
                        CharacterSet = "UTF-8"
                    }
                };

                try
                {
                    var bmp = qrWriter.Write(content);
                    dc.DrawImage(bmp, new Rect(leftPx, topPx, displaySize, displaySize));

                    if (showText)
                    {
                        double textFontSize = Math.Max(8.5, fontSize * 0.75) * scale;
                        var ft = new FormattedText(
                            content,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas"),
                            textFontSize,
                            Brushes.Black,
                            1.0);
                        ft.TextAlignment = TextAlignment.Center;
                        ft.MaxTextWidth = displaySize;
                        dc.DrawText(ft, new Point(leftPx, topPx + displaySize + (2 * scale)));
                    }
                }
                catch { }
            }
            else if (format == BarcodeFormat.DATA_MATRIX)
            {
                double displaySize = Math.Max(48, fontSize * 3.6) * scale;
                var writer = new ZXing.Presentation.BarcodeWriter
                {
                    Format = format,
                    Options = new EncodingOptions
                    {
                        Width = (int)displaySize,
                        Height = (int)displaySize,
                        Margin = 1
                    }
                };
                try
                {
                    string safeContent = string.IsNullOrWhiteSpace(content) ? "12345678" : content;
                    var bmp = writer.Write(safeContent);
                    dc.DrawImage(bmp, new Rect(leftPx, topPx, displaySize, displaySize));
                }
                catch { }
            }
            else
            {
                string safeContent = GetSafe1DContent(content, format);
                double targetHeight = Math.Max(26, fontSize * 2.2) * scale;
                double targetWidth = Math.Max(80, (safeContent?.Length ?? 8) * 11) * scale;

                var writer = new ZXing.Presentation.BarcodeWriter
                {
                    Format = format,
                    Options = new EncodingOptions
                    {
                        Width = (int)targetWidth,
                        Height = (int)targetHeight,
                        Margin = 0,
                        PureBarcode = true
                    }
                };

                WriteableBitmap bmp = null;
                try
                {
                    bmp = writer.Write(safeContent);
                }
                catch
                {
                    try
                    {
                        writer.Format = BarcodeFormat.CODE_128;
                        bmp = writer.Write("12345678");
                    }
                    catch { }
                }

                if (bmp != null)
                {
                    dc.DrawImage(bmp, new Rect(leftPx, topPx, targetWidth, targetHeight));

                    if (showText)
                    {
                        double textFontSize = Math.Max(9, fontSize * 0.85) * scale;
                        var ft = new FormattedText(
                            content,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas"),
                            textFontSize,
                            Brushes.Black,
                            1.0);
                        ft.TextAlignment = TextAlignment.Center;
                        ft.MaxTextWidth = targetWidth;
                        dc.DrawText(ft, new Point(leftPx, topPx + targetHeight + (2 * scale)));
                    }
                }
            }
        }
    }
}
