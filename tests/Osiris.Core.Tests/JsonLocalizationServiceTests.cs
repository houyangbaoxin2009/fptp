using Osiris.Core.Localization;
using Xunit;

namespace Osiris.Core.Tests;

/// <summary>
/// 语言包服务测试：加载/合并/优先级/模块语言包注册/未命中回退。
/// 用临时目录构造语言包，验证内置 → 模块 → 用户三档优先级与语言切换行为。
/// </summary>
public class JsonLocalizationServiceTests
{
    private static readonly string TempRoot =
        Path.Combine(Path.GetTempPath(), "osiris-l10n-tests", Guid.NewGuid().ToString("N"));

    /// <summary>构造临时目录树：内置（langs）/ 模块 A（langs）/ 模块 B（langs）/ 用户（langs）。</summary>
    private static string SetupTree()
    {
        string root = Path.Combine(TempRoot, Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        WriteLang(Path.Combine(root, "builtin"), "zh-cn", new Dictionary<string, string> { ["$name"] = "简体中文" });
        WriteLang(Path.Combine(root, "builtin"), "en-us", new Dictionary<string, string>
        {
            ["$name"] = "English",
            ["文件"] = "File",
            ["保存"] = "Save",
            ["图层"] = "Layers",
        });
        WriteLang(Path.Combine(root, "moduleA"), "en-us", new Dictionary<string, string>
        {
            ["$name"] = "English",
            ["打开"] = "Open",      // 模块 A 独有
            ["保存"] = "SaveAll",   // 覆盖内置（模块优先级 > 内置）
        });
        WriteLang(Path.Combine(root, "user"), "en-us", new Dictionary<string, string>
        {
            ["$name"] = "English",
            ["保存"] = "UserSave",  // 覆盖模块（用户优先级最高）
        });
        return root;
    }

    private static void WriteLang(string dir, string id, Dictionary<string, string> entries)
    {
        Directory.CreateDirectory(Path.Combine(dir, "langs"));
        string json = "{" + string.Join(",", entries.Select(kv => $"\"{kv.Key}\":\"{kv.Value}\"")) + "}";
        File.WriteAllText(Path.Combine(dir, "langs", $"{id}.json"), json);
    }

    [Fact]
    public void Translate_未命中返回原文()
    {
        string root = SetupTree();
        try
        {
            var svc = new JsonLocalizationService([Path.Combine(root, "builtin", "langs")]);
            svc.LoadLanguage("en-us");

            Assert.Equal("File", svc.Translate("文件"));     // 内置命中
            Assert.Equal("未翻译词", svc.Translate("未翻译词")); // 未命中返回原文
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void RegisterLanguagePack_模块条目合并且覆盖内置()
    {
        string root = SetupTree();
        try
        {
            var svc = new JsonLocalizationService([Path.Combine(root, "builtin", "langs")]);
            svc.LoadLanguage("en-us");
            Assert.Equal("Layers", svc.Translate("图层"));

            // 注册模块 A 语言包（晚于初始加载 → 自动重载合并）
            svc.RegisterLanguagePack(Path.Combine(root, "moduleA", "langs"));
            Assert.Equal("Open", svc.Translate("打开"));        // 模块独有条目生效
            Assert.Equal("SaveAll", svc.Translate("保存"));     // 覆盖内置
            Assert.Equal("Layers", svc.Translate("图层"));      // 内置条目保留
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void RegisterLanguagePack_用户目录优先级最高()
    {
        string root = SetupTree();
        try
        {
            // 内置（显式）+ 用户（显式，最高优先）+ 模块（注册）三档齐备
            var svc = new JsonLocalizationService(
                [Path.Combine(root, "builtin", "langs")],
                [Path.Combine(root, "user", "langs")]);
            svc.RegisterLanguagePack(Path.Combine(root, "moduleA", "langs"));
            svc.LoadLanguage("en-us");

            Assert.Equal("UserSave", svc.Translate("保存"));    // 用户覆盖模块与内置
            Assert.Equal("Open", svc.Translate("打开"));        // 模块独有仍在
            Assert.Equal("File", svc.Translate("文件"));        // 内置独有仍在
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void LoadLanguage_切换语言重新合并()
    {
        string root = SetupTree();
        try
        {
            var svc = new JsonLocalizationService([Path.Combine(root, "builtin", "langs")]);
            svc.RegisterLanguagePack(Path.Combine(root, "moduleA", "langs"));

            svc.LoadLanguage("en-us");
            Assert.Equal("Open", svc.Translate("打开"));

            // 中文：模块条目未命中 → 返回原文
            svc.LoadLanguage("zh-cn");
            Assert.Equal("打开", svc.Translate("打开"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void RegisterLanguagePack_幂等且目录不存在忽略()
    {
        string root = SetupTree();
        try
        {
            var svc = new JsonLocalizationService([Path.Combine(root, "builtin", "langs")]);
            svc.LoadLanguage("en-us");

            svc.RegisterLanguagePack(Path.Combine(root, "moduleA", "langs"));
            svc.RegisterLanguagePack(Path.Combine(root, "moduleA", "langs")); // 重复注册忽略
            svc.RegisterLanguagePack(Path.Combine(root, "不存在目录"));         // 空目录忽略

            Assert.Equal("Open", svc.Translate("打开"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Translate_参数化格式替换()
    {
        string root = SetupTree();
        try
        {
            WriteLang(Path.Combine(root, "builtin"), "en-us", new Dictionary<string, string>
            {
                ["$name"] = "English",
                ["版本：{0}"] = "Version: {0}",
            });
            var svc = new JsonLocalizationService([Path.Combine(root, "builtin", "langs")]);
            svc.LoadLanguage("en-us");

            Assert.Equal("Version: 1.0.0", svc.Translate("版本：{0}", "1.0.0"));
            // 未命中带参数：回退原文格式化
            Assert.Equal("颜料槽 3", svc.Translate("颜料槽 {0}", 3));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void AvailableLanguages_扫描全部目录并去重()
    {
        string root = SetupTree();
        try
        {
            var svc = new JsonLocalizationService(
                [Path.Combine(root, "builtin", "langs")],
                [Path.Combine(root, "user", "langs")]);
            svc.RegisterLanguagePack(Path.Combine(root, "moduleA", "langs"));

            var languages = svc.AvailableLanguages;
            Assert.Contains(languages, l => l.Id == "zh-cn");
            Assert.Contains(languages, l => l.Id == "en-us");
            Assert.Single(languages, l => l.Id == "en-us"); // 三目录同 id 去重
        }
        finally { Directory.Delete(root, true); }
    }
}
