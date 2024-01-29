using UnityEngine.Audio;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Reflection;

namespace LambdaCompany
{
	public static class ScrapPatcher
	{
		internal static Dictionary<string, ScrapEntry> scrapCatelog = new Dictionary<string, ScrapEntry>();

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
					int index = MoonIndex.GetValueOrDefault(level.name, -9);
					if (index == -9) { switch (level.maxScrap) {
							case >= 30:
								index = MoonIndex["ModdedLevel30"]; break;
							case >= 15:
								index = MoonIndex["ModdedLevel15"]; break;
							default:
								index = MoonIndex["ModdedLevel"];	break;
						}
					}
					if (scrap.rarity[index] == -1) continue;
					level.spawnableScrap.Add(new SpawnableItemWithRarity() { spawnableItem = scrap.item, rarity = scrap.rarity[index] });
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
			//Diffusion Profile
			PropertyInfo a = typeof(HDRenderPipeline).GetProperty("currentPipeline", BindingFlags.Static | BindingFlags.NonPublic);
			HDRenderPipeline currentPipeline = (HDRenderPipeline)a.GetValue(typeof(HDRenderPipeline));
			PropertyInfo b = currentPipeline.GetType().GetProperty("defaultDiffusionProfile", BindingFlags.Instance | BindingFlags.NonPublic);
			DiffusionProfileSettings defaultProfileSettings = (DiffusionProfileSettings)b.GetValue(currentPipeline);
			defaultProfileSettings.scatteringDistance = new Color(0.5f, 0.5f, 0.5f, 1f);
			FieldInfo c = defaultProfileSettings.GetType().GetField("profile", BindingFlags.Instance | BindingFlags.NonPublic);
			var defaultProfile = c.GetValue(defaultProfileSettings);
			defaultProfile.GetType().GetField("scatteringDistanceMultiplier").SetValue(defaultProfile, 1f);
			defaultProfile.GetType().GetField("transmissionTint").SetValue(defaultProfile, new Color(1f, 1f, 1f, 1f));
			defaultProfile.GetType().GetField("texturingMode").SetValue(defaultProfile, 1u);
			defaultProfile.GetType().GetField("transmissionMode").SetValue(defaultProfile, 1u);
			defaultProfile.GetType().GetField("thicknessRemap").SetValue(defaultProfile, new Vector2(0f, 5f));
			defaultProfile.GetType().GetField("worldScale").SetValue(defaultProfile, 1f);
			defaultProfile.GetType().GetField("ior").SetValue(defaultProfile, 1.8f);
			P.Log("Patched the default diffusion profile!");

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

		internal static Dictionary<string, int> MoonIndex = new Dictionary<string, int>()
		{
			{"ExperimentationLevel", 0},
			{"AssuranceLevel", 1},
			{"VowLevel", 2},

			{"OffenseLevel", 3},
			{"MarchLevel", 4},

			{"RendLevel", 5},
			{"DineLevel", 6},
			{"TitanLevel", 7},

			{"ModdedLevel", 8},
			{"ModdedLevel15", 9},
			{"ModdedLevel30", 10}
		};
	}

	public struct ScrapEntry
	{
		public ScrapEntry(string assetpath, int[] rarity) : this()
		{
			item = P.assets.LoadAsset<Item>(assetpath);
			this.rarity = rarity;
			ScrapPatcher.FixMixerGroups(item.spawnPrefab);
		}
		internal Item item;
		internal int[] rarity;
	}
}
