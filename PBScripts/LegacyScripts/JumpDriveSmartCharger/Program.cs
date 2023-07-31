using PBScripts.AddonModules;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PBScripts.LegacyScripts.SmartJumpDriveCharging
{
    internal class Program : SEProgramBase
    {
        private const float _stopChargingBelow = 0.75f;
        private const float _startChargingAbove = 0.95f;
        private const uint _delayMultiplier = 60;
        private uint _delayCounter;
        private string _echoString = "Starting up... ";

        private Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            // Ensure routine is executed on first run
            _delayCounter = _delayMultiplier;
        }

        public void Main()
        {
            CycleCoroutine(ref _jumpDriveChargeControl, PollAutoBatteries);
            _delayCounter++;
            Echo($"{_echoString} ({_delayCounter})");
        }

        // Routine

        private IEnumerator<object> _jumpDriveChargeControl = null;

        private IEnumerator<object> PollAutoBatteries()
        {
            // Delay
            while (_delayCounter < _delayMultiplier)
                yield return false;
            _delayCounter = 0;

            // Get all batteries
            var batteriesRaw = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(batteriesRaw);
            yield return null;

            // Pick relevant blocks
            var batteries = batteriesRaw.Where(x
                => x.OwnerId == Me.OwnerId &&
                x.IsFunctional && x.Enabled &&
                x.ChargeMode == ChargeMode.Auto)
                .ToHashSet();
            yield return null;

            // Assess batteries
            double totalCharge = 0;
            double currentCharge = 0;
            foreach (var battery in batteries)
            {
                totalCharge += battery.MaxStoredPower;
                currentCharge += battery.CurrentStoredPower;
            }
            double storedRatio = currentCharge / totalCharge;

            // Bail if inside no-change zone
            if (storedRatio > _stopChargingBelow && storedRatio < _startChargingAbove)
                yield break;
            yield return null;

            // Get jumpdrives
            var jumpDrivesRaw = new List<IMyJumpDrive>();
            GridTerminalSystem.GetBlocksOfType(jumpDrivesRaw);
            yield return null;

            // Pick relevant blocks
            var jumpDrives = jumpDrivesRaw.Where(x
                => x.OwnerId == Me.OwnerId &&
                x.CubeGrid == Me.CubeGrid &&
                x.IsFunctional && x.Enabled &&
                x.CurrentStoredPower != x.MaxStoredPower)
                .ToHashSet();
            yield return null;

            // Apply changes
            bool enable = storedRatio > _startChargingAbove;
            _echoString = $"Stored power: {storedRatio}.{Environment.NewLine}Charging is {(enable ? "enabled" : "disabled")}.";
            foreach (var jumpdrive in jumpDrives)
                jumpdrive.Recharge = enable;
        }
    }
}