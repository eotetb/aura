//--- Aura Script -----------------------------------------------------------
// Advent Calendar Event
//--- Description -----------------------------------------------------------
// Players receive one present per day from Coco, who is at the Unicorn
// statue in Dunbarton. Only one character per account can get a present
// every day.
//---------------------------------------------------------------------------

public class AdventCalendarEventScript : GameEventScript
{
	private List<Prop> props = new List<Prop>();

	public override void Load()
	{
		SetId("aura_advent_calendar");
		SetName(L("Advent Calendar"));
	}

	public override void AfterLoad()
	{
		ScheduleEvent(DateTime.Parse("2017-01-01 00:00"), DateTime.Parse("2018-01-01 00:00"));
	}

	protected override void OnStart()
	{
		props.Add(SpawnProp(44784, 14, 36085, 32912, 2.9f));
		props.Add(SpawnProp(44881, 14, 35925, 32912, 2.5f));
	}

	protected override void OnEnd()
	{
	}

	protected override void CleanUp()
	{
		foreach (var prop in props)
			RemoveProp(prop.EntityId);
	}
}

public class AdventCalendarQuestScript : QuestScript
{
	public override void Load()
	{
		SetId(1100000);
		SetName(L("Advent Calendar"));
		SetDescription(L("Would you visit me in Dunbarton, I have something for you. - Coco -"));
		SetCancelable(true);

		if (IsEnabled("QuestViewRenewal"))
			SetCategory(QuestCategory.Event);

		SetReceive(Receive.Automatically);
		AddPrerequisite(EventActive("aura_advent_calendar"));

		AddObjective("talk", "Talk to Coco at the unicorn statue in Dunbarton", 14, 36005, 33000, Talk("coco"));

		AddReward(Exp(10000));
	}
}

public class AdventCalenderEventNpcScript : NpcScript
{
	private const string LastReceivedVar = "AdventCalendarEventLastGift2016";

	static List<DropData> gifts = new List<DropData>
	{
		new DropData(itemId: 50206, chance: 6, foodQuality: 100), // Chocolate
		new DropData(itemId: 50177, chance: 6, foodQuality: 100), // Chocolate Chip Cookie
		new DropData(itemId: 50503, chance: 6, foodQuality: 100), // Hot Chocolate
		new DropData(itemId: 63295, chance: 6, amountMin: 1, amountMax: 5),  // Christmas Campfire Kit
		
		new DropData(itemId: 51125, chance: 4, amountMin: 2, amountMax: 20), // HP 100 Potion SE
		new DropData(itemId: 51126, chance: 4, amountMin: 2, amountMax: 20), // HP 300 Potion SE
		new DropData(itemId: 51130, chance: 4, amountMin: 2, amountMax: 20), // MP 100 Potion SE
		new DropData(itemId: 51131, chance: 4, amountMin: 2, amountMax: 20), // MP 300 Potion SE
		new DropData(itemId: 51133, chance: 4, amountMin: 2, amountMax: 20), // Stamina 100 Potion SE
		new DropData(itemId: 51134, chance: 4, amountMin: 2, amountMax: 20), // Stamina 300 Potion SE

		new DropData(itemId: 63037, chance: 4), // Dye Ampoule
		new DropData(itemId: 63030, chance: 4), // Fixed Color Dye Ampoule
		new DropData(itemId: 63030, chance: 4, color1: 0x6100b902), // Fixed Color Dye Ampoule (flashy red/green)
		new DropData(itemId: 63030, chance: 4, color1: 0xFFFFFFFF), // Fixed Color Dye Ampoule (white)
		new DropData(itemId: 63030, chance: 4, color1: 0xFF000000), // Fixed Color Dye Ampoule (black)
		new DropData(itemId: 63030, chance: 4, color1: 0xFFA5F2F3), // Fixed Color Dye Ampoule (ice blue)

		new DropData(itemId: 18030, chance: 4), // Short Reindeer Antler Headband
		new DropData(itemId: 18031, chance: 4), // Reindeer Antler Headband
		new DropData(itemId: 18032, chance: 4), // Long Reindeer Antler Headband
		new DropData(itemId: 28601, chance: 4), // Shaved Ice Bowl Hat
		new DropData(itemId: 18123, chance: 4), // Kirin's Winter Angel Cap
		new DropData(itemId: 19067, chance: 4), // Reindeer Robe

		new DropData(itemId: 80290, chance: 2), // Lovely Snowflake Coat (M)
		new DropData(itemId: 80291, chance: 2), // Lovely Snowflake Coat (F)
		new DropData(itemId: 92715, chance: 1), // Santa Outfit Box
	};

	public override void Load()
	{
		SetRace(10001);
		SetName("_coco@advent_calendar");
		SetBody(weight: 0.7f, upper: 0.7f, lower: 0.7f);
		SetFace(skinColor: 23, eyeType: 152, eyeColor: 38, mouthType: 48);

		if (IsEventActive("aura_advent_calendar"))
			SetLocation(14, 36005, 33000, 64);

		EquipItem(Pocket.Face, 3907, 0x00366969, 0x0094B330, 0x00737474);
		EquipItem(Pocket.Hair, 4933, 0x00E3CCBA, 0x00E3CCBA, 0x00E3CCBA);
		EquipItem(Pocket.Armor, 15779, 0x00EBD2D2, 0x00FFFFFF, 0x00C6794A);
		EquipItem(Pocket.Shoe, 17369, 0x00EBD2D2, 0x00FFFFFF, 0x00C6794A);
	}

	protected override async Task Talk()
	{
		if (Player.QuestActive(1100000, "talk"))
			Player.FinishQuestObjective(1100000, "talk");

		if (!IsEventActive("aura_advent_calendar"))
		{
			Msg(L("I'm sorry, the event is over."));
			End();
		}

		var now = DateTime.Now;
		var nowDay = now.Day;
		var lastReceivedDay = GetLastReceivedDay(now);

		if (nowDay == lastReceivedDay)
		{
			Msg(L("Oh, <username/>, you're back already?<br/>You already received your gift for today,<br/>please come back tomorrow."));
			End();
		}

		if (lastReceivedDay == 0)
		{
			Msg(L("Welcome, <username/>!<br/>I'm glad you're here, I have something for you."));
			Msg(L("I'd like to improve everyone's days until<br/>the holidays this year by giving out presents."));
			Msg(L("Every day I will have a new present for you,<br/>so please visit me daily!"));
			Msg(L("But now, let me give you your first present."));
		}
		else
		{
			Msg(L("Hello, <username/>!<br/>Are you here for your advent gift?"));
		}
		Msg(L("Let's see..."), AutoContinue(2000));
		Msg(L("Here it is, I hope you like it!"), Button(L("Continue"), "@continue"));
		await Select();

		var item = GetRandomGift();
		Player.GiveItem(item);
		Player.SystemNotice(L("Received {0} from Coco."), L(item.Data.Name));

		SaveLastReceivedDay(now);

		End();
	}

	private int GetLastReceivedDay(DateTime now)
	{
		var last = Player.Client.Account.Vars.Perm[LastReceivedVar] as DateTime?;
		if (last == null)
			return 0;

		return ((DateTime)last).Day;
	}

	private void SaveLastReceivedDay(DateTime now)
	{
		Player.Client.Account.Vars.Perm[LastReceivedVar] = now;
	}

	public static Item GetRandomGift()
	{
		var rnd = RandomProvider.Get();
		return Item.GetRandomDrop(rnd, gifts);
	}
}
