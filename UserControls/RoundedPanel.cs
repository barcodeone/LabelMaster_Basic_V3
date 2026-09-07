using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    /// <summary>
    /// 둥근 모서리를 가진 Panel 커스텀 컨트롤
    /// </summary>
    public class RoundedPanel : Panel
    {
        private Color borderColor = Color.LightGray;
        private int borderRadius = 12; // 모서리 둥글기
        private int borderWidth = 1;

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

        public int BorderWidth
        {
            get { return borderWidth; }
            set { borderWidth = value; Invalidate(); }
        }

        public RoundedPanel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                    ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.ResizeRedraw | 
                    ControlStyles.UserPaint, true);

            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.None;
            
            // BackgroundImage 변경 시 자동 다시 그리기
            this.BackgroundImageChanged += (s, e) => this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            
            // BackgroundImage가 있으면 먼저 그리기
            if (this.BackgroundImage != null)
            {
                // BackgroundImageLayout에 따라 이미지 그리기
                switch (this.BackgroundImageLayout)
                {
                    case ImageLayout.Center:
                        int x = (this.Width - this.BackgroundImage.Width) / 2;
                        int y = (this.Height - this.BackgroundImage.Height) / 2;
                        g.DrawImage(this.BackgroundImage, x, y);
                        break;
                    case ImageLayout.Stretch:
                        g.DrawImage(this.BackgroundImage, rect);
                        break;
                    case ImageLayout.Zoom:
                        DrawImageWithAspectRatio(g, this.BackgroundImage, rect);
                        break;
                    default:
                        g.DrawImage(this.BackgroundImage, 0, 0);
                        break;
                }
            }
            
            // 둥근 테두리 그리기
            using (GraphicsPath path = GetRoundedRectanglePath(rect, borderRadius))
            {
                using (Pen pen = new Pen(borderColor, borderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        private void DrawImageWithAspectRatio(Graphics g, Image image, Rectangle rect)
        {
            float imageRatio = (float)image.Width / image.Height;
            float rectRatio = (float)rect.Width / rect.Height;
            
            int newWidth, newHeight, x, y;
            
            if (imageRatio > rectRatio)
            {
                newWidth = rect.Width;
                newHeight = (int)(rect.Width / imageRatio);
                x = 0;
                y = (rect.Height - newHeight) / 2;
            }
            else
            {
                newHeight = rect.Height;
                newWidth = (int)(rect.Height * imageRatio);
                x = (rect.Width - newWidth) / 2;
                y = 0;
            }
            
            g.DrawImage(image, x, y, newWidth, newHeight);
        }

        private GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            // 왼쪽 상단
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            // 오른쪽 상단
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            // 오른쪽 하단
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            // 왼쪽 하단
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            
            path.CloseFigure();
            return path;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            
            // 배경 이미지가 없는 경우에만 배경색 채우기
            if (this.BackgroundImage == null)
            {
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }
            }
        }
    }
}

