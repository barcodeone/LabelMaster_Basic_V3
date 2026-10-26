using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PigPdfDocument = UglyToad.PdfPig.PdfDocument;
using WinPdfDocument = Windows.Data.Pdf.PdfDocument;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BarcodePrint.Model
{
    /// <summary>
    /// PDF 문서의 페이지 수 확인 및 페이지별 고품질 이미지 래스터라이징/추출 헬퍼
    /// (PdfPig 임베디드 이미지 고속 추출 + Windows 10/11 내장 Windows.Data.Pdf 하이브리드 엔진)
    /// </summary>
    public static class PdfRenderHelper
    {
        /// <summary>
        /// PDF 문서의 총 페이지 수를 반환합니다.
        /// </summary>
        public static int GetPdfPageCount(string pdfPath)
        {
            if (string.IsNullOrEmpty(pdfPath) || !File.Exists(pdfPath)) return 0;

            try
            {
                using (var document = PigPdfDocument.Open(pdfPath))
                {
                    return document.NumberOfPages;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PdfRenderHelper] GetPdfPageCount Error: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// PDF의 특정 페이지(0-indexed)를 고해상도 Bitmap 이미지로 렌더링 또는 추출합니다.
        /// </summary>
        /// <param name="pdfPath">PDF 파일 경로</param>
        /// <param name="pageIndex">0부터 시작하는 페이지 번호</param>
        /// <param name="targetDpi">대상 해상도 DPI (기본 203 DPI)</param>
        /// <returns>System.Drawing.Bitmap 객체</returns>
        public static Bitmap RenderPageToBitmap(string pdfPath, int pageIndex, int targetDpi = 203)
        {
            if (string.IsNullOrEmpty(pdfPath) || !File.Exists(pdfPath)) return null;

            // 1단계: 택배 송장 등 임베디드 이미지가 포함된 PDF인 경우 PdfPig로 무손실 고속 추출 시도
            try
            {
                using (var document = PigPdfDocument.Open(pdfPath))
                {
                    if (pageIndex >= 0 && pageIndex < document.NumberOfPages)
                    {
                        var page = document.GetPage(pageIndex + 1); // PdfPig는 1-indexed
                        var images = page.GetImages().ToList();

                        if (images.Count > 0)
                        {
                            // 가장 큰 이미지(메인 송장 비트맵) 선택
                            var mainImg = images.OrderByDescending(img => img.WidthInSamples * img.HeightInSamples).FirstOrDefault();
                            if (mainImg != null)
                            {
                                if (mainImg.TryGetPng(out byte[] pngBytes) && pngBytes != null && pngBytes.Length > 0)
                                {
                                    using (var ms = new MemoryStream(pngBytes))
                                    {
                                        return new Bitmap(ms);
                                    }
                                }
                                else if (mainImg.RawBytes.Length > 0)
                                {
                                    try
                                    {
                                        using (var ms = new MemoryStream(mainImg.RawBytes.ToArray()))
                                        {
                                            return new Bitmap(ms);
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PdfRenderHelper] PdfPig Extract Warning: {ex.Message}");
            }

            // 2단계: 임베디드 이미지가 없거나 벡터/텍스트 PDF인 경우 Windows 내장 Windows.Data.Pdf 래스터라이징 엔진 호출
            try
            {
                var winBmp = RenderWithWindowsNative(pdfPath, pageIndex, targetDpi);
                if (winBmp != null) return winBmp;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PdfRenderHelper] Windows Native Render Warning: {ex.Message}");
            }

            // 3단계: 최후의 Fallback (빈 안내 비트맵 생성)
            return CreateFallbackBitmap(pdfPath, pageIndex);
        }

        /// <summary>
        /// Windows 10/11 시스템 내장 WinRT Windows.Data.Pdf API를 C# 인프로세스에서 직접 호출하여 고품질 벡터 래스터화 수행
        /// (외부 프로세스 powershell.exe 호출을 완전히 배제하여 안랩 ASTx 등 백신의 Fileless 탐지 및 차단 위험 100% 없음)
        /// </summary>
        private static Bitmap RenderWithWindowsNative(string pdfPath, int pageIndex, int targetDpi)
        {
            try
            {
                return Task.Run(async () =>
                {
                    string fullPath = Path.GetFullPath(pdfPath);
                    var file = await StorageFile.GetFileFromPathAsync(fullPath);
                    var doc = await WinPdfDocument.LoadFromFileAsync(file);

                    if (pageIndex < doc.PageCount)
                    {
                        using (var page = doc.GetPage((uint)pageIndex))
                        {
                            var memStream = new InMemoryRandomAccessStream();
                            var renderOptions = new PdfPageRenderOptions();
                            renderOptions.DestinationWidth = (uint)(page.Size.Width * (targetDpi / 72.0));

                            await page.RenderToStreamAsync(memStream, renderOptions);

                            using (var netStream = memStream.AsStream())
                            {
                                return new Bitmap(netStream);
                            }
                        }
                    }
                    return null;
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PdfRenderHelper] WinRT In-Process Render Error: {ex.Message}");
                return null;
            }
        }

        private static Bitmap CreateFallbackBitmap(string pdfPath, int pageIndex)
        {
            var bmp = new Bitmap(800, 1000);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                using (var pen = new Pen(Color.LightGray, 2))
                {
                    g.DrawRectangle(pen, 10, 10, 780, 980);
                }
                using (var brush = new SolidBrush(Color.DarkSlateGray))
                using (var font = new Font("맑은 고딕", 16, FontStyle.Bold))
                using (var subFont = new Font("맑은 고딕", 12))
                {
                    string fileName = Path.GetFileName(pdfPath);
                    g.DrawString("📄 PDF 문서 페이지", font, brush, 30, 40);
                    g.DrawString($"파일: {fileName}", subFont, brush, 30, 80);
                    g.DrawString($"페이지 번호: {pageIndex + 1}", subFont, brush, 30, 110);
                }
            }
            return bmp;
        }

        /// <summary>
        /// PDF 페이지를 임시 PNG 파일로 저장하고 해당 경로를 반환합니다.
        /// </summary>
        public static string SavePageToTempPng(string pdfPath, int pageIndex, int targetDpi = 203)
        {
            var bmp = RenderPageToBitmap(pdfPath, pageIndex, targetDpi);
            if (bmp == null) return null;

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), $"lbm_pdf_import_{Guid.NewGuid():N}.png");
                bmp.Save(tempPath, ImageFormat.Png);
                bmp.Dispose();
                return tempPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PdfRenderHelper] SavePageToTempPng Error: {ex.Message}");
                bmp.Dispose();
                return null;
            }
        }
    }
}
