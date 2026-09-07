using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    public class CustomTextBox : Control
    {
        private TextBox textBox;
        private Label label;
        private string labelText = "";
        private Color borderColor = Color.LightGray;
        private Color focusBorderColor = Color.Blue;
        private bool isFocused = false;

        public event EventHandler CustomTextChanged;

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

        public new string Text
        {
            get { return textBox.Text; }
            set { textBox.Text = value; }
        }

        public CustomTextBox()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                    ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.ResizeRedraw, true);

            this.Size = new Size(200, 60);
            this.BackColor = Color.Transparent;

            // 라벨 생성
            label = new Label();
            label.AutoSize = true;
            label.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label.ForeColor = Color.Black;
            label.Location = new Point(5, 5);
            label.Text = labelText;

            // 텍스트박스 생성
            textBox = new TextBox();
            textBox.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Regular, GraphicsUnit.Point);
            textBox.BorderStyle = BorderStyle.None;
            textBox.Location = new Point(5, 30);
            textBox.Size = new Size(this.Width - 10, 25);
            textBox.TextChanged += (s, e) => CustomTextChanged?.Invoke(this, e);
            textBox.GotFocus += (s, e) => { isFocused = true; Invalidate(); };
            textBox.LostFocus += (s, e) => { isFocused = false; Invalidate(); };

            this.Controls.Add(label);
            this.Controls.Add(textBox);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // 테두리 그리기
            using (Pen pen = new Pen(isFocused ? focusBorderColor : borderColor, 2))
            {
                g.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (textBox != null)
            {
                textBox.Size = new Size(this.Width - 10, 25);
            }
        }
    }
}
