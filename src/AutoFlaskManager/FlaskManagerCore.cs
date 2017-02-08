﻿using PoeHUD.Controllers;
using PoeHUD.Poe.Components;
using PoeHUD.Plugins;
using System.Collections.Generic;
using PoeHUD.Poe;
using System.Windows.Forms;
using System.Threading;
using System;
using PoeHUD.Poe.EntityComponents;
using PoeHUD.Poe.Elements;
using System.Runtime.InteropServices;
using SharpDX;
using System.Diagnostics;
using PoeHUD.Hud.Health;
using System.IO;
using Newtonsoft.Json;


namespace FlaskManager
{
    public class FlaskManagerCore : BaseSettingsPlugin<FlaskManagerSettings>
    {
        private bool DEBUG = false;
        private readonly int logmsg_time = 3;
        private readonly int errmsg_time = 10;
        private bool isThreadEnabled;
        private IntPtr gameHandle;
        private Queue<Element> eleQueue;
        private Dictionary<string, float> debugDebuff;

        private bool isTownOrHideout;
        private DebuffPanelConfig debuffInfo;

        private float moveCounter;
        private float lastManaUsed;
        private float lastLifeUsed;
        private List<PlayerFlask> playerFlaskList;
        private FlaskKeys keyinfo;

        #region FlaskInformations
        private FlaskAction flask_name_to_action(string flaskname)
        {
            flaskname = flaskname.ToLower();
            FlaskAction ret = FlaskAction.NONE;
            String defense_pattern = @"bismuth|jade|stibnite|granite|amethyst|ruby|sapphire|topaz|aquamarine|quartz";
            String offense_pattern = @"silver|sulphur|basalt|diamond";
            if (flaskname.Contains("life"))
                ret = FlaskAction.LIFE;
            else if (flaskname.Contains("mana"))
                ret = FlaskAction.MANA;
            else if (flaskname.Contains("hybrid"))
                ret = FlaskAction.HYBRID;
            else if (flaskname.Contains("quicksilver"))
                ret = FlaskAction.SPEEDRUN;
            else if (System.Text.RegularExpressions.Regex.IsMatch(flaskname, defense_pattern))
                ret = FlaskAction.DEFENSE;
            else if (System.Text.RegularExpressions.Regex.IsMatch(flaskname, offense_pattern))
                ret = FlaskAction.OFFENSE;
            return ret;
        }
        private FlaskAction flask_mod_to_action(string flaskmodRawName)
        {
            flaskmodRawName = flaskmodRawName.ToLower();
            FlaskAction ret = FlaskAction.NONE;
            String defense_pattern = @"armour|evasion|lifeleech|manaleech|resistance";
            String ignore_pattern = @"levelrequirement|duration|charges|recharge|recovery|extramana|extralife|consecrate|smoke|ground";
            if (flaskmodRawName.Contains("unique"))
                ret = FlaskAction.UNIQUE_FLASK;
            else if (flaskmodRawName.Contains("poison"))
                ret = FlaskAction.POISON_IMMUNE;
            else if (flaskmodRawName.Contains("chill") && !flaskmodRawName.Contains("ground"))
                ret = FlaskAction.FREEZE_IMMUNE;
            else if (flaskmodRawName.Contains("burning"))
                ret = FlaskAction.IGNITE_IMMUNE;
            else if (flaskmodRawName.Contains("shock"))
                ret = FlaskAction.SHOCK_IMMUNE;
            else if (flaskmodRawName.Contains("bleeding"))
                ret = FlaskAction.BLEED_IMMUNE;
            else if (flaskmodRawName.Contains("curse"))
                ret = FlaskAction.CURSE_IMMUNE;
            else if (flaskmodRawName.Contains("knockback"))
                ret = FlaskAction.OFFENSE;
            else if (flaskmodRawName.Contains("movementspeed"))
                ret = FlaskAction.SPEEDRUN;
            else if (System.Text.RegularExpressions.Regex.IsMatch(flaskmodRawName, defense_pattern))
                ret = FlaskAction.DEFENSE;
            else if (System.Text.RegularExpressions.Regex.IsMatch(flaskmodRawName, ignore_pattern))
                ret = FlaskAction.IGNORE;
            return ret;
        }
        #endregion
        #region FlaskManagerInit
        public override void Render()
        {
            base.Render();
            if (Settings.Enable.Value && Settings.uiEnable.Value)
            {
                float X = GameController.Window.GetWindowRectangle().Width * Settings.positionX.Value * .01f;
                float Y = GameController.Window.GetWindowRectangle().Height * Settings.positionY.Value * .01f;
                Vector2 position = new Vector2(X, Y);
                float maxWidth = 0;
                float maxheight = 0;

                foreach (var flasks in playerFlaskList.ToArray())
                {
                    Color textColor = (flasks.isEnabled) ? Color.DarkKhaki : Color.Red;
                    var size = Graphics.DrawText(flasks.FlaskName, Settings.textSize.Value, position, textColor);
                    position.Y += size.Height;
                    maxheight += size.Height;
                    maxWidth = Math.Max(maxWidth, size.Width);
                }
                /* Debug Panel for buffs. TODO
                 * foreach (var buff in GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>().Buffs)
                {
                    if (float.IsInfinity(buff.Timer) || buff.Name.ToLower().Contains("flask"))
                        continue;
                    var size = Graphics.DrawText(buff.Name + ":" + buff.Timer, Settings.textSize.Value, position, Color.WhiteSmoke);
                    position.Y += size.Height;
                    maxheight += size.Height;
                    maxWidth = Math.Max(maxWidth, size.Width);
                }*/
                var background = new RectangleF(X, Y, maxWidth, maxheight);
                Graphics.DrawFrame(background, 5, Color.Black);
                Graphics.DrawImage("lightBackground.png", background);
            }
        }
        public override void OnClose()
        {
            base.OnClose();
            if (Settings.debugMode.Value)
                foreach (var key in debugDebuff)
                {
                    File.AppendAllText("autoflaskmanagerDebug.log", key.Key + " : " + key.Value + Environment.NewLine );
                }
        }
        public override void Initialise()
        {
            PluginName = "Flask Manager";
            if (File.Exists("config/flaskbind.json"))
            {
                string keyfile = File.ReadAllText("config/flaskbind.json");
                keyinfo = JsonConvert.DeserializeObject<FlaskKeys>(keyfile);
            } else
            {
                keyinfo = new FlaskKeys(Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5);
                File.WriteAllText("config/flaskbind.json", JsonConvert.SerializeObject(keyinfo));
            }
            playerFlaskList = new List<PlayerFlask>();
            string json = File.ReadAllText("config/debuffPanel.json");
            debuffInfo = JsonConvert.DeserializeObject<DebuffPanelConfig>(json);
            eleQueue = new Queue<Element>();
            DEBUG = Settings.debugMode.Value;
            debugDebuff = new Dictionary<string, float>();
            OnFlaskManagerToggle();
            GameController.Area.OnAreaChange += area => OnAreaChange(area);
            Settings.Enable.OnValueChanged += OnFlaskManagerToggle;
            Settings.debugMode.OnValueChanged += OnDebugMode;
        }
        private void OnDebugMode()
        {
            DEBUG = Settings.debugMode.Value;
        }
        private void OnFlaskManagerToggle()
        {
            try
            {
                if (Settings.Enable.Value)
                {
                    if (DEBUG)
                        LogMessage("Enabling FlaskManager.",logmsg_time);
                    moveCounter = 0;
                    isThreadEnabled = true;
                    lastManaUsed = 100f;
                    lastLifeUsed = 100f;
                    isTownOrHideout = true;
                    gameHandle = GameController.Window.Process.MainWindowHandle;
                    //We are creating our plugin thread inside PoEHUD!
                    Thread flaskThread = new Thread(FlaskThread) { IsBackground = true };
                    flaskThread.Start();
                }
                else
                {
                    if(DEBUG)
                        LogMessage("Disabling FlaskManager.", logmsg_time);
                    playerFlaskList.Clear();
                    isThreadEnabled = false;
                }
            }
            catch (Exception)
            {

                LogError("Error Starting FlaskManager Thread.", errmsg_time);
            }
        }
        private void OnAreaChange(AreaController area)
        {
            if (Settings.Enable.Value)
            {
                LogMessage("Area has been changed. Loading flasks info.", logmsg_time);
                if (GameController.Area.CurrentArea.IsHideout || GameController.Area.CurrentArea.IsTown)
                    isTownOrHideout = true;
                else
                    isTownOrHideout = false;
            }
        }
        #endregion
        #region FindingFlasks
        //Breath First Search for finding flask root.
        private Element findFlaskRoot()
        {
            Element current = null;
            InventoryItemIcon itm = null;
            Entity item = null;
            while (eleQueue.Count > 0)
            {
                current = eleQueue.Dequeue();
                if (current == null)
                    continue;
                foreach (var child in current.Children)
                {
                    eleQueue.Enqueue(child);
                }

                itm = current.AsObject<InventoryItemIcon>();

                if (itm.ToolTipType == ToolTipType.InventoryItem)
                {
                    item = itm.Item;
                    if (item != null && item.HasComponent<Flask>())
                    {
                        eleQueue.Clear();
                        return current.Parent;
                    }
                }
            }
            return null;
        }
        private Element getFlaskRoot()
        {
            try
            {
                eleQueue.Clear();
                eleQueue.Enqueue(GameController.Game.IngameState.UIRoot);
                return findFlaskRoot();
            }
            catch (Exception e)
            {
                LogError("Warning: Cannot find Flask list.", errmsg_time);
                LogError(e.Message + e.StackTrace, errmsg_time);
                return null;
            }
        }
        private bool GettingAllFlaskInfo(Element flaskRoot)
        {
            if(DEBUG)
                LogMessage("Getting Inventory Flasks info.", logmsg_time);
            playerFlaskList.Clear();
            try
            {
                int totalFlasksEquipped = (int)(flaskRoot.ChildCount);
                for (int j = 0; j < totalFlasksEquipped; j++)
                {
                    InventoryItemIcon flask = flaskRoot.Children[j].AsObject<InventoryItemIcon>();
                    Entity flaskItem = flask.Item;
                    Charges flaskCharges = flaskItem.GetComponent<Charges>();
                    Mods flaskMods = flaskItem.GetComponent<Mods>();
                    PlayerFlask newFlask = new PlayerFlask();

                    newFlask.SetSettings(Settings);
                    newFlask.Slot = flask.InventPosX;
                    newFlask.Item = flaskItem;
                    newFlask.MaxCharges = flaskCharges.ChargesMax;
                    newFlask.UseCharges = flaskCharges.ChargesPerUse;
                    newFlask.CurrentCharges = flaskCharges.NumCharges;
                    newFlask.FlaskName = GameController.Files.BaseItemTypes.Translate(flaskItem.Path).BaseName;
                    newFlask.FlaskAction1 = flask_name_to_action(newFlask.FlaskName);
                    //Checking flask action based on flask name.
                    if (newFlask.FlaskAction1 == FlaskAction.NONE)
                        LogError("Error: " + newFlask.FlaskName + " name not found. Is it unique flask? If not, report this error message.", errmsg_time);
                    FlaskAction action2 = newFlask.FlaskAction2 = FlaskAction.NONE;
                    //Checking flask action based on flask mods.
                    foreach (var mod in flaskMods.ItemMods)
                    {
                        action2 = flask_mod_to_action(mod.RawName);
                        if (action2 == FlaskAction.NONE)
                            LogError("Error: " + mod.RawName + " mod not found. Is it unique flask? If not, report this error message.", errmsg_time);
                        else if (action2 == FlaskAction.UNIQUE_FLASK)
                        {
                            newFlask.FlaskAction2 = FlaskAction.UNIQUE_FLASK;
                            break;
                        }
                        else if (action2 != FlaskAction.IGNORE)
                            newFlask.FlaskAction2 = action2;
                    }
                    // If it's a unique flask, ignore flask action based on flask name
                    // Depending if user have enabled or disabled unique flask or not.
                    if (newFlask.FlaskAction2 == FlaskAction.UNIQUE_FLASK)
                        if (!Settings.uniqFlaskEnable.Value)
                            newFlask.FlaskAction1 = FlaskAction.NONE;

                    // Speedrun mod on mana/life flask wouldn't work when full mana/life is full respectively,
                    // So we will ignore speedrun mod from mana/life flask. Other mods
                    // on mana/life flasks will work.
                    if ((newFlask.FlaskAction1 == FlaskAction.LIFE || newFlask.FlaskAction1 == FlaskAction.MANA ||
                        newFlask.FlaskAction1 == FlaskAction.HYBRID) && newFlask.FlaskAction2 == FlaskAction.SPEEDRUN)
                    {
                        LogError("Warning: Speed Run mod is ignored on mana/life/hybrid flasks.", errmsg_time);
                        newFlask.FlaskAction2 = FlaskAction.NONE;
                    }
                    newFlask.EnableDisableFlask();
                    playerFlaskList.Add(newFlask);
                }

            }
            catch (Exception e)
            {
                if (DEBUG)
                {
                    LogError("Warning: Error getting all flask Informations.", errmsg_time);
                    LogError(e.Message + e.StackTrace, errmsg_time);
                }
                playerFlaskList.Clear();
                return false;
            }
            playerFlaskList.Sort((x, y) => x.Slot.CompareTo(y.Slot));
            return true;
        }
        #endregion
        #region FlaskHelperFunctions
        private void UpdateFlaskChargesInfo(PlayerFlask flask)
        {
            flask.CurrentCharges = flask.Item.GetComponent<Charges>().NumCharges;
        }
        private void UseFlask(PlayerFlask flask)
        {
            KeyPressRelease(keyinfo.k[flask.Slot]);
        }
        private bool FindDrinkFlask(FlaskAction type1, FlaskAction type2)
        {
            var flaskList = playerFlaskList.FindAll(x => x.FlaskAction1 == type1 || x.FlaskAction2 == type2);
            foreach (var flask in flaskList)
            {
                if (flask.isEnabled && flask.CurrentCharges >= flask.UseCharges)
                {
                    UseFlask(flask);
                    UpdateFlaskChargesInfo(flask);
                    if (DEBUG)
                        LogMessage("Just Drank Flask on slot " + flask.Slot, logmsg_time);
                    // if there are multiple flasks, drinking 1 of them at a time is enough.
                    return true;
                }
                else
                {
                    UpdateFlaskChargesInfo(flask);
                }

            }
            return false;
        }
        #endregion
        #region FlaskAndChickenLogics
        private int ExitPoe(string ExeName, string arguments)
        {
            // Prepare the process to run
            ProcessStartInfo start = new ProcessStartInfo();
            // Enter in the command line arguments, everything you would enter after the executable name itself
            start.Arguments = arguments;
            // Enter the executable to run, including the complete path
            start.FileName = ExeName;
            // Do you want to show a console window?
            start.WindowStyle = ProcessWindowStyle.Hidden;
            start.CreateNoWindow = true;
            int exitCode;


            // Run the external process & wait for it to finish
            using (Process proc = Process.Start(start))
            {
                proc.WaitForExit();

                // Retrieve the app's exit code
                exitCode = proc.ExitCode;
            }
            return exitCode;
        }
        private void AutoChicken()
        {
            var LocalPlayer = GameController.Game.IngameState.Data.LocalPlayer;
            var PlayerHealth = LocalPlayer.GetComponent<Life>();
            if (Settings.isPercentQuit.Value && LocalPlayer.IsValid)
            {
                if (PlayerHealth.HPPercentage * 100 < Settings.percentHPQuit.Value)
                {
                    try
                    {
                        ExitPoe("cports.exe", "/close * * * * " + GameController.Window.Process.ProcessName + ".exe");
                    }
                    catch (Exception)
                    {
                        LogError("Error: Cannot find cports.exe, you must die now!", errmsg_time);
                    }

                }
                if (PlayerHealth.ESPercentage * 100 < Settings.percentESQuit.Value)
                {
                    try
                    {
                        ExitPoe("cports.exe", "/close * * * * " + GameController.Window.Process.ProcessName + ".exe");
                    }
                    catch (Exception)
                    {
                        LogError("Error: Cannot find cports.exe, you must die now!", errmsg_time);
                    }
                }
            }
            return;
        }
        private bool HasDebuff(Dictionary<string, int> dictionary, string buffName, bool isHostile)
        {
            int filterId;
            if (dictionary.TryGetValue(buffName, out filterId))
            {
                return filterId == 0 || isHostile == (filterId == 1);
            }
            return false;
        }
        private void SpeedFlaskLogic()
        {
            var LocalPlayer = GameController.Game.IngameState.Data.LocalPlayer;
            var PlayerHealth = LocalPlayer.GetComponent<Life>();
            var PlayerMovement = LocalPlayer.GetComponent<Actor>();
            moveCounter = PlayerMovement.isMoving ? moveCounter += 0.1f : 0;
            if (LocalPlayer.IsValid && Settings.qSEnable.Value && moveCounter >= Settings.qSDur.Value &&
                !PlayerHealth.HasBuff("flask_bonus_movement_speed") &&
                !PlayerHealth.HasBuff("flask_utility_sprint"))
            {
                FindDrinkFlask(FlaskAction.SPEEDRUN, FlaskAction.SPEEDRUN);
            }
        }
        private void ManaLogic()
        {
            var LocalPlayer = GameController.Game.IngameState.Data.LocalPlayer;
            var PlayerHealth = LocalPlayer.GetComponent<Life>();
            lastManaUsed += 0.1f;
            if (lastManaUsed < Settings.ManaDelay.Value)
                return;
            if (Settings.autoFlask.Value && LocalPlayer.IsValid)
            {
                if (PlayerHealth.MPPercentage * 100 < Settings.PerManaFlask.Value)
                {
                    if (FindDrinkFlask(FlaskAction.MANA, FlaskAction.IGNORE))
                        lastManaUsed = 0f;
                    else if (FindDrinkFlask(FlaskAction.HYBRID, FlaskAction.IGNORE))
                        lastManaUsed = 0f;
                }
            }
        }
        private void LifeLogic()
        {
            var LocalPlayer = GameController.Game.IngameState.Data.LocalPlayer;
            var PlayerHealth = LocalPlayer.GetComponent<Life>();
            lastLifeUsed += 0.1f;
            if (lastLifeUsed < Settings.HPDelay.Value)
                return;
            if (Settings.autoFlask.Value && LocalPlayer.IsValid)
            {
                if (PlayerHealth.HPPercentage * 100 < Settings.perHPFlask.Value)
                {
                    if (FindDrinkFlask(FlaskAction.LIFE, FlaskAction.IGNORE))
                        lastLifeUsed = 0f;
                    else if (FindDrinkFlask(FlaskAction.LIFE, FlaskAction.IGNORE))
                        lastLifeUsed = 0f;
                }
            }
        }
        private void AilmentLogic()
        {
            var LocalPlayer = GameController.Game.IngameState.Data.LocalPlayer;
            var PlayerHealth = LocalPlayer.GetComponent<Life>();
            foreach (var buff in PlayerHealth.Buffs)
            {
                var buffName = buff.Name;

                if (DEBUG)
                    debugDebuff[buffName] = buff.Timer;

                if (!Settings.remAilment.Value)
                    return;
                if (Settings.remCorrupt.Value && !float.IsInfinity(buff.Timer) && HasDebuff(debuffInfo.Bleeding, buffName, false))
                    LogMessage("Bleeding -> hasDrunkFlask:" + FindDrinkFlask(FlaskAction.IGNORE, FlaskAction.BLEED_IMMUNE), logmsg_time);
                else if (Settings.remPoison.Value && !float.IsInfinity(buff.Timer) && HasDebuff(debuffInfo.Poisoned, buffName, false))
                    LogMessage("Poison -> hasDrunkFlask:" + FindDrinkFlask(FlaskAction.IGNORE, FlaskAction.POISON_IMMUNE), logmsg_time);
                else if (Settings.remFrozen.Value && !float.IsInfinity(buff.Timer) && HasDebuff(debuffInfo.ChilledFrozen, buffName, false))
                    LogMessage("Frozen -> hasDrunkFlask:" + FindDrinkFlask(FlaskAction.IGNORE, FlaskAction.FREEZE_IMMUNE), logmsg_time);
                else if (Settings.remBurning.Value && !float.IsInfinity(buff.Timer) && HasDebuff(debuffInfo.Burning, buffName, false))
                    LogMessage("Burning -> hasDrunkFlask:" + FindDrinkFlask(FlaskAction.IGNORE, FlaskAction.IGNITE_IMMUNE), logmsg_time);
                else if (Settings.remShocked.Value && !float.IsInfinity(buff.Timer) && HasDebuff(debuffInfo.Shocked, buffName, false))
                    LogMessage("Shock -> hasDrunkFlask:" + FindDrinkFlask(FlaskAction.IGNORE, FlaskAction.SHOCK_IMMUNE), logmsg_time);
                else if (Settings.remCurse.Value && !float.IsInfinity(buff.Timer) && HasDebuff(debuffInfo.WeakenedSlowed, buffName, false))
                    LogMessage("Curse -> hasDrunkFlask:" + FindDrinkFlask(FlaskAction.IGNORE, FlaskAction.CURSE_IMMUNE), logmsg_time);
            }
        }
        #endregion

