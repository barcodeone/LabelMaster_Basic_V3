using System;
using System.IO;
using System.Text;
using Argon;
using BarcodePrint.Model;

namespace BarcodePrint.Services
{
    public class TemplateService : ITemplateService
    {
        public bool ExportTemplate(AppSettings settings, string filePath, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(filePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public AppSettings ImportTemplate(string filePath, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                if (!File.Exists(filePath))
                {
                    errorMessage = "지정된 템플릿 파일이 존재하지 않습니다.";
                    return null;
                }

                string json = File.ReadAllText(filePath, Encoding.UTF8);
                var loaded = JsonConvert.DeserializeObject<AppSettings>(json);
                if (loaded == null)
                {
                    errorMessage = "템플릿 파일을 역직렬화할 수 없습니다.";
                    return null;
                }

                return loaded;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return null;
            }
        }

        public bool ExportTemplateModel(LabelTemplateModel model, string filePath, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                if (model == null)
                {
                    errorMessage = "저장할 템플릿 모델이 비어있습니다.";
                    return false;
                }
                string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                AppDataPathHelper.WriteAllTextAtomic(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public LabelTemplateModel ImportTemplateModel(string filePath, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                if (!File.Exists(filePath))
                {
                    errorMessage = "지정된 템플릿 파일이 존재하지 않습니다.";
                    return null;
                }

                string json = File.ReadAllText(filePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json))
                {
                    errorMessage = "템플릿 파일 내용이 비어있습니다.";
                    return null;
                }

                // 1. 신규 LabelTemplateModel 역직렬화 시도
                if (json.Contains("\"Elements\"") || json.Contains("\"WidthMm\""))
                {
                    var model = JsonConvert.DeserializeObject<LabelTemplateModel>(json);
                    if (model != null && model.Elements != null)
                    {
                        return model;
                    }
                }

                // 2. 구버전 AppSettings 포맷 역직렬화 시도 및 변환
                var legacySettings = JsonConvert.DeserializeObject<AppSettings>(json);
                if (legacySettings != null)
                {
                    double w = legacySettings.PaperWidth > 0 ? legacySettings.PaperWidth : 70.0;
                    double h = legacySettings.PaperHeight > 0 ? legacySettings.PaperHeight : 30.0;
                    var converted = LabelTemplateModel.CreateDefault(w, h);
                    converted.Rotation = legacySettings.LabelRotationAngle;
                    // 기본 텍스트 요소 추가
                    var defElem = TemplateMigrationHelper.CreateElement(ElementType.TEXT, 5, 5);
                    defElem.Content = !string.IsNullOrEmpty(legacySettings.LastProductName) ? legacySettings.LastProductName : "텍스트";
                    converted.Elements.Add(defElem);
                    return converted;
                }

                errorMessage = "템플릿 형식을 인식할 수 없습니다.";
                return null;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return null;
            }
        }
    }
}
