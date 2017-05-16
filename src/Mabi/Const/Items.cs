// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;

namespace Aura.Mabi.Const
{
	/// <summary>
	/// Inventory pockets
	/// </summary>
	/// <remarks>
	/// Every item is stored in a specific pocket.
	/// </remarks>
	public enum Pocket : int
	{
		None = 0,
		Cursor = 1,
		Inventory = 2,
		Face = 3,
		Hair = 4,
		Armor = 5,
		Glove = 6,
		Shoe = 7,
		Head = 8,
		Robe = 9,

		// Actual RIGHT hand (left side in inv).
		RightHand1 = 10,
		RightHand2 = 11,

		// Actual LEFT hand (right side in inv).
		LeftHand1 = 12,
		LeftHand2 = 13,

		// Arrows go here, not in the left hand.
		Magazine1 = 14,
		Magazine2 = 15,

		Accessory1 = 16,
		Accessory2 = 17,

		Trade = 19,
		Temporary = 20,
		Quests = 23,
		Trash = 24,
		EntrustmentItem1 = 25,
		EntrustmentItem2 = 26,
		EntrustmentReward = 27,
		BattleReward = 28,
		EnchantReward = 29,
		ManaCrystalReward = 30,
		Falias1 = 32,
		Falias2 = 33,
		Falias3 = 34,
		Falias4 = 35,
		ComboCard = 41,
		ArmorStyle = 43,
		GloveStyle = 44,
		ShoeStyle = 45,
		HeadStyle = 46,
		RobeStyle = 47,
		PersonalInventory = 49,
		VIPInventory = 50,
		FarmStone = 81,
		TailStyle = 90,
		ItemBags = 100,
		ItemBagsMax = 199,

		BardBoardScroll1 = 1000,
		BardBoardScroll2 = 1001,
		BardBoardScroll3 = 1002,
		BardBoardScroll4 = 1003,
		BardBoardScroll5 = 1004,
		BardBoardScroll6 = 1005,
		BardBoardScroll7 = 1006,
		BardBoardScroll8 = 1007,
		BardBoardScroll9 = 1008,
		BardBoardScroll10 = 1009,
		BardBoardScroll11 = 1010,
		BardBoardScroll12 = 1011,
		BardBoardScroll13 = 1012,
		BardBoardScroll14 = 1013,
		BardBoardScroll15 = 1014,
		BardBoardScroll16 = 1015,
	}

	[Flags]
	public enum BagTags
	{
		Equipment = 0x01,
		RecoveryPotion = 0x02,
		Artifact = 0x04,
		AlchemyCrystal = 0x08,
		Herb = 0x10,
		ThreadBall = 0x20,
		Cloth = 0x40,
		Ore = 0x80,
		Gem = 0x100,
		CullinStone = 0x200,
		Firewood = 0x400,
		Fish = 0x800,
		Food = 0x1000,
		Enchants = 0x2000,
		Pass = 0x4000,
		FomorScroll = 0x8000,
		AncientBook = 0x10000,
	}

	/// <summary>
	/// Extensions for Pocket enum.
	/// </summary>
	public static class PocketExtensions
	{
		/// <summary>
		/// Returns true if pocket is an equipment pocket (incl Face and Hair).
		/// </summary>
		/// <param name="pocket"></param>
		/// <returns></returns>
		public static bool IsEquip(this Pocket pocket)
		{
			if ((pocket >= Pocket.Face && pocket <= Pocket.Accessory2) || (pocket >= Pocket.ArmorStyle && pocket <= Pocket.RobeStyle) || pocket == Pocket.TailStyle)
				return true;
			return false;
		}

		/// <summary>
		/// Returns true if pocket is a main equipment pocket (no style, hair, face, or second weapon set).
		/// </summary>
		/// <param name="pocket"></param>
		/// <param name="set"></param>
		/// <returns></returns>
		public static bool IsMainEquip(this Pocket pocket, WeaponSet set)
		{
			if (
				(pocket >= Pocket.Armor && pocket <= Pocket.Robe) ||
				(set == WeaponSet.First && (pocket == Pocket.RightHand1 || pocket == Pocket.LeftHand1 || pocket == Pocket.Magazine1)) ||
				(set == WeaponSet.Second && (pocket == Pocket.RightHand2 || pocket == Pocket.LeftHand2 || pocket == Pocket.Magazine2)) ||
				(pocket >= Pocket.Accessory1 && pocket <= Pocket.Accessory2)
			)
				return true;
			return false;
		}

