//--- Aura Script -----------------------------------------------------------
// Costume Fishing Event
//--- Description -----------------------------------------------------------
// While the event is active, players get a quest to go talk to Simon. After
// they completed it, they get Style Bait, which they use to fish anywhere,
// for special event items. They can buy more bait from Walter, and they
// can't fish normal items using this bait.
// 
// Reference: http://wiki.mabinogiworld.com/view/Costume_Fishing_Event_(2013)
//---------------------------------------------------------------------------

public class CostumeFishingEventScript : GameEventScript
{
	public override void Load()
	{
		SetId("aura_costume_fishing_event");
		SetName(L("Costume Fishing"));
	}

	public override void AfterLoad()
	{
		ScheduleEvent(DateTime.Parse("2017-01-01 00:00"), DateTime.Parse("2018-01-01 00:00"));
	}

	protected override void OnStart()
	{
		AddEventItemToShop("WalterShop", itemId: 60134, amount: 100); // Style Bait

		AddFishingGround(
			priority: 500000,
			chance: 100,
			bait: 60134, // Style Bait
			locations: new[]
			{
				"Uladh_main/town_TirChonaill/fish_tircho_res_",
				"Uladh_main/field_Tir_S_aa/fish_tircho_stream_",
				"Uladh_main/town_TirChonaill/fish_tircho_stream_",
				"Ula_Emainmacha/_Ula_Emainmacha_E/f",
				"Ula_Emainmacha/_Ula_Emainmacha_lake_S_portal/f",
				"Ula_Emainmacha/_Ula_Emainmacha_lake_water_4/f",
				"Ula_Emainmacha/_Ula_Emainmacha_W/f",
				"Ula_Emainmacha/_Ula_Emainmacha_lake_water_5/f",
				"Ula_Emainmacha/_Ula_Emainmacha_S/f",
				"Ula_Ceann_Harbor/_Ula_Ceann_Harbor_04/f",
				"Ula_Ceann_Harbor/_Ula_Ceann_Harbor_01/f",
				"Ula_Ceann_Harbor/_Ula_Ceann_Harbor_03/f",
				"Ula_Ceann_Harbor/_Ula_Ceann_Harbor_06/f",
				"Ula_Emainmacha_Ceo/_Ula_Emainmacha_Ceo_C1/f",
				"Ula_Emainmacha_Ceo/_Ula_Emainmacha_Ceo_C2/f",
				"Ula_Emainmacha_Ceo/_Ula_Emainmacha_Ceo_E1/f",
				"Ula_Emainmacha_Ceo/_Ula_Emainmacha_Ceo_E2/f",
				"Ula_Emainmacha_Ceo/_Ula_Emainmacha_Ceo_W1/f",
				"Ula_Emainmacha_Ceo/_Ula_Emainmacha_Ceo_W2/f",
				"Iria_Uladh_Ocean_fishingboat_float/_Iria_Uladh_Ocean_fishingboat_float/fishingboat_fish_",
			},
			items: new[] 
			{
				// Bodywear
				new DropData(itemId: 15663, chance: 1), // Bunny Ribbon Suit
				new DropData(itemId: 15674, chance: 1), // Cat Cape Outfit (F)
				new DropData(itemId: 15989, chance: 1), // China 7th Anniversary Outfit (F)
				new DropData(itemId: 15988, chance: 1), // China 7th Anniversary Outfit (M)
				new DropData(itemId: 15673, chance: 1), // Dog Cape Outfit (M)
				new DropData(itemId: 14136, chance: 1), // Gamyu Wizard Robe Armor (F)
				new DropData(itemId: 14135, chance: 1), // Gamyu Wizard Robe Armor (M)

				// Headgear
				new DropData(itemId: 18977, chance: 1), // Bunny Hairpin
				new DropData(itemId: 18973, chance: 1), // China 7th Anniversary Headdress (F)
				new DropData(itemId: 18972, chance: 1), // China 7th Anniversary Headdress (M)

				// Handgear
				new DropData(itemId: 16936, chance: 1), // China 7th Anniversary Ring

				// Footgear
				new DropData(itemId: 17296, chance: 1), // Bunny Dress Shoes
				new DropData(itemId: 17302, chance: 1), // Cat Cape Shoes (F)
				new DropData(itemId: 17821, chance: 1), // China 7th Anniversary Shoes (F)
				new DropData(itemId: 17820, chance: 1), // China 7th Anniversary Shoes (M)
				new DropData(itemId: 17301, chance: 1), // Dog Cape Shoes (M)
				new DropData(itemId: 17994, chance: 1), // Gamyu Wizard Robe Shoes (F)
				new DropData(itemId: 17993, chance: 1), // Gamyu Wizard Robe Shoes (M)

				// Robes
				new DropData(itemId: 19046, chance: 1), // Children's Day Robe (F)
				new DropData(itemId: 19045, chance: 1), // Children's Day Robe (M)
				new DropData(itemId: 19061, chance: 1), // Mushroom Robe

				// Weapons
				new DropData(itemId: 41096, chance: 1), // Broken Umbrella
				new DropData(itemId: 41021, chance: 1), // Clear Umbrella
				new DropData(itemId: 41098, chance: 1), // Crude Clear Umbrella
				new DropData(itemId: 41023, chance: 1), // Frog Umbrella
				new DropData(itemId: 41022, chance: 1), // Lace Umbrella
				new DropData(itemId: 41062, chance: 1), // Old Glory Umbrella
				new DropData(itemId: 41094, chance: 1), // Panda Umbrella
				new DropData(itemId: 41095, chance: 1), // Twinkle Star Umbrella

				// Potions
				new DropData(itemId: 51141, chance: 1, amount: 3), // HP 100 Potion RE
				new DropData(itemId: 51142, chance: 1, amount: 3), // HP 300 Potion RE
				new DropData(itemId: 51143, chance: 1, amount: 3), // HP 500 Potion RE
				new DropData(itemId: 51146, chance: 1, amount: 3), // MP 100 Potion RE
				new DropData(itemId: 51147, chance: 1, amount: 3), // MP 300 Potion RE
				new DropData(itemId: 51148, chance: 1, amount: 3), // MP 500 Potion RE
				new DropData(itemId: 51149, chance: 1, amount: 3), // Stamina 100 Potion RE
				new DropData(itemId: 51150, chance: 1, amount: 3), // Stamina 300 Potion RE
				new DropData(itemId: 51151, chance: 1, amount: 3), // Stamina 500 Potion RE
				new DropData(itemId: 51153, chance: 1, amount: 3), // Wound Remedy 100 Potion RE
				new DropData(itemId: 51154, chance: 1, amount: 3), // Wound Remedy 300 Potion RE
				new DropData(itemId: 51155, chance: 1, amount: 3), // Wound Remedy 500 Potion RE

				// Food (4 Star)
				new DropData(itemId: 50574, chance: 1, foodQuality: 80), // Abb Neagh Carp Stew
				new DropData(itemId: 50169, chance: 1, foodQuality: 80), // Bouillabaisse
				new DropData(itemId: 50271, chance: 1, foodQuality: 80), // Braised Angler Fish
				new DropData(itemId: 50575, chance: 1, foodQuality: 80), // Braised Catfish and Clams
				new DropData(itemId: 50170, chance: 1, foodQuality: 80), // Cheese Fondue
				new DropData(itemId: 50167, chance: 1, foodQuality: 80), // Coq Au Vin
				new DropData(itemId: 50509, chance: 1, foodQuality: 80), // Corn Tea
				new DropData(itemId: 50535, chance: 1, foodQuality: 80), // Crazy Chocolate Ball
				new DropData(itemId: 50166, chance: 1, foodQuality: 80), // Curry and Rice
				new DropData(itemId: 50666, chance: 1, foodQuality: 80), // Fish Soup
				new DropData(itemId: 50126, chance: 1, foodQuality: 80), // Hard-Boiled Egg
				new DropData(itemId: 50537, chance: 1, foodQuality: 80), // Heartbeat Gateau Au Chocolat
				new DropData(itemId: 50503, chance: 1, foodQuality: 80), // Hot Chocolate
				new DropData(itemId: 50531, chance: 1, foodQuality: 80), // Juniper Jelly
				new DropData(itemId: 50280, chance: 1, foodQuality: 80), // Mushroom Cappuccino Soup
				new DropData(itemId: 50279, chance: 1, foodQuality: 80), // Mushroom Consomme
				new DropData(itemId: 50278, chance: 1, foodQuality: 80), // Mushroom Potage
				new DropData(itemId: 50282, chance: 1, foodQuality: 80), // Poisonous Mushroom Stew
				new DropData(itemId: 50269, chance: 1, foodQuality: 80), // Ray Gill Filet
				new DropData(itemId: 50669, chance: 1, foodQuality: 80), // Rock Bream Fish Stew
				new DropData(itemId: 50231, chance: 1, foodQuality: 80), // Shark Fin Soup
				new DropData(itemId: 50197, chance: 1, foodQuality: 80), // Spicy Fish Stew
				new DropData(itemId: 50161, chance: 1, foodQuality: 80), // Steamed Brifne Carp
				new DropData(itemId: 50225, chance: 1, foodQuality: 80), // Steamed Corn
				new DropData(itemId: 50285, chance: 1, foodQuality: 80), // Steamed Mushroom
				new DropData(itemId: 50125, chance: 1, foodQuality: 80), // Steamed Potato
				new DropData(itemId: 50120, chance: 1, foodQuality: 80), // Steamed Rice
				new DropData(itemId: 50670, chance: 1, foodQuality: 80), // Steamed Shark Fin
				new DropData(itemId: 50168, chance: 1, foodQuality: 80), // Steamed Trout
				new DropData(itemId: 50534, chance: 1, foodQuality: 80), // Tear Noodles
				new DropData(itemId: 50505, chance: 1, foodQuality: 80), // Thyme Tea
				new DropData(itemId: 50137, chance: 1, foodQuality: 80), // Vegetable Soup
			}
		);
	}

