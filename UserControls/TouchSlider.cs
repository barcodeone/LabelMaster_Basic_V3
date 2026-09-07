using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    public partial class TouchSlider : UserControl
    {
        private int minimum = 0;
        private int maximum = 100;
        private int value = 0;

        private int trackHeight = 12;           // 터치 친화적인 두꺼운 트랙
        private int thumbSize = 28;             // 큰 썸 크기
        private int paddingHorizontal = 10;     // 좌우 여백

        private bool isDragging = false;
        private Rectangle thumbRect = Rectangle.Empty;

        public event EventHandler ValueChanged;

        public TouchSlider()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.Height = Math.Max(this.Height, thumbSize + 8);
            this.TabStop = true; // 포커스 허용 (키보드 입력용)
            this.SetStyle(ControlStyles.Selectable, true);
            RecalculateThumb();
        }

        [DefaultValue(0)]
        public int Minimum
        {
            get { return minimum; }
            set
            {
                minimum = value;
                if (maximum < minimum) maximum = minimum;
                if (this.value < minimum) this.value = minimum;
                RecalculateThumb();
                Invalidate();
            }
        }

        [DefaultValue(100)]
        public int Maximum
        {
            get { return maximum; }
            set
            {
                maximum = value;
                if (minimum > maximum) minimum = maximum;
                if (this.value > maximum) this.value = maximum;
                RecalculateThumb();
                Invalidate();
            }
        }

        [DefaultValue(0)]
        public int Value
        {
            get { return this.value; }
            set
            {
                int clamped = Math.Max(minimum, Math.Min(maximum, value));
                if (clamped == this.value) return;
                this.value = clamped;
                RecalculateThumb();
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 트랙 영역 계산
            int trackY = (this.Height - trackHeight) / 2;
            Rectangle trackRect = new Rectangle(paddingHorizontal, trackY, this.Width - paddingHorizontal * 2, trackHeight);

            // 트랙 배경
            using (var trackBrush = new SolidBrush(Color.FromArgb(230, 230, 235)))
            using (var borderPen = new Pen(Color.FromArgb(180, 180, 190)))
            using (var path = RoundedRect(trackRect, trackHeight / 2))
            {
                e.Graphics.FillPath(trackBrush, path);
                e.Graphics.DrawPath(borderPen, path);
            }

            // 진행된 영역
            int filledWidth = Math.Max(0, thumbRect.X + thumbRect.Width / 2 - trackRect.X);
            Rectangle filledRect = new Rectangle(trackRect.X, trackRect.Y, filledWidth, trackRect.Height);
            
            // Enabled 상태에 따라 색상 변경
            Color fillColor = this.Enabled 
                ? Color.DodgerBlue  // 활성화: 진한 파란색
                : Color.FromArgb(180, 200, 230); // 비활성화: 옅은 파란색
            
            using (var filledBrush = new SolidBrush(fillColor))
            using (var filledPath = RoundedRect(filledRect, trackHeight / 2))
            {
                e.Graphics.FillPath(filledBrush, filledPath);
            }

            // 썸 그리기 ('|' 모양의 세로선)
            // Enabled 상태에 따라 썸 색상 변경
            Color thumbColor = this.Enabled 
                ? Color.FromArgb(80, 80, 90)  // 활성화: 진한 회색
                : Color.FromArgb(180, 180, 190); // 비활성화: 옅은 회색
            
            using (var thumbPen = new Pen(thumbColor, 4))  // 두꺼운 세로선
            using (var thumbHighlight = new Pen(this.Enabled ? Color.White : Color.FromArgb(220, 220, 220), 2))  // 하이라이트
            {
                int centerX = thumbRect.X + thumbRect.Width / 2;
                int lineTop = trackRect.Y - 4;  // 트랙보다 위로 4px 확장
                int lineBottom = trackRect.Bottom + 4;  // 트랙보다 아래로 4px 확장
                
                // 그림자 효과 (활성화 시에만)
                if (this.Enabled)
                {
                    using (var shadowPen = new Pen(Color.FromArgb(40, 0, 0, 0), 4))
                    {
                        e.Graphics.DrawLine(shadowPen, centerX + 1, lineTop + 1, centerX + 1, lineBottom + 1);
                    }
                }
                
                // 메인 세로선
                e.Graphics.DrawLine(thumbPen, centerX, lineTop, centerX, lineBottom);
                
                // 하이라이트 (왼쪽에 얇은 흰색 선)
                e.Graphics.DrawLine(thumbHighlight, centerX - 1, lineTop, centerX - 1, lineBottom);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (!thumbRect.Contains(e.Location))
            {
                // 트랙을 탭하면 해당 위치로 이동
                Value = PixelToValue(e.X);
            }
            isDragging = true;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (isDragging)
            {
                Value = PixelToValue(e.X);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isDragging = false;
            Capture = false;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            RecalculateThumb();
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate(); // Enabled 상태 변경 시 다시 그리기
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (!Enabled) return;
            
            int step = 1; // 기본 단계
            
            // Ctrl 키가 눌려 있으면 큰 단계로 이동
            if (e.Control)
            {
                step = Math.Max(1, (maximum - minimum) / 20); // 5% 단계
            }
            
            // Arrow Keys
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down)
            {
                Value = Math.Max(minimum, Value - step);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up)
            {
                Value = Math.Min(maximum, Value + step);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Home)
            {
                Value = minimum;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.End)
            {
                Value = maximum;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                Value = Math.Max(minimum, Value - (maximum - minimum) / 10); // 10% 감소
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                Value = Math.Min(maximum, Value + (maximum - minimum) / 10); // 10% 증가
                e.Handled = true;
            }
        }

        private void RecalculateThumb()
        {
            // 현재 값 기준 썸 위치 계산
            int trackLeft = paddingHorizontal;
            int trackRight = this.Width - paddingHorizontal;
            int trackWidth = Math.Max(1, trackRight - trackLeft);
            double t = (maximum == minimum) ? 0 : (double)(value - minimum) / (maximum - minimum);
            int centerX = trackLeft + (int)Math.Round(t * trackWidth);
            int centerY = this.Height / 2;
            thumbRect = new Rectangle(centerX - thumbSize / 2, centerY - thumbSize / 2, thumbSize, thumbSize);
        }

        private int PixelToValue(int x)
        {
            int trackLeft = paddingHorizontal;
            int trackRight = this.Width - paddingHorizontal;
            if (x <= trackLeft) return minimum;
            if (x >= trackRight) return maximum;
            double t = (double)(x - trackLeft) / (trackRight - trackLeft);
            int v = minimum + (int)Math.Round(t * (maximum - minimum));
            return Math.Max(minimum, Math.Min(maximum, v));
        }

        private System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}


