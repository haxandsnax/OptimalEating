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
        private List<StardewValley.Object> bestItems = new List<StardewValley.Object>();
        private StardewValley.Object bestItem = null;
        private int lastItemIndex = -1;
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
            helper.Events.Input.ButtonPressed += CheckHotkey;
            helper.Events.Display.RenderedHud += CheckBestItemHeld;
            helper.Events.GameLoop.SaveLoaded += (object sender, SaveLoadedEventArgs e) =>
            {
                _communityCenter = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;
                _bundleData = Game1.content.Load<Dictionary<String, String>>("Data\\Bundles");

            };
        }



        /*********
        ** Private methods
        *********/

        /// <summary>Raised when button is pressed</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>

        private void CheckHotkey(object sender, ButtonPressedEventArgs e)
        {
            if (config.CancelKeybind.IsPressed())
            {
                RestoreLastItem();
                return;
            }

            if (bestItem != null && config.ForceEatFoodKeybind.IsPressed())
            {
                EatBestItem();
                return;
            }

            if (config.CycleFoodKeybind.IsPressed())
            {
                CheckBestItems();
            }

            if (config.EatCurrentFoodKeybind.IsPressed() || config.ForceEatFoodKeybind.IsPressed())
            {
                EatBestItem();
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


            if (bestItem != null && !Game1.player.Items.Contains(bestItem))
            {
                RestoreLastItem();
            }

            CheckNeededBundleItems();
        }

        private bool NeededForBundle(StardewValley.Object item)
        {
            return itemsNeeded.Any(i => i.Key == item.ParentSheetIndex && i.Value.Any(qual => item.quality >= qual));
        }

        private void CheckNeededBundleItems()
        {
            // Bundle checks adapted from UI Info Suite
            // https://github.com/Annosz/UIInfoSuite2/blob/master/UIInfoSuite2/UIElements/ShowItemHoverInformation.cs
            itemsNeeded = new Dictionary<int, List<int>>();
            foreach (var bundle in _bundleData)
            {
                string[] bundleRoomInfo = bundle.Key.Split('/');
                string bundleRoom = bundleRoomInfo[0];
                int roomNum;

                switch (bundleRoom)
                {
                    case "Pantry": roomNum = 0; break;
                    case "Crafts Room": roomNum = 1; break;
                    case "Fish Tank": roomNum = 2; break;
                    case "Boiler Room": roomNum = 3; break;
                    case "Vault": roomNum = 4; break;
                    case "Bulletin Board": roomNum = 5; break;
                    case "Abandoned Joja Mart": roomNum = 6; break;
                    default: continue;
                }

                if (_communityCenter.shouldNoteAppearInArea(roomNum))
                {
                    int bundleNumber = bundleRoomInfo[1].SafeParseInt32();
                    string[] bundleInfo = bundle.Value.Split('/');
                    string bundleName = bundleInfo[0];
                    string[] bundleValues = bundleInfo[2].Split(' ');
                    for (int i = 0; i < bundleValues.Length; i += 3)
                    {
                        int bundleValue = bundleValues[i].SafeParseInt32();
                        int quality = bundleValues[i + 2].SafeParseInt32();

                        if (bundleValue != -1 && !_communityCenter.bundles[bundleNumber][i / 3])
                        {
                            if (!itemsNeeded.ContainsKey(bundleValue))
                            {
                                itemsNeeded[bundleValue] = new List<int>();
                            }
                            itemsNeeded[bundleValue].Add(quality);
                        }
                    }
                }
            }
        }


        private void CheckBestItems()
        {
            List<StardewValley.Object> items = GetBestItems();
            if(items.Count == 0)
            {
                Game1.addHUDMessage(new HUDMessage("No food to eat", 3));
                return;
            }
            else if(Game1.player.Stamina == Game1.player.MaxStamina)
            {
                Game1.addHUDMessage(new HUDMessage("Energy at max", 4));
                return;
            }

            StardewValley.Object heldItem = (StardewValley.Object)(Game1.player.Items[Game1.player.CurrentToolIndex] is StardewValley.Object ? Game1.player.Items[Game1.player.CurrentToolIndex] : null);
            if (!items.Contains(heldItem))
            {
                // store current toolbar item to swap back after eating
                PreserveLastItem(heldItem);
            }
            else if (items.IndexOf(heldItem) == items.Count - 1)
            {
                RestoreLastItem();
                return;
            }
            
            // cycle to the next best item
            bestItem = bestItem == null ? items.FirstOrDefault() : items[(items.IndexOf(heldItem) + 1) % items.Count];

            if(bestItem != null)
            {
                Game1.player.CurrentToolIndex = Game1.player.Items.IndexOf(bestItem);
            }
        }

        private IEnumerable<StardewValley.Object> GetValidPlayerItems()
        {
            return Game1.player.Items
                .Where(i => i is StardewValley.Object)
                .Select(i => i as StardewValley.Object);
        }

        private List<StardewValley.Object> GetBestItems()
        {
            float energyNeeded = Game1.player.MaxStamina - Game1.player.Stamina;

            return GetValidPlayerItems()
                .Where(i => i.staminaRecoveredOnConsumption() > 0 && !NeededForBundle(i))
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

        private void EatBestItem()
        {
            if(bestItem != null && Game1.player.CurrentToolIndex == Game1.player.Items.IndexOf(bestItem) && !Game1.player.IsBusyDoingSomething())
            {
                Game1.player.eatHeldObject();
                RestoreLastItem();
            }
        }

        private void PreserveLastItem(StardewValley.Object heldItem)
        {
            lastItemIndex = Game1.player.CurrentToolIndex;
        }

        private void RestoreLastItem()
        {
            bestItem = null;
            if(lastItemIndex > -1)
            {
                Game1.player.CurrentToolIndex = lastItemIndex;
                lastItemIndex = -1;
                return;
            }
        }


        private void CheckBestItemHeld(object sender, RenderedHudEventArgs e)
        {
            if(bestItem != null && Game1.player.CurrentToolIndex == Game1.player.Items.IndexOf(bestItem))
            {
                string energyText = $"+{bestItem.staminaRecoveredOnConsumption()}";
                float stringWidth = Game1.tinyFont.MeasureString(energyText).X;
                float scale = Game1.options.zoomLevel;
                //Vector2 characterInViewport = Game1.player.getLocalPosition();
                Vector2 textLocation = new Vector2(Game1.viewportCenter.X, Game1.viewportCenter.Y + 20);
                Game1.drawWithBorder(
                        energyText,
                        Color.DarkSlateGray,
                        Color.LightSeaGreen,
                        textLocation);
            }
        }
    }
}