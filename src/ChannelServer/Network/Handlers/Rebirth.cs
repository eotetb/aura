// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Util;
using Aura.Channel.World.Entities;
using Aura.Data;
using Aura.Data.Database;
using Aura.Mabi.Const;
using Aura.Mabi.Network;
using Aura.Shared.Network;
using Aura.Shared.Util;
using System;

namespace Aura.Channel.Network.Handlers
{
	public partial class ChannelServerHandlers : PacketHandlerManager<ChannelClient>
	{
		/// <summary>
		/// Sent after selecting all rebirth options.
		/// </summary>
		/// <remarks>
		/// To get into the rebirth screen, send "<rebirth style='-1'/>"
		/// as an NPC message. The dialog result will be either "@rebirth"
		/// or "@cancel".
		/// 
		/// You can always go to Nao for rebirthing from the login screen,
		/// because the last rebirth time is only sent in the full
		/// creature info in 5209. In-game, the client checks the date,
		/// preventing one from going there before it should be possible
		/// under NA rules.
		/// </remarks>
		/// <example>
		/// 001 [0000000000000000] Long   : 0
		/// 002 [..............01] Byte   : 1 - Reset age toggle
		/// 003 [............0011]	Short  : 17 - New age if ^ true
		/// 004 [..............01] Byte   : 1 - Reset level toggle
		/// 005 [..............01] Byte   : 1 - Change gender toggle
		/// 006 [........00002712]	Int    : 10002 - New race if ^ true
		/// 007 [..............10] Byte   : 16
		/// 008 [........00000FF9] Int    : 4089
		/// 009 [..............1C] Byte   : 28
		/// 010 [............0020] Short  : 32
		/// 011 [..............1D] Byte   : 29
		/// 012 [..............02] Byte   : 2
		/// 013 [........00001324] Int    : 4900
		/// 014 [..............00] Byte   : 0
		/// 015 [..............12] Byte   : 18
		/// 
		/// Old rebirth packet
		/// 0001 [0000000000000000] Long   : 0
		/// 0002 [..............01] Byte   : 1
		/// 0003 [............0011] Short  : 17
		/// 0004 [..............01] Byte   : 1
		/// 0005 [..............00] Byte   : 0
		/// 0006 [..............00] Byte   : 0
		/// </example>
		[PacketHandler(Op.RequestRebirth)]
		public void RequestRebirth(ChannelClient client, Packet packet)
		{
			var unkLong = packet.GetLong();
			var resetAge = packet.GetBool();
			short age = 0;
			if (resetAge)
				age = packet.GetShort();
			var resetLevel = packet.GetBool();
			var resetGender = packet.GetBool();
			var race = 0;
			if (resetGender)
				race = packet.GetInt();
			var skinColor = packet.GetByte();
			var hairItemId = packet.GetInt();
			var hairColor = packet.GetInt(); // [200200, NA247 (2017-03-17)] Changed from byte to int
			var eyeType = packet.GetShort();
			var eyeColor = packet.GetByte();
			var mouthType = packet.GetByte();
			var faceItemId = packet.GetInt();
			var location = (RebirthLocation)packet.GetByte();
			var talent = packet.GetByte();

			// Get required data
			var creature = client.GetCreatureSafe(packet.Id);

			var hair = creature.Inventory.GetItemAt(Pocket.Hair, 0, 0);
			var face = creature.Inventory.GetItemAt(Pocket.Face, 0, 0);
			var hairItemData = AuraData.ItemDb.Find(hairItemId);
			var faceItemData = AuraData.ItemDb.Find(faceItemId);

			var raceData = (resetGender ? AuraData.RaceDb.Find(race) : creature.RaceData);
			var hairStyleData = AuraData.CharacterStyleDb.Find(raceData.Gender == Gender.Male ? CharacterStyleType.MaleHairStyle : CharacterStyleType.FemaleHairStyle, hairItemId);
			var faceData = AuraData.CharacterStyleDb.Find(raceData.Gender == Gender.Male ? CharacterStyleType.MaleFace : CharacterStyleType.FemaleFace, faceItemId);
			var hairColorData = AuraData.CharacterStyleDb.Find(CharacterStyleType.HairColor, hairColor);
			var skinColorData = AuraData.CharacterStyleDb.Find(CharacterStyleType.SkinColor, skinColor);
			var eyeColorData = AuraData.CharacterStyleDb.Find(CharacterStyleType.EyeColor, eyeColor);
			var eyeTypeData = AuraData.CharacterStyleDb.Find(CharacterStyleType.EyeType, eyeType);
			var mouthTypeData = AuraData.CharacterStyleDb.Find(CharacterStyleType.MouthType, mouthType);

			// Check age
			if (resetAge && (age < 10 || age > 17 || age > creature.Age))
				throw new SevereViolation("Player tried to rebirth with invalid age ({0}).", age);

			// Check race, the new one would be the same as the old one,
			// if you remove the last number (1 for female, 2 for male).
			if (resetGender && (creature.RaceId & ~3) != (race & ~3))
				throw new SevereViolation("Player tried to rebirth with invalid race ({0}).", race);

			// Check hair
			if (hairItemData == null || (hairItemData.Type != ItemType.Hair && hairItemData.Type != ItemType.Face))
				throw new SevereViolation("Player tried to rebirth with invalid hair ({0}).", hairItemId);

			// Check face
			if (faceItemData == null || (faceItemData.Type != ItemType.Face && faceItemData.Type != ItemType.Hair))
				throw new SevereViolation("Player tried to rebirth with invalid face ({0}).", faceItemId);

			// Check location
			if (location < RebirthLocation.Last || location > RebirthLocation.Iria)
				throw new SevereViolation("Player tried to rebirth with invalid location ({0}).", location);

			// Check styles
			var totalPrice = 0;
			if (hairItemId != hair.Info.Id)
			{
				if (hairStyleData == null)
				{
					Log.Error("Rebirth: Unknown hair style '{0}'.", hairItemId);
					goto L_Fail;
				}

				if (!raceData.HasTag(hairStyleData.Races))
					throw new ModerateViolation("Player tried to rebirth with hair not available to their race: {0}, {1}", creature.RaceId, hairItemId);

				totalPrice += hairStyleData.Price;
			}
			if (faceItemId != face.Info.Id)
			{
				if (faceData == null)
				{
					Log.Error("Rebirth: Unknown face '{0}'.", hairItemId);
					goto L_Fail;
				}

				if (!raceData.HasTag(faceData.Races))
					throw new ModerateViolation("Player tried to rebirth with face not available to their race: {0}, {1}", creature.RaceId, faceItemId);

				totalPrice += faceData.Price;
			}
			if (hairColor != (byte)hair.Info.Color1)
			{
				if (hairColorData == null)
				{
					Log.Error("Rebirth: Unknown hair color '{0}'.", hairItemId);
					goto L_Fail;
				}

				if (!raceData.HasTag(hairColorData.Races))
					throw new ModerateViolation("Player tried to rebirth with hair color not available to their race: {0}, {1}", creature.RaceId, hairColor);

				totalPrice += hairColorData.Price;
			}
			if (skinColor != creature.SkinColor)
			{
				if (skinColorData == null)
				{
					Log.Error("Rebirth: Unknown skin color '{0}'.", hairItemId);
					goto L_Fail;
				}

				if (!raceData.HasTag(skinColorData.Races))
					throw new ModerateViolation("Player tried to rebirth with skin color not available to their race: {0}, {1}", creature.RaceId, skinColor);

				totalPrice += skinColorData.Price;
			}
			if (eyeColor != creature.EyeColor)
			{
				if (eyeColorData == null)
				{
					Log.Error("Rebirth: Unknown eye color '{0}'.", hairItemId);
					goto L_Fail;
				}

				if (!raceData.HasTag(eyeColorData.Races))
					throw new ModerateViolation("Player tried to rebirth with eye color not available to their race: {0}, {1}", creature.RaceId, eyeColor);

				totalPrice += eyeColorData.Price;
			}
			if (eyeType != creature.EyeType)
			{
				if (eyeTypeData == null)
				{
					Log.Error("Rebirth: Unknown eye type '{0}'.", hairItemId);
					goto L_Fail;
				}

				if (!raceData.HasTag(eyeTypeData.Races))
					throw new ModerateViolation("Player tried to rebirth with eye type not available to their race: {0}, {1}", creature.RaceId, eyeType);

				totalPrice += eyeTypeData.Price;
			}
			if (mouthType != creature.MouthType)
			{
				if (mouthTypeData == null)
				{
					Log.Error("Rebirth: Unknown mouth type '{0}'.", hairItemId);
					goto L_Fail;
				}

				if (!raceData.HasTag(mouthTypeData.Races))
					throw new ModerateViolation("Player tried to rebirth with eye type not available to their race: {0}, {1}", creature.RaceId, mouthType);

				totalPrice += mouthTypeData.Price;
			}

			// Check points
			// Sanity check, client should prevent this.
			if (creature.Points < totalPrice)
				throw new ModerateViolation("Player tried to rebirth without being able to pay for the selected options, total price: {0}, Points: {1}", totalPrice, creature.Points);

			// Reset age
			if (resetAge)
			{
				creature.Age = age;
				creature.Height = Math.Min(1.0f, 1.0f / 7.0f * (age - 10.0f)); // 0 ~ 1.0
			}

			// Reset gender (race)
			if (resetGender)
				creature.RaceId = race;

			// Reset level and stats
			if (resetLevel)
			{
				creature.Level = 1;
				creature.Exp = 0;

				var ageStats = AuraData.StatsBaseDb.Find(creature.RaceId, creature.Age);
				creature.LifeMaxBase = ageStats.Life;
				creature.ManaMaxBase = ageStats.Mana;
				creature.StaminaMaxBase = ageStats.Stamina;
				creature.StrBase = ageStats.Str;
				creature.IntBase = ageStats.Int;
				creature.DexBase = ageStats.Dex;
				creature.WillBase = ageStats.Will;
				creature.LuckBase = ageStats.Luck;
				creature.FullHeal();
			}

			// Reset body
			creature.EyeType = eyeType;
			creature.EyeColor = eyeColor;
			creature.MouthType = mouthType;
			creature.SkinColor = skinColor;
			creature.Weight = 1;
			creature.Upper = 1;
			creature.Lower = 1;

			// Update face
			face.Info.Id = faceItemId;
			face.Info.Color1 = skinColor;

			// Update hair
			hair.Info.Id = hairItemId;
			hair.Info.Color1 = (uint)hairColor + 0x10000000u;

			// Reset LastX times
			var player = (PlayerCreature)creature;
			player.LastAging = DateTime.Now;
			player.LastRebirth = DateTime.Now;
			player.RebirthCount++;

			// Prevent pre-G4 Iria rebirth
			if (location == RebirthLocation.Iria && !AuraData.FeaturesDb.IsEnabled("IriaRebirth"))
				location = RebirthLocation.Tir;

			// Location
			switch (location)
			{
				// Tir beginner area
				case RebirthLocation.Tir: creature.SetLocation(125, 21489, 76421); break;

				// Iria beginner area (Rano)
				case RebirthLocation.Iria: creature.SetLocation(3001, 164533, 161862); break;
			}

			// Just rebirthed switch
			creature.Activate(CreatureStates.JustRebirthed);

			// Success
			Send.RequestRebirthR(creature, true);

			// Update points
			if (totalPrice != 0)
				creature.Points -= totalPrice;

			return;

		L_Fail:
			// Kill after fail packet, otherwise player might get stuck.
			Send.RequestRebirthR(creature, false);
			client.Kill();
		}

		/// <summary>
		/// Sent upon entering the rebirth screen.
		/// </summary>
		/// <remarks>
		/// A lack of a response does not lock the client.
		/// </remarks>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.EnterRebirth)]
		public void EnterRebirth(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			Send.EnterRebirthR(creature);
			Send.PointsUpdate(creature, creature.Points);
		}
	}
}
