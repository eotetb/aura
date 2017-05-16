// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.Skills.Magic;
using Aura.Channel.Skills.Combat;
using Aura.Channel.World;
using Aura.Channel.World.Entities;
using Aura.Data.Database;
using Aura.Mabi.Const;
using Aura.Mabi.Network;
using Aura.Shared.Network;
using Aura.Shared.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Channel.Skills.Combat
{
	/// <summary>
	/// Skill handler for The Fake Spiral Sword
	/// </summary>
	/// <remarks>
	/// Var1: Damage Percentage ?
	/// Var2: Attack Range ?
	/// Var3: Explosion Radius ?
	/// Var4: ?
	/// Var5: ?
	/// 
	/// There isn't much data on this skill, so skill variable use
	/// is mostly based on speculation from gameplay and packet data.
	/// </remarks>
	[Skill(SkillId.TheFakeSpiralSword)]
	public class TheFakeSpiralSword : ISkillHandler, IPreparable, IReadyable, ICompletable, ICancelable, ICombatSkill
	{
		/// <summary>
		/// Attacker's stun
		/// </summary>
		private const int AttackerStun = 500;

		/// <summary>
		/// Target's stun
		/// </summary>
		private const int TargetStun = 2000;

		/// <summary>
		/// Distance the target is knocked back
		/// </summary>
		private const int KnockbackDistance = 250;

		/// <summary>
		/// Prepares the skill
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		/// <returns></returns>
		public bool Prepare(Creature creature, Skill skill, Packet packet)
		{
			Send.Effect(creature, Effect.TheFakeSpiralSword, TheFakeSpiralSwordEffect.Prepare, DateTime.Now, skill.RankData.LoadTime);
			Send.SkillPrepare(creature, skill.Info.Id, skill.GetCastTime());

			return true;
		}

		/// <summary>
		/// Readies the skill
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		/// <returns></returns>
		public bool Ready(Creature creature, Skill skill, Packet packet)
		{
			skill.Stacks = 1;

			Send.Effect(creature, Effect.TheFakeSpiralSword, TheFakeSpiralSwordEffect.Ready);
			Send.SkillReady(creature, skill.Info.Id);

			return true;
		}

		/// <summary>
		/// Uses the skill
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <param name="targetEntityId"></param>
		/// <returns></returns>
		public CombatSkillResult Use(Creature attacker, Skill skill, long targetEntityId)
		{
			// Get Target
			var initTarget = attacker.Region.GetCreature(targetEntityId);

			// Check Target
			if (initTarget == null)
				return CombatSkillResult.InvalidTarget;

			var attackerPos = attacker.StopMove();
			var initTargetPos = initTarget.GetPosition();

			// Check Range
			var range = (int)skill.RankData.Var2;
			if (!attacker.GetPosition().InRange(initTargetPos, range))
				return CombatSkillResult.OutOfRange;

			// Check for Collisions
			if (attacker.Region.Collisions.Any(attackerPos, initTargetPos))
				return CombatSkillResult.InvalidTarget;

			initTarget.StopMove();

			// Effects
			Send.Effect(attacker, Effect.TheFakeSpiralSword, TheFakeSpiralSwordEffect.Attack, (DateTime.Now.Ticks / 10000), (byte)1);

			// Skill Use
			Send.SkillUseStun(attacker, skill.Info.Id, AttackerStun, 1);
			skill.Stacks = 0;

			// Prepare Combat Actions
			var cap = new CombatActionPack(attacker, skill.Info.Id);

			var aAction = new AttackerAction(CombatActionType.RangeHit, attacker, targetEntityId);
			aAction.Set(AttackerOptions.KnockBackHit1 | AttackerOptions.KnockBackHit2 | AttackerOptions.Result);
			cap.Add(aAction);

			aAction.Stun = AttackerStun;

			// Get Explosion Radius of Attack
			var explosionRadius = (int)skill.RankData.Var3 / 2;

			// Get Explosion Targets
			var targets = attacker.GetTargetableCreaturesAround(initTargetPos, explosionRadius);

			var rnd = RandomProvider.Get();

			// Get Critical Hit
			var crit = false;
			if (attacker.Skills.Has(SkillId.CriticalHit, SkillRank.RF))
			{
				var critChance = attacker.GetRightCritChance(0);
				crit = (rnd.Next(100) < critChance);
			}

			foreach (var target in targets)
			{
				var tAction = new TargetAction(CombatActionType.TakeHit, target, attacker, skill.Info.Id);
				tAction.Set(TargetOptions.Result);
				tAction.Delay = attackerPos.GetDistance(initTargetPos) / 2;
				cap.Add(tAction);

				// Damage
				var damage = (attacker.GetRndTotalDamage() * (skill.RankData.Var1 / 100f));

				// Critical Hit
				if (crit)
					CriticalHit.Handle(attacker, 100, ref damage, tAction);

				// Defense and Prot
				SkillHelper.HandleDefenseProtection(target, ref damage);

				// Defense
				Defense.Handle(aAction, tAction, ref damage);

				// Mana Shield
				ManaShield.Handle(target, ref damage, tAction);

				// Heavy Stander
				HeavyStander.Handle(attacker, target, ref damage, tAction);

				// Apply Damage
				target.TakeDamage(tAction.Damage = damage, attacker);

				// Aggro
				target.Aggro(attacker);

				// Stun Time
				tAction.Stun = TargetStun;

				// Death and Knockback
				if (target.Is(RaceStands.KnockDownable))
				{
					if (target.IsDead)
						tAction.Set(TargetOptions.FinishingKnockDown);
					else
						tAction.Set(TargetOptions.KnockDown);

					// Shove
					if (target == initTarget)
						attacker.Shove(target, KnockbackDistance);
					else
						initTarget.Shove(target, KnockbackDistance);
				}
			}

			aAction.Creature.Stun = aAction.Stun;
			cap.Handle();

			// User can attack multiple times if attack isn't locked, which will cause them to freeze.
			// This is automatically unlocked by the skill after Use is finished.
			attacker.Lock(Locks.Attack);

			return CombatSkillResult.Okay;
		}

		/// <summary>
		/// Completes the skill
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		public void Complete(Creature creature, Skill skill, Packet packet)
		{
			Send.Effect(creature, Effect.TheFakeSpiralSword, TheFakeSpiralSwordEffect.Complete);
			Send.SkillComplete(creature, skill.Info.Id);
		}

		/// <summary>
		/// Cancels the skill
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		public void Cancel(Creature creature, Skill skill)
		{
			Send.Effect(creature, Effect.TheFakeSpiralSword, TheFakeSpiralSwordEffect.Cancel);
			Send.SkillCancel(creature);
		}
	}
}
