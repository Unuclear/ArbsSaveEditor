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
        private static readonly string[] AttachmentMenuOptions =
        {
            "Change attack factor of a single attachment",
            "Change attack factor of all attachments of the same type",
            "Change attack factor of all attachments",
            "Clean up removed attachments",
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
            {
                var wapFiles = ArbsLocator.GetAllWeaponisedAnimalPrefabFiles();
                if (wapFiles.Length == 0)
                {
                    Console.WriteLine("No weaponised animal prefabs were found.");
                    return 0;
                }

                WapFile selected;
                switch (CommandLine.NumericMenu("Select a prefab:", wapFiles))
                {
                    case -2: // exit
                        return 0;
                    case -1: // back
                        Console.WriteLine();
                        continue;
                    case int choice:
                        selected = wapFiles[choice];
                        Console.WriteLine($" => Selected {selected.PrefabName}");
                        break;
                }

                var selectedPath = Path.Combine(dataDirectory, selected.ToFileName());
                WeaponisedAnimalPrefab prefab;
                try
                {
                    prefab = new WeaponisedAnimalPrefab(ArbsSerializer.Deserialize(
                        selectedPath,
                        "WeaponisedAnimalPrefab"
                    ));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return 6;
                }

                var hasChanges = false;

                var repeatEditMenu = true;
                while (repeatEditMenu)
                {
                    var editMenuTitle = new StringBuilder("\n")
                        .Append(prefab.Name)
                        .Append(" (")
                        .Append(selected.Timestamp)
                        .Append(")")
                        .Append(hasChanges ? " *" : "")
                        .ToString();

                    string[] editMenuOptions =
                    {
                        "Show all attributes",
                        "Print debug information",
                        $"Change name [{prefab.Name}]",
                        $"Change team [{prefab.Team}]",
                        $"Change AI mode [{prefab.AiMode}]",
                        $"Change detection distance [{prefab.DetectionDistance}]",
                        $"Change health factor [{prefab.HealthFactor}]",
                        $"Change attack factor [{prefab.AttackFactor}]",
                        $"Manage attachments [{prefab.Attachments.Count}]",
                        "Save & return",
                        "Cancel"
                    };

                    switch (CommandLine.NumericMenu(editMenuTitle, editMenuOptions))
                    {
                        case -2: // exit
                            return 0;
                        case 0: // Show all attributes
                            Console.WriteLine($"\n{prefab}");

                            Console.Write("\nPress enter to return...");
                            Console.ReadLine();

                            break;
                        case 1: // Print debug information
                            Console.WriteLine($"\n{prefab.ToDebugString()}");

                            Console.Write("\nPress enter to return...");
                            Console.ReadLine();

                            break;
                        case 2: // Change name
                        {
                            var repeat = true;
                            while (repeat)
                            {
                                Console.Write($"\nCurrent Name: {prefab.Name}\nNew name: ");
                                var newName = Console.ReadLine()?.Trim();
                                switch (newName?.ToLower())
                                {
                                    case "exit!":
                                        return 0;
                                    case "back!":
                                        repeat = false;
                                        continue;
                                }

                                if (string.IsNullOrEmpty(newName))
                                {
                                    Console.WriteLine("No name specified.");
                                }
                                else if (newName == prefab.Name)
                                {
                                    Console.WriteLine("The new name is the same as the old one.");
                                    repeat = false;
                                }
                                else if (!newName.All(char.IsLetterOrDigit))
                                {
                                    Console.WriteLine(
                                        "The new name is invalid. Only alphanumeric characters are allowed."
                                    );
                                }
                                else
                                {
                                    prefab.Name = newName;
                                    hasChanges = true;
                                    repeat = false;
                                }
                            }

                            break;
                        }
                        case 3: // Change team
                        {
                            var repeat = true;
                            while (repeat)
                            {
                                var result = CommandLine.ReadInt(
                                    $"\nCurrent Team: {prefab.Team}\nNew Team: ",
                                    out var newTeam
                                );
                                switch (result)
                                {
                                    case -2: // exit
                                        return 0;
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
                                    if (newTeam != prefab.Team)
                                    {
                                        prefab.Team = newTeam;
                                        hasChanges = true;
                                    }

                                    repeat = false;
                                }
                            }

                            break;
                        }
                        case 4: // Change AI mode
                        {
                            var repeat = true;
                            while (repeat)
                            {
                                Console.Write(
                                    $"\nCurrent AI mode: {prefab.AiMode}\nNew AI mode (Default, Idle, Guard, AI1, AI2, AI3): "
                                );
                                var newAiModeStr = Console.ReadLine()?.Trim().ToLower();

                                WeaponisedAnimalPrefab.PrefabAiMode newAiMode;
                                switch (newAiModeStr)
                                {
                                    case "exit":
                                        return 0;
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

                                if (newAiMode != prefab.AiMode)
                                {
                                    prefab.AiMode = newAiMode;
                                    hasChanges = true;
                                }

                                repeat = false;
                            }

                            break;
                        }
                        case 5: // Change detection distance
                        {
                            var result = CommandLine.ReadInt(
                                $"\nCurrent detection distance: {prefab.DetectionDistance}\nNew detection distance: ",
                                out var newDetectionDistance
                            );
                            switch (result)
                            {
                                case -2: // exit
                                    return 0;
                                case -1: // back
                                    continue;
                            }

                            if (newDetectionDistance != prefab.DetectionDistance)
                            {
                                prefab.DetectionDistance = newDetectionDistance;
                                hasChanges = true;
                            }

                            break;
                        }
                        case 6: // Change health factor
                        {
                            var result = CommandLine.ReadDecimal(
                                $"\nCurrent health factor: {prefab.HealthFactor}\nNew health factor: ",
                                out var newHealthFactor
                            );
                            switch (result)
                            {
                                case -2: // exit
                                    return 0;
                                case -1: // back
                                    continue;
                            }

                            if (newHealthFactor != prefab.HealthFactor)
                            {
                                prefab.HealthFactor = newHealthFactor;
                                hasChanges = true;
                            }

                            break;
                        }
                        case 7: // Change attack factor
                        {
                            var result = CommandLine.ReadDecimal(
                                $"\nCurrent attack factor: {prefab.AttackFactor}\nNew attack factor: ",
                                out var newAttackFactor
                            );
                            switch (result)
                            {
                                case -2: // exit
                                    return 0;
                                case -1: // back
                                    continue;
                            }

                            if (newAttackFactor != prefab.AttackFactor)
                            {
                                prefab.AttackFactor = newAttackFactor;
                                hasChanges = true;
                            }

                            break;
                        }
                        case 8: // Manage attachments submenu
                        {
                            var attachments = prefab.Attachments;
                            if (attachments.Count == 0)
                            {
                                Console.Write($"{prefab.Name} has no attachments...");
                                Console.ReadLine();
                                break;
                            }

                            var activeAttachments = attachments
                                .Where(attachment => !attachment.Removed)
                                .ToArray();

                            var repeatAttachmentMenu = true;
                            while (repeatAttachmentMenu)
                            {
                                var attachmentInfo = new StringBuilder("\n")
                                    .Append(prefab.Name)
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
                                        return 0;
                                    case 0: // Change attack factor of a single attachment
                                    {
                                        if (activeAttachments.Length == 0)
                                        {
                                            Console.Write($"{prefab.Name} has no active attachments...");
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
                                                    return 0;
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
                                                    return 0;
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
                                            Console.Write($"{prefab.Name} has no active attachments...");
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
                                                    return 0;
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
                                                    return 0;
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
                                            Console.Write($"{prefab.Name} has no active attachments...");
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
                                                return 0;
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
                                            Console.Write($"{prefab.Name} has no removed attachments...");
                                            Console.ReadLine();
                                            break;
                                        }

                                        prefab.CleanUpAttachments();
                                        hasChanges = true;

                                        // Recreate active attachments cache
                                        activeAttachments = attachments
                                            .Where(attachment => !attachment.Removed)
                                            .ToArray();

                                        Console.Write("All removed attachments have been cleaned up...");
                                        Console.ReadLine();

                                        break;
                                    default: // Cancel / back
                                        repeatAttachmentMenu = false;
                                        break;
                                }
                            }

                            break;
                        }
                        case 9: // Save & return
                        {
                            if (!hasChanges) goto default;

                            var copyFile = true;
                            if (prefab.Name == selected.PrefabName)
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
                                var saveFile = new WapFile(prefab.Name, timestamp);
                                savePath = Path.Combine(ArbsLocator.DataDirectory, saveFile.ToFileName());
                            }
                            else
                            {
                                savePath = selectedPath;
                            }

                            // Save changes
                            try
                            {
                                ArbsSerializer.Serialize(savePath, prefab.PrefabObject);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Failed to save prefab: {e.Message}");
                                throw;
                            }

                            // Copy preview image
                            if (copyFile)
                            {
                                var imageSourcePath = $"{selectedPath}-IMEG.jpg";
                                if (File.Exists(imageSourcePath))
                                    File.Copy(imageSourcePath, $"{savePath}-IMEG.jpg");
                                else
                                    Console.WriteLine(
                                        "The preview image could not be found. Make sure to copy it manually."
                                    );

                                Console.Write($"{prefab.Name} was saved at {savePath} ...");
                            }
                            else
                            {
                                Console.Write($"{savePath} was overwritten...");
                            }

                            Console.ReadLine();

                            repeatEditMenu = false;
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

                            repeatEditMenu = false;
                            break;
                    }
                }

                Console.WriteLine();
            }
        }
    }
}