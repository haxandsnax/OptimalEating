using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimalEating
{
    public class ModEntry : Mod
    {
        private const int MaxPlayerCount = 4;
        private PerScreen<StardewValley.Object> bestItem = new PerScreen<StardewValley.Object>();
        private PerScreen<List<StardewValley.Object>> bestItemCache = new PerScreen<List<StardewValley.Object>>();
        private PerScreen<int> lastItemIndex = new PerScreen<int>(() => -1);
        private PerScreen<KeybindList> myKeybinds = new PerScreen<KeybindList>();
        private long?[] connectedFarmers = new long?[4];
        private ModConfig config;
        private CommunityCenter _communityCenter;
        private Dictionary<string, string> _bundleData;
        private Dictionary<int, List<int>> itemsNeeded = new Dictionary<int, List<int>>();

        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<ModConfig>();
            helper.Events.Player.InventoryChanged += new EventHandler<InventoryChangedEventArgs>(CheckInventory);
            helper.Events.World.DebrisListChanged += new EventHandler<DebrisListChangedEventArgs>(CheckWorldItemPreferences);
            helper.Events.Input.ButtonsChanged += new EventHandler<ButtonsChangedEventArgs>(CheckHotkey);
            helper.Events.Display.RenderedHud += new EventHandler<RenderedHudEventArgs>(DisplayItemStats);
            helper.Events.GameLoop.SaveLoaded += new EventHandler<SaveLoadedEventArgs>(SetPlayerNumber);
            helper.Events.Multiplayer.PeerDisconnected += new EventHandler<PeerDisconnectedEventArgs>(ReleasePlayer);
            helper.Events.GameLoop.SaveLoaded += (sender, e) =>
            {
                _communityCenter = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;
                _bundleData = Game1.netWorldState.Value.BundleData;
            };
        }

        private void ReleasePlayer(object sender, PeerDisconnectedEventArgs e)
        {
            if (!Context.IsMainPlayer || !Context.IsSplitScreen)
                return;
            for (int index = 0; index < 4; ++index)
            {
                long? connectedFarmer = connectedFarmers[index];
                long playerId = e.Peer.PlayerID;
                if ((connectedFarmer.GetValueOrDefault() == playerId ? (connectedFarmer.HasValue ? 1 : 0) : 0) != 0)
                {
                    connectedFarmers = null;
                    break;
                }
            }
        }

        private void SetPlayerNumber(object sender, SaveLoadedEventArgs e)
        {
            ItemPreferences.Init(Game1.player);
            if (!Context.IsSplitScreen || Context.IsMainPlayer)
            {
                connectedFarmers[0] = new long?(Game1.player.UniqueMultiplayerID);
                myKeybinds.Value = config.Keybinds[0];
            }
            else
            {
                for (int index = 0; index < 4; ++index)
                {
                    if (!connectedFarmers[index].HasValue && config.Keybinds.Count >= index + 1)
                    {
                        connectedFarmers[index] = new long?(Game1.player.UniqueMultiplayerID);
                        myKeybinds.Value = config.Keybinds[index];
                        break;
                    }
                }
            }
        }

        private void CheckHotkey(object sender, ButtonsChangedEventArgs e)
        {
            if (myKeybinds.Value == null || Game1.player.IsBusyDoingSomething())
                return;
            KeybindList keybindList = myKeybinds.Value;
            Farmer player = Game1.player;
            if (!keybindList.JustPressed() || CheckFavorites(player, keybindList))
                return;
            if (bestItem.Value != null && keybindList.GetByType(KeybindType.CancelKeybind).IsPressed())
            {
                keybindList.SuppressType(Helper, KeybindType.CancelKeybind);
                RestoreLastItem(player);
            }
            else
            {
                if (keybindList.PressedByType(KeybindType.CycleFoodKeybind))
                {
                    keybindList.SuppressType(Helper, KeybindType.CycleFoodKeybind);
                    CheckBestItems(player);
                }

                if (keybindList.PressedByType(KeybindType.EatCurrentFoodKeybind))
                {
                    keybindList.SuppressType(Helper, KeybindType.EatCurrentFoodKeybind);
                    EatBestItem(player);
                }
            }
        }

        private void CheckInventory(object sender, InventoryChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !e.IsLocalPlayer)
                return;
            e.Removed.Where(i => ItemPreferences.FavoriteIndex(i, Game1.player) > -1).ToList<Item>().ForEach(i =>
            {
                if (Game1.player.CursorSlotItem == i)
                    return;
                ItemPreferences.Clear(i, e.Player);
            });
            if (bestItem.Value != null && e.Removed.Contains(bestItem.Value))
                RestoreLastItem(e.Player);
            bestItemCache.Value = GetBestItems(e.Player);
            CheckNeededBundleItems();
        }

        private void CheckWorldItemPreferences(object sender, DebrisListChangedEventArgs e)
        {
            e.Added.Where(i => ItemPreferences.FavoriteIndex(i.item, Game1.player) > -1).ToList().ForEach(i => ItemPreferences.Clear(i.item, Game1.player));
        }

        private bool CheckFavorites(Farmer player, KeybindList keybinds)
        {
            if (keybinds.GetByType(KeybindType.EatFavoriteFood1Keybind).IsPressed())
            {
                keybinds.SuppressType(Helper, KeybindType.EatFavoriteFood1Keybind);
                StardewValley.Object byIndex = ItemPreferences.GetByIndex(player, 0);
                if (byIndex != null)
                    EatItem(player, byIndex);
                return true;
            }
            if (keybinds.GetByType(KeybindType.EatFavoriteFood2Keybind).IsPressed())
            {
                keybinds.SuppressType(Helper, KeybindType.EatFavoriteFood2Keybind);
                StardewValley.Object byIndex = ItemPreferences.GetByIndex(player, 1);
                if (byIndex != null)
                    EatItem(player, byIndex);
                return true;
            }
            if (keybinds.GetByType(KeybindType.EatFavoriteFood3Keybind).IsPressed())
            {
                keybinds.SuppressType(Helper, KeybindType.EatFavoriteFood3Keybind);
                StardewValley.Object byIndex = ItemPreferences.GetByIndex(player, 2);
                if (byIndex != null)
                    EatItem(player, byIndex);
                return true;
            }
            if (keybinds.GetByType(KeybindType.EatFavoriteFood4Keybind).IsPressed())
            {
                keybinds.SuppressType(Helper, KeybindType.EatFavoriteFood4Keybind);
                StardewValley.Object byIndex = ItemPreferences.GetByIndex(player, 3);
                if (byIndex != null)
                    EatItem(player, byIndex);
                return true;
            }
            bool hasEatingValue = player.ActiveObject != null && (player.ActiveObject.staminaRecoveredOnConsumption() > 0 || player.ActiveObject.healthRecoveredOnConsumption() > 0);
            if (hasEatingValue && keybinds.GetByType(KeybindType.FavoriteFood1Keybind).IsPressed())
            {
                keybinds.SuppressType(Helper, KeybindType.FavoriteFood1Keybind);
                ItemPreferences.ToggleFavorite(player.ActiveObject, player, 0);
                return true;
            }
            if (hasEatingValue && keybinds.GetByType(KeybindType.FavoriteFood2Keybind).IsPressed())
            {
                keybinds.SuppressType(Helper, KeybindType.FavoriteFood2Keybind);
                ItemPreferences.ToggleFavorite(player.ActiveObject, player, 1);
                return true;
            }
            if (hasEatingValue && keybinds.GetByType(KeybindType.FavoriteFood3Keybind).IsPressed())
            {
                keybinds.SuppressType(Helper, KeybindType.FavoriteFood3Keybind);
                ItemPreferences.ToggleFavorite(player.ActiveObject, player, 2);
                return true;
            }
            if (hasEatingValue && keybinds.GetByType(KeybindType.FavoriteFood4Keybind).IsPressed())
            {
                keybinds.SuppressType(Helper, KeybindType.FavoriteFood4Keybind);
                ItemPreferences.ToggleFavorite(player.ActiveObject, player, 3);
                return true;
            }

            return false;
        }

        private bool NeededForBundle(StardewValley.Object item)
        {
            return itemsNeeded.Any(i => i.Key == item.ParentSheetIndex && i.Value.Any(qual => item.quality.Value >= qual));
        }

        // TODO: this is wild. Decompiler messed this up.
        private void CheckNeededBundleItems()
        {
            itemsNeeded = new Dictionary<int, List<int>>();
            foreach (KeyValuePair<string, string> keyValuePair in _bundleData)
            {
                string[] strArray1 = keyValuePair.Key.Split('/');
                int num;
                switch (strArray1[0])
                {
                    case "Abandoned Joja Mart":
                        num = 6;
                        break;
                    case "Boiler Room":
                        num = 3;
                        break;
                    case "Bulletin Board":
                        num = 5;
                        break;
                    case "Crafts Room":
                        num = 1;
                        break;
                    case "Fish Tank":
                        num = 2;
                        break;
                    case "Pantry":
                        num = 0;
                        break;
                    case "Vault":
                        num = 4;
                        break;
                    default:
                        continue;
                }
                if (_communityCenter.shouldNoteAppearInArea(num))
                {
                    int bundleIndex = strArray1[1].SafeParseInt32();
                    string[] strArray2 = keyValuePair.Value.Split('/');
                    string str = strArray2[0];
                    string[] strArray3 = strArray2[2].Split(' ');
                    for (int index = 0; index < strArray3.Length; index += 3)
                    {
                        int int32_2 = strArray3[index].SafeParseInt32();
                        int int32_3 = strArray3[index + 2].SafeParseInt32();
                        if (int32_2 != -1 && !_communityCenter.bundles[bundleIndex][index / 3])
                        {
                            if (!itemsNeeded.ContainsKey(int32_2))
                                itemsNeeded[int32_2] = new List<int>();
                            itemsNeeded[int32_2].Add(int32_3);
                        }
                    }
                }
            }
        }

        private void CheckBestItems(Farmer player)
        {
            if (bestItemCache.Value == null)
                bestItemCache.Value = GetBestItems(player);
            List<StardewValley.Object> bestItems = bestItemCache.Value;
            if (bestItems.Count == 0)
            {
                Game1.addHUDMessage(new HUDMessage("No food to eat", 3));
                return;
            }

            // If we're just starting to cycle through favorites,
            // store the index of the current item so we can get back to it
            if(bestItem.Value == null)
            {
                PreserveLastItem(player);
            }


            var currentObject = (StardewValley.Object)(player.CurrentItem is StardewValley.Object ? player.CurrentItem : null);
            if (bestItems.IndexOf(currentObject) == bestItems.Count - 1)
            {
                RestoreLastItem(player);
                return;
            }
            bestItem.Value = bestItem.Value == null ? bestItems.FirstOrDefault() : bestItems[(bestItems.IndexOf(currentObject) + 1) % bestItems.Count];
            if (bestItem.Value == null)
                return;
            player.CurrentToolIndex = player.Items.IndexOf(bestItem.Value);
        }

        private IEnumerable<StardewValley.Object> GetValidPlayerItems(Farmer player)
        {
            return player.Items.Where(i => i is StardewValley.Object).Select(i => i as StardewValley.Object);
        }

        private List<StardewValley.Object> GetBestItems(Farmer player)
        {
            double energyNeeded = player.MaxStamina - player.Stamina;
            double healthNeeded = (player.maxHealth - player.health);
            double energyWeight = Math.Pow(1.0 / (player.maxHealth - player.health + 1) * player.maxHealth, 2.0);
            double healthWeight = Math.Pow(1.0 / (player.maxHealth - player.health + 1) * player.maxHealth, 2.0);

            return GetValidPlayerItems(player).Where(i => (i.staminaRecoveredOnConsumption() > 0 || (i.healthRecoveredOnConsumption() > 0) && !NeededForBundle(i))).Where(i => !ItemPreferences.IsBanned(i, player, config)).OrderByDescending(i =>
            {
                int storePrice = i.sellToStorePrice(-1L);
                double energy = Math.Min(i.staminaRecoveredOnConsumption(), energyNeeded) * energyWeight;
                double health = Math.Min(i.healthRecoveredOnConsumption(), healthNeeded) * healthWeight;
                return storePrice == 0 ? 0.0 : (energy + health) / storePrice;
            }).ToList();
        }

        private void EatBestItem(Farmer player)
        {
            if (bestItem.Value == null || player.CurrentToolIndex != player.Items.IndexOf(bestItem.Value) || player.IsBusyDoingSomething())
                return;
            player.eatHeldObject();
            RestoreLastItem(player);
        }

        private void EatItem(Farmer player, StardewValley.Object item)
        {
            if (player.IsBusyDoingSomething())
                return;
            player.eatObject(item);
            if (item.Stack <= 1)
                player.removeItemFromInventory(item);
            else
                player.Items[player.Items.IndexOf(item)].Stack -= 1;
        }

        private void PreserveLastItem(Farmer player)
        {
            lastItemIndex.Value = player.CurrentToolIndex;
        }

        private void RestoreLastItem(Farmer player)
        {
            bestItem.Value = null;
            if (lastItemIndex.Value <= -1)
                return;
            player.CurrentToolIndex = lastItemIndex.Value;
            lastItemIndex.Value = -1;
        }

        private void DisplayItemStats(object sender, RenderedHudEventArgs e)
        {
            Farmer player = Game1.player;
            if (config.DisableText || bestItem.Value == null || player.CurrentToolIndex != player.Items.IndexOf(bestItem.Value))
                return;
            ItemPreferences.FavoriteIndex(bestItem.Value, player);
            Vector2 leftPos = Utility.ModifyCoordinatesForUIScale(player.getLocalPosition(Game1.viewport));
            leftPos.Y -= Utility.ModifyCoordinateForUIScale(150f);
            Extensions.DrawNumber(Math.Min(player.maxHealth - player.health, bestItem.Value.healthRecoveredOnConsumption()), leftPos);
            leftPos.X -= Utility.ModifyCoordinateForUIScale(21f);
            Game1.spriteBatch.Draw(Game1.mouseCursors, leftPos, new Rectangle?(new Rectangle(0, 438, 10, 10)), Color.White, 0.0f, Vector2.Zero, Utility.ModifyCoordinateForUIScale(2f), SpriteEffects.None, 0.01f);
            leftPos.X += Utility.ModifyCoordinateForUIScale(21f);
            leftPos.Y -= Utility.ModifyCoordinateForUIScale(21f);
            Extensions.DrawNumber(Math.Min(player.maxStamina.Value - (int)player.stamina, bestItem.Value.staminaRecoveredOnConsumption()), leftPos);
            leftPos.X -= Utility.ModifyCoordinateForUIScale(21f);
            Game1.spriteBatch.Draw(Game1.mouseCursors, leftPos, new Rectangle?(new Rectangle(0, 428, 10, 10)), Color.White, 0.0f, Vector2.Zero, Utility.ModifyCoordinateForUIScale(2f), SpriteEffects.None, 0.01f);
            int num = ItemPreferences.FavoriteIndex((Item)bestItem.Value, player);
            if (num <= -1)
                return;
            leftPos.Y -= Utility.ModifyCoordinateForUIScale(22f);
            Game1.spriteBatch.Draw(Game1.mouseCursors, leftPos, new Rectangle?(new Rectangle(211, 428, 7, 6)), Color.White, 0.0f, Vector2.Zero, Utility.ModifyCoordinateForUIScale(3f),SpriteEffects.None, 0.01f);
            leftPos.X += Utility.ModifyCoordinateForUIScale(21f);
            Extensions.DrawNumber(num + 1, leftPos);
        }

        private enum HotkeyAction
        {
            CycleFood,
            Eat,
            ForceEat,
            Cancel,
        }
    }
}
