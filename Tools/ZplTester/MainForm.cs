using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;

namespace ZplTester
{
    public class MainForm : Form
    {
        private ComboBox cboPrinters;
        private Button btnRefreshPrinters;
        private NumericUpDown numWidthMm;
        private NumericUpDown numHeightMm;
        private TextBox txtZpl;
        private TextBox txtLog;
        private Button btnPrint;

        public MainForm()
        {
            InitializeComponent();
            LoadPrinters();
            LoadSample1(); // 기본 샘플 로드
        }

        private void InitializeComponent()
        {
            this.Text = "ZPL 명령어 지원 테스트 도구 (Xprinter & Zebra)";
            this.Size = new Size(820, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("맑은 고딕", 9.5f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(248, 249, 250);

            // 1. 상단 프린터 선택 영역
            GroupBox grpPrinter = new GroupBox
            {
                Text = "1. 프린터 선택 및 용지 크기",
                Location = new Point(15, 12),
                Size = new Size(775, 75),
                BackColor = Color.White
            };

            Label lblPrinter = new Label { Text = "프린터:", Location = new Point(15, 30), AutoSize = true };
            cboPrinters = new ComboBox
            {
                Location = new Point(70, 27),
                Size = new Size(330, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnRefreshPrinters = new Button
            {
                Text = "새로고침",
                Location = new Point(410, 25),
                Size = new Size(75, 32),
                BackColor = Color.FromArgb(235, 238, 242)
            };
            btnRefreshPrinters.Click += (s, e) => LoadPrinters();

            Label lblWidth = new Label { Text = "너비(mm):", Location = new Point(510, 30), AutoSize = true };
            numWidthMm = new NumericUpDown
            {
                Location = new Point(580, 27),
                Size = new Size(55, 28),
                Minimum = 10,
                Maximum = 200,
                Value = 60
            };

            Label lblHeight = new Label { Text = "높이(mm):", Location = new Point(650, 30), AutoSize = true };
            numHeightMm = new NumericUpDown
            {
                Location = new Point(715, 27),
                Size = new Size(50, 28),
                Minimum = 10,
                Maximum = 300,
                Value = 40
            };

            grpPrinter.Controls.AddRange(new Control[] { lblPrinter, cboPrinters, btnRefreshPrinters, lblWidth, numWidthMm, lblHeight, numHeightMm });

            // 2. 샘플 프리셋 영역
            GroupBox grpPresets = new GroupBox
            {
                Text = "2. 테스트 ZPL 프리셋 선택",
                Location = new Point(15, 95),
                Size = new Size(775, 65),
                BackColor = Color.White
            };

            Button btnPreset1 = new Button { Text = "① 텍스트 + 바코드(Code128)", Location = new Point(15, 24), Size = new Size(185, 30) };
            btnPreset1.Click += (s, e) => LoadSample1();

            Button btnPreset2 = new Button { Text = "② QR 코드 테스트", Location = new Point(208, 24), Size = new Size(140, 30) };
            btnPreset2.Click += (s, e) => LoadSample2();

            Button btnPreset3 = new Button { Text = "③ 한글 그래픽(^GF 비트맵)", Location = new Point(356, 24), Size = new Size(185, 30) };
            btnPreset3.Click += (s, e) => LoadSample3();

            Button btnPreset4 = new Button { Text = "④ 테두리 박스 & 라인(^GB)", Location = new Point(549, 24), Size = new Size(175, 30) };
            btnPreset4.Click += (s, e) => LoadSample4();

            grpPresets.Controls.AddRange(new Control[] { btnPreset1, btnPreset2, btnPreset3, btnPreset4 });

            // 3. ZPL 코드 에디터 영역
            GroupBox grpCode = new GroupBox
            {
                Text = "3. 전송할 ZPL 명령어 (직접 수정 가능)",
                Location = new Point(15, 170),
                Size = new Size(775, 270),
                BackColor = Color.White
            };

            txtZpl = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10.5f),
                Location = new Point(15, 25),
                Size = new Size(745, 230),
                BackColor = Color.FromArgb(250, 250, 252)
            };
            grpCode.Controls.Add(txtZpl);

            // 4. 출력 실행 버튼
            btnPrint = new Button
            {
                Text = "🖨️ 선택한 프린터로 ZPL 명령어 전송 (출력 테스트)",
                Font = new Font("맑은 고딕", 11.5f, FontStyle.Bold),
                Location = new Point(15, 450),
                Size = new Size(775, 48),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += BtnPrint_Click;

            // 5. 로그 및 안내 영역
            GroupBox grpLog = new GroupBox
            {
                Text = "전송 결과 및 안내",
                Location = new Point(15, 510),
                Size = new Size(775, 155),
                BackColor = Color.White
            };

            txtLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9.0f),
                Location = new Point(15, 22),
                Size = new Size(745, 120),
                BackColor = Color.FromArgb(245, 245, 245)
            };
            grpLog.Controls.Add(txtLog);

            this.Controls.AddRange(new Control[] { grpPrinter, grpPresets, grpCode, btnPrint, grpLog });
        }

        private void LoadPrinters()
        {
            cboPrinters.Items.Clear();
            string defaultPrinter = string.Empty;

            try
            {
                PrinterSettings settings = new PrinterSettings();
                defaultPrinter = settings.PrinterName;

                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    cboPrinters.Items.Add(printer);
                }

                if (cboPrinters.Items.Count > 0)
                {
                    int defaultIndex = cboPrinters.Items.IndexOf(defaultPrinter);
                    cboPrinters.SelectedIndex = (defaultIndex >= 0) ? defaultIndex : 0;
                }
                Log($"프린터 목록 조회 완료: 총 {cboPrinters.Items.Count}대 검색됨");
            }
            catch (Exception ex)
            {
                Log($"프린터 목록 조회 오류: {ex.Message}");
            }
        }

