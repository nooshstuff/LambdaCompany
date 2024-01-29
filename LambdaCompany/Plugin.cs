using BepInEx.Configuration;
using BepInEx.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace LambdaCompany
{
	[BepInPlugin(GeneratedPluginInfo.Identifier, GeneratedPluginInfo.Name, GeneratedPluginInfo.Version)]
	public class P : BaseUnityPlugin
	{
		internal static AssetBundle assets;
		internal static P Instance;

		internal static ConfigEntry<float> explodeChance;

		private void Awake()
		{
			Instance = this;

			assets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lambdacompany"));

			explodeChance = Config.Bind("Chance", "Explode Chance", 1f / 3, "Chance for Explosive Barrel to explode when dropped from standing height. (0.0 - 1.0)");

			Scrap();
			NetPatcher.Patch();

			Logger.LogInfo($"Plugin {GeneratedPluginInfo.Identifier} is loaded!");
		}

		private void Scrap()
		{
			Dictionary<string, int[]> rarityMap = new Dictionary<string, int[]> {
				{ "GnomeScrap",				[2, 5, 4,		4, 1,		1, 10, 15,		1, 15, 20] },
				{ "DollScrap",				[30, 10, 10,	-1, -1,		25, 60, -1,		10, 50, 15] },
				{ "LanternScrap",			[-1, -1, -1,	12, 12,		20, 15, 15,		5, 15, 30] },
				{ "ExplosiveBarrelScrap",	[10, 18, -1,	25, -1,		8, -1, 30,		15, 12, 28	] }
			};

			ScrapPatcher.scrapCatelog.Add("GnomeScrap", new ScrapEntry("Assets/Scrap/Gnome/GnomeScrap.asset", rarityMap["GnomeScrap"]));
			ScrapPatcher.scrapCatelog.Add("DollScrap", new ScrapEntry("Assets/Scrap/Doll/DollScrap.asset", rarityMap["DollScrap"]));
			ScrapPatcher.scrapCatelog.Add("LanternScrap", new ScrapEntry("Assets/Scrap/Lantern/LanternScrap.asset", rarityMap["LanternScrap"]));
			ScrapPatcher.scrapCatelog.Add("ExplosiveBarrelScrap", new ScrapEntry("Assets/Scrap/ExplosiveBarrel/ExplosiveBarrelScrap.asset", rarityMap["ExplosiveBarrelScrap"]));

			ScrapPatcher.Activate();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Log(object data, BepInEx.Logging.LogLevel lvl = BepInEx.Logging.LogLevel.Message)
		{
			Instance.Logger.Log(lvl, data);
		}
	}
}
