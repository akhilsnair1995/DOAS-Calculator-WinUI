using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DOASCalculatorWinUI
{
    public static class PdfReportGenerator
    {
        static PdfReportGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static void Generate(string filePath, MainViewModel viewModel, SystemResults results)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("DOAS DESIGN REPORT").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text($"Generated on {DateTime.Now:f}").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(100).Column(col =>
                        {
                            col.Item().Text("One Man Buzz").SemiBold().FontSize(12).AlignRight();
                            col.Item().Text("DOAS Calculator v3.0").FontSize(8).AlignRight();
                        });
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // 1. PROJECT INPUTS SECTION
                        col.Item().PaddingTop(10).Text("1. System Configuration").FontSize(14).SemiBold().Underline();
                        col.Item().PaddingVertical(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Text("Altitude:").SemiBold();
                            table.Cell().Text($"{viewModel.Altitude} {(viewModel.IsIp ? "ft" : "m")}");
                            table.Cell().Text("OA Flow:").SemiBold();
                            table.Cell().Text($"{viewModel.OaFlow} {(viewModel.IsIp ? "CFM" : "L/s")}");

                            table.Cell().Text("OA DB:").SemiBold();
                            table.Cell().Text($"{viewModel.OaDb} {(viewModel.IsIp ? "°F" : "°C")}");
                            table.Cell().Text("OA WB:").SemiBold();
                            table.Cell().Text($"{viewModel.OaWb} {(viewModel.IsIp ? "°F" : "°C")}");

                            table.Cell().Text("EA Flow:").SemiBold();
                            table.Cell().Text($"{viewModel.EaFlow} {(viewModel.IsIp ? "CFM" : "L/s")}");
                            table.Cell().Text("EA DB/RH:").SemiBold();
                            table.Cell().Text($"{viewModel.EaDb}{(viewModel.IsIp ? "°F" : "°C")} / {viewModel.EaRh}%");
                        });

                        // 2. DESIGN RESULTS HIGHLIGHTS
                        col.Item().PaddingTop(20).Text("2. Performance Summary").FontSize(14).SemiBold().Underline();
                        col.Item().PaddingVertical(10).Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Blue.Lighten4).Background(Colors.Blue.Lighten5).Padding(10).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Total Cooling").FontSize(10).SemiBold();
                                c.Item().AlignCenter().Text(viewModel.ResCooling).FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                                c.Item().AlignCenter().Text(viewModel.ResCoolingBreakdown).FontSize(8);
                            });

                            row.ConstantItem(10);

                            row.RelativeItem().Border(1).BorderColor(Colors.Orange.Lighten4).Background(Colors.Orange.Lighten5).Padding(10).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Reheat Load").FontSize(10).SemiBold();
                                c.Item().AlignCenter().Text(viewModel.ResReheat).FontSize(16).Bold().FontColor(Colors.Orange.Medium);
                            });

                            row.ConstantItem(10);

                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                            {
                                c.Item().AlignCenter().Text("Fan Power").FontSize(10).SemiBold();
                                c.Item().AlignCenter().Text(viewModel.ResFanPower).FontSize(14).Bold();
                                c.Item().AlignCenter().Text(viewModel.ResFanBreakdown).FontSize(7);
                            });
                        });

                        // 3. PSYCHROMETRIC SCHEDULE
                        col.Item().PaddingTop(20).Text("3. Psychrometric Process Schedule").FontSize(14).SemiBold().Underline();
                        col.Item().PaddingVertical(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Component / Step").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Entering State").SemiBold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Leaving State").SemiBold();
                            });

                            foreach (var item in viewModel.Schedule)
                            {
                                if (string.IsNullOrEmpty(item.Component)) continue;

                                var color = Colors.Black;
                                if (item.Color == "SteelBlue") color = Colors.Blue.Medium;
                                if (item.Color == "DarkOrange") color = Colors.Orange.Medium;
                                if (item.Color == "Gray") color = Colors.Grey.Medium;

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(t =>
                                {
                                    var span = t.Span(item.Component).FontColor(color);
                                    if (item.Color == "Gray") span.SemiBold();
                                });

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(item.Entering).FontColor(color).FontFamily(Fonts.Consolas);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(item.Leaving).FontColor(color).FontFamily(Fonts.Consolas);
                            }
                        });
                        
                        // Technical Notes
                        col.Item().AlignRight().PaddingTop(20).Text("Calculations based on standard psychrometric formulas and ASHRAE 90.1-2019 baseline efficiency models.").FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }
    }
}
