using PBScripts.AddonModules;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System.Collections.Generic;
using System.Linq;

namespace PBScripts.LegacyScripts.SmartAirVents
{
    internal class Program : SEProgramBase
    {
        private struct AirVentInfo
        {
            public IMyAirVent Block;
            public string GroupName;
            public bool ToBeIgnored;
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        // Parameters

        private const string _sectionName = "smart-air-vents";
        private const float _refillLowerLimit = 0.75f;
        private const float _refillUpperLimit = 0.90f;
        private const uint _pollTicksPerCycle = 600;
        private uint _pollTicksCurrent = 0;

        private readonly HashSet<AirVentInfo> _vents = new HashSet<AirVentInfo>();
        private readonly HashSet<AirVentInfo> _active = new HashSet<AirVentInfo>();

        public void Main()
        {
            CycleCoroutine(ref _pollTask, () => PollVents());
            Echo(_pollList.Count.ToString());
            CycleCoroutine(ref _activateTask, () => ActivateVents());
            Echo(_activateList.Count.ToString());
            DeactivateVents();
            Echo(_active.Count.ToString());
        }

        // Polling

        private IEnumerator<bool> _pollTask = null;
        private List<IMyAirVent> _pollList = new List<IMyAirVent>();

        private IEnumerator<bool> PollVents()
        {
            foreach (var vent in _vents.ToArray())
            {
                if (vent.Block.Closed || !vent.Block.IsFunctional)
                    _vents.Remove(vent);
            }

            yield return true;

            _pollTicksCurrent = 0;
            GridTerminalSystem.GetBlocksOfType(_pollList);

            while (_pollList.Any())
            {
                yield return true;
                var vent = _pollList[0];
                _pollList.Remove(vent);
                _pollTicksCurrent++;

                if (vent.Closed || !vent.IsFunctional || vent.CubeGrid != Me.CubeGrid)
                    continue;

                _vents.Add(CreateAirVentInfo(vent));
            }

            while (_pollTicksCurrent < _pollTicksPerCycle)
            {
                yield return true;
                _pollTicksCurrent++;
            }
        }

        // Start Vent (Run enumerated, one vent or one group at a time)

        private IEnumerator<bool> _activateTask = null;
        private List<AirVentInfo> _activateList = new List<AirVentInfo>();

        private IEnumerator<bool> ActivateVents()
        {
            _activateList = _vents.ToList();
            while (_activateList.Any())
            {
                yield return true;

                var vent = _activateList[0];
                _activateList.Remove(vent);

                // Start grouped vents together
                if (!string.IsNullOrEmpty(vent.GroupName))
                {
                    List<AirVentInfo> groupVents = new List<AirVentInfo> { vent };
                    groupVents.AddRange(_activateList.Where(x => x.GroupName == vent.GroupName).ToArray());
                    _activateList.RemoveAll(x => groupVents.Contains(x));

                    foreach (var groupVent in groupVents)
                        StartVent(vent);
                    continue;
                }

                // Start solo
                StartVent(vent);
            }
        }

        // Stop Vents (Run every cycle, not enumerated)

        private void DeactivateVents()
        {
            foreach (var vent in _active.ToList())
                StopVent(vent);
        }

        // Vent Controls

        private void StartVent(AirVentInfo vent)
        {
            if (vent.ToBeIgnored)
                return;
            if (IsInvalidated(vent))
                return;
            if (vent.Block.GetOxygenLevel() >= _refillLowerLimit)
            {
                vent.Block.Enabled = false;
                return;
            }

            vent.Block.Depressurize = false;
            vent.Block.Enabled = true;
            _active.Add(vent);
        }

        private void StopVent(AirVentInfo vent)
        {
            if (vent.ToBeIgnored)
                return;
            if (IsInvalidated(vent))
                return;

            // On green reached, shut vent down, and remove from active tracking
            if (vent.Block.GetOxygenLevel() > _refillUpperLimit)
            {
                vent.Block.Enabled = false;
                _active.Remove(vent);
            }
        }

        // Validations and Parsing

        private AirVentInfo CreateAirVentInfo(IMyAirVent vent)
        {
            var properties = GetDataSection(vent, _sectionName);

            string ignoreString;
            string group;
            bool ignore = false;

            if (properties.TryGetValue("ignore", out ignoreString) && ignoreString.ToLowerInvariant().Equals("true"))
                ignore = true;
            if (!properties.TryGetValue("groupname", out group))
                group = string.Empty;

            return new AirVentInfo()
            {
                Block = vent,
                GroupName = group,
                ToBeIgnored = ignore,
            };
        }

        private bool IsInvalidated(AirVentInfo vent)
        {
            bool isInvalid = vent.Block.Closed || !vent.Block.IsFunctional;
            if (isInvalid)
            {
                _vents.Remove(vent);
                _active.Remove(vent);
            }
            return isInvalid;
        }
    }
}