using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace ZplTester
{
    public static class ZplBitmapHelper
    {
        /// <summary>
        /// Bitmap 이미지를 ZPL의 ^GF (Graphic Field) 커맨드로 변환
        /// </summary>
        public static string ConvertBitmapToZplGF(Bitmap bmp, int posX = 0, int posY = 0)
        {
            if (bmp == null) return string.Empty;

            int width = bmp.Width;
            int height = bmp.Height;
            int widthBytes = (width + 7) / 8;
            int totalBytes = widthBytes * height;

            StringBuilder hexSb = new StringBuilder();

            using (Bitmap cloned = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(cloned))
                {
                    g.Clear(Color.White);
                    g.DrawImage(bmp, 0, 0, width, height);
                }

                BitmapData bmpData = cloned.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    int stride = bmpData.Stride;
                    int bytesPerPixel = 4;
                    byte[] pixelBuffer = new byte[stride * height];
                    System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixelBuffer, 0, pixelBuffer.Length);

                    for (int y = 0; y < height; y++)
                    {
                        for (int byteIdx = 0; byteIdx < widthBytes; byteIdx++)
                        {
                            byte currentByte = 0;
                            for (int bit = 0; bit < 8; bit++)
                            {
                                int x = (byteIdx * 8) + bit;
                                if (x < width)
                                {
                                    int pixelIndex = (y * stride) + (x * bytesPerPixel);
                                    byte b = pixelBuffer[pixelIndex];
                                    byte gVal = pixelBuffer[pixelIndex + 1];
                                    byte r = pixelBuffer[pixelIndex + 2];

                                    // 그레이스케일 계산 (0.299R + 0.587G + 0.114B)
                                    int luminance = (int)(r * 0.299 + gVal * 0.587 + b * 0.114);
                                    
                                    // 128보다 어두우면 검은색 픽셀(ZPL에서는 1 = Black)
                                    if (luminance < 128)
                                    {
                                        currentByte |= (byte)(0x80 >> bit);
                                    }
                                }
                            }
                            hexSb.Append(currentByte.ToString("X2"));
                        }
                    }
                }
                finally
                {
                    cloned.UnlockBits(bmpData);
                }
            }

            // ^FO{posX},{posY}^GFA,{totalBytes},{totalBytes},{widthBytes},{hexData}^FS
            return $"^FO{posX},{posY}^GFA,{totalBytes},{totalBytes},{widthBytes},{hexSb.ToString()}^FS";
        }
    }
}
