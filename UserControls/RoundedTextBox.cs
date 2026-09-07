using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    /// <summary>
    /// 둥근 모서리를 가진 TextBox 커스텀 컨트롤
    /// </summary>
    public class RoundedTextBox : UserControl
    {
        private TextBox textBox;
        private Color borderColor = Color.LightGray;
        private Color focusBorderColor = Color.DodgerBlue;
        private int borderRadius = 8; // 모서리 둥글기
        private int borderWidth = 2;
        private bool isFocused = false;

        // TextBox의 속성들을 외부에 노출
        public override string Text
        {
            get { return textBox?.Text ?? ""; }
            set { if (textBox != null) textBox.Text = value; }
        }

        public int SelectionStart
        {
            get { return textBox?.SelectionStart ?? 0; }
            set { if (textBox != null) textBox.SelectionStart = value; }
        }

        public int SelectionLength
        {
            get { return textBox?.SelectionLength ?? 0; }
            set { if (textBox != null) textBox.SelectionLength = value; }
        }

        public bool Multiline
        {
            get { return textBox?.Multiline ?? false; }
            set 
            { 
                if (textBox != null) 
                {
                    textBox.Multiline = value;
                    if (value)
                    {
                        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
                        textBox.Height = this.Height - 26;
                    }
                    else
                    {
                        textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                        textBox.Height = 22;
                    }
                }
            }
        }

        public bool AcceptsReturn
        {
            get { return textBox?.AcceptsReturn ?? false; }
            set { if (textBox != null) textBox.AcceptsReturn = value; }
        }

        public HorizontalAlignment TextAlign
        {
            get { return textBox?.TextAlign ?? HorizontalAlignment.Left; }
            set { if (textBox != null) textBox.TextAlign = value; }
        }

        public Font TextBoxFont
        {
            get { return textBox?.Font; }
            set { if (textBox != null) textBox.Font = value; }
        }

        public Color TextBoxForeColor
        {
            get { return textBox?.ForeColor ?? Color.Black; }
            set { if (textBox != null) textBox.ForeColor = value; }
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

        public Color FocusBorderColor
        {
            get { return focusBorderColor; }
            set { focusBorderColor = value; Invalidate(); }
        }

        // TextBox 이벤트 전달
        public new event EventHandler TextChanged;
        public new event KeyEventHandler KeyDown;
        public new event KeyEventHandler KeyUp;
        public new event EventHandler Enter;
        public new event EventHandler Leave;

        public RoundedTextBox()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                    ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.ResizeRedraw | 
                    ControlStyles.UserPaint, true);

            this.Size = new Size(280, 48);
            this.BackColor = Color.White;
            this.Padding = new Padding(borderRadius / 2);

            // 내부 TextBox 생성
            textBox = new TextBox();
            textBox.BorderStyle = BorderStyle.None;
            textBox.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point);
            textBox.BackColor = Color.White;
            textBox.Location = new Point(10, 13);
            textBox.Size = new Size(this.Width - 20, 22);
            textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            // 이벤트 연결
            textBox.TextChanged += (s, e) => TextChanged?.Invoke(this, e);
            textBox.KeyDown += (s, e) => KeyDown?.Invoke(this, e);
            textBox.KeyUp += (s, e) => KeyUp?.Invoke(this, e);
            textBox.Enter += (s, e) => 
            { 
                isFocused = true; 
                Invalidate();
                Enter?.Invoke(this, e);
            };
            textBox.Leave += (s, e) => 
            { 
                isFocused = false; 
                Invalidate();
                Leave?.Invoke(this, e);
            };

            this.Controls.Add(textBox);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 배경 그리기
            using (GraphicsPath path = GetRoundedRectanglePath(new Rectangle(0, 0, this.Width - 1, this.Height - 1), borderRadius))
            {
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    g.FillPath(brush, path);
                }

                // 테두리 그리기
                Color currentBorderColor = isFocused ? focusBorderColor : borderColor;
                using (Pen pen = new Pen(currentBorderColor, borderWidth))
                {
                    g.DrawPath(pen, path);
                }
            }
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (textBox != null)
            {
                if (textBox.Multiline)
                {
                    textBox.Size = new Size(this.Width - 20, this.Height - 26);
                }
                else
                {
                    textBox.Size = new Size(this.Width - 20, textBox.Height);
                }
            }
            Invalidate();
        }

        // TextBox에 포커스 이동
        public new void Focus()
        {
            textBox?.Focus();
        }

        // 텍스트 선택
        public void SelectAll()
        {
            textBox?.SelectAll();
        }

        // 텍스트 클리어
        public void Clear()
        {
            if (textBox != null)
                textBox.Text = "";
        }
    }
}

