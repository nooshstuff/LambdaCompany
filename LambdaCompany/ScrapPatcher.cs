using UnityEngine.Audio;
using UnityEngine;

namespace LambdaCompany
{
	public static class ScrapPatcher
	{
		internal static Dictionary<string, ScrapEntry> scrapCatelog = new Dictionary<string, ScrapEntry>();
		internal static List<string> _easyBlacklist = ["ExperimentationLevel", "AssuranceLevel", "VowLevel"];

		public static void Activate()
		{
			//Activate Patches for Scrap Items
			On.StartOfRound.Awake += StartOfRound_Awake;
			//audio mixer patch
			On.StartOfRound.Start += StartOfRound_Start;
			On.MenuManager.Start += MenuManager_Start;
		}

		public static ScrapEntry GetEntry(string item) { return scrapCatelog[item]; }

		private static void StartOfRound_Awake(On.StartOfRound.orig_Awake orig, StartOfRound self)
		{
			orig(self);
			foreach (SelectableLevel level in self.levels)
			{
				foreach (ScrapEntry scrap in scrapCatelog.Values)
				{
					if (level.spawnableScrap.Any(x => x.spawnableItem == scrap.item)) continue;
					if (scrap.levelBlacklist != null) {
						if (scrap.levelBlacklist.Contains(level.name)) continue;
					}
					level.spawnableScrap.Add(new SpawnableItemWithRarity() { spawnableItem = scrap.item, rarity = scrap.rarity });
				}
			}

			foreach (ScrapEntry scrap in scrapCatelog.Values)
			{
				if (!self.allItemsList.itemsList.Contains(scrap.item))
				{
					P.Log($"Registered item: {scrap.item.itemName}");
					self.allItemsList.itemsList.Add(scrap.item);
				}
			}
		}

		internal static List<GameObject> prefabsToFix = new List<GameObject>();
		internal static List<GameObject> fixedPrefabs = new List<GameObject>();
		private static void StartOfRound_Start(On.StartOfRound.orig_Start orig, StartOfRound self)
		{
			AudioMixer audioMixer = SoundManager.Instance.diageticMixer;
			List<GameObject> prefabsToRemove = new List<GameObject>();
			for (int i = prefabsToFix.Count - 1; i >= 0; i--)
			{
				GameObject prefab = prefabsToFix[i];
				AudioSource[] audioSources = prefab.GetComponentsInChildren<AudioSource>();
				foreach (AudioSource audioSource in audioSources)
				{
					if (audioSource.outputAudioMixerGroup == null) continue;
					if (audioSource.outputAudioMixerGroup.audioMixer.name == "Diagetic")
					{
						var mixerGroup = audioMixer.FindMatchingGroups(audioSource.outputAudioMixerGroup.name)[0];
						if (mixerGroup != null)
						{
							audioSource.outputAudioMixerGroup = mixerGroup;
							P.Log($"Set mixer group for {audioSource.name} in {prefab.name} to Diagetic:{mixerGroup.name}");
							prefabsToRemove.Add(prefab);
						}
					}
				}
			}
			foreach (GameObject prefab in prefabsToRemove)
			{
				prefabsToFix.Remove(prefab);
			}
			orig(self);
		}
		private static void MenuManager_Start(On.MenuManager.orig_Start orig, MenuManager self)
		{
			orig(self);
			if (self.GetComponent<AudioSource>() == null) return;
			AudioMixer audioMixer = self.GetComponent<AudioSource>().outputAudioMixerGroup.audioMixer;
			List<GameObject> prefabsToRemove = new List<GameObject>();
			for (int i = prefabsToFix.Count - 1; i >= 0; i--)
			{
				GameObject prefab = prefabsToFix[i];
				AudioSource[] audioSources = prefab.GetComponentsInChildren<AudioSource>();
				foreach (AudioSource audioSource in audioSources)
				{
					if (audioSource.outputAudioMixerGroup == null) continue;
					if (audioSource.outputAudioMixerGroup.audioMixer.name == "NonDiagetic")
					{
						var mixerGroup = audioMixer.FindMatchingGroups(audioSource.outputAudioMixerGroup.name)[0];
						if (mixerGroup != null)
						{
							audioSource.outputAudioMixerGroup = mixerGroup;
							P.Log($"Set mixer group for {audioSource.name} in {prefab.name} to NonDiagetic:{mixerGroup.name}");
							prefabsToRemove.Add(prefab);
						}
					}
				}
			}
			foreach (GameObject prefab in prefabsToRemove)
			{
				prefabsToFix.Remove(prefab);
			}
		}
		public static void FixMixerGroups(GameObject prefab)
		{
			if (fixedPrefabs.Contains(prefab)) return;
			fixedPrefabs.Add(prefab);
			prefabsToFix.Add(prefab);
		}
	}

	public struct ScrapEntry
	{
		public ScrapEntry(string assetpath, int rarity, List<string>? levels) : this()
		{
			item = P.assets.LoadAsset<Item>(assetpath);
			this.rarity = rarity;
			this.levelBlacklist = levels;
			ScrapPatcher.FixMixerGroups(item.spawnPrefab);
		}
		internal Item item;
		internal int rarity;
		internal List<string>? levelBlacklist;
	}
}
