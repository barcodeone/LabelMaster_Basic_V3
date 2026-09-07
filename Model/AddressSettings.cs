using System;
using System.IO;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 주소 라벨 설정을 저장하고 로드하는 클래스
    /// </summary>
    public class AddressSettings
    {
        // 암호화된 파일 경로 (압축으로 유추 불가능)
        private static readonly string SettingsFilePath = SecureFileNameHelper.GetSecureFilePath("address_settings");

        /// <summary>
        /// 엑셀 파일 경로
        /// </summary>
        public string ExcelFilePath { get; set; } = "";

        /// <summary>
        /// 주소 모드 활성화 여부
        /// </summary>
        public bool IsAddressModeEnabled { get; set; } = false;

        /// <summary>
        /// 마지막 사용 폴더
        /// </summary>
        public string LastUsedFolder { get; set; } = "";

        /// <summary>
        /// 이름 컬럼 인덱스 (기본값: 0)
        /// </summary>
        public int NameColumnIndex { get; set; } = 0;

        /// <summary>
        /// 주소1 컬럼 인덱스 (기본값: 1)
        /// </summary>
        public int AddressColumnIndex { get; set; } = 1;

        /// <summary>
        /// 주소2 컬럼 인덱스 (기본값: 2)
        /// </summary>
        public int Address2ColumnIndex { get; set; } = 2;

        /// <summary>
        /// 우편번호 컬럼 인덱스 (기본값: 3)
        /// </summary>
        public int PostalCodeColumnIndex { get; set; } = 3;

        /// <summary>
        /// 연락처 컬럼 인덱스 (기본값: 4)
        /// </summary>
        public int PhoneColumnIndex { get; set; } = 4;

        /// <summary>
        /// 보내는 분/받는 분 라벨 (기본값: "표시없음")
        /// 가능한 값: "보내는 분", "받는 분", "표시없음"
        /// </summary>
        public string SenderReceiverLabel { get; set; } = "표시없음";

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
                    writer.WriteLine($"IsAddressModeEnabled={IsAddressModeEnabled}");
                    writer.WriteLine($"LastUsedFolder={LastUsedFolder}");
                    writer.WriteLine($"NameColumnIndex={NameColumnIndex}");
                    writer.WriteLine($"AddressColumnIndex={AddressColumnIndex}");
                    writer.WriteLine($"Address2ColumnIndex={Address2ColumnIndex}");
                    writer.WriteLine($"PostalCodeColumnIndex={PostalCodeColumnIndex}");
                    writer.WriteLine($"PhoneColumnIndex={PhoneColumnIndex}");
                    writer.WriteLine($"SenderReceiverLabel={SenderReceiverLabel}");
                }

                System.Diagnostics.Debug.WriteLine($"주소 설정 저장 완료: {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"주소 설정 저장 오류: {ex.Message}");
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
                    filePath = SecureFileNameHelper.FindSecureFile("address_settings",
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info", "address_settings.txt"));
                }

                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine("주소 설정 파일이 없습니다.");
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
                                    case "IsAddressModeEnabled":
                                        if (bool.TryParse(value, out bool isEnabled))
                                        {
                                            IsAddressModeEnabled = isEnabled;
                                        }
                                        break;
                                    case "LastUsedFolder":
                                        LastUsedFolder = value;
                                        break;
                                    case "NameColumnIndex":
                                        if (int.TryParse(value, out int nameCol))
                                        {
                                            NameColumnIndex = nameCol;
                                        }
                                        break;
                                    case "AddressColumnIndex":
                                        if (int.TryParse(value, out int addressCol))
                                        {
                                            AddressColumnIndex = addressCol;
                                        }
                                        break;
                                    case "Address2ColumnIndex":
                                        if (int.TryParse(value, out int address2Col))
                                        {
                                            Address2ColumnIndex = address2Col;
                                        }
                                        break;
                                    case "PostalCodeColumnIndex":
                                        if (int.TryParse(value, out int postalCol))
                                        {
                                            PostalCodeColumnIndex = postalCol;
                                        }
                                        break;
                                    case "PhoneColumnIndex":
                                        if (int.TryParse(value, out int phoneCol))
                                        {
                                            PhoneColumnIndex = phoneCol;
                                        }
                                        break;
                                    case "SenderReceiverLabel":
                                        SenderReceiverLabel = value;
                                        break;
                                }
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"주소 설정 로드 완료: ExcelFilePath={ExcelFilePath}, IsAddressModeEnabled={IsAddressModeEnabled}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"주소 설정 로드 오류: {ex.Message}");
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

