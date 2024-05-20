using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using static UnityEngine.ParticleSystem.PlaybackState;
using LTKLib.Utils;
using LTKLib.Patches;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using LTKLib.Extensions;

namespace LTKLib
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class LTKLibMod : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        //public static ConfigEntry<float> StopwatchCustomSlowSpeed;

        public static List<CustomPoint> customPointsData = new();

        private void Awake()
        {
            LTKLibMod.Log = base.Logger;

            // Plugin startup logic
            Log.LogInfo($"{PluginInfo.PLUGIN_GUID} is loaded!");
            // patch with harmony
            startPatching();
            Log.LogInfo($"{PluginInfo.PLUGIN_GUID} finished patching");
        }

        public static int CreateCustomPoint(String name, float width, Color color, bool alwaysAward)
        {
            int id = customPointsData.Count;
            customPointsData.Add(new CustomPoint(name, width, color, alwaysAward));
            return id;
        }

        public static void GiveCustomPoint(int id, int characterNetworkNumber)
        {
            PointBlock pb = new PointBlock(PointBlock.pointBlockType.coin, characterNetworkNumber);
            pb.GetAdditionalData().pointBlockCustomId = id;
            ScoreKeeper.Instance.AwardPoint(pb, true);
        }

        private void startPatching()
        {
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            MethodInfo original;
            MethodInfo patch;

            try
            {
                // =====================================<CustomPointPatch>=====================================
                // (CustomPointPatch) GraphScoreBoard.GetPreinstantiatedPointBlock() postfix
                // change the pointblock when it has custom id
                original = AccessTools.Method(typeof(GraphScoreBoard), "GetPreinstantiatedPointBlock");
                patch = AccessTools.Method(typeof(CustomPointPatch), "GraphScoreBoardGetPreinstantiatedPointBlockPostfix");
                PatchMethod(harmony, original, patch, "postfix");

                // (CustomPointPatch) PointBlock.get_AlwaysAward Prefix
                // return always award from data when it has custom id
                original = AccessTools.Method(typeof(PointBlock), "get_AlwaysAward");
                patch = AccessTools.Method(typeof(CustomPointPatch), "PointBlockget_AlwaysAwardPrefix");
                PatchMethod(harmony, original, patch, "prefix");
            }
            catch (Exception e)
            {
                Log.LogError($"stopped patching because of error:\n{e}");
            }
        }

        // patch a method with a specified patch
        private void PatchMethod(Harmony harmony, MethodInfo original, MethodInfo patch, string patchType)
        {
            string patchName = $"({patch.DeclaringType.Name}) {original.DeclaringType}.{original.Name}() {patchType}";
            try
            {
                switch (patchType)
                {
                    case "prefix":
                        harmony.Patch(original, prefix: new HarmonyMethod(patch));
                        break;
                    case "postfix":
                        harmony.Patch(original, postfix: new HarmonyMethod(patch));
                        break;
                    case "transpiler":
                        harmony.Patch(original, transpiler: new HarmonyMethod(patch));
                        break;
                    default:
                        throw new Exception($"no patch of type {patchType} exists");
                }
                Log.LogInfo($"{patchName} patched successfully");
            }
            catch (Exception e)
            {
                Log.LogError($"{patchName} not patched because of error:\n{e}");
            }
        }
    }
}
