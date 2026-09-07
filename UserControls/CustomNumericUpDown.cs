using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarcodePrint.UserControls
{
    public class CustomNumericUpDown : Control
    {
        private TextBox textBox;
        private Label label;
        private Panel inputPanel;
        private Panel btnUp;
        private Panel btnDown;
        private string labelText = "";
        private decimal currentValue = 1;
        private decimal minimum = 1;
        private decimal maximum = 999;
        private decimal increment = 1;
        private Color borderColor = Color.DodgerBlue;
        private Color focusBorderColor = Color.FromArgb(30, 144, 255);
        private bool isFocused = false;
        private bool isUpHovered = false;
        private bool isDownHovered = false;
        private bool isUpPressed = false;
        private bool isDownPressed = false;

        public event EventHandler ValueChanged;

        public string LabelText
        {
            get { return labelText; }
            set 
            { 
                labelText = value;
                if (label != null)
                {
                    label.Text = value;
                    label.Visible = !string.IsNullOrEmpty(labelText);
                    label.Bounds = string.IsNullOrEmpty(labelText) ? new Rectangle(0, 0, 0, 0) : new Rectangle(0, 16, 70, 25);
                    if (inputPanel != null)
                    {
                        int x = string.IsNullOrEmpty(labelText) ? 0 : 73;
                        inputPanel.Location = new Point(x, 5);
                    }
                }
            }
        }

        public decimal Value
        {
            get { return currentValue; }
            set 
            { 
                decimal newValue = Math.Max(minimum, Math.Min(maximum, value));
                if (currentValue != newValue)
                {
                    currentValue = newValue;
                    if (textBox != null)
                        textBox.Text = currentValue.ToString("0");
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public decimal Maximum
        {
            get { return maximum; }
            set { maximum = value; }
        }

        public decimal Minimum
        {
            get { return minimum; }
            set { minimum = value; }
        }

        public decimal Increment
        {
            get { return increment; }
            set { increment = value; }
        }

        public CustomNumericUpDown()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor | 
                    ControlStyles.OptimizedDoubleBuffer | 
                    ControlStyles.AllPaintingInWmPaint | 
                    ControlStyles.ResizeRedraw | 
                    ControlStyles.UserPaint, true);

            this.Size = new Size(200, 55);
            this.BackColor = Color.Transparent;

            // 라벨 생성 (LabelText가 있을 때만 표시)
            label = new Label();
            label.AutoSize = false;
            label.Font = new Font("맑은 고딕", 11F, FontStyle.Bold, GraphicsUnit.Point);
            label.ForeColor = Color.FromArgb(60, 60, 60);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Text = labelText;
            label.BackColor = Color.Transparent;
            label.Visible = !string.IsNullOrEmpty(labelText);
            label.Bounds = string.IsNullOrEmpty(labelText) ? new Rectangle(0, 0, 0, 0) : new Rectangle(0, 16, 70, 25);
             
            // 입력 패널 생성 (LabelText 비어있으면 (0,5)에 배치)
            inputPanel = new Panel();
            inputPanel.Location = new Point(string.IsNullOrEmpty(labelText) ? 0 : 73, 5);
            inputPanel.Size = new Size(125, 45);
            inputPanel.BackColor = Color.White;

            // TextBox 생성 (숫자 입력)
            textBox = new TextBox();
            textBox.Font = new Font("맑은 고딕", 18F, FontStyle.Bold, GraphicsUnit.Point);
            textBox.BorderStyle = BorderStyle.None;
            textBox.Location = new Point(10, 10);
            textBox.Size = new Size(50, 30);
            textBox.TextAlign = HorizontalAlignment.Center;
            textBox.Text = currentValue.ToString("0");
            textBox.BackColor = Color.White;
            textBox.GotFocus += (s, e) => { isFocused = true; inputPanel.Invalidate(); };
            textBox.LostFocus += (s, e) => 
            { 
                isFocused = false; 
                // 입력값 검증 및 적용
                if (decimal.TryParse(textBox.Text, out decimal val))
                {
                    Value = val;
                }
                else
                {
                    textBox.Text = currentValue.ToString("0");
                }
                inputPanel.Invalidate(); 
            };
            textBox.KeyPress += TextBox_KeyPress;
            textBox.KeyDown += TextBox_KeyDown;

            // 위쪽 버튼 생성 (패널 방식 - 터치 최적화 크기, 테두리 여백 확보)
            btnUp = new Panel();
            btnUp.Size = new Size(56, 20);
            btnUp.Location = new Point(66, 2);
            btnUp.BackColor = Color.White;
            btnUp.Cursor = Cursors.Hand;
            btnUp.Paint += BtnUp_Paint;
            btnUp.Click += (s, e) => BtnUp_Click();
            btnUp.MouseEnter += (s, e) => { isUpHovered = true; btnUp.Invalidate(); };
            btnUp.MouseLeave += (s, e) => { isUpHovered = false; isUpPressed = false; btnUp.Invalidate(); };
            btnUp.MouseDown += (s, e) => { isUpPressed = true; btnUp.Invalidate(); };
            btnUp.MouseUp += (s, e) => { isUpPressed = false; btnUp.Invalidate(); };

            // 아래쪽 버튼 생성 (패널 방식 - 터치 최적화 크기, 테두리 여백 확보)
            btnDown = new Panel();
            btnDown.Size = new Size(56, 20);
            btnDown.Location = new Point(66, 23);
            btnDown.BackColor = Color.White;
            btnDown.Cursor = Cursors.Hand;
            btnDown.Paint += BtnDown_Paint;
            btnDown.Click += (s, e) => BtnDown_Click();
            btnDown.MouseEnter += (s, e) => { isDownHovered = true; btnDown.Invalidate(); };
            btnDown.MouseLeave += (s, e) => { isDownHovered = false; isDownPressed = false; btnDown.Invalidate(); };
            btnDown.MouseDown += (s, e) => { isDownPressed = true; btnDown.Invalidate(); };
            btnDown.MouseUp += (s, e) => { isDownPressed = false; btnDown.Invalidate(); };

            inputPanel.Controls.Add(textBox);
            inputPanel.Controls.Add(btnUp);
            inputPanel.Controls.Add(btnDown);
            inputPanel.Paint += InputPanel_Paint;

            this.Controls.Add(label);
            this.Controls.Add(inputPanel);
        }

        private void InputPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 둥근 테두리 그리기 (주소라벨 방식 참고: 전체 영역을 안정적으로 사용)
            int radius = 6;
            Rectangle rect = new Rectangle(0, 0, inputPanel.Width - 1, inputPanel.Height - 1);
            
            using (GraphicsPath path = GetRoundedRectangle(rect, radius))
            {
                // 배경 그리기
                using (SolidBrush bgBrush = new SolidBrush(Color.White))
                {
                    g.FillPath(bgBrush, path);
                }

                // 테두리 그리기
                using (Pen pen = new Pen(isFocused ? focusBorderColor : borderColor, 1.5f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // TextBox와 버튼 사이 구분선 (버튼 위치와 연동)
            int sepX = btnUp != null ? btnUp.Left - 3 : inputPanel.Width - 62;
            using (Pen separatorPen = new Pen(borderColor, 1))
            {
                g.DrawLine(separatorPen, sepX, 6, sepX, inputPanel.Height - 6);
            }
        }

        private void BtnUp_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 배경색 (호버/클릭 효과)
            Color bgColor = Color.White;
            if (isUpPressed)
                bgColor = Color.FromArgb(180, 200, 230);
            else if (isUpHovered)
                bgColor = Color.FromArgb(220, 235, 255);

            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                g.FillRectangle(bgBrush, btnUp.ClientRectangle);
            }

            // 위쪽 화살표 그리기 (삼각형 - 터치 최적화 크기)
            Point[] arrowPoints = new Point[]
            {
                new Point(btnUp.Width / 2, 5),           // 상단 점
                new Point(btnUp.Width / 2 - 7, 14),      // 좌하단 점
                new Point(btnUp.Width / 2 + 7, 14)       // 우하단 점
            };

            using (SolidBrush arrowBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
            {
                g.FillPolygon(arrowBrush, arrowPoints);
            }
        }

        private void BtnDown_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 배경색 (호버/클릭 효과)
            Color bgColor = Color.White;
            if (isDownPressed)
                bgColor = Color.FromArgb(180, 200, 230);
            else if (isDownHovered)
                bgColor = Color.FromArgb(220, 235, 255);

            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                g.FillRectangle(bgBrush, btnDown.ClientRectangle);
            }

            // 아래쪽 화살표 그리기 (삼각형 - 터치 최적화 크기)
            Point[] arrowPoints = new Point[]
            {
                new Point(btnDown.Width / 2, 14),        // 하단 점
                new Point(btnDown.Width / 2 - 7, 6),     // 좌상단 점
                new Point(btnDown.Width / 2 + 7, 6)      // 우상단 점
            };

            using (SolidBrush arrowBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
            {
                g.FillPolygon(arrowBrush, arrowPoints);
            }
        }

        private void BtnUp_Click()
        {
            Value = Math.Min(maximum, currentValue + increment);
        }

        private void BtnDown_Click()
        {
            Value = Math.Max(minimum, currentValue - increment);
        }

        private void TextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // 숫자와 백스페이스만 허용
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != (char)Keys.Back)
            {
                e.Handled = true;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 위/아래 화살표 키로 값 증가/감소
            if (e.KeyCode == Keys.Up)
            {
                Value = Math.Min(maximum, currentValue + increment);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                Value = Math.Max(minimum, currentValue - increment);
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // 배경은 투명, inputPanel에서 테두리 그림
        }

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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (inputPanel == null) return;

            if (string.IsNullOrEmpty(labelText))
            {
                inputPanel.Location = new Point(0, (this.Height - inputPanel.Height) / 2);
                if (label != null) { label.Visible = false; label.Bounds = new Rectangle(0, 0, 0, 0); }
            }
            else
            {
                inputPanel.Location = new Point(this.Width - 125, (this.Height - inputPanel.Height) / 2);
                if (label != null) { label.Visible = true; label.Bounds = new Rectangle(0, 16, 70, 25); }
            }

            if (textBox != null)
            {
                textBox.Location = new Point(10, 10);
                textBox.Size = new Size(50, 30);
            }
            if (btnUp != null && btnDown != null)
            {
                btnUp.Size = new Size(56, 20);
                btnUp.Location = new Point(66, 2);
                btnDown.Size = new Size(56, 20);
                btnDown.Location = new Point(66, 23);
            }
            inputPanel.Invalidate();
        }
    }
}
