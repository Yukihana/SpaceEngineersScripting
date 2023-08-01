using PBScripts.AddonModules;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace PBScripts.Cooperative.Maintenance.JumpDriveCharger
{
    internal class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "JumpDriveCharger";

        private Program()
        {
            OutputTitle = $"Maintenance-{SCRIPT_ID}";
            OutputFontColor = new Color(0.5f, 0.25f, 1f);
            TagSelf("MaintenanceScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => ChargeJumpDrives()); }

        private IEnumerator<object> _enumerator = null;

        // TagSelf

        // CycleCoroutine

        // GetOutput

        // Validate

        // ScriptOutput

        // Required

        private readonly Random _random = new Random();
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(1);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromMinutes(2);

        private readonly string IGNORE_MARKER = $"[{SCRIPT_ID}Ignore]";
        private const int BATCH_SIZE = 32;

        private const float LOWER_THRESHOLD = 0.75f;
        private const float UPPER_THRESHOLD = 0.95f;

        private IMyProgrammableBlock _script;
        private readonly List<IMyJumpDrive> _jumpDrives = new List<IMyJumpDrive>();
        private bool _chargingEnabled = true;
        private uint _evaluated = 0;

        // Routine

        private IEnumerator<object> ChargeJumpDrives()
        {
            DateTime startTime = DateTime.Now;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            uint count = 0, full = 0;
            float maximum = 0f, stored = 0f, distance = 0f;
            string valueString;
            yield return null;

            // Get current power storage factor (Defaults to max)
            float gridPowerFactor;
            if (!TryGetScript("GridPowerStorage", "MonitorScript", out _script) ||
                !TryGetOutput(_script, "FilledFactor", out valueString) ||
                !float.TryParse(valueString, out gridPowerFactor))
                gridPowerFactor = 1f; // Set default: Max
            yield return null;

            // Get jumpdrives
            _jumpDrives.Clear();
            GridTerminalSystem.GetBlocksOfType(_jumpDrives);
            yield return null;

            // Determine operation vector (Defaults with true)
            if (gridPowerFactor < LOWER_THRESHOLD)
                _chargingEnabled = false;
            else if (gridPowerFactor > UPPER_THRESHOLD)
                _chargingEnabled = true;

            // Enumerate
            foreach (var jumpDrive in _jumpDrives)
            {
                unchecked { _evaluated++; }
                if (_evaluated % BATCH_SIZE == 0)
                    yield return null;

                if (!ValidateBlockOnSameConstruct(jumpDrive, IGNORE_MARKER))
                    continue;

                count++;
                jumpDrive.Recharge = _chargingEnabled;
                maximum += jumpDrive.MaxStoredPower;
                stored += jumpDrive.CurrentStoredPower;
                distance += jumpDrive.MaxJumpDistanceMeters;

                if (stored == maximum)
                    full++;
            }

            // Calculate
            OutputStats["InputPowerFactor"] = gridPowerFactor.ToString("0.###");
            OutputStats["ChargingEnabled"] = _chargingEnabled.ToString();
            OutputStats["JumpdrivesTotal"] = count.ToString();
            OutputStats["JumpdrivesCharged"] = full.ToString();
            OutputStats["ChargeMaximum"] = maximum.ToString("0.###") + " MWh";
            OutputStats["ChargeStored"] = stored.ToString("0.###") + " MWh";
            OutputStats["ChargeRemaining"] = (maximum - stored).ToString("0.###") + " MWh";
            OutputStats["FilledFactor"] = (stored / maximum).ToString("0.###");
            OutputStats["MaxJumpDistance"] = (distance / 1000).ToString("0.###") + " km";
            OutputStats["UpdateGuid"] = _evaluated.ToString();

            // Output
            DoManualOutput();
            yield return null;

            // On early finish, wait for interval
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            DateTime waitTill = startTime + TimeSpan.FromSeconds(_random.Next(
                (int)INTERVAL_MINIMUM.TotalSeconds,
                (int)INTERVAL_MAXIMUM.TotalSeconds));
            while (DateTime.UtcNow < waitTill)
                yield return null;
        }
    }
}