	protected override void OnEnd()
	{
		RemoveFishingGrounds();
		RemoveEventItemsFromShop("WalterShop");
	}
}

public class CostumeFishingEventQuestScript : QuestScript
{
	public override void Load()
	{
		SetId(1100002);
		SetName(L("Fashion Fishing"));
		SetDescription(L("My new clothes will make a big splash! Can you catch them? - Simon -"));
		SetCancelable(true);

		if (IsEnabled("QuestViewRenewal"))
			SetCategory(QuestCategory.Event);

		SetReceive(Receive.Automatically);
		AddPrerequisite(EventActive("aura_costume_fishing_event"));

		AddObjective("talk", "Talk to Simon in Dunbarton.", 17, 1314, 921, Talk("simon"));

		AddReward(Exp(9900));
		AddReward(Item(60134, 100)); // 100x Style Bait

		AddHook("_simon", "after_intro", AfterIntro);
	}

	private async Task<HookResult> AfterIntro(NpcScript npc, params object[] args)
	{
		if (!npc.Player.QuestActive(this.Id, "talk") || !IsEventActive("aura_costume_fishing_event"))
			return HookResult.Continue;

		npc.Player.FinishQuestObjective(this.Id, "talk");

		npc.Msg(L("Boatloads of clothes were dumped into<br/>rivers and lakes everywhere!!"));
		npc.Msg(L("Quick, complete the quest,<br/>take the fishing bait, and rescue them!"));

		return HookResult.Break;
	}
}
