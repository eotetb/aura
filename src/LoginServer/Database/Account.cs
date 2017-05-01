// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Data;
using Aura.Data.Database;
using Aura.Shared.Database;
using Aura.Mabi.Const;
using Aura.Mabi.Util;
using Aura.Shared.Util;

namespace Aura.Login.Database
{
	public class Account
	{
		public string Name { get; set; }
		public string Password { get; set; }
		public string SecondaryPassword { get; set; }
		public long SessionKey { get; set; }

		public byte Authority { get; set; }

		public DateTime Creation { get; set; }
		public DateTime LastLogin { get; set; }
		public string LastIp { get; set; }

		public string BannedReason { get; set; }
		public DateTime BannedExpiration { get; set; }

		public PremiumServices PremiumServices { get; private set; }

		public List<Card> CharacterCards { get; set; }
		public List<Card> PetCards { get; set; }
		public List<Character> Characters { get; set; }
		public List<Character> Pets { get; set; }
		public List<Gift> Gifts { get; set; }

		public Account()
		{
			this.Creation = DateTime.Now;
			this.LastLogin = DateTime.Now;

			this.PremiumServices = new PremiumServices();

			this.CharacterCards = new List<Card>();
			this.PetCards = new List<Card>();
			this.Gifts = new List<Gift>();

			this.Characters = new List<Character>();
			this.Pets = new List<Character>();
		}

		/// <summary>
		/// Returns character card with id, or null if it doesn't exist.
		/// </summary>
		public Card GetCharacterCard(long id)
		{
			return this.CharacterCards.FirstOrDefault(a => a.Id == id);
		}

		/// <summary>
		/// Returns pet/partner card with id, or null if it doesn't exist.
		/// </summary>
		public Card GetPetCard(long id)
		{
			return this.PetCards.FirstOrDefault(a => a.Id == id);
		}

		/// <summary>
		/// Returns gift with id, or null if it doesn't exist.
		/// </summary>
		public Gift GetGift(long id)
		{
			return this.Gifts.FirstOrDefault(a => a.Id == id);
		}

		/// <summary>
		/// Returns character with id, or null if it doesn't exist.
		/// </summary>
		public Character GetCharacter(long id)
		{
			return this.Characters.FirstOrDefault(a => a.EntityId == id);
		}

		/// <summary>
		/// Returns pet/partner with id, or null if it doesn't exist.
		/// </summary>
		public Character GetPet(long id)
		{
			return this.Pets.FirstOrDefault(a => a.EntityId == id);
		}

		/// <summary>
		/// Creates new character for this account. Returns true if successful,
		/// character's ids are also set in that case.
		/// </summary>
		/// <param name="character"></param>
		/// <param name="cardInfo"></param>
		/// <returns></returns>
		public bool CreateCharacter(Character character, List<Item> items)
		{
			if (!LoginServer.Instance.Database.CreateCharacter(this.Name, character, items))
				return false;

			this.Characters.Add(character);

			return true;
		}

		/// <summary>
		/// Creates new pet for this account. Returns true if successful,
		/// pet's ids are also set in that case.
		/// </summary>
		/// <param name="pet"></param>
		/// <returns></returns>
		public bool CreatePet(Character pet)
		{
			if (!LoginServer.Instance.Database.CreatePet(this.Name, pet))
				return false;

			this.Pets.Add(pet);

			return true;
		}

		/// <summary>
		/// Creates new partner for this account. Returns true if successful,
		/// pet's ids are also set in that case.
		/// </summary>
		/// <param name="partner"></param>
		/// <returns></returns>
		public bool CreatePartner(Character partner)
		{
			int setId = 0;
			if (partner.Race == 730201 || partner.Race == 730202 || partner.Race == 730204 || partner.Race == 730205)
				setId = 1000;
			else if (partner.Race == 730203)
				setId = 1001;
			else if (partner.Race == 730206)
				setId = 1002;
			else if (partner.Race == 730207)
				setId = 1004;

			// Create start items for card and hair/face
			var cardItems = AuraData.CharCardSetDb.Find(setId, partner.Race);
			if (cardItems == null)
			{
				Log.Error("Partner creation: Invalid item set ({0}) for race {1}.", setId, partner.Race);
				return false;
			}

			// TODO: Hash seems to be incorrect.
			var items = Item.CardItemsToItems(cardItems);
			Item.GenerateItemColors(ref items, (this.Name + partner.Race + partner.SkinColor + partner.Hair + partner.HairColor + 1 + partner.EyeType + partner.EyeColor + partner.MouthType + partner.Face));

			items.Add(new Item(partner.Face, Pocket.Face, partner.SkinColor, 0, 0));
			items.Add(new Item(partner.Hair, Pocket.Hair, (uint)partner.HairColor + 0x10000000u, 0, 0));

			if (!LoginServer.Instance.Database.CreatePartner(this.Name, partner, items))
			{
				Log.Error("Partner creation: Failed for unknown reasons.");
				return false;
			}

			this.Pets.Add(partner);

			return true;
		}

		/// <summary>
		/// Deletes character card from account.
		/// </summary>
		/// <param name="cardId"></param>
		/// <param name="card"></param>
		public bool DeleteCharacterCard(Card card)
		{
			if (!LoginServer.Instance.Database.DeleteCard(card))
				return false;

			this.CharacterCards.Remove(card);

			return true;
		}

		/// <summary>
		/// Deletes pet card from account.
		/// </summary>
		/// <param name="card"></param>
		public bool DeletePetCard(Card card)
		{
			if (!LoginServer.Instance.Database.DeleteCard(card))
				return false;

			this.PetCards.Remove(card);

			return true;
		}

		/// <summary>
		/// Changes gift to an ordinary card.
		/// </summary>
		/// <param name="gift"></param>
		public void ChangeGiftToCard(Gift gift)
		{
			this.Gifts.Remove(gift);

			if (gift.IsCharacter)
				this.CharacterCards.Add(gift);
			else
				this.PetCards.Add(gift);

			LoginServer.Instance.Database.ChangeGiftToCard(gift.Id);
		}

		/// <summary>
		/// Deletes gift from account.
		/// </summary>
		/// <param name="gift"></param>
		public void DeleteGift(Gift gift)
		{
			LoginServer.Instance.Database.DeleteCard(gift);
			this.Gifts.Remove(gift);
		}
	}
}
