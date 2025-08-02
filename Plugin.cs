using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using UnityEngine;
using static LethalLib.Modules.Enemies;

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

        private void Awake()
        {
            RollSpeed = Config.Bind("General", "Roll Speed", 0.6f, "The movement speed of the chair.");

            Log = Logger;
            Assets.PopulateAssets();

            ChairEnemy = Assets.MainAssetBundle.LoadAsset<EnemyType>("Chair.asset");
            var Node = Assets.MainAssetBundle.LoadAsset<TerminalNode>("ChairTN");
            var Keyword = Assets.MainAssetBundle.LoadAsset<TerminalKeyword>("ChairKW");

            RegisterEnemy(ChairEnemy, 100, LethalLib.Modules.Levels.LevelTypes.All, Node, Keyword);

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
