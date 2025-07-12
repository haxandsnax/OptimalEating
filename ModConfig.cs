using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimalEating
{
    public enum KeybindType
    {
        CycleFoodKeybind,
        CancelKeybind,
        EatCurrentFoodKeybind,
        FavoriteFood1Keybind,
        FavoriteFood2Keybind,
        FavoriteFood3Keybind,
        FavoriteFood4Keybind,
        EatFavoriteFood1Keybind,
        EatFavoriteFood2Keybind,
        EatFavoriteFood3Keybind,
        EatFavoriteFood4Keybind,
    }

    class ModConfig
    {
        public List<KeybindList> Keybinds { get; set; } = new List<KeybindList>()
        {
            new KeybindList(
                new Keybind(SButton.R),
                new Keybind(SButton.Escape),
                new Keybind(SButton.Space),
                new Keybind(SButton.LeftAlt, SButton.W),
                new Keybind(SButton.LeftAlt, SButton.D),
                new Keybind(SButton.LeftAlt, SButton.S),
                new Keybind(SButton.LeftAlt, SButton.A),
                new Keybind(SButton.LeftControl, SButton.W),
                new Keybind(SButton.LeftControl, SButton.D),
                new Keybind(SButton.LeftControl, SButton.S),
                new Keybind(SButton.LeftControl, SButton.A)
            ),
            new KeybindList(
                new Keybind(SButton.LeftStick),
                new Keybind(SButton.RightStick),
                new Keybind(SButton.ControllerB),
                new Keybind(SButton.LeftStick, SButton.RightStick),
                new Keybind(SButton.LeftStick, SButton.ControllerX),
                new Keybind(SButton.LeftStick, SButton.LeftShoulder),
                new Keybind(SButton.LeftStick, SButton.RightShoulder),
                new Keybind(SButton.ControllerB, SButton.RightStick),
                new Keybind(SButton.ControllerB, SButton.ControllerX),
                new Keybind(SButton.ControllerB, SButton.LeftShoulder),
                new Keybind(SButton.ControllerB, SButton.RightShoulder)
            ),
            new KeybindList(
                new Keybind(SButton.LeftStick),
                new Keybind(SButton.RightStick),
                new Keybind(SButton.ControllerB),
                new Keybind(SButton.LeftStick, SButton.RightStick),
                new Keybind(SButton.LeftStick, SButton.ControllerX),
                new Keybind(SButton.LeftStick, SButton.LeftShoulder),
                new Keybind(SButton.LeftStick, SButton.RightShoulder),
                new Keybind(SButton.ControllerB, SButton.RightStick),
                new Keybind(SButton.ControllerB, SButton.ControllerX),
                new Keybind(SButton.ControllerB, SButton.LeftShoulder),
                new Keybind(SButton.ControllerB, SButton.RightShoulder)
            ),
            new KeybindList(
                new Keybind(SButton.LeftStick),
                new Keybind(SButton.RightStick),
                new Keybind(SButton.ControllerB),
                new Keybind(SButton.LeftStick, SButton.RightStick),
                new Keybind(SButton.LeftStick, SButton.ControllerX),
                new Keybind(SButton.LeftStick, SButton.LeftShoulder),
                new Keybind(SButton.LeftStick, SButton.RightShoulder),
                new Keybind(SButton.ControllerB, SButton.RightStick),
                new Keybind(SButton.ControllerB, SButton.ControllerX),
                new Keybind(SButton.ControllerB, SButton.LeftShoulder),
                new Keybind(SButton.ControllerB, SButton.RightShoulder)
            )
        };

        public bool DisableText { get; set; }

        public List<string> BannedItemsByName { get; set; } = new List<string>();

        public List<int> BannedItemIDs { get; set; } = new List<int>();

        public bool ClearWhenRemoved { get; set; }
    }
}
