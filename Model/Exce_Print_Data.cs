using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Data;
using System.Printing;


namespace BarcodePrint.Model
{

    public class Exce_Print_Data
    {
        private const int DEFAULT_COPY_COUNT = 1;

        public static Queue<Row_Set> Excel_Queue { get; private set; } = new Queue<Row_Set>();
        
        public void SetRowList(DataTable dt)
        {
            try
            {
                //유효성 검사
                ValidateDataTable(dt);
                // 초기화
                InitializeRowSet();
                // 데이터 처리
                ProcessRows(dt);
            }
            catch (Exception ex)
            {
            }            
        }

        private void ValidateDataTable(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
                throw new ArgumentException("유효하지 않은 데이터입니다.");
        }

        private void InitializeRowSet()
        {           
            Excel_Queue?.Clear();
            Excel_Queue = new Queue<Row_Set>();
            
            //COLUM_DATA.Column_Sets = new List<Column_Set>();            
        }

        private void ProcessRows(DataTable dt)
        {
            foreach (DataRow row in dt.Rows)
            {                
                var rowData = new Row_Set
                {
                    Cell_string = new List<string>(),
                    CopyCount = DEFAULT_COPY_COUNT
                };
                
                ProcessColumns(dt, row, rowData);
                Excel_Queue.Enqueue(rowData);
            }
        }

        private void ProcessColumns(DataTable dt, DataRow row, Row_Set rowData)
        {
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                if (dt.Columns[i].ColumnName.Equals("Copy", StringComparison.OrdinalIgnoreCase))
                {// copy 컬럼만 복사
                    ProcessCopyColumn(row[i], rowData);
                }
                else
                {// 그외 데이터 복사
                    string cellValue = row[i]?.ToString() ?? string.Empty;
                    
                    // 가격 컬럼인 경우 자동 포맷 적용
                    if (PriceFormatHelper.IsPriceColumn(dt.Columns[i].ColumnName))
                    {
                        cellValue = PriceFormatHelper.FormatPrice(cellValue);
                    }
                    
                    rowData.Cell_string.Add(cellValue);
                }
            }
        }

        private void ProcessCopyColumn(object value, Row_Set rowData)
        {
            if (int.TryParse(value?.ToString(), out int copyValue) && copyValue > 0)
            {
                rowData.CopyCount = copyValue;
            }
        }               
    }
}