using System;
using System.Collections.Generic;

namespace SuccubusSEMods.Addons
{
    internal static class Coroutines
    {
        public static void CycleCoroutine(ref IEnumerator<object> enumerator, Func<IEnumerator<object>> enumeratorFactory, bool reset = false)
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