        private int GetWidthDots() => (int)Math.Round(numWidthMm.Value * 8.0m);
        private int GetHeightDots() => (int)Math.Round(numHeightMm.Value * 8.0m);

        private void LoadSample1()
        {
            int pw = GetWidthDots();
            int ll = GetHeightDots();
            txtZpl.Text =
$"^XA\r\n" +
$"^PW{pw}\r\n" +
$"^LL{ll}\r\n" +
$"^FO40,30^A0N,36,36^FDXprinter ZPL Test^FS\r\n" +
$"^FO40,80^A0N,24,24^FDModel: Auto-Sensing Test^FS\r\n" +
$"^FO40,120^BCN,80,Y,N,N^FD1234567890^FS\r\n" +
$"^XZ\r\n";
            Log("샘플 1 로드: 텍스트 + Code128 바코드");
        }

        private void LoadSample2()
        {
            int pw = GetWidthDots();
            int ll = GetHeightDots();
            txtZpl.Text =
$"^XA\r\n" +
$"^PW{pw}\r\n" +
$"^LL{ll}\r\n" +
$"^FO40,30^A0N,32,32^FDQR-Code ZPL Test^FS\r\n" +
$"^FO40,80^BQN,2,6^FDQA,https://xprinter.co.kr^FS\r\n" +
$"^FO220,100^A0N,26,26^FDScan for Website^FS\r\n" +
$"^FO220,140^A0N,22,22^FDSize: {numWidthMm.Value}x{numHeightMm.Value}mm^FS\r\n" +
$"^XZ\r\n";
            Log("샘플 2 로드: QR 코드 + 텍스트");
        }

        private void LoadSample3()
        {
            try
            {
                int pw = GetWidthDots();
                int ll = GetHeightDots();

                // 200DPI 기준 비트맵 생성 후 한글 그리기
                using (Bitmap bmp = new Bitmap(pw, ll))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        using (Font fTitle = new Font("맑은 고딕", 16, FontStyle.Bold))
                        using (Font fBody = new Font("맑은 고딕", 11, FontStyle.Regular))
                        using (Brush brush = Brushes.Black)
                        {
                            g.DrawString("한글 ZPL 비트맵 출력 테스트", fTitle, brush, 20, 20);
                            g.DrawString($"일자: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", fBody, brush, 20, 70);
                            g.DrawString("프린터 폰트 없이 한글 완벽 지원 (^GF)", fBody, brush, 20, 110);
                            g.DrawString("엑스프린터 / 지브라 호환 검증", fBody, brush, 20, 150);
                            g.DrawRectangle(Pens.Black, 15, 10, pw - 30, ll - 20);
                        }
                    }

                    string gfCommand = ZplBitmapHelper.ConvertBitmapToZplGF(bmp, 0, 0);
                    txtZpl.Text =
$"^XA\r\n" +
$"^PW{pw}\r\n" +
$"^LL{ll}\r\n" +
$"{gfCommand}\r\n" +
$"^XZ\r\n";
                }
                Log("샘플 3 로드: 한글 텍스트를 비트맵(^GF)으로 렌더링한 ZPL");
            }
            catch (Exception ex)
            {
                Log($"샘플 3 생성 오류: {ex.Message}");
            }
        }

        private void LoadSample4()
        {
            int pw = GetWidthDots();
            int ll = GetHeightDots();
            txtZpl.Text =
$"^XA\r\n" +
$"^PW{pw}\r\n" +
$"^LL{ll}\r\n" +
$"^FO20,20^GB{pw - 40},{ll - 40},3,B,0^FS\r\n" +
$"^FO40,40^A0N,30,30^FDGraphic Box Test^FS\r\n" +
$"^FO40,90^GB{pw - 80},2,2,B,0^FS\r\n" +
$"^FO40,110^A0N,24,24^FDStatus: PASS^FS\r\n" +
$"^XZ\r\n";
            Log("샘플 4 로드: 라인 및 테두리 박스(^GB)");
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (cboPrinters.SelectedItem == null)
            {
                MessageBox.Show("프린터를 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string printerName = cboPrinters.SelectedItem.ToString();
            string zplData = txtZpl.Text.Trim();

            if (string.IsNullOrEmpty(zplData))
            {
                MessageBox.Show("전송할 ZPL 명령어가 없습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Log($"[{printerName}] 프린터로 ZPL 전송 시작...");
                byte[] bytes = Encoding.UTF8.GetBytes(zplData);

                bool success = RawPrinterHelper.SendBytesToPrinter(printerName, bytes);
                if (success)
                {
                    Log($"✅ 전송 성공! (전송 크기: {bytes.Length} 바이트)");
                    Log("👉 프린터가 정상 반응하여 라벨이 인쇄되었는지 확인하세요.");
                }
                else
                {
                    Log("❌ 전송 실패: Windows Spooler에 전송하지 못했습니다. 프린터 연결 및 전원을 확인하세요.");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ 전송 중 예외 발생: {ex.Message}");
            }
        }

        private void Log(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            txtLog.AppendText(line + Environment.NewLine);
        }
    }
}
