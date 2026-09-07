using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    public class CustomCloseButton : Control
    {
        private bool isHovered = false;
        private bool isPressed = false;

        public CustomCloseButton()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                    ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.ResizeRedraw, true);
            
            this.Size = new Size(40, 40);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 배경 원 그리기
            Color backgroundColor = isPressed ? Color.FromArgb(200, 255, 0, 0) : 
                                   isHovered ? Color.FromArgb(150, 255, 0, 0) : 
                                   Color.FromArgb(100, 255, 0, 0);

            using (SolidBrush bgBrush = new SolidBrush(backgroundColor))
            {
                g.FillEllipse(bgBrush, 0, 0, Width - 1, Height - 1);
            }

            // X 표시 그리기
            using (Pen xPen = new Pen(Color.White, 3))
            {
                int margin = 8;
                g.DrawLine(xPen, margin, margin, Width - margin, Height - margin);
                g.DrawLine(xPen, Width - margin, margin, margin, Height - margin);
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
            if (e.Button == MouseButtons.Left)
            {
                isPressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isPressed)
            {
                isPressed = false;
                Invalidate();
                OnClick(e);
            }
            base.OnMouseUp(e);
        }
    }
}
