using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    /// <summary>
    /// 모드 선택 메뉴 아이템 (라운드 사각형으로 선택 표시)
    /// </summary>
    public class ModeMenuItem : UserControl
    {
        private bool isSelected = false;
        private string menuText = "";
        private Image menuIcon = null;
        private Color selectedBackColor = Color.FromArgb(30, 144, 255); // DodgerBlue - 선택된 메뉴
        private Color hoverBackColor = Color.FromArgb(200, 220, 255); // 호버 시 연한 파란색
        private Color normalBackColor = Color.FromArgb(230, 240, 255); // 미선택 시 더 연한 파란색
        private int borderRadius = 8;
        private bool isHovered = false;
        private Color selectedTextColor = Color.White; // 선택 시 흰색 텍스트
        private Color normalTextColor = Color.FromArgb(60, 60, 60); // 미선택 시 진한 회색 텍스트

        public event EventHandler MenuClicked;

        public bool IsSelected
        {
            get { return isSelected; }
            set 
            { 
                isSelected = value;
                Invalidate();
            }
        }

        public string MenuText
        {
            get { return menuText; }
            set 
            { 
                menuText = value;
                Invalidate();
            }
        }

        public Image MenuIcon
        {
            get { return menuIcon; }
            set 
            { 
                menuIcon = value;
                Invalidate();
            }
        }

        public ModeMenuItem()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                    ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.ResizeRedraw | 
                    ControlStyles.UserPaint, true);

            this.Size = new Size(200, 50);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.Padding = new Padding(10, 5, 10, 5);
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
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            MenuClicked?.Invoke(this, e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 배경 그리기 (항상)
            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            Color bgColor;
            
            if (isSelected)
            {
                bgColor = selectedBackColor; // 파란색 (DodgerBlue)
            }
            else if (isHovered)
            {
                bgColor = hoverBackColor; // 더 연한 파란색
            }
            else
            {
                bgColor = Color.FromArgb(230, 240, 255); // 미선택 시 연한 파란색
            }
            
            using (GraphicsPath path = GetRoundedRectanglePath(rect, borderRadius))
            {
                using (SolidBrush brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }
            }

            // 아이콘 그리기
            int currentX = this.Padding.Left;
            if (menuIcon != null)
            {
                int iconSize = this.Height - this.Padding.Top - this.Padding.Bottom;
                int iconY = this.Padding.Top;
                g.DrawImage(menuIcon, currentX, iconY, iconSize, iconSize);
                currentX += iconSize + 8;
            }

            // 텍스트 그리기
            Font font = new Font("맑은 고딕", 11F, isSelected ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
            Color textColor = isSelected ? selectedTextColor : normalTextColor;
            
            SizeF textSize = g.MeasureString(menuText, font);
            float textY = (this.Height - textSize.Height) / 2;
            
            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                g.DrawString(menuText, font, textBrush, currentX, textY);
            }
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
    }
}

