using System;
using System.Collections.Generic;

namespace PBScriptBase
{
    public partial class SEProgramBase
    {
        public readonly Dictionary<string, IEnumerator<object>> _enumerators
            = new Dictionary<string, IEnumerator<object>>();

        public void CycleCoroutine(ref IEnumerator<object> enumerator, Func<IEnumerator<object>> enumeratorFactory, bool reset = false)
        {
            if (enumerator == null)
                enumerator = enumeratorFactory();
            else if (!enumerator.MoveNext() || reset)
            {
                enumerator.Dispose();
                enumerator = null;
            }
        }

        [Obsolete]
        public bool RunCoroutineEx(ref IEnumerator<object> enumerator, Func<IEnumerator<object>> enumeratorFactory, bool reset = false)
        {
            if (reset)
            {
                enumerator?.Dispose();
                enumerator = enumeratorFactory();
            }

            if (enumerator.MoveNext())
                return true;

            enumerator.Dispose();
            enumerator = null;
            return false;
        }

        public bool RunCoroutine(IEnumerator<object> enumerator)
        {
            if (enumerator.MoveNext())
                return true;
            enumerator.Dispose();
            return false;
        }
    }
}