using System;
using System.Collections.Generic;
using System.Linq;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 기존 8열 기반 LayoutData / ViewModel 속성과 신규 LabelTemplateModel 간의 상호 변환 헬퍼
    /// </summary>
    public static class TemplateMigrationHelper
    {
        /// <summary>
        /// ViewModel의 현재 8열 설정값을 LabelTemplateModel로 변환
        /// </summary>
        public static LabelTemplateModel ExportFromViewModel(
            double paperWidth, double paperHeight, int rotation,
            int activeColCount,
            Func<int, string> getHeader,
            Func<int, string> getInput,
            Func<int, int> getFontSize,
            Func<int, string> getFontName,
            Func<int, bool> getBold,
            Func<int, bool> getItalic,
            Func<int, string> getAlign,
            Func<int, bool> getBarcode,
            Func<int, string> getBarcodeType,
            Func<int, int> getOffsetX,
            Func<int, int> getOffsetY,
            Func<int, int> getColWidth,
            Func<int, bool> getIncludeHeader,
            Func<int, bool> getPriceFormat,
            Func<int, bool> getKeep)
        {
            var template = new LabelTemplateModel
            {
                WidthMm = paperWidth,
                HeightMm = paperHeight,
                Rotation = rotation,
                Elements = new List<TemplateElement>()
            };

            for (int i = 1; i <= activeColCount; i++)
            {
                bool isBc = getBarcode(i);
                var elem = new TemplateElement
                {
                    InputOrder = i,
                    HeaderName = getHeader(i),
                    Content = getInput(i) ?? "",
                    FontSize = getFontSize(i),
                    FontName = getFontName(i) ?? "맑은 고딕",
                    IsBold = getBold(i),
                    IsItalic = getItalic(i),
                    Alignment = getAlign(i) ?? "LEFT",
                    Type = isBc ? (getBarcodeType(i) == "QR_CODE" ? ElementType.QR_CODE : ElementType.BARCODE) : ElementType.TEXT,
                    BarcodeType = getBarcodeType(i) ?? "CODE128",
                    X = getOffsetX(i),
                    Y = getOffsetY(i),
                    Width = getColWidth(i) > 0 ? getColWidth(i) : (isBc ? 40.0 : 0.0),
                    Height = isBc ? 15.0 : 0.0,
                    IncludeHeaderOnLabel = getIncludeHeader(i),
                    IsPriceFormat = getPriceFormat(i),
                    IsKeep = getKeep(i),
                    IsVariable = true
                };
                template.Elements.Add(elem);
            }

            return template;
        }

        /// <summary>
        /// 신규 요소를 템플릿에 추가할 때 기본값 생성
        /// </summary>
        public static TemplateElement CreateElement(ElementType type, double xMm, double yMm, string initialContent = "")
        {
            var elem = new TemplateElement
            {
                Type = type,
                X = xMm,
                Y = yMm,
                IsVariable = (type == ElementType.TEXT || type == ElementType.BARCODE || type == ElementType.QR_CODE)
            };

            switch (type)
            {
                case ElementType.TEXT:
                    elem.Content = !string.IsNullOrEmpty(initialContent) ? initialContent : "텍스트";
                    elem.Width = 0.0; // 0: 텍스트 실제 크기에 100% 자동 맞춤
                    elem.Height = 0.0;
                    elem.FontSize = 10.0;
                    elem.HeaderName = "텍스트 항목";
                    break;
                case ElementType.BARCODE:
                    elem.Content = !string.IsNullOrEmpty(initialContent) ? initialContent : "12345678";
                    elem.BarcodeType = "CODE128";
                    elem.Width = 45.0;
                    elem.Height = 15.0;
                    elem.HeaderName = "바코드 항목";
                    break;
                case ElementType.QR_CODE:
                    elem.Content = !string.IsNullOrEmpty(initialContent) ? initialContent : "https://example.com";
                    elem.BarcodeType = "QR_CODE";
                    elem.Width = 18.0;
                    elem.Height = 18.0;
                    elem.HeaderName = "QR코드 항목";
                    break;
                case ElementType.LINE:
                    elem.Width = 40.0;
                    elem.Height = 1.0;
                    elem.LineWidth = 1.0f;
                    elem.IsVariable = false;
                    break;
                case ElementType.SHAPE:
                    elem.ShapeType = "RECTANGLE";
                    elem.Width = 25.0;
                    elem.Height = 25.0; // 1:1 비율 기본 생성
                    elem.LineWidth = 1.0f;
                    elem.IsVariable = false;
                    break;
                case ElementType.TABLE:
                    elem.Rows = 2;
                    elem.Columns = 2;
                    elem.Width = 50.0;
                    elem.Height = 18.0;
                    elem.TableData = new string[][] { new[] { "항목", "내용" }, new[] { "품명", "" } };
                    elem.IsVariable = false;
                    break;
                case ElementType.DATE_TIME:
                    elem.DateTimeFormat = "yyyy-MM-dd HH:mm";
                    elem.AutoUpdateDate = true;
                    elem.Content = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    elem.Width = 35.0;
                    elem.Height = 7.0;
                    elem.FontSize = 9.0;
                    elem.HeaderName = "일시 항목";
                    break;
            }

            return elem;
        }
    }
}
