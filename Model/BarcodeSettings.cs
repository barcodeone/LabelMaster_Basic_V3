using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 바코드 설정 정보를 저장하는 클래스
    /// </summary>
    [Serializable]
    public class BarcodeSettings
    {
        public int BarcodeSize { get; set; } = 50;
        public int BarcodeX { get; set; } = 0;
        public int BarcodeY { get; set; } = 0;
        public string BarcodeType { get; set; } = "QR_CODE";
        public bool ShowBarcodeText { get; set; } = true;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public string SettingsName { get; set; } = "기본 설정";
    }

    /// <summary>
    /// 바코드 설정 컬렉션을 관리하는 클래스
    /// </summary>
    [Serializable]
    public class BarcodeSettingsCollection
    {
        public List<BarcodeSettings> SettingsList { get; set; } = new List<BarcodeSettings>();
        public string CurrentSettingsName { get; set; } = "기본 설정";
    }

    /// <summary>
    /// 바코드 설정을 저장하고 불러오는 매니저 클래스
    /// </summary>
    public class BarcodeSettingsManager
    {
        private const string SETTINGS_FILE = "BarcodeSettings.xml";
        private const string BACKUP_FILE = "BarcodeSettings_backup.xml";
        private string settingsPath;

        public BarcodeSettingsManager()
        {
            // Info 폴더에 설정 파일 저장
            string infoFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info");
            if (!Directory.Exists(infoFolder))
            {
                Directory.CreateDirectory(infoFolder);
            }
            settingsPath = Path.Combine(infoFolder, SETTINGS_FILE);
        }

        /// <summary>
        /// 현재 바코드 설정을 저장합니다.
        /// </summary>
        /// <param name="settings">저장할 바코드 설정</param>
        /// <returns>저장 성공 여부</returns>
        public bool SaveSettings(BarcodeSettings settings)
        {
            try
            {
                // 기존 설정 파일이 있으면 백업
                if (File.Exists(settingsPath))
                {
                    string infoFolder = Path.GetDirectoryName(settingsPath);
                    File.Copy(settingsPath, Path.Combine(infoFolder, BACKUP_FILE), true);
                }

                // 설정 컬렉션 로드 또는 새로 생성
                BarcodeSettingsCollection collection = LoadSettingsCollection();
                if (collection == null)
                {
                    collection = new BarcodeSettingsCollection();
                }

                // 기존 설정이 있으면 업데이트, 없으면 추가
                var existingSettings = collection.SettingsList.Find(s => s.SettingsName == settings.SettingsName);
                if (existingSettings != null)
                {
                    existingSettings.BarcodeSize = settings.BarcodeSize;
                    existingSettings.BarcodeX = settings.BarcodeX;
                    existingSettings.BarcodeY = settings.BarcodeY;
                    existingSettings.BarcodeType = settings.BarcodeType;
                    existingSettings.ShowBarcodeText = settings.ShowBarcodeText;
                    existingSettings.LastModified = DateTime.Now;
                }
                else
                {
                    settings.LastModified = DateTime.Now;
                    collection.SettingsList.Add(settings);
                }

                // 현재 설정 이름 업데이트
                collection.CurrentSettingsName = settings.SettingsName;

                // XML로 저장
                XmlSerializer serializer = new XmlSerializer(typeof(BarcodeSettingsCollection));
                using (FileStream fileStream = new FileStream(settingsPath, FileMode.Create))
                {
                    serializer.Serialize(fileStream, collection);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"바코드 설정 저장 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 현재 설정을 불러옵니다.
        /// </summary>
        /// <returns>현재 바코드 설정</returns>
        public BarcodeSettings LoadCurrentSettings()
        {
            try
            {
                BarcodeSettingsCollection collection = LoadSettingsCollection();
                if (collection != null && !string.IsNullOrEmpty(collection.CurrentSettingsName))
                {
                    var currentSettings = collection.SettingsList.Find(s => s.SettingsName == collection.CurrentSettingsName);
                    if (currentSettings != null)
                    {
                        return currentSettings;
                    }
                }

                // 기본 설정 반환
                return new BarcodeSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"바코드 설정 불러오기 오류: {ex.Message}");
                return new BarcodeSettings();
            }
        }

        /// <summary>
        /// 특정 이름의 설정을 불러옵니다.
        /// </summary>
        /// <param name="settingsName">설정 이름</param>
        /// <returns>바코드 설정</returns>
        public BarcodeSettings LoadSettings(string settingsName)
        {
            try
            {
                BarcodeSettingsCollection collection = LoadSettingsCollection();
                if (collection != null)
                {
                    var settings = collection.SettingsList.Find(s => s.SettingsName == settingsName);
                    if (settings != null)
                    {
                        return settings;
                    }
                }

                return new BarcodeSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"바코드 설정 불러오기 오류: {ex.Message}");
                return new BarcodeSettings();
            }
        }

        /// <summary>
        /// 모든 설정 목록을 불러옵니다.
        /// </summary>
        /// <returns>설정 목록</returns>
        public List<BarcodeSettings> LoadAllSettings()
        {
            try
            {
                BarcodeSettingsCollection collection = LoadSettingsCollection();
                if (collection != null)
                {
                    return collection.SettingsList;
                }

                return new List<BarcodeSettings>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"바코드 설정 목록 불러오기 오류: {ex.Message}");
                return new List<BarcodeSettings>();
            }
        }

        /// <summary>
        /// 설정을 삭제합니다.
        /// </summary>
        /// <param name="settingsName">삭제할 설정 이름</param>
        /// <returns>삭제 성공 여부</returns>
        public bool DeleteSettings(string settingsName)
        {
            try
            {
                BarcodeSettingsCollection collection = LoadSettingsCollection();
                if (collection != null)
                {
                    var settingsToDelete = collection.SettingsList.Find(s => s.SettingsName == settingsName);
                    if (settingsToDelete != null)
                    {
                        collection.SettingsList.Remove(settingsToDelete);
                        
                        // 현재 설정이 삭제된 경우 기본 설정으로 변경
                        if (collection.CurrentSettingsName == settingsName)
                        {
                            collection.CurrentSettingsName = collection.SettingsList.Count > 0 ? 
                                collection.SettingsList[0].SettingsName : "기본 설정";
                        }

                        // XML로 저장
                        XmlSerializer serializer = new XmlSerializer(typeof(BarcodeSettingsCollection));
                        using (FileStream fileStream = new FileStream(settingsPath, FileMode.Create))
                        {
                            serializer.Serialize(fileStream, collection);
                        }

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"바코드 설정 삭제 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 설정 컬렉션을 XML에서 불러옵니다.
        /// </summary>
        /// <returns>설정 컬렉션</returns>
        private BarcodeSettingsCollection LoadSettingsCollection()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(BarcodeSettingsCollection));
                    using (FileStream fileStream = new FileStream(settingsPath, FileMode.Open))
                    {
                        return (BarcodeSettingsCollection)serializer.Deserialize(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 컬렉션 불러오기 오류: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 설정 파일이 존재하는지 확인합니다.
        /// </summary>
        /// <returns>설정 파일 존재 여부</returns>
        public bool SettingsFileExists()
        {
            return File.Exists(settingsPath);
        }

        /// <summary>
        /// 백업 파일에서 설정을 복원합니다.
        /// </summary>
        /// <returns>복원 성공 여부</returns>
        public bool RestoreFromBackup()
        {
            try
            {
                string infoFolder = Path.GetDirectoryName(settingsPath);
                string backupPath = Path.Combine(infoFolder, BACKUP_FILE);
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, settingsPath, true);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"백업에서 복원 오류: {ex.Message}");
                return false;
            }
        }
    }
}
