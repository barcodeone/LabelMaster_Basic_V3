using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Drawing.Point;
using Argon;

namespace BarcodePrint.Model
{
    [Serializable]
    public static class PRINT_INFO_CLASS {
        public static PaperClass PrinterInfo = new PaperClass();

        // 데이터를 파일로 저장
        public static void SaveToFile(string filePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[PRINT_INFO_CLASS.SaveToFile] 시작 (JSON)");
                System.Diagnostics.Debug.WriteLine($"  - 파일 경로: {filePath}");
                System.Diagnostics.Debug.WriteLine($"  - 저장할 BarcodeType: {PrinterInfo.BarcodeType}");
                System.Diagnostics.Debug.WriteLine($"  - 저장할 Paper_Width: {PrinterInfo.Paper_Width}mm");
                System.Diagnostics.Debug.WriteLine($"  - 저장할 Paper_Height: {PrinterInfo.Paper_Height}mm");
                
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    System.Diagnostics.Debug.WriteLine($"  - 디렉토리 생성: {directoryPath}");
                }

                string json = JsonConvert.SerializeObject(PrinterInfo, Formatting.Indented);
                File.WriteAllText(filePath, json, Encoding.UTF8);
                
                System.Diagnostics.Debug.WriteLine($"[PRINT_INFO_CLASS.SaveToFile] 완료");
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[PRINT_INFO_CLASS.SaveToFile] 오류 발생!");
                System.Diagnostics.Debug.WriteLine($"  - 오류 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
        }


        // 파일에서 데이터를 읽어와서 설정
        public static void LoadFromFile(string filePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[PRINT_INFO_CLASS.LoadFromFile] 시작");
                System.Diagnostics.Debug.WriteLine($"  - 파일 경로: {filePath}");
                
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"  - 파일 없음, 기본값 사용");
                    return;
                }
                
                string content = File.ReadAllText(filePath, Encoding.UTF8);
                
