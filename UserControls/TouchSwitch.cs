using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    /// <summary>
    /// iOS 스타일의 On/Off 스위치 컨트롤
    /// </summary>
    public class TouchSwitch : Control
    {
        private bool isChecked = false;
        private bool isHovered = false;
        private bool isPressed = false;
        private float animationValue = 0f; // 0.0 (Off) ~ 1.0 (On)
        private Timer animationTimer;

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
                    AnimateSwitch();
                    OnCheckedChanged();
                }
            }
        }

        public string SwitchText
        {
            get { return Text; }
            set { Text = value; Invalidate(); }
        }

        public TouchSwitch()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.ResizeRedraw, true);

            this.Size = new Size(180, 45);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.TabStop = false;
            this.Text = "Switch";

            // 애니메이션 타이머 초기화
            animationTimer = new Timer();
            animationTimer.Interval = 10; // 10ms 간격
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void AnimateSwitch()
        {
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (isChecked)
            {
                // On으로 애니메이션
                animationValue += 0.15f;
                if (animationValue >= 1.0f)
                {
                    animationValue = 1.0f;
                    animationTimer.Stop();
                }
            }
            else
            {
                // Off로 애니메이션
                animationValue -= 0.15f;
                if (animationValue <= 0.0f)
                {
                    animationValue = 0.0f;
                    animationTimer.Stop();
                }
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 스위치 크기
            int switchWidth = 60;
            int switchHeight = 30;
            int switchX = 0; // 왼쪽에 배치 (텍스트는 외부 라벨로 처리)
            int switchY = (Height - switchHeight) / 2;

            // 배경 색상 (애니메이션)
            Color bgColor;
            if (this.Enabled)
            {
                // On 상태: 초록색, Off 상태: 회색
                int r = (int)(200 + (76 - 200) * animationValue);
                int g_val = (int)(200 + (175 - 200) * animationValue);
                int b = (int)(200 + (80 - 200) * animationValue);
                bgColor = Color.FromArgb(r, g_val, b);
            }
            else
            {
                // 비활성화 상태
                bgColor = Color.FromArgb(220, 220, 220);
            }

            // 스위치 배경 그리기 (둥근 사각형)
            using (GraphicsPath path = GetRoundedRectPath(switchX, switchY, switchWidth, switchHeight, switchHeight / 2))
            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                g.FillPath(bgBrush, path);
            }

            // 스위치 토글 버튼 (원)
            int toggleSize = switchHeight - 4;
            int toggleX = (int)(switchX + 2 + (switchWidth - toggleSize - 4) * animationValue);
            int toggleY = switchY + 2;

            Color toggleColor = this.Enabled ? Color.White : Color.FromArgb(200, 200, 200);
            using (SolidBrush toggleBrush = new SolidBrush(toggleColor))
            {
                g.FillEllipse(toggleBrush, toggleX, toggleY, toggleSize, toggleSize);
            }
        }

        private GraphicsPath GetRoundedRectPath(int x, int y, int width, int height, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Stop();
                animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

