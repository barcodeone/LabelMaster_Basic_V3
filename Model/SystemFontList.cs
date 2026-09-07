using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Text;

namespace BarcodePrint.Model
{
    public class SystemFontList
    {
        public List<string> getSystemFont()
        {
            
            List<string> list = new List<string>();
            InstalledFontCollection fontCollection = new InstalledFontCollection();

            foreach (FontFamily fa in fontCollection.Families)
            {
                list.Add(fa.Name);
            }

            return list;
        }
    }
}
