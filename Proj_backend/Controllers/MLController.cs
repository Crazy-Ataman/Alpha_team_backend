using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Proj_backend.Models_ML;
using Proj_backend.Service_ML;

namespace Proj_backend.Controllers
{
    public abstract class BaseMLController<TInput, TOutput> : ControllerBase
        where TInput : class
        where TOutput : class, new()
    {
        protected readonly MLService<TInput, TOutput> _mlService;
        protected readonly MLContext _mlContext;
        protected readonly string _modelPath;

        public BaseMLController(string modelPath)
        {
            _mlContext = new MLContext();
            _mlService = new MLService<TInput, TOutput>();
            _modelPath = modelPath;
        }

        protected ITransformer GetOrCreateModel(string dataPath, char separator, bool hasHeader, IEstimator<ITransformer> pipeline)
        {
            if (System.IO.File.Exists(_modelPath))
            {
                return _mlService.LoadModel(_modelPath);
            }

            var data = _mlService.LoadData(dataPath, separator, hasHeader);
            _mlService.PreviewData(data);
            return _mlService.TrainModel(data, pipeline, _modelPath);
        }
    }
}
