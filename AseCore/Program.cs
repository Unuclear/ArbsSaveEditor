using System;
using System.IO;
using System.Linq;
using System.Text;
using AseCore.Extensions;
using AseCore.ReflectionWrappers;

namespace AseCore
{
    internal static class Program
    {
        private static readonly string[] MainMenuOptions =
        {
            "Manipulate user save",
            "Manage animal prefabs",
            "Exit"
        };

        private static readonly string[] UserSaveMenuOptions =
        {
            "Modify maze coin amount",
            "Unlock all skins",
            "Return"
        };

        private static readonly string[] AttachmentMenuOptions =
        {
            "Change attack factor of a single attachment",
            "Change attack factor of all attachments of the same type",
            "Change attack factor of all attachments",
            "Clean up removed attachments (UNSAFE)",
            "Return"
        };

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

            if (!ArbsAsmLoader.Load()) return 3;

            var dataDirectory = CommandLine.GetOption("--data-dir", args);
            if (string.IsNullOrEmpty(dataDirectory))
            {
                dataDirectory = ArbsLocator.FindDataDirectory();
                if (dataDirectory == null)
                {
                    Console.WriteLine(
                        $"{ArbsLocator.GameName} data directory not found. Please use --data-dir \"path/to/data/directory\" to specify the location."
                    );
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
                switch (CommandLine.NumericMenu("Select an option:", MainMenuOptions))
                {
                    case -1: // back
                        Console.WriteLine();
                        break;
                    case 0: // Manipulate user save
                    {
                        try
                        {
                            UserSaveMenu(out var exitRequested);
                            if (exitRequested) return 0;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            return 6;
                        }

                        break;
                    }
                    case 1: // Manage animal prefabs
                    {
                        try
                        {
                            WapSelectionMenu(out var exitRequested);
                            if (exitRequested) return 0;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            return 7;
                        }

                        break;
                    }
                    default: // exit
                        return 0;
                }
        }

        private static void UserSaveMenu(out bool exitRequested)
        {
            exitRequested = false;

            var playerSave = new SaveFile(ArbsSerializer.Deserialize(
                ArbsLocator.SaveFile,
                "SaveFile"
            ));

            var repeatUserSaveMenu = true;
            while (repeatUserSaveMenu)
                switch (CommandLine.NumericMenu($"\n{playerSave}\n", UserSaveMenuOptions))
                {
                    case -2: // exit
                        exitRequested = true;
                        return;
                    case 0: // Modify maze coin amount
                    {
                        ModifyMazeCoinAmount(playerSave, out exitRequested);
                        if (exitRequested) return;
                        break;
                    }
                    case 1: // Unlock all skins
                        UnlockAllSkins(playerSave);
                        break;
                    default: // Return / back
                        Console.WriteLine();
                        repeatUserSaveMenu = false;
                        continue;
                }
        }

        private static void ModifyMazeCoinAmount(SaveFile saveFile, out bool exitRequested)
        {
            exitRequested = false;

            var repeat = true;
            while (repeat)
            {
                var result = CommandLine.ReadInt(
                    $"\nCurrent maze coin amount: {saveFile.MazeCoins}\nNew amount: ",
                    out var newMazeCoinAmount
                );
                switch (result)
                {
                    case -2: // exit
                        exitRequested = true;
                        return;
                    case -1: // back
                        repeat = false;
                        continue;
                }

                if (newMazeCoinAmount < 0)
                {
                    Console.Write($"{saveFile.MazeCoins} is negative.");
                    continue;
                }

                if (newMazeCoinAmount != saveFile.MazeCoins)
                {
                    saveFile.MazeCoins = newMazeCoinAmount;
                    saveFile.Save();
                }

                repeat = false;
            }
        }

        private static void UnlockAllSkins(SaveFile saveFile)
        {
            var hasUnlockedSkins = saveFile.UnlockAllSkins();
            if (hasUnlockedSkins)
            {
                saveFile.Save();
                Console.WriteLine("\nAll missing skins have been unlocked.");
            }
            else
            {
                Console.WriteLine("\nYou already have unlocked all skins.");
            }
        }

        private static void WapSelectionMenu(out bool exitRequested)
        {
            exitRequested = false;

            var repeat = true;
            while (repeat)
            {
                var wapFiles = ArbsLocator.LoadAllWeaponisedAnimalPrefabFiles();
                if (wapFiles.Length == 0)
                {
                    Console.WriteLine("\nNo weaponised animal prefabs were found.\n");
                    break;
                }

                WapFile selected;
                switch (CommandLine.NumericMenu("\nSelect a prefab:", wapFiles))
                {
                    case -2: // exit
                        exitRequested = true;
                        return;
                    case -1: // back
                        Console.WriteLine();
                        repeat = false;
                        continue;
                    case int choice:
                        selected = wapFiles[choice];
                        Console.WriteLine($" => Selected {selected.PrefabName}");
                        break;
                }

                var selectedPath = Path.Combine(ArbsLocator.DataDirectory, selected.ToFileName());
                var wap = new WeaponisedAnimalPrefab(ArbsSerializer.Deserialize(
                    selectedPath,
                    "WeaponisedAnimalPrefab"
                ));

                WapEditMenu(wap, selected, selectedPath, out exitRequested);
                if (exitRequested) return;
            }
        }

        private static void WapEditMenu(
            WeaponisedAnimalPrefab wap,
            WapFile wapFile,
            string wapPath,
            out bool exitRequested
        )
        {
            exitRequested = false;

            var hasChanges = false;
            var repeat = true;
            while (repeat)
            {
                var editMenuTitle = new StringBuilder("\n")
                    .Append(wap.Name)
                    .Append(" (")
                    .Append(wapFile.Timestamp)
                    .Append(")")
                    .Append(hasChanges ? " *" : "")
                    .ToString();

                string[] editMenuOptions =
                {
                    "Show all attributes",
                    "Print debug information",
                    $"Change name [{wap.Name}]",
                    $"Change team [{wap.Team}]",
                    $"Change AI mode [{wap.AiMode}]",
                    $"Change detection distance [{wap.DetectionDistance}]",
                    $"Change health factor [{wap.HealthFactor}]",
                    $"Change attack factor [{wap.AttackFactor}]",
                    $"Manage attachments [{wap.Attachments.Count}]",
                    "Save & return",
                    "Cancel"
                };

                switch (CommandLine.NumericMenu(editMenuTitle, editMenuOptions))
                {
                    case -2: // exit
                        exitRequested = true;
                        return;
                    case 0: // Show all attributes
                        CommandLine.PrintAndPause($"\n{wap}");
                        break;
                    case 1: // Print debug information
                        CommandLine.PrintAndPause($"\n{wap.ToDebugString()}");
                        break;
                    case 2: // Change name
                    {
                        ChangeName(wap, ref hasChanges, out exitRequested);
                        if (exitRequested) return;
                        break;
                    }
                    case 3: // Change team
                    {
                        ChangeTeam(wap, ref hasChanges, out exitRequested);
                        if (exitRequested) return;
                        break;
                    }
                    case 4: // Change AI mode
                    {
                        ChangeAiMode(wap, ref hasChanges, out exitRequested);
                        if (exitRequested) return;
                        break;
                    }
                    case 5: // Change detection distance
                    {
                        ChangeDetectionDistance(wap, ref hasChanges, out exitRequested);
                        if (exitRequested) return;
                        break;
                    }
                    case 6: // Change health factor
                    {
                        ChangeHealthFactor(wap, ref hasChanges, out exitRequested);
                        if (exitRequested) return;
                        break;
                    }
                    case 7: // Change attack factor
                    {
                        ChangeAttackFactor(wap, ref hasChanges, out exitRequested);
                        if (exitRequested) return;
                        break;
                    }
                    case 8: // Manage attachments submenu
                    {
                        ManageAttachments(wap, ref hasChanges, out exitRequested);
                        if (exitRequested) return;
                        break;
                    }
                    case 9: // Save & return
                    {
                        if (!hasChanges) goto default;

                        var copyFile = true;
                        if (wap.Name == wapFile.PrefabName)
                        {
                            Console.Write("Do you want to overwrite the prefab file? (y/n): ");
                            copyFile = char.ToLower(Console.ReadKey().KeyChar) != 'y';
                            Console.WriteLine();
                        }

                        string savePath;
                        if (copyFile)
                        {
                            var timestamp = new StringBuilder().Append(DateTime.Now.Year)
                                .Append("-")
                                .Append(DateTime.Now.DayOfYear)
                                .Append("-")
                                .Append(DateTime.Now.Hour)
                                .Append("-")
                                .Append(DateTime.Now.Second)
                                .Append("-")
                                .Append(DateTime.Now.Millisecond)
                                .ToString();
                            var newWapFile = new WapFile(wap.Name, timestamp);
                            savePath = Path.Combine(ArbsLocator.DataDirectory, newWapFile.ToFileName());
                        }
                        else
                        {
                            savePath = wapPath;
                        }

                        // Save changes
                        try
                        {
                            ArbsSerializer.Serialize(savePath, wap.Instance);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Failed to save prefab: {e.Message}");
                            throw;
                        }

                        // Copy preview image
                        if (copyFile)
                        {
                            var imageSourcePath = $"{wapPath}-IMEG.jpg";
                            if (File.Exists(imageSourcePath))
                                File.Copy(imageSourcePath, $"{savePath}-IMEG.jpg");
                            else
                                Console.WriteLine(
                                    "The preview image could not be found. Make sure to copy it manually."
                                );

                            Console.Write($"{wap.Name} was saved at {savePath} ...");
                        }
                        else
                        {
                            Console.Write($"{savePath} was overwritten...");
                        }

                        Console.ReadLine();

                        repeat = false;
                        break;
                    }
                    default: // Cancel / back
                        if (hasChanges)
                        {
                            Console.Write("Do you really want to discard the changes? (y/n): ");
                            var keepChanges = char.ToLower(Console.ReadKey().KeyChar) != 'y';
                            Console.WriteLine();

                            if (keepChanges) break;
                        }

                        repeat = false;
                        break;
                }
            }
        }

        private static void ChangeName(WeaponisedAnimalPrefab wap, ref bool hasChanges, out bool exitRequested)
        {
            exitRequested = false;

            var repeat = true;
            while (repeat)
            {
                Console.Write($"\nCurrent name: {wap.Name}\nNew name: ");
                var newName = Console.ReadLine()?.Trim();
                switch (newName?.ToLower())
                {
                    case "exit!":
                        exitRequested = true;
                        return;
                    case "back!":
                        repeat = false;
                        continue;
                }

                if (string.IsNullOrEmpty(newName))
                {
                    Console.WriteLine("No name specified.");
                }
                else if (newName == wap.Name)
                {
                    Console.WriteLine("The new name is the same as the old one.");
                    repeat = false;
                }
                else if (!newName.All(char.IsLetterOrDigit))
                {
                    Console.WriteLine("The new name is invalid. Only alphanumeric characters are allowed.");
                }
                else
                {
                    wap.Name = newName;
                    hasChanges = true;
                    repeat = false;
                }
            }
        }

        private static void ChangeTeam(WeaponisedAnimalPrefab wap, ref bool hasChanges, out bool exitRequested)
        {
            exitRequested = false;

            var repeat = true;
            while (repeat)
            {
                var result = CommandLine.ReadInt($"\nCurrent team: {wap.Team}\nNew Team: ", out var newTeam);
                switch (result)
                {
                    case -2: // exit
                        exitRequested = true;
                        return;
                    case -1: // back
                        repeat = false;
                        continue;
                }

                if (newTeam < 1 || newTeam > 7)
                {
                    Console.WriteLine("Please enter a team number between 1 and 7.");
                }
                else
                {
                    if (newTeam != wap.Team)
                    {
                        wap.Team = newTeam;
                        hasChanges = true;
                    }

                    repeat = false;
                }
            }
        }

        private static void ChangeAiMode(WeaponisedAnimalPrefab wap, ref bool hasChanges, out bool exitRequested)
        {
            exitRequested = false;

            var repeat = true;
            while (repeat)
            {
                Console.Write(
                    $"\nCurrent AI mode: {wap.AiMode}\nNew AI mode (Default, Idle, Guard, AI1, AI2, AI3): "
                );
                var newAiModeStr = Console.ReadLine()?.Trim().ToLower();

                WeaponisedAnimalPrefab.PrefabAiMode newAiMode;
                switch (newAiModeStr)
                {
                    case "exit":
                        exitRequested = true;
                        return;
                    case "back":
                        repeat = false;
                        continue;
                    case "default":
                    case "ai1":
                    case "1":
                        newAiMode = WeaponisedAnimalPrefab.PrefabAiMode.Default;
                        break;
                    case "idle":
                    case "ai2":
                    case "2":
                        newAiMode = WeaponisedAnimalPrefab.PrefabAiMode.Idle;
                        break;
                    case "guard":
                    case "ai3":
                    case "3":
                        newAiMode = WeaponisedAnimalPrefab.PrefabAiMode.Guard;
                        break;
                    default:
                        Console.WriteLine("Invalid ai mode.");
                        continue;
                }

                if (newAiMode != wap.AiMode)
                {
                    wap.AiMode = newAiMode;
                    hasChanges = true;
                }

                repeat = false;
            }
        }

        private static void ChangeDetectionDistance(
            WeaponisedAnimalPrefab wap,
            ref bool hasChanges,
            out bool exitRequested
        )
        {
            exitRequested = false;

            var result = CommandLine.ReadInt(
                $"\nCurrent detection distance: {wap.DetectionDistance}\nNew detection distance: ",
                out var newDetectionDistance
            );
            switch (result)
            {
                case -2: // exit
                    exitRequested = true;
                    return;
                case -1: // back
                    return;
            }

            if (newDetectionDistance == wap.DetectionDistance) return;
            wap.DetectionDistance = newDetectionDistance;
            hasChanges = true;
        }

        private static void ChangeHealthFactor(
            WeaponisedAnimalPrefab wap,
            ref bool hasChanges,
            out bool exitRequested
        )
        {
            exitRequested = false;

            var result = CommandLine.ReadDecimal(
                $"\nCurrent health factor: {wap.HealthFactor}\nNew health factor: ",
                out var newHealthFactor
            );
            switch (result)
            {
                case -2: // exit
                    exitRequested = true;
                    return;
                case -1: // back
                    return;
            }

            if (newHealthFactor == wap.HealthFactor) return;
            wap.HealthFactor = newHealthFactor;
            hasChanges = true;
        }

        private static void ChangeAttackFactor(
            WeaponisedAnimalPrefab wap,
            ref bool hasChanges,
            out bool exitRequested
        )
        {
            exitRequested = false;

            var result = CommandLine.ReadDecimal(
                $"\nCurrent attack factor: {wap.AttackFactor}\nNew attack factor: ",
                out var newAttackFactor
            );
            switch (result)
            {
                case -2: // exit
                    exitRequested = true;
                    return;
                case -1: // back
                    return;
            }

            if (newAttackFactor == wap.AttackFactor) return;
            wap.AttackFactor = newAttackFactor;
            hasChanges = true;
        }

        private static void ManageAttachments(
            WeaponisedAnimalPrefab wap,
            ref bool hasChanges,
            out bool exitRequested
        )
        {
            exitRequested = false;

            var attachments = wap.Attachments;
            if (attachments.Count == 0)
            {
                Console.Write($"{wap.Name} has no attachments...");
                Console.ReadLine();
                return;
            }

            var activeAttachments = attachments
                .Where(attachment => !attachment.Removed)
                .ToArray();

            var repeatAttachmentMenu = true;
            while (repeatAttachmentMenu)
            {
                var attachmentInfo = new StringBuilder("\n")
                    .Append(wap.Name)
                    .Append(" has ")
                    .Append(attachments.Count)
                    .Append(" attachment")
                    .Append(attachments.Count != 1 ? "s" : "")
                    .AppendLine(":");
                var indexLeftPad = attachments.Count.DigitCount();
                for (var index = 0; index < attachments.Count; index++)
                    attachmentInfo.Append(" (")
                        .Append((index + 1).ToString().PadLeft(indexLeftPad))
                        .Append(") ")
                        .Append(attachments[index])
                        .AppendLine();
                Console.Write(attachmentInfo.ToString());

                switch (CommandLine.NumericMenu("\nOptions:", AttachmentMenuOptions))
                {
                    case -2: // exit
                        exitRequested = true;
                        return;
                    case 0: // Change attack factor of a single attachment
                    {
                        if (activeAttachments.Length == 0)
                        {
                            Console.Write($"{wap.Name} has no active attachments...");
                            Console.ReadLine();
                            break;
                        }

                        var prompt = new StringBuilder("\n");

                        if (activeAttachments.Length == 1)
                        {
                            var singleAttachment = activeAttachments[0];

                            prompt.Append("Current attack factor of ")
                                .Append(singleAttachment.Name)
                                .Append(": ")
                                .Append(singleAttachment.AttackFactor)
                                .AppendLine()
                                .Append("New attack factor: ");

                            var result = CommandLine.ReadDecimal(
                                prompt.ToString(),
                                out var newAttackFactor
                            );
                            switch (result)
                            {
                                case -2: // exit
                                    exitRequested = true;
                                    return;
                                case -1: // back
                                    continue;
                            }

                            if (newAttackFactor != singleAttachment.AttackFactor)
                            {
                                singleAttachment.AttackFactor = newAttackFactor;
                                hasChanges = true;
                            }

                            break;
                        }

                        indexLeftPad = activeAttachments.Length.DigitCount();
                        for (var index = 0; index < activeAttachments.Length; index++)
                        {
                            var attachment = activeAttachments[index];
                            prompt.Append(" (")
                                .Append((index + 1).ToString().PadLeft(indexLeftPad))
                                .Append(") ")
                                .Append(attachment.Name)
                                .Append(": ")
                                .Append(attachment.AttackFactor)
                                .AppendLine("x attack");
                        }

                        prompt.AppendLine()
                            .Append("Attachment number followed by the new attack factor: ");

                        var repeat = true;
                        while (repeat)
                        {
                            var result = CommandLine.ReadIntAndDecimal(
                                prompt.ToString(),
                                out var selectedIndex,
                                out var newAttackFactor
                            );
                            switch (result)
                            {
                                case -2:
                                    exitRequested = true;
                                    return;
                                case -1:
                                    repeat = false;
                                    continue;
                            }

                            if (selectedIndex < 1 || selectedIndex > activeAttachments.Length)
                            {
                                Console.WriteLine("Please enter a valid attachment number.");
                                continue;
                            }

                            var attachment = activeAttachments[selectedIndex - 1];
                            if (newAttackFactor != attachment.AttackFactor)
                            {
                                attachment.AttackFactor = newAttackFactor;
                                hasChanges = true;
                            }

                            repeat = false;
                        }

                        break;
                    }
                    case 1: // Change attack factor of multiple attachments of the same type
                    {
                        if (activeAttachments.Length == 0)
                        {
                            Console.Write($"{wap.Name} has no active attachments...");
                            Console.ReadLine();
                            break;
                        }

                        var attachmentTypes = activeAttachments
                            .GroupBy(attachment => attachment.Id)
                            .ToArray();

                        var prompt = new StringBuilder("\n");

                        if (attachmentTypes.Length == 1)
                        {
                            var attachmentsOfSingleType = attachmentTypes[0].ToArray();

                            prompt.Append("New attack factor for all attachments of type ")
                                .Append(attachmentsOfSingleType[0].Name)
                                .Append(": ");

                            var result = CommandLine.ReadDecimal(
                                prompt.ToString(),
                                out var newAttackFactor
                            );
                            switch (result)
                            {
                                case -2: // exit
                                    exitRequested = true;
                                    return;
                                case -1: // back
                                    continue;
                            }

                            foreach (var attachment in attachmentsOfSingleType)
                            {
                                if (newAttackFactor == attachment.AttackFactor) continue;
                                attachment.AttackFactor = newAttackFactor;
                                hasChanges = true;
                            }

                            break;
                        }

                        indexLeftPad = activeAttachments.Length.DigitCount();
                        foreach (var attachmentType in attachmentTypes)
                            prompt.Append(" (")
                                .Append(attachmentType.Key.ToString().PadLeft(indexLeftPad))
                                .Append(") ")
                                .Append(attachmentType.First().Name)
                                .AppendLine();

                        prompt.AppendLine()
                            .Append("Type ID followed by the new attack factor: ");

                        var repeat = true;
                        while (repeat)
                        {
                            var result = CommandLine.ReadIntAndDecimal(
                                prompt.ToString(),
                                out var selectedAttachmentTypeId,
                                out var newAttackFactor
                            );
                            switch (result)
                            {
                                case -2:
                                    exitRequested = true;
                                    return;
                                case -1:
                                    repeat = false;
                                    continue;
                            }

                            var selectedAttachments = activeAttachments
                                .Where(attachment => selectedAttachmentTypeId == attachment.Id)
                                .ToArray();

                            if (selectedAttachments.Length == 0)
                            {
                                Console.WriteLine("Please enter a valid attachment type ID.");
                                continue;
                            }

                            foreach (var attachment in selectedAttachments)
                            {
                                if (newAttackFactor == attachment.AttackFactor) continue;
                                attachment.AttackFactor = newAttackFactor;
                                hasChanges = true;
                            }

                            repeat = false;
                        }

                        break;
                    }
                    case 2: // Change attack factor of all attachments
                    {
                        if (activeAttachments.Length == 0)
                        {
                            Console.Write($"{wap.Name} has no active attachments...");
                            Console.ReadLine();
                            break;
                        }

                        var result = CommandLine.ReadDecimal(
                            "\nNew attack factor for all attachments: ",
                            out var newAttackFactor
                        );
                        switch (result)
                        {
                            case -2: // exit
                                exitRequested = true;
                                return;
                            case -1: // back
                                continue;
                        }

                        foreach (var attachment in activeAttachments)
                        {
                            if (newAttackFactor == attachment.AttackFactor) continue;
                            attachment.AttackFactor = newAttackFactor;
                            hasChanges = true;
                        }

                        break;
                    }
                    case 3: // Clean up removed attachments
                        if (activeAttachments.Length == attachments.Count)
                        {
                            Console.Write($"{wap.Name} has no removed attachments...");
                            Console.ReadLine();
                            break;
                        }

                        Console.WriteLine(
                            "WARNING: THIS OPERATION IS UNSAFE AND MAY LEAD TO YOUR PREFAB BECOMING UNPLACEABLE!"
                        );
                        Console.Write("Do you still want to proceed? (y/n): ");
                        var cancel = char.ToLower(Console.ReadKey().KeyChar) != 'y';
                        Console.WriteLine();
                        if (cancel)
                        {
                            Console.Write("No attachments removed. Press any key to continue...");
                            Console.ReadLine();
                            break;
                        }

                        wap.CleanUpAttachments();
                        hasChanges = true;

                        // Recreate active attachments cache
                        activeAttachments = attachments.Where(attachment => !attachment.Removed).ToArray();

                        Console.Write("All removed attachments have been cleaned up...");
                        Console.ReadLine();

                        break;
                    default: // Return / back
                        repeatAttachmentMenu = false;
                        break;
                }
            }
        }
    }
}