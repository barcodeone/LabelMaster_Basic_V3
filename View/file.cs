using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization.Formatters.Binary;
using BarcodePrint.Model;
using System.IO;
using System.Drawing;


namespace BarcodePrint.View
{
    internal class file    {    }
    public partial class MainForm
    {

        private BinaryFormatter binaryFormatter = new BinaryFormatter();

        string paperPath = "info\\data.dat";
        string columPath = "info\\col.dat";


        /// <summary>
        /// ///////////////////////////////////////////////////////////////////////
        /// </summary> 컨트롤 값을 파일로 저장 
        private void SaveData()
        {
            // control value 복사
            UpdateBarcodeSettingsToPrintInfo();
            
            FileSave_data();
        }

        /// <summary>
        /// 바코드 설정을 PRINT_INFO_CLASS에 업데이트
        /// </summary>
        private void UpdateBarcodeSettingsToPrintInfo()
        {
            try
            {
                // MainForm의 바코드 설정을 PRINT_INFO_CLASS에 복사
                PRINT_INFO_CLASS.PrinterInfo.BarcodeSize = barcodeSize;
                PRINT_INFO_CLASS.PrinterInfo.BarcodeX = barcodeX;
                PRINT_INFO_CLASS.PrinterInfo.BarcodeY = barcodeY;
                PRINT_INFO_CLASS.PrinterInfo.ShowBarcodeText = showBarcodeText;
                
                System.Diagnostics.Debug.WriteLine($"바코드 설정 저장: Size={barcodeSize}, X={barcodeX}, Y={barcodeY}, ShowText={showBarcodeText}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"바코드 설정 저장 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// PRINT_INFO_CLASS에서 바코드 설정을 불러와서 MainForm 변수에 설정
        /// </summary>
        private void LoadBarcodeSettingsFromPrintInfo()
        {
            try
            {
                // PRINT_INFO_CLASS에서 바코드 설정을 MainForm 변수에 복사
                barcodeSize = PRINT_INFO_CLASS.PrinterInfo.BarcodeSize;
                barcodeX = PRINT_INFO_CLASS.PrinterInfo.BarcodeX;
                barcodeY = PRINT_INFO_CLASS.PrinterInfo.BarcodeY;
                showBarcodeText = PRINT_INFO_CLASS.PrinterInfo.ShowBarcodeText;
                currentBarcodeType = PRINT_INFO_CLASS.PrinterInfo.BarcodeType;
            }
            catch (Exception ex)
            {
            }
        }

     
        private void FileSave_data()
        {
            // 데이터를 파일로 저장
            PRINT_INFO_CLASS.SaveToFile(paperPath);

            COLUM_DATA.SaveToFile(columPath);
            Console.WriteLine("데이터가 파일에 저장되었습니다.");
        }

        /////////////////////////////////////////////////////////////////////////
        // 파일 읽어오기
        private void FileLoad_Excel_data()
        {            
            System.Diagnostics.Debug.WriteLine($"데이터 파일 로딩 시작 - paperPath: {paperPath}");
            
            // 파일 존재 여부 확인
            if (File.Exists(paperPath))
            {
                System.Diagnostics.Debug.WriteLine($"용지 설정 파일 존재: {paperPath}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"용지 설정 파일 없음: {paperPath}");
            }
            
            // 파일에서 데이터를 읽어오기
            if (COLUM_DATA.LoadFromFile(columPath)) { 
                System.Diagnostics.Debug.WriteLine("컬럼 데이터 로드 성공");
            } else {
                System.Diagnostics.Debug.WriteLine("컬럼 데이터 로드 실패 - 초기화");
                Init_ColumData();
            }

            // 용지 정보 로드 전 상태 확인
            System.Diagnostics.Debug.WriteLine($"용지 정보 로드 전 - Width: {PRINT_INFO_CLASS.PrinterInfo.Paper_Width}, Height: {PRINT_INFO_CLASS.PrinterInfo.Paper_Height}");
            
            PRINT_INFO_CLASS.LoadFromFile(paperPath);
            
            // 용지 정보 로드 후 상태 확인
            System.Diagnostics.Debug.WriteLine($"용지 정보 로드 후 - Width: {PRINT_INFO_CLASS.PrinterInfo.Paper_Width}, Height: {PRINT_INFO_CLASS.PrinterInfo.Paper_Height}");
            
            // 바코드 설정 불러오기
            LoadBarcodeSettingsFromPrintInfo();
            
            // 용지 사이즈가 로드되지 않은 경우에만 기본값 설정
            if (PRINT_INFO_CLASS.PrinterInfo.Paper_Width == 0 || PRINT_INFO_CLASS.PrinterInfo.Paper_Height == 0)
            {
                PRINT_INFO_CLASS.PrinterInfo.Paper_Width = 100; // 기본 라벨 너비 100mm
                PRINT_INFO_CLASS.PrinterInfo.Paper_Height = 40; // 기본 라벨 높이 40mm
                System.Diagnostics.Debug.WriteLine($"라벨 크기 기본값 설정: {PRINT_INFO_CLASS.PrinterInfo.Paper_Width}x{PRINT_INFO_CLASS.PrinterInfo.Paper_Height}mm");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"라벨 크기 로드됨: {PRINT_INFO_CLASS.PrinterInfo.Paper_Width}x{PRINT_INFO_CLASS.PrinterInfo.Paper_Height}mm");
            }
        }

        /// <summary>
        /// /////////////////////////////////////////////////////////////////////
        /// </summary> control -> class
        public void Set_PrintInfoClass() {

            
        }
                
    }
}
