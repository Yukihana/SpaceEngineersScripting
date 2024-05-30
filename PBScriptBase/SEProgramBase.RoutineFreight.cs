using System.Collections.Generic;
using System.Linq;

namespace PBScriptBase
{
    // Routine Freight :
    // Runs multiple non-cyclic routines
    // No renewals for these coroutines.
    // Replacement must dispose the previous.
    public partial class SEProgramBase
    {
        public readonly Dictionary<string, IEnumerator<object>> _routineRunnerPendingList
            = new Dictionary<string, IEnumerator<object>>();

        public readonly List<string> _routineRunnerRemovalList
            = new List<string>();

        public void QueueRoutine(string identifier, IEnumerator<object> routine)
        {
            if (routine == null)
                return;

            if (_routineRunnerPendingList.ContainsKey(identifier))
                _routineRunnerPendingList[identifier]?.Dispose();

            _routineRunnerPendingList[identifier] = routine;
        }

        public bool ProcessPendingRoutines()
        {
            foreach (var routine in _routineRunnerPendingList)
            {
                if (routine.Value.MoveNext())
                    continue;

                routine.Value.Dispose();
                _routineRunnerRemovalList.Add(routine.Key);
            }

            foreach (var routine in _routineRunnerRemovalList)
                _routineRunnerPendingList.Remove(routine);
            _routineRunnerRemovalList.Clear();

            return _routineRunnerPendingList.Any();
        }
    }
}