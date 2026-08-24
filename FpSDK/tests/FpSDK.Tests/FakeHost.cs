using Osiris.Abstractions;
using Osiris.Abstractions.Document;
using Osiris.Abstractions.Plugins;
using Osiris.Abstractions.Progress;
using Osiris.Abstractions.Ui;

namespace FpSdk.Tests;

/// <summary>内存服务注册表（测试桩）。</summary>
internal sealed class FakeRegistry : IServiceRegistry
{
    private readonly Dictionary<Type, object> _services = [];

    public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

    public T? Get<T>() where T : class
        => _services.TryGetValue(typeof(T), out object? o) ? (T)o : null;
}

/// <summary>进度 stub（记录最后上报值）。</summary>
internal sealed class FakeProgress : IProgress
{
    public double LastPercent { get; private set; }
    public string LastMessage { get; private set; } = "";

    public void Report(double percent, string message)
    {
        LastPercent = percent;
        LastMessage = message;
    }
}

/// <summary>最小宿主桩：无 UI、无活动文档，提供内存服务注册表与进度。</summary>
internal sealed class FakeHost : IHostContext
{
    public OsirisDocument? ActiveDocument => null;
    public IServiceRegistry Services { get; } = new FakeRegistry();
    public IUiService? Ui => null;
    public IProgress Report { get; } = new FakeProgress();
}