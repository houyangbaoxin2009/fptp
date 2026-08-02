using System.Collections.Generic;

namespace Osiris.Core.Filters
{
    /// <summary>参数控件类型：壳据此生成输入控件。</summary>
    public enum FilterParameterKind
    {
        /// <summary>整数滑块/数值框（Min~Max）。</summary>
        Int,
        /// <summary>下拉选项（Choices/ChoiceValues）。</summary>
        Choice,
        /// <summary>颜色下拉（值经 ColorUtil.PackBgra 打包，壳可预览色块）。</summary>
        Color
    }

    /// <summary>
    /// 滤镜参数声明式描述：壳据此自动生成参数对话框（模组零 WinForms 依赖）。
    /// Choice 选项值可为任意 object（如 int[] 表示宽高组合），语义由滤镜定义。
    /// </summary>
    public sealed class FilterParameterDescriptor
    {
        /// <summary>参数键（写入 FilterParameters）。</summary>
        public string Key { get; set; }
        /// <summary>对话框显示名。</summary>
        public string Label { get; set; }
        public FilterParameterKind Kind { get; set; }
        /// <summary>Int 型范围。</summary>
        public int Min { get; set; }
        public int Max { get; set; }
        /// <summary>Choice/Color 选项显示文本。</summary>
        public string[] Choices { get; set; }
        /// <summary>Choice/Color 选项对应值（与 Choices 等长）。</summary>
        public object[] ChoiceValues { get; set; }
    }
}
