using System;

#nullable enable

namespace Discord_Driver_Bot
{
    public static class Utility
    {
        public static bool InDocker { get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; } }

        public const string PatreonUrl = "https://patreon.com/konnokai";
        public const string PaypalUrl = "https://paypal.me/jun112561";

        public static object? GetEnvironmentVariable(string varName, Type T, bool exitIfNoVar = false)
        {
            string? value = Environment.GetEnvironmentVariable(varName);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (exitIfNoVar)
                {
                    Log.Error($"{varName} 遺失，請輸入至環境變數後重新運行");
                    if (!Console.IsInputRedirected)
                        Console.ReadKey();
                    Environment.Exit(3);
                }
                return default;
            }
            return Convert.ChangeType(value, T);
        }
    }
}
