using FpSDK;
using Osiris.Abstractions;
using Osiris.Abstractions.Modules;
using Osiris.Abstractions.Plugins;
using Xunit;

namespace FpSdk.Tests;

[PluginExport]
internal sealed class SampleModule : ModuleBase
{
    public override string Id => "test.mod";
    public override string Name => "测试模块";
    public bool Initialized { get; private set; }
    protected override void OnInitialize(IHostContext host) => Initialized = true;
}

/// <summary>ModuleBase / FpContext 冒烟：默认元数据、Initialize 注入、服务注册与获取。</summary>
public class ModuleBaseTests
{
    [Fact]
    public void Defaults_AreSane()
    {
        var m = new SampleModule();
        Assert.Equal("test.mod", m.Id);
        Assert.Equal("测试模块", m.Name);
        Assert.Equal("1.0.0.0", m.Version);
        Assert.Equal("1.0.0", m.MinHostVersion);
        Assert.Equal(ModuleKind.Extension, m.Kind);
        Assert.Empty(m.Dependencies);
        Assert.Null(m.Host);              // 未初始化
    }

    [Fact]
    public void Initialize_InjectsHost_AndFiresHook()
    {
        var host = new FakeHost();
        var m = new SampleModule();
        m.Initialize(host);
        Assert.True(m.Initialized);
        Assert.Same(host, m.Host);
        Assert.True(m.Context.HasUi == false);
    }

    [Fact]
    public void FpContext_RegisterAndGetService()
    {
        var host = new FakeHost();
        var m = new SampleModule();
        m.Initialize(host);

        m.Context.Register(new SampleService());
        var got = m.Context.Service<SampleService>();
        Assert.NotNull(got);
    }

    [Fact]
    public void FpContext_Report_Forwards()
    {
        var host = new FakeHost();
        var m = new SampleModule();
        m.Initialize(host);
        m.Context.Report(50, "处理中");
        Assert.Equal(50, ((FakeProgress)host.Report).LastPercent);
        Assert.Equal("处理中", ((FakeProgress)host.Report).LastMessage);
    }

    internal sealed class SampleService { }
}