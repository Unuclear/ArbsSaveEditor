using System;
using System.Diagnostics;
using System.IO;
using AseCore.ReflectionWrappers;

namespace AseCore
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
                    Console.WriteLine($"{ArbsLocator.GameName} not found. Please use --game-dir \"path/to/game/directory\" to specify the location.");
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

            if (!ArbsAsmLoader.Load()) return 3;

            var dataDirectory = CommandLine.GetOption("--data-dir", args);
            if (string.IsNullOrEmpty(dataDirectory))
            {
                dataDirectory = ArbsLocator.FindDataDirectory();
                if (dataDirectory == null)
                {
                    Console.WriteLine($"{ArbsLocator.GameName} data directory not found. Please use --data-dir \"path/to/data/directory\" to specify the location.");
                    return 4;
                }
            }
            else if (!Directory.Exists(dataDirectory))
            {
                Console.WriteLine("Specified data directory not found. Make sure the directory exists.");
                return 5;
            }
            else
            {
                ArbsLocator.DataDirectory = dataDirectory;
            }

            while (true)
            {
                var wapFiles = ArbsLocator.GetAllWeaponisedAnimalPrefabFiles();
                var numberOfWapFiles = wapFiles.Count;
                if (numberOfWapFiles == 0)
                {
                    Console.WriteLine("No weaponised animal prefabs were found.");
                    return 0;
                }

                var indexLeftPad = numberOfWapFiles.DigitCount();
                Console.WriteLine("Select a weaponised animal prefab:");
                for (var index = 0; index < numberOfWapFiles; index++)
                    Console.WriteLine($" {(index + 1).ToString().PadLeft(indexLeftPad)}. {wapFiles[index]}");

                Console.Write("\nEnter number: ");
                var isValidChoice = false;
                string selected = null;
                do
                {
                    var input = Console.ReadLine();
                    if (input == null) continue;

                    input = input.Trim();
                    if (input == "exit") return 0;

                    isValidChoice = int.TryParse(input, out var choice) && choice >= 1 && choice <= numberOfWapFiles;
                    if (isValidChoice)
                    {
                        selected = wapFiles[choice - 1].ToFileName();
                        Console.WriteLine($" => Selected {selected}");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid choice.");
                    }
                } while (!isValidChoice);
                
                Debug.Assert(selected != null, nameof(selected) + " != null");

                try
                {
                    var prefabObject = ArbsSerializer.Deserialize(
                        Path.Combine(dataDirectory, selected),
                        "WeaponisedAnimalPrefab"
                    );
                    var prefab = new WeaponisedAnimalPrefab(prefabObject);

                    Console.WriteLine($"\n{prefab}\n");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return 6;
                }
            }
        }
    }
}