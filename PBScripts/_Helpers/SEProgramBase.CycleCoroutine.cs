using System;
using System.Collections.Generic;

namespace PBScripts._Helpers
{
    internal partial class SEProgramBase
    {
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
    }
}