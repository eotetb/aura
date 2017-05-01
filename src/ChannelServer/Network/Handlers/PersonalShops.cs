// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.Network.Sending;
using Aura.Channel.Util;
using Aura.Shared.Network;
using Aura.Shared.Util;
using Aura.Channel.World;
using Aura.Mabi.Const;
using Aura.Channel.World.Inventory;
using Aura.Mabi.Network;
using System;
using Aura.Channel.World.Shops;
using Aura.Data;

namespace Aura.Channel.Network.Handlers
{
	public partial class ChannelServerHandlers : PacketHandlerManager<ChannelClient>
	{
		/// <summary>
		/// Sent when trying to prepare to set up a shop.
		/// </summary>
		/// <example>
		/// 001 [005000CB8F42A1E9] Long   : 22518872418722281
		/// 002 [005000CCCA71DA4D] Long   : 22518877706639949
		/// </example>
		[PacketHandler(Op.PersonalShopCheck)]
		public void PersonalShopCheck(ChannelClient client, Packet packet)
		{
			var bagEntityId = packet.GetLong();
			var licenseEntityId = packet.GetLong();

			var creature = client.GetCreatureSafe(packet.Id);
			var bag = creature.Inventory.GetItemSafe(bagEntityId);
			var license = creature.Inventory.GetItemSafe(licenseEntityId);

			// Check feature
			if (!AuraData.FeaturesDb.IsEnabled("PersonalShop"))
			{
				Send.MsgBox(creature, Localization.Get("This feature has not been enabled yet."));
				Send.PersonalShopCheckR(creature, false, 0, 0);
				return;
			}

			// Check bag
			if (!bag.HasTag("/personal_shop_available/"))
			{
				Log.Warning("PersonalShopCheck: User '{0}' tried to use invalid item bag.", client.Account.Id);

				Send.MsgBox(creature, Localization.Get("Invalid item bag."));
				Send.PersonalShopCheckR(creature, false, 0, 0);
				return;
			}

			// Check license
			if (!license.HasTag("/personalshoplicense/"))
			{
				Log.Warning("PersonalShopCheck: User '{0}' tried to use invalid license.", client.Account.Id);

				Send.MsgBox(creature, Localization.Get("Invalid license."));
				Send.PersonalShopCheckR(creature, false, 0, 0);
				return;
			}

			if (license.MetaData1.Has("EVALUE") && !ChannelServer.Instance.Conf.World.ReusingPersonalShopLicenses)
			{
				// Unofficial
				Send.MsgBox(creature, Localization.Get("You can't re-use a license that has revenue on it."));
				Send.PersonalShopCheckR(creature, false, 0, 0);
				return;
			}

			// Check location
			if (!PersonalShop.CanPlace(creature, license.Data.PersonalShopLicense))
			{
				Send.MsgBox(creature, Localization.Get("Personal Shops are allowed only in designated areas."));
				Send.PersonalShopCheckR(creature, false, 0, 0);
				return;
			}

			// Create shop
			var shop = new PersonalShop(creature, bag, license);

			// Response
			Send.PersonalShopCheckR(creature, true, bagEntityId, licenseEntityId);
		}

		/// <summary>
		/// Sent when changing a shop's title.
		/// </summary>
		/// <example>
		/// 001 [................] String : test
		/// </example>
		[PacketHandler(Op.PersonalShopChangeTitle)]
		public void PersonalShopChangeTitle(ChannelClient client, Packet packet)
		{
			var title = packet.GetString();

			var creature = client.GetCreatureSafe(packet.Id);
			var shop = creature.Temp.ActivePersonalShop;

			// Check shop
			if (shop == null)
			{
				Log.Warning("PersonalShopChangeTitle: User '{0}' tried to change title of non-existent shop.", client.Account.Id);
				return;
			}

			// Check title
			if (!Math2.Between(title.Length, 0, 80))
			{
				Log.Warning("PersonalShopChangeTitle: User '{0}' tried to set invalid title '{1}'.", client.Account.Id, title);
				return;
			}

			shop.ChangeTitle(title);
		}

		/// <summary>
		/// Sent when changing a shop's description.
		/// </summary>
		/// <example>
		/// 001 [................] String : test
		/// </example>
		[PacketHandler(Op.PersonalShopChangeDescription)]
		public void PersonalShopChangeDescription(ChannelClient client, Packet packet)
		{
			var description = packet.GetString();

			var creature = client.GetCreatureSafe(packet.Id);
			var shop = creature.Temp.ActivePersonalShop;

			// Check shop
			if (shop == null)
			{
				Log.Warning("PersonalShopChangeDescription: User '{0}' tried to change description of non-existent shop.", client.Account.Id);
				return;
			}

			// Check description
			if (!Math2.Between(description.Length, 0, 80))
			{
				Log.Warning("PersonalShopChangeDescription: User '{0}' tried to set invalid description '{1}'.", client.Account.Id, description);
				return;
			}

			shop.ChangeDescription(description);
		}

