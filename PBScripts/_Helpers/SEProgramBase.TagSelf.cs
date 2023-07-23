using System;

namespace PBScripts._Helpers
{
    internal partial class SEProgramBase
    {
        public void TagSelf(string tag)
        {
            string tagString = $"[{tag}]";
            string data = Me.CustomData;
            if (!data.Contains(tagString))
                Me.CustomData = data.TrimEnd() + Environment.NewLine + tagString;
        }
    }
}