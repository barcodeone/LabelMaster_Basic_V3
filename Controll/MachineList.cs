using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BarcodePrint.Model;

namespace BarcodePrint.Controll
{
    public class MachineList
    {
        public EventHandler machineListChanged;

        public void getMachineList() {

            USB_status.USB_init();
            USB_status.GetPrinterPortInfo();

            machineListChanged?.Invoke(this, EventArgs.Empty);
            //machineListChanged = null;
        }
    }
}