		/// <summary>
		/// Returns true if pocket is a main armor pocket (no style, hair, face, or weapons).
		/// </summary>
		/// <param name="pocket"></param>
		/// <returns></returns>
		public static bool IsMainArmor(this Pocket pocket)
		{
			return (pocket >= Pocket.Armor && pocket <= Pocket.Robe);
		}

		/// <summary>
		/// Returns true if pocket is between min and max bag.
		/// </summary>
		/// <param name="pocket"></param>
		/// <returns></returns>
		public static bool IsBag(this Pocket pocket)
		{
			return (pocket >= Pocket.ItemBags && pocket <= Pocket.ItemBagsMax);
		}
	}

	/// <summary>
	/// Attack speed of a weapon.
	/// </summary>
	/// <remarks>
	/// Used in ItemOptionInfo.
	/// </remarks>
	public enum AttackSpeed : byte
	{
		VeryFast,
		Fast,
		Normal,
		Slow,
		VerySlow,
	}

	/// <summary>
	/// Information about what can be done with an item.
	/// </summary>
	/// <remarks>
	/// Attr_ActionFlag in item db.
	/// </remarks>
	public enum ItemAction
	{
		/// <summary>
		/// Normal item, no restrictions.
		/// </summary>
		NormalItem = 0,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Tradeable, but loot protected like personal items?
		/// Example: Black Orb Fragment
		/// </remarks>
		StaticItem = 1,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Quest items? Egos?
		/// </remarks>
		ImportantItem = 2,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Account specific?
		/// </remarks>
		AccountPersonalItem = 3,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Special weapons/items for dungeons?
		/// </remarks>
		DungeonItem = 4,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Character specific?
		/// Examples: Elsinore, Training Short Sword, Shop licenses
		/// </remarks>
		CharacterPersonalItem = 5,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Items that can only be used in one region?
		/// Examples: Treasure Chest Key
		/// </remarks>
		RegionFixedItem = 6,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Items that can't be placed in a bank?
		/// Examples: Gems?
		/// </remarks>
		BankBlockedItem = 7,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Events?
		/// </remarks>
		NewBagItem = 8,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Personal item that can't be placed in bank?
		/// </remarks>
		BankBlockedCharacterPersonalItem = 9,

		/// <summary>
		/// Guild Robe
		/// </summary>
		GuildItem = 10,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Not tradeable?
		/// </remarks>
		NotDealItem = 12,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Examples: Brionac, Yui
		/// </remarks>
		Important2Item = 13,

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Item shop items?
		/// </remarks>
		TradeLimitItem = 14,

		/// <summary>
		/// ?
		/// </summary>
		LordKeyItem = 16,
	}

	public enum EgoRace : byte
	{
		None = 0,
		SwordM = 1,
		SwordF = 2,
		BluntM = 3,
		BluntF = 4,
		WandM = 5,
		WandF = 6,
		BowM = 7,
		BowF = 8,
		EirySword = 9,
		EiryBow = 10,
		EiryAxe = 11,
		EiryLute = 12,
		EiryCylinder = 13,
		EiryWind = 14, // ?
		CylinderM = 15,
		CylinderF = 16,
	}

	[Flags]
	public enum ItemFlags : byte
	{
		/// <summary>
		/// ?
		/// </summary>
		Default = 0x01,

		/// <summary>
		/// ?
		/// </summary>
		Unknown2 = 0x02,

		/// <summary>
		/// Item blessed with Holy Water.
		/// </summary>
		Blessed = 0x04,

		/// <summary>
		/// Used in production, e.g. Tailoring.
		/// </summary>
		Incomplete = 0x08,

		/// <summary>
		/// Personalized item, e.g. due to an upgrade.
		/// </summary>
		/// <remarks>
		/// Adds "...-only Item" text, with "..." being the name of
		/// the player, taken from the "OWNER" string value in MetaData1.
		/// </remarks>
		Personalized = 0x10,

		/// <summary>
		/// Removes "(Original)" text?
		/// </summary>
		Reproduction = 0x20,
	}

