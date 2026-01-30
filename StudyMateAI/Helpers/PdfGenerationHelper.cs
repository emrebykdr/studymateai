using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace StudyMateAI.Helpers
{
    public static class PdfGenerationHelper
    {
        public static void GeneratePdf(string filePath, string title, string content)
        {
             Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Row(row => 
                        {
                            row.RelativeItem().Column(col => 
                            {
                                col.Item().Text(title).SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);
                                col.Item().Text($"Oluşturulma Tarihi: {System.DateTime.Now:g}").FontSize(10).FontColor(Colors.Grey.Medium);
                            });
                             
                            row.ConstantItem(100).AlignRight().Text("StudyMate AI").FontSize(10).FontColor(Colors.Grey.Medium);
                        });

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            // Process content to handle simple formatting like **Bold** or headers if possible
                            // For now, simpler text approach but splitting by lines for paragraphs
                            
                            var lines = content.Split(new[] { '\n' }, System.StringSplitOptions.None);
                            foreach(var line in lines)
                            {
                                var text = line.Trim();
                                if (string.IsNullOrWhiteSpace(text))
                                {
                                    column.Item().PaddingBottom(5);
                                    continue;
                                }

                                if (text.StartsWith("###"))
                                {
                                     column.Item().PaddingTop(10).PaddingBottom(5).Text(text.Replace("###", "").Trim()).Bold().FontSize(14);
                                }
                                else if (text.StartsWith("##"))
                                {
                                     column.Item().PaddingTop(10).PaddingBottom(5).Text(text.Replace("##", "").Trim()).Bold().FontSize(16);
                                }
                                else if (text.StartsWith("#"))
                                {
                                     column.Item().PaddingTop(15).PaddingBottom(5).Text(text.Replace("#", "").Trim()).Bold().FontSize(18).FontColor(Colors.Blue.Darken1);
                                }
                                else if (text.StartsWith("- ") || text.StartsWith("* "))
                                {
                                    column.Item().PaddingLeft(10).Text($"• {text.Substring(2)}");
                                }
                                else
                                {
                                    // Bolding check (simple)
                                    column.Item().Text(text);
                                }
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Sayfa ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf(filePath);
        }
    }
}
