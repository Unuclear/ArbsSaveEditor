namespace AseCore
{
    public static class CommandLine
    {
        public static string GetOption(string option, string[] args)
        {
            for (var i = 0; i < args.Length; i++)
                if (args[i] == option && i + 1 < args.Length)
                    return args[i + 1];
            return null;
        }
    }
}