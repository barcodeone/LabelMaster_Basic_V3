using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 파일명을 압축으로 유추할 수 없도록 암호화하는 헬퍼 클래스
    /// </summary>
    public static class SecureFileNameHelper
    {
        /// <summary>
        /// 원본 파일명을 암호화된 파일명으로 변환
        /// </summary>
        public static string GetSecureFileName(string originalName)
        {
            // SHA256 해시를 사용하여 일관된 암호화된 파일명 생성
            using (SHA256 sha256 = SHA256.Create())
            {
                // 원본 파일명 + 고정 키로 해시 생성
                string input = originalName + "XQuickSecure2025";
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                
                // 앞 16자만 사용하여 .dat 확장자 추가
                return hashString.Substring(0, 16) + ".dat";
            }
        }

        /// <summary>
        /// Info 폴더의 암호화된 파일 경로 생성
        /// </summary>
        public static string GetSecureFilePath(string originalFileName)
        {
            string infoFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info");
            if (!Directory.Exists(infoFolder))
            {
                Directory.CreateDirectory(infoFolder);
            }
            
            string encryptedFileName = GetSecureFileName(originalFileName);
            return Path.Combine(infoFolder, encryptedFileName);
        }

        /// <summary>
        /// Info 폴더에서 암호화된 파일 찾기
        /// </summary>
        public static string FindSecureFile(string originalFileName, string legacyFileName = null)
        {
            try
            {
                string infoFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info");
                
                if (!Directory.Exists(infoFolder))
                {
                    return null;
                }

                // 1순위: 암호화된 파일명 검색
                string encryptedFileName = GetSecureFileName(originalFileName);
                string encryptedPath = Path.Combine(infoFolder, encryptedFileName);
                if (File.Exists(encryptedPath))
                {
                    return encryptedPath;
                }

                // 2순위: 기존 파일명 (txt 확장자)
                if (!string.IsNullOrEmpty(legacyFileName))
                {
                    string legacyPath = Path.Combine(infoFolder, legacyFileName);
                    if (File.Exists(legacyPath))
                    {
                        // 기존 파일을 암호화된 이름으로 복사
                        File.Copy(legacyPath, encryptedPath);
                        System.Diagnostics.Debug.WriteLine($"기존 파일을 암호화된 이름으로 복사: {legacyFileName} -> {encryptedFileName}");
                        return encryptedPath;
                    }
                }

                // 3순위: 암호화된 파일이 없으면 생성 예정임을 알림
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"암호화된 파일 찾기 오류: {ex.Message}");
                return null;
            }
        }
    }
}

