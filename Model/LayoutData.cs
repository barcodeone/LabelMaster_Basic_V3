using System;
using System.IO;
using Argon;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 레이아웃 전용 데이터 저장 클래스 (입력 텍스트 제외)
    /// </summary>
    [Serializable]
    public class LayoutData
    {
        // 파일 버전 관리용
        public string Version { get; set; } = "1.0";
        public DateTime SavedDate { get; set; } = DateTime.Now;

        // 설정값 (AppSettings의 주요 데이터)
        public string ColumnPositionData { get; set; } = ""; // 컬럼 위치/크기 설정 문자열
        public string PaperTypeColumnPositionData { get; set; } = ""; // 용지별 컬럼 위치 데이터

        public int TitleSize { get; set; } = 8;
        public int TitleOffsetY { get; set; } = 2;
        public int TitleOffsetX { get; set; } = 0;
        public bool IsTitleEnabled { get; set; } = false;
        public string PaperTypeTitleData { get; set; } = ""; // 용지별 타이틀 데이터

        // 추가 설정
        public string BarcodeType { get; set; } = "QR_CODE";
        public int PaperWidth { get; set; } = 70;
        public int PaperHeight { get; set; } = 28;
        public string PaperTypeName { get; set; } = ""; // 용지 타입 이름 (예: 105x145(블랙마크))
        public bool IsRotated { get; set; } = false;
        public string MultiColumnSettingsData { get; set; } = "";

        // 전역 오프셋 (프린터 설정)
        public int GlobalOffsetX { get; set; } = 0;
        public int GlobalOffsetY { get; set; } = 0;
        public int PaperGap { get; set; } = 2; // 용지 간격 (nGAP)

        // 프린터 세부 설정
        public PrinterSettingsSnapshot PrinterSettings { get; set; } = new PrinterSettingsSnapshot();

        /// <summary>
        /// 레이아웃 파일로 저장
        /// </summary>
        public static bool SaveToFile(LayoutData data, string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LayoutData] 저장 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 레이아웃 파일에서 불러오기
        /// </summary>
        public static LayoutData LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                string content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                return JsonConvert.DeserializeObject<LayoutData>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LayoutData] 로드 실패: {ex.Message}");
                return null;
            }
        }
    }
}
