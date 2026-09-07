using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BarcodePrint.Model
{
    /// <summary>
    /// 가격 데이터 포맷 처리 헬퍼 클래스
    /// 숫자를 원화 포맷(₩ 기호 + 천단위 구분)으로 변환
    /// </summary>
    public static class PriceFormatHelper
    {
        /// <summary>
        /// 가격 데이터를 원화 포맷으로 변환
        /// </summary>
        /// <param name="value">변환할 값</param>
        /// <param name="addWonSymbol">₩ 기호 추가 여부 (기본값: true)</param>
        /// <returns>포맷된 가격 문자열</returns>
        public static string FormatPrice(string value, bool addWonSymbol = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            System.Diagnostics.Debug.WriteLine($"[FormatPrice] 입력값: '{value}'");

            // ₩, \, 쉼표 제거하여 순수 숫자 추출
            string cleanValue = value.Replace("₩", "")
                                     .Replace("\\", "")
                                     .Replace(",", "")
                                     .Trim();

            System.Diagnostics.Debug.WriteLine($"[FormatPrice] 정제된 값: '{cleanValue}'");

            // 숫자가 없으면 빈 문자열 반환
            if (string.IsNullOrWhiteSpace(cleanValue))
            {
                System.Diagnostics.Debug.WriteLine($"[FormatPrice] 숫자 없음, 빈 문자열 반환");
                return "";
            }

            // 숫자인지 확인 (정수만 허용 - 소수점 제거)
            if (decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal priceValue))
            {
                // 정수로 변환 (소수점 제거)
                long intValue = (long)Math.Truncate(priceValue);
                
                // 천단위 구분 기호 적용 (N0 포맷: 천단위 구분 기호 포함, 소수점 없음)
                string formatted = intValue.ToString("N0", CultureInfo.InvariantCulture);
                
                // 원화 기호 제거 요청으로 항상 기호 없이 포맷된 숫자만 반환
                if (addWonSymbol)
                {
                    System.Diagnostics.Debug.WriteLine($"[FormatPrice] 원화 기호 추가 요청이 무시됨: '{cleanValue}' → '{formatted}'");
                }
                
                return formatted;
            }

            // 숫자가 아닌 경우 빈 문자열 반환 (숫자만 입력받도록 했으므로)
            System.Diagnostics.Debug.WriteLine($"[FormatPrice] 숫자 파싱 실패, 빈 문자열 반환: '{value}'");
            return "";
        }

        /// <summary>
        /// 간편 가격 입력 변환
        /// 예: '1' 입력시 '₩10,000원', '0.7' 입력시 '₩7,000원', '15000' 입력시 '₩15,000원'
        /// </summary>
        /// <param name="value">입력값</param>
        /// <param name="includeWonPrefixAndSuffix">₩ 기호 및 '원' 접미사 포함 여부 (기본: true)</param>
        /// <returns>포맷팅된 원화 가격 문자열</returns>
        public static string FormatShortPriceInput(string value, bool includeWonPrefixAndSuffix = true)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            // ₩, \, 쉼표, '원' 제거하여 정제
            string cleanValue = value.Replace("₩", "")
                                     .Replace("\\", "")
                                     .Replace(",", "")
                                     .Replace("원", "")
                                     .Trim();

            if (string.IsNullOrWhiteSpace(cleanValue))
                return "";

            if (decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
            {
                // 간편 입력 로직: 100 미만 숫자는 만 단위 곱함 (예: 1 => 10,000, 0.7 => 7,000, 1.5 => 15,000)
                if (val > 0 && val < 100)
                {
                    val = val * 10000m;
                }

                long intVal = (long)Math.Truncate(val);
                string numStr = intVal.ToString("N0", CultureInfo.InvariantCulture);

                if (includeWonPrefixAndSuffix)
                {
                    return $"₩{numStr}원";
                }
                else
                {
                    return $"{numStr}원";
                }
            }

            return value;
        }

        /// <summary>
        /// 포맷된 가격에서 숫자만 추출
        /// </summary>
        /// <param name="formattedPrice">포맷된 가격 문자열</param>
        /// <returns>숫자만 포함된 문자열</returns>
        public static string ExtractNumericValue(string formattedPrice)
        {
            if (string.IsNullOrWhiteSpace(formattedPrice))
                return formattedPrice;

            // ₩, \, 쉼표 제거
            return formattedPrice.Replace("₩", "")
                                  .Replace("\\", "")
                                  .Replace(",", "")
                                  .Trim();
        }

        /// <summary>
        /// 값이 숫자인지 확인
        /// </summary>
        /// <param name="value">확인할 값</param>
        /// <returns>숫자 여부</returns>
        public static bool IsNumeric(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string cleanValue = ExtractNumericValue(value);
            return decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }

        /// <summary>
        /// 가격 컬럼 여부 확인 (컬럼명으로 판단)
        /// </summary>
        /// <param name="columnName">컬럼명</param>
        /// <returns>가격 컬럼 여부</returns>
        public static bool IsPriceColumn(string columnName)
        {
            return false;
        }
    }
}

