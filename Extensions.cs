using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OptimalEating
{
    public static class Extensions
    {
        public static int SafeParseInt32(this string s)
        {
            int result = 0;
            if (!string.IsNullOrWhiteSpace(s))
                int.TryParse(s, out result);
            return result;
        }

        public static bool IsPressed(this Keybind binding) => binding != null && binding.GetState() == SButtonState.Pressed;

        public static void Suppress(this IModHelper helper, Keybind keybind)
        {
            keybind.Buttons.ToList().ForEach((b => helper.Input.Suppress(b)));
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

        public static void DrawNumber(int num, Vector2 leftPos, float scale = 2f)
        {
            Rectangle rectangle1 = new Rectangle(512, 128, 8, 8);
            Stack<Rectangle> rectangleStack = new Stack<Rectangle>();
            for (bool flag = false; num > 0 || !flag; num /= 10)
            {
                flag = true;
                int num1 = num % 10;
                rectangleStack.Push(new Rectangle(rectangle1.X + num1 % 6 * 8, rectangle1.Y + num1 / 6 * 8, 8, 8));
                num -= num1;
            }
            float num2 = 0.0f;
            while (rectangleStack.Count > 0)
            {
                Rectangle rectangle2 = rectangleStack.Pop();
                Game1.spriteBatch.Draw(Game1.mouseCursors, new Vector2(leftPos.X + Utility.ModifyCoordinateForUIScale(num2 * 9f * scale), leftPos.Y), new Rectangle?(rectangle2), Color.White, 0.0f, Vector2.Zero, Utility.ModifyCoordinateForUIScale(scale), (SpriteEffects)0, 0.01f);
                ++num2;
            }
        }
    }
}
