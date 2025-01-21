using Proj_backend.Models_ML;
using Proj_backend.Service_ML;

namespace Proj_backend.Services
{
    public class CustomerService
    {
        public static List<CustomerData> LoadDataFromCSV(IFormFile inputFile)
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
                            Cost = (values.Length > 9 && float.TryParse(values[9], out var cost)) ? cost : 0,
                            IsHighCost = (values.Length > 10 && float.TryParse(values[10], out var isHighCost)) ? isHighCost : 0
                        };

                        customers.Add(customer);
                        Console.WriteLine(customer.Cost);
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
