using BepInEx;
using SBCameraScroll;
using System;
using System.Linq;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ScreenPeek
{

    [BepInPlugin("ceko.screenpeek", "Screen Peek", "3.0.0")]
    public class MainMod : BaseUnityPlugin
    {

        //Screen check margins
        private const float xMargin = 500f;
        private const float yMargin = 500f;

        private bool keyToggled = false;
        private bool toggleOnNextPress = true;
        private bool keyHeld => !MainModOptions.togglePeeking.Value ? Input.GetKey(MainModOptions.keybinds[targetPlayer, 0].Value) : keyToggled;
        private bool aimChanged => previousAim != aim;
        private bool isPeeking = false;
        private bool isOrigin = true;
        private int[] lastPeekTimer = new int[4] { 0, 0, 0, 0 };
        private Vector2 aim = Vector2.zero;
        private Vector2 previousAim = Vector2.zero;
        private int camPos = -1;
        private int originCamPos = 0;
        private int targetPlayer = 0;
        RWCustom.IntVector2 intvec = new RWCustom.IntVector2(0, 0);

        public static bool isInitialized = false;

        //Split screen variables
        private Vector2 sSize = Vector2.zero;
        public static bool is_sb_screen_scroll_enabled = false;
        public static bool is_split_screen_coop_enabled = false;
        private int xSpeed = 1000;
        private int ySpeed = 1000;
        private static bool is_on_mods_init_initialized;
        public static readonly string MOD_ID = "ceko.screenpeek";
        public static readonly string version = "3.0.0";

        public MainMod()
        { }

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.RainWorld.PostModsInit += RainWorld_PostModsInit;
        }

        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            if(ModManager.ActiveMods.Any(_ => _.id == "SBCameraScroll"))
            {
                SBCameraScroll.MainModOptions.innercameraboxx_position.Value = 0;
                SBCameraScroll.MainModOptions.innercameraboxy_position.Value = 0;
            }
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            sSize = self.screenSize/2;
            //sSize.y *= -1; //it go UP
            //Debug.Log("ACTIVE MODS: "+String.Join(", ",ModManager.ActiveMods.Select(_ => _.id))+"sSize (x,y):"+sSize.ToString());
            foreach (ModManager.Mod mod in ModManager.ActiveMods)
            {
                //Logger.Log(BepInEx.Logging.LogLevel.Debug, mod.id);
                //Debug.Log(mod.id);
                if (mod.id == "SBCameraScroll")
                {
                    is_sb_screen_scroll_enabled = true;
                    continue;
                }

                if (mod.id == "henpemaz_splitscreencoop")
                {
                    is_split_screen_coop_enabled = true;
                    continue;
                }
            }
            
            MachineConnector.SetRegisteredOI(MOD_ID, MainModOptions.instance);
            //if (MainMod.is_on_mods_init_initialized)
            //{
            //    return;
            //}
            //MainMod.is_on_mods_init_initialized = true;
            targetPlayer = 0; //If jolly is disabled

            if (isInitialized) return;
            isInitialized = true;

            Debug.Log("ScreenPeek: Loaded. Version: " + version);
            
            On.Player.MovementUpdate += Player_MovementUpdate;
            
            if (!is_sb_screen_scroll_enabled)
            {
                On.RoomCamera.Update += RoomCamera_Update;
                On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int;
                On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera_int;
                On.RoomCamera.ChangeRoom += RoomCamera_ChangeRoom;
            }
            else
            {
                On.RoomCamera.Update += CameraScroll_RoomCamera_Update;
                On.Player.Die += Player_Die;
            }
        }

        #region Main Mod
        private void RoomCamera_ChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
        {
            orig(self, newRoom, cameraPosition);
            //Debug.Log("ChangeRoom cameraposition: " + cameraPosition + ", room name: " + newRoom.abstractRoom.name);
            originCamPos = cameraPosition;
        }

        private void RoomCamera_MoveCamera_Room_int(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera self, Room newRoom, int camPos)
        {
            orig(self, newRoom, camPos);
            originCamPos = camPos; //Update current campos of the slugcat when entering a new room, it's -1 when switching to a scug in another room.
        }

        private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            orig(self);
            var player = self.followAbstractCreature?.realizedCreature;
            if (player == null) return;

            if(ModManager.JollyCoop) //Have to reset targetPlayer to 0 in init if jolly is disabled
                targetPlayer = (player as Player).playerState.playerNumber;

            previousAim = aim;
            aim = Vector2.zero;
            if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 1].Value) || intvec.x == -1) //Deal with this later, DON'T complain to me if you use controller and keyboard simultaneously
                    aim += new Vector2(-1000, 0);
            if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 2].Value) || intvec.y == 1)
                    aim += new Vector2(0, 1000);
            if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 3].Value) || intvec.x == 1)
                    aim += new Vector2(1000, 0);
            if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 4].Value) || intvec.y == -1)
                    aim += new Vector2(0, -1000);
            
            //Debug.Log("Aim: " + aim + ", targetplayer: " + targetPlayer);
            //intvec *= 0; //In case we release peeking/switch cameras before releasing the joystick

            if (keyHeld && (!isPeeking || aimChanged) && !player.Consious)
            {
                ChangeCamera(self);
                isPeeking = true;
                isOrigin = false;
            }
            else if (!keyHeld && !isOrigin || (!isOrigin && player.Consious))
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
            toggleOnNextPress |= !peekKeyPressed;
            if (toggleOnNextPress && peekKeyPressed)
            {
                toggleOnNextPress = false;
                keyToggled = !keyToggled;
            }

            if (keyHeld && currentPlayer == targetPlayer)
            {
                intvec = self.input[0].IntVec * (self.input[0].gamePad ? 1 : 0); //Have to capture the analog input before we set it to 0 below, IF the input is from analog
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
            if (lastPeekTimer[currentPlayer] > 0)
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
            if (!keyHeld || aimChanged || !isPeeking)
            {
                orig(self, this.camPos == -1 ? camPos : this.camPos); //This changes the virtualMicrophone?.. it's a feature :^)
            }
        }

        private void ChangeCamera(RoomCamera rc)
        {
            //Debug.Log("ScreenPeek: Button Pressed");
            camPos = FindTargetCamera(rc, rc.CamPos(originCamPos), aim);
            rc.MoveCamera(camPos);
        }

        private int FindTargetCamera(RoomCamera rc, Vector2 camVector, Vector2 aimVector)
        {
            Vector2 targetVector = Vector2.zero;
            Debug.Log("ScreenPeek: Find target cam, estimate coordinates: " + (camVector + aimVector));
            
            for (int i = 6; i <= 10; i++)
            {
                targetVector.Set(camVector.x + (aimVector.x * (i / 10.0f)), camVector.y + (aimVector.y * (i / 10.0f)));
                for (int j = 0; j < rc.room.cameraPositions.Length; j++)
                {
                    if (!(Math.Abs(rc.CamPos(j).y - targetVector.y) > yMargin ||
                        Math.Abs(rc.CamPos(j).x - targetVector.x) > xMargin)) //Camera is within margin 
                    {
                        Debug.Log("ScreenPeek: Return camera " + j + " as closest.");
                        return j;
                    }
                }
            }
                        
            Debug.Log("ScreenPeek: Target camera not found, return current camera " + rc.currentCameraPosition);
            return rc.currentCameraPosition;
           
        }

        #endregion

        #region SBCameraScroll
        private void CameraScroll_RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera rc)
        {
            var player = rc.followAbstractCreature?.realizedCreature as Player;
            if (player == null)
            {
                orig(rc);
                return;
            }
            if (ModManager.JollyCoop) //Have to reset targetPlayer to 0 in init if jolly is disabled
                targetPlayer = player.playerState.playerNumber;

            if (keyHeld && !isPeeking)
            {
                isPeeking = true;
                previousAim = aim;
            }
            else if (!keyHeld && isPeeking)
            {
                isPeeking = false;
                if (!aimChanged || !MainModOptions.toggleCSPeeking.Value)
                    aim = Vector2.zero;
            }

            var playerPos = player.mainBodyChunk.pos;
            Vector2? prevCamPos = null; //cant be an initial camera pos?
            if (isPeeking)
            {
                Debug.Log(String.Format("Peeking, RC: {0}, Aim: {1}, Player: {2}", rc.pos.ToString(), aim.ToString(), playerPos.ToString()));
                var pAim = aim;
                if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 1].Value) || intvec.x == -1 /*&& if camera moved -x last time, make sure to check for first frame*/)
                {
                    aim += new Vector2(-xSpeed, 0) / 40;//+ rc.pos + sSize

                } //Deal with this later, DON'T complain to me if you use controller and keyboard simultaneously
                if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 2].Value) || intvec.y == 1)
                {
                    aim += new Vector2(0, ySpeed) / 40;

                }
                if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 3].Value) || intvec.x == 1)
                {
                    aim += new Vector2(xSpeed, 0) / 40;

                }
                if (Input.GetKey(MainModOptions.keybinds[targetPlayer, 4].Value) || intvec.y == -1)
                {
                    aim += new Vector2(0, -ySpeed) / 40;

                }
                if (pAim != aim)
                {
                    prevCamPos = rc.pos;
                    Debug.Log("Aim changed, set prevcampos:" + prevCamPos.ToString());
                }
            }
            //else
            //{
            //    orig(self);
            //    return;
            //}

            //aim += -(playerPos - previousPlayerPos) * (MainModOptions.toggleCSAnchor.Value ? 0 : 1);//works fine unless you die/reload i guess. change the logic to only add current userpos if anchor is on maybe
            player.mainBodyChunk.pos = rc.pos + sSize + aim;
            orig(rc);
            player.mainBodyChunk.pos = playerPos;
        }

        private void Player_Die(On.Player.orig_Die orig, Player self)
        {
            orig(self);
            aim = Vector2.zero;
            previousAim = Vector2.zero;
        }

        #endregion

    }
}