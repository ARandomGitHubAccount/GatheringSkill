using System;
using GatheringSkill;
using HarmonyLib;
using SkillManager;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GatheringSkill;

public class Patches
{
    [HarmonyPatch(typeof(Pickable), "Interact")]
    public static class GardeningSkillIncrease
    {

        [HarmonyPrefix]
        public static void Prefix(Humanoid character, bool ___m_picked, Pickable __instance)
        {
            // TODO: Probably refactor this
            //if I'm interacting and it's not picked, I'm gonna pick it!
            if (!___m_picked)
            {
                //add some skillzz!
                // IncreaseSkill(character, __instance.name);
                Player.m_localPlayer.RaiseSkill("Gathering");
            }
        }
    }
    
    [HarmonyPatch(typeof(Pickable), "Drop")]
    public static class DropMultiply
    {
        [HarmonyPrefix]
        public static void Prefix(GameObject prefab, int offset, ref int stack, ZNetView ___m_nview, bool ___m_picked, Pickable __instance)
        {
            if (!ShouldDrop(___m_nview)) return;
            int maxAdditionalBySkill = Mathf.RoundToInt(Player.m_localPlayer.GetSkillFactor("Gathering") * GatheringSkillPlugin.maxMultiplier.Value);
            Debug.Log($"maxAdditionalBySkill: {maxAdditionalBySkill}");
            int totalStackSize = 1;
            switch (GatheringSkillPlugin.mode.Value)
            {
                case GatheringSkillPlugin.DropMode.PartialRandom:
                    if (maxAdditionalBySkill > 0) totalStackSize += maxAdditionalBySkill - 1;
                    if (Random.value > .5) totalStackSize++;
                    Debug.Log($"PartialRandom - totalStackSize = {totalStackSize}");
                    break;
                case GatheringSkillPlugin.DropMode.Linear:
                    totalStackSize += maxAdditionalBySkill;
                    Debug.Log($"Linear - totalStackSize = {totalStackSize}");
                    break;
                case GatheringSkillPlugin.DropMode.Random:
                    if (maxAdditionalBySkill > 0)
                    {
                        totalStackSize += UnityEngine.Random.Range(0, maxAdditionalBySkill + 1);
                        Debug.Log($"Random - totalStackSize = {totalStackSize}");
                    }
                    break;
            }
            stack = totalStackSize;
        }
        
        private static bool ShouldDrop(ZNetView znet)
        {
            if (
                !znet.IsValid()
                || GatheringSkillPlugin.changingDropAmmounts.Value == GatheringSkillPlugin.Toggle.Off
                // Add check for list of accepted pickables
            )
            {
                Debug.Log("ShouldDrop() returning false");
                return false;
            }
            return true;
        }
    }
    
    [HarmonyPatch(typeof(Pickable), "GetHoverText")]
    public static class GetHoverTextPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref string __result, bool ___m_picked, ZNetView ___m_nview, int ___m_respawnTimeMinutes, Pickable __instance)
        {
            if (GatheringSkillPlugin.enableTimeEstimate.Value != GatheringSkillPlugin.Toggle.On) return;
            if (___m_nview.GetZDO() is null) return;
            if (!___m_picked) return;
            if (__instance.name.ToLower().Contains(("surt"))) return; //TODO: change this to check a list
            if (Player.m_localPlayer.GetSkillFactor("Gathering") <= 0.0) return;
            __result = HoverText.Build(__instance);
        }
    }
    

}