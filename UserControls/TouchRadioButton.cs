using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    /// <summary>
    /// 터치 최적화 라디오버튼 컨트롤
    /// </summary>
    public class TouchRadioButton : Control
    {
        private bool isChecked = false;
        private bool isHovered = false;
        private bool isPressed = false;
        private string groupName = "default";

        public event EventHandler CheckedChanged;

        /// <summary>
        /// 라디오버튼 선택 상태
        /// </summary>
        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
                    
                    // 체크된 경우 같은 그룹의 다른 라디오버튼 해제
                    if (isChecked && Parent != null)
                    {
                        UncheckOtherRadioButtons();
                    }
                    
                    OnCheckedChanged();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 라디오버튼 그룹 컬럼3
        /// </summary>
        public string GroupName
        {
            get { return groupName; }
            set { groupName = value; }
        }

        /// <summary>
        /// 라디오버튼 텍스트
        /// </summary>
        public string RadioButtonText
        {
            get { return Text; }
            set { Text = value; Invalidate(); }
        }

        public TouchRadioButton()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint, true);

            this.Size = new Size(140, 50);
            this.BackColor = Color.Transparent;
            this.Cursor = Cursors.Hand;
            this.TabStop = true;
            this.Text = "옵션";
            this.Font = new Font("맑은 고딕", 12F, FontStyle.Bold, GraphicsUnit.Point);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // 라디오버튼 원 크기 (터치 친화적)
            int circleSize = 28;
            int circleCenterY = Height / 2;
            int circleMargin = 10;

            // Enabled 상태에 따라 색상 변경
            Color circleBgColor;
            Color borderColor;
            Color textColor;
            Color dotColor;

            if (this.Enabled)
            {
                // 활성화 상태
                if (isPressed)
                {
                    circleBgColor = Color.FromArgb(230, 240, 250);
                    borderColor = Color.FromArgb(0, 90, 180);
                }
                else if (isHovered)
                {
                    circleBgColor = Color.FromArgb(240, 248, 255);
                    borderColor = Color.DodgerBlue;
                }
                else
                {
                    circleBgColor = Color.White;
                    borderColor = isChecked ? Color.DodgerBlue : Color.FromArgb(180, 180, 180);
                }
                textColor = Color.Black;
                dotColor = Color.DodgerBlue;
            }
            else
            {
                // 비활성화 상태
                circleBgColor = Color.FromArgb(245, 245, 245);
                borderColor = Color.LightGray;
                textColor = Color.LightGray;
                dotColor = Color.LightGray;
            }

            // 라디오버튼 배경 (전체 컨트롤)
            if (isPressed || isHovered)
            {
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(30, 0, 120, 215)))
                {
                    using (GraphicsPath path = GetRoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), 8))
                    {
                        g.FillPath(bgBrush, path);
                    }
                }
            }

            // 원형 배경
            using (SolidBrush circleBrush = new SolidBrush(circleBgColor))
            {
                g.FillEllipse(circleBrush, circleMargin, circleCenterY - circleSize / 2, circleSize, circleSize);
            }

            // 원형 테두리
            int borderWidth = isChecked ? 3 : 2;
            using (Pen borderPen = new Pen(borderColor, borderWidth))
            {
                g.DrawEllipse(borderPen, circleMargin, circleCenterY - circleSize / 2, circleSize, circleSize);
            }

            // 선택 표시 (중앙 점)
            if (isChecked)
            {
                int dotSize = 14;
                using (SolidBrush dotBrush = new SolidBrush(dotColor))
                {
                    g.FillEllipse(dotBrush,
                        circleMargin + (circleSize - dotSize) / 2,
                        circleCenterY - dotSize / 2,
                        dotSize,
                        dotSize);
                }
            }

            // 텍스트 그리기
            using (SolidBrush textBrush = new SolidBrush(textColor))
            {
                StringFormat sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Near;

                int textMargin = circleMargin + circleSize + 10;
                Rectangle textRect = new Rectangle(textMargin, 0, Width - textMargin - 5, Height);
                g.DrawString(Text, this.Font, textBrush, textRect, sf);
            }

            base.OnPaint(e);
        }

        /// <summary>
        /// 둥근 모서리 사각형 경로 생성
        /// </summary>
        private GraphicsPath GetRoundedRectangle(Rectangle rect, int radius)
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

        /// <summary>
        /// 같은 그룹의 다른 라디오버튼 해제
        /// </summary>
        private void UncheckOtherRadioButtons()
        {
            if (Parent == null) return;

            foreach (Control control in Parent.Controls)
            {
                if (control is TouchRadioButton otherRadio &&
                    otherRadio != this &&
                    otherRadio.GroupName == this.GroupName)
                {
                    otherRadio.isChecked = false;
                    otherRadio.Invalidate();
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            if (this.Enabled)
            {
                isHovered = true;
                Invalidate();
            }
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
                
                // 이미 선택된 경우에도 이벤트는 발생
                if (!Checked)
                {
                    Checked = true;
                }
                else
                {
                    Invalidate();
                }
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                if (!Checked)
                {
                    Checked = true;
                }
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected virtual void OnCheckedChanged()
        {
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

