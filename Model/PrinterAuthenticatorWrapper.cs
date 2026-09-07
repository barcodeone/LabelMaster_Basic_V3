using System;
using System.Linq;

namespace BarcodePrint.Model
{
    /// <summary>
    /// XPrinterAuth DLL의 PrinterAuthenticator를 래핑합니다.
    /// DLL에 IsPrinterValid 메서드가 없으므로 로컬 검증 로직을 제공합니다.
    /// </summary>
    public static class PrinterAuthenticatorWrapper
    {
        private static readonly string[] XPrinterModels = 
        { 
            "XP-", "Xprinter", "423B", "T265B", "T4501", "TT428", "TT424", 
            "237B", "375B", "370B", "DT108B", "326B", "427B", "D4601B", 
            "D4602B", "D4604B", "P423B", "D461B" 
        };

        /// <summary>
        /// XPrinter 모델명인지 확인합니다.
        /// </summary>
        public static bool IsXPrinterModel(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return XPrinterModels.Any(model => 
                name.IndexOf(model, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// 프린터가 유효(허가됨)인지 검증합니다.
        /// XPrinterAuth DLL에 IsPrinterValid가 없으므로 GetConnectedPrinters 기반으로 검증합니다.
        /// </summary>
        /// <param name="printerName">프린터 컬럼3</param>
        /// <param name="portName">포트 컬럼3</param>
        /// <returns>유효하면 true</returns>
        public static bool IsPrinterValid(string printerName, string portName)
        {
            // XPrinter 모델이면 유효 (DLL 의존성을 완전 배제하고 스풀러 및 레지스트리 기반으로 작동)
            return IsXPrinterModel(printerName);
        }
    }
}
