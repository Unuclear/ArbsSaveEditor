using System;
using System.Text;
using AseCore.Extensions;

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

        public static int NumericMenu<T>(string title, T[] options)
        {
            var optionsText = new StringBuilder(title)
                .AppendLine();
            var indexLeftPad = options.Length.DigitCount();
            for (var index = 0; index < options.Length; index++)
                optionsText.Append(" ")
                    .Append((index + 1).ToString().PadLeft(indexLeftPad))
                    .Append(". ")
                    .Append(options[index])
                    .AppendLine();
            Console.Write(optionsText.AppendLine().ToString());

            while (true)
            {
                Console.Write("Enter a menu number: ");
                var userInput = Console.ReadLine()?.Trim().ToLower();

                switch (userInput)
                {
                    case "exit":
                        return -2;
                    case "back":
                        return -1;
                    case "help":
                        Console.WriteLine("Type in a number corresponding to a menu entry above to traverse the menu, \"back\" to leave menu and \"exit\" to exit the program.");
                        continue;
                }

                if (int.TryParse(userInput, out var choice) && choice >= 1 && choice <= options.Length)
                    return choice - 1;

                Console.WriteLine("Invalid choice, try again or type \"help\" to list available options.");
            }
        }

        public static int ReadInt(string prompt, out int value)
        {
            value = 0;

            while (true)
            {
                Console.Write(prompt);
                var userInput = Console.ReadLine()?.Trim().ToLower();

                switch (userInput)
                {
                    case "exit":
                        return -2;
                    case "back":
                        return -1;
                }

                if (int.TryParse(userInput, out value)) return 0;

                Console.WriteLine("Please enter an integer.");
            }
        }

        public static int ReadDecimal(string prompt, out decimal value)
        {
            value = 0;

            while (true)
            {
                Console.Write(prompt);
                var userInput = Console.ReadLine()?.Trim().ToLower();

                switch (userInput)
                {
                    case "exit":
                        return -2;
                    case "back":
                        return -1;
                }

                if (decimal.TryParse(userInput, out value)) return 0;

                Console.WriteLine("Please enter a number.");
            }
        }

        public static int ReadIntAndDecimal(string prompt, out int intValue, out decimal decimalValue)
        {
            intValue = 0;
            decimalValue = 0;

            while (true)
            {
                Console.Write(prompt);
                var userInput = Console.ReadLine()?.Trim().ToLower();

                switch (userInput)
                {
                    case "exit":
                        return -2;
                    case "back":
                        return -1;
                }

                var values = userInput?.Split(' ');
                if (values == null || values.Length == 0) continue;

                if (values.Length == 2 &&
                    int.TryParse(values[0], out intValue) &&
                    decimal.TryParse(values[1], out decimalValue))
                    return 0;

                Console.WriteLine("Please enter an integer followed by a number.");
            }
        }
    }
}