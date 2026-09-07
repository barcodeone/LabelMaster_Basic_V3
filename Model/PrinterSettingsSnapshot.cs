using System;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 프린터 설정을 스냅샷으로 저장하기 위한 클래스
    /// </summary>
    [Serializable]
    public class PrinterSettingsSnapshot
    {
        public string TargetPrint { get; set; } = "";
        public int PrintCopies { get; set; } = 1;
        public int PrintSpeed { get; set; } = 3;
        public int Darkness { get; set; } = 8;
        public int GAP { get; set; } = 2;
        public bool Ribbon { get; set; } = false;
        public string PaperType { get; set; } = PAPER_TYPE.GAP;
        public bool PaperRotated { get; set; } = false;
        public int PaperWidth { get; set; } = 70;
        public int PaperHeight { get; set; } = 28;
        public int OffsetX { get; set; } = 0;
        public int OffsetY { get; set; } = 0;
        public int CorrectionX { get; set; } = 0;
        public int CorrectionY { get; set; } = 0;
        public int HiddenOffsetX { get; set; } = 0;
        public int HiddenOffsetY { get; set; } = 0;
        public string BarcodeType { get; set; } = "QR_CODE";
        public int BarcodeSize { get; set; } = 50;
        public int BarcodeHeight { get; set; } = 15;
        public int BarcodeX { get; set; } = 0;
        public int BarcodeY { get; set; } = 0;
        public bool ShowBarcodeText { get; set; } = true;

        /// <summary>
        /// PaperClass 객체로부터 스냅샷 생성
        /// </summary>
        public static PrinterSettingsSnapshot FromPaperClass(PaperClass source)
        {
            if (source == null) return new PrinterSettingsSnapshot();

            return new PrinterSettingsSnapshot
            {
                TargetPrint = source.TargetPrint,
                PrintCopies = source.nPrintCopies,
                PrintSpeed = source.nPrintSpeed,
                Darkness = source.nDarkness,
                GAP = source.nGAP,
                Ribbon = source.bRibbon,
                PaperType = source.PaperType,
                PaperRotated = source.bPaperRotated,
                PaperWidth = source.Paper_Width,
                PaperHeight = source.Paper_Height,
                OffsetX = source.offset_X,
                OffsetY = source.offset_Y,
                CorrectionX = source.correction_X,
                CorrectionY = source.correction_Y,
                HiddenOffsetX = source.hidden_Offset_X,
                HiddenOffsetY = source.hidden_Offset_Y,
                BarcodeType = source.BarcodeType,
                BarcodeSize = source.BarcodeSize,
                BarcodeHeight = source.BarcodeHeight,
                BarcodeX = source.BarcodeX,
                BarcodeY = source.BarcodeY,
                ShowBarcodeText = source.ShowBarcodeText
            };
        }

        /// <summary>
        /// 스냅샷 정보를 PaperClass 객체에 적용
        /// </summary>
        public void ApplyTo(PaperClass target)
        {
            if (target == null) return;

            target.TargetPrint = this.TargetPrint;
            target.nPrintCopies = this.PrintCopies;
            target.nPrintSpeed = this.PrintSpeed;
            target.nDarkness = this.Darkness;
            target.nGAP = this.GAP;
            target.bRibbon = this.Ribbon;
            target.PaperType = this.PaperType;
            target.bPaperRotated = this.PaperRotated;
            target.Paper_Width = this.PaperWidth;
            target.Paper_Height = this.PaperHeight;
            target.offset_X = this.OffsetX;
            target.offset_Y = this.OffsetY;
            target.correction_X = this.CorrectionX;
            target.correction_Y = this.CorrectionY;
            target.hidden_Offset_X = this.HiddenOffsetX;
            target.hidden_Offset_Y = this.HiddenOffsetY;
            target.BarcodeType = this.BarcodeType;
            target.BarcodeSize = this.BarcodeSize;
            target.BarcodeHeight = this.BarcodeHeight;
            target.BarcodeX = this.BarcodeX;
            target.BarcodeY = this.BarcodeY;
            target.ShowBarcodeText = this.ShowBarcodeText;
        }
    }
}