	/// <summary>
	/// Item's type
	/// </summary>
	/// <remarks>
	/// Basically the "Attr_Type" value found in the client, however,
	/// we made some adjustments in Aura. Officially, Gold Pouches have
	/// type 5. In Aura they have type 1000, we turn them into Sacs for
	/// Gold items. This way we can treat them like any other Sac.
	/// Also, all items that official have type 1000, but an id > 10000
	/// were changed to type 1001, because they aren't sacs.
	/// This difference was important in Aura before we had support for tags.
	/// TODO: Utilize tags more?
	/// </remarks>
	public enum ItemType
	{
		Armor = 0,
		Headgear = 1,
		Glove = 2,
		Shoe = 3,
		Book = 4,
		Currency = 5,
		ItemBag = 6,
		Weapon = 7,
		Weapon2H = 8, // 2H, bows, tools, etc
		Weapon2 = 9, // Ineffective Weapons? Signs, etc.
		Instrument = 10,
		Shield = 11,
		Robe = 12,
		Accessory = 13,
		SecondaryWeapon = 14,
		MusicScroll = 15,
		Manual = 16,
		EnchantScroll = 17,
		CollectionBook = 18,
		ShopLicense = 19,
		FaliasTreasure = 20,
		Kiosk = 21,
		StyleArmor = 22,
		StyleHeadgear = 23,
		StyleGlove = 24,
		StyleShoe = 25,
		ComboCard = 27,
		Unknown2 = 28,
		Hair = 100,
		Face = 101,
		Usable = 501,
		Quest = 502,
		Usable2 = 503,
		Unknown1 = 504,
		Sac = 1000,
		Misc = 1001,
	}

	/// <summary>
	/// The way an item is stacked.
	/// </summary>
	public enum StackType
	{
		/// <summary>
		/// Single item (1, e.g. equipment)
		/// </summary>
		None = 0,

		/// <summary>
		/// Stackable item (1+, e.g. potions)
		/// </summary>
		Stackable = 1,

		/// <summary>
		/// Sac, containing items (0+, e.g. Gold Pouches)
		/// </summary>
		Sac = 2,
	}

	/// <summary>
	/// Type of an instrument item, used to specify the sound.
	/// </summary>
	public enum InstrumentType
	{
		Lute = 0,
		Ukulele = 1,
		Mandolin = 2,
		Whistle = 3,
		Roncadora = 4,
		Flute = 5,
		Chalumeau = 6,

		ToneBottleC = 7,
		ToneBottleD = 8,
		ToneBottleE = 9,
		ToneBottleF = 10,
		ToneBottleG = 11,
		ToneBottleB = 12,
		ToneBottleA = 13,

		Tuba = 18,
		Lyra = 19,
		ElectricGuitar = 20,
		Piano = 21,
		Violin = 22,
		Cello = 23,
		Harp = 24,
		TunedViolin = 25,
		TunedCello = 26,

		TunedFlute = 55,
		TunedWhistle = 56,

		BassDrum = 66,
		Drum = 67,
		Cymbals = 68,

		HandbellC = 69,
		HandbellD = 70,
		HandbellE = 71,
		HandbellF = 72,
		HandbellG = 73,
		HandbellB = 74,
		HandbellA = 75,
		HandbellHighC = 76,

		Xylophone = 77,

		MaleVoiceKr1 = 81,
		MaleVoiceKr2 = 82,
		MaleVoiceKr3 = 83,
		MaleVoiceKr4 = 84,
		FemaleVoiceKr1 = 90,
		FemaleVoiceKr2 = 91,
		FemaleVoiceKr3 = 92,
		FemaleVoiceKr4 = 93,
		FemaleVoiceKr5 = 94,

		MaleChorusVoice = 100,
		FemaleChorusVoice = 110,

		MaleVoiceJp = 120,
		FemaleVoiceJp = 121,
	}

	/// <summary>
	/// Type of upgrade.
	/// </summary>
	public enum UpgradeType
	{
		/// <summary>
		/// Enchant prefix.
		/// </summary>
		/// <remarks>
		/// Shows the effect under the prefix name, if item has a prefix.
		/// </remarks>
		Prefix = 0,

		/// <summary>
		/// Enchant suffix.
		/// </summary>
		/// <remarks>
		/// Shows the effect under the suffix name, if item has a suffix.
		/// </remarks>
		Suffix = 1,

		/// <summary>
		/// Element "enchant".
		/// </summary>
		/// <remarks>
		/// If Unk2 isn't 0x02, the effect will be displayed like an anchant,
		/// i.e. "Fire 1 Increase".
		/// </remarks>
		Elemental = 2,

		/// <summary>
		/// Effect in orange inside the item attributes.
		/// </summary>
		/// <remarks>
		/// Used only by Holy Flame?
		/// </remarks>
		ItemAttribute = 5,

