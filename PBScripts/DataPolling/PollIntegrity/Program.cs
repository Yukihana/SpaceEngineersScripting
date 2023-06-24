﻿using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System;
using System.Text;
using VRage.Game.ModAPI.Ingame;

namespace PBScripts.DataPolling.PollIntegrity
{
    internal class Program : SEProgramBase
    {
        public Program()
        { Runtime.UpdateFrequency = UpdateFrequency.Update100; }

        public void Main()
        { RunCoroutine(ref _enumerator, () => PollIntegrity()); }

        private IEnumerator<bool> _enumerator = null;
        private readonly TimeSpan INTERVAL_FIXED_MINIMUM = TimeSpan.FromMinutes(1);
        private const int BATCH_SIZE = 20;
        private readonly Random _random = new Random();
        private const int INTERVAL_APPENDED_MAXIMUM = 60;

        // Coroutine

        private IEnumerator<bool> PollIntegrity()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            ulong evaluated = 0;
            int count = 0;

            // Enumerate all blocks
            var Blocks = new HashSet<IMySlimBlock>();
            var pendingEnum = new Queue<IMySlimBlock>();

            // Starter blocks
            var startPosition = Me.Position;
            var firstblock = Me.CubeGrid.GetCubeBlock(startPosition);
            pendingEnum.Enqueue(firstblock);

            // Enumerate
            while (pendingEnum.Count > 0)
            {
                unchecked { evaluated++; }
                var block = pendingEnum.Dequeue();
            }

            // Calculate

            // Prepare stats
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"[PolledStatistics:GridOxygen]");
            sb.AppendLine();
            sb.AppendLine($"[GridOxygenStored:{storedOxygen}]");
            sb.AppendLine($"[GridOxygenMaximum:{maxOxygen}]");
            sb.AppendLine($"[GridOxygenFactor:{oxygenPercent}]");
            sb.AppendLine($"[GridOxygenTankCount:{count}]");
            string output = sb.ToString();
            yield return true;

            // Post stats
            IMyTextSurface monitor = Me.GetSurface(0);

            monitor.ContentType = ContentType.TEXT_AND_IMAGE;
            monitor.FontColor = new VRageMath.Color(0f, 1f, 0.5f);
            monitor.WriteText(output);
            Me.CustomData = output;
            yield return true;

            // On early finish, wait for interval
            while (DateTime.UtcNow - startTime < INTERVAL_FIXED_MINIMUM)
                yield return true;

            // Followed that by an additional random interval
            startTime = DateTime.UtcNow;
            TimeSpan randomInterval = TimeSpan.FromSeconds(_random.Next(0, INTERVAL_APPENDED_MAXIMUM));
            while (DateTime.UtcNow - startTime < randomInterval)
                yield return true;
        }
    }
}