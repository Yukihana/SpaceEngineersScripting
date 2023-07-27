using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PBScripts._Helpers
{
    internal partial class SEProgramBase
    {
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

        public bool RunCoroutine(ref IEnumerator<object> enumerator, Func<IEnumerator<object>> enumeratorFactory, bool reset = false)
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

        [Obsolete]
        public void CycleCoroutine(ref IEnumerator<bool> enumerator, Func<IEnumerator<bool>> enumeratorFactory, bool reset = false)
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
        public bool RunCoroutineOnce(IEnumerator<bool> enumerator)
        {
            if (enumerator.MoveNext())
                return true;
            enumerator.Dispose();
            return false;
        }
    }
}