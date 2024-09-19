using GUZ.Core.Globals;
using UnityEngine;

namespace GUZ.Core.Debugging
{
    public class PrintStackTrace : MonoBehaviour
    {
        public bool PrintStackTraceNow;

        private void OnValidate()
        {
            if (!PrintStackTraceNow)
            {
                return;
            }

            GameData.GothicVm.PrintStackTrace();
            PrintStackTraceNow = false;
        }
    }
}
