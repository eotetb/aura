// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.Skills.Combat;
using Aura.Channel.World.Entities;
using Aura.Mabi.Const;
using Aura.Mabi.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aura.Channel.Skills.Magic
{
	/// <summary>
	/// Handler for Thunder skill.
	/// </summary>
	/// <remarks>
	/// Var1: Min Damage
	/// Var2: Max Damage
	/// Var3: Thunderbolt Delay (ms)
	/// Var4: Minimum Initial Damage
	/// Var5: Maximum Initial Damage
	/// Var7: ?
	/// Var8: ?
	/// </remarks>
	[Skill(SkillId.Thunder)]
	public class Thunder : IPreparable, IReadyable, ICombatSkill, ICompletable, ICancelable, ICustomHitCanceler
	{
		/// <summary>
		/// Skill's range.
		/// </summary>
		private const int Range = 2000;

		/// <summary>
		/// Stability reduction on overcharge.
		/// </summary>
		protected const float OverchargeStabilityReduction = 110;

		/// <summary>
		/// Units the target is knocked back.
		/// </summary>
		protected const int KnockbackDistance = 400;

		/// <summary>
		/// Amount added to the knock back meter on each hit.
		/// </summary>
		protected const float StabilityReduction = 45;

		/// <summary>
		/// Minimum stability required to not get knocked down.
		/// </summary>
		protected const float MinStability = 20;

		/// <summary>
		/// Target's stun after the final thunderbolts.
		/// </summary>
		protected const float ThunderboltTargetStun = 2000;

		/// <summary>
		/// Target area radius.
		/// </summary>
		protected const int AreaOfEffect = 600;

		/// <summary>
		/// Prepares skill, showing effects/motions.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		/// <returns></returns>
		public bool Prepare(Creature creature, Skill skill, Packet packet)
		{
			creature.StopMove();

			if (!this.CheckWeapon(creature))
			{
				creature.Notice("You need a Lightning Wand to use this skill.");
				return false;
			}

			Send.SkillInitEffect(creature, "thunder", skill.Info.Id);
			Send.SkillPrepare(creature, skill.Info.Id, skill.GetCastTime());

			return true;
		}

		/// <summary>
		/// Returns true if creature can use the skill with its currently
		/// equipped weapon.
		/// </summary>
		/// <param name="creature"></param>
		/// <returns></returns>
		private bool CheckWeapon(Creature creature)
		{
			// Give NPCs a free pass, their AI decides what they can and
			// can't do.
			if (creature.Has(CreatureStates.Npc))
				return true;

			var rightHand = creature.RightHand;

			// No weapon, no Thunder.
			if (rightHand == null)
				return false;

			// TODO: Elemental charging

			// Disallow non-wands and staffs without special tag.
			if (!rightHand.HasTag("/lightning_wand/|/no_bolt_stack/"))
				return false;

			return true;
		}

		/// <summary>
		/// Readies skill, increasing stack.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		/// <returns></returns>
		public bool Ready(Creature creature, Skill skill, Packet packet)
		{
			// Increase stack count
			if (skill.Stacks < skill.RankData.StackMax)
			{
				var addStacks = this.GetStacks(creature, skill);
				skill.Stacks = Math.Min(skill.RankData.StackMax, skill.Stacks + addStacks);
			}

			// Novice training
			if (skill.Info.Rank == SkillRank.Novice && skill.Stacks == skill.RankData.StackMax)
				skill.Train(1); // Fully charge Thunder.

			Send.Effect(creature, Effect.StackUpdate, "lightningbolt", (byte)skill.Stacks, (byte)0);
			Send.Effect(creature, Effect.HoldMagic, "thunder", (ushort)skill.Info.Id);
			Send.SkillReady(creature, skill.Info.Id);

			return true;
		}

		/// <summary>
		/// Returns the number of stacks to charge on each Ready.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <returns></returns>
		private int GetStacks(Creature creature, Skill skill)
		{
			var stacks = skill.RankData.Stack;

			var hasChainCasting = creature.Skills.Has(SkillId.ChainCasting);
			var isMonster = creature.Has(CreatureStates.Npc);

			// Monsters and creatures with the Chain Casting skill get the
			// max stacks.
			if (hasChainCasting || isMonster)
				stacks = skill.RankData.StackMax;

			return stacks;
		}

		/// <summary>
		/// Uses skill, letting Thunder strike down on the target.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		public CombatSkillResult Use(Creature attacker, Skill skill, long targetEntityId)
		{
			// Check target
			var mainTarget = attacker.Region.GetCreature(targetEntityId);
			if (mainTarget == null)
				return CombatSkillResult.InvalidTarget;

			// Check range
			var attackerPos = attacker.GetPosition();
			var targetPos = mainTarget.GetPosition();
			var range = Range;

			if (!attackerPos.InRange(targetPos, range))
				return CombatSkillResult.OutOfRange;

			// Get targets
			var stacks = skill.Stacks;
			var maxTargets = (skill.Info.Rank < SkillRank.R1 ? 1 : 4) + ((stacks - 1) * 2);

			var potentialTargets = attacker.GetTargetableCreaturesAround(targetPos, AreaOfEffect, TargetableOptions.IgnoreWalls);
			var takeAmount = Math.Min(maxTargets, potentialTargets.Count);

			var targets = new List<Creature>(potentialTargets.Take(takeAmount));
			var targetCount = targets.Count;

			// Send effects and create prop
			var regionId = attacker.RegionId;
			var thunderboltDelay = (int)skill.RankData.Var3;

			Send.Effect(mainTarget, Effect.UseMagic, "thunder", (byte)1, mainTarget.EntityId, (ushort)skill.Info.Id);

			var damageProp = new Prop(280, regionId, targetPos.X, targetPos.Y, 0.19f, 1);
			attacker.Region.AddProp(damageProp);

			var effectArgs = new List<object>();
			effectArgs.Add(attacker.EntityId);
			effectArgs.Add(targetCount);
			foreach (var target in targets)
				effectArgs.Add(mainTarget.EntityId);

			Send.Effect(mainTarget, Effect.Lightningbolt, effectArgs.ToArray());

			// Update weapon
			SkillHelper.UpdateWeapon(attacker, mainTarget, ProficiencyGainType.Melee, attacker.RightHand);

			// Finalize skill
			Send.SkillUse(attacker, skill.Info.Id, targetEntityId, thunderboltDelay, 1);

			skill.Stacks = 0;
			Send.Effect(attacker, Effect.StackUpdate, "lightningbolt", (byte)0, (byte)0);

			// Bolts
			this.Bolts(attacker, skill, stacks, mainTarget, targets, thunderboltDelay);

			// Schedule callback for the Thunderbolt
			Task.Run(() => this.Thunderbolts(attacker, skill, stacks, damageProp, thunderboltDelay, targets));

			return CombatSkillResult.Okay;
		}

		/// <summary>
		/// Handles initial bolts.
		/// </summary>
		private void Bolts(Creature attacker, Skill skill, int stacks, Creature mainTarget, List<Creature> targets, int thunderboltDelay)
		{
			// Combat action
			var aAction = new AttackerAction(CombatActionType.RangeHit, attacker, mainTarget.EntityId);
			aAction.Set(AttackerOptions.Result);
			aAction.Stun = (short)thunderboltDelay;

			var cap = new CombatActionPack(attacker, skill.Info.Id, aAction);

			foreach (var target in targets)
			{
				target.StopMove();
				target.Aggro(attacker);

				var tAction = new TargetAction(CombatActionType.TakeHit, target, attacker, skill.Info.Id);
				tAction.Set(TargetOptions.Result);
				tAction.Stun = (short)(thunderboltDelay + 1000);

				var damage = this.GetBoltDamage(attacker, skill, stacks);

				// Critical Hit
				var critChance = attacker.GetTotalCritChance(target.Protection, true);
				CriticalHit.Handle(attacker, critChance, ref damage, tAction);

				// Reduce damage
				Defense.Handle(aAction, tAction, ref damage);
				SkillHelper.HandleMagicDefenseProtection(target, ref damage);
				SkillHelper.HandleConditions(attacker, target, ref damage);
				ManaShield.Handle(target, ref damage, tAction);

				// Mana Deflector
				var mdResult = ManaDeflector.Handle(attacker, target, ref damage, tAction);
				var delayReduction = mdResult.DelayReduction;
				var pinged = mdResult.Pinged;

				// Deal damage
				if (damage > 0)
					target.TakeDamage(tAction.Damage = damage, attacker);

				// Reduce stun, based on ping
				if (pinged && delayReduction > 0)
					tAction.Stun = (short)Math.Max(0, tAction.Stun - (tAction.Stun / 100 * delayReduction));

				// Death/Knockback
				if (target.IsDead)
				{
					tAction.Set(TargetOptions.FinishingKnockDown);
				}
				else
				{
					// If knocked down, instant recovery,
					// if repeat hit, knock down,
					// otherwise potential knock back.
					if (target.IsKnockedDown)
					{
						tAction.Stun = 0;
					}
					else if (target.Stability < MinStability)
					{
						tAction.Set(TargetOptions.KnockDown);
					}
					else
					{
						var stabilityReduction = StabilityReduction;

						if (delayReduction > 0)
							stabilityReduction = (short)Math.Max(0, stabilityReduction - (stabilityReduction / 100 * delayReduction));

						target.Stability -= stabilityReduction;

						if (target.IsUnstable)
							tAction.Set(TargetOptions.KnockBack);
					}
				}

				cap.Add(tAction);

				this.Train(skill, tAction);
			}

			cap.Handle();
		}

		/// <summary>
		/// Handles final Thunderbolts.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <param name="damageProp"></param>
		private async void Thunderbolts(Creature attacker, Skill skill, int stacks, Prop damageProp, int thunderboltDelay, List<Creature> targets)
		{
			var mainTarget = targets.First();
			var targetCount = targets.Count;
			var end = DateTime.Now.AddMilliseconds(thunderboltDelay);

			var effectArgs = new List<object>();
			effectArgs.Add(attacker.EntityId);
			effectArgs.Add(targetCount);
			foreach (var target in targets)
				effectArgs.Add(mainTarget.EntityId);

			while (DateTime.Now < end)
			{
				Send.Effect(mainTarget, Effect.Lightningbolt, effectArgs.ToArray());
				await Task.Delay(500);
			}

			for (int i = 0; i < stacks; ++i)
			{
				this.Thunderbolt(attacker, skill, stacks, damageProp, targets);
				await Task.Delay(500);
			}

			damageProp.DisappearTime = DateTime.Now;
		}

		/// <summary>
		///  Hits targets with thunderbolt.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <param name="stacks"></param>
		/// <param name="damageProp"></param>
		/// <param name="targets"></param>
		private void Thunderbolt(Creature attacker, Skill skill, int stacks, Prop damageProp, List<Creature> targets)
		{
			// Skip if all targets are already dead
			if (targets.All(a => a.IsDead))
				return;

			var locationId = damageProp.GetLocation().ToLocationId();
			var aAction = new AttackerAction(CombatActionType.SpecialHit, attacker, locationId, skill.Info.Id);
			aAction.Set(AttackerOptions.KnockBackHit1 | AttackerOptions.UseEffect);
			aAction.PropId = damageProp.EntityId;

			var cap = new CombatActionPack(attacker, skill.Info.Id, aAction);

			foreach (var target in targets.Where(a => !a.IsDead))
			{
				var tAction = new TargetAction(CombatActionType.TakeHit, target, attacker, skill.Info.Id);
				tAction.Set(TargetOptions.Result | TargetOptions.KnockDown);
				tAction.EffectFlags = EffectFlags.SpecialRangeHit;
				tAction.Delay = 313;
				tAction.Stun = (short)ThunderboltTargetStun;

				var damage = this.GetThunderboltDamage(attacker, skill, stacks);

				// Critical Hit
				var critChance = attacker.GetTotalCritChance(target.Protection, true);
				CriticalHit.Handle(attacker, critChance, ref damage, tAction);

				// Reduce damage
				Defense.Handle(aAction, tAction, ref damage);
				SkillHelper.HandleMagicDefenseProtection(target, ref damage);
				SkillHelper.HandleConditions(attacker, target, ref damage);
				ManaShield.Handle(target, ref damage, tAction);

				// Mana Deflector
				var mdResult = ManaDeflector.Handle(attacker, target, ref damage, tAction);
				var delayReduction = mdResult.DelayReduction;
				var pinged = mdResult.Pinged;

				// Deal damage
				if (damage > 0)
					target.TakeDamage(tAction.Damage = damage, attacker);

				// Reduce stun, based on ping
				if (pinged && delayReduction > 0)
					tAction.Stun = (short)Math.Max(0, tAction.Stun - (tAction.Stun / 100 * delayReduction));

				// Death/Knockback
				if (target.IsDead)
					tAction.Set(TargetOptions.FinishingKnockDown);

				cap.Add(tAction);
			}

			cap.Handle();
		}

		/// <summary>
		/// Completes skill.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		public void Complete(Creature creature, Skill skill, Packet packet)
		{
			Send.Echo(creature, packet);
		}

		/// <summary>
		/// Cancels skill, removing effects.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		public void Cancel(Creature creature, Skill skill)
		{
			skill.Stacks = 0;
			Send.Effect(creature, Effect.StackUpdate, "lightningbolt", (byte)skill.Stacks, (byte)0);
			Send.Effect(creature, Effect.CancelMagic, "thunder", (short)skill.Info.Id);
			Send.MotionCancel2(creature, 1);
		}

		/// <summary>
		/// Called when creature is hit while skill is active.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="tAction"></param>
		public void CustomHitCancel(Creature creature, TargetAction tAction)
		{
			// Lose only 1 stack on r1
			var skill = creature.Skills.ActiveSkill;
			if (skill.Info.Rank < SkillRank.R1 || skill.Stacks <= 1)
			{
				creature.Skills.CancelActiveSkill();
				return;
			}

			skill.Stacks -= 1;
			Send.Effect(creature, Effect.StackUpdate, "lightningbolt", (byte)skill.Stacks, (byte)0);
		}

		/// <summary>
		/// Returns the damage for the initial bolts.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <param name="stacks"></param>
		private float GetBoltDamage(Creature attacker, Skill skill, int stacks)
		{
			var damage = attacker.GetRndMagicDamage(skill, skill.RankData.Var4, skill.RankData.Var5);
			if (stacks >= skill.RankData.StackMax)
				damage *= 2f;
			else
				damage *= 1.5f;

			return damage;
		}

		/// <summary>
		/// Returns the damage for the final thunderbolt.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <param name="stacks"></param>
		private float GetThunderboltDamage(Creature attacker, Skill skill, int stacks)
		{
			var damage = attacker.GetRndMagicDamage(skill, skill.RankData.Var1, skill.RankData.Var2);
			if (stacks >= skill.RankData.StackMax)
				damage *= 2f;
			else
				damage *= 1.5f;

			return damage;
		}

		/// <summary>
		/// Called for training the skill, based on what happened.
		/// </summary>
		/// <param name="attackerSkill"></param>
		/// <param name="tAction"></param>
		private void Train(Skill attackerSkill, TargetAction tAction)
		{
			var target = tAction.Creature;

			if (attackerSkill.Info.Rank == SkillRank.RF)
			{
				attackerSkill.Train(1); // Attack an enemy.

				if (target.IsDead)
					attackerSkill.Train(2); // Defeat an enemy.

				return;
			}

			var rating = tAction.Attacker.GetPowerRating(target);

			if (attackerSkill.Info.Rank >= SkillRank.RE && attackerSkill.Info.Rank <= SkillRank.RD)
			{
				attackerSkill.Train(1); // Attack an enemy.

				if (target.IsDead)
					attackerSkill.Train(2); // Defeat an enemy.

				if (rating == PowerRating.Normal)
				{
					attackerSkill.Train(3); // Attack a similar-ranked enemy.

					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(4); // Knock down a similar-ranked enemy.

					if (target.IsDead)
						attackerSkill.Train(5); // Defeat a similar-ranked enemy.
				}
				else if (rating == PowerRating.Strong)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(6); // Knock down a powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(7); // Defeat a powerful enemy.
				}

				return;
			}

			if (attackerSkill.Info.Rank >= SkillRank.RC && attackerSkill.Info.Rank <= SkillRank.RB)
			{
				if (rating == PowerRating.Normal)
				{
					attackerSkill.Train(1); // Attack a similar-ranked enemy.

					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(2); // Knock down a similar-ranked enemy.

					if (target.IsDead)
						attackerSkill.Train(3); // Defeat a similar-ranked enemy.
				}
				else if (rating == PowerRating.Strong)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(4); // Knock down a powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(5); // Defeat a powerful enemy.
				}
				else if (rating == PowerRating.Awful)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(6); // Knock down a very powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(7); // Defeat a very powerful enemy.
				}

				return;
			}

			if (attackerSkill.Info.Rank >= SkillRank.RA && attackerSkill.Info.Rank <= SkillRank.R9)
			{
				if (rating == PowerRating.Normal)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(1); // Knock down a similar-ranked enemy.

					if (target.IsDead)
						attackerSkill.Train(2); // Defeat a similar-ranked enemy.
				}
				else if (rating == PowerRating.Strong)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(3); // Knock down a powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(4); // Defeat a powerful enemy.
				}
				else if (rating == PowerRating.Awful)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(5); // Knock down a very powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(6); // Defeat a very powerful enemy.
				}

				return;
			}

			if (attackerSkill.Info.Rank == SkillRank.R8)
			{
				if (rating == PowerRating.Normal)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(1); // Knock down a similar-ranked enemy.

					if (target.IsDead)
						attackerSkill.Train(2); // Defeat a similar-ranked enemy.
				}
				else if (rating == PowerRating.Strong)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(3); // Knock down a powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(4); // Defeat a powerful enemy.
				}
				else if (rating == PowerRating.Awful)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(5); // Knock down a very powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(6); // Defeat a very powerful enemy.
				}
				else if (rating == PowerRating.Boss)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(7); // Knock down a boss-level enemy.

					if (target.IsDead)
						attackerSkill.Train(8); // Defeat a boss-level enemy.
				}

				return;
			}

			if (attackerSkill.Info.Rank == SkillRank.R7)
			{
				if (rating == PowerRating.Normal)
				{
					if (target.IsDead)
						attackerSkill.Train(1); // Defeat a similar-ranked enemy.
				}
				else if (rating == PowerRating.Strong)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(2); // Knock down a powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(3); // Defeat a powerful enemy.
				}
				else if (rating == PowerRating.Awful)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(4); // Knock down a very powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(5); // Defeat a very powerful enemy.
				}
				else if (rating == PowerRating.Boss)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(6); // Knock down a boss-level enemy.

					if (target.IsDead)
						attackerSkill.Train(7); // Defeat a boss-level enemy.
				}

				return;
			}

			if (attackerSkill.Info.Rank >= SkillRank.R6 && attackerSkill.Info.Rank <= SkillRank.R1)
			{
				if (rating == PowerRating.Strong)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(1); // Knock down a powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(2); // Defeat a powerful enemy.
				}
				else if (rating == PowerRating.Awful)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(3); // Knock down a very powerful enemy.

					if (target.IsDead)
						attackerSkill.Train(4); // Defeat a very powerful enemy.
				}
				else if (rating == PowerRating.Boss)
				{
					if (tAction.Has(TargetOptions.KnockDown))
						attackerSkill.Train(5); // Knock down a boss-level enemy.

					if (target.IsDead)
						attackerSkill.Train(6); // Defeat a boss-level enemy.
				}

				return;
			}
		}
	}
}
