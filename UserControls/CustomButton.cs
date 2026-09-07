using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    public class CustomButton : Control
    {
        private bool isHovered = false;
        private bool isPressed = false;
        private int borderRadius = 15;
        private Color buttonColor = Color.DodgerBlue;

        public int BorderRadius
        {
            get { return borderRadius; }
            set { borderRadius = value; Invalidate(); }
        }

        public Color ButtonColor
        {
            get { return buttonColor; }
            set { buttonColor = value; Invalidate(); }
        }

        public CustomButton()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint, true);

            this.Size = new Size(120, 40);
            this.BackColor = Color.Transparent;
            this.ForeColor = Color.White;
            this.Cursor = Cursors.Hand;
            this.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 버튼의 실제 배경색 (상태에 따라 변경)
            Color currentBackColor = buttonColor;
            if (isPressed)
            {
                currentBackColor = ControlPaint.Dark(buttonColor, 0.1f);
            }
            else if (isHovered)
            {
                currentBackColor = ControlPaint.Light(buttonColor, 0.1f);
            }

            // 그리기 영역
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (GraphicsPath path = GetRoundedRectanglePath(rect, borderRadius))
            {
                // 그림자 효과 (선택사항, 깔끔함을 위해 제외하거나 단순화)
                // 배경 그리기
                using (SolidBrush brush = new SolidBrush(currentBackColor))
                {
                    g.FillPath(brush, path);
                }

                // 테두리 그리기 (필요 시)
                using (Pen pen = new Pen(currentBackColor, 1))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 텍스트 그리기
            TextRenderer.DrawText(g, this.Text, this.Font, new Rectangle(0, 0, Width, Height),
                this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);

            path.CloseFigure();
            return path;
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
            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isPressed = false;
                Invalidate();
                // Control 클래스는 기본적으로 Click 이벤트를 발생시키지만, 
                // 명시적으로 처리 흐름을 맞춥니다.
                // if (this.ClientRectangle.Contains(e.Location))
                // {
                //    OnClick(e);
                // }
                // base.OnMouseUp(e)에서 Click 이벤트를 발생시키므로 중복 호출 제거
            }
            base.OnMouseUp(e);
        }

        // 텍스트 변경 시 다시 그리기
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }
    }
}
