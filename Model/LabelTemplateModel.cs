using System;
using System.Collections.Generic;
using System.IO;
using Argon;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 안드로이드(Label_Master) LabelTemplateModel과 100% 호환되는 라벨 템플릿 모델
    /// </summary>
    [Serializable]
    public class LabelTemplateModel
    {
        public string TemplateId { get; set; } = Guid.NewGuid().ToString();
        public string TemplateName { get; set; } = "신규 라벨 템플릿";
        public double WidthMm { get; set; } = 70.0;
        public double HeightMm { get; set; } = 30.0;
        public int Dpi { get; set; } = 203;

        public string PaperType { get; set; } = "GAP"; // GAP, BLINE, CONTINUOUS
        public double GapHeightMm { get; set; } = 2.0;
        public double BlineOffsetMm { get; set; } = 0.0;
        public int Rotation { get; set; } = 0; // 라벨 전체 회전 (0, 90, 180, 270)

        public List<TemplateElement> Elements { get; set; } = new List<TemplateElement>();

        /// <summary>
        /// 다중 페이지 라벨 지원을 위한 페이지별 요소 목록 (null이거나 1개 이하이면 단일 페이지)
        /// </summary>
        public List<List<TemplateElement>> Pages { get; set; }

        public string ExcelFileName { get; set; }
        public string ExcelFilePath { get; set; }

        // 다중 행·열 라벨지 배열 설정 (예: 2열 롤 라벨지, A4 라벨지 등)
        public int LabelColumns { get; set; } = 1;
        public int LabelRows { get; set; } = 1;
        public double PageMarginLeftMm { get; set; } = 0.0;
        public double PageMarginTopMm { get; set; } = 0.0;
        public double PageMarginRightMm { get; set; } = 0.0;
        public double PageMarginBottomMm { get; set; } = 0.0;
        public double ColumnGapMm { get; set; } = 2.0;
        public double RowGapMm { get; set; } = 2.0;

        public static LabelTemplateModel CreateDefault(double widthMm = 70.0, double heightMm = 30.0)
        {
            var model = new LabelTemplateModel
            {
                WidthMm = widthMm,
                HeightMm = heightMm
            };
            return model;
        }

        public bool SaveToFile(string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LabelTemplateModel] 저장 실패: {ex.Message}");
                return false;
            }
        }

        public static LabelTemplateModel LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                string content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                return JsonConvert.DeserializeObject<LabelTemplateModel>(content);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LabelTemplateModel] 로드 실패: {ex.Message}");
                return null;
            }
        }
    }
}
