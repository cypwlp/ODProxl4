namespace ODProxl.ClientCommonModels.Onnx
{
    public class OnnxResult
    {        // 分类结果（适用于分类模型）
        public List<ClassificationPrediction> Classifications { get; set; }

        // 检测结果（适用于目标检测模型）
        public List<BoundingBox> Boxes { get; set; }

        // 分割掩码（适用于分割模型）
        public byte[][] Masks { get; set; }

        // 原始输出张量（保留原始数据，供调试或自定义处理）
        public object RawOutput { get; set; }
    }
}
