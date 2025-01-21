using Microsoft.ML.Data;

namespace Proj_backend.Models_ML
{
    public class CustomerData
    {
        [LoadColumn(0)] public int CustomerID { get; set; }
        [LoadColumn(1)] public string FirstName { get; set; }
        [LoadColumn(2)] public string LastName { get; set; }
        [LoadColumn(3)] public float Age { get; set; }
        [LoadColumn(4)] public string Gender { get; set; }
        [LoadColumn(5)] public float Height_cm { get; set; }
        [LoadColumn(6)] public float Weight_kg { get; set; }
        [LoadColumn(7)] public string HealthStatus { get; set; }
        [LoadColumn(8)] public string LastContactDate { get; set; }
        [LoadColumn(9)] public float Cost { get; set; }
        [LoadColumn(10)] public float IsHighCost { get; set; }
    }

    public class CostPrediction
    {
        [ColumnName("Score")]
        public float Cost { get; set; }
    }

    public class IsHighCostPrediction
    {
        [ColumnName("Score")]
        public float IsHighCost { get; set; }
    }
}
