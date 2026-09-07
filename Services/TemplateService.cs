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
    }
}
