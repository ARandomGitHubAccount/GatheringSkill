using System;
using SkillManager;
using UnityEngine;

namespace GatheringSkill;

public static class HoverText
{
    public static string Build(Pickable pickable)
    {
        long pickedTime = pickable.m_nview.GetZDO().GetLong("picked_time", 0L);
        double remainingMinutes = pickable.m_respawnTimeMinutes - (ZNet.instance.GetTime() - new DateTime(pickedTime)).TotalMinutes;
        double remainingRatio = remainingMinutes / pickable.m_respawnTimeMinutes;
        string color = GetColor(remainingRatio);
        int skillLevel = (int)((Player.m_localPlayer.GetSkillFactor("Gathering") * 100));
        // Debug.Log($"remainingRatio: {remainingRatio}");
        // Debug.Log($"playerSkillFactor: {skillLevel}");
        // Debug.Log($"showDetailedEstimateLevel: {TestSkillPlugin.showDetailedEstimateLevel.Value}");
        // Debug.Log($"showSimpleEstimateLevel: {TestSkillPlugin.showSimpleEstimateLevel.Value}");
        if (skillLevel >= GatheringSkillPlugin.showDetailedEstimateLevel.Value)
        {
            return BuildDetailedEstimateString(remainingMinutes, color, pickable.GetHoverName());
        }
        else if (skillLevel >= GatheringSkillPlugin.showSimpleEstimateLevel.Value)
        {
            return BuildSimpleEstimateString(remainingRatio, color, pickable.GetHoverName());
            
        }
        return Localization.instance.Localize(pickable.GetHoverName());
    }

    private static string BuildDetailedEstimateString(double remainingMinutes, string color, string pickableName)
    {
        string phrase;
        if (remainingMinutes < 0.0)
        {
            phrase = $"\n(<color={color}><b>Ready any second now</b></color>)";
        }
        else if (remainingMinutes < 1.0)
        {
            phrase = $"\n(<color={color}><b>Ready in less than a minute</b></color>)";
        }
        else
        {
            phrase = $"\n(<color={color}><b>Ready in {remainingMinutes:F0} minutes</b></color>)";
        }
        return Localization.instance.Localize(pickableName + phrase);
    }

    private static string BuildSimpleEstimateString(double remainingRatio, string color, string pickableName)
    {
        string phrase = "";
        if (remainingRatio < 0)
        {
            phrase = $"\n(<color={color}><b>I can probably wait for this one</b></color>)";
        }
        else if (remainingRatio < 0.25)
        {
            phrase = $"\n(<color={color}><b>Maybe come back in a bit</b></color>)";
        }
        else if (remainingRatio < 0.5)
        {
            phrase = $"\n(<color={color}><b>This one's a long way from being ready</b></color>)";
        }
        else
        {
            phrase = $"\n(<color={color}><b>No idea when this will be ready to pick.</b></color>)";
        }
        Debug.Log($"BuildSimpleEstimateString: {phrase}");
        return Localization.instance.Localize(pickableName + phrase);
    }

    private static string GetColor(double ratio)
    {
        if (ratio < 0)
        {
            return "blue";
        }
        else if (ratio < 0.25)
        {
            return "green";
        }
        else if (ratio < 0.5)
        {
            return "yellow";
        }
        else
        {
            return "red";
        }
    }
}