using System;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 메인 뷰의 자주 사용하는 라벨 규격 프리셋 항목
    /// </summary>
    public class PresetLabelItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public double WidthMm { get; set; } = 100;
        public double HeightMm { get; set; } = 100;
        public string Description { get; set; } = "";
        public string ThemeColor { get; set; } = "#2563EB";

        public string DisplaySize => $"{WidthMm:0.##}×{HeightMm:0.##} mm";

        public PresetLabelItem()
        {
        }

        public PresetLabelItem(string id, string title, double widthMm, double heightMm, string description, string themeColor = "#2563EB")
        {
            Id = id;
            Title = title;
            WidthMm = widthMm;
            HeightMm = heightMm;
            Description = description;
            ThemeColor = themeColor;
        }

        public PresetLabelItem Clone()
        {
            return new PresetLabelItem(Id, Title, WidthMm, HeightMm, Description, ThemeColor);
        }
    }
}
