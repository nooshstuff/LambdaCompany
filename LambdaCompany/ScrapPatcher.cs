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
			// TODO: fix audio mixer groups https://github.com/EvaisaDev/LethalLib/blob/main/LethalLib/Modules/Utilities.cs
		}

		public static ScrapEntry GetEntry(string item)
		{
			return scrapCatelog[item];
		}

		private static void StartOfRound_Awake(On.StartOfRound.orig_Awake orig, StartOfRound self)
		{
			orig(self);

			foreach (SelectableLevel level in self.levels)
			{
				foreach (ScrapEntry scrap in scrapCatelog.Values)
				{
					if (level.spawnableScrap.Any(x => x.spawnableItem == scrap.item)) continue;
					if (scrap.levelBlacklist == null) continue;
					if (scrap.levelBlacklist.Contains(level.name)) continue;

					level.spawnableScrap.Add(new SpawnableItemWithRarity() { spawnableItem = scrap.item, rarity = scrap.rarity });
				}
			}

			foreach (ScrapEntry scrap in scrapCatelog.Values)
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
