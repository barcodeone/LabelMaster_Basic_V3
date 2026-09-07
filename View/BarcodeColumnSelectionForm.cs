using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace BarcodePrint.View
{
    public partial class BarcodeColumnSelectionForm : Form
    {
        private IXLWorksheet worksheet;
        private ComboBox comboBoxColumns;
        private Button btnOK;
        private Button btnCancel;
        private Label labelInfo;
        
        public int SelectedColumn { get; private set; } = -1;
        
        public BarcodeColumnSelectionForm(IXLWorksheet worksheet, Form parentForm = null)
        {
            this.worksheet = worksheet;
            InitializeComponent();
            
            // 부모 폼이 지정된 경우 자식 폼으로 설정
            if (parentForm != null)
            {
                this.Owner = parentForm;
                this.TopMost = true;
            }
            
            LoadColumns();
        }
        
        private void InitializeComponent()
        {
            this.comboBoxColumns = new ComboBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.labelInfo = new Label();
            
            // 폼 설정
            this.Text = "바코드 컬럼 선택";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false; // 작업 표시줄에 표시하지 않음
            
            // 라벨 설정
            this.labelInfo.Text = "바코드로 사용할 컬럼을 선택하세요:";
            this.labelInfo.Location = new Point(20, 20);
            this.labelInfo.Size = new Size(350, 25);
            this.labelInfo.Font = new Font("맑은 고딕", 10F);
            
            // 콤보박스 설정
            this.comboBoxColumns.Location = new Point(20, 50);
            this.comboBoxColumns.Size = new Size(350, 25);
            this.comboBoxColumns.Font = new Font("맑은 고딕", 10F);
            this.comboBoxColumns.DropDownStyle = ComboBoxStyle.DropDownList;
            
            // 확인 버튼 설정
            this.btnOK.Text = "확인";
            this.btnOK.Location = new Point(200, 120);
            this.btnOK.Size = new Size(80, 30);
            this.btnOK.Font = new Font("맑은 고딕", 9F);
            this.btnOK.Click += BtnOK_Click;
            
            // 취소 버튼 설정
            this.btnCancel.Text = "취소";
            this.btnCancel.Location = new Point(290, 120);
            this.btnCancel.Size = new Size(80, 30);
            this.btnCancel.Font = new Font("맑은 고딕", 9F);
            this.btnCancel.Click += BtnCancel_Click;
            
            // 컨트롤 추가
            this.Controls.Add(this.labelInfo);
            this.Controls.Add(this.comboBoxColumns);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            
            // 기본 버튼 설정
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
        }
        
        private void LoadColumns()
        {
            try
            {
                var range = worksheet.RangeUsed();
                if (range != null)
                {
                    int columnCount = range.ColumnCount();
                    
                    for (int col = 1; col <= columnCount; col++)
                    {
                        var headerCell = worksheet.Cell(1, col);
                        string headerText = headerCell.IsEmpty() ? $"컬럼 {col}" : headerCell.GetString();
                        
                        // 컬럼 정보를 콤보박스에 추가 (인덱스와 헤더 텍스트)
                        comboBoxColumns.Items.Add($"{col}: {headerText}");
                    }
                    
                    // 첫 번째 컬럼을 기본 선택
                    if (comboBoxColumns.Items.Count > 0)
                    {
                        comboBoxColumns.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"컬럼 정보를 불러오는 중 오류가 발생했습니다: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (comboBoxColumns.SelectedIndex >= 0)
            {
                // 선택된 컬럼의 인덱스 추출 (콤보박스 텍스트에서 숫자 부분만 추출)
                string selectedText = comboBoxColumns.SelectedItem.ToString();
                if (int.TryParse(selectedText.Split(':')[0], out int columnIndex))
                {
                    SelectedColumn = columnIndex - 1; // 0부터 시작하는 인덱스로 변환
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            else
            {
                MessageBox.Show("컬럼을 선택해주세요.", "알림", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
