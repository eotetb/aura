// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Aura.Channel.Network.Sending;
using Aura.Channel.Scripting;
using Aura.Channel.World;
using Aura.Channel.World.Entities;
using Aura.Data.Database;
using Aura.Mabi.Const;
using Aura.Mabi.Structs;
using Aura.Shared.Network;
using Aura.Shared.Util;
using Aura.Channel.World.Inventory;
using Aura.Mabi.Network;
using Aura.Data;
using Aura.Channel.Skills.Magic;

namespace Aura.Channel.Network.Handlers
{
	public partial class ChannelServerHandlers : PacketHandlerManager<ChannelClient>
	{
		/// <summary>
		/// Sent when interacting with items, using the cursor.
		/// </summary>
		/// <example>
		/// ...
		/// </example>
		[PacketHandler(Op.ItemMove)]
		public void ItemMove(ChannelClient client, Packet packet)
		{
			var entityId = packet.GetLong();
			var untrustedSource = (Pocket)packet.GetInt(); // Discard this, NA does too // [200200, NA233 (2016-08-12)] Changed from byte to int
			var target = (Pocket)packet.GetInt(); // [200200, NA233 (2016-08-12)] Changed from byte to int
			var unk = packet.GetByte();
			var targetX = packet.GetByte();
			var targetY = packet.GetByte();

			// Get creature
			var creature = client.GetCreatureSafe(packet.Id);

			// Get item
			var item = creature.Inventory.GetItemSafe(entityId);
			var source = item.Info.Pocket;

			// Check lock
			if ((source.IsEquip() || target.IsEquip()) && !creature.Can(Locks.ChangeEquipment))
			{
				Log.Debug("ChangeEquipment locked for '{0}'.", creature.Name);
				goto L_Fail;
			}

			// Check ability to move equip.
			// (For example, RP characters usually can't.)
			if ((source.IsEquip() || target.IsEquip()) && !creature.CanMoveEquip)
			{
				goto L_Fail;
			}

			// Check touchability
			if (target.IsEquip())
			{
				string error;
				if (!item.CanBeTouchedBy(creature, out error))
				{
					Send.MsgBox(creature, error);
					goto L_Fail;
				}
			}

			// Check bag
			if (item.IsBag && target.IsBag() && !ChannelServer.Instance.Conf.World.Bagception)
			{
				Send.ServerMessage(creature, Localization.Get("Item bags can't be stored inside other bags."));
				goto L_Fail;
			}

			// Check TwinSword feature
			if (target == creature.Inventory.LeftHandPocket && !item.IsShieldLike && creature.RightHand != null && !AuraData.FeaturesDb.IsEnabled("TwinSword"))
			{
				// TODO: Is this message sufficient? Do we need a better one?
				//   Do we need one at all? Or would that confuse people even more?
				Send.Notice(creature, NoticeType.MiddleSystem, Localization.Get("The Dual Wielding feature hasn't been enabled yet."));
				goto L_Fail;
			}

			// Check premium
			// Technically it would be cheating to drop or destroy items
			// that are inside bags, with expired service, but we'll ignore
			// those checks for now, to reduce redundancy. You are supposed
			// to be able to get to your items, who cares if they edit
			// packets to make them drop?
			if (!client.Account.PremiumServices.CanUseBags)
			{
				var freeBag = true;

				// Check source
				if (source.IsBag())
				{
					var bagItem = creature.Inventory.GetItem(a => a.OptionInfo.LinkedPocketId == source);
					freeBag = (bagItem != null && bagItem.HasTag("/free_bag/"));
				}

				// Check target
				if (target.IsBag() && freeBag)
				{
					var bagItem = creature.Inventory.GetItem(a => a.OptionInfo.LinkedPocketId == target);
					freeBag = (bagItem != null && bagItem.HasTag("/free_bag/"));
				}

				// Stop if not a free bag
				if (!freeBag)
				{
					// Unofficial, what does the client do when you have a
					// bag open and then try to use it, after the service
					// expired?
					Send.MsgBox(creature, Localization.Get("Inventory Plus is required to use bags.\nPlease use the 'Unequip the Bag' command to retrieve the items in the bag."));
					goto L_Fail;
				}
			}

			// Check trade
			if ((target == Pocket.Trade || source == Pocket.Trade) && creature.Temp.ActiveTrade == null)
			{
				Log.Warning("ItemMove: User '{0}' tried to move something to or from the trade window without being in a trade.", client.Account.Id);
				goto L_Fail;
			}

			// Stop moving when changing weapons
			if ((target >= Pocket.RightHand1 && target <= Pocket.Magazine2) || (source >= Pocket.RightHand1 && source <= Pocket.Magazine2))
				creature.StopMove();

			// Try to move item
			if (!creature.Inventory.Move(item, target, targetX, targetY))
			{
				Log.Debug("ItemMove: Moving item from '{0}' to '{1}' failed.", source, target);
				goto L_Fail;
			}

			Send.ItemMoveR(creature, true);

			return;

		L_Fail:
			Send.ItemMoveR(creature, false);
		}

