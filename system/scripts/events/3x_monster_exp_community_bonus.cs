//--- Aura Script -----------------------------------------------------------
// 3x Monster EXP Community Bonus Event
//--- Description -----------------------------------------------------------
// Tripples EXP received from monsters during the duration of the event.
// Ref.: http://wiki.mabinogiworld.com/view/3X_Monster_EXP_Community_Bonus
//---------------------------------------------------------------------------

public class TrippleMonsterExpEventScript : GameEventScript
{
	public override void Load()
	{
		SetId("aura_3x_monster_exp_community_bonus");
		SetName(L("3x Monster EXP Community Bonus"));
	}

	public override void AfterLoad()
	{
		ScheduleEvent(DateTime.Parse("2017-01-01 00:00"), DateTime.Parse("2018-01-01 00:00"));
	}

	protected override void OnStart()
	{
		AddGlobalBonus(GlobalBonusStat.CombatExp, 3);
	}

	protected override void OnEnd()
	{
		RemoveGlobalBonuses();
	}
}
