using System;
using System.IO;
using System.Reflection;

namespace AseCore
{
    public static class ArbsAsmLoader
    {
        private static Assembly _asm;

        public static bool Load()
        {
            var path = ArbsLocator.AssemblyDllPath;
            if (!File.Exists(path))
            {
                Console.WriteLine($"{ArbsLocator.GameName} assembly could not be found: {path}");
                return false;
            }

            try
            {
                _asm = Assembly.LoadFrom(path);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{ArbsLocator.GameName} assembly could not be loaded: {e.Message}");
                return false;
            }

            return true;
        }

        public static Type GetArbsType(string name)
        {
            if (_asm == null)
                throw new InvalidOperationException($"{ArbsLocator.GameName} assembly has not been loaded yet!");

            var type = _asm.GetType(name);
            if (type != null) return type;

            throw new TypeLoadException($"Type {name} not found!");
        }
    }
}