using BepInEx;
using System;
using System.Security.Permissions;
using UnityEngine;
using System.Collections.Generic;
using System.Timers;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ScreenPeek
{

    [BepInPlugin("ceko.screenpeek", "Screen Peek", "2.0.0")]
    public class MainMod : BaseUnityPlugin
    {

        //Screen check margins
        private const float xMargin = 350f;
        private const float yMargin = 350f;

        private bool keyToggled = false;
        private bool toggleOnNextPress = true;
        private bool keyPressed => !MainModOptions.togglePeeking.Value ? Input.GetKey(MainModOptions.keybinds[targetPlayer, 0].Value) : keyToggled;
        private bool aimChanged => previousAim != aim;
        private bool isPeeking = false;
        private bool isOrigin = true;
        private int[] lastPeekTimer = new int[4] { 0, 0, 0, 0 };
        private Vector2 aim = Vector2.zero;
        private Vector2 previousAim = Vector2.zero;
        private int camPos = -1;
        private int originCamPos = 0;
        private int targetPlayer = 0;

        public static bool isInitialized = false;

        public static readonly string MOD_ID = "ceko.screenpeek";
        public static readonly string version = "2.0.0";

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
            On.RoomCamera.ChangeCameraToPlayer += RoomCamera_ChangeCameraToPlayer;
            On.RoomCamera.ChangeRoom += RoomCamera_ChangeRoom;
            On.Player.PermaDie += Player_PermaDie;
        }

        private void Player_PermaDie(On.Player.orig_PermaDie orig, Player self)
        {
            orig(self);
            var game = self.abstractCreature.Room.realizedRoom.game;
            if (self.playerState.playerNumber == targetPlayer && game.FirstAlivePlayer?.realizedCreature != null) //Is this check necessary, say when a DLL eats you? idk
            {
                targetPlayer = (game.FirstAlivePlayer.realizedCreature as Player).playerState.playerNumber;
            }
        }

        private void RoomCamera_ChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
        {
            orig(self, newRoom, cameraPosition);
            //Debug.Log("ChangeRoom cameraposition: " + cameraPosition + ", room name: " + newRoom.abstractRoom.name);
            originCamPos = cameraPosition;
        }

        private void RoomCamera_ChangeCameraToPlayer(On.RoomCamera.orig_ChangeCameraToPlayer orig, RoomCamera self, AbstractCreature cameraTarget)
        {
            orig(self, cameraTarget);
            Debug.Log("ScreenPeek: Change camera to player " + cameraTarget);
            targetPlayer = self.game.Players.IndexOf(cameraTarget);

            //originCamPos = self.currentCameraPosition;
        }

        private void RoomCamera_MoveCamera_Room_int(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera self, Room newRoom, int camPos)
        {
            orig(self, newRoom, camPos);
            originCamPos = camPos; //Update current campos of the slugcat when entering a new room, it's -1 when switching to a scug in another room.
        }

        private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            orig(self);
            var stunned = self.followAbstractCreature?.realizedCreature != null && (self.followAbstractCreature.realizedCreature.Stunned || self.followAbstractCreature.realizedCreature.dead);
    
            previousAim = aim;
            aim = Vector2.zero;
            if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 1].Value)) //Deal with this later
                aim += new Vector2(-1000, 0);
            if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 2].Value))
                aim += new Vector2(0, 1000);
            if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 3].Value))
                aim += new Vector2(1000, 0);
            if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 4].Value))
                aim += new Vector2(0, -1000);

            if (keyPressed && (!isPeeking || aimChanged) && !stunned)
            {
                ChangeCamera(self);
                isPeeking = true;
                isOrigin = false;
            }
            else if (!keyPressed && !isOrigin || (!isOrigin && stunned))
            {
                Debug.Log("ScreenPeek: Reset to main camera");
                camPos = -1;
                isOrigin = true;
                isPeeking = false;
            }
        }

        private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            var currentPlayer = self.playerState.playerNumber;
      
            //Toggle option
            var peekKeyPressed = Input.GetKey(MainModOptions.keybinds[targetPlayer, 0].Value);
            toggleOnNextPress = toggleOnNextPress || !peekKeyPressed;
            if (toggleOnNextPress && peekKeyPressed)
            {
                toggleOnNextPress = false;
                keyToggled = !keyToggled;
            }

            if (keyPressed && currentPlayer == targetPlayer)
            {
                if (aim.magnitude != 0)
                {
                    (self.graphicsModule as PlayerGraphics).LookAtPoint(aim + self.mainBodyChunk.pos, 10001f);
                    lastPeekTimer[currentPlayer] = 40; 
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
            if (lastPeekTimer[self.playerState.playerNumber] > 0)
            {
                //Debug.Log("Current player: "+ currentPlayer + ", Targer player: "+targetPlayer+", lastPeekTimer: " + lastPeekTimer[currentPlayer]);
                if (--lastPeekTimer[currentPlayer] == 0)
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

        private void ChangeCamera(RoomCamera rc)
        {
            Debug.Log("ScreenPeek: Button Pressed");
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