using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EatValueOptimizer
{
    class ModConfig
    {
        public Keybind CycleFoodKeybind { get; set; } = new Keybind(SButton.R);
        public Keybind ForceEatFoodKeybind { get; set; } = new Keybind(SButton.LeftControl, SButton.R);
        public Keybind CancelKeybind { get; set; } = new Keybind(SButton.LeftShift, SButton.R);
        public Keybind EatCurrentFoodKeybind { get; set; } = new Keybind(SButton.Space);
    }
}