		/// <summary>
		/// Artisan upgrade.
		/// </summary>
		/// <remarks>
		/// Requires LKUP:8:262244 meta data to show up.
		/// </remarks>
		Artisan = 6,

		/// <summary>
		/// Reforge.
		/// </summary>
		/// <remarks>
		/// Requires MTWR:1:1~3 meta data to show up.
		/// </remarks>
		Reforge = 7,
	}

	/// <summary>
	/// Stat the upgrade changes.
	/// </summary>
	public enum UpgradeStat : byte
	{
		HP = 0x00,
		MaxHP = 0x01,
		MP = 0x02,
		MaxMP = 0x03,
		Stamina = 0x04,
		MaxStamina = 0x05,
		Hunger = 0x06,
		Level = 0x07,
		ExperiencePoints = 0x08,
		Age = 0x09,
		STR = 0x0A,
		Dexterity = 0x0B,
		Intelligence = 0x0C,
		Will = 0x0D,
		Luck = 0x0E,
		MinDamage = 0x0F,
		MaxDamage = 0x10,
		MinInjuryRate = 0x11,
		MaxInjuryRate = 0x12,
		Critical = 0x13,
		Protection = 0x14,
		Defense = 0x15,
		DamageBalance = 0x16,

		Fire = 0x1B,
		Ice = 0x1C,
		Lightning = 0x1D,
		Poisoned = 0x1E,
		Deadly = 0x1F,
		PotionPoisoning = 0x20,
		Numb = 0x21,
		Silence = 0x22,
		Petrified = 0x23,
		Coward = 0x24,
		Outraged = 0x25,
		Confused = 0x26,
		Combat2xEXP = 0x27,
		Slow = 0x28,
		Luck2 = 0x29,
		Misfortune = 0x2A,
		LeadersBlessing = 0x2B,
		Explode1 = 0x2C,
		Explode2 = 0x2D,
		Mirage = 0x2E,
		Weak = 0x2F,
		PVPPenaltiy = 0x30,
		Lethargic = 0x31,
		CancelDarkKnight = 0x32,
		CombatEXP1_1 = 0x33,
		MagicDefense = 0x34,
		MagicAttack = 0x35,
		MagicProtection = 0x36,

		MinAttackPower = 0x54,
		MaxAttackPower = 0x55,
		CombatPower = 0x56,
		ExplorationLevel = 0x57,
		PoisonImmunity = 0x58,
		PetrificationImmunity = 0x59,
		LessenManaUsage = 0x5A,
		LessenStaminaUsage = 0x5B,
		ExplosionDefense = 0x5C,
		StompDefense = 0x5D,
		FireAlchemicDamage = 0x5E,
		ClayAlchemicDamage = 0x5F,
		WindAlchemicDamage = 0x60,
		WaterAlchemicDamage = 0x61,
		CrystalMakingSuccessRate = 0x62,
		SynthesisSuccessRate = 0x63,
		FragmentationSuccessRate = 0x64,
		Golem = 0x65,
		BarrierSpikes = 0x66,
		MagicAttack2 = 0x67,
		FrozenBlastDuration = 0x68,
		FrozenBlastRange = 0x69,
		BarrierSpikesDurability = 0x6A,
		StaminaUsage = 0x6B,
		AttackSpeed = 0x6C,
		PiercingLevel = 0x6D,
		TotalLevel = 0x6E,
		ScaleRatio = 0x6F,
		MovementSpeedIncreaseGatheringSpeedIncrease = 0x70,
		MusicBuffEffect = 0x71,
		MusicBuffDuration = 0x72,

		Skill = 0x9B,
		AutoMeleeDefense = 0x9C,
		AutoMagicDefense = 0x9D,
		AutoRangedDefense = 0x9E,
		GatheringSpeed = 0x9F,
		CollectionProbability = 0xA0,
		CollectionQuantity = 0xA1,
		EffectiveRange = 0xA2,
		CastingSpeed = 0xA3,
		MovementSpeed = 0xA4,
		TransportSpeed = 0xA5,
		MaxDurability = 0xA6,
		MinAttackPower2 = 0xA7,
		MaxAttackPower2 = 0xA8,

		Pet = 0xAF,

