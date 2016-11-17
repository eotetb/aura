// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Data.Database;

namespace Aura.Data
{
	/// <summary>
	/// Easy access static wrapper for all file databases.
	/// </summary>
	public static class AuraData
	{
		public static ActorDb ActorDb = new ActorDb();
		public static AncientDropDb AncientDropDb = new AncientDropDb();
		public static BankDb BankDb = new BankDb();
		public static ChairDb ChairDb = new ChairDb();
		public static CharCardDb CharCardDb = new CharCardDb();
		public static CharCardSetDb CharCardSetDb = new CharCardSetDb();
		public static CharacterStyleDb CharacterStyleDb = new CharacterStyleDb();
		public static CollectingDb CollectingDb = new CollectingDb();
		public static ColorMapDb ColorMapDb = new ColorMapDb();
		public static CookingDb CookingDb = new CookingDb();
		public static CutscenesDb CutscenesDb = new CutscenesDb();
		public static DungeonDb DungeonDb = new DungeonDb();
		public static DungeonBlocksDb DungeonBlocksDb = new DungeonBlocksDb();
		public static ExpDb ExpDb = new ExpDb();
		//public static FlightDb FlightDb = new FlightDb();
		public static FeaturesDb FeaturesDb = new FeaturesDb();
		public static FishDb FishDb = new FishDb();
		public static FishingGroundsDb FishingGroundsDb = new FishingGroundsDb();
		public static ItemDb ItemDb = new ItemDb();
		public static ItemUpgradesDb ItemUpgradesDb = new ItemUpgradesDb();
		public static KeywordDb KeywordDb = new KeywordDb();
		public static ManualDb ManualDb = new ManualDb();
		public static MotionDb MotionDb = new MotionDb();
		public static NovelOptionDb NovelOptionDb = new NovelOptionDb();
		public static OptionSetDb OptionSetDb = new OptionSetDb();
		public static PetDb PetDb = new PetDb();
		public static PortalDb PortalDb = new PortalDb();
		public static ProductionDb ProductionDb = new ProductionDb();
		public static PropsDb PropsDb = new PropsDb();
		public static PropDropDb PropDropDb = new PropDropDb();
		public static PropDefaultsDb PropDefaultsDb = new PropDefaultsDb();
		public static RaceDb RaceDb = new RaceDb();
		public static RegionDb RegionDb = new RegionDb();
		public static RegionInfoDb RegionInfoDb = new RegionInfoDb();
		public static ShamalaDb ShamalaDb = new ShamalaDb();
		public static ShopLicenseDb ShopLicenseDb = new ShopLicenseDb();
		public static SkillDb SkillDb = new SkillDb();
		//public static SpawnDb SpawnDb = new SpawnDb();
		public static SpeedDb SpeedDb = new SpeedDb();
		public static StatsAgeUpDb StatsAgeUpDb = new StatsAgeUpDb();
		public static StatsBaseDb StatsBaseDb = new StatsBaseDb();
		public static StatsLevelUpDb StatsLevelUpDb = new StatsLevelUpDb();
		//public static TalentExpDb TalentExpDb = new TalentExpDb();
		//public static TalentRankDb TalentRankDb = new TalentRankDb();
		public static TitleDb TitleDb = new TitleDb();
		public static WeatherTableDb WeatherTableDb = new WeatherTableDb();
		public static WeatherDb WeatherDb = new WeatherDb();
	}
}
