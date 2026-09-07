using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarcodePrint.Controll
{
    /// <summary>
    /// 커스텀 TextBox 컨트롤 - MaterialSkin 스타일 적용
    /// </summary>
    public class CustomTextBox : TextBox
    {
        private bool _focused = false;
        private Color _borderColor = Color.FromArgb(222, 0, 0, 0);
        private Color _focusedBorderColor = Color.FromArgb(255, 33, 150, 243);
        private Color _backgroundColor = Color.White;
        private Color _focusedBackgroundColor = Color.White;

        public CustomTextBox()
        {
            // 기본 설정
            this.BorderStyle = BorderStyle.None;
            this.BackColor = _backgroundColor;
            this.ForeColor = Color.FromArgb(222, 0, 0, 0);
            this.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            this.TextAlign = HorizontalAlignment.Right;
            
            // 이벤트 연결
            this.GotFocus += CustomTextBox_GotFocus;
            this.LostFocus += CustomTextBox_LostFocus;
            this.Paint += CustomTextBox_Paint;
        }

        private void CustomTextBox_GotFocus(object sender, EventArgs e)
        {
            _focused = true;
            this.BackColor = _focusedBackgroundColor;
            this.Invalidate();
        }

        private void CustomTextBox_LostFocus(object sender, EventArgs e)
        {
            _focused = false;
            this.BackColor = _backgroundColor;
            this.Invalidate();
        }

        private void CustomTextBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 배경 그리기
            using (SolidBrush bgBrush = new SolidBrush(this.BackColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // 테두리 그리기
            Color currentBorderColor = _focused ? _focusedBorderColor : _borderColor;
            using (Pen borderPen = new Pen(currentBorderColor, 2f))
            {
                // 하단 테두리만 그리기 (Material Design 스타일)
                Point startPoint = new Point(0, Height - 2);
                Point endPoint = new Point(Width, Height - 2);
                g.DrawLine(borderPen, startPoint, endPoint);
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.Invalidate();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            this.Invalidate();
        }
    }
}
