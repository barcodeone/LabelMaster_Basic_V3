using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarcodePrint.View
{
    internal class BtnMouseEvent
    {
    }

    public partial class MainForm {
        

        /// ////////////////////////////////////////////////////////////////////////////////// <summary>
        ///  메인폼 이동 
        private Point mousePoint = Point.Empty;
        private bool isFormDragging = false;
        
        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            // 상단 헤더 영역(0~80픽셀)에서만 드래그 시작 허용
            if (e.Button == MouseButtons.Left && e.Y <= 80)
            {
                // 컨트롤 위가 아닌 경우에만 폼 이동 허용
                Control control = this.GetChildAtPoint(e.Location);
                if (control == null || control == panel1)
                {
                    isFormDragging = true;
                    mousePoint = new Point(e.X, e.Y);
                }
            }
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isFormDragging && e.Button == MouseButtons.Left && mousePoint != Point.Empty)
            {
                this.Location = new Point(this.Location.X + e.X - mousePoint.X, this.Location.Y + e.Y - mousePoint.Y);
            }
        }
        
        private void MainForm_MouseUp(object sender, MouseEventArgs e)
        {
            // 마우스 버튼을 놓았을 때 이동 상태 해제
            isFormDragging = false;
            mousePoint = Point.Empty;
        }


        private void label2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y <= 50)
            {
                mousePoint = new Point(e.X, e.Y);
            }
        }

        private void label2_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && mousePoint != Point.Empty)
            {
                this.Location = new Point(this.Location.X + e.X - mousePoint.X, this.Location.Y + e.Y - mousePoint.Y);
            }
        }

        private void label2_MouseUp(object sender, MouseEventArgs e)
        {
            // 마우스 버튼을 놓았을 때 이동 상태 해제
            mousePoint = Point.Empty;
        }





      
  

       
        /// //////////////////////////////////////////////////////////////////////////////
        /// 창닫기 버튼 클릭 이벤트      



        private void panel_PrintButton_MouseEnter(object sender, EventArgs e)
        {
            MouseEnter(sender, e);
        }

        private void panel_PrintButton_MouseLeave(object sender, EventArgs e)
        {
            MouseLeave(sender, e);
        }

        private void panel_PrintButton_MouseDown(object sender, MouseEventArgs e)
        {
            MousePush(sender, e);
        }

        private void panel_PrintButton_MouseUp(object sender, MouseEventArgs e)
        {
            MouseUpper(sender, e);            
        }

    }
}
