using System;

namespace Osiris.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.WriteLine("Osiris.Cli (2.0 骨架)");
            Console.WriteLine($"参数: {string.Join(" ", args)}");
            return 0;
        }
    }
}
