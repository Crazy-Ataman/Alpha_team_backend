using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using System.IO;
using Proj_backend.Models_ML;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Proj_backend.Services;
using CsvHelper;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Proj_backend.Controllers
{
    [ApiController]
    [Route("api/linereg")]
    public class Controller_LinearRegression : BaseMLController<CustomerData, CostPrediction>
    {
        public Controller_LinearRegression()
            : base(Path.Combine(AppContext.BaseDirectory, "../../../ML_Models/LineReg.zip")) { }

        string trainingDataPath = Path.Combine(AppContext.BaseDirectory, "../../../Data/Training_data.csv");

        [HttpPost("test-predict")]
        public IActionResult TestPredict()
        {
            try
            {
                var pipeline = _mlContext.Transforms.DropColumns(new[] { "CustomerID", "FirstName", "LastName", "Gender", "LastContactDate", "IsHighCost" })
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("HealthStatus"))
                    .Append(_mlContext.Transforms.Concatenate("Features", "Age", "Height_cm", "Weight_kg", "HealthStatus"))
                    .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "Cost", featureColumnName: "Features"));

                var model = GetOrCreateModel(trainingDataPath, ',', true, pipeline);

                var customer = new CustomerData
                {
                    CustomerID = 111,
                    FirstName = "John",
                    LastName = "Doe",
                    Age = 50,
                    Gender = "male",
                    Height_cm = 160,
                    Weight_kg = 80,
                    HealthStatus = "fair",
                    LastContactDate = "2021-01-01",
                };

                var prediction = _mlService.Predict(model, customer);

                string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                Console.WriteLine(strExeFilePath);

                return Ok(Math.Round(prediction.Cost));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("predict")]
        [Consumes("multipart/form-data")]
        public IActionResult Predict(IFormFile inputFile)
        {
            if (inputFile == null || inputFile.Length == 0)
            {
                return BadRequest("No file was uploaded.");
            }

            try
            {
                List<CustomerData> customers = CustomerService.LoadDataFromCSV(inputFile);

                customers = PredictCost(customers, trainingDataPath);

                //string outputPath = "../../Output/test.csv";
                //using (var writer = new StreamWriter(outputPath))

                //using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                //{
                //    csv.WriteRecords(customers);
                //}

                //// Return the CSV file
                //byte[] fileBytes = System.IO.File.ReadAllBytes(outputPath);
                //return File(fileBytes, "text/csv", "test.csv");

                var memoryStream = new MemoryStream();
                using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(customers);
                    writer.Flush();
                }

                memoryStream.Position = 0;

                return File(memoryStream, "text/csv", "lineReg.csv");

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [NonAction]
        public List<CustomerData> PredictCost(List<CustomerData> customers, string trainingFilePath)
        {
            var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding("HealthStatus")
                .Append(_mlContext.Transforms.Concatenate("Features", "Age", "Height_cm", "Weight_kg", "HealthStatus"))
                .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "Cost", featureColumnName: "Features"));

            var model = GetOrCreateModel(trainingFilePath, ',', true, pipeline);

            var customerDataView = _mlContext.Data.LoadFromEnumerable(customers);
            var predictions = model.Transform(customerDataView);
            var predictedCosts = _mlContext.Data.CreateEnumerable<CostPrediction>(predictions, reuseRowObject: false).ToList();

            for (int i = 0; i < customers.Count; i++)
            {
                customers[i].Cost = (float)Math.Round(predictedCosts[i].Cost);
            }

            return customers;
        }

    }
}
