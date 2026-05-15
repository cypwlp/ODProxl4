using ODProxl.ClientCommonModels.Onnx;

namespace ODProxl.ClientServices
{
    public interface IOnnxInferenceService
    {
        Task<OnnxResult> PredictAsync(object image);
        Task<List<OnnxResult>> PredictBatchAsync(IEnumerable<object> images);
    }
}