		/// <summary>
		/// Sent when dropping an item.
		/// </summary>
		/// <example>
		/// ...
		/// </example>
		[PacketHandler(Op.ItemDrop)]
		public void ItemDrop(ChannelClient client, Packet packet)
		{
			var entityId = packet.GetLong();
			var unkByte = packet.GetByte();
			var x = packet.GetInt(); // [200200, NA233 (2016-08-12)]
			var y = packet.GetInt(); // [200200, NA233 (2016-08-12)]

			var creature = client.GetCreatureSafe(packet.Id);
			if (creature.Region == Region.Limbo)
				return;

			// Check lock
			if (!creature.Can(Locks.PickUpAndDrop))
			{
				Log.Debug("PickUpAndDrop locked for '{0}'.", creature.Name);
				Send.ItemDropR(creature, false, 0);
				return;
			}

			// Check item
			var item = creature.Inventory.GetItem(entityId);
			if (item == null)
			{
				Send.ItemDropR(creature, false, 0);
				return;
			}

			// Check ability to move equip.
			// (For example, RP characters usually can't.)
			if (item.Info.Pocket.IsEquip() && !creature.CanMoveEquip)
			{
				Send.ItemDropR(creature, false, 0);
				return;
			}

			// Check pocket, players only have limited access.
			if (!CreatureInventory.AccessiblePockets.Contains(item.Info.Pocket))
			{
				Log.Warning("ItemDrop: Player '{0}' ({1:X16}) tried to drop from inaccessible pocket.", creature.Name, creature.EntityId);
				Send.ItemDropR(creature, false, 0);
				return;
			}

			// Check for filled bags
			if (item.IsBag && item.OptionInfo.LinkedPocketId != Pocket.None && creature.Inventory.CountItemsInPocket(item.OptionInfo.LinkedPocketId) > 0)
			{
				Log.Warning("ItemDrop: Player '{0}' ({1:X16}) tried to drop filled item bag.", creature.Name, creature.EntityId);
				Send.ItemDropR(creature, false, 0);
				return;
			}

			// Check quest items for progress, you can't drop quests that
			// have been "started". If you can get rid of the progress,
			// e.g. by dropping all items that were to be collected,
			// you can drop the quest.
			if (item.Quest != null && item.Quest.HasProgress)
			{
				Send.ItemDropR(creature, Localization.Get("You cannot drop a quest that has already started."));
				return;
			}

			// Check droppability
			if (item.HasTag("/not_dropable/") || item.Data.Action == ItemAction.ImportantItem || item.Data.Action == ItemAction.Important2Item)
			{
				Send.ItemDropR(creature, Localization.Get("You cannot drop this item."));
				return;
			}

			// Try to remove item
			if (!creature.Inventory.Remove(item))
			{
				Send.ItemDropR(creature, false, 0);
				return;
			}

			// Drop item if it wasn't used to access a dungeon
			if (!ChannelServer.Instance.World.DungeonManager.CheckDrop(creature, item))
				item.Drop(creature.Region, creature.GetPosition(), Item.DropRadius, creature, true);

			Send.ItemDropR(creature, true, item.EntityId);
		}

		/// <summary>
		/// Sent when clicking an item on the ground, to pick it up.
		/// </summary>
		/// <example>
		/// ...
		/// </example>
		[PacketHandler(Op.ItemPickUp)]
		public void ItemPickUp(ChannelClient client, Packet packet)
		{
			var entityId = packet.GetLong();
			var unkByte = packet.GetByte(); // [200200, NA233 (2016-08-12)]

			var creature = client.GetCreatureSafe(packet.Id);
			if (creature.Region == Region.Limbo)
				return;

			// Check lock
			if (!creature.Can(Locks.PickUpAndDrop))
			{
				Log.Debug("PickUpAndDrop locked for '{0}'.", creature.Name);
				Send.ItemPickUpR(creature, ItemPickUpResult.Fail, entityId);
				return;
			}

			// Get item from region
			var item = creature.Region.GetItem(entityId);
			if (item == null)
			{
				Send.ItemPickUpR(creature, ItemPickUpResult.NotFound, entityId);
				return;
			}

			// Check distance
			// The client usually tries to get to the item first, but if
			// there's an obstacle, it tries to send ItemPickUp early.
			// We have to tell it when it's not in range yet.
			if (!creature.GetPosition().InRange(item.GetPosition(), 200))
			{
				Send.ItemPickUpR(creature, ItemPickUpResult.OutOfRange, entityId);
				return;
			}

			// Check protection
			if (!creature.CanPickUp(item))
			{
				if (creature.IsDev)
				{
					Send.Notice(creature, Localization.Get("You stole an innocent player's loot, feeling like a big, strong devCAT now? Shame on you."));
				}
				else
				{
					Send.ItemPickUpR(creature, ItemPickUpResult.NotYours, entityId);
					return;
				}
			}

			// Check touchability
			string error;
			if (!item.CanBeTouchedBy(creature, out error))
			{
				Send.MsgBox(creature, error);
				Send.ItemPickUpR(creature, ItemPickUpResult.FailNoMessage, entityId);
				return;
			}

			// Try to pick up item
			if (!creature.Inventory.PickUp(item))
			{
				Send.ItemPickUpR(creature, ItemPickUpResult.NoSpace, entityId);
				return;
			}

			// Pick up effect for keys
			if (item.HasTag("/key/"))
				Send.Effect(creature, Effect.PickUpItem, (byte)1, item.Info.Id, item.Info.Color1, item.Info.Color2, item.Info.Color3);

			Send.ItemPickUpR(creature, ItemPickUpResult.Success, entityId);
		}

		/// <summary>
		/// Sent when destroying an item (right click option).
		/// </summary>
		/// <example>
		/// 001 [005000CBB3152EEC] Long   : 22518873019723500
		/// </example>
		[PacketHandler(Op.ItemDestroy)]
		public void ItemDestroy(ChannelClient client, Packet packet)
		{
			var itemEntityId = packet.GetLong();

			var creature = client.GetCreatureSafe(packet.Id);
			var item = creature.Inventory.GetItemSafe(itemEntityId);

			// Check if item is destroyable
			if (!item.IsDestroyable)
			{
				Log.Warning("ItemDestroy: Creature '{0:X16}' tried to destroy a non-destroyable item.", creature.EntityId);
				Send.ItemDestroyR(creature, false);
				return;
			}

			// Try to remove item
			if (!creature.Inventory.Remove(item))
			{
				Send.ItemDestroyR(creature, false);
				return;
			}

			Send.ItemDestroyR(creature, true);
		}

