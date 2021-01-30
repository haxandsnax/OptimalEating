using StardewModdingAPI.Utilities;
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
    }
}
