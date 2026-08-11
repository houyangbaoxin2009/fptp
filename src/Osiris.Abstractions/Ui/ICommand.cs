namespace Osiris.Abstractions.Ui;

/// <summary>
/// 命令契约：插件命令与壳内置命令统一接口。
/// 骨架占位：实现阶段扩充（CanExecute/快捷键等）。
/// </summary>
public interface ICommand
{
    /// <summary>命令唯一 Id。</summary>
    string Id { get; }

    /// <summary>显示名。</summary>
    string DisplayName { get; }

    /// <summary>执行命令。</summary>
    void Execute(object? parameter);
}
