using BarcodePrint.Model;

namespace BarcodePrint.Services
{
    public interface ITemplateService
    {
        bool ExportTemplate(AppSettings settings, string filePath, out string errorMessage);
        AppSettings ImportTemplate(string filePath, out string errorMessage);

        bool ExportTemplateModel(LabelTemplateModel model, string filePath, out string errorMessage);
        LabelTemplateModel ImportTemplateModel(string filePath, out string errorMessage);
    }
}
