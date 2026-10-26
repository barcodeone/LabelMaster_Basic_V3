using System;

namespace BarcodePrint.Model
{
    public enum LicenseMode
    {
        Full,       // 정식 버전 (제한 없는 완전 무료 버전)
        Trial       // 미사용 (기존 호환성 유지용)
    }

    /// <summary>
    /// LabelMaster Basic v2 라이선스 관리자 (모든 기능 100% 무료 제공, 제한 없음)
    /// </summary>
    public static class TrialLicenseManager
    {
        // 모든 제한 해제 (무제한)
        public const int MAX_TRIAL_EXCEL_ROWS = int.MaxValue;
        public const int MAX_TRIAL_PRINT_COPIES = int.MaxValue;
        public const int MAX_TRIAL_SERIAL_STEPS = int.MaxValue;

        // 항상 제한 없는 Full 모드로 유지
        public static LicenseMode CurrentMode => LicenseMode.Full;
        public static string AuthenticatedSerial { get; private set; } = "FREE-LICENSE-OK";
        public static string CurrentPrinterName { get; private set; } = "";

        /// <summary>
        /// 시리얼 검증 없이 항상 정식 무료(Full) 모드를 반환합니다.
        /// </summary>
        public static LicenseMode EvaluateLicense(bool hasValidHardwareSerial = true, string serial = "", string printerName = "")
        {
            if (!string.IsNullOrEmpty(serial) && serial != "-")
            {
                AuthenticatedSerial = serial;
            }

            if (!string.IsNullOrEmpty(printerName))
            {
                CurrentPrinterName = printerName;
            }

            return LicenseMode.Full;
        }
    }
}
