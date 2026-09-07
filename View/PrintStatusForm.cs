using MaterialSkin.Controls;
using MetroFramework.Controls;
using MetroFramework.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using MetroFramework.Forms;

namespace BarcodePrint.View
{
    public partial class PrintStatusForm : Form
    {
        private Label lblStatus;
        private ProgressBar progressBar;
        public PrintStatusForm()
        {
            this.Text = "인쇄 준비중 (0%)";
            this.Size = new Size(300, 140);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None; // 둥근 모서리를 위한 설정
            this.BackColor = Color.WhiteSmoke;
            this.Padding = new Padding(10);
            this.TopMost = true;

            // 진행 상태 라벨
            lblStatus = new Label
            {
                Text = "0% 완료",
                Dock = DockStyle.Top,
                Height = 40,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 진행 바
            progressBar = new MetroProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                UseCustomBackColor = true,
                UseCustomForeColor = true,
                BackColor = Color.LightGray,
                ForeColor = Color.DodgerBlue
            };

            // 닫기 버튼
            Button closeButton = new Button
            {
                Text = "×",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.DimGray,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(30, 30),
                Location = new Point(this.Width - 40, 5)
            };
            closeButton.Click += (s, e) => this.Close();

            // 둥근 모서리 적용 (폼 페인팅)
            this.Paint += (s, e) => e.Graphics.DrawRectangle(new Pen(Color.Gray, 2), 0, 0, this.Width - 1, this.Height - 1);

            // 컨트롤 추가
            this.Controls.Add(lblStatus);
            this.Controls.Add(progressBar);
            this.Controls.Add(closeButton);
        }


        public void UpdateProgress(int percentage)
        {
            // 폼이 해제되었거나 닫혔는지 확인
            if (this.IsDisposed || this.Disposing)
                return;

            if (this.InvokeRequired)
            {
                try
                {
                    // 폼이 여전히 유효한지 다시 한번 확인
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        this.Invoke(new Action<int>(UpdateProgress), percentage);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // 폼이 해제된 경우 무시
                    return;
                }
                catch (InvalidOperationException)
                {
                    // 핸들이 생성되지 않은 경우 무시
                    return;
                }
            }
            else
            {
                try
                {
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        lblStatus.Text = $"인쇄준비중 ({percentage}%) ";
                        progressBar.Value = percentage;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // 컨트롤이 해제된 경우 무시
                    return;
                }
            }
        }

        public void UpdateProgressText(string text, int percentage)
        {
            if (this.IsDisposed || this.Disposing)
                return;

            if (this.InvokeRequired)
            {
                try
                {
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        this.Invoke(new Action<string, int>(UpdateProgressText), text, percentage);
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        lblStatus.Text = text;
                        progressBar.Value = percentage;
                    }
                }
                catch { }
            }
        }

        public void UpdateStatus(int sentCount, int actualCount, int totalCount)
        {
            if (this.IsDisposed || this.Disposing)
                return;

            if (this.InvokeRequired)
            {
                try
                {
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        this.Invoke(new Action<int, int, int>(UpdateStatus), sentCount, actualCount, totalCount);
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        lblStatus.Text = $"전송 완료: {sentCount} / {totalCount} 페이지\n실제 출력: {actualCount} / {totalCount} 페이지";
                        int percentage = totalCount > 0 ? (actualCount * 100) / totalCount : 0;
                        if (percentage < 0) percentage = 0;
                        if (percentage > 100) percentage = 100;
                        progressBar.Value = percentage;
                    }
                }
                catch { }
            }
        }

        public void ShowErrorStatus(int errorPage)
        {
            if (this.IsDisposed || this.Disposing)
                return;

            if (this.InvokeRequired)
            {
                try
                {
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        this.Invoke(new Action<int>(ShowErrorStatus), errorPage);
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        lblStatus.Text = $"오류: {errorPage}번째 인쇄 도중 멈춤";
                        lblStatus.ForeColor = Color.Red;
                        progressBar.Value = 100;
                        progressBar.ForeColor = Color.Red;
                    }
                }
                catch { }
            }
        }

        public void ShowCompleteStatus()
        {
            if (this.IsDisposed || this.Disposing)
                return;

            if (this.InvokeRequired)
            {
                try
                {
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        this.Invoke(new Action(ShowCompleteStatus));
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    if (!this.IsDisposed && !this.Disposing)
                    {
                        lblStatus.Text = "인쇄가 완료되었습니다.";
                        lblStatus.ForeColor = Color.DarkGreen;
                        progressBar.Value = 100;
                        progressBar.ForeColor = Color.DarkGreen;
                    }
                }
                catch { }
            }
        }
    }
}
