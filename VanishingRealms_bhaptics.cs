using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MelonLoader;
using HarmonyLib;

using MyBhapticsTactsuit;

namespace VanishingRealms_bhaptics
{
    public class VanishingRealms_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr;

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }

        /*
        [HarmonyPatch(typeof(VertigoPlayer), "Die", new Type[] { })]
        public class bhaptics_PlayerDies
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }
        */

    }
}
