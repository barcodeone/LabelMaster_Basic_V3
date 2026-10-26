using System;

namespace BarcodePrint.Model
{
    public class NameCodePair
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }

        public NameCodePair() { }

        public NameCodePair(string code, string name, string icon = "")
        {
            Code = code;
            Name = name;
            Icon = icon;
        }

        public string DisplayText => string.IsNullOrEmpty(Icon) ? Name : $"{Icon}  {Name}";

        public override string ToString() => DisplayText;
    }
}
