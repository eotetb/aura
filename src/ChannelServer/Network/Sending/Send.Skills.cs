// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Channel.World.Entities;
using Aura.Shared.Network;
using Aura.Channel.World;
using Aura.Channel.Skills;
using Aura.Mabi.Const;
using Aura.Data.Database;
using Aura.Mabi.Network;
using Aura.Channel.Skills.Life;
using Aura.Channel.Skills.Base;

namespace Aura.Channel.Network.Sending
{
	public static partial class Send
	{
		/// <summary>
		/// Sends SkillInfo to creature's client.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		public static void SkillInfo(Creature creature, Skill skill)
		{
			var packet = new Packet(Op.SkillInfo, creature.EntityId);
			packet.PutBin(skill.Info);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends negative SkillRankUp to creature's client.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="creature"></param>
		public static void SkillAdvance_Fail(Creature creature)
		{
			var packet = new Packet(Op.SkillRankUp, creature.EntityId);
			packet.PutByte(false);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillRankUp to creature's client.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		public static void SkillRankUp(Creature creature, Skill skill)
		{
			var packet = new Packet(Op.SkillRankUp, creature.EntityId);
			packet.PutByte(1);
			packet.PutBin(skill.Info);
			packet.PutFloat(0);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Broadcasts RankUp in range of creature.
		/// </summary>
		/// <remarks>
		/// The second parameter is the rank, but doesn't seem to be necessary.
		/// </remarks>
		/// <param name="creature"></param>
		/// <param name="skillId">Excluded if 0</param>
		public static void RankUp(Creature creature, SkillId skillId = 0)
		{
			var packet = new Packet(Op.RankUp, creature.EntityId);
			if (skillId > 0)
				packet.PutUShort((ushort)skillId);
			packet.PutShort(1); // Rank

			creature.Region.Broadcast(packet, creature);
		}

		/// <summary>
		/// Sends SkillStartSilentCancel to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		public static void SkillStartSilentCancel(Creature creature, SkillId skillId)
		{
			var packet = new Packet(Op.SkillStartSilentCancel, creature.EntityId);
			packet.PutUShort((ushort)skillId);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillStopSilentCancel to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		public static void SkillStopSilentCancel(Creature creature, SkillId skillId)
		{
			var packet = new Packet(Op.SkillStopSilentCancel, creature.EntityId);
			packet.PutUShort((ushort)skillId);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillPrepareSilentCancel to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		public static void SkillPrepareSilentCancel(Creature creature, SkillId skillId)
		{
			var packet = new Packet(Op.SkillPrepareSilentCancel, creature.EntityId);
			packet.PutUShort((ushort)skillId);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUseSilentCancel to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		public static void SkillUseSilentCancel(Creature creature)
		{
			var packet = new Packet(Op.SkillUseSilentCancel, creature.EntityId);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillStart to creature's client or broadcasts it if skill is
		/// of type "BroadcastStartStop".
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="extra"></param>
		public static void SkillStart(Creature creature, Skill skill, string extra)
		{
			var packet = new Packet(Op.SkillStart, creature.EntityId);
			packet.PutUShort((ushort)skill.Info.Id);
			packet.PutString(extra);

			if (skill.Data.Type != SkillType.BroadcastStartStop)
				creature.Client.Send(packet);
			else
				creature.Region.Broadcast(packet, creature);
		}

		/// <summary>
		/// Sends SkillStart to creature's client or broadcasts it if skill is
		/// of type "BroadcastStartStop".
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="unkByte"></param>
		public static void SkillStart(Creature creature, Skill skill, byte unkByte)
		{
			var packet = new Packet(Op.SkillStart, creature.EntityId);
			packet.PutUShort((ushort)skill.Info.Id);
			packet.PutByte(unkByte);

			if (skill.Data.Type != SkillType.BroadcastStartStop)
				creature.Client.Send(packet);
			else
				creature.Region.Broadcast(packet, creature);
		}

		/// <summary>
		/// Sends SkillStop to creature's client or broadcasts it if skill is
		/// of type "BroadcastStartStop".
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="extra"></param>
		public static void SkillStop(Creature creature, Skill skill, string extra)
		{
			var packet = new Packet(Op.SkillStop, creature.EntityId);
			packet.PutUShort((ushort)skill.Info.Id);
			packet.PutString(extra);

			if (skill.Data.Type != SkillType.BroadcastStartStop)
				creature.Client.Send(packet);
			else
				creature.Region.Broadcast(packet, creature);
		}

		/// <summary>
		/// Sends SkillStop to creature's client or broadcasts it if skill is
		/// of type "BroadcastStartStop".
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="unkByte"></param>
		public static void SkillStop(Creature creature, Skill skill, byte unkByte)
		{
			var packet = new Packet(Op.SkillStop, creature.EntityId);
			packet.PutUShort((ushort)skill.Info.Id);
			packet.PutByte(unkByte);

			if (skill.Data.Type != SkillType.BroadcastStartStop)
				creature.Client.Send(packet);
			else
				creature.Region.Broadcast(packet, creature);
		}

		/// <summary>
		/// Sends SkillReady to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="extra"></param>
		public static void SkillReady(Creature creature, SkillId skillId, string extra = "")
		{
			var packet = new Packet(Op.SkillReady, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutString(extra);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillReady to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="unkInt"></param>
		public static void SkillReady(Creature creature, SkillId skillId, int unkInt)
		{
			var packet = new Packet(Op.SkillReady, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutInt(unkInt);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillReady to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="itemEntityId"></param>
		/// <param name="dyeEntityId"></param>
		public static void SkillReadyDye(Creature creature, SkillId skillId, long itemEntityId, long dyeEntityId)
		{
			var packet = new Packet(Op.SkillReady, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(itemEntityId);
			packet.PutLong(dyeEntityId);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		public static void SkillUse(Creature creature, SkillId skillId)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="unkByte"></param>
		public static void SkillUse(Creature creature, SkillId skillId, byte unkByte)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutByte(unkByte);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="unkInt"></param>
		public static void SkillUse(Creature creature, SkillId skillId, int unkInt)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutInt(unkInt);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="dict"></param>
		public static void SkillUse(Creature creature, SkillId skillId, string dict)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutString(dict);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="entityId"></param>
		/// <param name="unk1"></param>
		/// <param name="unk2"></param>
		public static void SkillUse(Creature creature, SkillId skillId, long entityId, int unk1, int unk2)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(entityId);
			packet.PutInt(unk1);
			packet.PutInt(unk2);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="entityId"></param>
		/// <param name="unk1"></param>
		public static void SkillUse(Creature creature, SkillId skillId, long entityId, int unk1)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(entityId);
			packet.PutInt(unk1);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="entityId"></param>
		/// <param name="unk1"></param>
		/// <param name="unk2"></param>
		public static void SkillUse(Creature creature, SkillId skillId, long entityId, bool unk1, string unk2)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(entityId);
			packet.PutByte(unk1);
			packet.PutString(unk2);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="entityId1"></param>
		/// <param name="entityId2"></param>
		public static void SkillUse(Creature creature, SkillId skillId, long entityId1, long entityId2)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(entityId1);
			packet.PutLong(entityId2);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="stun"></param>
		/// <param name="unk"></param>
		public static void SkillUseStun(Creature creature, SkillId skillId, int stun, int unk)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutInt(stun);
			packet.PutInt(unk);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="part"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public static void SkillUseDye(Creature creature, SkillId skillId, int part, short x, short y)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutInt(part);
			packet.PutShort(x);
			packet.PutShort(y);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="part"></param>
		/// <param name="unkByte"></param>
		public static void SkillUseDye(Creature creature, SkillId skillId, int part, byte unkByte)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutInt(part);
			packet.PutByte(unkByte);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillUse to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="entityId"></param>
		public static void SkillUseEntity(Creature creature, SkillId skillId, long entityId)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(entityId);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Broadcasts Effect in range of creature.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="instrument"></param>
		/// <param name="compressedMML"></param>
		/// <param name="rndScore"></param>
		public static void SkillUsePlayingInstrument(Creature creature, SkillId skillId, InstrumentType instrument, string compressedMML, int rndScore)
		{
			var packet = new Packet(Op.SkillUse, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(0);
			packet.PutByte(compressedMML != null); // has scroll
			if (compressedMML != null)
				packet.PutString(compressedMML);
			else
				packet.PutInt(rndScore);
			packet.PutInt(22124); // [200200, NA242 (2016-12-15)]
			packet.PutByte((byte)instrument);
			packet.PutByte(1);
			packet.PutByte(0);
			packet.PutInt(0); // [200200, NA242 (2016-12-15)]

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillTrainingUp to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skill"></param>
		/// <param name="exp">Exp gained</param>
		public static void SkillTrainingUp(Creature creature, Skill skill, float exp, string bonus = "")
		{
			var packet = new Packet(Op.SkillTrainingUp, creature.EntityId);
			packet.PutBin(skill.Info);
			packet.PutFloat(exp);
			packet.PutByte(1);
			packet.PutString(bonus); // (Specialized Skill Bonus: x2)

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillCancel to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		public static void SkillCancel(Creature creature)
		{
			var packet = new Packet(Op.SkillCancel, creature.EntityId);
			packet.PutByte(0);
			packet.PutByte(1);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillComplete to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		public static void SkillComplete(Creature creature, SkillId skillId)
		{
			var packet = new Packet(Op.SkillComplete, creature.EntityId);
			packet.PutUShort((ushort)skillId);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillComplete to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="unkByte"></param>
		public static void SkillComplete(Creature creature, SkillId skillId, byte unkByte)
		{
			var packet = new Packet(Op.SkillComplete, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutByte(unkByte);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillComplete to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="unkInt"></param>
		public static void SkillComplete(Creature creature, SkillId skillId, int unkInt)
		{
			var packet = new Packet(Op.SkillComplete, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutInt(unkInt);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillComplete to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="dict"></param>
		public static void SkillComplete(Creature creature, SkillId skillId, string dict)
		{
			var packet = new Packet(Op.SkillComplete, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutString(dict);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillComplete to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="entityId"></param>
		/// <param name="unkInt"></param>
		public static void SkillComplete(Creature creature, SkillId skillId, long entityId, int unkInt)
		{
			var packet = new Packet(Op.SkillComplete, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(entityId);
			packet.PutInt(unkInt);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillComplete to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="entityId"></param>
		/// <param name="unkInt1"></param>
		/// <param name="unkInt2"></param>
		public static void SkillComplete(Creature creature, SkillId skillId, long entityId, int unkInt1, int unkInt2)
		{
			var packet = new Packet(Op.SkillComplete, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(entityId);
			packet.PutInt(unkInt1);
			packet.PutInt(unkInt2);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillComplete to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="entityId1"></param>
		/// <param name="entityId2"></param>
		public static void SkillComplete(Creature creature, SkillId skillId, long entityId1, long entityId2)
		{
			var packet = new Packet(Op.SkillComplete, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(entityId1);
			packet.PutLong(entityId2);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillComplete to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="entityId"></param>
		/// <param name="unkInt"></param>
		/// <param name="unkShort"></param>
		public static void SkillCompleteUnk(Creature creature, SkillId skillId, long entityId, int unkInt, short unkShort)
		{
			var packet = new Packet(Op.SkillCompleteUnk, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(entityId);
			packet.PutInt(unkInt);
			packet.PutShort(unkShort);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillComplete to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="part"></param>
		public static void SkillCompleteDye(Creature creature, SkillId skillId, int part)
		{
			var packet = new Packet(Op.SkillComplete, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutInt(part);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillComplete to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="entityId"></param>
		public static void SkillCompleteEntity(Creature creature, SkillId skillId, long entityId)
		{
			var packet = new Packet(Op.SkillComplete, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutLong(entityId);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillPrepare to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="castTime">Not always the cast time.</param>
		public static void SkillPrepare(Creature creature, SkillId skillId, int castTime)
		{
			var packet = new Packet(Op.SkillPrepare, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			if (skillId != SkillId.None)
				packet.PutInt(castTime);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillPrepare to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="dict">.</param>
		public static void SkillPrepare(Creature creature, SkillId skillId, string dict)
		{
			var packet = new Packet(Op.SkillPrepare, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutString(dict);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Broadcasts SkillTeleport to creature's region.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public static void SkillTeleport(Creature creature, int x, int y)
		{
			var packet = new Packet(Op.SkillTeleport, creature.EntityId);
			packet.PutByte(0); //unk1
			packet.PutInt(x);
			packet.PutInt(y);
			packet.PutByte(0); //unk2

			creature.Region.Broadcast(packet);
		}

		/// <summary>
		/// Sends SharpMind to all creatures in range of user.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="target"></param>
		/// <param name="skillId"></param>
		/// <param name="state"></param>
		public static void SharpMind(Creature user, Creature target, SkillId skillId, SharpMindStatus state)
		{
			var packet = new Packet(Op.SharpMind, target.EntityId);
			packet.PutLong(user.EntityId);
			packet.PutByte(1);
			packet.PutByte(1);
			packet.PutUShort((ushort)skillId);
			packet.PutInt((int)state);

			target.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillStackSet to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="stacks"></param>
		public static void SkillStackSet(Creature creature, SkillId skillId, int stacks)
		{
			var packet = new Packet(Op.SkillStackSet, creature.EntityId);
			packet.PutByte((byte)stacks);
			packet.PutByte(1);
			packet.PutUShort((ushort)skillId);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Broadcasts CollectAnimation in creature's range.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="entityId"></param>
		/// <param name="collectId"></param>
		/// <param name="pos"></param>
		public static void CollectAnimation(Creature creature, long entityId, int collectId, Position pos)
		{
			var packet = new Packet(Op.CollectAnimation, creature.EntityId);
			packet.PutLong(entityId);
			packet.PutInt(collectId);
			packet.PutFloat(pos.X);
			packet.PutFloat(pos.Y);
			packet.PutFloat(1);

			creature.Region.Broadcast(packet, creature);
		}

		/// <summary>
		/// Broadcasts CollectAnimationCancel in creature's range.
		/// </summary>
		/// <param name="creature"></param>
		public static void CollectAnimationCancel(Creature creature)
		{
			var packet = new Packet(Op.CollectAnimationCancel, creature.EntityId);

			creature.Region.Broadcast(packet, creature);
		}

		/// <summary>
		/// Sends FishingActionRequired to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="catchSize"></param>
		/// <param name="time">The time you have to react.</param>
		/// <param name="fishSpeed">Fish speed for manual catching, 0 = no movement, 3+ = pretty challenging.</param>
		public static void FishingActionRequired(Creature creature, CatchSize catchSize, int time, float fishSpeed)
		{
			var packet = new Packet(Op.FishingActionRequired, creature.EntityId);
			packet.PutByte((byte)catchSize);
			packet.PutInt(time);
			packet.PutFloat(fishSpeed);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Broadcasts CombatSetAimR in creature's range, putting it in
		/// "aim mode".
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="targetEntityId">Set 0 for negative response.</param>
		/// <param name="skillId"></param>
		/// <param name="flag"></param>
		public static void CombatSetAimR(Creature creature, long targetEntityId, SkillId skillId, byte flag)
		{
			var packet = new Packet(Op.CombatSetAimR, creature.EntityId);
			packet.PutByte(targetEntityId != 0);
			if (targetEntityId != 0)
			{
				packet.PutLong(targetEntityId);
				packet.PutShort((short)skillId);
				packet.PutByte(flag);
			}

			creature.Region.Broadcast(packet, creature);
		}

		/// <summary>
		/// Sends ResetCooldown to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		public static void ResetCooldown(Creature creature, SkillId skillId)
		{
			var packet = new Packet(Op.ResetCooldown, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutByte(0); // end of list?

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends ProductionSuccessRequestR to creature's client, informing it
		/// about the success rate it requested.
		/// </summary>
		/// <remarks>
		/// This version of the packet is used for "normal" Production,
		/// like Weaving and Handicraft.
		/// </remarks>
		/// <param name="creature"></param>
		/// <param name="skillId">Skill the rate is used for.</param>
		/// <param name="successRate">
		/// Bonus success rate, added to the value calculated by the client,
		/// or the total success rate to use, if totalSuccess is true.
		/// </param>
		/// <param name="totalSuccess">
		/// If true, the client will display the given successRate, if it's false,
		/// it will calculate the default rate itself and add successRate as bonus.
		/// </param>
		public static void ProductionSuccessRequestR(Creature creature, SkillId skillId, float successRate, bool totalSuccess)
		{
			var gp = new Packet(Op.ProductionSuccessRequestR, creature.EntityId);

			gp.PutByte(1);
			gp.PutUShort((ushort)skillId);
			gp.PutShort(5); // unkShort1?
			gp.PutFloat(successRate);
			gp.PutByte(0);
			gp.PutByte(totalSuccess);

			creature.Client.Send(gp);
		}

		/// <summary>
		/// Sends ProductionSuccessRequestR to creature's client, informing it
		/// about the success rate it requested.
		/// </summary>
		/// <remarks>
		/// This version of the packet is used for Tailoring and Blacksmithing.
		/// </remarks>
		/// <param name="creature"></param>
		/// <param name="skillId">Skill the rate is used for.</param>
		/// <param name="successRate">
		/// Bonus success rate, added to the value calculated by the client,
		/// or the total success rate to use, if totalSuccess is true.
		/// </param>
		/// <param name="totalSuccess">
		/// If true, the client will display the given successRate, if it's false,
		/// it will calculate the default rate itself and add successRate as bonus.
		/// </param>
		public static void ProductionSuccessRequestR(Creature creature, SkillId skillId, float successRate, bool totalSuccess, float unkFloat)
		{
			var gp = new Packet(Op.ProductionSuccessRequestR, creature.EntityId);

			gp.PutByte(1);
			gp.PutUShort((ushort)skillId);
			gp.PutShort(6);
			gp.PutFloat(successRate);
			gp.PutByte(0);
			gp.PutByte(totalSuccess);
			gp.PutFloat(unkFloat);

			creature.Client.Send(gp);
		}

		/// <summary>
		/// Sends TailoringMiniGame to creature's client to start tailoring minigame.
		/// </summary>
		/// <remarks>
		/// The offsets specify the distance of the "stitch points" from the
		/// center of the 200x200px minigame field. X is 1:1 the distance
		/// from the center for each point, while Y gets added up.
		/// The point closest to the center is Y/2 px away, the second Y/2+Y,
		/// and the third Y/2+Y*2.
		/// 
		/// Deviation is an array of 6 values, one for each point, that specify
		/// the amount of pixels your clicks can deviate from the actual
		/// position you clicked. For example, if you click on 60x180 for the
		/// first point, and the first byte in deviation is 3, the actual
		/// position sent to the server is between 57x177 and 63x183,
		/// randomized by the client.
		/// If the deviation values are too big, the minigame glitches and is
		/// likely to fail, the biggest value seen in logs was 4.
		/// </remarks>
		/// <param name="creature"></param>
		/// <param name="item">Item that is to be finished.</param>
		/// <param name="xOffset">Offset of stitch points on the x-axis.</param>
		/// <param name="yOffset">Offset of stitch points on the y-axis.</param>
		/// <param name="deviation">Randomization for the 6 stitch points.</param>
		public static void TailoringMiniGame(Creature creature, Item item, int xOffset, int yOffset, byte[] deviation, byte deviation2)
		{
			if (deviation == null || deviation.Length != 6)
				throw new ArgumentException("rng needs exactly 6 values.");

			var packet = new Packet(Op.TailoringMiniGame, creature.EntityId);

			packet.PutShort((short)xOffset);
			packet.PutShort((short)yOffset);
			packet.PutBin(deviation);

			// Modifies cursor size, glitches the minigame if smaller than
			// deviation? Seems to be a general deviation that applies to
			// all points? Setting all deviations, incl this, to 0 gives
			// the most spot-on results.
			packet.PutByte(deviation2);

			packet.PutLong(0);
			packet.PutInt(0);
			packet.PutLong(item.EntityId);
			packet.PutInt(0);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends BlacksmithingMiniGame to creature's client, which starts
		/// the Blacksmithing mini-game.
		/// </summary>
		/// <remarks>
		/// The position of the dots is relative to the upper left of the
		/// field. They land exactly on those spots after "wavering" for a
		/// moment. This wavering is randomized on the client side and
		/// doesn't affect anything.
		/// 
		/// The time bar is always the same, but the time it takes to fill
		/// up changes based on the "time displacement". The lower the value,
		/// the longer it takes to fill up. Using values that are too high
		/// or too low mess up the calculations and cause confusing results.
		/// The official range seems to be between ~0.81 and ~0.98.
		/// </remarks>
		/// <param name="creature"></param>
		/// <param name="prop"></param>
		/// <param name="item"></param>
		/// <param name="dots"></param>
		/// <param name="deviation"></param>
		public static void BlacksmithingMiniGame(Creature creature, Prop prop, Item item, List<BlacksmithDot> dots, int deviation)
		{
			if (dots == null || dots.Count != 5)
				throw new ArgumentException("5 dots required.");

			var packet = new Packet(Op.BlacksmithingMiniGame, creature.EntityId);

			// Untested if this is actually the deviation/cursor size,
			// but Tailoring does something very similar. Just like with
			// Tailoring, wrong values cause failed games.
			packet.PutShort((short)deviation);

			foreach (var dot in dots)
			{
				packet.PutShort((short)dot.X);
				packet.PutShort((short)dot.Y);
				packet.PutFloat(dot.TimeDisplacement);
				packet.PutShort((short)dot.Deviation);
			}

			packet.PutLong(prop.EntityId);
			packet.PutInt(0);
			packet.PutLong(item.EntityId);
			packet.PutInt(0);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Sends SkillPrepareCancellation to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="skillId"></param>
		/// <param name="unkByte"></param>
		public static void SkillPrepareCancellation(Creature creature, SkillId skillId, byte unkByte)
		{
			var packet = new Packet(Op.SkillPrepareCancellation, creature.EntityId);
			packet.PutUShort((ushort)skillId);
			packet.PutByte(unkByte);

			creature.Client.Send(packet);
		}

		/// <summary>
		/// Dummy response for SkillApTraining to prevent locking.
		/// </summary>
		/// <param name="creature"></param>
		public static void SkillApTrainingR(Creature creature)
		{
			var packet = new Packet(Op.SkillApTrainingR, creature.EntityId);
			packet.PutByte(false);

			creature.Client.Send(packet);
		}
	}
}
