using System.Collections.Generic;
using BarcodePrint.Model;

namespace BarcodePrint.Services
{
    public interface IExcelService
    {
        bool ExportToExcel(IEnumerable<PrintRecordModel> records, AppSettings settings, string filePath, out string errorMessage);
        bool ImportFromExcel(string filePath, out List<string> headers, out List<PrintRecordModel> records, out string errorMessage);
        List<string> GetSheetNames(string filePath);
        List<string> GetSheetHeaders(string filePath, string sheetName);
        bool ImportFromExcelWithMapping(string filePath, string sheetName, List<int> selectedColIndices, out List<string> selectedHeaders, out List<PrintRecordModel> records, out string errorMessage);
        bool SaveBackup(IEnumerable<PrintRecordModel> records, AppSettings settings, out string timestampedPath, out string latestPath, out string errorMessage);
        List<PrintRecordModel> ImportBackup(string filePath, out string errorMessage);
    }
}