		/// <summary>
		/// Sent when splitting stacks
		/// </summary>
		/// <example>
		/// ...
		/// </example>
		[PacketHandler(Op.ItemSplit)]
		public void ItemSplit(ChannelClient client, Packet packet)
		{
			var itemId = packet.GetLong();
			var amount = packet.GetUShort();
			var unkPocket = packet.GetInt(); // [200200, NA233 (2016-08-12)] Apparently a pocket, probably of the item?

			var creature = client.GetCreatureSafe(packet.Id);

			// Check item
			var item = creature.Inventory.GetItemSafe(itemId);
			if (item.Data.StackType == StackType.None)
			{
				Send.ItemSplitR(creature, false);
				return;
			}

			// Check requested amount
			if (item.Info.Amount < amount)
				amount = item.Info.Amount;

			// Clone item or create new one based on stack item
			var splitItem = item.Data.StackItemId == 0 ? new Item(item) : new Item(item.Data.StackItemId);
			splitItem.Info.Amount = amount;

			// New item on cursor
			if (!creature.Inventory.Add(splitItem, Pocket.Cursor))
			{
				Send.ItemSplitR(creature, false);
				return;
			}

			// Update amount or remove
			item.Info.Amount -= amount;

			if (item.Info.Amount > 0 || item.Data.StackItemId != 0)
			{
				Send.ItemAmount(creature, item);
			}
			else
			{
				creature.Inventory.Remove(item);
			}

			Send.ItemSplitR(creature, true);
		}

		/// <summary>
		/// Sent when switching weapon sets (eg, on Tab/W).
		/// </summary>
		/// <example>
		/// ...
		/// </example>
		[PacketHandler(Op.SwitchSet)]
		public void SwitchSet(ChannelClient client, Packet packet)
		{
			var set = (WeaponSet)packet.GetByte();

			var creature = client.GetCreatureSafe(packet.Id);

			// Check set
			if (!Enum.IsDefined(typeof(WeaponSet), set))
			{
				Log.Warning("Creature '{0:X16}' tried to switch to unknown weapon set '{1}'.", creature.EntityId, set);
				Send.SwitchSetR(creature, false);
				return;
			}

			// Check if creature can change their equip
			if (!creature.Can(Locks.ChangeEquipment))
			{
				Log.Debug("ChangeEquipment locked for '{0}'.", creature.Name);
				Send.SwitchSetR(creature, false);
				return;
			}

			creature.StopMove();
			creature.Inventory.ChangeWeaponSet(set);

			Send.SwitchSetR(creature, true);
		}

		/// <summary>
		/// Sent when changing an item state, eg hood on robes.
		/// </summary>
		/// <remarks>
		/// The client isn't able to handle multiple state changable items properly,
		/// like an armor and a robe. The armor will always take priority,
		/// resulting in only the helmet changing states, if anything,
		/// when a robe is hiding the armor. Is this official? Should we fix it?
		/// </remarks>
		/// <example>
		/// ...
		/// </example>
		[PacketHandler(Op.ItemStateChange)]
		public void ItemStateChange(ChannelClient client, Packet packet)
		{
			var firstTarget = (Pocket)packet.GetInt();  // [200200, NA233 (2016-08-12)] Changed from byte to int
			var secondTarget = (Pocket)packet.GetInt(); // [200200, NA233 (2016-08-12)] Changed from byte to int
			var unkPocket = packet.GetInt(); // [200200, NA233 (2016-08-12)] Changed from byte to int (apparently it's a pocket as well?)

			var creature = client.GetCreatureSafe(packet.Id);

			// This might not be entirely correct, but works well.
			// Robe is opened first, Helm secondly, then Robe and Helm are both closed.

			foreach (var target in new[] { firstTarget, secondTarget })
			{
				// Don't change pocket None.
				if (target == 0)
					continue;

				// Check if pocket is valid
				if (target != Pocket.Head && target != Pocket.Robe && target != Pocket.Armor && target != Pocket.HeadStyle && target != Pocket.RobeStyle && target != Pocket.ArmorStyle)
				{
					Log.Warning("ItemStateChange: Creature '{0:X16}' tried to change state of invalid pocket's item ({1}).", creature.EntityId, target);
					continue;
				}

				var item = creature.Inventory.GetItemAt(target, 0, 0);
				if (item != null)
				{
					item.Info.State = (byte)(item.Info.State == 1 ? 0 : 1);
					Send.EquipmentChanged(creature, item);
				}
			}

			Send.ItemStateChangeR(creature);
		}

