// Decompiled with JetBrains decompiler
// Type: OptimalEating.ItemPreferences
// Assembly: OptimalEating, Version=1.3.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 460F38FC-2B2E-41CC-8304-455D2E096437
// Assembly location: C:\Users\clint\OneDrive\Games\StardewValley\clintmods\OptimalEating\OptimalEating.dll

using Netcode;
using StardewValley;
using StardewValley.Mods;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimalEating
{
    internal class ItemPreferences
    {
        public static string favoriteString = "haxandsnax.OptimalEating:favorite";

        public static void Init(Farmer player)
        {
            Guid guid;
            if (!player.modData.ContainsKey($"{favoriteString}:1"))
            {
                player.modData[$"{favoriteString}:1"] = Guid.NewGuid().ToString();
            }
            if (!player.modData.ContainsKey($"{favoriteString}:2"))
            {
                player.modData[$"{favoriteString}:2"] = Guid.NewGuid().ToString();
            }
            if (!player.modData.ContainsKey($"{favoriteString}:3"))
            {
                player.modData[$"{favoriteString}:3"] = Guid.NewGuid().ToString();
            }
            if (!player.modData.ContainsKey($"{favoriteString}:4"))
            {
                player.modData[$"{favoriteString}:4"] = Guid.NewGuid().ToString();
            }
        }

        public static int GetIndexBySlotID(Farmer player, string slotID)
        {
            return new List<string>()
            {
              player.modData[$"{favoriteString}:1"],
              player.modData[$"{favoriteString}:2"],
              player.modData[$"{favoriteString}:3"],
              player.modData[$"{favoriteString}:4"]
            }.IndexOf(slotID);
        }

        public static int FavoriteIndex(Item item, Farmer player)
        {
            if (item == null || !item.modData.ContainsKey($"{favoriteString}:{player.UniqueMultiplayerID}"))
                return -1;
            int indexBySlotId = GetIndexBySlotID(player, item.modData[$"{favoriteString}:{player.UniqueMultiplayerID}"]);
            if (indexBySlotId < 0)
                item.modData.Remove($"{favoriteString}:{player.UniqueMultiplayerID}");
            return indexBySlotId;
        }

        public static string GetSlotID(Farmer player, int index)
        {
            return player.modData[$"{favoriteString}:{index + 1}"];
        }

        public static bool ToggleFavorite(Item item, Farmer player, int index)
        {
            if (FavoriteIndex(item, player) == index)
            {
                item.modData.Remove($"{favoriteString}:{player.UniqueMultiplayerID}");
                return false;
            }
            GetByIndex(player, index)?.modData.Remove($"{favoriteString}:{player.UniqueMultiplayerID}");
            item.modData[$"{favoriteString}:{player.UniqueMultiplayerID}"] = GetSlotID(player, index);
            return true;
        }

        public static StardewValley.Object GetByIndex(Farmer player, int index)
        {
            string slotID = GetSlotID(player, index);
            string str;
            return (StardewValley.Object)player.Items.Where((i => i != null)).FirstOrDefault((i => i.modData.TryGetValue($"{favoriteString}:{player.UniqueMultiplayerID}", out str) && str == slotID));
        }

        public static void Clear(Item item, Farmer player)
        {
            item.modData.Remove($"{favoriteString}:{player.UniqueMultiplayerID}");
        }

        public static bool IsBanned(Item item, Farmer player, ModConfig config)
        {
            return config.BannedItemIDs.Contains(item.ParentSheetIndex) || config.BannedItemsByName.Contains((item.DisplayName));
        }
    }
}
