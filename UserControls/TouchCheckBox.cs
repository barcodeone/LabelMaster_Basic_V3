using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    public class TouchCheckBox : Control
    {
        private bool isChecked = true;
        private bool isHovered = false;
        private bool isPressed = false;

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
                    OnCheckedChanged();
                    Invalidate();
                }
            }
        }

        public string CheckBoxText
        {
            get { return Text; }
            set { Text = value; Invalidate(); }
        }

        public TouchCheckBox()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                    ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.ResizeRedraw, true);
            
            this.Size = new Size(400, 80);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.TabStop = false;
            this.Text = "바코드 값 표시";
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 체크박스 크기 (터치 친화적) - 컨트롤 크기에 맞게 조정
            int checkboxSize = Math.Min(30, Math.Min(Width - 20, Height - 10));
            int margin = 5;
            int textMargin = checkboxSize + margin + 5;

            // Enabled 상태에 따라 색상 변경
            Color checkboxBgColor;
            Color borderColor;
            Color textColor;
            Color checkMarkColor;
            
            if (this.Enabled)
            {
                // 활성화 상태
                checkboxBgColor = isPressed ? Color.FromArgb(200, 0, 120, 215) : 
                                   isHovered ? Color.FromArgb(150, 0, 120, 215) : 
                                   Color.FromArgb(100, 0, 120, 215);
                borderColor = Color.FromArgb(200, 0, 120, 215);
                textColor = Color.Black;
                checkMarkColor = Color.White;
            }
            else
            {
                // 비활성화 상태 - 회색
                checkboxBgColor = Color.FromArgb(100, 220, 220, 220);
                borderColor = Color.FromArgb(200, 180, 180, 180);
                textColor = Color.LightGray;
                checkMarkColor = Color.FromArgb(200, 200, 200);
            }

            // 체크박스 배경
            using (SolidBrush bgBrush = new SolidBrush(checkboxBgColor))
            {
                g.FillRectangle(bgBrush, margin, (Height - checkboxSize) / 2, checkboxSize, checkboxSize);
            }

            // 체크박스 테두리
            using (Pen borderPen = new Pen(borderColor, 2))
            {
                g.DrawRectangle(borderPen, margin, (Height - checkboxSize) / 2, checkboxSize, checkboxSize);
            }

            // 체크 표시 (체크된 경우)
            if (isChecked)
            {
                using (Pen checkPen = new Pen(checkMarkColor, 3))
                {
                    int checkMargin = 6;
                    int checkX = margin + checkMargin;
                    int checkY = (Height - checkboxSize) / 2 + checkMargin;
                    int checkSize = checkboxSize - (checkMargin * 2);

                    // 체크 표시 그리기 (✓ 모양)
                    g.DrawLine(checkPen, checkX, checkY + checkSize / 2, checkX + checkSize / 3, checkY + checkSize);
                    g.DrawLine(checkPen, checkX + checkSize / 3, checkY + checkSize, checkX + checkSize, checkY);
                }
            }

            // 텍스트 그리기
            using (Font textFont = new Font("맑은 고딕", Math.Max(12, checkboxSize * 0.6f), FontStyle.Bold, GraphicsUnit.Point))
            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                StringFormat sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Near;

                Rectangle textRect = new Rectangle(textMargin, 0, Width - textMargin, Height);
                g.DrawString(Text, textFont, textBrush, textRect, sf);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHovered = false;
            isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.Enabled)
            {
                isPressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isPressed && this.Enabled)
            {
                isPressed = false;
                Checked = !Checked;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            isHovered = false;
            isPressed = false;
            Cursor = this.Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected virtual void OnCheckedChanged()
        {
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
