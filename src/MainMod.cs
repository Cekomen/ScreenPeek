using BepInEx;
using RWCustom;
using System;
using System.Security.Permissions;
using UnityEngine;
using Unity;
using System.Collections.Generic;
using On;
using BepInEx.Logging;
using System.Runtime.CompilerServices;
using System.Security;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine.Assertions.Must;
using System.IO;
using System.Reflection;

#pragma warning disable CS0618 // Do not remove the following line.
//[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
//[module: UnverifiableCode]

namespace ScreenPeek
{

    [BepInPlugin("ceko.screenpeek", "Screen Peek", "1.1.0")]
    public class MainMod : BaseUnityPlugin
    {
        private static Dictionary<KeyCode, Vector2> keyCodes = new()
        {
            {KeyCode.UpArrow, new Vector2(0,1000) },
            {KeyCode.RightArrow, new Vector2(1000,0) },
            {KeyCode.DownArrow, new Vector2(0,-1000) },
            {KeyCode.LeftArrow, new Vector2(-1000,0) }
        }; //Supports keyboard keycodes for now

        //Screen check margins
        private const float xMargin = 350f;
        private const float yMargin = 350f;

        private bool keyToggled = false;
        private bool toggleOnNextPress = true;
        private bool keyPressed => !MainModOptions.togglePeeking.Value ? Input.GetKey(MainModOptions.keyboardKeybind.Value) : keyToggled;
        private bool aimChanged => previousAim != aim;
        private bool isPeeking = false;
        private bool isOrigin = true;
        private int lastPeekTimer = 0;
        private Vector2 aim = Vector2.zero;
        private Vector2 previousAim = Vector2.zero;
        private int camPos = -1;
        private int originCamPos = 0;

        public static bool isInitialized = false;

        public static readonly string MOD_ID = "ceko.screenpeek";
        public static readonly string version = "1.1.0";

        public MainMod()
        { }

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            MachineConnector.SetRegisteredOI(MOD_ID, MainModOptions.instance);

            if (isInitialized) return;
            isInitialized = true;

            Debug.Log("ScreenPeek: Loaded. Version: " + version);

            On.RoomCamera.Update += RoomCamera_Update;
            On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int;
            On.Player.MovementUpdate += Player_MovementUpdate;
            On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera_int;
        }

        private void RoomCamera_MoveCamera_Room_int(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera self, Room newRoom, int camPos)
        {
            orig(self, newRoom, camPos);
            originCamPos = camPos; //Update current campos of the slugcat when entering a new room
        }

        private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            orig(self);
            UpdateAim();
            if (keyPressed && (!isPeeking || aimChanged))
            {
                ChangeCamera(self);
                isPeeking = true;
                isOrigin = false;
            }
            else if (!keyPressed && !isOrigin)
            {
                Debug.Log("ScreenPeek: Reset to main camera");
                camPos = -1;
                isOrigin = true;
                isPeeking = false;
            }
        }

        private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            //Toggle option
            var peekKeyPressed = Input.GetKey(MainModOptions.keyboardKeybind.Value);
            toggleOnNextPress = toggleOnNextPress || !peekKeyPressed;
            if (toggleOnNextPress && peekKeyPressed)
            {
                toggleOnNextPress = false;
                keyToggled = !keyToggled;
            }

            if (keyPressed)
            {
                if (aim.magnitude != 0)
                {
                    (self.graphicsModule as PlayerGraphics).LookAtPoint(aim + self.mainBodyChunk.pos, 10001f);
                    lastPeekTimer = 41; //Odd number so it aligns with the second if check down below? (lame)
                }
                else
                {
                    lastPeekTimer--;
                }

                if (MainModOptions.standStillWhilePeeking.Value)
                {
                    self.input[0].x = 0;
                    self.input[0].y = 0;
                    self.input[0].jmp = false;
                    self.input[0].thrw = false;
                    self.input[0].pckp = false;
                }
            }

            if (lastPeekTimer > 0)
            {
                if (--lastPeekTimer == 0)
                {
                    (self.graphicsModule as PlayerGraphics).objectLooker.lookAtPoint = null;
                    (self.graphicsModule as PlayerGraphics).LookAtNothing();
                }
            }
            orig(self, eu);
        }

        private void RoomCamera_MoveCamera_int(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera self, int camPos)
        {
            originCamPos = camPos; //Update current campos of the slugcat in the same room
            if (!keyPressed || aimChanged || !isPeeking)
            {
                orig(self, this.camPos == -1 ? camPos : this.camPos); //This changes the virtualMicrophone?.. it's a feature :^)
            }
        }

        private void UpdateAim()
        {
            previousAim = aim;
            aim = Vector2.zero;
            foreach (var keyCode in keyCodes)
            {
                if (Input.GetKey(keyCode.Key))
                    aim += keyCode.Value;
            }
        }

        private void ChangeCamera(RoomCamera rc)
        {
            Debug.Log("Pressed");
            camPos = FindTargetCamera(rc, rc.CamPos(originCamPos) + aim);
            rc.MoveCamera(camPos);
        }

        private int FindTargetCamera(RoomCamera rc, Vector2 targetVector)
        {
            Debug.Log("ScreenPeek: Find target cam, targetVector: " + targetVector);
            int camPos = 0;
            float diff = float.MaxValue;
            Vector2 cam; //Cycled camera vector
            for (int i = 0; i < rc.room.cameraPositions.Length; i++)
            {
                cam = rc.room.cameraPositions[i];
                var newDiff = Vector2.Distance(cam, targetVector);
                //Debug.Log("For loop, index " + i + ", distance is " + newDiff);
                if (newDiff < diff) //if new camera is closer
                {
                    //Debug.Log("New closest cam found, index " + i + " and vector " + cam);
                    diff = newDiff;
                    camPos = i;
                }
            }
            if (Math.Abs(rc.CamPos(camPos).y - targetVector.y) > yMargin ||
               Math.Abs(rc.CamPos(camPos).x - targetVector.x) > xMargin) //if camera found exceeds the allowed margin
            {
                Debug.Log("ScreenPeek: Target camera not found, return current camera.");
                camPos = rc.currentCameraPosition;
            }
            Debug.Log("ScreenPeek: Return campos " + camPos + " as closest.");
            return camPos;
        }

    }
}