// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.Skills.Life;
using Aura.Channel.Skills.Magic;
using Aura.Channel.World.Entities;
using Aura.Mabi.Const;
using Aura.Mabi.Network;
using Aura.Shared.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Channel.Skills.Combat
{
	/// <summary>
	/// Ranged Attack handler
	/// </summary>
	/// <remarks>
	/// Var1: Min Ranged Attack Damage
	/// Var2: Max Ranged Attack Damage
	/// Var3: Aim Speed
	/// Var5: Ranged Attack Balance
	/// </remarks>
	[Skill(SkillId.RangedAttack)]
	public class RangedAttack : ISkillHandler, IPreparable, IReadyable, ICompletable, ICancelable, ICombatSkill, IInitiableSkillHandler
	{
		/// <summary>
		/// Distance to knock the target back.
		/// </summary>
		private const int KnockBackDistance = 400;

		/// <summary>
		/// Amount of stability lost on hit.
		/// </summary>
		private const float StabilityReduction = 60f;
		private const float StabilityReductionElf = 30f;

		/// <summary>
		/// Stun for the attacker
		/// </summary>
		private const int AttackerStun = 800;
		private const int AttackerStunElf = 600;
		private const int DefenseAttackerStun = 520;

		/// <summary>
		/// Stun for the target
		/// </summary>
		private const int TargetStun = 2100;
		private const int TargetStunElf = 2600;

		/// <summary>
		/// Bonus damage for fire arrows
		/// </summary>
		public const float FireBonus = 1.5f;

		/// <summary>
		/// Sets up subscriptions for skill training.
		/// </summary>
		public void Init()
		{
			ChannelServer.Instance.Events.CreatureAttack += this.OnCreatureAttacks;
		}

		/// <summary>
		/// Prepares skill.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		/// <returns></returns>
		public bool Prepare(Creature creature, Skill skill, Packet packet)
		{
			Send.SkillPrepare(creature, skill.Info.Id, skill.GetCastTime());

			// Lock running if not elf
			if (!creature.CanRunWithRanged)
				creature.Lock(Locks.Run);

			return true;
		}

		/// <summary>
		/// Readies skill, activates fire effect.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		/// <returns></returns>
		public bool Ready(Creature creature, Skill skill, Packet packet)
		{
			creature.Temp.FireArrow = false;

			// Light arrows (!) on fire if there's a campfire nearby
			if (creature.RightHand != null && creature.RightHand.HasTag("/bow/"))
				creature.Temp.FireArrow = Campfire.GetNearbyCampfire(creature, 500) != null;

			// Add fire arrow effect to arrow
			if (creature.Temp.FireArrow)
				Send.Effect(creature, Effect.FireArrow, true);

			Send.SkillReady(creature, skill.Info.Id);

			// Lock running
			if (!creature.CanRunWithRanged)
				creature.Lock(Locks.Run);

			return true;
		}

		/// <summary>
		/// Completes skill, stopping the aim meter and disabling the fire effect.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		public void Complete(Creature creature, Skill skill, Packet packet)
		{
			this.Cancel(creature, skill);
			Send.SkillComplete(creature, skill.Info.Id);
		}

		/// <summary>
		/// Cancels skill, stopping the aim meter and disabling the fire effect.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		public void Cancel(Creature creature, Skill skill)
		{
			creature.AimMeter.Stop();

			// Disable fire arrow effect
			if (creature.Temp.FireArrow)
				Send.Effect(creature, Effect.FireArrow, false);
		}

		/// <summary>
		/// Uses the skill.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <param name="targetEntityId"></param>
		/// <returns></returns>
		public CombatSkillResult Use(Creature attacker, Skill skill, long targetEntityId)
		{
			// Get target
			var target = attacker.Region.GetCreature(targetEntityId);
			if (target == null)
				return CombatSkillResult.InvalidTarget;

			var targetPos = target.GetPosition();
			var attackerPos = attacker.GetPosition();

			var actionType = (attacker.IsElf ? CombatActionPackType.ChainRangeAttack : CombatActionPackType.NormalAttack);
			var attackerStun = (short)(actionType == CombatActionPackType.ChainRangeAttack ? AttackerStunElf : AttackerStun);

			// "Cancels" the skill
			// 800 = old load time? == aAction.Stun? Varies? Doesn't seem to be a stun.
			Send.SkillUse(attacker, skill.Info.Id, attackerStun, 1);

			var chance = attacker.AimMeter.GetAimChance(target);
			var rnd = RandomProvider.Get().NextDouble() * 100;
			var successfulHit = (rnd < chance);

			var maxHits = (actionType == CombatActionPackType.ChainRangeAttack && successfulHit ? 2 : 1);
			var prevId = 0;

			for (byte i = 1; i <= maxHits; ++i)
			{
				target.StopMove();

				// Actions
				var cap = new CombatActionPack(attacker, skill.Info.Id);
				cap.Hit = i;
				cap.Type = actionType;
				cap.PrevId = prevId;
				prevId = cap.Id;

				var aAction = new AttackerAction(CombatActionType.RangeHit, attacker, targetEntityId);
				aAction.Set(AttackerOptions.Result);
				aAction.Stun = attackerStun;
				cap.Add(aAction);

				// Target action if hit
				if (successfulHit)
				{
					var tAction = new TargetAction(CombatActionType.TakeHit, target, attacker, skill.Info.Id);
					tAction.Set(TargetOptions.Result);
					tAction.Stun = (short)(actionType == CombatActionPackType.ChainRangeAttack ? TargetStunElf : TargetStun);
					if (actionType == CombatActionPackType.ChainRangeAttack)
						tAction.EffectFlags = EffectFlags.SpecialRangeHit;

					cap.Add(tAction);

					// Damage
					var damage = attacker.GetRndRangedDamage();

					// Elementals
					damage *= attacker.CalculateElementalDamageMultiplier(target);

					// More damage with fire arrow
					// XXX: Does this affect the element?
					if (attacker.Temp.FireArrow)
						damage *= FireBonus;

					// Critical Hit
					var critChance = attacker.GetRightCritChance(target.Protection);
					CriticalHit.Handle(attacker, critChance, ref damage, tAction);

					// Subtract target def/prot
					SkillHelper.HandleDefenseProtection(target, ref damage);

					// Conditions
					SkillHelper.HandleConditions(attacker, target, ref damage);

					// Defense
					Defense.Handle(aAction, tAction, ref damage);

					// Mana Shield
					ManaShield.Handle(target, ref damage, tAction);

					// Natural Shield
					var nsResult = NaturalShield.Handle(attacker, target, ref damage, tAction);
					var delayReduction = nsResult.DelayReduction;
					var pinged = nsResult.Pinged;

					// Deal with it!
					if (damage > 0)
					{
						target.TakeDamage(tAction.Damage = damage, attacker);
						SkillHelper.HandleInjury(attacker, target, damage);
					}

					// Knock down on deadly
					if (target.Conditions.Has(ConditionsA.Deadly))
					{
						tAction.Set(TargetOptions.KnockDown);
						tAction.Stun = (short)(actionType == CombatActionPackType.ChainRangeAttack ? TargetStunElf : TargetStun);
					}

					// Aggro
					target.Aggro(attacker);

					// Death/Knockback
					if (target.IsDead)
					{
						tAction.Set(TargetOptions.FinishingKnockDown);
						maxHits = 1;
					}
					else
					{
						// Insta-recover in knock down
						// TODO: Tied to stability?
						if (target.IsKnockedDown)
						{
							tAction.Stun = 0;
						}
						// Knock down if hit repeatedly
						else if (target.Stability < 30)
						{
							tAction.Set(TargetOptions.KnockDown);
						}
						// Normal stability reduction
						else
						{
							var stabilityReduction = (actionType == CombatActionPackType.ChainRangeAttack ? StabilityReductionElf : StabilityReduction);

							// Reduce reduction, based on ping
							// According to the Wiki, "the Knockdown Gauge
							// [does not] build up", but it's still possible
							// to knock back with repeated hits. The stability
							// reduction is probably reduced, just like stun.
							if (delayReduction > 0)
								stabilityReduction = (short)Math.Max(0, stabilityReduction - (stabilityReduction / 100 * delayReduction));

							target.Stability -= stabilityReduction;
							if (target.IsUnstable)
							{
								tAction.Set(TargetOptions.KnockBack);
							}
						}
					}

					// Knock Back
					if (tAction.IsKnockBack)
						attacker.Shove(target, KnockBackDistance);

					// Reduce stun, based on ping
					if (pinged && delayReduction > 0)
						tAction.Stun = (short)Math.Max(0, tAction.Stun - (tAction.Stun / 100 * delayReduction));

					// No second hit if defended
					if (tAction.SkillId == SkillId.Defense)
					{
						// Set to 1 to prevent second run
						maxHits = 1;

						// Set normal type if hit didn't come from the second
						// arrow.
						if (cap.Hit != 2)
							cap.Type = CombatActionPackType.NormalAttack;

						// Override stun set by Defense
						aAction.Stun = DefenseAttackerStun;
					}
				}
				else
				{
					// Dummy target action on miss, so the client knows what
					// the target would've been. Possibly affects arrow
					// animations.
					var tAction = new TargetAction(CombatActionType.None, target, attacker, SkillId.None);
					cap.Add(tAction);
				}

				// Update current weapon
				SkillHelper.UpdateWeapon(attacker, target, ProficiencyGainType.Ranged, attacker.RightHand);

				// Skill training
				if (skill.Info.Rank == SkillRank.Novice || skill.Info.Rank == SkillRank.RF)
					skill.Train(1); // Try ranged attack.

				// Reduce arrows
				if (attacker.Magazine != null && !ChannelServer.Instance.Conf.World.InfiniteArrows && !attacker.Magazine.HasTag("/unlimited_arrow/"))
					attacker.Inventory.Decrement(attacker.Magazine);

				cap.Handle();
			}

			// Disable fire arrow effect
			if (attacker.Temp.FireArrow)
				Send.Effect(attacker, Effect.FireArrow, false);

			return CombatSkillResult.Okay;
		}

		/// <summary>
		/// Handles the majority of the skill training.
		/// </summary>
		/// <param name="tAction"></param>
		private void OnCreatureAttacks(TargetAction tAction)
		{
			if (tAction.AttackerSkillId != SkillId.RangedAttack)
				return;

			var attackerSkill = tAction.Attacker.Skills.Get(SkillId.RangedAttack);
			var targetSkill = tAction.Creature.Skills.Get(SkillId.RangedAttack);
			var targetPowerRating = tAction.Attacker.GetPowerRating(tAction.Creature);
			var attackerPowerRating = tAction.Creature.GetPowerRating(tAction.Attacker);

			if (attackerSkill != null)
			{
				if (attackerSkill.Info.Rank == SkillRank.RF)
				{
					attackerSkill.Train(2); // Attack an enemy.

					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(3); // Down the enemy with continuous hit.

					if (tAction.Creature.IsDead)
						attackerSkill.Train(4); // Kill an enemy.
				}
				else if (attackerSkill.Info.Rank == SkillRank.RE)
				{
					if (targetPowerRating == PowerRating.Normal)
						attackerSkill.Train(3); // Attack a same level enemy.

					if (tAction.Has(TargetOptions.KnockDown))
					{
						attackerSkill.Train(1); // Down the enemy with continuous hit.

						if (targetPowerRating == PowerRating.Normal)
							attackerSkill.Train(4); // Down a same level enemy. 

						if (targetPowerRating == PowerRating.Strong)
							attackerSkill.Train(6); // Down a strong enemy.
					}

					if (tAction.Creature.IsDead)
					{
						attackerSkill.Train(2); // Kill an enemy.

						if (targetPowerRating == PowerRating.Normal)
							attackerSkill.Train(5); // Kill a same level enemy. 

						if (targetPowerRating == PowerRating.Strong)
							attackerSkill.Train(7); // Kill a strong enemy.
					}
				}
				else if (attackerSkill.Info.Rank == SkillRank.RD)
				{
					attackerSkill.Train(1); // Attack any enemy. 

					if (targetPowerRating == PowerRating.Normal)
						attackerSkill.Train(4); // Attack a same level enemy.

					if (tAction.Has(TargetOptions.KnockDown))
					{
						attackerSkill.Train(2); // Down the enemy with continuous hit.

						if (targetPowerRating == PowerRating.Normal)
							attackerSkill.Train(5); // Down a same level enemy. 

						if (targetPowerRating == PowerRating.Strong)
							attackerSkill.Train(7); // Down a strong enemy.
					}

					if (tAction.Creature.IsDead)
					{
						attackerSkill.Train(3); // Kill an enemy.

						if (targetPowerRating == PowerRating.Normal)
							attackerSkill.Train(6); // Kill a same level enemy. 

						if (targetPowerRating == PowerRating.Strong)
							attackerSkill.Train(8); // Kill a strong enemy.
					}
				}
				else if (attackerSkill.Info.Rank >= SkillRank.RC && attackerSkill.Info.Rank <= SkillRank.RB)
				{
					if (targetPowerRating == PowerRating.Normal)
						attackerSkill.Train(1); // Attack a same level enemy.

					if (tAction.Has(TargetOptions.KnockDown))
					{
						if (targetPowerRating == PowerRating.Normal)
							attackerSkill.Train(2); // Down a same level enemy. 

						if (targetPowerRating == PowerRating.Strong)
							attackerSkill.Train(4); // Down a strong enemy.

						if (targetPowerRating == PowerRating.Awful)
							attackerSkill.Train(6); // Down an awful enemy.
					}

					if (tAction.Creature.IsDead)
					{
						if (targetPowerRating == PowerRating.Normal)
							attackerSkill.Train(3); // Kill a same level enemy. 

						if (targetPowerRating == PowerRating.Strong)
							attackerSkill.Train(5); // Kill a strong enemy.

						if (targetPowerRating == PowerRating.Awful)
							attackerSkill.Train(7); // Kill an awful enemy.
					}
				}
				else if (attackerSkill.Info.Rank >= SkillRank.RA && attackerSkill.Info.Rank <= SkillRank.R8)
				{
					if (tAction.Has(TargetOptions.KnockDown))
					{
						if (targetPowerRating == PowerRating.Normal)
							attackerSkill.Train(1); // Down a same level enemy. 

						if (targetPowerRating == PowerRating.Strong)
							attackerSkill.Train(3); // Down a strong enemy.

						if (targetPowerRating == PowerRating.Awful)
							attackerSkill.Train(5); // Down an awful enemy.

						if (targetPowerRating == PowerRating.Boss && attackerSkill.Info.Rank == SkillRank.R8)
							attackerSkill.Train(7); // Down a boss level enemy.
					}

					if (tAction.Creature.IsDead)
					{
						if (targetPowerRating == PowerRating.Normal)
							attackerSkill.Train(2); // Kill a same level enemy. 

						if (targetPowerRating == PowerRating.Strong)
							attackerSkill.Train(4); // Kill a strong enemy.

						if (targetPowerRating == PowerRating.Awful)
							attackerSkill.Train(6); // Kill an awful enemy.

						if (targetPowerRating == PowerRating.Boss && attackerSkill.Info.Rank == SkillRank.R8)
							attackerSkill.Train(8); // Kill a boss level enemy.
					}
				}
				else if (attackerSkill.Info.Rank >= SkillRank.R7 && attackerSkill.Info.Rank <= SkillRank.R1)
				{
					if (tAction.Has(TargetOptions.KnockDown))
					{
						if (targetPowerRating == PowerRating.Strong)
							attackerSkill.Train(1); // Down a strong enemy.

						if (targetPowerRating == PowerRating.Awful)
							attackerSkill.Train(3); // Down an awful enemy.

						if (targetPowerRating == PowerRating.Boss)
							attackerSkill.Train(5); // Down a boss level enemy.
					}

					if (tAction.Creature.IsDead)
					{
						if (targetPowerRating == PowerRating.Strong)
							attackerSkill.Train(2); // Kill a strong enemy.

						if (targetPowerRating == PowerRating.Awful)
							attackerSkill.Train(4); // Kill an awful enemy.

						if (targetPowerRating == PowerRating.Boss)
							attackerSkill.Train(6); // Kill a boss level enemy.
					}
				}
			}

			if (targetSkill != null)
			{
				if (targetSkill.Info.Rank == SkillRank.RF)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						targetSkill.Train(5); // Learn by falling down.

					if (tAction.Creature.IsDead)
						targetSkill.Train(6); // Learn through losing.
				}
				else if (targetSkill.Info.Rank == SkillRank.RD)
				{
					if (attackerPowerRating == PowerRating.Strong)
						targetSkill.Train(8); // Receive a powerful attack from a powerful enemy.
				}
				else if (targetSkill.Info.Rank == SkillRank.R7)
				{
					if (attackerPowerRating == PowerRating.Strong)
						targetSkill.Train(7); // Receive a powerful attack from a powerful enemy.
				}
			}
		}
	}
}
