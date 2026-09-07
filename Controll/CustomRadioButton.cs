using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MaterialSkin;

namespace BarcodePrint.Controll
{
    public class CustomRadioButton : Control
    {
        private bool _checked = false;
        private bool _hover = false;
        private bool _pressed = false;
        private int _depth = 0;
        private bool _ripple = true;
        private MouseState _mouseState = MouseState.OUT;
        private Point _mouseLocation = new Point(-1, -1);

        public event EventHandler CheckedChanged;

        public CustomRadioButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.UserPaint | 
                    ControlStyles.DoubleBuffer | 
                    ControlStyles.ResizeRedraw | 
                    ControlStyles.SupportsTransparentBackColor, true);
            
            Size = new Size(83, 37);
            Font = new Font("굴림", 12F, FontStyle.Regular, GraphicsUnit.Point);
            BackColor = Color.Transparent;
            ForeColor = Color.FromArgb(222, 0, 0, 0);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    OnCheckedChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        public int Depth
        {
            get { return _depth; }
            set { _depth = value; }
        }

        public bool Ripple
        {
            get { return _ripple; }
            set { _ripple = value; }
        }

        public MouseState MouseState
        {
            get { return _mouseState; }
            set { _mouseState = value; }
        }

        public Point MouseLocation
        {
            get { return _mouseLocation; }
            set { _mouseLocation = value; }
        }

        public bool UseVisualStyleBackColor
        {
            get { return true; }
            set { }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            _mouseState = MouseState.HOVER;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _mouseState = MouseState.OUT;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                _mouseState = MouseState.DOWN;
                _mouseLocation = e.Location;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = false;
                _mouseState = MouseState.HOVER;
                if (_hover)
                {
                    Checked = true;
                }
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected virtual void OnCheckedChanged(EventArgs e)
        {
            // 같은 부모 컨트롤의 다른 라디오 버튼들을 해제
            if (_checked && Parent != null)
            {
                foreach (Control control in Parent.Controls)
                {
                    if (control is CustomRadioButton otherRadio && otherRadio != this)
                    {
                        otherRadio.Checked = false;
                    }
                }
            }
            
            CheckedChanged?.Invoke(this, e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 배경 그리기
            using (SolidBrush bgBrush = new SolidBrush(BackColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // 라디오 버튼 원형 크기를 글자 크기보다 조금 작게 설정
            int radioSize = Math.Max(18, Font.Height - 8); // 글자 크기보다 8px 작게, 최소 18px
            int radioX = 2;
            int radioY = (Height - radioSize) / 2 - 2; // 위로 2px 올림

            // 외부 원형 테두리 (푸른색 계열)
            Color borderColor = Color.FromArgb(255, 33, 150, 243); // Material Blue
            using (Pen borderPen = new Pen(borderColor, 2f))
            {
                g.DrawEllipse(borderPen, radioX, radioY, radioSize, radioSize);
            }

            // 내부 원형 (선택된 경우) - 푸른색 계열
            if (_checked)
            {
                int innerSize = radioSize - 8; // 테두리와의 여백 고려
                int innerX = radioX + (radioSize - innerSize) / 2;
                int innerY = radioY + (radioSize - innerSize) / 2;
                
                using (SolidBrush fillBrush = new SolidBrush(borderColor))
                {
                    g.FillEllipse(fillBrush, innerX, innerY, innerSize, innerSize);
                }
            }

            // 텍스트 그리기
            using (SolidBrush textBrush = new SolidBrush(ForeColor))
            {
                StringFormat sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                sf.Alignment = StringAlignment.Near;
                
                Rectangle textRect = new Rectangle(radioSize + 8, 0, Width - radioSize - 8, Height);
                g.DrawString(Text, Font, textBrush, textRect, sf);
            }

            // 호버 효과 (푸른색 계열)
            if (_hover)
            {
                using (SolidBrush hoverBrush = new SolidBrush(Color.FromArgb(30, 33, 150, 243)))
                {
                    g.FillRectangle(hoverBrush, ClientRectangle);
                }
            }

            // 프레스 효과
            if (_pressed)
            {
                using (SolidBrush pressBrush = new SolidBrush(Color.FromArgb(30, 63, 63, 63)))
                {
                    g.FillRectangle(pressBrush, ClientRectangle);
                }
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = true;
            base.OnClick(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                Checked = true;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }
    }

    public enum MouseState
    {
        HOVER,
        OUT,
        DOWN
    }
}
