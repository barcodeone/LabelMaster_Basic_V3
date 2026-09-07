using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BarcodePrint.Model
{
    public static class SerialCryptoManager
    {
        // AES-256을 위한 32바이트 Key
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("XQuick_License_Secret_Key_2026!#");
        // AES-256을 위한 16바이트 IV
        private static readonly byte[] Iv = Encoding.UTF8.GetBytes("XQuick_InitVecto");

        // 표준 암호화 시리얼 라이선스 파일 경로 (58c5dec13bce64ea.dat)
        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info", "58c5dec13bce64ea.dat");

        /// <summary>
        /// 문자열을 AES-256-CBC로 암호화하여 Base64 문자열로 반환
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs, Encoding.UTF8))
                        {
                            sw.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Base64 암호화 문자열을 AES-256-CBC로 복호화
        /// </summary>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs, Encoding.UTF8))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Raw 바이트 배열을 AES-256-CBC로 복호화
        /// </summary>
        public static string DecryptBytes(byte[] cipherBytes)
        {
            if (cipherBytes == null || cipherBytes.Length == 0) return string.Empty;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = Iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (MemoryStream ms = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs, Encoding.UTF8))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Info 폴더 내의 암호화된 시리얼 번호 파일들(.dat)을 자동 탐색 및 복호화하여 로드
        /// </summary>
        public static List<string> LoadSerials()
        {
            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string infoDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Info");
                if (Directory.Exists(infoDir))
                {
                    // 1. Info 디렉토리 내의 모든 .dat 파일 검색 (해시 파일명, serials.dat, 58c5dec13bce64ea.dat 등)
                    var datFiles = Directory.GetFiles(infoDir, "*.dat", SearchOption.TopDirectoryOnly);

                    foreach (var file in datFiles)
                    {
                        try
                        {
                            byte[] fileBytes = File.ReadAllBytes(file);
                            if (fileBytes == null || fileBytes.Length == 0) continue;

                            string decrypted = string.Empty;

                            // 1) Base64 텍스트 기반 복호화 시도
                            try
                            {
                                string textContent = Encoding.UTF8.GetString(fileBytes).Trim();
                                if (!string.IsNullOrEmpty(textContent))
                                {
                                    decrypted = Decrypt(textContent);
                                }
                            }
                            catch { }

                            // 2) Base64 복호화 실패 시 Raw 바이너리 AES 복호화 시도
                            if (string.IsNullOrEmpty(decrypted))
                            {
                                try
                                {
                                    decrypted = DecryptBytes(fileBytes);
                                }
                                catch { }
                            }

                            if (!string.IsNullOrEmpty(decrypted))
                            {
                                string[] lines = decrypted.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var line in lines)
                                {
                                    string trimmed = line.Trim('\uFEFF', ' ', '\t', '\r', '\n');
                                    if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                                    {
                                        list.Add(trimmed);
                                    }
                                }
                            }
                        }
                        catch (Exception fEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SerialCryptoManager] File '{file}' read error: {fEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SerialCryptoManager] Load Error: {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine($"[SerialCryptoManager] 총 로드된 인증 시리얼 개수: {list.Count}");
            return list;
        }

        /// <summary>
        /// 시리얼 번호 목록을 암호화하여 기본 Info/58c5dec13bce64ea.dat 파일로 저장
        /// </summary>
        public static bool SaveSerials(IEnumerable<string> serials)
        {
            try
            {
                string infoDir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(infoDir))
                {
                    Directory.CreateDirectory(infoDir);
                }

                string joined = string.Join(Environment.NewLine, serials);
                string cipherText = Encrypt(joined);
                File.WriteAllText(FilePath, cipherText, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SerialCryptoManager] Save Error: {ex.Message}");
                return false;
            }
        }
    }
}
