//--- Aura Script -----------------------------------------------------------
// Treasure Chest Event
//--- Description -----------------------------------------------------------
// All monsters drop treasure chest and the keys needed to open them and
// to get the items inside. There are two types of chests and keys:
// - Ordinary: Contain one item.
// - Premium:  Contain two items, one of which is an advanced play item.
// 
// While ordinary keys can only open ordinary chests, premium keys can open
// both, but those are only supposed to be obtainable with premium currency.
// For that reason premium chests are optional, and you can activate them
// below by uncommenting them. If you do so, you should either activate their
// keys dropping as well, or add the keys to your premium shop. Their id
// is "70156".
// 
// Ref.: http://wiki.mabinogiworld.com/view/Treasure_Chest_Event_(Apr._2009)
//---------------------------------------------------------------------------

public class TreasureChestEventScript : GameEventScript
{
	public const int OrdinaryChestId = 91038;
	public const int PremiumChestId = 91039;
	public const int OrdinaryKeyId = 70155;
	public const int PremiumKeyId = 70156;

	public override void Load()
	{
		SetId("aura_treasure_chest");
		SetName(L("Treasure Chest"));
	}

	public override void AfterLoad()
	{
		ScheduleEvent(DateTime.Parse("2017-01-01 00:00"), DateTime.Parse("2018-01-01 00:00"));
	}

	protected override void OnStart()
	{
		AddGlobalDrop(GlobalDropType.Npcs, new DropData(itemId: OrdinaryChestId, chance: 2)); // Ordinary Chest
		AddGlobalDrop(GlobalDropType.Npcs, new DropData(itemId: OrdinaryKeyId, chance: 2)); // Ordinary Key

		//AddGlobalDrop(GlobalDropType.Npcs, new DropData(itemId: PremiumChestId, chance: 1)); // Premium Chest
		//AddGlobalDrop(GlobalDropType.Npcs, new DropData(itemId: PremiumKeyId, chance: 1)); // Premium Key
	}

	protected override void OnEnd()
	{
		RemoveGlobalDrops();
	}
}

[ItemScript(TreasureChestEventScript.OrdinaryChestId)]
public class OrdinaryChest91038ItemScript : ItemScript
{
	private static readonly List<DropData> rewards = new List<DropData>
	{
		new DropData(itemId: 40012, chance: 1),                   // Bastard Sword
		new DropData(itemId: 40024, chance: 1),                   // Blacksmith Hammer
		new DropData(itemId: 64500, chance: 1, formId: 20130),    // Blacksmith Manual - Blacksmith Hammer
		new DropData(itemId: 64500, chance: 1, formId: 20105),    // Blacksmith Manual - Longsword
		new DropData(itemId: 64500, chance: 1, formId: 20131),    // Blacksmith Manual - Short Sword
		new DropData(itemId: 64500, chance: 1, formId: 20106),    // Blacksmith Manual - Sickle
		new DropData(itemId: 50201, chance: 1, foodQuality: 100), // BnR
		new DropData(itemId: 63029, chance: 1, amount: 5),        // Campfire Kit
		new DropData(itemId: 50180, chance: 1),                   // Chocolate Milk
		new DropData(itemId: 60008, chance: 1, amount: 5),        // Cobweb
		new DropData(itemId: 40041, chance: 1),                   // Combat Wand
		new DropData(itemId: 60020, chance: 1, amount: 10),       // Common Fabric
		new DropData(itemId: 60024, chance: 1, amount: 10),       // Common Leather
		new DropData(itemId: 60028, chance: 1, amount: 10),       // Common Leather Strap
		new DropData(itemId: 60816, chance: 1),                   // Copper Ingot
		new DropData(itemId: 64004, chance: 1, amount: 10),       // Copper Ore
		new DropData(itemId: 15040, chance: 1),                   // Cores Thief Suit (M)
		new DropData(itemId: 50183, chance: 1, foodQuality: 60),  // Devenish Black Beer
		new DropData(itemId: 40095, chance: 1),                   // Dragon Blade
		new DropData(itemId: 60025, chance: 1, amount: 10),       // Fine Leather
		new DropData(itemId: 40040, chance: 1),                   // Fire Wand
		new DropData(itemId: 51031, chance: 1, amount: 2),        // Full Recovery Potion
		new DropData(itemId: 63016, chance: 1, amount: 3),        // Holy Water of Lymilark
		new DropData(itemId: 51001, chance: 1, amount: 10),       // HP 10 Potion
		new DropData(itemId: 51003, chance: 1, amount: 5),        // HP 50 Potion
		new DropData(itemId: 64002, chance: 1, amount: 10),       // Iron Ore
		new DropData(itemId: 50005, chance: 1),                   // Large Meat
		new DropData(itemId: 63000, chance: 1, amount: 3),        // Phoenix Feather
		new DropData(itemId: 63058, chance: 1, amount: 2),        // Recovery Booster
		//new DropData(itemId: 63047, chance: 1),                 // Remote Healer Coupon
		//new DropData(itemId: 63057, chance: 1),                 // Remote Tailor Coupon
		new DropData(itemId: 50123, chance: 1),                   // Roasted Bacon
		new DropData(itemId: 13010, chance: 1),                   // Round Pauldron Chainmail
		new DropData(itemId: 13023, chance: 1),                   // Rose Plate Armor (Type B)
		new DropData(itemId: 60000, chance: 1, formId: 123),      // Sewing Pattern - Ring-Type Mini Leather Dress (F)
		new DropData(itemId: 60000, chance: 1, formId: 107),      // Sewing Pattern - Mongo's Traveler Suit (F)
		new DropData(itemId: 60000, chance: 1, formId: 108),      // Sewing Pattern - Mongo's Traveler Suit (M)
		new DropData(itemId: 60000, chance: 1, formId: 115),      // Sewing Pattern - Mongo's Long Skirt (F)
		new DropData(itemId: 60000, chance: 1, formId: 106),      // Sewing Pattern - Popo's Skirt (F)
		new DropData(itemId: 60000, chance: 1, formId: 112),      // Sewing Pattern - Wizard Hat
		new DropData(itemId: 40026, chance: 1),                   // Sickle
		new DropData(itemId: 64005, chance: 1, amount: 10),       // Silver Ingot
		new DropData(itemId: 64006, chance: 1, amount: 10),       // Silver Ore
		new DropData(itemId: 50006, chance: 1, amount: 5),        // Slice of Meat
		new DropData(itemId: 51011, chance: 1, amount: 10),       // Stamina 10 Potion
		new DropData(itemId: 50164, chance: 1, foodQuality: 100), // T-Bone Steak
		new DropData(itemId: 40002, chance: 1),                   // Wooden Blade
		new DropData(itemId: 40020, chance: 1),                   // Wooden Club
	};

