using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace LambdaCompany
{
	internal static class NetPatcher
	{
		internal static List<GameObject> netPrefabs = [];

		private static void Collect()
		{
			// Scrap
			foreach (ScrapEntry scrap in ScrapPatcher.scrapCatelog.Values)
			{
				if (!netPrefabs.Contains(scrap.item.spawnPrefab)) netPrefabs.Add(scrap.item.spawnPrefab);
			}
		}

		private static void GameNetworkManager_Start(On.GameNetworkManager.orig_Start orig, GameNetworkManager self)
		{
			orig(self);
			foreach (GameObject obj in netPrefabs)
			{
				if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(obj)) NetworkManager.Singleton.AddNetworkPrefab(obj);
			}
		}

		// if you ever add any event managers make sure to patch StartOfRound.Awake() to spawn them https://github.com/OE100/LuckyDice/blob/master/LuckyDice/Patches/NetworkStuffPatch.cs

		internal static void Patch()
		{
			Collect();
			On.GameNetworkManager.Start += GameNetworkManager_Start;

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
}
