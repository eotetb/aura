//--- Aura Script -----------------------------------------------------------
// 100% Repair Rate Event
//--- Description -----------------------------------------------------------
// Sets repair rate to 100% for all NPCs.
// Reference: http://mabinogi.nexon.net/News/Announcements/1/00IwL
//--- Remarks ---------------------------------------------------------------
// The event doesn't need to set any bonuses or anything, since the client
// automatically switches to 100% mode as soon as the event is active.
// As such, the server does the same.
//---------------------------------------------------------------------------

public class RepairRate100EventScript : GameEventScript
{
	public override void Load()
	{
		SetId("all_repairrate_100");
		SetName(L("100% Repair"));
	}

	public override void AfterLoad()
	{
		ScheduleEvent(DateTime.Parse("2017-01-01 00:00"), DateTime.Parse("2018-01-01 00:00"));
	}

	protected override void OnStart()
	{
	}

	protected override void OnEnd()
	{
	}
}
