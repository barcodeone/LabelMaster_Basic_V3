using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using BarcodePrint.Model;

namespace BarcodePrint.View
{
    internal class getPrinterList { }
    public partial class MainForm
    {
        private void ReceiveEventPrinterListUpdate(object sender, EventArgs e)
        {
            // 프린터 목록 업데이트는 AdjustForm에서 처리하므로 여기서는 아무것도 하지 않음
            // 필요시 AdjustForm의 프린터 콤보박스를 업데이트할 수 있음
        }
    }
}