                // JSON 형식인지 체크 (구버전 바이너리 호환성)
                if (content.TrimStart().StartsWith("{"))
                {
                    var loadedData = JsonConvert.DeserializeObject<PaperClass>(content);
                    if (loadedData != null)
                    {
                        PrinterInfo = loadedData;
                        System.Diagnostics.Debug.WriteLine($"  - PrinterInfo 교체 완료 (JSON)");
                    }
                }
                else
                {
                    // 레거시 바이너리 포맷으로 로드 시도 (Fallback)
                    System.Diagnostics.Debug.WriteLine("[PRINT_INFO_CLASS] 레거시 바이너리 포맷 감지, Fallback 로드 시도");
                    using (FileStream fs = new FileStream(filePath, FileMode.Open))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        PrinterInfo = (PaperClass)formatter.Deserialize(fs);
                        System.Diagnostics.Debug.WriteLine($"  - 레거시 바이너리 로드 성공");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[PRINT_INFO_CLASS.LoadFromFile] 완료");
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"========================================");
                System.Diagnostics.Debug.WriteLine($"[PRINT_INFO_CLASS.LoadFromFile] 오류 발생!");
                System.Diagnostics.Debug.WriteLine($"  - 오류 메시지: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"========================================");
            }
        }
    }

    [Serializable]
    public class PaperClass : IDisposable    {

        public string TargetPrint = ""; //적용할 프린터모델
        public string printerName { get; set; } = "";//프린터
        public string printerPortName { get; set; } //프린터포트
        public string PrinterSerial { get; set; } = ""; //프린터 시리얼번호
        public float printer_DPI { get; set; } = 203f;//프린터해상도
        public int nPrintCopies { get; set; } = 1;//인쇄매수
        public int nPrintSpeed { get; set; } = 3;//인쇄속도
        public int nDarkness { get; set; } = 8; // 인쇄농도
        public int nGAP { get; set; } = 2; // labael 간격, 0 이면 연속
        public double BlineOffset { get; set; } = 0.0; // 블랙마크 이탈량 / 표시 오프셋 (mm)
        public bool bRibbon { get; set; } = false; //인쇄모드 (열전사 / 감열)
        public string PaperType { get; set; } = PAPER_TYPE.GAP;// 용지타입 (GAP)

        public bool bPaperRotated { get; set; } = false;//용지회전 (기본값: OFF)
        public int Paper_Width { get; set; } = 80;//가로폭 (기본값: 80mm)
        public int Paper_Height { get; set; } = 60;//세로폭 (기본값: 60mm)

        [field: NonSerialized]
        public string SelectedPaperType { get; set; } = "80x60(갭)"; // Paper Type 선택 (저장하지 않음)

        public int offset_X { get; set; } = 0;//가로위치
        public int offset_Y { get; set; } = 0;//세로위치

        // 중간에 위치하기위한 고정값 
        public int correction_X { get; set; } = 0;
        public int correction_Y { get; set; } = 0;


        // 모니터와 실제 출력시 위치 보정
        public int hidden_Offset_X { get; set; } = 0;//가로위치 (초기값: 0)
        public int hidden_Offset_Y { get; set; } = 0;//세로위치 (초기값: 0)

        // 바코드 타입 설정
        public string BarcodeType { get; set; } = "QR_CODE";//바코드 타입
        
        // 바코드 크기 및 위치 설정
        public int BarcodeSize { get; set; } = 50;//바코드 크기
        public int BarcodeX { get; set; } = 0;//바코드 X 위치
        public int BarcodeY { get; set; } = 0;//바코드 Y 위치
        public bool ShowBarcodeText { get; set; } = true;//바코드 텍스트 표시 여부
        public int BarcodeHeight { get; set; } = 15; //바코드 세로 크기 (기본값 15)

        public void Dispose() { }
    }

   
    /// /////////// /////////// /////////// /////////// /////////// /////////// ////////
    /// </summary> colum별 세팅 데이터
    [Serializable]
    public static class COLUM_DATA 
    {
        // static 데이터
        public static List<Column_Set> Column_Sets = new List<Column_Set>();

        // 데이터를 파일로 저장
        public static void SaveToFile(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, Column_Sets);
            }
        }

        // 파일에서 데이터를 읽어와서 Column_Sets에 설정
        public static bool LoadFromFile(string filePath)
        {
            try
            {
                // 기존 데이터를 클리어
                COLUM_DATA.Column_Sets.Clear();

                if (!File.Exists(filePath))
                    throw new FileNotFoundException("파일을 찾을 수 없습니다.", filePath);
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    Column_Sets = (List<Column_Set>)formatter.Deserialize(fs);                    
                }
                foreach(var column in Column_Sets)
                {
                    column.HeaderName = "";
                }
                

            }
            catch (Exception ex) {
                return false;
            }
            return true;
        }

        // static 데이터 확인용 메서드
        public static void PrintInfo()
        {
            foreach (var column in Column_Sets)
            {
                Console.WriteLine($"Font: {column.font.Name}, Alignment: {column.alignment}, Vertical: {column.text_vertical}");
            }
        }
    }

    [Serializable]
    public class Column_Set : IDisposable
    {
        public string HeaderName;
        public Font font = new Font("굴림",12);
        public StringAlignment alignment = StringAlignment.Near;
        public Point Location;
        public StringFormatFlags text_vertical;
        public string Fixed_Text = ""; //고정 텍스트 
        public bool Fixed_Text_Check = false; //고정 텍스트 사용여부
        public bool Fixed_Text_After = true; // true == 앞, false == 뒤

                    // EnabeBarcode 제거됨
        public string BarcodeType = "QR_CODE"; //바코드 타입
        public int Barcode_Size = 20; //바코드 크기
        public Point Barcode_Location; //위치

        public void Dispose() { }
    }

    public class Row_Set : IDisposable
    {
        public List<string> Cell_string { get; set; }
        public List<Bitmap> BarCode { get; set; }
        public int CopyCount { get; set; }
        public void Dispose() { }
    }

    public static class PAPER_TYPE {
        public static string GAP = "GAP";
        public static string BLINE = "BLINE";
        public static string CONTINEUS = "CONTINUES";        
    }  

}
