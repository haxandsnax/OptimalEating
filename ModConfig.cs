using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimalEating
{
    public enum KeybindType : int 
    {
        CycleFoodKeybind = 0,
        ForceEatFoodKeybind = 1,
        CancelKeybind = 2,
        EatCurrentFoodKeybind = 3
    }

    class ModConfig
    {
        public List<KeybindList> Keybinds { get; set; } = new List<KeybindList>() {
            new KeybindList(new Keybind(SButton.R),new Keybind(SButton.LeftControl, SButton.R), new Keybind(SButton.Escape),new Keybind(SButton.Space)),
            new KeybindList(new Keybind(SButton.LeftStick), new Keybind(SButton.RightStick, SButton.LeftStick), new Keybind(SButton.ControllerB), new Keybind(SButton.RightStick)),
            new KeybindList(new Keybind(SButton.LeftStick), new Keybind(SButton.RightStick, SButton.LeftStick), new Keybind(SButton.ControllerB), new Keybind(SButton.RightStick)),
            new KeybindList(new Keybind(SButton.LeftStick), new Keybind(SButton.RightStick, SButton.LeftStick), new Keybind(SButton.ControllerB), new Keybind(SButton.RightStick))
        };
        public bool DisableText { get; set; } = false;
    }
}
