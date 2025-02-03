using Microsoft.AspNetCore.Components.Forms;
using Microsoft.ML;
using Proj_backend.Models_ML;

namespace Proj_backend.Service_ML
{
    public class MLService<TInput, TOutput>
        where TInput : class
        where TOutput : class, new()
    {
        private readonly MLContext _mlContext;

        public MLService()
        {
            _mlContext = new MLContext();
        }

        public IDataView LoadData(string dataPath, char separator, bool hasHeader)
        {
            return _mlContext.Data.LoadFromTextFile<TInput>(dataPath, separatorChar: separator, hasHeader: hasHeader);
        }

        // For debugging purposes
        public void PreviewData(IDataView data)
        {
            var preview = data.Preview();
            Console.WriteLine("Data Preview:");
            foreach (var row in preview.RowView)
            {
                foreach (var column in row.Values)
                {
                    Console.WriteLine($"{column.Key}: {column.Value}");
                }
                Console.WriteLine("-----------------");
            }
        }

        public ITransformer TrainModel(IDataView data, IEstimator<ITransformer> pipeline, string modelPath)
        {
            var model = pipeline.Fit(data);
            _mlContext.Model.Save(model, data.Schema, modelPath);
            Console.WriteLine($"Model trained and saved to {modelPath}");
            return model;
        }

        public ITransformer LoadModel(string modelPath)
        {
            if (File.Exists(modelPath))
            {
                Console.WriteLine("Loading model from disk...");
                return _mlContext.Model.Load(modelPath, out var modelSchema);
            }
            throw new FileNotFoundException($"Model file not found at {modelPath}");
        }

        public TOutput Predict(ITransformer model, TInput input)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<TInput, TOutput>(model);
            return predictionEngine.Predict(input);
        }

        public List<CustomerData> LoadDataFromCSV(IFormFile inputFile)
        {
            if (inputFile == null || inputFile.Length == 0)
            {
                throw new FileNotFoundException("No file was uploaded.");
            }

            List<CustomerData> customers = new List<CustomerData>();

            try
            {
                using (var reader = new StreamReader(inputFile.OpenReadStream()))
                {
                    string[] headers = reader.ReadLine()?.Split(','); // Read the header row

                    if (headers == null)
                        throw new Exception("The input file is empty or missing headers.");

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Split each line into columns
                        string[] values = line.Split(',');

                        var customer = new CustomerData
                        {
                            CustomerID = int.Parse(values[0]),
                            FirstName = values[1],
                            LastName = values[2],
                            Age = float.Parse(values[3]),
                            Gender = values[4],
                            Height_cm = float.Parse(values[5]),
                            Weight_kg = float.Parse(values[6]),
                            HealthStatus = values[7],
                            LastContactDate = values[8],
                            Cost = 0,
                            IsHighCost = 0
                        };

                        customers.Add(customer);
                        Console.WriteLine(customer);
                    }
                }

                Console.WriteLine("CSV file read successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return customers;
        }
    }
}
