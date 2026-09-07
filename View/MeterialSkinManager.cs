using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin.Controls;
using MaterialSkin;
using System.Windows.Controls;
using Panel = System.Windows.Forms.Panel;
using Image = System.Drawing.Image;
using ComboBox = System.Windows.Forms.ComboBox;

namespace BarcodePrint.View
{
    public partial class MainForm : Form
    {
        private PictureBox pictureButton;
        private List<BTN_PICTURE> originalImage;
        private void ConfigureMaterialSkin()
        {
            // MaterialSkinManager 인스턴스 가져오기
            MaterialSkinManager skinManager = MaterialSkinManager.Instance;

            skinManager.Theme = MaterialSkinManager.Themes.LIGHT;

            // 폰트 유형 설정
            skinManager.getFontByType(MaterialSkinManager.fontType.Subtitle2);

            // 기본 색상 설정
            skinManager.ColorScheme = new MaterialSkin.ColorScheme(
                Primary.Blue500, // 기본 색상 (Primary)
                Primary.Blue700, // 강조 색상 (Accent)
                Primary.Blue300, // 버튼 색상 (Raised button)
                Accent.LightBlue200, // 액센트 색상
                TextShade.BLACK // 텍스트 색상                
            );
            // 폰트 스타일 설정 (기본 폰트: Subtitle2)
            // skinManager.Font = new Font("Arial", 12);            
        }


        private void MakeComboBoxRounded(ComboBox comboBox, int cornerRadius)
        {
            using (GraphicsPath path = GetRoundedPath(comboBox.ClientRectangle, cornerRadius))
            {
                comboBox.Region = new Region(path); // 콤보박스의 모서리를 둥글게 처리
            }
        }

        // 둥근 경로 생성 함수
        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.StartFigure();

            // 각 모서리마다 아크를 추가
            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90); // 좌상단
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90); // 우상단
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90); // 우하단
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90); // 좌하단

            path.CloseFigure(); // 경로 닫기
            return path;
        }

        public Image previewImage = null;
        private void InitPictureButton()
        {
            // 원본 이미지 저장
            originalImage = new List<BTN_PICTURE>();

            // 원본 이미지 저장 ("컨트롤 컬럼3" ,컨트롤컬럼3)
            // originalImage.Add(new BTN_PICTURE("pictureBtn_Closed", pictureBtn_Closed)); // 커스텀 버튼으로 교체됨
            //originalImage.Add(new BTN_PICTURE("pictureBtn_Home", pictureBtn_Home));
            this.Size = new Size(1024, 768);
        }


        // 이미지 음영 효과 적용 함수
        private Image ApplyShadowEffect(Image img, float rate = 0.8f)
        {
            Bitmap bmp = new Bitmap(img);

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    int r = (int)(pixel.R * rate); // 80% 밝기로 감소
                    int g = (int)(pixel.G * rate);
                    int b = (int)(pixel.B * rate);
                    bmp.SetPixel(x, y, Color.FromArgb(pixel.A, r, g, b));
                }
            }
            return bmp;
        }


        
        // Panel에 음영 효과 적용 함수
        private Color PanelApplyShadowEffect(Color color, float rate)
        {
            // RGB 값을 직접 조정하여 음영 효과 적용
            int r = (int)(color.R * rate);
            int g = (int)(color.G * rate);
            int b = (int)(color.B * rate);

            // 값이 0~255 사이로 유지되도록 조정
            r = Math.Max(0, Math.Min(255, r));
            g = Math.Max(0, Math.Min(255, g));
            b = Math.Max(0, Math.Min(255, b));

            return Color.FromArgb(color.A, r, g, b);
        }

        private new void MouseEnter(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox)
            {
                var btnData = originalImage.FirstOrDefault(x => x.name == pictureBox.Name);
                if (btnData != null)
                {
                    pictureBox.Image = ApplyShadowEffect(btnData.originalImage, 0.8f);
                }
            }
            else if(sender is Panel panel) {
                new_PrintButton(0.8f);
            }
        }

        private new void MouseLeave(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox)
            {
                var btnData = originalImage.FirstOrDefault(x => x.name == pictureBox.Name);
                if (btnData != null)
                {
                    // 원래 이미지로
                    pictureBox.Size = btnData.originSize;
                    pictureBox.Image = btnData.originalImage;
                }
            }
            else if (sender is Panel panel)
            {
                new_PrintButton(1f);
            }
        }

        private void MousePush(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox)
            {
                var btnData = originalImage.FirstOrDefault(x => x.name == pictureBox.Name);
                if (btnData != null)
                {
                    pictureBox.Image = ApplyShadowEffect(btnData.originalImage, 0.5f);
                }
            }
            else if (sender is Panel panel)
            {
                new_PrintButton(0.5f);
            }
        }

        private void MouseUpper(object sender, EventArgs e)
        {
            if (sender is PictureBox pictureBox)
            {
                var btnData = originalImage.FirstOrDefault(x => x.name == pictureBox.Name);
                if (btnData != null)
                {
                    pictureBox.Image = ApplyShadowEffect(btnData.originalImage, 1);
                }
            }
            else if (sender is Panel panel)
            {
                //panel.BackColor = PanelApplyShadowEffect(originPrintBtn.BackColor, 1f);

                new_PrintButton();
            }
        }


    }

    public class BTN_PICTURE
    {
        public string name; //컨트롤 컬럼3
        public Image originalImage; // 사진 컬럼3
        public Size originSize;

        public BTN_PICTURE(string name, PictureBox pictureBox)
        {
            this.name = name;
            this.originalImage = pictureBox.Image;
            this.originSize = pictureBox.Size;
        }
    }

}
