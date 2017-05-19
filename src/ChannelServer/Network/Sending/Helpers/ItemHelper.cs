// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.World.Entities;
using Aura.Mabi.Network;
using Aura.Shared.Network;

namespace Aura.Channel.Network.Sending.Helpers
{
	public static class ItemHelper
	{
		public static Packet AddItemInfo(this Packet packet, Item item, ItemPacketType type)
		{
			var isEgoWeapon = item.HasTag("/ego_weapon/");
			var isGuildRobe = item.HasTag("/guild_robe/");

			packet.PutLong(item.EntityId);
			packet.PutByte((byte)type);
			packet.PutBin(item.Info);

			if (isGuildRobe)
				// EBCL1:4:-16351525;EBCL2:4:-875718;EBLM1:1:46;EBLM2:1:11;EBLM3:1:4; (GLDNAM:s:European;)
				packet.PutString(item.MetaData1.ToString());

			if (type == ItemPacketType.Public)
			{
				packet.PutByte(1);

				// Affects color of dropped item's name (blue or green),
				// indicating its value.
				packet.PutByte((byte)item.UpgradeEffectCount);

				//packet.PutByte(0); // Bitmask
				// if & 1
				//     float
				packet.PutByte(1);
				packet.PutFloat(1); // Size multiplicator *hint: Server side giant key mod*

				packet.PutByte(item.FirstTimeAppear); // 0: No bouncing, 1: Bouncing, 2: Delayed bouncing
			}
			else if (type == ItemPacketType.Private)
			{
				packet.PutBin(item.OptionInfo);

				// Ego data
				if (isEgoWeapon)
				{
					packet.PutString(item.EgoInfo.Name);
					packet.PutByte((byte)item.EgoInfo.Race);
					packet.PutByte(0); // ? increased from 14 to 18 when I fed a bottle to a sword

					packet.PutByte(item.EgoInfo.SocialLevel);
					packet.PutInt(item.EgoInfo.SocialExp);
					packet.PutByte(item.EgoInfo.StrLevel);
					packet.PutInt(item.EgoInfo.StrExp);
					packet.PutByte(item.EgoInfo.IntLevel);
					packet.PutInt(item.EgoInfo.IntExp);
					packet.PutByte(item.EgoInfo.DexLevel);
					packet.PutInt(item.EgoInfo.DexExp);
					packet.PutByte(item.EgoInfo.WillLevel);
					packet.PutInt(item.EgoInfo.WillExp);
					packet.PutByte(item.EgoInfo.LuckLevel);
					packet.PutInt(item.EgoInfo.LuckExp);
					packet.PutByte(item.EgoInfo.AwakeningEnergy);
					packet.PutInt(item.EgoInfo.AwakeningExp);

					packet.PutLong(0);
					packet.PutLong(item.EgoInfo.LastFeeding); // Last feeding time?
					packet.PutInt(0);
				}

				packet.PutString(item.MetaData1.ToString());
				packet.PutString(item.MetaData2.ToString());

				// Upgrades
				var upgradeEffects = item.GetUpgradeEffects();
				packet.PutByte((byte)upgradeEffects.Length);
				foreach (var upgradeEffect in upgradeEffects)
					packet.PutBin(upgradeEffect);

				// Special upgrades? (example)
				//0608 [0000000000000000] Long   : 0
				//0609 [........00000002] Int    : 2
				//0610 [........00000024] Int    : 36
				//0611 [........00000008] Int    : 8
				//0612 [........00000026] Int    : 38
				//0613 [........00000004] Int    : 4

				packet.PutLong(item.QuestId);

				if (isGuildRobe)
					packet.PutString(item.MetaData1.GetString("GLDNAM"));

				// In NA235 (Sep. 2016) we found an additional integer for
				// Scythe that Reaps Darkness here, with no known purpose.
				// In NA236 we had to remove said int again.
				// It will be missed.
				//if (item.Info.Id == 41237)
				//	packet.PutInt(0);

				// [190100, NA200 (2015-01-15)] ?
				{
					packet.PutByte(item.IsNew);
					packet.PutByte(0);
				}

				// [200200, NA252 (2017-05-18)]
				// New long that is equal to the owner's entity id for all
				// items in the creature info packet. Maybe the id of the
				// owner?
				{
					packet.PutLong(0);
				}
			}

			return packet;
		}
	}

	public enum ItemPacketType : byte
	{
		/// <summary>
		/// Used in public appears, doesn't include option info.
		/// </summary>
		Public = 1,

		/// <summary>
		/// Used in private item packets, includes option info.
		/// </summary>
		Private = 2
	}
}
