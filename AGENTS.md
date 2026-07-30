# 用户偏好

## 命名风格
- 中文拼音拼接命名：`Assalg`（助理+算法）、`Prepalg`（预处理+算法）、`gen_setting`（生成设置）

## 语言
- 全程中文交流
- 代码注释、提交消息用中文

## 工作流
1. 改完 → `dotnet build` 验证（0 错误）
2. 用户自行检查
3. 确认后让 AI 提交并推送

## 关注点
- 编译零错误
- UI 美观实用
- 配置持久化（JSON）
- 兼容 Windows 7（.NET Framework 4.8）
- 代码模块化

## 代码风格
- `Bitmap`、`Graphics` 等非托管资源必须 `using` 或显式 `Dispose`
- 即时保存配置（用户操作即写文件）
- 不写多余注释
- 功能与方法一一对应

## 文档
- 维护 README.md、CONTRIBUTING.md、CHANGELOG.md

## 沟通风格
- 指令简短直接
- 不需要啰嗦解释
