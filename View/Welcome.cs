using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BarcodePrint.View
{
    public partial class Welcome : Form
    {
        private bool loginFlag;

        public Welcome()
        {
            InitializeComponent();
            this.CenterToScreen();

            loginFlag = false;
            checkBox_keySave.Checked = true;

            loadKey();

            button_login.Click += ReceiveButtonEvent;
        }

        private void ReceiveButtonEvent(object sender, EventArgs e)
        {
            if (sender.Equals(button_login))
            {
                this.loginFlag = login();

                if (this.loginFlag)
                {
                    saveKey();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("로그인에 실패하였습니다.");
                }
            }
        }

        private bool login()
        {
            string permission = "1234";
            string accessKey = textBox_AccessKey.Text;

            if (accessKey == permission)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool GetLoginFlag()
        {
            return this.loginFlag;
        }


        public void saveKey()
        {
            if (!checkBox_keySave.Checked)
            {
                //폴더 생성
                DirectoryInfo dia = new DirectoryInfo("Info");
                if (dia.Exists == false)
                {
                    dia.Create();
                }

                //파일 생성
                StreamWriter swr = new StreamWriter(new FileStream("Info\\login.dat", FileMode.Create, FileAccess.Write));

                swr.Write(";");
                swr.Close();

                return;
            }

            string access = textBox_AccessKey.Text;

            //폴더 생성
            DirectoryInfo di = new DirectoryInfo("Info");
            if (di.Exists == false)
            {
                di.Create();
            }

            //파일 생성
            StreamWriter sw = new StreamWriter(new FileStream("Info\\login.dat", FileMode.Create, FileAccess.Write));

            sw.Write(access + ";");
            sw.Close();
        }

        public bool loadKey()
        {
            try
            {
                string[] getValue = File.ReadAllLines("Info\\login.dat");
                string[] strTemp = getValue[0].Split(';');
                textBox_AccessKey.Text = strTemp[0];
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }

        }

        private void Welcome_FormClosing(object sender, FormClosingEventArgs e)
        {

            saveKey();

        }
    }
}