		Act2ThresholdCutterDamageBoost = 0xB0,
		MarionetteMinDamage = 0xB1,
		MarionetteMaxDamage = 0xB2,
		MarionetteHP = 0xB3,
		MarionetteDefense = 0xB4,
		MarionetteProtection = 0xB5,
		MarionetteMagicDefense = 0xB6,
		ControlMarionetteMinDamage = 0xB7,
		ControlMarionetteMaxDamage = 0xB8,
		ControlCriticalRate = 0xB9,
		ControlCriticalBalanceBoosted = 0xBA,
		IncitingIncidentStunTimeBoosted = 0xBB,
		RisingActionDamageBoosted = 0xBC,
		CrisisRangeBoosted = 0xBD,
		ClimaticCrashDamageBoosted = 0xBE,
		Act2ThresholdCutter3HitDamage = 0xBF,
		Quality = 0xC0,
		AlchemySuccessRateIncreased = 0xC1,
		DualGunMinDamage = 0xC2,
		DualGunMaxDamage = 0xC3,
		UnknownKr1 = 0xC4,
		UnknownKr2 = 0xC5,

		// TODO: Get actual values.
		CommerceDiscount = 0xFE,
		CommerceCredibility = 0xFF,
	}

	/// <summary>
	/// The way the upgrade value is applied.
	/// </summary>
	/// <remarks>
	/// 3 and 4 are types that assumingly have never been used officially,
	/// they set the stat to a fixed amout or percentage of the current value.
	/// </remarks>
	public enum UpgradeValueType : byte
	{
		/// <summary>
		/// Simple +- value.
		/// </summary>
		Value = 0x00,

		/// <summary>
		/// Treats value as a percentage.
		/// </summary>
		/// <remarks>
		/// 1 = 0.1%
		/// 10 = 1.0%
		/// 156 = 15.6%
		/// </remarks>
		Percent = 0x01,

		// v Obscure

		/// <summary>
		/// Fixes stat at value.
		/// </summary>
		Fix = 0x03,

		/// <summary>
		/// Changes stat to a percentage of its current value.
		/// </summary>
		ChangeToPercent = 0x04,
	}

	/// <summary>
	/// Type of check to determine if effect applies.
	/// </summary>
	public enum UpgradeCheckType
	{
		/// <summary>
		/// Check value greater than ...
		/// </summary>
		GreaterThan = 0x04,

		/// <summary>
		/// Check value lower than ...
		/// </summary>
		LowerThan = 0x05,

		/// <summary>
		/// Check value great than or equal ...
		/// </summary>
		GreaterEqualThan = 0x06,

		/// <summary>
		/// Check value lower than or equal ...
		/// </summary>
		LowerEqualThan = 0x07,

		/// <summary>
		/// Check value equal ...
		/// </summary>
		Equal = 0x08,

		/// <summary>
		/// Default value?
		/// </summary>
		None = 0x0A,

		/// <summary>
		/// Skill rank greater than ...
		/// </summary>
		SkillRankEqual = 0x0D,

		/// <summary>
		/// Skill rank greater than ...
		/// </summary>
		SkillRankGreaterThan = 0x0E,

		/// <summary>
		/// Skill rank lower than ...
		/// </summary>
		SkillRankLowerThan = 0x0F,

		/// <summary>
		/// In a state of ...
		/// </summary>
		/// <remarks>
		/// "While in a state of Crazy Chocolate Balls"
		/// Stat+ValueType = condition bit
		/// </remarks>
		InAStateOf = 0x10,

		/// <summary>
		/// While holding title...
		/// </summary>
		/// <remarks>
		/// Stat+ValueType = title id
		/// </remarks>
		HoldingTitle = 0x11,

		/// <summary>
		/// During month ...
		/// </summary>
		/// <remarks>
		/// Stat = 0-6 (Sunday-Saturday)
		/// </remarks>
		WhileBeing = 0x12,

		/// <summary>
		/// If PTJ ... was completed more than ... times
		/// </summary>
		/// <remarks>
		/// Stat = PTJ id
		/// Value = count
		/// </remarks>
		IfPtjCompletedMoreThan = 0x13,

		/// <summary>
		/// When item is broken.
		/// </summary>
		/// <remarks>
		/// Stat = bool: 0=intact, 1=broken
		/// </remarks>
		WhenBroken = 0x14,

		/// <summary>
		/// If supporting race ...
		/// </summary>
		/// <remarks>
		/// Stat = race: 1=Elf, 2=Giant
		/// </remarks>
		WhenSupporting = 0x15,

		/// <summary>
		/// While ... is summoned.
		/// </summary>
		/// <remarks>
		/// Stat: Pet, BarrierSpikes, or Golem
		/// </remarks>
		WhileSummoned = 0x18,
	}

