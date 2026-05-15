namespace ODProxl.ClientCommonModels.Onnx
{
    public class BoundingBox
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string Label { get; set; }
        public float Confidence { get; set; }
        public byte[,] Mask { get; set; }           // 最终掩码
        public float[] MaskCoeffs { get; set; }     // 临时存储掩码系数
    }
}
