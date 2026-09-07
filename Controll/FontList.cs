using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Text;

using BarcodePrint.Model;

namespace BarcodePrint.Controll
{
    public class FontList
    {
        public EventHandler<List<string>> FontListUpdated;

        public void getFontList()
        {
            SystemFontList fonts = new SystemFontList();
            FontListUpdated?.Invoke(this, fonts.getSystemFont());
            //FontListUpdated = null;
        }
    }
}
