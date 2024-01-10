using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
// #pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace LambdaCompany
{
	public static class ScrapPatcher
	{
		internal static Dictionary<string, ScrapEntry> scrapCatelog = new Dictionary<string, ScrapEntry>();
		internal static List<ScrapEntry> _scrapItems;
		internal static List<string> _easyBlacklist = ["ExperimentationLevel", "AssuranceLevel", "VowLevel"];
		public static void Activate()
		{
			_scrapItems = [];

			_scrapItems.AddRange(scrapCatelog.Values);
			//Activate Patches for Scrap Items
			On.GameNetworkManager.Start += GameNetworkManager_Start;
			On.StartOfRound.Awake += StartOfRound_Awake;
		}

		public static ScrapEntry GetEntry(string item)
		{
			return scrapCatelog[item];
		}

		private static void GameNetworkManager_Start(On.GameNetworkManager.orig_Start orig, GameNetworkManager self)
		{
			foreach (ScrapEntry scrap in _scrapItems)
			{
				self.GetComponent<NetworkManager>().AddNetworkPrefab(scrap.item.spawnPrefab);
			}
		}

		private static void StartOfRound_Awake(On.StartOfRound.orig_Awake orig, StartOfRound self)
		{
			orig(self);

			foreach (SelectableLevel level in self.levels)
			{
				foreach (ScrapEntry scrap in _scrapItems)
				{
					if (level.spawnableScrap.Any(x => x.spawnableItem == scrap.item)) continue;
					if (scrap.levelBlacklist == null) continue;
					if (scrap.levelBlacklist.Contains(level.name)) continue;

					level.spawnableScrap.Add(new SpawnableItemWithRarity() { spawnableItem = scrap.item, rarity = scrap.rarity });
				}
			}

			foreach (ScrapEntry scrap in _scrapItems)
			{
				if (!self.allItemsList.itemsList.Contains(scrap.item))
				{
					Plugin.Log($"Registered item: {scrap.item.itemName}");

					self.allItemsList.itemsList.Add(scrap.item);
				}
			}
		}
	}

	public struct ScrapEntry
	{
		public ScrapEntry(string assetpath, int rarity, List<string>? levels) : this()
		{
			item = Plugin.assets.LoadAsset<Item>(assetpath); ;
			this.rarity = rarity;
			this.levelBlacklist = levels;
		}
		internal Item item;
		internal int rarity;
		internal List<string>? levelBlacklist;
	}
}
