using PBScripts._HelperMethods;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace PBScripts.SmartJumpDriveCharging
{
    internal class Program : SEProgramBase
    {
        private const float _stopChargingBelow = 0.75f;
        private const float _startChargingAbove = 0.95f;
        private const uint _delayMultiplier = 60;
        private uint _delayCounter = 60;
        private string _echoString = "Waiting... ";

        private Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        {
            RunCoroutine(ref _jumpDriveChargeControl, PollAutoBatteries);
            Echo($"{_echoString} ({_delayCounter})");
        }

        // Routine

        private IEnumerator<bool> _jumpDriveChargeControl = null;

        private IEnumerator<bool> PollAutoBatteries()
        {
            // Delay

            _delayCounter = 0;
            while (_delayCounter < _delayMultiplier)
            {
                _delayCounter++;
                yield return false;
            }

            // Get batteries

            var batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(batteries, x
                => x.Enabled &&
                x.ChargeMode == ChargeMode.Auto &&
                x.OwnerId == Me.OwnerId);
            yield return true;

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
            yield return true;

            // Get jumpdrives

            var jumpDrives = new List<IMyJumpDrive>();
            GridTerminalSystem.GetBlocksOfType(jumpDrives, x
                => x.CubeGrid == Me.CubeGrid &&
                x.OwnerId == Me.OwnerId &&
                x.CurrentStoredPower != x.MaxStoredPower);
            yield return true;

            // Apply changes

            bool enable = storedRatio > _startChargingAbove;
            _echoString = $"Charging was last {(enable ? "enabled" : "disabled")}.";
            foreach (var jumpdrive in jumpDrives)
                jumpdrive.Enabled = enable;
        }
    }
}