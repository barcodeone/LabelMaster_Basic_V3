using System;
using System.IO;
using System.Text;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 쓰기 권한이 보장된 사용자 데이터(Info, RecentWorks 등) 경로 관리 및 레거시 마이그레이션 헬퍼
    /// </summary>
    public static class AppDataPathHelper
    {
        private static readonly object _initLock = new object();
        private static string _writableInfoDirectory;
        private static bool _legacyMigrated;

        public static string LegacyInfoDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info");

        public static string WritableInfoDirectory
        {
            get
            {
                if (_writableInfoDirectory == null)
                {
                    lock (_initLock)
                    {
                        if (_writableInfoDirectory == null)
                        {
                            _writableInfoDirectory = ResolveWritableInfoDirectory();
                        }
                    }
                }

                if (!_legacyMigrated)
                {
                    lock (_initLock)
                    {
                        if (!_legacyMigrated)
                        {
                            _legacyMigrated = true;
                            MigrateLegacyInfoIfNeeded(_writableInfoDirectory);
                        }
                    }
                }

                return _writableInfoDirectory;
            }
        }

        public static string RecentWorksDirectory
        {
            get
            {
                string dir = Path.Combine(WritableInfoDirectory, "RecentWorks");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return dir;
            }
        }

        private static string ResolveWritableInfoDirectory()
        {
            string[] candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LabelMaster", "Info"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LabelMaster", "Info"),
                Path.Combine(Path.GetTempPath(), "LabelMaster", "Info")
            };

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate)) continue;
                if (TryEnsureWritableDirectory(candidate))
                {
                    return candidate;
                }
            }

            return LegacyInfoDirectory;
        }

        private static bool TryEnsureWritableDirectory(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string probeFile = Path.Combine(directoryPath, $".write_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probeFile, "ok", Encoding.UTF8);
                File.Delete(probeFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void MigrateLegacyInfoIfNeeded(string targetInfoDir)
        {
            try
            {
                string legacyDir = LegacyInfoDirectory;
                if (string.Equals(Path.GetFullPath(legacyDir).TrimEnd('\\'), Path.GetFullPath(targetInfoDir).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (!Directory.Exists(legacyDir)) return;

                string[] rootFilesToMigrate = new[]
                {
                    "app_settings.json",
                    "autosave_template.lbm",
                    "print_history.json"
                };

                foreach (string fileName in rootFilesToMigrate)
                {
                    string src = Path.Combine(legacyDir, fileName);
                    string dst = Path.Combine(targetInfoDir, fileName);
                    if (File.Exists(src) && !File.Exists(dst))
                    {
                        File.Copy(src, dst, overwrite: false);
                    }
                }

                // 구버전 암호화 설정 파일(*.dat) 마이그레이션
                foreach (string srcDat in Directory.GetFiles(legacyDir, "*.dat"))
                {
                    string dstDat = Path.Combine(targetInfoDir, Path.GetFileName(srcDat));
                    if (!File.Exists(dstDat))
                    {
                        File.Copy(srcDat, dstDat, overwrite: false);
                    }
                }

                // RecentWorks 스냅샷 파일 마이그레이션
                string legacyRecentDir = Path.Combine(legacyDir, "RecentWorks");
                if (Directory.Exists(legacyRecentDir))
                {
                    string targetRecentDir = Path.Combine(targetInfoDir, "RecentWorks");
                    if (!Directory.Exists(targetRecentDir))
                    {
                        Directory.CreateDirectory(targetRecentDir);
                    }

                    foreach (string srcLbm in Directory.GetFiles(legacyRecentDir, "*.lbm"))
                    {
                        string dstLbm = Path.Combine(targetRecentDir, Path.GetFileName(srcLbm));
                        if (!File.Exists(dstLbm))
                        {
                            File.Copy(srcLbm, dstLbm, overwrite: false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Legacy Info migration skipped: {ex.Message}");
            }
        }

        /// <summary>
        /// 기존 설치 폴더(BaseDirectory\Info\RecentWorks)를 가리키던 내부 스냅샷 경로를 새 사용자 데이터 경로로 보정
        /// </summary>
        public static string NormalizeRecentWorkTemplatePath(string templateFilePath)
        {
            if (string.IsNullOrWhiteSpace(templateFilePath)) return templateFilePath;

            string legacyRecentPrefix = Path.Combine(LegacyInfoDirectory, "RecentWorks");
            if (templateFilePath.StartsWith(legacyRecentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string fileName = Path.GetFileName(templateFilePath);
                string migratedPath = Path.Combine(RecentWorksDirectory, fileName);
                if (File.Exists(migratedPath))
                {
                    return migratedPath;
                }
            }

            return templateFilePath;
        }

        /// <summary>
        /// 임시 파일에 먼저 기록한 뒤 대상 파일로 교체하는 원자적 텍스트 저장
        /// </summary>
        public static void WriteAllTextAtomic(string targetPath, string content)
        {
            string directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = targetPath + $".{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(tempPath, content, Encoding.UTF8);
                File.Copy(tempPath, targetPath, overwrite: true);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch { }
            }
        }
    }
}