		/// <summary>
		/// Sent when using an item.
		/// </summary>
		/// <remarks>
		/// Not all usable items send this packet. The client has some
		/// items send different packets, like starting hidden skills
		/// (eg res, hw, dye, etc).
		/// </remarks>
		/// <example>
		/// 0001 [005000CBB535EFC6] Long   : 22518873055424454
		/// </example>
		[PacketHandler(Op.UseItem)]
		public void UseItem(ChannelClient client, Packet packet)
		{
			var entityId = packet.GetLong();
			string parameter = "";
			if (packet.Peek() == PacketElementType.String)
				parameter = packet.GetString();

			var creature = client.GetCreatureSafe(packet.Id);

			// Check states
			if (creature.IsDead)
			{
				Log.Warning("Player '{0}' tried to use item while being dead.", creature.Name);
				goto L_Fail;
			}

			// Check lock
			if (!creature.Can(Locks.UseItem))
			{
				Log.Debug("UseItem locked for '{0}'.", creature.Name);
				goto L_Fail;
			}

			// Get item
			var item = creature.Inventory.GetItem(entityId);
			if (item == null)
				goto L_Fail;

			// Meta Data Scripts
			var gotMetaScript = false;
			{
				// Sealed books
				if (item.MetaData1.Has("MGCWRD") && item.MetaData1.Has("MGCSEL"))
				{
					var magicWords = item.MetaData1.GetString("MGCWRD");
					try
					{
						var sms = new MagicWordsScript(magicWords);
						sms.Run(creature, item);

						gotMetaScript = true;
					}
					catch (Exception ex)
					{
						Log.Exception(ex, "Problem while running magic words script '{0}'", magicWords);
						Send.ServerMessage(creature, Localization.Get("Unimplemented item."));
						goto L_Fail;
					}
				}
			}

			// Aura Scripts
			if (!gotMetaScript)
			{
				// Get script
				var script = ChannelServer.Instance.ScriptManager.ItemScripts.Get(item.Info.Id);
				if (script == null)
				{
					Log.Unimplemented("Item script for '{0}' not found.", item.Info.Id);
					Send.ServerMessage(creature, Localization.Get("This item isn't implemented yet."));
					goto L_Fail;
				}

				// Run script
				try
				{
					script.OnUse(creature, item, parameter);
				}
				catch (NotImplementedException)
				{
					Log.Unimplemented("UseItem: Item OnUse method for '{0}'.", item.Info.Id);
					Send.ServerMessage(creature, Localization.Get("This item isn't implemented completely yet."));
					goto L_Fail;
				}
			}

			ChannelServer.Instance.Events.OnPlayerUsesItem(creature, item);

			// Decrease item count
			if (item.Data.Consumed)
			{
				creature.Inventory.Decrement(item);

				// Replace consumed items with something else,
				// e.g milk bottles with empty bottles.
				if (item.Data.ReplaceItemId != 0)
					creature.Inventory.Add(new Item(item.Data.ReplaceItemId), true);
			}

			// Break seal after use
			if (item.MetaData1.Has("MGCSEL"))
			{
				item.MetaData1.Remove("MGCWRD");
				item.MetaData1.Remove("MGCSEL");
				Send.ItemUpdate(creature, item);
			}

			// Mandatory stat update
			Send.StatUpdate(creature, StatUpdateType.Private,
				Stat.Life, Stat.LifeInjured, Stat.Mana, Stat.Stamina, Stat.Hunger,
				Stat.ToxicStr, Stat.ToxicInt, Stat.ToxicDex, Stat.ToxicWill, Stat.ToxicLuck
			);
			Send.StatUpdate(creature, StatUpdateType.Public, Stat.Life, Stat.LifeInjured);

			Send.UseItemR(creature, true, item.Info.Id);
			return;

		L_Fail:
			Send.UseItemR(creature, false, 0);
		}

		/// <summary>
		/// Sent after regular dye was prepared.
		/// </summary>
		/// <remarks>
		/// What's sent back are the parameters for the wave algorithm,
		/// creating the random pattern.
		/// </remarks>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.DyePaletteReq)]
		public void DyePaletteReq(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			var rnd = RandomProvider.Get();

			var a1 = creature.Temp.DyeDistortA1 = rnd.Next(256);
			var a2 = creature.Temp.DyeDistortA2 = rnd.Next(256);
			var a3 = creature.Temp.DyeDistortA3 = rnd.Next(256);
			var a4 = creature.Temp.DyeDistortA4 = rnd.Next(256);

			Send.DyePaletteReqR(creature, a1, a2, a3, a4);
		}

		/// <summary>
		/// Sent when clicking "Pick Color".
		/// </summary>
		/// <remarks>
		/// Generates the randomly placed pickers' positions for regular dyes.
		/// They are placed relative to the cursor, if the whole struct
		/// is 0 all pickers will be at the cursor, giving all 5 options
		/// the same color.
		/// </remarks>
		/// <example>
		/// 0001 [005000CB994586F1] Long   : 22518872586684145
		/// </example>
		[PacketHandler(Op.DyePickColor)]
		public void DyePickColor(ChannelClient client, Packet packet)
		{
			var itemEntityId = packet.GetLong();

			var creature = client.GetCreatureSafe(packet.Id);

			var rnd = RandomProvider.Get();

			var pickers = new DyePickers();
			if (ChannelServer.Instance.Conf.World.DyeDifficulty >= 2)
			{
				pickers.Picker2.X = (short)-rnd.Next(10, 16);
				pickers.Picker2.Y = (short)-rnd.Next(10, 16);
			}
			if (ChannelServer.Instance.Conf.World.DyeDifficulty >= 3)
			{
				pickers.Picker3.X = (short)+rnd.Next(10, 16);
				pickers.Picker3.Y = (short)-rnd.Next(10, 16);
			}
			if (ChannelServer.Instance.Conf.World.DyeDifficulty >= 4)
			{
				pickers.Picker4.X = (short)-rnd.Next(10, 16);
				pickers.Picker4.Y = (short)+rnd.Next(10, 16);
			}
			if (ChannelServer.Instance.Conf.World.DyeDifficulty >= 5)
			{
				pickers.Picker5.X = (short)+rnd.Next(10, 16);
				pickers.Picker5.Y = (short)+rnd.Next(10, 16);
			}

			creature.Temp.RegularDyePickers = pickers;

			Send.DyePickColorR(creature, true);
		}

		/// <summary>
		/// Sent when "gifting" an item.
		/// </summary>
		/// <example>
		/// 0001 [0010F000000005E7] Long   : 4767482418038247
		/// 0002 [005000CC7FFA923C] Long   : 22518876457308732
		/// </example>
		[PacketHandler(Op.GiftItem)]
		public void GiftItem(ChannelClient client, Packet packet)
		{
			var npcId = packet.GetLong();
			var itemId = packet.GetLong();

			var creature = client.Controlling;
			var target = client.Controlling.Region.GetCreatureSafe(npcId);

			// Check item
			var item = creature.Inventory.GetItem(itemId);
			if (item == null)
			{
				Send.GiftItemR(creature, false);
				return;
			}

			// Temp check for pets, giving food to them uses the same packet
			// as gifting to NPCs.
			if (target is Pet)
			{
				Send.SystemMessage(creature, Localization.Get("Unimplemented."));
				Send.GiftItemR(creature, false);
				return;
			}

			var npc = target as NPC;

			// TODO: If !Item is giftable..
			// TODO: If !Npc in range...

			if (npc.ScriptType == null)
				return;

			Send.NpcTalkStartR(creature, target.EntityId);

			client.NpcSession.StartGift(npc, creature, item);

			creature.Inventory.Remove(item);

			Send.GiftItemR(creature, true);
		}

