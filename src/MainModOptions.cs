using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Menu.Remix.MixedUI;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ScreenPeek
{
    public class MainModOptions : OptionInterface
    {
        public static MainModOptions instance = new MainModOptions();

        private readonly Vector2 buttonSize = new Vector2(150f, 34f);

        public static Configurable<KeyCode>[,] keybinds = new Configurable<KeyCode>[4, 5]
        {
            {
                instance.config.Bind("keybinds00", KeyCode.LeftAlt, new ConfigurableInfo("Keybind to hold to peek.")),
                instance.config.Bind("keybinds01", KeyCode.LeftArrow, new ConfigurableInfo("Directional left key.")),
                instance.config.Bind("keybinds02", KeyCode.UpArrow, new ConfigurableInfo("Directional up key.")),
                instance.config.Bind("keybinds03", KeyCode.RightArrow, new ConfigurableInfo("Directional right key.")),
                instance.config.Bind("keybinds04", KeyCode.DownArrow, new ConfigurableInfo("Directional down key.")) 
            },
            {
                instance.config.Bind("keybinds10", KeyCode.LeftAlt, new ConfigurableInfo("Keybind to hold to peek.")),
                instance.config.Bind("keybinds11", KeyCode.LeftArrow, new ConfigurableInfo("Directional left key.")),
                instance.config.Bind("keybinds12", KeyCode.UpArrow, new ConfigurableInfo("Directional up key.")),
                instance.config.Bind("keybinds13", KeyCode.RightArrow, new ConfigurableInfo("Directional right key.")),
                instance.config.Bind("keybinds14", KeyCode.DownArrow, new ConfigurableInfo("Directional down key."))
            },
            {
                instance.config.Bind("keybinds20", KeyCode.LeftAlt, new ConfigurableInfo("Keybind to hold to peek.")),
                instance.config.Bind("keybinds21", KeyCode.LeftArrow, new ConfigurableInfo("Directional left key.")),
                instance.config.Bind("keybinds22", KeyCode.UpArrow, new ConfigurableInfo("Directional up key.")),
                instance.config.Bind("keybinds23", KeyCode.RightArrow, new ConfigurableInfo("Directional right key.")),
                instance.config.Bind("keybinds24", KeyCode.DownArrow, new ConfigurableInfo("Directional down key."))
            },
            {
                instance.config.Bind("keybinds30", KeyCode.LeftAlt, new ConfigurableInfo("Keybind to hold to peek.")),
                instance.config.Bind("keybinds31", KeyCode.LeftArrow, new ConfigurableInfo("Directional left key.")),
                instance.config.Bind("keybinds32", KeyCode.UpArrow, new ConfigurableInfo("Directional up key.")),
                instance.config.Bind("keybinds33", KeyCode.RightArrow, new ConfigurableInfo("Directional right key.")),
                instance.config.Bind("keybinds34", KeyCode.DownArrow, new ConfigurableInfo("Directional down key."))
            }
        };

        public static Configurable<bool> standStillWhilePeeking = instance.config.Bind("standStillWhilePeeking", true, new ConfigurableInfo("Disables movement actions while peeking."));

        public static Configurable<bool> togglePeeking = instance.config.Bind("togglePeeking", false, new ConfigurableInfo("When checked, you will toggle in/out of the peeking state instead of having to hold it."));

        public override void Initialize()
        {
            base.Initialize();

            Tabs = new OpTab[2];
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
                new OpLabel(new Vector2(0f, 460f), new Vector2(100f, 25f), "Stand still", FLabelAlignment.Center, false, null)
                {
                    alignment = FLabelAlignment.Right,
                    verticalAlignment = OpLabel.LabelVAlignment.Center,
                    description = standStillWhilePeeking.info.description
                },
                new OpCheckBox(standStillWhilePeeking, new Vector2(150f, 460f))
            });

            tab.AddItems(new UIelement[]
            {
                new OpLabel(new Vector2(0f, 400f), new Vector2(100f, 25f), "Toggle peek", FLabelAlignment.Center, false, null)
                {
                    alignment = FLabelAlignment.Right,
                    verticalAlignment = OpLabel.LabelVAlignment.Center,
                    description = togglePeeking.info.description
                },
                new OpCheckBox(togglePeeking, new Vector2(150f, 400f))
            });

            Tabs[1] = new OpTab(this, "Keybinds");
            tab = Tabs[1];
            AddDirectionKeys(ref tab, new Vector2(0, 460));
        }

        private void AddDirectionKeys(ref OpTab tab, Vector2 pos)
        {
            var buttonPos = pos + new Vector2(150, 34); //Initially peek button pos (up left)
            for (int i = 0; i < (ModManager.JollyCoop ? 4 : 1); i++)
            {
                var peekBind = new OpKeyBinder(keybinds[i, 0], buttonPos, buttonSize, false, OpKeyBinder.BindController.AnyController);
                peekBind.colorEdge = Color.cyan;
                peekBind.description = $"Peek key for player {i+1}.";
                tab.AddItems(new UIelement[]
                {
                    new OpLabel(pos, new Vector2(100f, 34f), $"Player {i+1} keybinds", FLabelAlignment.Center, false, null)
                    {
                        alignment = FLabelAlignment.Right,
                        verticalAlignment = OpLabel.LabelVAlignment.Center,
                        description = keybinds[i,0].info.description
                    },
                    peekBind,
                    new OpKeyBinder(keybinds[i,1], buttonPos + new Vector2(0, -buttonSize.y), buttonSize, false, OpKeyBinder.BindController.AnyController),
                    new OpKeyBinder(keybinds[i,2], buttonPos + new Vector2(buttonSize.x, 0), buttonSize, false, OpKeyBinder.BindController.AnyController),
                    new OpKeyBinder(keybinds[i,3], buttonPos + new Vector2(buttonSize.x*2, -buttonSize.y), buttonSize, false, OpKeyBinder.BindController.AnyController),
                    new OpKeyBinder(keybinds[i,4], buttonPos + new Vector2(buttonSize.x, -buttonSize.y), buttonSize, false, OpKeyBinder.BindController.AnyController)
                });

                buttonPos.y -= 100;
                pos.y -= 100;
            }

        }

    }
}
