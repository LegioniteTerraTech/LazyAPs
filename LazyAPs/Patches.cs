using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace LazyAPs
{
#if STEAM
    public class KickStartLazyAPs : ModBase
    {
        internal static KickStartLazyAPs oInst;

        bool isInit = false;
        Harmony harmonyInst = new Harmony("legionite.lazyaps");
        public override bool HasEarlyInit()
        {
            return true;
        }

        // IDK what I should init here...
        public override void EarlyInit()
        {
            if (oInst == null)
            {
                harmonyInst.PatchAll();
                oInst = this;
                isInit = true;
            }
        }
        public override void Init()
        {
            if (isInit)
                return;
            if (oInst == null)
                oInst = this;

            harmonyInst.PatchAll();
            isInit = true;
        }
        public override void DeInit()
        {
            if (!isInit)
                return;

            harmonyInst.UnpatchAll("legionite.lazyaps");
            isInit = false;
        }
    }
#else
    public class KickStart
    {
        static Harmony harmonyInst = new Harmony("legionite.lazyaps");
        public static void Main()
        {
            harmonyInst.PatchAll();
        }
    }
#endif

    internal static class Patches
    {
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("PrePool")]//On Creation
        private static class PatchAllBlocksForOHKOProjectile
        {
            private static void Postfix(TankBlock __instance)
            {
                var lap = __instance.GetComponent<ModuleLazyAPs>();
                if ((bool)lap)
                    ManLazyAPs.AddBlock(lap);
            }
        }
        [HarmonyPatch(typeof(TankBlock))]
        [HarmonyPatch("OnSpawn")]//On Creation
        private static class PatchAllBlocksForPainting
        {
            private static void Postfix(TankBlock __instance)
            {
                var lap = __instance.GetComponent<ModuleLazyAPs>();
                if ((bool)lap)
                    ManLazyAPs.AddBlock(lap);
                ManLazyAPs.TryAddAPs(__instance);
            }
        }
    }
}