		/// <summary>
		/// Sent when unequipping a filled bag.
		/// </summary>
		/// <example>
		/// 001 [0050000000000066] Long   : 22517998136852582
		/// </example>
		[PacketHandler(Op.UnequipBag)]
		public void UnequipBag(ChannelClient client, Packet packet)
		{
			var entityId = packet.GetLong();

			var creature = client.GetCreatureSafe(packet.Id);

			// Check bag
			var bag = creature.Inventory.GetItemSafe(entityId);
			if (!bag.IsBag || bag.OptionInfo.LinkedPocketId == Pocket.None)
			{
				Log.Warning("Player '{0}' ({1:X16}) tried to unequip invalid bag.", creature.Name, creature.EntityId);
				Send.UnequipBagR(creature, false);
				return;
			}

			// Remove items
			var items = creature.Inventory.GetAllItemsFrom(bag.OptionInfo.LinkedPocketId);
			foreach (var item in items)
				creature.Inventory.Remove(item);

			// Add items, temporarily remove bag pocket,
			// so items aren't readded in there
			creature.Inventory.Remove(bag.OptionInfo.LinkedPocketId);
			foreach (var item in items)
				creature.Inventory.Add(item, true);
			creature.Inventory.AddBagPocket(bag);

			// Success
			Send.UnequipBagR(creature, true);
		}

		/// <summary>
		/// Request to combine similar items in stacks,
		/// sent upon clicking button in inv.
		/// </summary>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.ItemMagnet)]
		public void ItemMagnet(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			Send.MsgBox(creature, Localization.Get("Not supported yet."));
		}

		/// <summary>
		/// Notification that player saw a new item in the inv,
		/// sent when hovering an item that's highlighted as new.
		/// </summary>
		/// <example>
		/// 001 [0050000000000066] Long   : 22517998136852582
		/// </example>
		[PacketHandler(Op.SawItemNotification)]
		public void SawItemNotification(ChannelClient client, Packet packet)
		{
			var itemEntityId = packet.GetLong();

			var creature = client.GetCreatureSafe(packet.Id);
			var item = creature.Inventory.GetItemSafe(itemEntityId);

			item.IsNew = false;
		}

		/// <summary>
		/// Sent when trying to burn an item in a campfire.
		/// </summary>
		/// <example>
		/// 001 [00A1000E000A000E] Long   : 45317531380613134
		/// 002 [00500000000003B9] Long   : 22517998136853433
		/// 003 [..............00] Byte   : 0
		/// </example>
		[PacketHandler(Op.BurnItem)]
		public void BurnItem(ChannelClient client, Packet packet)
		{
			var propEntityId = packet.GetLong();
			var itemEntityId = packet.GetLong();
			var enchantersBurn = packet.GetBool();

			// Get creature and item
			var creature = client.GetCreatureSafe(packet.Id);
			var item = creature.Inventory.GetItemSafe(itemEntityId);

			// Check if prop is still there (campfires may vanish),
			// fail if it's gone or creature is not in range.
			var prop = creature.Region.GetProp(propEntityId);
			if (prop == null || !creature.GetPosition().InRange(prop.GetPosition(), 1000))
			{
				Send.BurnItemR(creature, false);
				return;
			}

			// Get skill handler
			var skillHandler = ChannelServer.Instance.SkillManager.GetHandler<Enchant>(SkillId.Enchant);
			if (skillHandler == null)
			{
				Log.Error("BurnItem: Enchant handler missing.");
				Send.BurnItemR(creature, false);
				return;
			}

			// Burn
			var success = skillHandler.Burn(creature, item, prop, enchantersBurn);
			Send.BurnItemR(creature, success);
		}

