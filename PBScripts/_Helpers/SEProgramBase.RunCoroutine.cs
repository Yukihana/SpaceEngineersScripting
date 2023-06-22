using System;
using System.Collections.Generic;

namespace PBScripts._Helpers
{
    internal partial class SEProgramBase
    {
        public void RunCoroutine(ref IEnumerator<bool> enumerator, Func<IEnumerator<bool>> enumeratorFactory)
        {
            if (enumerator == null)
                enumerator = enumeratorFactory();
            else if (!enumerator.MoveNext())
            {
                enumerator.Dispose();
                enumerator = null;
            }
        }
    }
}