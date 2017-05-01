// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Shared.Network;
using Aura.Channel.Network.Sending;
using Aura.Shared.Util;
using Aura.Mabi.Const;
using Aura.Data;
using Aura.Mabi.Network;
using Aura.Mabi;
using Aura.Channel.World.Entities;

namespace Aura.Channel.Network.Handlers
{
	public partial class ChannelServerHandlers : PacketHandlerManager<ChannelClient>
	{
		/// <summary>
		/// Sent on login to request a list of new mails.
		/// </summary>
		/// <remarks>
		/// Only here to get rid of the unimplemented log for now.
		/// </remarks>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.MailsRequest)]
		public void MailsRequest(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			// Empty answer for now.
			Send.MailsRequestR(creature);
		}

		/// <summary>
		/// Sent on login, answer determines whether the SOS button is displayed.
		/// </summary>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.SosButtonRequest)]
		public void SosButtonRequest(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			// Disable by default, until we have the whole thing.
			Send.SosButtonRequestR(creature, false);
		}

		/// <summary>
		/// Sent on login to get homestead information.
		/// </summary>
		/// <remarks>
		/// Only called once, a few seconds after the player logged in.
		/// This makes it a good place for OnPlayerLoggedIn,
		/// at that point it's safe to do anything.
		/// </remarks>
		/// <example>
		/// 001 [..............00] Byte   : 0
		/// </example>
		[PacketHandler(Op.HomesteadInfoRequest)]
		public void HomesteadInfoRequest(ChannelClient client, Packet packet)
		{
			var unkByte = packet.GetByte();

			var creature = client.GetCreatureSafe(packet.Id);

			// Default answer for now
			Send.HomesteadInfoRequestR(creature);

			// Re-open GMCP
			if (creature.Vars.Perm.GMCP != null && client.Account.Authority >= ChannelServer.Instance.Conf.World.GmcpMinAuth)
				Send.GmcpOpen(creature);

			ChannelServer.Instance.Events.OnPlayerLoggedIn(creature);

			// Temp fix for Novice rank Support Shot, that shouldn't have been
			// given to players. TODO: Remove in a few days. (exec, 2015-01-19)
			if (creature.Skills.Is(SkillId.SupportShot, SkillRank.Novice))
				creature.Skills.Give(SkillId.SupportShot, SkillRank.RF);
		}

