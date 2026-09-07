using System;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 다중 컬럼 데이터 구조
    /// </summary>
    public class MultiColumnData
    {
        public string Value { get; set; }
        public bool IsBarcode { get; set; }
        public string BarcodeType { get; set; }
        public string ColumnName { get; set; }
        public int Size { get; set; } = 50; // 기본 크기
        public int BarcodeHeight { get; set; } = 15; // 바코드 세로 크기 (기본값 15)
        public int OffsetX { get; set; } = 0; // 기본 X 위치
        public int OffsetY { get; set; } = 0; // 기본 Y 위치
        public bool ShowBarcodeText { get; set; } = true; // 바코드 값 표시 여부
        public int BarcodeTextOffsetX { get; set; } = 0; // 바코드 값 텍스트 X 오프셋
        public int BarcodeTextOffsetY { get; set; } = 0; // 바코드 값 텍스트 Y 오프셋
        public int BarcodeTextSize { get; set; } = 0; // 바코드 값 텍스트 크기 (0이면 Size의 60% 또는 8)
        public string FontFamily { get; set; } = "맑은 고딕"; // 폰트 종류
        public bool IsBold { get; set; } = true;            // 굵게 여부
        public int CustomWidth { get; set; } = 0;           // 가로폭 (0=자동)
        public int RotateAngle { get; set; } = 0;          // 회전 각도 (0, 90, 180, 270)
        public string Alignment { get; set; } = "Left";   // 정렬 상태: Left, Center, Right
    }
}
