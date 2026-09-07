using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;
using ClosedXML.Graphics;

namespace BarcodePrint.Model.PDFReader
{
    internal class BarcodeExtractor
    {
        // 바코드 생성 메서드 (기본 버전)
        public Bitmap GenerateBarcode(string barcodeText, int originalWidth, int originalHeight, float targetDpi, string barcodeType)
        {
            return GenerateBarcode(barcodeText, originalWidth, originalHeight, targetDpi, barcodeType, 50);
        }

        // 바코드 생성 메서드 (크기 조절 포함)
        public Bitmap GenerateBarcode(string barcodeText, int originalWidth, int originalHeight, float targetDpi, string barcodeType, int barcodeSize)
        {
            // 디버그 메시지
            System.Diagnostics.Debug.WriteLine($"BarcodeExtractor.GenerateBarcode - 전달받은 바코드 타입: {barcodeType}");
            
            // 현재 화면의 Graphics 객체를 사용하여 DPI 값을 가져옴
            float dpiX;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                dpiX = g.DpiX;
            }

            float dpiRatio = targetDpi / dpiX;  // DPI 비율 계산
            int width = (int)(originalWidth * dpiRatio);
            int height = (int)(originalHeight * dpiRatio);

            // 바코드 생성
            var barcodeWriter = new BarcodeWriter
            {
                Format = GetBarcodeFormat(barcodeType),
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 10,
                    PureBarcode = false  // 바코드만 생성 (텍스트 제외)
                }
            };
            
            // 디버그 메시지
            System.Diagnostics.Debug.WriteLine($"BarcodeExtractor.GenerateBarcode - 실제 사용할 바코드 포맷: {GetBarcodeFormat(barcodeType)}");

            // 바코드 생성 전 텍스트 처리 (한글 문자 처리)
            string processedText = ProcessBarcodeText(barcodeText, barcodeType);
            
            Bitmap barcodeBitmap = barcodeWriter.Write(processedText);

            // 바코드만 반환 (텍스트는 GetPrintState.cs에서 처리)
            return barcodeBitmap;
        }

        // 참고 프로젝트의 GenerateBarcode 메서드 (단순 버전)
        public Bitmap GenerateBarcode(string barcodeText, int originalWidth, int originalHeight, float targetDpi)
        {
            // 원하는 고해상도 DPI를 설정

            // 현재 화면의 Graphics 객체를 사용하여 DPI 값을 가져옵니다.
            float dpiX = 0;
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                dpiX = g.DpiX;  // 수평 DPI (horizontal DPI)
                //dpiY = g.DpiY;  // 수직 DPI (vertical DPI)
            }

            float dpiRatio = targetDpi / dpiX;  // 모니터의 해상도를 고해상도로 변경하기 위한 비율

            // 바코드 이미지 크기 계산 (dpiRatio를 적용하여 원래 크기를 고해상도 크기로 변환)
            int width = (int)(originalWidth * dpiRatio);
            int height = (int)(originalHeight * dpiRatio);

            // BarcodeWriter 객체 생성
            var barcodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,  // 생성할 바코드 형식 (CODE128)
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = width,   // 바코드 이미지의 너비
                    Height = height, // 바코드 이미지의 높이
                    Margin = 10,      // 바코드 주변 여백 (기본값은 10)
                    PureBarcode = true // 바코드의 하단 텍스트를 렌더링 하지 않도록 설정
                },               
            };

            // 바코드 이미지 생성
            Bitmap barcodeBitmap = barcodeWriter.Write(barcodeText);

            return barcodeBitmap;
        }

        /// <summary>
        /// 바코드 생성 전 텍스트 처리 (한글 문자 처리)
        /// </summary>
        private string ProcessBarcodeText(string data, string barcodeType)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                    return data;

                // CODE_128의 경우 영문자와 숫자만 지원
                if (barcodeType == "CODE_128")
                {
                    // 한글 문자를 제거하고 영문자, 숫자, 일부 특수문자만 유지
                    var allowedChars = new List<char>();
                    foreach (char c in data)
                    {
                        // 영문자, 숫자, 하이픈, 언더스코어만 허용 (공백 제외)
                        if ((c >= 'A' && c <= 'Z') || 
                            (c >= 'a' && c <= 'z') || 
                            (c >= '0' && c <= '9') || 
                            c == '-' || c == '_')
                        {
                            allowedChars.Add(c);
                        }
                    }
                    
                    string processedData = new string(allowedChars.ToArray()).Trim();
                    
                    // 빈 문자열이거나 공백만 있으면 기본값 사용
                    if (string.IsNullOrEmpty(processedData))
                    {
                        processedData = "BARCODE";
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"BarcodeExtractor - 바코드 텍스트 처리: '{data}' -> '{processedData}'");
                    return processedData;
                }
                
                // QR_CODE의 경우 모든 문자 지원
                if (barcodeType == "QR_CODE")
                {
                    return data; // QR 코드는 한글도 지원
                }
                
                // 기타 바코드 타입의 경우 기본 처리
                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BarcodeExtractor - 바코드 텍스트 처리 오류: {ex.Message}");
                return "BARCODE"; // 오류 시 기본값 반환
            }
        }

        // 바코드 타입을 ZXing BarcodeFormat으로 변환
        private BarcodeFormat GetBarcodeFormat(string barcodeType)
        {
            System.Diagnostics.Debug.WriteLine($"GetBarcodeFormat - 입력받은 바코드 타입: {barcodeType}");
            
            BarcodeFormat result;
            switch (barcodeType)
            {
                case "QR_CODE":
                    result = BarcodeFormat.QR_CODE;
                    break;
                case "CODE_128":
                    result = BarcodeFormat.CODE_128;
                    break;
                case "CODE_39":
                    result = BarcodeFormat.CODE_39;
                    break;
                case "EAN_13":
                    result = BarcodeFormat.EAN_13;
                    break;
                case "EAN_8":
                    result = BarcodeFormat.EAN_8;
                    break;
                case "DATA_MATRIX":
                    result = BarcodeFormat.DATA_MATRIX;
                    break;
                case "PDF_417":
                    result = BarcodeFormat.PDF_417;
                    break;
                default:
                    result = BarcodeFormat.QR_CODE; // 기본값
                    break;
            }
            
            System.Diagnostics.Debug.WriteLine($"GetBarcodeFormat - 반환할 바코드 포맷: {result}");
            return result;
        }
    }
}