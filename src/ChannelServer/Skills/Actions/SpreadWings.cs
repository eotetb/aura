// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.World;
using Aura.Channel.World.Entities;
using Aura.Mabi;
using Aura.Mabi.Const;
using Aura.Mabi.Network;
using Aura.Shared.Util;

namespace Aura.Channel.Skills.Action
{
	/// <summary>
	/// Spread Wings skill handler
	/// </summary>
	[Skill(SkillId.SpreadWings)]
	public class SpreadWings : StartStopSkillHandler
	{
		public override StartStopResult Start(Creature creature, Skill skill, MabiDictionary dict)
		{
			creature.Conditions.Activate(ConditionsD.SpreadWings);
			Send.UseMotion(creature, 134, 0, true, false);

			return StartStopResult.Okay;
		}

		public override StartStopResult Stop(Creature creature, Skill skill, MabiDictionary dict)
		{
			creature.Conditions.Deactivate(ConditionsD.SpreadWings);
			Send.MotionCancel2(creature, 1);

			return StartStopResult.Okay;
		}
	}
}
