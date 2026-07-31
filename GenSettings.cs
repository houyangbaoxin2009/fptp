namespace fptp
{
	/// <summary>处理预设：一组参数组合，一键套用。</summary>
	public class PresetProfile
	{
		public string Name { get; set; } = "";
		public int DefaultSize { get; set; } = 1;
		public string BackgroundColor { get; set; } = "蓝色";
		public int Tolerance { get; set; } = 60;
		public bool AnimeMode { get; set; } = false;
		public int LayoutPreset { get; set; } = 0;
		public string SaveFormat { get; set; } = "jpg";
		public int SaveQuality { get; set; } = 100;
	}

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

		/// <summary>动画模式：换底色时用连通域洪泛填充，保护眼白等被主体包围的相似色区域</summary>
		public bool AnimeMode { get; set; } = false;

		/// <summary>排版预设索引：0=5寸 1=6寸 2=A4 3=A5 4=自定义</summary>
		public int LayoutPreset { get; set; } = 0;
		public int CustomLayoutW { get; set; } = 1500;
		public int CustomLayoutH { get; set; } = 1050;

		/// <summary>处理预设模板列表</summary>
		public System.Collections.Generic.List<PresetProfile> Presets { get; set; } =
			new System.Collections.Generic.List<PresetProfile>();

		/// <summary>当前选中的预设索引，-1 表示未使用预设</summary>
		public int CurrentPreset { get; set; } = -1;
	}
}
