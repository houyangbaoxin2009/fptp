using System.IO;
using System.Text.Json;

namespace Osiris.Settings
{
    /// <summary>JSON 配置持久化（即时保存）。</summary>
    public static class SettingsStore
    {
        private static string _path;

        /// <summary>初始化配置文件路径。</summary>
        public static void Init(string path)
        {
            _path = path;
        }

        /// <summary>读取配置（不存在则返回默认实例）。</summary>
        public static T Load<T>(T defaults = null) where T : class, new()
        {
            if (_path == null || !File.Exists(_path))
                return defaults ?? new T();
            try
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<T>(json) ?? defaults ?? new T();
            }
            catch
            {
                return defaults ?? new T();
            }
        }

        /// <summary>保存配置（即时写文件）。</summary>
        public static void Save<T>(T value)
        {
            if (_path == null) return;
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_path, JsonSerializer.Serialize(value,
                new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
