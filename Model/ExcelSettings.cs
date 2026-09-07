using System;
using System.IO;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 엑셀 연동 설정을 저장하고 로드하는 클래스
    /// </summary>
    public class ExcelSettings
    {
        // 암호화된 파일 경로 (압축으로 유추 불가능)
        private static readonly string SettingsFilePath = SecureFileNameHelper.GetSecureFilePath("excel_settings");

        public string ExcelFilePath { get; set; } = "";
        public bool IsExcelModeEnabled { get; set; } = false;
        public string LastUsedFolder { get; set; } = "";
        public int SelectedBarcodeColumn { get; set; } = 0;
        public int SelectedTextColumn { get; set; } = 1;

        /// <summary>
        /// 설정을 파일에 저장
        /// </summary>
        public void Save()
        {
            try
            {
                // 디렉토리가 없으면 생성
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 설정을 파일에 저장
                using (StreamWriter writer = new StreamWriter(SettingsFilePath))
                {
                    writer.WriteLine($"ExcelFilePath={ExcelFilePath}");
                    writer.WriteLine($"IsExcelModeEnabled={IsExcelModeEnabled}");
                    writer.WriteLine($"LastUsedFolder={LastUsedFolder}");
                    writer.WriteLine($"SelectedBarcodeColumn={SelectedBarcodeColumn}");
                    writer.WriteLine($"SelectedTextColumn={SelectedTextColumn}");
                }

                System.Diagnostics.Debug.WriteLine($"엑셀 설정 저장 완료: {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"엑셀 설정 저장 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일에서 설정을 로드
        /// </summary>
        public void Load()
        {
            try
            {
                // 기존 txt 파일도 지원 (마이그레이션)
                string filePath = SettingsFilePath;
                if (!File.Exists(filePath))
                {
                    filePath = SecureFileNameHelper.FindSecureFile("excel_settings", 
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info", "excel_settings.txt"));
                }

                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine("엑셀 설정 파일이 없습니다.");
                    return;
                }

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("="))
                        {
                            string[] parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                string key = parts[0].Trim();
                                string value = parts[1].Trim();

                                switch (key)
                                {
                                    case "ExcelFilePath":
                                        ExcelFilePath = value;
                                        break;
                                    case "IsExcelModeEnabled":
                                        if (bool.TryParse(value, out bool isEnabled))
                                        {
                                            IsExcelModeEnabled = isEnabled;
                                        }
                                        break;
                                    case "LastUsedFolder":
                                        LastUsedFolder = value;
                                        break;
                                    case "SelectedBarcodeColumn":
                                        if (int.TryParse(value, out int barcodeCol))
                                        {
                                            SelectedBarcodeColumn = barcodeCol;
                                        }
                                        break;
                                    case "SelectedTextColumn":
                                        if (int.TryParse(value, out int textCol))
                                        {
                                            SelectedTextColumn = textCol;
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"엑셀 설정 로드 완료: ExcelFilePath={ExcelFilePath}, IsExcelModeEnabled={IsExcelModeEnabled}, LastUsedFolder={LastUsedFolder}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"엑셀 설정 로드 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 엑셀 파일이 존재하는지 확인
        /// </summary>
        public bool IsExcelFileValid()
        {
            return !string.IsNullOrEmpty(ExcelFilePath) && File.Exists(ExcelFilePath);
        }
    }
}