		/// <summary>
		/// Sent after selecting a destination in the Moonlight Traveler Book.
		/// </summary>
		/// <example>
		/// 001 [0050F000000005DA] Long   : 22781880927520218
		/// 002 [........00000006] Int    : 6
		/// </example>
		[PacketHandler(Op.BeginnerWarpBook)]
		public void BeginnerWarpBook(ChannelClient client, Packet packet)
		{
			var itemEntityid = packet.GetLong();
			var destination = packet.GetInt();

			var creature = client.GetCreatureSafe(packet.Id);

			// Check creature
			if (creature.TotalLevel >= 1000)
			{
				// Unofficial
				Send.Notice(creature, Localization.Get("You can only use this item with a cumulative level of less than 1000."));
				return;
			}

			// Check item
			var item = creature.Inventory.GetItem(itemEntityid);
			if (item == null || !item.HasTag("/beginnerwarpbook/"))
			{
				Log.Warning("BeginnerWarpBook: Creature '{0:X16}' doesn't have valid item.", itemEntityid);
				return;
			}

			// Get coordinates
			int regionId, x, y;
			switch (destination)
			{
				case 0: regionId = 1; x = 13579; y = 22326; break;
				#region Coordinates
				case 1: regionId = 1; x = 32308; y = 27698; break;
				case 2: regionId = 1; x = 3154; y = 52811; break;
				case 3: regionId = 16; x = 20064; y = 67827; break;
				case 4: regionId = 16; x = 31520; y = 33204; break;
				case 5: regionId = 48; x = 7951; y = 30593; break;
				case 6: regionId = 14; x = 26311; y = 38204; break;
				case 7: regionId = 30; x = 8348; y = 81487; break;
				case 8: regionId = 30; x = 49128; y = 60582; break;
				case 9: regionId = 30; x = 64788; y = 33623; break;
				case 10: regionId = 30; x = 43145; y = 19012; break;
				case 11: regionId = 96; x = 15905; y = 25145; break;
				case 12: regionId = 100; x = 49991; y = 43443; break;
				case 13: regionId = 53; x = 120280; y = 104580; break;
				case 14: regionId = 53; x = 73212; y = 115903; break;
				case 15: regionId = 302; x = 97035; y = 91284; break;
				case 16: regionId = 302; x = 125883; y = 80385; break;
				case 17: regionId = 301; x = 84766; y = 101012; break;
				case 18: regionId = 301; x = 87253; y = 81792; break;
				case 19: regionId = 52; x = 50853; y = 34452; break;
				case 20: regionId = 52; x = 21679; y = 32126; break;
				case 21: regionId = 52; x = 52491; y = 64447; break;
				case 22: regionId = 52; x = 20019; y = 67454; break;
				case 23: regionId = 300; x = 193867; y = 205950; break;
				case 24: regionId = 300; x = 238871; y = 196052; break;
				case 25: regionId = 300; x = 232070; y = 136426; break;
				case 26: regionId = 300; x = 195239; y = 173012; break;
				case 27: regionId = 300; x = 189570; y = 219060; break;
				case 28: regionId = 300; x = 146395; y = 187801; break;
				case 29: regionId = 300; x = 257649; y = 239436; break;
				case 30: regionId = 401; x = 59694; y = 124234; break;
				case 31: regionId = 401; x = 111770; y = 62730; break;
				case 32: regionId = 401; x = 68248; y = 103550; break;
				case 33: regionId = 437; x = 116960; y = 112065; break;
				case 34: regionId = 402; x = 85528; y = 14223; break;
				case 35: regionId = 402; x = 37692; y = 17899; break;
				case 36: regionId = 402; x = 33541; y = 39287; break;
				case 37: regionId = 56; x = 8460; y = 8895; break;
				case 38: regionId = 23; x = 37115; y = 38003; break;
				case 39: regionId = 4005; x = 33807; y = 17349; break;
				case 40: regionId = 4005; x = 40321; y = 45514; break;
				case 41: regionId = 4014; x = 38890; y = 41112; break;
				case 42: regionId = 4014; x = 42737; y = 35072; break;
				case 43: regionId = 4014; x = 74680; y = 46398; break;
				case 44: regionId = 4014; x = 44910; y = 63821; break;
				case 45: regionId = 4014; x = 69500; y = 72698; break;
				case 46: regionId = 3001; x = 159908; y = 171678; break;
				case 47: regionId = 3001; x = 192021; y = 291671; break;
				case 48: regionId = 3001; x = 292245; y = 326127; break;
				case 49: regionId = 3001; x = 321445; y = 286146; break;
				case 50: regionId = 3001; x = 96264; y = 312501; break;
				case 51: regionId = 3001; x = 159925; y = 347087; break;
				case 52: regionId = 3001; x = 235975; y = 207542; break;
				case 53: regionId = 3001; x = 287568; y = 273578; break;
				case 54: regionId = 3001; x = 327176; y = 247806; break;
				case 55: regionId = 3001; x = 267927; y = 139778; break;
				case 56: regionId = 3001; x = 326549; y = 172878; break;
				case 57: regionId = 3001; x = 402148; y = 264873; break;
				case 58: regionId = 3001; x = 403249; y = 246044; break;
				case 59: regionId = 3001; x = 430304; y = 163469; break;
				case 60: regionId = 3001; x = 407742; y = 125883; break;
				case 61: regionId = 3001; x = 348010; y = 139290; break;
				case 62: regionId = 3001; x = 304640; y = 121520; break;
				case 63: regionId = 3100; x = 137070; y = 229339; break;
				case 64: regionId = 3100; x = 177534; y = 203653; break;
				case 65: regionId = 3100; x = 146584; y = 325163; break;
				case 66: regionId = 3100; x = 190747; y = 289647; break;
				case 67: regionId = 3100; x = 194297; y = 361121; break;
				case 68: regionId = 3100; x = 268954; y = 370296; break;
				case 69: regionId = 3100; x = 300050; y = 340960; break;
				case 70: regionId = 3100; x = 269342; y = 258545; break;
				case 71: regionId = 3100; x = 321100; y = 280880; break;
				case 72: regionId = 3100; x = 343964; y = 345509; break;
				case 73: regionId = 3100; x = 390133; y = 345752; break;
				case 74: regionId = 3100; x = 456013; y = 379761; break;
				case 75: regionId = 3100; x = 391682; y = 451110; break;
				case 76: regionId = 3100; x = 352160; y = 418340; break;
				case 77: regionId = 3100; x = 317103; y = 466760; break;
				case 78: regionId = 3100; x = 275091; y = 467067; break;
				case 79: regionId = 3300; x = 57387; y = 226499; break;
				case 80: regionId = 3300; x = 133940; y = 265772; break;
				case 81: regionId = 3300; x = 94810; y = 235480; break;
				case 82: regionId = 3300; x = 155230; y = 205010; break;
				case 83: regionId = 3300; x = 143845; y = 163780; break;
				case 84: regionId = 3300; x = 201988; y = 195173; break;
				case 85: regionId = 3300; x = 254660; y = 162990; break;
				case 86: regionId = 3300; x = 254639; y = 197404; break;
				case 87: regionId = 3300; x = 255150; y = 224263; break;
				case 88: regionId = 3300; x = 370645; y = 223131; break;
				case 89: regionId = 3300; x = 440364; y = 179199; break;
				case 90: regionId = 3300; x = 315098; y = 170835; break;
				case 91: regionId = 3300; x = 296328; y = 116863; break;
				case 92: regionId = 3300; x = 228056; y = 76053; break;
				case 93: regionId = 3300; x = 183808; y = 57453; break;
				case 94: regionId = 3300; x = 153406; y = 100848; break;
				case 95: regionId = 3300; x = 168020; y = 134682; break;
				case 96: regionId = 3300; x = 126075; y = 137745; break;
				case 97: regionId = 3300; x = 44581; y = 180517; break;
				case 98: regionId = 3300; x = 92660; y = 164640; break;
				case 99: regionId = 3300; x = 60246; y = 138141; break;
				case 100: regionId = 3300; x = 67666; y = 130851; break;
				case 101: regionId = 3200; x = 188791; y = 158998; break;
				case 102: regionId = 3200; x = 167080; y = 171707; break;
				case 103: regionId = 3200; x = 150451; y = 209240; break;
				case 104: regionId = 3200; x = 195029; y = 244641; break;
				case 105: regionId = 3200; x = 288409; y = 159394; break;
				case 106: regionId = 3200; x = 275370; y = 125710; break;
				case 107: regionId = 3200; x = 263626; y = 217510; break;
				case 108: regionId = 3200; x = 253193; y = 241151; break;
				case 109: regionId = 3200; x = 342492; y = 251671; break;
				case 110: regionId = 3200; x = 364389; y = 225304; break;
				case 111: regionId = 3200; x = 398282; y = 272748; break;
				case 112: regionId = 3200; x = 427950; y = 244159; break;
				case 113: regionId = 3200; x = 377349; y = 180449; break;
				case 114: regionId = 3200; x = 337995; y = 146241; break;
				case 115: regionId = 3200; x = 383389; y = 140146; break;
				case 116: regionId = 3400; x = 321772; y = 212578; break;
				case 117: regionId = 3400; x = 326077; y = 179143; break;
				case 118: regionId = 3400; x = 303722; y = 169110; break;
				case 119: regionId = 3400; x = 279197; y = 156698; break;
				case 120: regionId = 3400; x = 256968; y = 158885; break;
				case 121: regionId = 3400; x = 205320; y = 181180; break;
				case 122: regionId = 3400; x = 211517; y = 148776; break;
				case 123: regionId = 3400; x = 181041; y = 263383; break;
				case 124: regionId = 3400; x = 168242; y = 214715; break;
				case 125: regionId = 3400; x = 187663; y = 238669; break;
				case 126: regionId = 3400; x = 177402; y = 258100; break;
				case 127: regionId = 3400; x = 220876; y = 241651; break;
				#endregion
				case 128: regionId = 3400; x = 239649; y = 264187; break;

				default:
					Send.ServerMessage(creature, Localization.Get("Unknown destination."));
					Log.Warning("BeginnerWarpBook: Unknown destination '{0}'.", destination);
					return;
			}

			// Warp
			creature.Warp(regionId, x, y);
		}