		/// <summary>
		/// Sent when changing a shop's description.
		/// </summary>
		/// <example>
		/// 001 [................] String : test
		/// </example>
		[PacketHandler(Op.PersonalShopTakeDown)]
		public void PersonalShopTakeDown(ChannelClient client, Packet packet)
		{
			var byDistance = packet.GetBool();

			var creature = client.GetCreatureSafe(packet.Id);
			var shop = creature.Temp.ActivePersonalShop;

			// Check shop
			if (shop == null)
			{
				Log.Warning("PersonalShopTakeDown: User '{0}' tried to take down non-existent shop.", client.Account.Id);
				Send.PersonalShopTakeDownR(creature, false);
				return;
			}

			// Notice if the shop was closed due to being too far from it.
			if (byDistance)
				Send.MsgBox(creature, Localization.Get("The Personal Shop has been closed."));

			shop.TakeDown();

			Send.PersonalShopTakeDownR(creature, true);
		}

		/// <summary>
		/// Sent when setting up the shop, after setting the item prices.
		/// </summary>
		/// <example>
		/// 001 [................] String : title
		/// 002 [................] String : description
		/// </example>
		[PacketHandler(Op.PersonalShopSetUp)]
		public void PersonalShopSetUp(ChannelClient client, Packet packet)
		{
			var title = packet.GetString();
			var description = packet.GetString();

			var creature = client.GetCreatureSafe(packet.Id);
			var shop = creature.Temp.ActivePersonalShop;

			// Check shop
			if (shop == null)
			{
				Log.Warning("PersonalShopSetUp: User '{0}' tried to set up non-existent shop.", client.Account.Id);
				Send.PersonalShopSetUpR_Fail(creature);
				return;
			}

			var success = shop.SetUp(title, description);

			// Shouldn't happen on an unmodified client.
			if (!success)
				Send.MsgBox(creature, Localization.Get("Failed to set up shop."));

			Send.PersonalShopSetUpR(creature, shop.Prop);
		}

		/// <summary>
		/// Sent when changing the price of one or more items
		/// (Price vs PriceForAll).
		/// </summary>
		/// <example>
		/// 001 [0050000000000AD5] Long   : 22517998136855253
		/// 002 [........000001F4] Int    : 500
		/// </example>
		[PacketHandler(Op.PersonalShopSetPrice, Op.PersonalShopSetPriceForAll)]
		public void PersonalShopSetPrice(ChannelClient client, Packet packet)
		{
			var itemEntityId = packet.GetLong();
			var price = packet.GetInt();

			var creature = client.GetCreatureSafe(packet.Id);
			var shop = creature.Temp.ActivePersonalShop;

			// Check shop
			if (shop == null)
			{
				Log.Warning("PersonalShopSetPrice: User '{0}' tried to set price for an item in a non-existent shop.", client.Account.Id);
				Send.PersonalShopTakeDownR(creature, false);
				return;
			}

			// Check limit
			if (price < 0 || price > shop.LicenseData.Limit)
			{
				Send.MsgBox(creature, Localization.Get("Exceeded the price limit for permit."));
				if (packet.Op == Op.PersonalShopSetPriceForAll)
					Send.PersonalShopSetPriceForAllR(creature, false);
				else
					Send.PersonalShopSetPriceR(creature, false);
				return;
			}

			// Set and response
			var success = false;
			if (packet.Op == Op.PersonalShopSetPriceForAll)
			{
				success = shop.SetPrices(itemEntityId, price);
				Send.PersonalShopSetPriceForAllR(creature, success);
			}
			else
			{
				success = shop.SetPrice(itemEntityId, price);
				Send.PersonalShopSetPriceR(creature, success);
			}

			if (!success)
				Log.Warning("PersonalShopSetPrice: User '{0}' tried to set price for an invalid item.", client.Account.Id);
		}

