using BarcodePrint.Model;

namespace BarcodePrint.Services
{
    public interface ITemplateService
    {
        bool ExportTemplate(AppSettings settings, string filePath, out string errorMessage);
        AppSettings ImportTemplate(string filePath, out string errorMessage);
    }
}