		/// <summary>
		/// Sent to open the cash item shop.
		/// </summary>
		/// <remarks>
		/// No parameters.
		/// </remarks>
		[PacketHandler(Op.OpenItemShop)]
		public void OpenItemShop(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			if (!AuraData.FeaturesDb.IsEnabled("ItemShop"))
			{
				Send.ServerMessage(creature, Localization.Get("The item shop isn't available yet."));
				Send.OpenItemShopR(creature, false, null);
				return;
			}

			// The item shop URL has one parameter, "key", that is set to
			// the value we send here. The web page has to use this value
			// to identify the user. To provide some security, we send the
			// session key, which should only be known by this client.
			var parameter = client.Account.SessionKey.ToString();

			Send.OpenItemShopR(creature, true, parameter);
		}

		/// <summary>
		/// Requests list of expired items to destroy, sent when clicking
		/// the respective button in the inventory window.
		/// </summary>
		/// <remarks>
		/// Response doesn't seem to be required for fail, but it is sent
		/// on officials.
		/// </remarks>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.DestroyExpiredItems)]
		public void DestroyExpiredItems(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);

			Send.MsgBox(creature, Localization.Get("Not supported yet."));
			Send.DestroyExpiredItemsR(creature, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <example>
		/// Op: 000096D5 ?, Id: 0010000000000002
		/// 001 [........00000001] Int    : 1
		/// 002 [........FFFFFFFF] Int    : -1
		/// 003 [........000165E1] Int    : 91617
		/// 004 [................] String : Style Tab (30 days)
		/// 005 [0000000000000000] Long   : 0
		/// </example>
		[PacketHandler(Op.PurchaseMerchandise)]
		public void PurchaseMerchandise(ChannelClient client, Packet packet)
		{
			var merchandise = new List<Merchandise>();

			var count = packet.GetInt();
			for (int i = 0; i < count; ++i)
			{
				var type = packet.GetInt();
				var id = packet.GetInt();
				var name = packet.GetString();
				var unk1 = packet.GetLong();

				merchandise.Add(new Merchandise(type, id, name));
			}

			var creature = client.GetCreatureSafe(packet.Id);

			// Disabled for now
			Send.MsgBox(creature, Localization.Get("The Merchandise Shop doesn't work yet."));
			goto L_End;

			// Check amount
			if (merchandise.Count == 0)
			{
				Log.Debug("PurchaseMerchandise: Empty list.");
				goto L_End;
			}

			// Handle
			var sum = 0;
			foreach (var good in merchandise)
			{
				// ...
			}

			// Check points
			if (creature.Points < sum)
			{
				// Unofficial
				Send.Notice(creature, Localization.Get("You don't have enough Pon."));
				goto L_End;
			}

			creature.Points -= sum;

			// Give out
			foreach (var good in merchandise)
			{
				// ...

				// Not always?
				//Send.Notice(creature, Localization.Get("You purchased {0}."), good.Name);
			}

		L_End:
			Send.PurchaseMerchandiseR(creature);
		}

		private class Merchandise
		{
			public int Type { get; set; }
			public int Id { get; set; }
			public string Name { get; set; }

			public Merchandise(int type, int id, string name)
			{
				this.Type = type;
				this.Id = id;
				this.Name = name;
			}
		}

		static int x = 0;

		/// <summary>
		/// Sent when trying to add an item to a collection book.
		/// </summary>
		/// <example>
		/// 001 [0050F0000000063B] Long   : 22781880927520315
		/// 002 [0050F0000000063C] Long   : 22781880927520316
		/// </example>
		[PacketHandler(Op.CollectionAddItem)]
		public void CollectionAddItem(ChannelClient client, Packet packet)
		{
			var bookEntityId = packet.GetLong();
			var itemEntityId = packet.GetLong();

			var creature = client.GetCreatureSafe(packet.Id);
			var book = creature.Inventory.GetItemSafe(bookEntityId);
			var item = creature.Inventory.GetItemSafe(itemEntityId);
			var max = book.Data.CollectionMax;

			// Check book
			if (book.Data.CollectionMax == 0)
			{
				Log.Warning("CollectionAddItem: User '{0}' tried to add an item to an item that isn't a collection book ({1}).", client.Account.Id, book.Info.Id);
				Send.CollectionAddItemR(creature, false);
				return;
			}

			var script = ChannelServer.Instance.ScriptManager.CollectionBookScripts.Get(book.Info.Id);
			if (script == null)
			{
				Log.Unimplemented("Collection book '{0}'.", book.Info.Id);
				Send.MsgBox(creature, Localization.Get("This collection book hasn't been implemented yet."));
				Send.CollectionAddItemR(creature, false);
				return;
			}

			var collectionList = book.GetCollectionList();
			var complete = (collectionList.Count(a => a == '1') >= max);
			if (complete)
			{
				Send.MsgBox(creature, Localization.Get("The collection book is complete."));
				Send.CollectionAddItemR(creature, false);
				return;
			}

			// Check index
			var itemIndex = script.GetIndex(item);
			if (itemIndex == -1)
			{
				Send.MsgBox(creature, Localization.Get("The item doesn't belong in this collection book."));
				Send.CollectionAddItemR(creature, false);
				return;
			}

			if (itemIndex < 0 || itemIndex > collectionList.Length - 1)
			{
				Log.Warning("CollectionAddItem: Invalid index '{0}' for '{1}' in '{2}'.", itemIndex, item.Info.Id, book.Info.Id);
				Send.MsgBox(creature, Localization.Get("Something went wrong."));
				Send.CollectionAddItemR(creature, false);
				return;
			}

			if (collectionList[itemIndex] == '1')
			{
				Send.MsgBox(creature, Localization.Get("This item has already been added to the collection book."));
				Send.CollectionAddItemR(creature, false);
				return;
			}

			// Add item
			script.OnAdd(creature, book, item);
			collectionList[itemIndex] = '1';

			// Update items
			creature.Inventory.Remove(item);

			complete = (collectionList.Count(a => a == '1') >= max);
			if (complete)
			{
				script.OnComplete(creature, book);
				book.MetaData1.SetByte("COLFLAG", 1); // 1 = can collect
			}

			book.SetCollectionList(collectionList);
			Send.ItemUpdate(creature, book);

			// Response
			Send.CollectionAddItemR(creature, true);
		}

		/// <summary>
		/// Sent when claiming the reward for a completed collection book.
		/// </summary>
		/// <example>
		/// 001 [0050F0000000063B] Long   : 22781880927520315
		/// </example>
		[PacketHandler(Op.CollectionGetReward)]
		public void CollectionGetReward(ChannelClient client, Packet packet)
		{
			var bookEntityId = packet.GetLong();

			var creature = client.GetCreatureSafe(packet.Id);
			var book = creature.Inventory.GetItemSafe(bookEntityId);
			var max = book.Data.CollectionMax;

			// Check book
			if (book.Data.CollectionMax == 0)
			{
				Log.Warning("CollectionGetReward: User '{0}' tried to claim a reward for an item that isn't a collection book ({1}).", client.Account.Id, book.Info.Id);
				Send.CollectionGetRewardR(creature, false);
				return;
			}

			if (book.MetaData1.GetByte("COLFLAG") == 3)
			{
				Send.MsgBox(creature, Localization.Get("The reward has been collected already."));
				Send.CollectionGetRewardR(creature, false);
				return;
			}

			var script = ChannelServer.Instance.ScriptManager.CollectionBookScripts.Get(book.Info.Id);
			if (script == null)
			{
				Log.Unimplemented("Collection book '{0}'.", book.Info.Id);
				Send.MsgBox(creature, Localization.Get("This collection book hasn't been implemented yet."));
				Send.CollectionGetRewardR(creature, false);
				return;
			}

			var collectionList = book.GetCollectionList();
			var complete = (collectionList.Count(a => a == '1') >= max);
			if (!complete)
			{
				Send.MsgBox(creature, Localization.Get("The collection book is not complete."));
				Send.CollectionGetRewardR(creature, false);
				return;
			}

			// Reward
			script.OnReward(creature, book);

			// Update items
			book.MetaData1.SetByte("COLFLAG", 3); // 3 = collected
			Send.ItemUpdate(creature, book);

			// Response
			Send.CollectionGetRewardR(creature, true);
		}

		/// <summary>
		/// Sent when using an Ordinary Chest.
		/// </summary>
		/// <remarks>
		/// The exact purpose of this packet is unknown, and the response
		/// can be considered a dummy, since it's not based on logs. Sending
		/// true + the entity id simply gets us past this, to the use packet.
		/// </remarks>
		[PacketHandler(Op.UnkOrdinaryChest)]
		public void UnkOrdinaryChest(ChannelClient client, Packet packet)
		{
			var chestItemEntityId = packet.GetLong();

			var creature = client.GetCreatureSafe(packet.Id);

			Send.UnkOrdinaryChestR(creature, chestItemEntityId);
		}
	}
}