	public override void OnUse(Creature creature, Item item, string parameter)
	{
		var ordinaryKeyId = TreasureChestEventScript.OrdinaryKeyId;
		var premiumKeyId = TreasureChestEventScript.PremiumKeyId;

		if (creature.HasItem(ordinaryKeyId))
		{
			GiveRandomGift(creature);
			creature.RemoveItem(ordinaryKeyId);

			Send.Notice(creature, L("Opened chest with Key to the Ordinary Chest."));
		}
		else if (creature.HasItem(premiumKeyId))
		{
			GiveRandomGift(creature);
			creature.RemoveItem(premiumKeyId);

			Send.Notice(creature, L("Opened chest with Key to the Premium Chest."));
		}
		else
		{
			Send.MsgBox(creature, L("You don't have a key that fits this chest's lock."));
			return;
		}

		creature.Inventory.Remove(item);
	}

	public static void GiveRandomGift(Creature creature)
	{
		var rnd = RandomProvider.Get();
		var item = Item.GetRandomDrop(rnd, rewards);

		creature.AcquireItem(item);
	}
}

[ItemScript(TreasureChestEventScript.PremiumChestId)]
public class PremiumChest91039ItemScript : ItemScript
{
	private static readonly List<DropData> rewards = new List<DropData>
	{
		new DropData(itemId: 51022, chance: 1, amount: 5),     // HP && MP 30 Potion
		new DropData(itemId: 63029, chance: 1, amount: 5),     // Campfire Kit
		new DropData(itemId: 64002, chance: 1, amount: 10),    // Iron Ore
		new DropData(itemId: 64004, chance: 1, amount: 10),    // Copper Ore
		new DropData(itemId: 40013, chance: 1),                // Long Bow
		new DropData(itemId: 40042, chance: 1),                // Cooking Knife
		new DropData(itemId: 64500, chance: 1, formId: 20135), // Blacksmith Manual - Broadsword
		new DropData(itemId: 64500, chance: 1, formId: 20108), // Blacksmith Manual - Spiked Cap
		new DropData(itemId: 60000, chance: 1, formId: 102),   // Sewing Pattern - Magic School Uniform
		new DropData(itemId: 60000, chance: 1, formId: 117),   // Sewing Pattern - Lirina's Long Skirt
		new DropData(itemId: 40031, chance: 1, prefix: 20701), // (Stiff) Crossbow
		new DropData(itemId: 40018, chance: 1),                // Ukulele
		new DropData(itemId: 40041, chance: 1, suffix: 31003), // (Wizard) Combat Wand
	};

	private static readonly List<DropData> rewardsAdv = new List<DropData>
	{
		new DropData(itemId: 51031, chance: 1, amount: 3),                   // Full Recovery Potion
		new DropData(itemId: 63025, chance: 1, amount: 3),                   // Massive Holy Water of Lymilark
		new DropData(itemId: 63044, chance: 1, amountMin: 3, amountMax: 5),  // Party Phoenix Feathers
		new DropData(itemId: 63059, chance: 1),                              // Tendering Potion
	};

	public override void OnUse(Creature creature, Item item, string parameter)
	{
		var premiumKeyId = TreasureChestEventScript.PremiumKeyId;

		if (creature.HasItem(premiumKeyId))
		{
			GiveRandomGifts(creature);
			creature.RemoveItem(premiumKeyId);

			Send.Notice(creature, L("Opened chest with Key to the Premium Chest."));
		}
		else
		{
			Send.MsgBox(creature, L("You don't have a key that fits this chest's lock."));
			return;
		}

		creature.Inventory.Remove(item);
	}

	public static void GiveRandomGifts(Creature creature)
	{
		var rnd = RandomProvider.Get();

		var item1 = Item.GetRandomDrop(rnd, rewards);
		var item2 = Item.GetRandomDrop(rnd, rewardsAdv);

		creature.AcquireItem(item1);
		creature.AcquireItem(item2);
	}
}
