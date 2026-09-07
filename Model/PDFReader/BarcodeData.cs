using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BarcodePrint.Model
{
    public class BarcodeData
    {
        public string BarcodeText { get; set; }
        public string ItemText { get; set; } // 참고 프로젝트에서 추가된 속성
        public RectangleF Position { get; set; }
        public Bitmap BarcodeImage { get; set; } // 참고 프로젝트에서 추가된 속성
        public string BarcodeType { get; set; } = "QR_CODE"; // 기본값
        public int BarcodeSize { get; set; } = 50; // 바코드 크기
        public int BarcodeHeight { get; set; } = 50; // 바코드 세로 크기
        public int BarcodeX { get; set; } = 0; // 바코드 X 위치
        public int BarcodeY { get; set; } = 0; // 바코드 Y 위치
        public int copyCount { get; set; } = 1; // 참고 프로젝트에서 추가된 속성
        public bool ShowBarcodeText { get; set; } = true; // 바코드 하단 텍스트 표시 여부
        public int BarcodeTextSize { get; set; } = 8; // 바코드 하단 텍스트 크기

        // 다중 컬럼 지원
        public List<MultiColumnData> MultiColumnData { get; set; } // 다중 컬럼 데이터
        public bool IsMultiColumn => MultiColumnData != null && MultiColumnData.Count > 0; // 다중 컬럼 모드 여부
    }
}
