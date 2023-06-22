using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using VRageMath;

namespace PBScripts.LegacyScripts.LegacySmartVent
{
    internal class Program : SEProgramBase
    {
        // Constructor
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            InitObjects();
        }

        // Main Routine
        public void Main(string argument, UpdateType updateSource)
        {
            DeText = string.Empty;

            // If forced
            if (argument.ToLowerInvariant() == "force")
            {
                SetVentingStatus(true);

                UpdateRoomPressure();
                UpdateTick = UpdateInterval - 1;

                PrintToLCD();
                return;
            }

            // Venting Status Check
            if (Venting)
            {
                CycleOnVent();
            }
            else
            {
                CycleOffVent();
            }

            // Realign Ticks
            if (UpdateTick < 0)
            {
                UpdateTick = UpdateInterval - 1;
            }

            // UpdateScreen
            if (string.IsNullOrWhiteSpace(DeText))
            {
                DeText = null;
            }
            PrintToLCD();
        }

        private void CycleOnVent()
        {
            UpdateRoomPressure();

            // Not Pressurised
            if (RoomPressure < 0)
            {
                SetVentingStatus(false);
                UpdateTick = UpdateInterval - 1;
                return;
            }

            // Check if vent is working
            if (Vent != null)
            {
                if (!Vent.IsWorking)
                {
                    SetVentingStatus(true);
                }
            }

            // Stopper
            if (RoomPressure > RefillThreshold)
            {
                SetVentingStatus(false);
                UpdateTick = UpdateInterval - 1;
            }
        }

        private void CycleOffVent()
        {
            if (UpdateTick > 0)
            {
                UpdateTick--;
                return;
            }

            UpdateRoomPressure();
            CounterReset = true;

            // Not Pressurised
            if (RoomPressure < 0)
            {
                SetVentingStatus(false);
                UpdateTick = UpdateInterval - 1;
                return;
            }

            // Starter
            if (RoomPressure < PressureLow)
            {
                SetVentingStatus(true);
            }
            else
            {
                UpdateTick = UpdateInterval - 1;
            }
        }

        // Init Objects
        private IMyTextPanel LCD;

        private IMyAirVent Vent;
        private string DeText = null;
        private string Counter = string.Empty;

        private readonly int UpdateInterval = 10;
        private readonly int PressureLow = 80;
        private readonly int RefillThreshold = 98;

        private int UpdateTick = 0;
        private bool CounterReset = true;

        private bool Venting = true;
        private double RoomPressure = 0;

        private void InitObjects()
        {
            Vent = GridTerminalSystem.GetBlockWithName("AirVent-Room") as IMyAirVent;
            LCD = GridTerminalSystem.GetBlockWithName("DebugPanel") as IMyTextPanel;
            UpdateRoomPressure();
        }

        // Action
        private void SetVentingStatus(bool state)
        {
            Vent?.ApplyAction(state ? "OnOff_On" : "OnOff_Off");
            Venting = state;
        }

        // Update Pressure in the room
        private void UpdateRoomPressure()
        {
            try
            {
                string d = Vent.DetailedInfo;

                // Check for not pressurised
                if (d.ToLowerInvariant().Contains("not pressurized"))
                {
                    RoomPressure = -1;
                    return;
                }

                string[] dx = d.Split('\n');
                string x = string.Empty;
                foreach (string s in dx)
                {
                    if (s.ToLowerInvariant().Contains("room pressure"))
                    {
                        x = s;
                    }
                }

                if (!string.IsNullOrWhiteSpace(x))
                {
                    string r = System.Text.RegularExpressions.Regex.Match(x, "[0-9]+\x2E[0-9]+").Value;
                    RoomPressure = double.Parse(r);
                }
            }
            catch (Exception e)
            {
                DeText = e.Source + ": " + e.Message + "\n" + e.StackTrace;
                RoomPressure = 50;
            }
        }

        // Indicator
        private string GetIndicator()
        {
            string r = string.Empty;
            int n = (int)(RoomPressure / 2.5);
            for (int i = 0; i < 40; i++)
            {
                if (i == 30)
                {
                    r += ":";
                    continue;
                }
                if (i >= n)
                {
                    r += ",";
                    continue;
                }
                r += i < 30 ? "[" : "]";
            }
            return r;
        }

        // Counter
        private string GetCounter(bool NoChange = false)
        {
            if (NoChange)
            {
                return Counter;
            }

            if (Venting)
            {
                return "----------";
            }

            if (CounterReset || Counter.Length >= 18)
            {
                CounterReset = false;
                return Counter = string.Empty;
            }
            else
            {
                return Counter += RoomPressure < 0 ? "##" : ">>";
            }
        }

        // Print
        private void PrintToLCD()
        {
            if (LCD == null)
            {
                return;
            }

            // Not pressurised
            if (RoomPressure < 0)
            {
                LCD.FontColor = new Color(255, 0, 0);
                LCD.WriteText(
                    "Room is not pressurised."
                    + "\n-" + GetCounter()
                    + "\n-" + GetCounter(true)
                    + "\n-" + GetCounter(true)
                    + "\n-" + GetCounter(true)
                    + "\n-" + GetCounter(true)
                    );
                return;
            }

            // Normal display
            LCD.FontColor = Venting ? new Color(255, 141, 53) : new Color(53, 141, 255);
            LCD.WriteText(
                "Room Pressure - " + RoomPressure
                + "%\n" + GetIndicator()
                + "\nVenting - " + (Venting ? "ON" : "OFF")
                + "\n-" + GetCounter()
                + "\n" + (DeText ?? string.Empty));
        }
    }
}