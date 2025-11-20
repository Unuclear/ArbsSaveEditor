using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AseCore;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AseNameExtractor
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            var gameDirectory = CommandLine.GetOption("--game-dir", args);
            if (string.IsNullOrEmpty(gameDirectory))
            {
                gameDirectory = ArbsLocator.FindGameDirectory();
                if (gameDirectory == null)
                {
                    Console.WriteLine(
                        $"{ArbsLocator.GameName} not found. Please use --game-dir \"path/to/game/directory\" to specify the location."
                    );
                    return 1;
                }
            }
            else if (!Directory.Exists(gameDirectory))
            {
                Console.WriteLine("Specified game directory not found. Make sure the game is installed.");
                return 2;
            }
            else
            {
                ArbsLocator.GameDirectory = gameDirectory;
            }

            var asm = AssemblyDefinition.ReadAssembly(ArbsLocator.AssemblyDllPath);

            var prefabsManager = asm.MainModule.GetType("PrefabsManager");
            if (prefabsManager == null)
            {
                Console.WriteLine("Could not find PrefabsManager class!");
                return 3;
            }

            var startMethod = prefabsManager.Methods.FirstOrDefault(m => m.Name == "Start");
            if (startMethod == null)
            {
                Console.WriteLine("Could not find Start() method!");
                return 4;
            }

            var unitDictionary = new Dictionary<int, string>();
            var attachmentDictionary = new Dictionary<int, string>();

            var instructions = startMethod.Body.Instructions;
            for (var i = 0; i < instructions.Count - 2; i++)
            {
                var instruction = instructions[i];
                var nextInstruction = instructions[i + 1];

                // Look for: ldc.i4 <int> followed by ldstr <string> followed by a call to Dictionary.Add
                var key = GetIntInstructionOperand(instruction);
                if (key == 0 ||
                    nextInstruction.OpCode != OpCodes.Ldstr ||
                    instructions[i + 2].OpCode != OpCodes.Callvirt) continue;
                // Skip map-related game objects
                if (key >= 1000 && key < 4000) continue;

                var value = ((string)nextInstruction.Operand).Split('/')[1];
                if (key < 1000) unitDictionary[key] = value;
                else attachmentDictionary[key] = value;
            }

            Console.WriteLine($"Found {unitDictionary.Count} unit names.");
            foreach (var kv in unitDictionary) Console.WriteLine($" {kv.Key}: {kv.Value}");

            Console.WriteLine($"\nFound {attachmentDictionary.Count} attachment names.");
            foreach (var kv in attachmentDictionary) Console.WriteLine($" {kv.Key}: {kv.Value}");

            if (unitDictionary.Count <= 0 && attachmentDictionary.Count <= 0) return 0;

            Console.Write("\nDo you want to save the names? (y/n) : ");
            var answer = char.ToLower(Console.ReadKey().KeyChar);
            Console.WriteLine();
            if (answer != 'y') return 0;

            if (unitDictionary.Count > 0)
            {
                const string unitDictionaryFile = "unit-dictionary.json";
                File.WriteAllText(unitDictionaryFile, JsonSerializer.Serialize(unitDictionary));
                Console.WriteLine($"=> Saved unit names to {unitDictionaryFile}");
            }

            if (attachmentDictionary.Count <= 0) return 0;

            const string attachmentDictionaryFile = "attachment-dictionary.json";
            File.WriteAllText(attachmentDictionaryFile, JsonSerializer.Serialize(attachmentDictionary));
            Console.WriteLine($"=> Saved attachment names to {attachmentDictionaryFile}");

            return 0;
        }

        private static int GetIntInstructionOperand(Instruction instruction)
        {
            switch (instruction.OpCode.Code)
            {
                case Code.Ldc_I4_1: return 1;
                case Code.Ldc_I4_2: return 2;
                case Code.Ldc_I4_3: return 3;
                case Code.Ldc_I4_4: return 4;
                case Code.Ldc_I4_5: return 5;
                case Code.Ldc_I4_6: return 6;
                case Code.Ldc_I4_7: return 7;
                case Code.Ldc_I4_8: return 8;
                case Code.Ldc_I4_S: return (sbyte)instruction.Operand;
                case Code.Ldc_I4: return (int)instruction.Operand;
                default: return 0;
            }
        }
    }
}