using CsvHelper;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Proj_backend.Models_ML;
using Proj_backend.Services;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using SkiaSharp;
using iText.IO.Image;
using System.IO.Packaging;
using System.IO.Compression;

namespace Proj_backend.Controllers
{
    [Route("api/combined")]
    [ApiController]
    public class CombinedController : Controller
    {
        private const float THRESHOLD = 0.5f;
        const string trainingDataPath = "././Data/Training_data.csv";
        [HttpPost("predict-combined")]
        public IActionResult PredictCombined(IFormFile inputFile)
        {
            if (inputFile == null || inputFile.Length == 0)
            {
                return BadRequest("No file was uploaded.");
            }

            try
            {
                List<CustomerData> customers = CustomerService.LoadDataFromCSV(inputFile);

                customers = new Controller_LogisticRegression().PredictHighCost(customers, trainingDataPath);
                customers = new Controller_LinearRegression().PredictCost(customers, trainingDataPath);

                var memoryStream = new MemoryStream();
                using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(customers);
                    writer.Flush();
                }

                memoryStream.Position = 0;
                return File(memoryStream, "text/csv", "combined.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("predict-with-report")]
        public IActionResult PredictWithReport(IFormFile inputFile)
        {
            if (inputFile == null || inputFile.Length == 0)
            {
                return BadRequest("No file was uploaded.");
            }

            try
            {
                List<CustomerData> customers = CustomerService.LoadDataFromCSV(inputFile);

                customers = new Controller_LogisticRegression().PredictHighCost(customers, trainingDataPath);
                customers = new Controller_LinearRegression().PredictCost(customers, trainingDataPath);

                var memoryStreamCsv = new MemoryStream();
                using (var writer = new StreamWriter(memoryStreamCsv, leaveOpen: true))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(customers);
                    writer.Flush();
                }
                memoryStreamCsv.Position = 0;
                

                MemoryStream memoryStreamPdf = new MemoryStream();

                var pdfWriter = new PdfWriter(memoryStreamPdf);
                pdfWriter.SetCloseStream(false);

                var pdfDocument = new PdfDocument(pdfWriter);
                var document = new iText.Layout.Document(pdfDocument);
                

                var font = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN);

                document.Add(new Paragraph("Classification Report")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(18));

                document.Add(new Paragraph($"Report generated on: {DateTime.Now}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12));

                document.Add(new Paragraph("\n"));

                document.Add(new Paragraph("The application performed the following steps:")
                    .SetFontSize(14));

                document.Add(new Paragraph("1. Applied Logistic Regression to classify customers as 'High Cost' (1) or 'Not High Cost' (0) via threshold."));
                document.Add(new Paragraph("Threshold Explanation:").SetFontSize(14));
                document.Add(new Paragraph($"- The threshold for classifying a customer as 'High Cost' is set at {THRESHOLD * 100:F2}%."));
                document.Add(new Paragraph($"- Customers with a probability above this threshold are classified as 'High Cost'."));

                document.Add(new Paragraph("2. Applied Linear Regression to predict the exact cost for each customer."));
                document.Add(new Paragraph($"3. Processed {customers.Count} customer records based on the uploaded input file."));

                document.Add(new Paragraph("\n"));

                int highCostCount = customers.Count(c => c.IsHighCost == 1);
                int lowCostCount = customers.Count - highCostCount;
                document.Add(new Paragraph($"Summary of Results:").SetFontSize(14));
                document.Add(new Paragraph($"- High Cost Customers: {highCostCount}"));
                document.Add(new Paragraph($"- Not High Cost Customers: {lowCostCount}"));
                document.Add(new Paragraph($"- Total Customers Processed: {customers.Count}"));

                byte[] barChartBytes = GenerateBarChart(highCostCount, lowCostCount);
                var barChartImage = new iText.Layout.Element.Image(ImageDataFactory.Create(barChartBytes)).ScaleToFit(500, 300);
                document.Add(new Paragraph("Bar Chart: High Cost vs Low Cost Customers").SetFontSize(14));
                document.Add(barChartImage);

                byte[] pieChartBytes = GeneratePieChart(highCostCount, lowCostCount);
                var pieChartImage = new iText.Layout.Element.Image(ImageDataFactory.Create(pieChartBytes)).ScaleToFit(500, 500);
                document.Add(new Paragraph("Pie Chart: Proportion of High Cost Customers").SetFontSize(14));
                document.Add(pieChartImage);

                byte[] lineChartBytes = GenerateLineChart(customers.Select(c => c.Cost).ToList());
                var lineChartImage = new iText.Layout.Element.Image(ImageDataFactory.Create(lineChartBytes)).ScaleToFit(600, 300);
                document.Add(new Paragraph("Line Chart: Predicted Costs").SetFontSize(14));
                document.Add(lineChartImage);

                var highestCostCustomer = customers.OrderByDescending(c => c.Cost).First();
                var lowestCostCustomer = customers.OrderBy(c => c.Cost).First();

                document.Add(new Paragraph("\n"));
                document.Add(new Paragraph("Top Insights:")
                    .SetFontSize(14));
                document.Add(new Paragraph($"- Customer with the highest predicted cost: {highestCostCustomer.FirstName} {highestCostCustomer.LastName}, Cost: {highestCostCustomer.Cost:C2}"));
                document.Add(new Paragraph($"- Customer with the lowest predicted cost: {lowestCostCustomer.FirstName} {highestCostCustomer.LastName}, Cost: {lowestCostCustomer.Cost:C2}"));

                document.Add(new Paragraph("\n"));

                document.Close();

                memoryStreamPdf.Position = 0;

                var zipMemoryStream = new MemoryStream();
                using (var zipArchive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var csvEntry = zipArchive.CreateEntry("result.csv");
                    using (var entryStream = csvEntry.Open())
                    {
                        memoryStreamCsv.CopyTo(entryStream);
                        memoryStreamCsv.Position = 0;
                    }

                    var pdfEntry = zipArchive.CreateEntry("report.pdf");
                    using (var entryStream = pdfEntry.Open())
                    {
                        memoryStreamPdf.CopyTo(entryStream);
                        memoryStreamPdf.Position = 0;
                    }
                }

                zipMemoryStream.Position = 0;

                return File(zipMemoryStream, "application/zip", "Result_and_report.zip");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private byte[] GenerateBarChart(int highCostCount, int lowCostCount)
        {
            int width = 2000, height = 2000;
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;

            canvas.Clear(SKColors.White);

            int barWidth = 400;
            int highCostBarHeight = (int)(height * (highCostCount / (float)(highCostCount + lowCostCount)));
            int lowCostBarHeight = height - highCostBarHeight;

            var paint = new SKPaint { Color = SKColors.Blue, IsAntialias = true };
            canvas.DrawRect(400, height - highCostBarHeight, barWidth, highCostBarHeight, paint);

            paint.Color = SKColors.Red;
            canvas.DrawRect(1200, height - lowCostBarHeight, barWidth, lowCostBarHeight, paint);

            var textPaint = new SKPaint { Color = SKColors.Black, TextSize = 80, IsAntialias = true };
            canvas.DrawText("High Cost", 400, height - highCostBarHeight - 50, textPaint);
            canvas.DrawText("Low Cost", 1200, height - lowCostBarHeight - 50, textPaint);

            canvas.Flush();
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private byte[] GeneratePieChart(int highCostCount, int lowCostCount)
        {
            int width = 2000, height = 2000;
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;

            canvas.Clear(SKColors.White);

            float total = highCostCount + lowCostCount;
            float highCostAngle = 360 * (highCostCount / total);
            float lowCostAngle = 360 - highCostAngle;

            var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };

            paint.Color = SKColors.Blue;
            canvas.DrawArc(new SKRect(200, 200, 1800, 1800), -90, highCostAngle, true, paint);

            paint.Color = SKColors.Red;
            canvas.DrawArc(new SKRect(200, 200, 1800, 1800), -90 + highCostAngle, lowCostAngle, true, paint);

            canvas.Flush();
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private byte[] GenerateLineChart(List<float> costs)
        {
            int width = 600, height = 300;
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            var canvas = surface.Canvas;

            canvas.Clear(SKColors.White);

            var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = true };
            canvas.DrawLine(50, 250, 550, 250, axisPaint); // X-axis
            canvas.DrawLine(50, 250, 50, 50, axisPaint);  // Y-axis

            var linePaint = new SKPaint { Color = SKColors.Blue, StrokeWidth = 2, IsAntialias = true };
            var pointPaint = new SKPaint { Color = SKColors.Red, IsAntialias = true, Style = SKPaintStyle.Fill };

            float xStep = 500f / (costs.Count - 1);
            float maxCost = costs.Max();
            float yScale = 200f / maxCost;

            for (int i = 0; i < costs.Count - 1; i++)
            {
                float x1 = 50 + i * xStep;
                float y1 = 250 - (costs[i] * yScale);
                float x2 = 50 + (i + 1) * xStep;
                float y2 = 250 - (costs[i + 1] * yScale);

                canvas.DrawLine(x1, y1, x2, y2, linePaint);
                canvas.DrawCircle(x1, y1, 3, pointPaint);
            }

            // Export as image
            canvas.Flush();
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

    }
}
