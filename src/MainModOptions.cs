using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace ScreenPeek
{
    public class MainModOptions : OptionInterface
    {
        public static MainModOptions instance = new MainModOptions();

        public static Configurable<KeyCode> keyboardKeybind = instance.config.Bind("keyboardKeybind", KeyCode.LeftAlt, new ConfigurableInfo("Keybind to hold to peek.", null, "", new object[]
        {
            "Peek state keybind"
        }));

        public static Configurable<KeyCode> aimKeybind = instance.config.Bind("aimKeybind", KeyCode.LeftAlt, new ConfigurableInfo("Directional keys to control the camera.", null, "", new object[]
        {
            "Aim keybinds"
        }));

        public static Configurable<bool> standStillWhilePeeking = instance.config.Bind("standStillWhilePeeking", true, new ConfigurableInfo("Disables movement actions while peeking.", null, "", new object[]
        {
            "Stand still while peeking?"
        }));

        public static Configurable<bool> togglePeeking = instance.config.Bind("togglePeeking", false, new ConfigurableInfo("When checked, you will toggle in/out of the peeking state instead of having to hold it.", null, "", new object[]
        {
            "Toggle peeking?"
        }));

        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[1];
            Tabs[0] = new OpTab(this, "General");
            var tab = Tabs[0];

            tab.AddItems(new UIelement[]
            {
                new OpLabel(new Vector2(280f, 520f), new Vector2(150f, 69f), "Screen Peek Configuration", FLabelAlignment.Center, true, null)
                {
                    alignment = FLabelAlignment.Right,
                    verticalAlignment = OpLabel.LabelVAlignment.Center,
                    description = standStillWhilePeeking.info.description
                }
            });

            tab.AddItems(new UIelement[]
            {
                new OpLabel(new Vector2(0f, 460f), new Vector2(100f, 34f), "Peek key", FLabelAlignment.Center, false, null)
                {
                    alignment = FLabelAlignment.Right,
                    verticalAlignment = OpLabel.LabelVAlignment.Center,
                    description = keyboardKeybind.info.description
                },
                new OpKeyBinder(keyboardKeybind, new Vector2(150f, 460f), new Vector2(146f, 34f), false, OpKeyBinder.BindController.AnyController)
            });

            tab.AddItems(new UIelement[]
            {
                new OpLabel(new Vector2(0f, 400f), new Vector2(100f, 25f), "Stand still", FLabelAlignment.Center, false, null)
                {
                    alignment = FLabelAlignment.Right,
                    verticalAlignment = OpLabel.LabelVAlignment.Center,
                    description = standStillWhilePeeking.info.description
                },
                new OpCheckBox(standStillWhilePeeking, new Vector2(150f, 400f))
            });

            tab.AddItems(new UIelement[]
            {
                new OpLabel(new Vector2(0f, 340f), new Vector2(100f, 25f), "Toggle peek", FLabelAlignment.Center, false, null)
                {
                    alignment = FLabelAlignment.Right,
                    verticalAlignment = OpLabel.LabelVAlignment.Center,
                    description = togglePeeking.info.description
                },
                new OpCheckBox(togglePeeking, new Vector2(150f, 340f))
            });
        }

    }
}
