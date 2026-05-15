using Microsoft.ML.OnnxRuntime;
using ODProxl.ClientCommonModels.Onnx;

namespace ODProxl.ClientServices
{
    public interface IPostprocessor
    {
        OnnxResult Process(IReadOnlyList<NamedOnnxValue> outputs);
    }
}
