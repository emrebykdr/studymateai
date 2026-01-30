using System;
using System.IO;

namespace StudyMateAI.Helpers
{
    public static class DocumentHelper
    {
        public static string ExtractTextFromFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();

            return extension switch
            {
                ".txt" => ExtractFromText(filePath),
                ".pdf" => ExtractFromPdf(filePath),
                ".docx" => ExtractFromDocx(filePath),
                _ => throw new NotSupportedException($"File type {extension} is not supported")
            };
        }

        private static string ExtractFromText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        private static string ExtractFromPdf(string filePath)
        {
            try
            {
                using (var document = UglyToad.PdfPig.PdfDocument.Open(filePath))
                {
                    var text = string.Empty;
                    int pageCount = document.NumberOfPages;

                    if (pageCount == 0)
                        return "[HATA] PDF dosyası boş veya okunamadı.";

                    foreach (var page in document.GetPages())
                    {
                        var pageText = page.Text;
                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            text += pageText + "\n";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return "[UYARI] PDF içeriğinden metin çıkarılamadı.\n\n" +
                               "Olası Nedenler:\n" +
                               "1. Döküman taratılmış (resim formatında) olabilir.\n" +
                               "2. Döküman şifreli veya korumalı olabilir.\n" +
                               "3. İçerik standart olmayan bir font kullanıyor olabilir.\n\n" +
                               "Bu sistem şu an için sadece metin tabanlı dijital PDF'leri desteklemektedir.";
                    }

                    return text;
                }
            }
            catch (Exception ex)
            {
                return $"[HATA] PDF okuma hatası: {ex.Message}\n\nLütfen dosyanın bozuk olmadığından emin olun.";
            }
        }

        private static string ExtractFromDocx(string filePath)
        {
            // For now, return a placeholder
            // In a real implementation, you would use DocumentFormat.OpenXml
            return $"[DOCX Content from {Path.GetFileName(filePath)}]\n\n" +
                   "DOCX içerik çıkarma için harici kütüphane gereklidir.\n" +
                   "Şimdilik sadece .txt dosyaları tam olarak desteklenmektedir.";
        }

        public static bool IsValidDocumentFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension == ".txt" || extension == ".pdf" || extension == ".docx";
        }

        public static long GetFileSizeInKB(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length / 1024;
        }
    }
}
