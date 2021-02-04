using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OptimalEating
{
    public static class Extensions
    {
        public static int SafeParseInt32(this string s)
        {
            int result = 0;

            if (!string.IsNullOrWhiteSpace(s))
            {
                int.TryParse(s, out result);
            }

            return result;
        }

        public static bool IsPressed(this Keybind binding)
        {
            return binding == null ? false : binding.GetState() == StardewModdingAPI.SButtonState.Pressed;
        }

        public static void Suppress(this IModHelper helper, Keybind keybind)
        {
            keybind.Buttons.ToList().ForEach(b => helper.Input.Suppress(b));
        }

        public static bool PressedByType(this KeybindList list, KeybindType type)
        {
            return list.GetByType(type).IsPressed();
        }

        public static Keybind GetByType(this KeybindList list, KeybindType type)
        {
            return list.Keybinds[(int)type];
        }

        public static void SuppressType(this KeybindList list, IModHelper helper, KeybindType type)
        {
            helper.Suppress(list.GetByType(type));
        }

    }
}
