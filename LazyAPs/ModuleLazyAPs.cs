using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RandomAdditions;

// For Easy Access
public class ModuleLazyAPs : LazyAPs.ModuleLazyAPs { };
namespace LazyAPs
{
    // throw APs on blocks that have this module
    /*
        "ModuleLazyAPs":{ // Add AP meshes to your blocks
          "DoApply": true,          // Do we apply the APs here?
          "CorpToApplyTo": "TAC",   // Official AP Corp to apply to
          "IDStartRange": 0,        // Unofficial AP start range
          "IDEndRange": 0,          // Unofficial AP end range
        },
     */
    public class ModuleLazyAPs : ExtModule
    {
        public bool DoApply = true;
        public string CorpToApplyTo = null;
        public int IDStartRange = 0;
        public int IDEndRange = 0;

        private bool AppliedAPs = false;

        protected override void Pool()
        {
            if (!DoApply)
                return;
            TryApplyAPs();
            if (Singleton.Manager<ManSpawn>.inst.IsTankBlockLoaded(block.BlockType))
            {
                var searchup = Singleton.Manager<ManSpawn>.inst.GetBlockPrefab(block.BlockType);
                if ((bool)searchup)
                {
                    var aps = searchup.GetComponent<ModuleLazyAPs>();
                    if ((bool)aps)
                    {
                        aps.TryApplyAPs();
                    }
                }
            }
        }
        private void TryApplyAPs()
        {
            if (AppliedAPs)
                return;
            TankBlock block = GetComponent<TankBlock>();
            if (!(bool)block)
                return;

            GameObject CopyTarget = GetCopyTarget();
            if (CopyTarget != null)
            {
                if (transform.Find("LazyAP_0"))
                    return;
                int totAP = block.attachPoints.Count();
                List<Renderer> batchRends = new List<Renderer>();
                for (int step = 0; step < totAP; step++)
                {
                    try
                    {
                        Vector3 APPos = block.attachPoints.ElementAt(step);
                        IntVector3 CellPos = block.GetFilledCellForAPIndex(step);

                        var newAP = CopyTarget.transform;
                        newAP.SetParent(transform);
                        newAP.name = "LazyAP_" + step;
                        newAP.gameObject.SetActive(true);
                        newAP.localScale = Vector3.one;
                        newAP.localPosition = CellPos;
                        Vector3 FaceDirect = (APPos - CellPos).normalized;
                        newAP.localRotation = Quaternion.LookRotation(FaceDirect, FaceDirect.y.Approximately(0) ? Vector3.up : Vector3.forward);
                        batchRends.AddRange(newAP.GetComponents<Renderer>());
                        CopyTarget = GetCopyTarget();
                    }
                    catch
                    {
                        try
                        {
                            LogHandler.ThrowWarning("LazyAPs: Invalid AP in " + block.name + "!  AP number " + step);
                        }
                        catch { }
                    }
                }
                byte skin = block.GetSkinIndex();
                var mat = block.GetComponent<MaterialSwapper>();

                FactionSubTypes FST = ManSpawn.inst.GetCorporation(block.BlockType);
                mat.SetupMaterial(block.GetComponent<Damageable>(), FST);
                /*
                Dictionary<Material, List<Renderer>> matRends = (Dictionary<Material, List<Renderer>>)matL.GetValue(mat);
                try
                {
                    foreach (var item in batchRends)
                    {
                        Material SDM = Singleton.Manager<ManTechMaterialSwap>.inst.GetSharedDefaultMaterial(item.sharedMaterial);
                        if (SDM != null)
                        {
                            List<Renderer> list;
                            if (matRends.TryGetValue(SDM, out list))
                            {
                                matRends.Remove(SDM);
                            }
                            else
                            {
                                list = new List<Renderer>();
                            }
                            list.Add(item);
                            item.sharedMaterial = SDM;
                            matRends.Add(SDM, list);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("LazyAPs: " + e);
                }
                matL.SetValue(mat, matRends);
                Renderer[] rendB = (Renderer[])rends.GetValue(mat);
                if (rendB != null)
                    batchRends.AddRange(rendB.ToList());
                rends.SetValue(mat, batchRends.ToArray());
                */
                block.SetSkinIndex(skin);
                Debug.Log("LazyAPs: Set up lazy APs for " + block.name);
            }
            else
            {
                try
                {
                    LogHandler.ThrowWarning("LazyAPs: Could not find target \"_APTemp\" for block " + block.name + "!");
                }
                catch { }
            }
            AppliedAPs = true;
        }
        private GameObject GetCopyTarget()
        {
            GameObject CopyTarget = null;
            if (transform.Find("_APTemp"))
            {
                CopyTarget = Instantiate(transform.Find("_APTemp").gameObject, null);
                Collider[] cols = CopyTarget.GetComponents<Collider>();
                int colCount = cols.Count();
                for (int i = 0; i < colCount; i++)
                {
                    Destroy(cols[i]);
                }
                CopyTarget.SetActive(false);
            }
            if (!CopyTarget)
                Debug.Log("LazyAPs: FAILED TO FETCH CopyTarget!");
            return CopyTarget;
        }


