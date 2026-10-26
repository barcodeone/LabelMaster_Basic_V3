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
        /// 실제 표준 바코드 및 QR코드 그래픽 UIElement 생성 (ZXing 기반 정밀 렌더링 & 바코드 위/아래/미표시 텍스트 위치 지원)
        /// </summary>
        public static FrameworkElement CreateBarcodeVisual(string content, string barcodeType, double fontSize, bool isSelected, bool showText = true, double widthPx = 0, double heightPx = 0, string textPosition = "BELOW")
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
                    return CreateQrCodeVisual(content, fontSize, isSelected, showText, widthPx, heightPx, textPosition);
                }
                else if (format == BarcodeFormat.DATA_MATRIX || format == BarcodeFormat.AZTEC || format == BarcodeFormat.PDF_417)
                {
                    return Create2DVisual(content, format, fontSize, isSelected, showText, widthPx, heightPx, textPosition);
                }
                else
                {
                    return Create1DBarcodeVisual(content, format, fontSize, isSelected, showText, widthPx, heightPx, textPosition);
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

        public static BarcodeFormat MapBarcodeFormat(string barcodeType)
        {
            switch (barcodeType?.ToUpper())
            {
                case "QR_CODE":
                case "QR": return BarcodeFormat.QR_CODE;
                case "DATAMATRIX":
                case "DATA_MATRIX": return BarcodeFormat.DATA_MATRIX;
                case "PDF417":
                case "PDF-417": return BarcodeFormat.PDF_417;
                case "AZTEC": return BarcodeFormat.AZTEC;
                case "CODE39":
                case "CODE-39": return BarcodeFormat.CODE_39;
                case "CODE93":
                case "CODE-93": return BarcodeFormat.CODE_93;
                case "EAN13":
                case "EAN-13": return BarcodeFormat.EAN_13;
                case "EAN8":
                case "EAN-8": return BarcodeFormat.EAN_8;
                case "UPC-A":
                case "UPCA": return BarcodeFormat.UPC_A;
                case "UPC-E":
                case "UPCE": return BarcodeFormat.UPC_E;
                case "ITF":
                case "ITF-14": return BarcodeFormat.ITF;
                case "CODABAR": return BarcodeFormat.CODABAR;
                default: return BarcodeFormat.CODE_128;
            }
        }

        private static FrameworkElement CreateQrCodeVisual(string content, double fontSize, bool isSelected, bool showText, double widthPx = 0, double heightPx = 0, string textPosition = "BELOW")
        {
            bool isTextVisible = showText && !string.Equals(textPosition, "NONE", StringComparison.OrdinalIgnoreCase);
            double availableW = (widthPx > 0) ? widthPx : Math.Max(52, fontSize * 4.2);
            double availableH = (heightPx > 0) ? heightPx : availableW;
            double textH = isTextVisible ? Math.Max(12.0, Math.Min(20.0, availableH * 0.22)) : 0.0;
            double displaySize = isTextVisible ? Math.Min(availableW, Math.Max(10.0, availableH - textH)) : Math.Min(availableW, availableH);

            var rootPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = availableW,
                Height = availableH
            };

            var qrWriter = new ZXing.Presentation.BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Width = (int)Math.Max(32, displaySize * 2),
                    Height = (int)Math.Max(32, displaySize * 2),
                    Margin = 1,
                    ErrorCorrection = ErrorCorrectionLevel.M,
                    CharacterSet = "UTF-8"
                }
            };

            Image qrImage = null;
            try
            {
                var bitmapSource = qrWriter.Write(content);
                qrImage = new Image
                {
                    Source = bitmapSource,
                    Width = displaySize,
                    Height = displaySize,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(qrImage, BitmapScalingMode.NearestNeighbor);
            }
            catch { }

            TextBlock textBlock = null;
            if (isTextVisible)
            {
                textBlock = new TextBlock
                {
                    Text = content,
                    FontFamily = new FontFamily("Consolas, Malgun Gothic, Segoe UI"),
                    FontSize = Math.Max(8.0, textH * 0.75),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = string.Equals(textPosition, "ABOVE", StringComparison.OrdinalIgnoreCase)
                        ? new Thickness(0, 0, 0, 2)
                        : new Thickness(0, 2, 0, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = availableW
                };
            }

            if (string.Equals(textPosition, "ABOVE", StringComparison.OrdinalIgnoreCase))
            {
                if (textBlock != null) rootPanel.Children.Add(textBlock);
                if (qrImage != null) rootPanel.Children.Add(qrImage);
            }
            else
            {
                if (qrImage != null) rootPanel.Children.Add(qrImage);
                if (textBlock != null) rootPanel.Children.Add(textBlock);
            }

            return rootPanel;
        }

        private static FrameworkElement Create1DBarcodeVisual(string content, BarcodeFormat format, double fontSize, bool isSelected, bool showText, double widthPx = 0, double heightPx = 0, string textPosition = "BELOW")
        {
            string safeContent = GetSafe1DContent(content, format);
            bool isTextVisible = showText && !string.Equals(textPosition, "NONE", StringComparison.OrdinalIgnoreCase);

            double targetWidth = (widthPx > 0) ? widthPx : Math.Max(80.0, (safeContent?.Length ?? 8) * 11.0);
            double totalHeight = (heightPx > 0) ? heightPx : Math.Max(26.0, fontSize * 2.2);
            double textH = isTextVisible ? Math.Max(12.0, Math.Min(22.0, totalHeight * 0.25)) : 0.0;
            double targetHeight = isTextVisible ? Math.Max(8.0, totalHeight - textH) : totalHeight;

            var rootPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = targetWidth,
                Height = totalHeight
            };

            var writer = new ZXing.Presentation.BarcodeWriter
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Width = (int)Math.Max(60, targetWidth * 2),
                    Height = (int)Math.Max(20, targetHeight * 2),
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
                try
                {
                    writer.Format = BarcodeFormat.CODE_128;
                    bitmapSource = writer.Write("12345678");
                }
                catch { }
            }

            Image barcodeImage = null;
            if (bitmapSource != null)
            {
                barcodeImage = new Image
                {
                    Source = bitmapSource,
                    Width = targetWidth,
                    Height = targetHeight,
                    Stretch = Stretch.Fill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(barcodeImage, BitmapScalingMode.NearestNeighbor);
            }

            TextBlock textBlock = null;
            if (isTextVisible)
            {
                textBlock = new TextBlock
                {
                    Text = content,
                    FontFamily = new FontFamily("Consolas, Malgun Gothic, Segoe UI"),
                    FontSize = Math.Max(8.0, textH * 0.75),
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    TextAlignment = TextAlignment.Center,
                    Margin = string.Equals(textPosition, "ABOVE", StringComparison.OrdinalIgnoreCase)
                        ? new Thickness(0, 0, 0, 1)
                        : new Thickness(0, 1, 0, 0),
                    MaxWidth = targetWidth,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
            }

            if (string.Equals(textPosition, "ABOVE", StringComparison.OrdinalIgnoreCase))
            {
                if (textBlock != null) rootPanel.Children.Add(textBlock);
                if (barcodeImage != null) rootPanel.Children.Add(barcodeImage);
            }
            else
            {
                if (barcodeImage != null) rootPanel.Children.Add(barcodeImage);
                if (textBlock != null) rootPanel.Children.Add(textBlock);
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

                case BarcodeFormat.UPC_E:
                    string digitsUpcE = new string(raw.Where(char.IsDigit).ToArray());
                    if (digitsUpcE.Length == 6 || digitsUpcE.Length == 7 || digitsUpcE.Length == 8) return digitsUpcE;
                    return "01234565";

                case BarcodeFormat.ITF:
                    string digitsItf = new string(raw.Where(char.IsDigit).ToArray());
                    if (digitsItf.Length >= 2)
                    {
                        if (digitsItf.Length % 2 != 0) digitsItf = "0" + digitsItf;
                        return digitsItf;
                    }
                    return "12345678";

                case BarcodeFormat.CODABAR:
                    string clean = new string(raw.Where(c => char.IsDigit(c) || "-$:/.+".Contains(c)).ToArray());
                    if (string.IsNullOrEmpty(clean)) clean = "123456";
                    return "A" + clean.Trim('A', 'B', 'C', 'D') + "B";

                default:
                    return raw;
            }
        }

        private static FrameworkElement Create2DVisual(string content, BarcodeFormat format, double fontSize, bool isSelected, bool showText = true, double widthPx = 0, double heightPx = 0, string textPosition = "BELOW")
        {
            bool isTextVisible = showText && !string.Equals(textPosition, "NONE", StringComparison.OrdinalIgnoreCase);
            double availableW = (widthPx > 0) ? widthPx : Math.Max(52, fontSize * 4.2);
            double availableH = (heightPx > 0) ? heightPx : availableW;
            double textH = isTextVisible ? Math.Max(12.0, Math.Min(20.0, availableH * 0.22)) : 0.0;
            double displaySize = isTextVisible ? Math.Min(availableW, Math.Max(10.0, availableH - textH)) : Math.Min(availableW, availableH);

            var rootPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = availableW,
                Height = availableH
            };

            var writer = new ZXing.Presentation.BarcodeWriter
            {
                Format = format,
                Options = new EncodingOptions
                {
                    Width = (int)Math.Max(32, displaySize * 2),
                    Height = (int)Math.Max(32, displaySize * 2),
                    Margin = 1
                }
            };

            Image image = null;
            try
            {
                string safeContent = string.IsNullOrWhiteSpace(content) ? "12345678" : content;
                var bitmapSource = writer.Write(safeContent);
                image = new Image
                {
                    Source = bitmapSource,
                    Width = displaySize,
                    Height = displaySize,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            }
            catch { }

            TextBlock textBlock = null;
            if (isTextVisible)
            {
                textBlock = new TextBlock
                {
                    Text = content,
                    FontFamily = new FontFamily("Consolas, Malgun Gothic, Segoe UI"),
                    FontSize = Math.Max(8.0, textH * 0.75),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = string.Equals(textPosition, "ABOVE", StringComparison.OrdinalIgnoreCase)
                        ? new Thickness(0, 0, 0, 2)
                        : new Thickness(0, 2, 0, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = availableW
                };
            }

            if (string.Equals(textPosition, "ABOVE", StringComparison.OrdinalIgnoreCase))
            {
                if (textBlock != null) rootPanel.Children.Add(textBlock);
                if (image != null) rootPanel.Children.Add(image);
            }
            else
            {
                if (image != null) rootPanel.Children.Add(image);
                if (textBlock != null) rootPanel.Children.Add(textBlock);
            }

            return rootPanel;
        }

        /// <summary>
        /// 203 DPI / 300 DPI 등 인쇄용 비트맵 생성 시 DrawingContext에 화면과 100% 동일한 고화질 바코드/QR코드 비트맵 및 가독 텍스트 렌더링 (WYSIWYG)
        /// </summary>
        public static void RenderBarcodeOnDc(DrawingContext dc, string content, string barcodeType, double fontSize, double offsetXMm, double offsetYMm, double dotsPerMm, bool showText = true, string textPosition = "BELOW", double widthMm = 0, double heightMm = 0)
        {
            if (string.IsNullOrEmpty(content)) content = "12345678";

            bool isTextVisible = showText && !string.Equals(textPosition, "NONE", StringComparison.OrdinalIgnoreCase);
            bool isAbove = string.Equals(textPosition, "ABOVE", StringComparison.OrdinalIgnoreCase);

            double leftPx = offsetXMm * dotsPerMm;
            double topPx = offsetYMm * dotsPerMm;

            BarcodeFormat format = MapBarcodeFormat(barcodeType);

            if (format == BarcodeFormat.QR_CODE)
            {
                // 화면 미리보기(CreateQrCodeVisual)와 100% 일치하는 정밀 규격 계산
                double defaultMm = 18.0;
                double effWMm = (widthMm > 0) ? widthMm : defaultMm;
                double effHMm = (heightMm > 0) ? heightMm : effWMm;

                double availW = effWMm * dotsPerMm;
                double availH = effHMm * dotsPerMm;
                double textH = isTextVisible ? Math.Max(12.0 * (dotsPerMm / 3.7795), Math.Min(20.0 * (dotsPerMm / 3.7795), availH * 0.22)) : 0.0;
                double displaySize = isTextVisible ? Math.Min(availW, Math.Max(10.0, availH - textH)) : Math.Min(availW, availH);
                double textFontSize = Math.Max(8.0 * (dotsPerMm / 3.7795), textH * 0.75);

                var qrWriter = new ZXing.Presentation.BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Width = (int)Math.Max(32, displaySize * 2),
                        Height = (int)Math.Max(32, displaySize * 2),
                        Margin = 1,
                        ErrorCorrection = ErrorCorrectionLevel.M,
                        CharacterSet = "UTF-8"
                    }
                };

                try
                {
                    var bmp = qrWriter.Write(content);
                    double qrLeftPx = leftPx + Math.Max(0, (availW - displaySize) / 2.0);
                    double qrTopPx = (isTextVisible && isAbove) ? topPx + textH : (topPx + Math.Max(0, (availH - (isTextVisible ? displaySize + textH : displaySize)) / 2.0));
                    dc.DrawImage(bmp, new Rect(qrLeftPx, qrTopPx, displaySize, displaySize));

                    if (isTextVisible)
                    {
                        var ft = new FormattedText(
                            content,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas, 맑은 고딕, Segoe UI"),
                            textFontSize,
                            Brushes.Black,
                            1.0);
                        ft.TextAlignment = TextAlignment.Center;
                        ft.MaxTextWidth = availW;

                        double textTopPx = isAbove ? topPx : (qrTopPx + displaySize + (2.0 * (dotsPerMm / 3.7795)));
                        dc.DrawText(ft, new Point(leftPx, textTopPx));
                    }
                }
                catch { }
            }
            else if (format == BarcodeFormat.DATA_MATRIX || format == BarcodeFormat.AZTEC || format == BarcodeFormat.PDF_417)
            {
                double defaultMm = 18.0;
                double effWMm = (widthMm > 0) ? widthMm : defaultMm;
                double effHMm = (heightMm > 0) ? heightMm : effWMm;
                double availW = effWMm * dotsPerMm;
                double availH = effHMm * dotsPerMm;
                double textH = isTextVisible ? Math.Max(12.0 * (dotsPerMm / 3.7795), Math.Min(20.0 * (dotsPerMm / 3.7795), availH * 0.22)) : 0.0;
                double displaySize = isTextVisible ? Math.Min(availW, Math.Max(10.0, availH - textH)) : Math.Min(availW, availH);
                double textFontSize = Math.Max(8.0 * (dotsPerMm / 3.7795), textH * 0.75);

                var writer = new ZXing.Presentation.BarcodeWriter
                {
                    Format = format,
                    Options = new EncodingOptions
                    {
                        Width = (int)Math.Max(32, displaySize * 2),
                        Height = (int)Math.Max(32, displaySize * 2),
                        Margin = 1
                    }
                };
                try
                {
                    string safeContent = string.IsNullOrWhiteSpace(content) ? "12345678" : content;
                    var bmp = writer.Write(safeContent);
                    double codeLeftPx = leftPx + Math.Max(0, (availW - displaySize) / 2.0);
                    double codeTopPx = (isTextVisible && isAbove) ? topPx + textH : topPx;
                    dc.DrawImage(bmp, new Rect(codeLeftPx, codeTopPx, displaySize, displaySize));

                    if (isTextVisible)
                    {
                        var ft = new FormattedText(
                            content,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas, 맑은 고딕, Segoe UI"),
                            textFontSize,
                            Brushes.Black,
                            1.0);
                        ft.TextAlignment = TextAlignment.Center;
                        ft.MaxTextWidth = availW;

                        double textTopPx = isAbove ? topPx : (codeTopPx + displaySize + (2.0 * (dotsPerMm / 3.7795)));
                        dc.DrawText(ft, new Point(leftPx, textTopPx));
                    }
                }
                catch { }
            }
            else
            {
                // 화면 미리보기(Create1DBarcodeVisual)와 100% 일치하는 정밀 규격 계산
                string safeContent = GetSafe1DContent(content, format);
                double defaultWMm = 45.0;
                double defaultHMm = 15.0;
                double effWMm = (widthMm > 0) ? widthMm : defaultWMm;
                double effHMm = (heightMm > 0) ? heightMm : defaultHMm;

                double totalWidth = effWMm * dotsPerMm;
                double totalHeight = effHMm * dotsPerMm;
                double textH = isTextVisible ? Math.Max(12.0 * (dotsPerMm / 3.7795), Math.Min(22.0 * (dotsPerMm / 3.7795), totalHeight * 0.25)) : 0.0;
                double targetHeight = isTextVisible ? Math.Max(8.0 * (dotsPerMm / 3.7795), totalHeight - textH) : totalHeight;
                double targetWidth = totalWidth;
                double textFontSize = Math.Max(8.0 * (dotsPerMm / 3.7795), textH * 0.75);

                var writer = new ZXing.Presentation.BarcodeWriter
                {
                    Format = format,
                    Options = new EncodingOptions
                    {
                        Width = (int)Math.Max(60, targetWidth),
                        Height = (int)Math.Max(20, targetHeight),
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
                    double barTopPx = (isTextVisible && isAbove) ? topPx + textH : topPx;
                    dc.DrawImage(bmp, new Rect(leftPx, barTopPx, targetWidth, targetHeight));

                    if (isTextVisible)
                    {
                        var ft = new FormattedText(
                            content,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface("Consolas, 맑은 고딕, Segoe UI"),
                            textFontSize,
                            Brushes.Black,
                            1.0);
                        ft.TextAlignment = TextAlignment.Center;
                        ft.MaxTextWidth = targetWidth;

                        double textTopPx = isAbove ? topPx : (barTopPx + targetHeight + (1.5 * (dotsPerMm / 3.7795)));
                        dc.DrawText(ft, new Point(leftPx, textTopPx));
                    }
                }
            }
        }
    }
}
