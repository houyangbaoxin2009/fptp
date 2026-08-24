using FpSDK;
using Osiris.Abstractions;
using Osiris.Abstractions.Plugins;

namespace {{Name}};

/// <summary>{{DisplayName}} 模块。</summary>
[PluginExport]
public sealed class {{Name}}Module : ModuleBase
{
    /// <inheritdoc />
    public override string Id => "{{Id}}";

    /// <inheritdoc />
    public override string Name => "{{DisplayName}}";

    /// <summary>初始化钩子：注册服务 / 贡献命令、菜单、面板、设置组。</summary>
    protected override void OnInitialize(IHostContext host)
    {
        // TODO（示例）：
        // host.Services.Register(new MyService());
        // if (host.Ui is { } ui) ui.RegisterCommand(new MyCommand(host));
    }
}