        internal static FieldInfo access = typeof(ManMods).GetField("m_CurrentSession", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static FieldInfo rends = typeof(MaterialSwapper).GetField("m_Renderers", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo matL = typeof(MaterialSwapper).GetField("m_MatRendererLookup", BindingFlags.NonPublic | BindingFlags.Instance);


        internal bool CanApplyAPsToOtherBlock(TankBlock blockToApplyTo)
        {
            if (!(bool)blockToApplyTo)
                return false;
            if (blockToApplyTo.trans.Find("LazyAP_0"))
                return false;
            FactionSubTypes FST = ManSpawn.inst.GetCorporation(block.BlockType);
#if STEAM
            if (ManMods.inst.IsModdedBlock(blockToApplyTo.BlockType))
            {
                if (ManSpawn.inst.GetCorporation(blockToApplyTo.BlockType) != FST)
                    return;
            }
            else
                return;
#else
            int bID = (int)blockToApplyTo.BlockType;
            if (bID > IDEndRange || bID < IDStartRange)
                return false;
#endif
            return true;
        }
        internal void TryApplyAPsToOtherBlock(TankBlock blockToApplyTo)
        {
            if (!(bool)blockToApplyTo)
                return;
            FactionSubTypes FST = ManSpawn.inst.GetCorporation(block.BlockType);
#if STEAM
            if (ManMods.inst.IsModdedBlock(blockToApplyTo.BlockType))
            {
                if (ManSpawn.inst.GetCorporation(blockToApplyTo.BlockType) != FST)
                    return;
            }
            else
                return;
#else
            int bID = (int)blockToApplyTo.BlockType;
            if (bID > IDEndRange || bID < IDStartRange)
                return;
#endif
            GameObject CopyTarget = GetCopyTarget();
            if (CopyTarget != null)
            {
                if (blockToApplyTo.trans.Find("LazyAP_0"))
                    return;
                int totAP = blockToApplyTo.attachPoints.Count();
                List<Renderer> batchRends = new List<Renderer>();
                for (int step = 0; step < totAP; step++)
                {
                    try
                    {
                        Vector3 APPos = blockToApplyTo.attachPoints.ElementAt(step);
                        IntVector3 CellPos = blockToApplyTo.GetFilledCellForAPIndex(step);

                        var newAP = CopyTarget.transform;
                        newAP.parent = blockToApplyTo.transform;
                        newAP.name = "LazyAP_" + step;
                        newAP.gameObject.SetActive(true);
                        newAP.localScale = Vector3.one;
                        newAP.localPosition = CellPos;
                        Vector3 FaceDirect = (APPos - CellPos).normalized;
                        newAP.localRotation = Quaternion.LookRotation(FaceDirect, FaceDirect.y.Approximately(0) ? Vector3.up : Vector3.forward);
                        batchRends.AddRange(newAP.GetComponents<Renderer>());
                        CopyTarget = GetCopyTarget();
                    }
                    catch { }
                }
                byte skin = blockToApplyTo.GetSkinIndex();
                var mat = blockToApplyTo.GetComponent<MaterialSwapper>();
                foreach (var item in batchRends)
                {
                    item.enabled = true;
                }
                mat.SetupMaterial(blockToApplyTo.GetComponent<Damageable>(), FST);
                Debug.Log("LazyAPs: ManLazyAPs - Applied " + totAP +  " APs with renderer count " + batchRends.Count + " to "+ blockToApplyTo.name);
                
                Dictionary<Material, List<Renderer>> matRends = (Dictionary<Material, List<Renderer>>)matL.GetValue(mat);
                List<Renderer> list;
                foreach (var item in batchRends)
                {
                    Material SDM = Singleton.Manager<ManTechMaterialSwap>.inst.GetSharedDefaultMaterial(item.sharedMaterial);
                    if (SDM != null)
                    {
                        if (matRends.TryGetValue(SDM, out list))
                        {
                            matRends.Remove(SDM);
                        }
                        else
                        {
                            list = new List<Renderer>();
                        }
                        list.Add(item);
                        item.sharedMaterial = SDM;
                        matRends.Add(SDM, list);
                    }
                }
                matL.SetValue(mat,matRends);
                Renderer[] rendB = (Renderer[])rends.GetValue(mat);
                if (rendB != null)
                    batchRends.AddRange(rendB.ToList());
                rends.SetValue(mat, batchRends.ToArray());
                
                blockToApplyTo.SetSkinIndex(skin);
                Debug.Log("LazyAPs: Set up lazy APs for " + blockToApplyTo.name);
            }
            AppliedAPs = true;
        }
    }

    internal class ManLazyAPs : MonoBehaviour
    {
        private static ManLazyAPs inst;
        private static List<ModuleLazyAPs> Replacables = new List<ModuleLazyAPs>();
        private static List<KeyValuePair<ModuleLazyAPs, TankBlock>> Queue = new List<KeyValuePair<ModuleLazyAPs, TankBlock>>();
        private static bool nextFrameDoAdd = false;

        public static void Init()
        {
            if (inst)
                return;
            inst = new GameObject("ManLazyAPs").AddComponent<ManLazyAPs>();
        }

        public static void AddBlock(ModuleLazyAPs rp)
        {
            try
            {
                ModuleLazyAPs match = Replacables.Find(delegate (ModuleLazyAPs cand) { return cand.CorpToApplyTo == rp.CorpToApplyTo; });
                if (!match)
                {
                    Replacables.Add(rp);
                    Debug.Log("LazyAPs: ManLazyAPs - Registered " + rp.name);
                }
                else
                {
                    Replacables.Remove(match);
                    Replacables.Add(rp);
                    Debug.Log("LazyAPs: ManLazyAPs - ReRegistered " + rp.name);
                }
            }
            catch { }
        }
        public static void RemoveAllBlocks()
        {
            Queue.Clear();
            Replacables.Clear();
        }

        public static void TryAddAPs(TankBlock blockToAddTo)
        {
            try
            {
                if ((int)blockToAddTo.BlockType < Enum.GetValues(typeof(BlockTypes)).Length)
                    return;
                foreach (var item in Replacables)
                {
                    try
                    {
                        if (item.CanApplyAPsToOtherBlock(blockToAddTo))
                        {
                            KeyValuePair<ModuleLazyAPs, TankBlock> pair = new KeyValuePair<ModuleLazyAPs, TankBlock>(item, blockToAddTo);
                            Queue.Add(pair);
                        }
                    }
                    catch { }
                }
                if (nextFrameDoAdd)
                    return;
                Init();
                inst.Invoke("DoAddAPs", 0.001f);
                nextFrameDoAdd = true;
            }
            catch { }
        }
        public void DoAddAPs()
        {
            try
            {
                foreach (var item in Queue)
                {
                    try
                    {
                        item.Key.TryApplyAPsToOtherBlock(item.Value);
                    }
                    catch { }
                }
            }
            catch { }
            nextFrameDoAdd = false;
        }
    }

    // Do not use any of these alone. They will do nothing useful.
    /// <summary>
    /// Used solely for this mod for modules compat with MP
    /// </summary>
    public class ExtModule : MonoBehaviour
    {
        public TankBlock block { get; private set; }
        public Tank tank => block.tank;
        public ModuleDamage dmg { get; private set; }

        /// <summary>
        /// Always fires first before the module
        /// </summary>
        public void OnPool()
        {
            if (!block)
            {
                block = gameObject.GetComponent<TankBlock>();
                if (!block)
                {
                    LogHandler.ThrowWarning("LazyAPs: Modules must be in the lowest JSONBLOCK/Deserializer GameObject layer!\nThis operation cannot be handled automatically.\nCause of error - Block " + gameObject.name);
                    enabled = false;
                    return;
                }
                dmg = gameObject.GetComponent<ModuleDamage>();
                try
                {
                    block.AttachEvent.Subscribe(OnAttach);
                    block.DetachEvent.Subscribe(OnDetach);
                }
                catch
                {
                    Debug.LogError("LazyAPs: ExtModule - TankBlock is null");
                    enabled = false;
                    return;
                }
                enabled = true;
                Pool();
            }
        }
        protected virtual void Pool() { }
        public virtual void OnAttach() { }
        public virtual void OnDetach() { }


    }
}
