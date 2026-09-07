using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    public class CustomRadioGroup : Control
    {
        private List<RadioButton> radioButtons;
        private Label label;
        private string labelText = "";
        private Color borderColor = Color.LightGray;
        private int selectedIndex = -1;

        public event EventHandler SelectedIndexChanged;

        public string LabelText
        {
            get { return labelText; }
            set 
            { 
                labelText = value;
                if (label != null)
                    label.Text = value;
            }
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set 
            { 
                if (value >= 0 && value < radioButtons.Count)
                {
                    selectedIndex = value;
                    for (int i = 0; i < radioButtons.Count; i++)
                    {
                        radioButtons[i].Checked = (i == selectedIndex);
                    }
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string SelectedText
        {
            get 
            { 
                if (selectedIndex >= 0 && selectedIndex < radioButtons.Count)
                    return radioButtons[selectedIndex].Text;
                return "";
            }
        }

        public CustomRadioGroup()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                    ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.ResizeRedraw, true);

            this.Size = new Size(300, 80);
            this.BackColor = Color.Transparent;
            radioButtons = new List<RadioButton>();

            // 라벨 생성
            label = new Label();
            label.AutoSize = true;
            label.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label.ForeColor = Color.Black;
            label.Location = new Point(5, 5);
            label.Text = labelText;

            this.Controls.Add(label);
        }

        public void AddRadioButton(string text)
        {
            RadioButton radio = new RadioButton();
            radio.Text = text;
            radio.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Regular, GraphicsUnit.Point);
            radio.Size = new Size(80, 30);
            radio.Location = new Point(10 + (radioButtons.Count * 90), 35);
            radio.CheckedChanged += RadioButton_CheckedChanged;

            radioButtons.Add(radio);
            this.Controls.Add(radio);

            // 첫 번째 라디오 버튼을 기본 선택
            if (radioButtons.Count == 1)
            {
                radio.Checked = true;
                selectedIndex = 0;
            }
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radio = sender as RadioButton;
            if (radio.Checked)
            {
                selectedIndex = radioButtons.IndexOf(radio);
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // 테두리 그리기
            using (Pen pen = new Pen(borderColor, 2))
            {
                g.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
    }
}