        #region Keyboard Input
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, UIntPtr wParam, UIntPtr lParam);
        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);
        public void KeyDown(Keys Key)
        {
            SendMessage(gameHandle, 0x100, (int)Key, 0);
        }
        public void KeyUp(Keys Key)
        {
            SendMessage(gameHandle, 0x101, (int)Key, 0);
        }
        public void KeyPressRelease(Keys key)
        {
            KeyDown(key);
            Thread.Sleep((int)(GameController.Game.IngameState.CurLatency));
            // working as a double key.
            //KeyUp(key);
        }
        private void Write(string text, params object[] args)
        {
            foreach (var character in string.Format(text, args))
            {
                PostMessage(gameHandle, 0x0102, new UIntPtr(character), UIntPtr.Zero);
            }
        }
        #endregion
        #region Threading, do not touch
        private void FlaskMain()
        {
            if (!GameController.Game.IngameState.Data.LocalPlayer.IsValid)
                return;

            Element flaskRoot = getFlaskRoot();

            if (flaskRoot == null)
                return;

            var totalFlask = (int)(flaskRoot.ChildCount);
            if (totalFlask > 0 && totalFlask != playerFlaskList.Count)
            {
                if(DEBUG)
                    LogMessage("Invalid Flask Count, Recalculating it.", logmsg_time);
                if (!GettingAllFlaskInfo(flaskRoot))
                    return;
            }

            for (int j = 0; j < playerFlaskList.Count; j++)
                if (!playerFlaskList[j].Item.IsValid)
                {
                    GettingAllFlaskInfo(flaskRoot);
                    return;
                }

            if (isTownOrHideout || GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Life>().HasBuff("grace_period"))
                return;

            SpeedFlaskLogic();
            ManaLogic();
            LifeLogic();
            AilmentLogic();
            return;
        }
        private void FlaskThread()
        {
            while (isThreadEnabled)
            {
                FlaskMain();
                for (int j=0; j< 10; j++)
                {
                    AutoChicken();
                    Thread.Sleep(10);
                }
            }
       }
        #endregion
    }
    public class PlayerFlask
    {
        public string FlaskName;
        public int Slot;
        public bool isEnabled;

        public Entity Item;
        public FlaskAction FlaskAction1;
        public FlaskAction FlaskAction2;

        public int CurrentCharges;
        public int UseCharges;
        public int MaxCharges;

        private FlaskManagerSettings Settings;
        public void SetSettings(FlaskManagerSettings s)
        {
            Settings = s;
        }
        public void EnableDisableFlask()
        {
            switch (Slot)
            {
                case 0:
                    isEnabled = Settings.flaskSlot1Enable.Value;
                    Settings.flaskSlot1Enable.OnValueChanged += this.EnableDisableFlask;
                    break;
                case 1:
                    isEnabled = Settings.flaskSlot2Enable.Value;
                    Settings.flaskSlot2Enable.OnValueChanged += this.EnableDisableFlask;
                    break;
                case 2:
                    isEnabled = Settings.flaskSlot3Enable.Value;
                    Settings.flaskSlot3Enable.OnValueChanged += this.EnableDisableFlask;
                    break;
                case 3:
                    isEnabled = Settings.flaskSlot4Enable.Value;
                    Settings.flaskSlot4Enable.OnValueChanged += this.EnableDisableFlask;
                    break;
                case 4:
                    isEnabled = Settings.flaskSlot5Enable.Value;
                    Settings.flaskSlot5Enable.OnValueChanged += this.EnableDisableFlask;
                    break;
                default:
                    break;
            }
        }
        ~PlayerFlask()
        {
            switch (Slot)
            {
                case 0:
                    if (Settings.flaskSlot1Enable.OnValueChanged != null)
                        Settings.flaskSlot1Enable.OnValueChanged -= this.EnableDisableFlask;
                    break;
                case 1:
                    if (Settings.flaskSlot2Enable.OnValueChanged != null)
                        Settings.flaskSlot2Enable.OnValueChanged -= this.EnableDisableFlask;
                    break;
                case 2:
                    if (Settings.flaskSlot3Enable.OnValueChanged != null)
                        Settings.flaskSlot3Enable.OnValueChanged -= this.EnableDisableFlask;
                    break;
                case 3:
                    if (Settings.flaskSlot4Enable.OnValueChanged != null)
                        Settings.flaskSlot4Enable.OnValueChanged -= this.EnableDisableFlask;
                    break;
                case 4:
                    if (Settings.flaskSlot5Enable.OnValueChanged != null)
                        Settings.flaskSlot5Enable.OnValueChanged -= this.EnableDisableFlask;
                    break;
                default:
                    break;
            }
        }
    }
    public enum FlaskAction : int
    {
        IGNORE = 0, // ignore mods and don't give error
        NONE, // flask isn't initilized.
        LIFE, //life
        MANA, //mana
        HYBRID, //hybrid flasks
        DEFENSE, //bismuth, jade, stibnite, granite,
                 //amethyst, ruby, sapphire, topaz,
                 // aquamarine, quartz
                 //MODS: iron skin, reflexes, gluttony,
                 // craving, resistance
        SPEEDRUN, //quicksilver, MOD: adrenaline,
        OFFENSE, //silver, sulphur, basalt, diamond, MOD: Fending
        POISON_IMMUNE,// MOD: curing
        FREEZE_IMMUNE,// MOD: heat
        IGNITE_IMMUNE,// MOD: dousing
        SHOCK_IMMUNE,// MOD: grounding
        BLEED_IMMUNE,// MOD: staunching
        CURSE_IMMUNE, // MOD: warding
        UNIQUE_FLASK
    }
    public class FlaskKeys
    {
        public Keys[] k;
        public FlaskKeys(Keys k1, Keys k2, Keys k3, Keys k4, Keys k5)
        {
            k = new Keys[5];
            k[0] = k1;
            k[1] = k2;
            k[2] = k3;
            k[3] = k4;
            k[4] = k5;
        }
    }
}
