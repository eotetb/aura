// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Skills.Base;
using Aura.Channel.Skills.Combat;
using Aura.Channel.World;
using Aura.Channel.World.Entities;
using Aura.Mabi.Const;
using Aura.Mabi.Network;
using Aura.Shared.Util;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Aura.Channel.Skills.Magic
{
	/// <summary>
	/// Skill handler for Fireball.
	/// </summary>
	/// <remarks>
	/// Var1: Min Damage
	/// Var2: Max Damage
	/// </remarks>
	[Skill(SkillId.Fireball)]
	public class Fireball : IPreparable, IReadyable, IUseable, ICompletable, ICancelable, ICustomHitCanceler
	{
		/// <summary>
		/// Radius of the explosion.
		/// </summary>
		private const int ExplosionRadius = 800;

		/// <summary>
		/// Time the fire ball is in the air.
		/// </summary>
		private const int FlyTime = 2500;

		/// <summary>
		/// Target's stun.
		/// </summary>
		private const int TargetStun = 2000;

		/// <summary>
		/// Distance to knock back targets.
		/// </summary>
		private const int KnockbackDistance = 400;

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
				creature.Notice("You need a Fire Wand to use this skill.");
				return false;
			}

			Send.SkillInitEffect(creature, "fireball", skill.Info.Id);
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

			// No weapon, no Fireball.
			if (rightHand == null)
				return false;

			// TODO: Elemental charging

			// Disallow non-wands and staffs without special tag.
			if (!rightHand.HasTag("/fire_wand/|/no_bolt_stack/"))
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
				skill.Train(1); // Fully charge Fireball.

			Send.Effect(creature, Effect.StackUpdate, "firebolt", (byte)skill.Stacks, (byte)0);
			Send.Effect(creature, Effect.HoldMagic, "fireball", (ushort)skill.Info.Id);
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
		/// Uses skill, firing the fire ball and scheduling the impact.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		public void Use(Creature attacker, Skill skill, Packet packet)
		{
			var targetEntityId = packet.GetLong();
			var unkInt1 = 0;
			var unkInt2 = 0;

			// The following two ints aren't sent always?
			if (packet.NextIs(PacketElementType.Int)) unkInt1 = packet.GetInt();
			if (packet.NextIs(PacketElementType.Int)) unkInt2 = packet.GetInt();

			this.Use(attacker, skill, targetEntityId, unkInt1, unkInt2);
		}

		/// <summary>
		/// Uses skill, firing the fire ball and scheduling the impact.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <param name="packet"></param>
		public void Use(Creature attacker, Skill skill, long targetEntityId, int unkIn1, int unkInt2)
		{
			if (skill.Stacks < skill.RankData.StackMax)
			{
				attacker.Skills.CancelActiveSkill();
				Log.Warning("Fireball.Use: User '{0}' tried to fire a not fully charged Fireball.", attacker.Client.Account.Id);
				return;
			}

			var target = attacker.Region.GetCreature(targetEntityId);
			if (target == null)
			{
				Log.Warning("Fireball.Use: Creature '0x{0}' tried to use Fireball on invalid target.", attacker.EntityId);
				Send.SkillUse(attacker, skill.Info.Id, targetEntityId, unkIn1, unkInt2);
				return;
			}

			var regionId = attacker.RegionId;
			var attackerPos = attacker.GetPosition();
			var targetPos = target.GetPosition();

			Send.Effect(attacker, Effect.UseMagic, "fireball", (byte)1, targetEntityId, (ushort)skill.Info.Id);

			attacker.TurnTo(targetPos);

			var fromX = (float)attackerPos.X;
			var fromY = (float)attackerPos.Y;
			var toX = (float)targetPos.X;
			var toY = (float)targetPos.Y;
			var time = FlyTime;

			var fireballProp = new Prop(280, regionId, targetPos.X, targetPos.Y, 0.19f, 1);
			fireballProp.DisappearTime = DateTime.Now.AddMilliseconds(FlyTime + 1000);
			attacker.Region.AddProp(fireballProp);

			Send.Effect(fireballProp, Effect.FireballFly, 0, regionId, fromX, fromY, toX, toY, time, (byte)0, (ushort)skill.Info.Id);

			SkillHelper.UpdateWeapon(attacker, target, ProficiencyGainType.Melee, attacker.RightHand);

			Send.SkillUse(attacker, skill.Info.Id, targetEntityId, unkIn1, unkInt2);

			skill.Stacks = 0;
			Send.Effect(attacker, Effect.StackUpdate, "firebolt", (byte)0, (byte)0);

			Task.Delay(time).ContinueWith(_ => this.Impact(attacker, skill, fireballProp));
		}

		/// <summary>
		/// Handles explosion.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <param name="fireballProp"></param>
		private void Impact(Creature attacker, Skill skill, Prop fireballProp)
		{
			var regionId = attacker.RegionId;
			var propPos = fireballProp.GetPosition();
			var targetLocation = new Location(regionId, propPos);
			var targets = attacker.GetTargetableCreaturesAround(propPos, ExplosionRadius);

			var aAction = new AttackerAction(CombatActionType.SpecialHit, attacker, targetLocation.ToLocationId(), skill.Info.Id);
			aAction.Set(AttackerOptions.UseEffect);
			aAction.PropId = fireballProp.EntityId;

			var cap = new CombatActionPack(attacker, skill.Info.Id, aAction);

			foreach (var target in targets)
			{
				target.StopMove();

				var tAction = new TargetAction(CombatActionType.TakeHit, target, attacker, skill.Info.Id);
				tAction.Set(TargetOptions.Result | TargetOptions.KnockDown);
				tAction.Stun = TargetStun;
				tAction.Delay = 200;
				tAction.EffectFlags = EffectFlags.SpecialRangeHit;

				cap.Add(tAction);

				// Damage
				var damage = this.GetDamage(attacker, skill);

				// Elements
				damage *= this.GetElementalDamageMultiplier(attacker, target);

				// Critical Hit
				var critChance = attacker.GetTotalCritChance(target.Protection, true);
				CriticalHit.Handle(attacker, critChance, ref damage, tAction);

				// Reduce damage
				SkillHelper.HandleMagicDefenseProtection(target, ref damage);
				SkillHelper.HandleConditions(attacker, target, ref damage);
				ManaShield.Handle(target, ref damage, tAction);
				ManaDeflector.Handle(attacker, target, ref damage, tAction);

				// Deal damage
				if (damage > 0)
					target.TakeDamage(tAction.Damage = damage, attacker);
				target.Aggro(attacker);

				// Knockback
				target.Stability = Creature.MinStability;
				target.GetShoved(fireballProp, KnockbackDistance);
				if (target.IsDead)
					tAction.Set(TargetOptions.FinishingKnockDown);

				this.Train(skill, tAction);
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
			Send.Effect(creature, Effect.StackUpdate, "firebolt", (byte)skill.Stacks, (byte)0);
			Send.Effect(creature, Effect.CancelMagic, "fireball", (short)skill.Info.Id);
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
			Send.Effect(creature, Effect.StackUpdate, "firebolt", (byte)skill.Stacks, (byte)0);
		}

		/// <summary>
		/// Returns damage for attacker using skill.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="skill"></param>
		/// <returns></returns>
		protected float GetDamage(Creature attacker, Skill skill)
		{
			var damage = attacker.GetRndMagicDamage(skill, skill.RankData.Var1, skill.RankData.Var2);

			return damage;
		}

		/// <summary>
		/// Returns elemental damage multiplier for this skill.
		/// </summary>
		/// <param name="attacker"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		protected float GetElementalDamageMultiplier(Creature attacker, Creature target)
		{
			return attacker.CalculateElementalDamageMultiplier(0, Creature.MaxElementalAffinity, 0, target);
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
