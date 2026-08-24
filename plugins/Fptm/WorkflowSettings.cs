using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Localization;
using Osiris.Abstractions.Settings;
using Osiris.Abstractions.Ui;

namespace Fptm;

/// <summary>
/// 证件照工作流设置组（fptm）：换底色 / 智能裁切 / 排版的参数与相纸设置。
/// 经 ISettingProvider 即时 JSON 持久化，工作流命令读取当前值组装参数。
/// </summary>
internal static class Settings
{
    // 换底色
    public static readonly ColorSettingItem ReplaceBgColor = new((uint)Osiris.Algorithms.ColorUtil.PackBgra(0, 0, 255))
    {
        GroupId = FptmModule.ModuleId, Key = "replaceBgColor",
        Label = L10n.T("换底色"), Scope = SettingScope.User,
    };

    public static readonly NumberSettingItem ReplaceBgTolerance = new(60, 10, 200, 5)
    {
        GroupId = FptmModule.ModuleId, Key = "replaceBgTolerance",
        Label = L10n.T("换底容差"), Scope = SettingScope.User,
    };

    public static readonly NumberSettingItem ReplaceBgFeather = new(3, 0, 20, 1)
    {
        GroupId = FptmModule.ModuleId, Key = "replaceBgFeather",
        Label = L10n.T("边缘羽化"), Scope = SettingScope.User,
    };

    public static readonly FilePathSettingItem ReplaceBgImage = new("", false)
    {
        GroupId = FptmModule.ModuleId, Key = "replaceBgImage",
        Label = L10n.T("背景图片"), Scope = SettingScope.User,
    };

    // 智能裁切
    public static readonly ChoiceSettingItem SmartCropPreset = new(
        Workflow.SmartCrop.SizePresets.Select(p => p.Name).ToArray(),
        Workflow.SmartCrop.SizePresets[0].Name)
    {
        GroupId = FptmModule.ModuleId, Key = "smartCropPreset",
        Label = L10n.T("裁切尺寸预设"), Scope = SettingScope.User,
    };

    // 排版
    public static readonly ChoiceSettingItem LayoutPaper = new(
        ["5寸", "6寸", "A5", "A4", Workflow.LayoutComposer.CustomPaper], "5寸")
    {
        GroupId = FptmModule.ModuleId, Key = "layoutPaper",
        Label = L10n.T("排版相纸"), Scope = SettingScope.User,
    };

    public static readonly NumberSettingItem LayoutWidth = new(1500, 100, 6000, 50)
    {
        GroupId = FptmModule.ModuleId, Key = "layoutWidth",
        Label = L10n.T("自定义相纸宽"), Scope = SettingScope.User,
    };

    public static readonly NumberSettingItem LayoutHeight = new(1050, 100, 6000, 50)
    {
        GroupId = FptmModule.ModuleId, Key = "layoutHeight",
        Label = L10n.T("自定义相纸高"), Scope = SettingScope.User,
    };

    public static readonly BoolSettingItem LayoutGuides = new(true)
    {
        GroupId = FptmModule.ModuleId, Key = "layoutGuides",
        Label = L10n.T("画裁剪引导线"), Scope = SettingScope.User,
    };

    public static SettingGroup Group { get; } = new()
    {
        Id = FptmModule.ModuleId,
        DisplayName = L10n.T("证件照工作流"),
        Items = [ReplaceBgColor, ReplaceBgTolerance, ReplaceBgFeather, ReplaceBgImage,
                 SmartCropPreset, LayoutPaper, LayoutWidth, LayoutHeight, LayoutGuides],
    };
}