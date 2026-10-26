using System;
using System.Linq;
using BarcodePrint.Model;
using BarcodePrint.ViewModel;

namespace BarcodePrint
{
    class ProgramTest
    {
        [STAThread]
        static void Main()
        {
            var vm = new MainViewModel();
            Console.WriteLine($"Initial Elements count: {vm.CurrentTemplate?.Elements?.Count}");
            Console.WriteLine($"Initial ActiveColumnCount: {vm.ActiveColumnCount}");
            Console.WriteLine($"IsTableBorderEnabled: {vm.IsTableBorderEnabled}");

            // 1. 새 라벨 마법사 시뮬레이션
            vm.ApplyWizardSetup(80, 60, "GAP", 0);
            Console.WriteLine("\nAfter ApplyWizardSetup(80, 60):");
            Console.WriteLine($"Elements count: {vm.CurrentTemplate.Elements.Count}");
            foreach (var el in vm.CurrentTemplate.Elements)
            {
                Console.WriteLine($" - {el.Type} | Content='{el.Content}' | InputOrder={el.InputOrder} | X={el.X}, Y={el.Y}, W={el.Width}, H={el.Height}");
            }
            Console.WriteLine($"ActiveColumnCount: {vm.ActiveColumnCount}");
            Console.WriteLine($"Col1: input='{vm.InputCol1}', barcode={vm.IsColumn1Barcode}");

            // 2. 바코드 추가 시뮬레이션
            vm.AddNewElement(ElementType.BARCODE);
            Console.WriteLine("\nAfter AddNewElement(BARCODE):");
            Console.WriteLine($"Elements count: {vm.CurrentTemplate.Elements.Count}");
            foreach (var el in vm.CurrentTemplate.Elements)
            {
                Console.WriteLine($" - {el.Type} | Content='{el.Content}' | InputOrder={el.InputOrder} | X={el.X}, Y={el.Y}, W={el.Width}, H={el.Height}");
            }
            Console.WriteLine($"ActiveColumnCount: {vm.ActiveColumnCount}");
            Console.WriteLine($"Col2: input='{vm.InputCol2}', barcode={vm.IsColumn2Barcode}, type={vm.Col2BarcodeType}");

            // 3. QR코드 추가 시뮬레이션
            vm.AddNewElement(ElementType.QR_CODE);
            Console.WriteLine("\nAfter AddNewElement(QR_CODE):");
            Console.WriteLine($"Elements count: {vm.CurrentTemplate.Elements.Count}");
            foreach (var el in vm.CurrentTemplate.Elements)
            {
                Console.WriteLine($" - {el.Type} | Content='{el.Content}' | InputOrder={el.InputOrder} | X={el.X}, Y={el.Y}, W={el.Width}, H={el.Height}");
            }
            Console.WriteLine($"ActiveColumnCount: {vm.ActiveColumnCount}");
            Console.WriteLine($"Col3: input='{vm.InputCol3}', barcode={vm.IsColumn3Barcode}, type={vm.Col3BarcodeType}");
        }
    }
}
