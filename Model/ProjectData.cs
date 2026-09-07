using System;
using System.IO;
using System.Xml.Serialization;
using Argon;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 프로젝트 저장용 데이터 클래스
    /// </summary>
    [Serializable]
    public class ProjectData
    {
        // 파일 버전 관리용
        public string Version { get; set; } = "1.0";
        public DateTime SavedDate { get; set; } = DateTime.Now;

        // 텍스트 입력값
        public string TitleText { get; set; } = "";
        public bool IsTitleEnabled { get; set; } = false;
        public string Column1Text { get; set; } = "";
        public string Column2Text { get; set; } = "";
        public string Column3Text { get; set; } = "";
        public string Column4Text { get; set; } = "";
        public int PrintCopies { get; set; } = 1;

        // 설정값 (AppSettings의 주요 데이터)
        public string ColumnPositionData { get; set; } = ""; // 컬럼 위치/크기 설정 문자열
        public int TitleSize { get; set; } = 8;
        public int TitleOffsetY { get; set; } = 2;
        public int TitleOffsetX { get; set; } = 0;

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
        /// 프로젝트 파일로 저장
        /// </summary>
        public static bool SaveToFile(ProjectData data, string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectData] 저장 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 프로젝트 파일에서 불러오기
        /// </summary>
        public static ProjectData LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                string content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                
                // JSON 형식인지 체크 (구버전 XML 호환성)
                if (content.TrimStart().StartsWith("{"))
                {
                    return JsonConvert.DeserializeObject<ProjectData>(content);
                }
                else
                {
                    // 레거시 XML 포맷으로 파싱 시도 (Fallback)
                    System.Diagnostics.Debug.WriteLine("[ProjectData] 레거시 XML 포맷 감지, Fallback 로드 시도");
                    XmlSerializer serializer = new XmlSerializer(typeof(ProjectData));
                    using (StringReader reader = new StringReader(content))
                    {
                        return (ProjectData)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProjectData] 로드 실패: {ex.Message}");
                
                // 파싱 오류 발생 시 마지막 수단으로 기존 XML 형식으로 재시도
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ProjectData));
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        return (ProjectData)serializer.Deserialize(fileStream);
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProjectData] 레거시 Fallback 로드도 실패: {fallbackEx.Message}");
                    return null;
                }
            }
        }
    }
}