		/// <summary>
		/// Sent when selecting "Enter Homestead" in friend/guild list.
		/// Dummy handler so characters don't get locked when clicking
		/// the button.
		/// </summary>
		/// <example>
		/// 001 [..............02] Byte   : 2
		/// 002 [................] String : Aura
		/// 003 [................] String : Zerono
		/// </example>
		[PacketHandler(Op.HomesteadEnterRequest)]
		public void HomesteadEnterRequest(ChannelClient client, Packet packet)
		{
			var unkByte = packet.GetByte();
			var serverName = packet.GetString();
			var characterName = packet.GetString();

			var creature = client.GetCreatureSafe(packet.Id);

			Send.MsgBox(creature, Localization.Get("This feature hasn't been implemented yet."));
			Send.HomesteadEnterRequestR(creature);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Dummy handler. Answer is a 1 byte with 2 0 ints.
		/// Sent together with Homestead info request on login.
		/// </remarks>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.CollectionRequest)]
		public void CollectionRequest(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			// Default answer
			Send.CollectionRequestR(creature);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Dummy handler. Sent on login and after certain warps.
		/// Appears to be a request for the cool down
		/// of the continent warp (see response).
		/// </remarks>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.ContinentWarpCoolDown)]
		public void ContinentWarpCoolDown(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			// Default answer
			Send.ContinentWarpCoolDownR(creature);
		}

		/// <summary>
		/// Sent when the cutscene is over.
		/// </summary>
		/// <example>
		/// 001 [........000186A4] Int    : 100004
		/// </example>
		[PacketHandler(Op.FinishedCutscene)]
		public void FinishedCutscene(ChannelClient client, Packet packet)
		{
			var unkInt = packet.GetInt(); // Cutscene id?

			var creature = client.GetCreatureSafe(packet.Id);

			// Check if there is a cutscene to cancel
			if (creature.Temp.CurrentCutscene == null)
			{
				// This can happen if multiple member's cutscenes end at the
				// same time, they all send the finish packet, no matter whether
				// they're the leader or not. (Is that normal?)
				// The cutscene could also be set to null due to a bug though,
				// in which case players get stuck in it, without any error
				// message... TODO: We need a better check than "== null".
				//Log.Error("FinishedCutscene: Player '{0}' tried to finish invalid cutscene.", creature.EntityIdHex);
				return;
			}

			// Only leader can stop the cutscene
			if (creature.Temp.CurrentCutscene.Leader != creature)
			{
				// Unofficial
				Send.Notice(creature, Localization.Get("Someone else is still watching the cutscene."));
				return;
			}

			// Finish cutscene
			creature.Temp.CurrentCutscene.Finish();
		}

		/// <summary>
		/// Sent to use gesture.
		/// </summary>
		/// <example>
		/// 001 [................] String : rare_1
		/// </example>
		[PacketHandler(Op.UseGesture)]
		public void UseGesture(ChannelClient client, Packet packet)
		{
			var gestureName = packet.GetString();

			var creature = client.GetCreatureSafe(packet.Id);

			// Check lock
			if (!creature.Can(Locks.Gesture))
			{
				Log.Debug("Gesture locked for '{0}'.", creature.Name);
				Send.UseGestureR(creature, false);
				return;
			}

			// Stop movement
			creature.StopMove();

			// Get motion data
			var motionData = AuraData.MotionDb.Find(gestureName);
			if (motionData == null)
			{
				Log.Warning("Creature '{0:X16}' tried to use missing gesture '{1}'.", creature.EntityId, gestureName);
				Send.UseGestureR(creature, false);
				return;
			}

			// Response
			Send.UseMotion(creature, motionData.Category, motionData.Type, motionData.Loop);
			Send.UseGestureR(creature, true);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Purpose unknown, sent when pressing escape and switching weapon sets.
		/// </remarks>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.UnkEsc)]
		public void UnkEsc(ChannelClient client, Packet packet)
		{
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <remarks>
		/// Purpose unknown, sent if character is "stuck" because of
		/// incompatibilities or missing responses, commonly happens
		/// after an update of creature info (5209).
		/// </remarks>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.IncompatibleUnk)]
		public void IncompatibleUnk(ChannelClient client, Packet packet)
		{
			Log.Warning("A client seems to be incompatible with the server, the latest version of Aura only supports the latest NA update. (Account id: {0})", client.Account.Id);
		}

		/// <summary>
		/// Send when trying to view someone's equipment.
		/// </summary>
		/// <example>
		/// 001 [0010000000017B99] Long   : 4503599627467673
		/// </example>
		[PacketHandler(Op.ViewEquipment)]
		public void ViewEquipment(ChannelClient client, Packet packet)
		{
			var targetEntityId = packet.GetLong();

			// Get creatures
			var creature = client.GetCreatureSafe(packet.Id);
			var target = creature.Region.GetCreature(targetEntityId);

			Send.ViewEquipmentR(creature, target);
		}

		/// <summary>
		/// Sent when using a skill without ammo, e.g. Ranged without arrows.
		/// </summary>
		/// <example>
		/// 001 [................] String : /arrow/
		/// </example>
		[PacketHandler(Op.AmmoRequired)]
		public void AmmoRequired(ChannelClient client, Packet packet)
		{
			// Officials don't do anything here... auto equip ammo? =D
		}

		/// <summary>
		/// Sent when a cutscene is finished or canceled?
		/// </summary>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.UnkCutsceneEnd)]
		public void UnkCutsceneEnd(ChannelClient client, Packet packet)
		{
			// Doesn't look like the server sends a response to this.
		}

		/// <summary>
		/// Sent when an inquiry is answered with OK.
		/// </summary>
		/// <example>
		/// 001 [..............02] Byte   : 2
		/// </example>
		[PacketHandler(Op.InquiryResponse)]
		public void InquiryResponse(ChannelClient client, Packet packet)
		{
			var id = packet.GetByte();

			var creature = client.GetCreatureSafe(packet.Id);

			// Try to call callback
			creature.HandleInquiry(id);

			Send.InquiryResponseR(creature, true);
		}

		/// <summary>
		/// Sent upon spinning the color wheel, used in name color change skill.
		/// </summary>
		/// <example>
		/// 001 [............0032] Short  : 50
		/// </example>
		[PacketHandler(Op.SpinColorWheel)]
		public void SpinColorWheel(ChannelClient client, Packet packet)
		{
			// [200100, NA229 (2016-06-16)] Removed
			//var strength = packet.GetShort();

			var creature = client.GetCreatureSafe(packet.Id);

			// Check active skill
			if (!creature.Skills.IsActive(SkillId.NameColorChange) && !creature.Skills.IsActive(SkillId.UseItemChattingColorChange))
			{
				Log.Warning("SpinColorWheel: Creature '{0:X16}' tried to spin color wheel without the necesseray skill being active.", creature.EntityId);
				return;
			}

			// Calculate random result
			var rnd = RandomProvider.Get();
			var slot = (int)rnd.Between(0, 31);
			if (slot == 1) // TODO: extra spin
				slot = 2;
			var radian = (float)(Math.PI / 16f * slot);

			creature.Temp.ColorWheelResult = slot + 1;

			// Response
			Send.SpinColorWheelR(creature, radian);
		}

		/// <summary>
		/// Sent when color wheel stops spinning.
		/// </summary>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.ChangeNameColor)]
		public void ChangeNameColor(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			// Check item id
			if (creature.Temp.NameColorItemEntityId == 0)
			{
				Log.Warning("ChangeNameColor: Creature '{0:X16}' sent packet while item id is 0.", creature.EntityId);
				return;
			}

			// Check item
			var item = creature.Inventory.GetItem(creature.Temp.NameColorItemEntityId);
			if (item == null)
			{
				Log.Warning("ChangeNameColor: Creature '{0:X16}' doesn't have the item.", creature.EntityId);
				return;
			}

			// Figure out what to change
			var bothChange = item.HasTag("/name_chatting_color_change/");
			var nameChange = bothChange || item.HasTag("/name_color_change/");
			var chatChange = bothChange || item.HasTag("/chatting_color_change/");

			if (!nameChange && !chatChange)
			{
				Log.Warning("ChangeNameColor: Creature '{0:X16}' tried to use invalid item.", creature.EntityId);
				return;
			}

			creature.Temp.NameColorItemEntityId = 0;

			// "Calculate" color
			// It's currently unknown how the client gets the colors, the index
			// sent to the client is not the slot the wheel lands on, and it's
			// not a color from the color table. Might be hardcoded.
			var idx = -1;
			switch (creature.Temp.ColorWheelResult)
			{
				case 1: idx = 28; break;
				case 2:
				case 3: idx = 14; break;
				case 4: idx = 13; break;
				case 5: idx = 11; break;
				case 6: idx = 1; break;
				case 7: idx = 6; break;
				case 8: idx = 26; break;
				case 9: idx = 5; break;
				case 10: idx = 25; break;
				case 11: idx = 1; break;
				case 12: idx = 11; break;
				case 13: idx = 4; break;
				case 14: idx = 3; break;
				case 15: idx = 11; break;
				case 16: idx = 18; break;
				case 17: idx = 22; break;
				case 18: idx = 10; break;
				case 19: idx = 24; break;
				case 20: idx = 27; break;
				case 21: idx = 12; break;
				case 22: idx = 23; break;
				case 23: idx = 19; break;
				case 24: idx = 3; break;
				case 25: idx = 16; break;
				case 26: idx = 8; break;
				case 27: idx = 30; break;
				case 28: idx = 2; break;
				case 29: idx = 20; break;
				case 30: idx = 21; break;
				case 31: idx = 15; break;
				case 32: idx = 9; break;
				default:
					Log.Warning("ChangeNameColor: Calculating color failed, result: {0}.", creature.Temp.ColorWheelResult);
					return;
			}

			// Remove item from inv
			creature.Inventory.Remove(item);

			// Expiration apparently varies based on the item,
			// no expiration time can be found in the db.
			var end = DateTime.Now.AddDays(item.Info.Id != 85563 ? 7 : 30);

			// Set conditions that modify the colors
			var extra = new MabiDictionary();
			extra.SetInt("IDX", idx);

			// Activate name color change
			if (nameChange)
			{
				creature.Conditions.Activate(ConditionsB.NameColorChange, extra);
				creature.Vars.Perm["NameColorIdx"] = idx;
				creature.Vars.Perm["NameColorEnd"] = end;
			}

			// Activate chat color change
			if (chatChange)
			{
				creature.Conditions.Activate(ConditionsB.ChatColorChange, extra);
				creature.Vars.Perm["ChatColorIdx"] = idx;
				creature.Vars.Perm["ChatColorEnd"] = end;
			}

			// Notice
			if (bothChange)
				Send.Notice(creature, NoticeType.Middle, Localization.Get("Your name and chat text colors have changed."));
			else if (nameChange)
				Send.Notice(creature, NoticeType.Middle, Localization.Get("Your name color has changed."));
			else if (chatChange)
				Send.Notice(creature, NoticeType.Middle, Localization.Get("Your chat text color has changed."));
		}

		/// <summary>
		/// Dummy handler.
		/// </summary>
		/// <remarks>
		/// Client and server exchange those packets from time to time.
		/// As they just contain bins, and were seemingly added when NGS
		/// was added to NA, it's probably a security mechanism.
		/// </remarks>
		[PacketHandler(Op.NGS1)]
		public void NGS1(ChannelClient client, Packet packet)
		{
			var length = packet.GetInt();
			var payload = packet.GetBin();

			// Ignore, client doesn't require an answer.
		}

		/// <summary>
		/// Sent when entering a region, to get the names of the NPCs in it.
		/// </summary>
		[PacketHandler(Op.RequestNpcNames)]
		public void RequestNpcNames(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);
			var npcs = creature.Region.GetNpcs(a => a.Has(CreatureStates.NamedNpc));

			Send.RequestNpcNamesR(creature, npcs);
		}

		/// <summary>
		/// Sent when clicking search in the NPC search window.
		/// </summary>
		/// <example>
		/// 001 [................] String : duncan
		/// </example>
		[PacketHandler(Op.SearchNpcName)]
		public void SearchNpcName(ChannelClient client, Packet packet)
		{
			var searchString = packet.GetString();

			var creature = client.GetCreatureSafe(packet.Id);

			// TODO: Log.
			Send.MsgBox(creature, Localization.Get("Not supported yet."));
		}

		/// <summary>
		/// Sent when opening dressing room. (Dummy handler)
		/// </summary>
		/// <remarks>
		/// Client sends DressingRoomClose when it receives a negative
		/// response to this packet.
		/// </remarks>
		/// <example>
		/// No parameter.
		/// </example>
		[PacketHandler(Op.DressingRoomOpen)]
		public void DressingRoomOpen(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			Send.DressingRoomOpenR(creature, false);
		}

		/// <summary>
		/// Sent when closing dressing room. (Dummy handler)
		/// </summary>
		/// <example>
		/// No parameter.
		/// </example>
		[PacketHandler(Op.DressingRoomClose)]
		public void DressingRoomClose(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			Send.DressingRoomCloseR(creature, false);
			Send.MsgBox(creature, Localization.Get("This feature hasn't been implemented yet."));
		}

		/// <summary>
		/// Sent on login, presumably to request the list of Erinn Land
		/// achievements. (Dummy handler)
		/// </summary>
		/// <remarks>
		/// The purpose is guessed, based on the ids in the response packet on
		/// NA matching the ids found in the Erinn Land achievement XML.
		/// </remarks>
		/// <example>
		/// No parameter.
		/// </example>
		[PacketHandler(Op.ErinnLandAchievementsRequest)]
		public void ErinnLandAchievementsRequest(ChannelClient client, Packet packet)
		{
			// ... ErinnLandAchievementsList
			// 001 [........00000000] Int    : 0
			// 002 [........00000000] Int    : 0
			// 003 [........000000CA] Int    : 202 // count
			//     004 [........00000001] Int    : 1
			//     005 [........00000001] Int    : 1 // count
			//         006 [..............00] Byte   : 0
		}

		/// <summary>
		/// ? (dummy)
		/// </summary>
		/// <remarks>
		/// Sent shortly after login, together with homestead info request.
		/// Potentially related to the Potential System.
		/// </remarks>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.UnkRequest)]
		public void UnkRequest(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			Send.UnkRequestR(creature);
		}

		/// <summary>
		/// ? (dummy)
		/// </summary>
		/// <remarks>
		/// Sent when opening the char window, potentially a request
		/// for information? Maybe Potential System related?
		/// </remarks>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.UnkCharWindow)]
		public void UnkCharWindow(ChannelClient client, Packet packet)
		{
		}
	}
}