		/// <summary>
		/// Sent when instructing an already spawned pet to protect the
		/// personal shop.
		/// </summary>
		/// <example>
		/// No parameters.
		/// </example>
		[PacketHandler(Op.PersonalShopPetProtectRequest)]
		public void PersonalShopPetProtectRequest(ChannelClient client, Packet packet)
		{
			var creature = client.GetCreatureSafe(packet.Id);
			var pet = creature.Pet;
			var shop = creature.Temp.ActivePersonalShop;

			if (creature.Master != null)
				Send.MsgBox(creature.Master, Localization.Get("This feature is not available yet."));
			Send.PersonalShopPetProtectRequestR(creature, false);

			//// Check shop
			//if (shop == null)
			//{
			//	Log.Warning("PersonalShopPetProtectRequest: User '{0}' tried to set protection pet for a non-existent shop.", client.Account.Id);
			//	Send.PersonalShopPetProtectRequestR(creature, false);
			//	return;
			//}

			//// Check pet
			//if (pet == null)
			//{
			//	Log.Warning("PersonalShopPetProtectRequest: User '{0}' tried to set a non-existent pet to protect the shop.", client.Account.Id);
			//	Send.PersonalShopPetProtectRequestR(creature, false);
			//	return;
			//}
		}

		/// <summary>
		/// Sent when player clicked on a shop banner.
		/// </summary>
		/// <example>
		/// 001 [00100000000E7560] Long   : 4503599628318048
		/// </example>
		[PacketHandler(Op.PersonalShopOpen)]
		public void PersonalShopOpen(ChannelClient client, Packet packet)
		{
			var ownerEntityId = packet.GetLong();

			var creature = client.GetCreatureSafe(packet.Id);

			// Check owner
			var owner = ChannelServer.Instance.World.GetCreature(ownerEntityId);
			if (owner == null)
			{
				Send.MsgBox(creature, Localization.Get("Personal Shop not found."));
				Send.PersonalShopOpenR(creature, null);
				return;
			}

			// Check shop
			var shop = owner.Temp.ActivePersonalShop;
			if (shop == null || shop.Region != creature.Region || !creature.GetPosition().InRange(shop.Prop.GetPosition(), 1000))
			{
				Send.MsgBox(creature, Localization.Get("Personal Shop not found."));
				Send.PersonalShopOpenR(creature, null);
				return;
			}

			shop.OpenFor(creature);
		}

		/// <summary>
		/// Sent when player clicked on a shop banner.
		/// </summary>
		/// <example>
		/// 001 [00100000000E7560] Long   : 4503599628318048
		/// </example>
		[PacketHandler(Op.PersonalShopClose)]
		public void PersonalShopClose(ChannelClient client, Packet packet)
		{
			var ownerEntityId = packet.GetLong();

			var creature = client.GetCreatureSafe(packet.Id);

			// Try to call close on the shop, but send close window if
			// anything goes wrong, so the player doesn't get stuck.

			// Check owner
			var owner = ChannelServer.Instance.World.GetCreature(ownerEntityId);
			if (owner == null)
			{
				Send.PersonalShopCloseWindow(creature);
				return;
			}

			// Check shop
			var shop = owner.Temp.ActivePersonalShop;
			if (shop == null)
			{
				Send.PersonalShopCloseWindow(creature);
				return;
			}

			shop.CloseFor(creature);
		}

		/// <summary>
		/// Sent when player clicked on a shop banner.
		/// </summary>
		/// <example>
		/// 001 [0050000000000AD5] Long   : 22517998136855253
		/// 002 [..............00] Byte   : 0
		/// 003 [..............00] Byte   : 0
		/// 004 [0010000000000002] Long   : 4503599627370498
		/// 005 [........00000032] Int    : 50
		/// </example>
		[PacketHandler(Op.PersonalShopBuy)]
		public void PersonalShopBuy(ChannelClient client, Packet packet)
		{
			var itemEntityId = packet.GetLong();
			var unkByte1 = packet.GetByte();
			var directBankTransaction = packet.GetBool();
			var ownerEntityId = packet.GetLong();
			var price = packet.GetInt(); // Totally gonna use this.

			var creature = client.GetCreatureSafe(packet.Id);

			// Close shop if anything goes wrong.

			// Check owner
			var owner = ChannelServer.Instance.World.GetCreature(ownerEntityId);
			if (owner == null)
			{
				Send.MsgBox(creature, Localization.Get("Personal Shop not found."));
				Send.PersonalShopBuyR(creature, false, 0);
				Send.PersonalShopCloseWindow(creature);
				return;
			}

			// Check shop
			var shop = owner.Temp.ActivePersonalShop;
			if (shop == null || shop.Region != creature.Region || !creature.GetPosition().InRange(shop.Prop.GetPosition(), 1000))
			{
				Send.MsgBox(creature, Localization.Get("Personal Shop not found."));
				Send.PersonalShopBuyR(creature, false, 0);
				Send.PersonalShopCloseWindow(creature);
				return;
			}

			var success = shop.Buy(creature, itemEntityId, directBankTransaction);
			if (success)
				Send.PersonalShopBuyR(creature, true, itemEntityId);
			else
				Send.PersonalShopBuyR(creature, false, 0);
		}
	}
}
