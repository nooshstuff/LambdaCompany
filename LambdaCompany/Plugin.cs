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
			ScrapPatcher.scrapCatelog.Add("GnomeScrap", new ScrapEntry("Assets/Scrap/Gnome/GnomeScrap.asset", 15, ScrapPatcher._easyBlacklist));
			ScrapPatcher.scrapCatelog.Add("DollScrap", new ScrapEntry("Assets/Scrap/Doll/DollScrap.asset", 30, null));
			ScrapPatcher.scrapCatelog.Add("LanternScrap", new ScrapEntry("Assets/Scrap/Lantern/LanternScrap.asset", 25, ScrapPatcher._easyBlacklist));
			ScrapPatcher.scrapCatelog.Add("ExplosiveBarrelScrap", new ScrapEntry("Assets/Scrap/ExplosiveBarrel/ExplosiveBarrelScrap.asset", 10, null));

			ScrapPatcher.Activate();
		}

		public static bool SpawnInItem(string itemname = "GinoScrap", int value = 999)
		{
			Item? item = ScrapPatcher.GetEntry(itemname).item;
			if (item == null) { return false; }
			var position = StartOfRound.Instance.allPlayerScripts[0].gameplayCamera.transform.position;
			var obj = GameObject.Instantiate(item.spawnPrefab, position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
			var netObject = obj.GetComponent<NetworkObject>();
			var grabble = obj.GetComponent<GrabbableObject>();
			grabble.transform.rotation = Quaternion.Euler(grabble.itemProperties.restingRotation);
			grabble.fallTime = 0f;
			if (value > 0) { grabble.SetScrapValue(value); }
			netObject.Spawn();
			if (item.isScrap)
			{
				RoundManager.Instance.SyncScrapValuesClientRpc([new NetworkObjectReference(netObject)], [value]);
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Log(object data, BepInEx.Logging.LogLevel lvl = BepInEx.Logging.LogLevel.Message)
		{
			Instance.Logger.Log(lvl, data);
		}
	}
}
