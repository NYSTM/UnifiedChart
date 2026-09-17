namespace UnifiedChart
{
    /// <summary>
    /// プロット領域内の任意のデータ座標にテキストを表示するための注釈。
    /// </summary>
    public class TextAnnotation
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Color { get; set; } = "Black";
    }
}
