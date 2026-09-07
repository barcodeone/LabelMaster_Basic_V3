using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    /// <summary>
    /// 둥근 모서리를 가진 Button 커스텀 컨트롤
    /// </summary>
    public class RoundedButton : Button
    {
        private Color borderColor = Color.DodgerBlue;
        private int borderRadius = 10;
        private int borderWidth = 1;
        private bool isHovered = false;
        private bool isPressed = false;

        // Primary/Secondary 버튼 스타일
        private ButtonStyle buttonStyle = ButtonStyle.Primary;

        public enum ButtonStyle
        {
            Primary,   // 파란색 배경
            Secondary  // 흰색 배경
        }

        public ButtonStyle Style
        {
            get { return buttonStyle; }
            set 
            { 
                buttonStyle = value;
                UpdateColors();
                Invalidate();
            }
        }

        public int BorderRadius
        {
            get { return borderRadius; }
            set { borderRadius = value; Invalidate(); }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; Invalidate(); }
        }

        public RoundedButton()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                    ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.ResizeRedraw | 
                    ControlStyles.UserPaint, true);

            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Font = new Font("맑은 고딕", 12F, FontStyle.Bold, GraphicsUnit.Point);
            this.Cursor = Cursors.Hand;
            
            UpdateColors();
        }

        private void UpdateColors()
        {
            if (buttonStyle == ButtonStyle.Primary)
            {
                this.BackColor = Color.DodgerBlue;
                this.ForeColor = Color.White;
                this.borderColor = Color.DodgerBlue;
            }
            else
            {
                this.BackColor = Color.White;
                this.ForeColor = Color.DodgerBlue;
                this.borderColor = Color.LightGray;
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 부모의 배경색으로 배경을 채워 둥근 모서리 외부가 하얗게 보이지 않도록 함
            if (this.Parent != null)
            {
                using (var brush = new SolidBrush(this.Parent.BackColor))
                {
                    g.FillRectangle(brush, this.ClientRectangle);
                }
            }
            else
            {
                g.Clear(this.BackColor);
            }

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            // 배경색 결정: 지정된 BackColor를 기반으로 밝기 조절
            Color bgColor = this.BackColor;
            if (isPressed)
            {
                bgColor = GetControlColor(this.BackColor, -30); // 30단계 어둡게
            }
            else if (isHovered)
            {
                bgColor = GetControlColor(this.BackColor, 20); // 20단계 밝게
            }

            using (GraphicsPath path = GetRoundedRectanglePath(rect, borderRadius))
            {
                // 배경 그리기
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }

                // 테두리 그리기
                using (Pen pen = new Pen(borderColor, borderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 텍스트 그리기
            TextRenderer.DrawText(g, this.Text, this.Font, rect,
                this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private Color GetControlColor(Color color, int delta)
        {
            int r = Math.Max(0, Math.Min(255, color.R + delta));
            int g = Math.Max(0, Math.Min(255, color.G + delta));
            int b = Math.Max(0, Math.Min(255, color.B + delta));
            return Color.FromArgb(color.A, r, g, b);
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(1, radius * 2); // 0 방지

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            
            path.CloseFigure();
            return path;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // OnPaint에서 직접 처리
        }
    }
}

