namespace Osiris.Core.Storage;

/// <summary>
/// 配置存储抽象（格式中立）：注册表/模块配置持久化的唯一入口。
/// 数据面为扁平键值（"组.键" -> 标量值），值类型与 JSON/tie:data 标量一一对应
/// （bool / double / string）。当前实现 JsonConfigStore（System.Text.Json），
/// 未来 TieDataConfigStore 实现同接口即无缝切换（tie:data 预留，见架构 9.1 节）。
/// </summary>
public interface IConfigStore
{
    /// <summary>
    /// 读取配置：返回扁平键值字典。
    /// 文件不存在、损坏（非法 JSON）或读取失败时返回空字典（调用方按"无配置"处理）。
    /// </summary>
    IReadOnlyDictionary<string, object> Load(string filePath);

    /// <summary>写入配置：先确保文件所在目录存在（Directory.CreateDirectory），再整文件覆盖写入。</summary>
    void Save(string filePath, IReadOnlyDictionary<string, object> data);
}