	/// <summary>
	/// Selected weapon set in inventory.
	/// </summary>
	public enum WeaponSet : byte
	{
		First = 0,
		Second = 1,
	}

	public static class UpgradeStatExtension
	{
		public static Stat ToStat(this UpgradeStat upgradeStat)
		{
			switch (upgradeStat)
			{
				case UpgradeStat.MinDamage: return Stat.AttackMinMod;
				case UpgradeStat.MaxDamage: return Stat.AttackMaxMod;
				case UpgradeStat.MinInjuryRate: return Stat.InjuryMinMod;
				case UpgradeStat.MaxInjuryRate: return Stat.InjuryMaxMod;
				case UpgradeStat.Critical: return Stat.CriticalMod;
				case UpgradeStat.DamageBalance: return Stat.BalanceMod;
				case UpgradeStat.Defense: return Stat.DefenseMod;
				case UpgradeStat.Protection: return Stat.ProtectionMod;
				case UpgradeStat.STR: return Stat.StrMod;
				case UpgradeStat.Dexterity: return Stat.DexMod;
				case UpgradeStat.Intelligence: return Stat.IntMod;
				case UpgradeStat.Will: return Stat.WillMod;
				case UpgradeStat.Luck: return Stat.LuckMod;
				case UpgradeStat.MaxHP: return Stat.LifeMaxMod;
				case UpgradeStat.MaxMP: return Stat.ManaMaxMod;
				case UpgradeStat.MaxStamina: return Stat.StaminaMaxMod;
				case UpgradeStat.MagicAttack: return Stat.MagicAttackMod;
				case UpgradeStat.MagicDefense: return Stat.MagicDefenseMod;
				case UpgradeStat.CombatPower: return Stat.CombatPowerMod;
				case UpgradeStat.Lightning: return Stat.ElementLightning;
				case UpgradeStat.Ice: return Stat.ElementIce;
				case UpgradeStat.Fire: return Stat.ElementFire;
				case UpgradeStat.PoisonImmunity: return Stat.PoisonImmuneMod;
				case UpgradeStat.PiercingLevel: return Stat.ArmorPierceMod;
				//case UpgradeStat.Quality: return Stat.;
				//case UpgradeStat.StompDefense: return Stat.;
				//case UpgradeStat.AttackSpeed: return Stat.;
				//case UpgradeStat.LessenManaUsage: return Stat.;
				//case UpgradeStat.StaminaUsage: return Stat.;
				//case UpgradeStat.MusicBuffEffect: return Stat.;
				//case UpgradeStat.MusicBuffDuration: return Stat.;
				//case UpgradeStat.MarionetteMinDamage: return Stat.;
				//case UpgradeStat.MarionetteMaxDamage: return Stat.;
				//case UpgradeStat.MarionetteMagicDefense: return Stat.;
				//case UpgradeStat.MarionetteHP: return Stat.;
				//case UpgradeStat.MarionetteDefense: return Stat.;
				//case UpgradeStat.MarionetteProtection: return Stat.;
				//case UpgradeStat.ControlMarionetteMinDamage: return Stat.;
				//case UpgradeStat.ControlMarionetteMaxDamage: return Stat.;
				//case UpgradeStat.FrozenBlastDuration: return Stat.;
				//case UpgradeStat.FrozenBlastRange: return Stat.;
				//case UpgradeStat.FireAlchemicDamage: return Stat.;
				//case UpgradeStat.WaterAlchemicDamage: return Stat.;
				//case UpgradeStat.WindAlchemicDamage: return Stat.;
				//case UpgradeStat.ClayAlchemicDamage: return Stat.;
				//case UpgradeStat.AlchemySuccessRateIncreased: return Stat.;
				//case UpgradeStat.BarrierSpikes: return Stat.;
				//case UpgradeStat.BarrierSpikesDurability: return Stat.;
				//case UpgradeStat.ControlCriticalRate: return Stat.;
				//case UpgradeStat.ControlCriticalBalanceBoosted: return Stat.;
				//case UpgradeStat.ExplosionDefense: return Stat.;
				//case UpgradeStat.SynthesisSuccessRate: return Stat.;
				//case UpgradeStat.CrystalMakingSuccessRate: return Stat.;
				//case UpgradeStat.FragmentationSuccessRate: return Stat.;

				default:
					return Stat.None;
			}
		}
	}
}
