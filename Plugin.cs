using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using static LethalLib.Modules.Enemies;
using static LethalLib.Modules.Levels;

namespace TheRollingChair
{
    [BepInPlugin(Plugin.GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "ccode.chair";
        public const string NAME = "Rolling Chair";
        public const string VERSION = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(GUID);

        public static ManualLogSource Log;

        public static EnemyType ChairEnemy;

        public static ConfigEntry<float> RollSpeed;

        public static ConfigEntry<float> RollVolume;

        public static ConfigEntry<string> Levels;

        private void Awake()
        {
            RollSpeed = Config.Bind("General", "Roll Speed", 0.6f, "The movement speed of the chair.");
            RollVolume = Config.Bind("General", "Roll Volume", 0.5f, "The volume of the rolling sound effect. This setting is client side.");
            Levels = Config.Bind("General", "Moons", "All:75", "Moons that it will spawn on. Format as: \"MoonName:SpawnWeight\".");

            (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = SolveLevels(Levels.Value);

            Log = Logger;
            Assets.PopulateAssets();

            ChairEnemy = Assets.MainAssetBundle.LoadAsset<EnemyType>("Chair.asset");
            var Node = Assets.MainAssetBundle.LoadAsset<TerminalNode>("ChairTN");
            var Keyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("ChairKW");

            RegisterEnemy(ChairEnemy, spawnRateByLevelType, spawnRateByCustomLevelType, Node, Keyword);

            NetworkPrefabs.RegisterNetworkPrefab(ChairEnemy.enemyPrefab);

            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        (Dictionary<LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) SolveLevels(string config)
        {
            Dictionary<LevelTypes, int> spawnRateByLevelType = new Dictionary<LevelTypes, int>();
            Dictionary<string, int> spawnRateByCustomLevelType = new Dictionary<string, int>();

            string[] configSplit = config.Split(';');

            foreach (string entry in configSplit)
            {
                string[] levelDef = entry.Trim().Split(':');

                if (levelDef.Length != 2)
                {
                    continue;
                }

                int spawnrate = 0;

                if (!int.TryParse(levelDef[1], out spawnrate))
                {
                    continue;
                }

                if (Enum.TryParse<LevelTypes>(levelDef[0], true, out LevelTypes levelType))
                {
                    spawnRateByLevelType[levelType] = spawnrate;
                    Logger.LogInfo($"Registered spawn rate for level type {levelType} to {spawnrate}");
                }
                else
                {
                    spawnRateByCustomLevelType[levelDef[0]] = spawnrate;
                    Logger.LogInfo($"Registered spawn rate for custom level type {levelDef[0]} to {spawnrate}");
                }
            }


            return (spawnRateByLevelType, spawnRateByCustomLevelType);
        }
    }

    public static class Assets
    {
        public static AssetBundle MainAssetBundle = null;
        public static void PopulateAssets()
        {
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            MainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "chairassets"));
            if (MainAssetBundle == null)
            {
                Plugin.Log.LogError("Failed to load custom assets.");
                return;
            }
        }
    }
}
