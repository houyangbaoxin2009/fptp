namespace fptp
{
	public class GenSettings
	{
		public string SaveFormat { get; set; } = "jpg";

		/// <summary>JPEG 输出质量（70-100，默认 100）</summary>
		public int SaveQuality { get; set; } = 100;

		/// <summary>排版辅助线样式：0=虚线 1=实线 2=无</summary>
		public int GuideLineStyle { get; set; } = 0;

		public int DefaultSize { get; set; } = 1;
		public string BackgroundColor { get; set; } = "蓝色";
		public int Tolerance { get; set; } = 60;

		/// <summary>排版预设索引：0=5寸 1=6寸 2=A4 3=A5 4=自定义</summary>
		public int LayoutPreset { get; set; } = 0;
		public int CustomLayoutW { get; set; } = 1500;
		public int CustomLayoutH { get; set; } = 1050;
	}
}
