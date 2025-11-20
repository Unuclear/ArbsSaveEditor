using System;
using System.Collections.Generic;
using System.Text;

namespace AseCore.ReflectionWrappers
{
    public class SaveFile : ReflectedObject
    {
        private static readonly int[] UnlockableSkins = { 164, 166, 167, 170, 171, 172 };

        private int _mazeCoins;

        public SaveFile(object instance) : base(CheckIfInstanceIsSaveFile(instance))
        {
            _mazeCoins = (int)Get("mUserTotalMazeCoin");
            UnlockedSkins = (List<int>)Get("mNotFreeItems");
            UnlockedSkins.Sort();
        }

        public int MazeCoins
        {
            get => _mazeCoins;
            set
            {
                Set("mUserTotalMazeCoin", value);
                _mazeCoins = value;
            }
        }

        public List<int> UnlockedSkins { get; }

        public bool UnlockAllSkins()
        {
            var hasUnlockedSkins = false;

            foreach (var skinId in UnlockableSkins)
            {
                if (UnlockedSkins.Contains(skinId)) continue;
                UnlockedSkins.Add(skinId);
                hasUnlockedSkins = true;
            }

            if (!hasUnlockedSkins) return false;
            UnlockedSkins.Sort();
            return true;
        }

        public void Save()
        {
            try
            {
                ArbsSerializer.Serialize(ArbsLocator.SaveFile, Instance);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to write user save: {e.Message}");
                throw;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder("User save:")
                .AppendLine()
                .Append("  Maze coins: ")
                .Append(_mazeCoins)
                .AppendLine()
                .Append("  Unlocked skins:");

            if (UnlockedSkins.Count == 0) return sb.Append(" none").ToString();

            foreach (var skinId in UnlockedSkins)
                sb.AppendLine()
                    .Append("   - ")
                    .Append(WeaponisedAnimalPrefab.GetUnitName(skinId))
                    .Append(" (")
                    .Append(skinId)
                    .Append(")");

            return sb.ToString();
        }

        public string ToDebugString()
        {
            var sb = new StringBuilder(Type.FullName)
                .AppendLine(" {")
                .Append("  mUserTotalMazeCoin: ")
                .Append(_mazeCoins)
                .AppendLine(",")
                .Append("  mNotFreeItems: List<int> [");
            foreach (var skinId in UnlockedSkins)
                sb.Append("    ")
                    .Append(skinId)
                    .AppendLine(",");
            return sb.AppendLine("  ],")
                .Append("}")
                .ToString();
        }

        private static object CheckIfInstanceIsSaveFile(object instance)
        {
            return instance.GetType().Name == "SaveFile"
                ? instance
                : throw new ArgumentException("Instance is not a SaveFile!");
        }
    }
}