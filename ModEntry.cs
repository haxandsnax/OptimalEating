using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace OptimalEating
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        const int MaxPlayerCount = 4;
        private PerScreen<StardewValley.Object> bestItem = new PerScreen<StardewValley.Object>();
        private PerScreen<int> lastItemIndex = new PerScreen<int>(() => -1);
        private PerScreen<KeybindList> myKeybinds = new PerScreen<KeybindList>();
        private long?[] connectedFarmers = new long?[MaxPlayerCount];
        private ModConfig config;

        private CommunityCenter _communityCenter;
        private Dictionary<string, string> _bundleData;
        private Dictionary<int, List<int>> itemsNeeded = new Dictionary<int, List<int>>();

        private enum HotkeyAction
        {
            CycleFood,
            Eat,
            ForceEat,
            Cancel
        };

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            config = helper.ReadConfig<ModConfig>();
            helper.Events.Player.InventoryChanged += CheckInventory;
            helper.Events.Input.ButtonsChanged += CheckHotkey;
            helper.Events.Display.RenderedHud += CheckBestItemHeld;
            helper.Events.GameLoop.SaveLoaded += SetPlayerNumber;
            helper.Events.Multiplayer.PeerDisconnected += ReleasePlayer;
            helper.Events.GameLoop.SaveLoaded += (object sender, SaveLoadedEventArgs e) =>
            {
                _communityCenter = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;
                _bundleData = Game1.netWorldState.Value.BundleData;

            };
        }

        private void ReleasePlayer(object sender, PeerDisconnectedEventArgs e)
        {
            if (!Context.IsMainPlayer || !Context.IsSplitScreen)
            {
                return;
            }
            // clear player from keybind / player slot
            for (int i = 0; i < MaxPlayerCount; ++i)
            {
                if (connectedFarmers[i] == e.Peer.PlayerID)
                {
                    connectedFarmers = null;
                    return;
                }
            }
        }

        private void SetPlayerNumber(object sender, SaveLoadedEventArgs e)
        {
            if (!Context.IsSplitScreen || Context.IsMainPlayer)
            {
                connectedFarmers[0] = Game1.player.UniqueMultiplayerID;
                myKeybinds.Value = config.Keybinds[0];
                return;
            }

            // find next available keybind / player slot
            for (int i = 0; i < MaxPlayerCount; ++i) {
                if(connectedFarmers[i] == null)
                {
                    if(config.Keybinds.Count < i+1)
                    {
                        
                        continue;
                    }
                    connectedFarmers[i] = Game1.player.UniqueMultiplayerID;
                    myKeybinds.Value = config.Keybinds[i];
                    return;
                }
            }
        }

        /*********
        ** Private methods
        *********/

        /// <summary>Raised when button is pressed</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>

        private void CheckHotkey(object sender, ButtonsChangedEventArgs e)
        {
            if(myKeybinds.Value == null)
            {
                return;
            }
            KeybindList keybinds = myKeybinds.Value;
            Farmer player = Game1.player;
            if(!keybinds.JustPressed())
            {
                return;
            }

            if (bestItem.Value != null && keybinds.GetByType(KeybindType.CancelKeybind).IsPressed())
            {
                keybinds.SuppressType(Helper, KeybindType.CancelKeybind);
                RestoreLastItem(player);
                return;
            }

            if (bestItem.Value != null && keybinds.PressedByType(KeybindType.ForceEatFoodKeybind))
            {
                keybinds.SuppressType(Helper, KeybindType.ForceEatFoodKeybind);
                EatBestItem(player);
                return;
            }

            if (keybinds.PressedByType(KeybindType.CycleFoodKeybind))
            {
                keybinds.SuppressType(Helper, KeybindType.CycleFoodKeybind);
                CheckBestItems(player);
            }

            if (bestItem != null && (keybinds.PressedByType(KeybindType.ForceEatFoodKeybind) || keybinds.PressedByType(KeybindType.EatCurrentFoodKeybind)))
            {
                keybinds.SuppressType(Helper, KeybindType.ForceEatFoodKeybind);
                keybinds.SuppressType(Helper, KeybindType.EatCurrentFoodKeybind);
                EatBestItem(player);
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void CheckInventory(object sender, InventoryChangedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet or it's not the local player
            if (!Context.IsWorldReady || !e.IsLocalPlayer)
                return;
            if (bestItem.Value != null && !e.Player.Items.Contains(bestItem.Value))
            {
                RestoreLastItem(e.Player);
            }
        }

        private void CheckBestItems(Farmer player)
        {
            List<StardewValley.Object> items = GetBestItems(player);
            if(items.Count == 0)
            {
                Game1.addHUDMessage(new HUDMessage("No food to eat", 3));
                return;
            }
            else if(player.Stamina == player.MaxStamina)
            {
                Game1.addHUDMessage(new HUDMessage("Energy at max", 4));
                return;
            }

            StardewValley.Object heldItem = (StardewValley.Object)(player.Items[player.CurrentToolIndex] is StardewValley.Object ? player.Items[player.CurrentToolIndex] : null);
            if (!items.Contains(heldItem))
            {
                // store current toolbar item to swap back after eating
                PreserveLastItem(player);
            }
            else if (items.IndexOf(heldItem) == items.Count - 1)
            {
                RestoreLastItem(player);
                return;
            }
            
            // cycle to the next best item
            bestItem.Value = bestItem.Value == null ? items.FirstOrDefault() : items[(items.IndexOf(heldItem) + 1) % items.Count];

            if(bestItem.Value != null)
            {
                player.CurrentToolIndex = player.Items.IndexOf(bestItem.Value);
            }
        }

        private IEnumerable<StardewValley.Object> GetValidPlayerItems(Farmer player)
        {
            return player.Items
                .Where(i => i is StardewValley.Object)
                .Select(i => i as StardewValley.Object);
        }

        private List<StardewValley.Object> GetBestItems(Farmer player)
        {
            float energyNeeded = player.MaxStamina - player.Stamina;

            return GetValidPlayerItems(player)
                .Where(i => i.staminaRecoveredOnConsumption() > 0)
                .OrderByDescending(i =>
                {
                    int price = i.sellToStorePrice();
                    if (price == 0)
                    {
                        return 0;
                    }
                    return Math.Min(i.staminaRecoveredOnConsumption(), energyNeeded) / price;
                }).ToList();
        }

        private void EatBestItem(Farmer player)
        {
            if(bestItem.Value != null && player.CurrentToolIndex == player.Items.IndexOf(bestItem.Value) && !player.IsBusyDoingSomething())
            {
                player.eatHeldObject();
                player.reduceActiveItemByOne();
                RestoreLastItem(player);
            }
        }

        private void PreserveLastItem(Farmer player)
        {
            lastItemIndex.Value = player.CurrentToolIndex;
        }

        private void RestoreLastItem(Farmer player)
        {
            bestItem.Value = null;
            if(lastItemIndex.Value > -1)
            {
                player.CurrentToolIndex = lastItemIndex.Value;
                lastItemIndex.Value = -1;
                return;
            }
        }


        private void CheckBestItemHeld(object sender, RenderedHudEventArgs e)
        {
            Farmer player = Game1.player;
            if(!config.DisableText && bestItem.Value != null && player.CurrentToolIndex == player.Items.IndexOf(bestItem.Value))
            {
                string energyText = $"Energy: {bestItem.Value.staminaRecoveredOnConsumption()}";
                float stringWidth = Game1.tinyFont.MeasureString(energyText).X;
                Vector2 pos = player.getLocalPosition(Game1.viewport);
                pos.Y -= 100;
                pos.X -= stringWidth / 2;
                Utility.drawBoldText(
                        Game1.spriteBatch,
                        energyText,
                        Game1.tinyFont,
                        Utility.ModifyCoordinatesForUIScale(pos),
                        Color.Black);
            }
        }
    }
}