# 贡献指南

感谢你考虑为 FPTP 贡献代码！

## 开发环境

- Visual Studio 2022 或 JetBrains Rider
- .NET Framework 4.8 SDK（随 VS 安装）
- Windows 7 SP1+

## 项目规范

### 代码风格

- 使用 `namespace fptp;` 文件范围命名空间
- 静态工具类放在独立文件中（如 `Basic.cs`、`Prepalg.cs`、`Assalg.cs`）
- 公开方法添加 XML 文档注释
- 使用 `#region` 组织同类方法

### 提交 PR

1. Fork 本仓库
2. 创建特性分支：`git checkout -b feature/your-feature`
3. 提交更改，保持提交信息简洁清晰
4. 推送分支并发起 Pull Request

### 注意事项

- 算法类（`Prepalg`/`Assalg`）应保持无状态静态设计
- UI 逻辑写在 `mainBox.cs`，算法逻辑写在对应工具类
- 图片资源使用后必须 `Dispose()`，避免内存泄漏
- 耗时操作应通过 `Application.DoEvents()` 或异步方式防止 UI 卡死
