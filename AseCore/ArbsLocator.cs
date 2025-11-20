using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AseCore
{
    public static class ArbsLocator
    {
        private const string GameId = "1215390";
        public const string GameName = "Animal Revolt Battle Simulator";

        public static string GameDirectory { get; set; }

        public static string DataDirectory { get; set; }

        public static string FindGameDirectory()
        {
            string[] paths;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                paths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                paths = new[]
                {
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Library",
                        "Application Support",
                        "Steam"
                    )
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                paths = new[]
                {
                    Path.Combine(home, ".local", "share", "Steam"),
                    Path.Combine(home, ".steam", "steam")
                };
            }
            else
            {
                paths = Array.Empty<string>();
            }

            foreach (var path in paths)
            {
                var gamePath = Path.Combine(path, "steamapps", "common", GameName);
                if (!Directory.Exists(gamePath)) continue;

                GameDirectory = Path.GetFullPath(gamePath);
                return GameDirectory;
            }

            return null;
        }

        public static string FindDataDirectory()
        {
            string[] paths;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                paths = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                paths = new[]
                {
                    Path.Combine(
                        home,
                        ".local",
                        "share",
                        "Steam",
                        "steamapps",
                        "compatdata",
                        GameId,
                        "pfx",
                        "drive_c",
                        "users",
                        "steamuser"
                    ),
                    Path.Combine(
                        home,
                        ".steam",
                        "steam",
                        "steamapps",
                        "compatdata",
                        GameId,
                        "pfx",
                        "drive_c",
                        "users",
                        "steamuser"
                    )
                };
            }
            else
            {
                paths = Array.Empty<string>();
            }

            foreach (var path in paths)
            {
                var dataPath = Path.Combine(path, "AppData", "LocalLow", "VDimension", GameName);
                if (!Directory.Exists(dataPath)) continue;

                DataDirectory = Path.GetFullPath(dataPath);
                return DataDirectory;
            }

            return null;
        }

        public static List<WapFile> GetAllWeaponisedAnimalPrefabFiles()
        {
            if (DataDirectory == null) throw new InvalidOperationException("Set the data directory first!");

            return Directory.EnumerateFiles(
                    DataDirectory,
                    "WEP_ANIM_PREF_*",
                    SearchOption.TopDirectoryOnly
                ).Where(file => string.IsNullOrEmpty(Path.GetExtension(file))) // Filter out preview images
                .Select(Path.GetFileName)
                .OrderBy(file => file)
                .Select(WapFile.FromFile)
                .ToList();
        }
    }

    public class WapFile
    {
        private const string Prefix = "WEP_ANIM_PREF_";

        private WapFile(string prefabName, string timestamp)
        {
            if (string.IsNullOrWhiteSpace(prefabName))
                throw new ArgumentException("Prefab name cannot be empty!", nameof(prefabName));
            if (string.IsNullOrWhiteSpace(timestamp))
                throw new ArgumentException("Timestamp cannot be empty!", nameof(timestamp));

            PrefabName = prefabName;
            Timestamp = timestamp;
        }

        public string PrefabName { get; }
        public string Timestamp { get; }

        public string ToFileName()
        {
            return new StringBuilder(Prefix)
                .Append(PrefabName)
                .Append("_")
                .Append(Timestamp)
                .ToString();
        }

        public override string ToString()
        {
            return new StringBuilder(PrefabName)
                .Append(" (")
                .Append(Timestamp)
                .Append(")")
                .ToString();
        }

        public static WapFile FromFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty!", nameof(fileName));
            if (!fileName.StartsWith(Prefix))
                throw new ArgumentException(
                    $"Invalid weaponised animal prefab file name: {fileName}",
                    nameof(fileName)
                );

            var withoutPrefix = fileName.Substring(Prefix.Length);

            var split = withoutPrefix.Split('_');
            if (split.Length != 2)
                throw new ArgumentException(
                    $"Invalid weaponised animal prefab file name: {fileName}",
                    nameof(fileName)
                );

            return new WapFile(split[0], split[1]);
        }
    }
}