using System.Collections.Generic;
using BarcodePrint.Model;

namespace BarcodePrint.Services
{
    public interface IPrinterService
    {
        List<UsbPrinterInfo> GetVerifiedPrinters(List<string> authenticatedSerials);
        string GetEffectivePrinterPort(UsbPrinterInfo printer);
        void CheckRealtimePrinterStatus(string selectedPrinterName, List<UsbPrinterInfo> cachedPrinters, out string badge, out string color);
        bool SendTsplRawCommand(string printerName, string command, List<UsbPrinterInfo> cachedPrinters, out string errorMessage);
        bool SendLabelPrintBytes(string printerName, byte[] tsplCommandBytes, List<UsbPrinterInfo> cachedPrinters, out string errorMessage);
        Dictionary<string, string> GetPrinterHardwareDetails(string printerName, List<UsbPrinterInfo> cachedPrinters);
    }
}
