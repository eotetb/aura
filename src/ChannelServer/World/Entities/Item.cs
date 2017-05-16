// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Linq;
using System.Threading;
using Aura.Data;
using Aura.Data.Database;
using Aura.Mabi.Const;
using Aura.Mabi.Structs;
using Aura.Shared.Util;
using Aura.Mabi;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Aura.Channel.World.Quests;

namespace Aura.Channel.World.Entities
{
	public class Item : Entity
	{
		/// <summary>
		/// Default radius for item drops.
		/// </summary>
		public const int DropRadius = 50;

		/// <summary>
		/// Maximum item experience (proficiency).
		/// </summary>
		private const int MaxProficiency = 101000;

		/// <summary>
		/// Maximum item experience (proficiency).
		/// </summary>
		private const int UncappedMaxProficiency = 251000;

		/// <summary>
		/// Unique item id that is increased for every new item.
		/// </summary>
		private static long _itemId = MabiId.TmpItems;

		/// <summary>
		/// List of upgrade effects.
		/// </summary>
		private List<UpgradeEffect> _upgrades = new List<UpgradeEffect>();

		/// <summary>
		/// Returns entity data type "Item".
		/// </summary>
		public override DataType DataType { get { return DataType.Item; } }

		/// <summary>
		/// Gets or sets the item's region, forwarding to Info.Region.
		/// </summary>
		public override int RegionId
		{
			get { return this.Info.Region; }
			set { this.Info.Region = value; }
		}

		/// <summary>
		/// Public item information
		/// </summary>
		public ItemInfo Info;

		/// <summary>
		/// Private item information
		/// </summary>
		public ItemOptionInfo OptionInfo;

		/// <summary>
		/// Aura database item data
		/// </summary>
		public ItemData Data { get; protected set; }

		/// <summary>
		/// Meta information 1
		/// </summary>
		public MabiDictionary MetaData1 { get; protected set; }

		/// <summary>
		/// Meta information 2
		/// </summary>
		public MabiDictionary MetaData2 { get; protected set; }

		/// <summary>
		/// Ego weapon information
		/// </summary>
		public EgoInfo EgoInfo { get; protected set; }

		/// <summary>
		/// Bank at which the item is currently lying around.
		/// </summary>
		public string Bank { get; set; }

		/// <summary>
		/// Time at which the current transfer, if any, started.
		/// </summary>
		/// <remarks>
		/// TODO: Make a new base class BankItem and restructure the db,
		///   to have one item table, that other tables can reference?
		///   (E.g. inventory, bank, mail, etc.)
		/// </remarks>
		public DateTime BankTransferStart { get; set; }

		/// <summary>
		/// Duration of the current transfer.
		/// </summary>
		public int BankTransferDuration { get; set; }

		/// <summary>
		/// Milliseconds remaining until item arrives at new bank.
		/// </summary>
		public int BankTransferRemaining
		{
			get
			{
				var now = DateTime.Now;
				var end = this.BankTransferStart.AddMilliseconds(this.BankTransferDuration);

				if (end > now)
					return (int)(end - now).TotalMilliseconds;
				else
					return 0;
			}
		}

		/// <summary>
		/// Returns item's quality on a scale from 0 to 100. (Used for food.)
		/// </summary>
		public int Quality
		{
			get
			{
				if (this.MetaData1.Has("QUAL"))
					// -100~100 + 100 = 0~200 / 2 = 0~100
					return (this.MetaData1.GetInt("QUAL") + 100) / 2;

				// Return 0 if no quality was set, the client does the same.
				return 0;
			}
		}

		private bool _firstTimeAppear = true;
		/// <summary>
		/// Returns true once, and false afterwards, until it's set true again.
		/// Used to decide whether the appearing item should bounce.
		/// </summary>
		public bool FirstTimeAppear
		{
			get
			{
				var result = _firstTimeAppear;
				_firstTimeAppear = false;
				return result;
			}
		}

		/// <summary>
		/// If item includes a quest, this value is equal to its unique id.
		/// </summary>
		public long QuestId { get { return (this.Quest == null ? 0 : this.Quest.UniqueId); } }

		/// <summary>
		/// Gets or sets the quest this item includes.
		/// </summary>
		/// <remarks>
		/// Mainly used by quest scrolls. If an item that includes a quest
		/// is added/removed to/from the inventory, the quest is
		/// added/removed as well.
		/// </remarks>
		public Quest Quest { get; set; }

		/// <summary>
		/// Sets and returns the current amount (Info.Amount).
		/// Setting is restricted to a minimum of 0 and a maximum of StackMax.
		/// </summary>
		public int Amount
		{
			get { return this.Info.Amount; }
			set { this.Info.Amount = (ushort)Math2.Clamp(0, this.Data.StackMax, value); }
		}

		/// <summary>
		/// Returns ".OptionInfo.Balance / 100".
		/// </summary>
		public float Balance
		{
			get { return this.OptionInfo.Balance / 100f; }
		}

		/// <summary>
		/// Returns ".OptionInfo.Critical / 100".
		/// </summary>
		public float Critical
		{
			get { return this.OptionInfo.Critical / 100f; }
		}

		/// <summary>
		/// Returns true if tag contains "/pounch/bag/" (item bags).
		/// </summary>
		public bool IsBag
		{
			get { return this.Data.HasTag("/pouch/bag/"); }
		}

		/// <summary>
		/// Returns true if tag contains "/pouch/money/" (gold pouches).
		/// </summary>
		public bool IsGoldPouch
		{
			get { return this.Data.HasTag("/pouch/money/"); }
		}

		/// <summary>
		/// Returns true if tag contains "/sac_item/" (gathering bags, e.g. wool pouch).
		/// </summary>
		public bool IsGatheringPouch
		{
			get { return this.Data.HasTag("/sac_item/"); }
		}

		/// <summary>
		/// Gets or sets item's durability, capping it at 0~DuraMax.
		/// </summary>
		public int Durability
		{
			get { return this.OptionInfo.Durability; }
			set { this.OptionInfo.Durability = Math2.Clamp(0, this.OptionInfo.DurabilityMax, value); }
		}

		/// <summary>
		/// Gets or sets item's experience (proficiency), capping it at 0~100.
		/// </summary>
		/// <remarks>
		/// Officially two fields are used, Experience and EP, EP being the points
		/// and Exp the value in the parentheses. But using only one value
		/// seems much easier. Makes it work more like Durability. This
		/// requires some fixing for the client though.
		/// </remarks>
		public int Proficiency
		{
			get { return this.OptionInfo.Experience + this.OptionInfo.EP * 1000; }
			set
			{
				var max = MaxProficiency;
				if (ChannelServer.Instance.Conf.World.UncapProficiency)
					max = UncappedMaxProficiency;

				var newValue = Math2.Clamp(0, max, value);
				if (newValue == max)
				{
					this.OptionInfo.Experience = 1000;
					this.OptionInfo.EP = (byte)((newValue / 1000) - 1);
				}
				else
				{
					this.OptionInfo.Experience = (short)(newValue % 1000);
					this.OptionInfo.EP = (byte)(newValue / 1000);
				}
			}
		}

		/// <summary>
		/// The id of the entity that "produced" this item, and the only one
		/// who can pick it up during the protection time.
		/// </summary>
		public long OwnerId { get; set; }

		/// <summary>
		/// The time at which the item becomes "free for all", where not only
		/// the owner can pick it up.
		/// </summary>
		public DateTime? ProtectionLimit { get; set; }

		/// <summary>
		/// Returns true if item is a able to receive proficiency.
		/// </summary>
		public bool IsTrainableWeapon
		{
			get { return this.Data.WeaponType != 0; }
		}

		/// <summary>
		/// Gets or sets whether the item is displayed as new in inv.
		/// [190100, NA200 (2015-01-15)]
		/// </summary>
		public bool IsNew { get; set; }

		/// <summary>
		/// Returns true if item has "Blessed" flag.
		/// </summary>
		public bool IsBlessed { get { return ((this.OptionInfo.Flags & ItemFlags.Blessed) != 0); } }

		/// <summary>
		/// Returns true if item is a dungeon key.
		/// </summary>
		/// <remarks>
		/// We could check for /dungeon/key/ here, but there's more items with
		/// that tag, especially event keys, and I don't think we want to drop
		/// those automatically in dungeons.
		/// Instead we're gonna check the 3 key ids used for (presumably) every
		/// dungeon in the game.
		/// </remarks>
		public bool IsDungeonKey { get { return (this.Info.Id >= 70028 && this.Info.Id <= 70030); } }

		/// <summary>
		/// Returns true if item is a dungeon room or boss room key.
		/// </summary>
		public bool IsDungeonRoomKey { get { return (this.Info.Id >= 70029 && this.Info.Id <= 70030); } }

		/// <summary>
		/// Returns true if item is a dungeon pass.
		/// </summary>
		/// <remarks>
		/// Quest items that work like a dungeon pass basically are dungeon
		/// passes, and should return true as well.
		/// </remarks>
		public bool IsDungeonPass { get { return (this.HasTag("/dungeon_pass/")); } }

		/// <summary>
		/// Returns true if item is a shield.
		/// </summary>
		public bool IsShield { get { return this.HasTag("/shield/"); } }

		/// <summary>
		/// Returns true if item is a shield or is equipped like a shield (e.g. books).
		/// </summary>
		public bool IsShieldLike { get { return (this.IsShield || this.IsEquippableBook); } }

		/// <summary>
		/// Returns true if item is two handed.
		/// </summary>
		public bool IsTwoHand { get { return this.HasTag("/twohand/"); } }

		/// <summary>
		/// Returns true if item is a book that can be equipped.
		/// </summary>
		public bool IsEquippableBook { get { return this.HasTag("/equip/*/book/"); } }

		/// <summary>
		/// Returns true if item is an enchant.
		/// </summary>
		public bool IsEnchant { get { return this.HasTag("/enchantscroll/"); } }

		/// <summary>
		/// Returns true if item is a bow or crossbow.
		/// </summary>
		public bool IsBow { get { return this.HasTag("/bow/|/bow01|/crossbow/"); } }

		/// <summary>
		/// Returns true if item is generally able to lose durability.
		/// </summary>
		public bool IsBreakable { get { return (this.OptionInfo.DurabilityOriginal != 0); } }

		/// <summary>
		/// Returns true if item can be destroyed.
		/// </summary>
		public bool IsDestroyable
		{
			get
			{
				// The check for the Sword of Elsinore is a terrible hack,
				// but in my defense, it was devCAT's idea. Instead of adding the
				// destroyable tag to the item, the client checks for the
				// hamlets_sword tag >_>
				return this.HasTag("/destroyable/|/hamlets_sword/|/guild_robe/");
			}
		}

		/// <summary>
		/// Returns true if item has upgrade effects, e.g. from upgrades
		/// or enchants.
		/// </summary>
		public int UpgradeEffectCount { get { lock (_upgrades) return (_upgrades.Count); } }

		/// <summary>
		/// Returns true if item can be blessed.
		/// </summary>
		public bool IsBlessable
		{
			get
			{
				return
					(this.HasTag("/equip/") && !this.HasTag("/not_bless/")) &&
					(this.Info.Pocket != Pocket.Magazine1 && this.Info.Pocket != Pocket.Magazine2);
			}
		}

		/// <summary>
		/// Returns true if item hasn't been completed yet, e.g via Tailoring
		/// or Blacksmithing.
		/// </summary>
		public bool IsIncomplete { get { return this.MetaData1.Has("PRGRATE"); } }

		/// <summary>
		/// Item's price in a personal shop.
		/// </summary>
		public int PersonalShopPrice { get; set; }

		/// <summary>
		/// Amount of times the item can be bought from an NPC shop.
		/// </summary>
		public int Stock { get; set; }

		/// <summary>
		/// New item based on item id.
		/// </summary>
		/// <param name="itemId"></param>
		public Item(int itemId)
		{
			this.Init(itemId);
			this.SetNewEntityId();

			// Run OnCreation script
			var script = ChannelServer.Instance.ScriptManager.ItemScripts.Get(itemId);
			if (script != null)
				script.OnCreation(this);

			// Color of book seals
			var sealColor = this.MetaData1.GetString("MGCSEL");
			if (sealColor != null)
			{
				switch (sealColor)
				{
					//case "blue": this.Info.Color3 = 0xF4AE05; break;
					//case "red": this.Info.Color3 = 0xF4AE05; break;
					case "yellow": this.Info.Color3 = 0xF4AE05; break;
				}
			}
		}

		/// <summary>
		/// Creates new item based on drop data.
		/// </summary>
		/// <param name="dropData"></param>
		public Item(DropData dropData)
			: this(dropData.ItemId)
		{
			var rnd = RandomProvider.Get();

			// Amount
			this.Info.Amount = (ushort)rnd.Next(dropData.AmountMin, dropData.AmountMax + 1);
			if (this.Data.StackType != StackType.Sac && this.Info.Amount < 1)
				this.Info.Amount = 1;

			// Meta data, set before anything else, so other properties can
			// overwrite the ones set manually.
			if (!string.IsNullOrWhiteSpace(dropData.MetaData1))
				this.MetaData1.Parse(dropData.MetaData1);
			if (!string.IsNullOrWhiteSpace(dropData.MetaData2))
				this.MetaData2.Parse(dropData.MetaData2);

			// Set enchant meta data or apply option sets to item
			if (dropData.Prefix != 0 || dropData.Suffix != 0)
			{
				if (this.HasTag("/enchantscroll/"))
				{
					// Prefix
					if (dropData.Prefix != 0)
					{
						var data = AuraData.OptionSetDb.Find(dropData.Prefix);
						if (data == null) throw new ArgumentException("Option set doesn't exist: " + dropData.Prefix);
						if (data.Category != OptionSetCategory.Prefix) throw new ArgumentException("Option set " + dropData.Prefix + " is not a prefix.");

						this.MetaData1.SetInt("ENPFIX", dropData.Prefix);
					}

					// Suffix
					if (dropData.Suffix != 0)
					{
						var data = AuraData.OptionSetDb.Find(dropData.Suffix);
						if (data == null) throw new ArgumentException("Option set doesn't exist: " + dropData.Suffix);
						if (data.Category != OptionSetCategory.Suffix) throw new ArgumentException("Option set " + dropData.Suffix + " is not a suffix.");

						this.MetaData1.SetInt("ENSFIX", dropData.Suffix);
					}

					// TODO: Expiration?
				}
				else
					this.ApplyPreSuffix(dropData.Prefix, dropData.Suffix);
			}

			// Colors
			if (dropData.Color1 != null) this.Info.Color1 = (uint)dropData.Color1;
			if (dropData.Color2 != null) this.Info.Color2 = (uint)dropData.Color2;
			if (dropData.Color3 != null) this.Info.Color3 = (uint)dropData.Color3;

			// Lowered durability
			if (dropData.Durability != -1)
				this.Durability = dropData.Durability;

			// Food quality
			if (dropData.FoodQuality != null)
				this.MetaData1.SetInt("QUAL", (int)dropData.FoodQuality);

			// Form id (manuals)
			if (dropData.FormId != null)
				this.MetaData1.SetInt("FORMID", (int)dropData.FormId);

			// Scale (fish, gems)
			if (dropData.Scale != null)
				this.MetaData1.SetFloat("SCALE", (float)dropData.Scale);
		}

		/// <summary>
		/// Item based on item and entity id.
		/// </summary>
		/// <param name="itemId"></param>
		/// <param name="entityId"></param>
		public Item(int itemId, long entityId)
		{
			this.Init(itemId);
			this.EntityId = entityId;
		}

		/// <summary>
		/// New item based on existing item.
		/// </summary>
		/// <param name="baseItem"></param>
		public Item(Item baseItem)
		{
			this.Info = baseItem.Info;
			this.OptionInfo = baseItem.OptionInfo;
			this.Data = baseItem.Data;
			this.MetaData1 = new MabiDictionary(baseItem.MetaData1.ToString());
			this.MetaData2 = new MabiDictionary(baseItem.MetaData2.ToString());
			this.EgoInfo = baseItem.EgoInfo.Copy();
			this.AddUpgradeEffect(baseItem.GetUpgradeEffects());

			if (baseItem.Quest != null)
			{
				this.Quest = new Quest(baseItem.Quest.Id);
				this.Quest.QuestItem = this;
			}

			this.SetNewEntityId();
		}

		/// <summary>
		/// Sets item id, initializes item and loads defaults.
		/// </summary>
		/// <param name="itemId"></param>
		private void Init(int itemId)
		{
			this.Info.Id = itemId;
			this.MetaData1 = new MabiDictionary();
			this.MetaData2 = new MabiDictionary();
			this.EgoInfo = new EgoInfo();

			this.LoadDefault();
		}

		/// <summary>
		/// Returns new ego weapon.
		/// </summary>
		/// <param name="itemId"></param>
		/// <param name="name"></param>
		/// <param name="egoRace"></param>
		/// <returns></returns>
		public static Item CreateEgo(int itemId, EgoRace egoRace, string name)
		{
			var item = new Item(itemId);
			item.EgoInfo.Race = egoRace;
			item.EgoInfo.Name = name;
			item.Info.FigureB = 1;

			return item;
		}

		/// <summary>
		/// Returns new stack of gold.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public static Item CreateGold(int amount)
		{
			var item = new Item(2000);
			item.Amount = amount;

			return item;
		}

		/// <summary>
		/// Creates item based on parameters.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public static Item Create(int id, int amount = 0, int amountMin = 0, int amountMax = 0, uint? color1 = null, uint? color2 = null, uint? color3 = null, int prefix = 0, int suffix = 0, int expires = 0, int durability = -1)
		{
			var dropData = new DropData(id, 100, amount, amountMin, amountMax, color1, color2, color3, prefix, suffix, expires, durability);
			return new Item(dropData);
		}

		/// <summary>
		/// Returns new item, enchanted with the given prefix/suffix.
		/// </summary>
		/// <param name="itemId">Id of the item to create.</param>
		/// <param name="prefix">Id of the prefix option set to apply to item, 0 for none.</param>
		/// <param name="suffix">Id of the suffix option set to apply to item, 0 for none.</param>
		/// <returns></returns>
		public static Item CreateEnchanted(int itemId, int prefix = 0, int suffix = 0)
		{
			var item = new Item(itemId);
			item.ApplyPreSuffix(prefix, suffix);

			return item;
		}

		/// <summary>
		/// Applies given prefix and/or suffix.
		/// </summary>
		/// <param name="prefix">Prefix to apply, 0 for none.</param>
		/// <param name="suffix">Suffix to apply, 0 for none.</param>
		public void ApplyPreSuffix(int prefix, int suffix)
		{
			// Prefix
			if (prefix > 0)
			{
				var data = AuraData.OptionSetDb.Find(prefix);
				if (data == null)
					throw new ArgumentException("Option set doesn't exist: " + prefix);
				if (data.Category != OptionSetCategory.Prefix)
					throw new ArgumentException("Option set " + prefix + " is not a prefix.");

				this.OptionInfo.Prefix = (ushort)prefix;
				this.ApplyOptionSet(data, true);
			}

			// Suffix
			if (suffix > 0)
			{
				var data = AuraData.OptionSetDb.Find(suffix);
				if (data == null)
					throw new ArgumentException("Option set doesn't exist: " + suffix);
				if (data.Category != OptionSetCategory.Suffix)
					throw new ArgumentException("Option set " + suffix + " is not a suffix.");

				this.OptionInfo.Suffix = (ushort)suffix;
				this.ApplyOptionSet(data, true);
			}
		}

		/// <summary>
		/// Returns enchant with the given option set id.
		/// </summary>
		/// <param name="optionSetId">Option set to use, either Prefix or Suffix.</param>
		/// <param name="expiration">The time in minutes after which the enchant expires, 0 for none.</param>
		/// <returns></returns>
		public static Item CreateEnchant(int optionSetId, int expiration = 0)
		{
			var optionSetData = AuraData.OptionSetDb.Find(optionSetId);
			if (optionSetData == null)
				throw new ArgumentException("Option set doesn't exist: " + optionSetId);

			bool isSuffix;
			if ((isSuffix = optionSetData.Category != OptionSetCategory.Prefix) && optionSetData.Category != OptionSetCategory.Suffix)
				throw new ArgumentException("Option set is neither prefix nor suffix, use custom enchant instead.");

			var item = new Item(optionSetData.ItemId);
			item.MetaData1.SetInt(isSuffix ? "ENSFIX" : "ENPFIX", optionSetId);
			if (expiration != 0)
				item.MetaData1.SetLong(isSuffix ? "XPRSFX" : "XPRPFX", DateTime.Now.AddMinutes(expiration));

			return item;
		}

		/// <summary>
		/// Returns enchant with the given option set id.
		/// </summary>
		/// <remarks>
		/// This method allows the creation of enchants with a prefix *and*
		/// a suffix.
		/// </remarks>
		/// <param name="itemId">Id of the enchant scoll item, defaults to item id specified for the *fix.</param>
		/// <param name="prefix">The option set to use as prefix, 0 for none.</param>
		/// <param name="suffix">The option set to use as suffix, 0 for none.</param>
		/// <param name="expiration">The time in minutes after which the enchant expires, 0 for none.</param>
		/// <returns></returns>
		public static Item CreateEnchant(int itemId, int prefix, int suffix, int expiration)
		{
			if (prefix == 0 && suffix == 0)
				throw new ArgumentException("Prefix, suffix, or both must be set.");

			OptionSetData prefixData, suffixData;

			// Prefix
			if (prefix != 0)
			{
				prefixData = AuraData.OptionSetDb.Find(prefix);
				if (prefixData == null) throw new ArgumentException("Option set doesn't exist: " + prefix);
				if (prefixData.Category != OptionSetCategory.Prefix) throw new ArgumentException("Option set " + prefix + " is not a prefix.");

				if (itemId == 0)
					itemId = prefixData.ItemId;
			}

			// Suffix
			if (suffix != 0)
			{
				suffixData = AuraData.OptionSetDb.Find(suffix);
				if (suffixData == null) throw new ArgumentException("Option set doesn't exist: " + suffix);
				if (suffixData.Category != OptionSetCategory.Suffix) throw new ArgumentException("Option set " + suffix + " is not a suffix.");

				if (itemId == 0)
					itemId = suffixData.ItemId;
			}

			// Create item
			var item = new Item(itemId);
			if (prefix != 0)
			{
				item.MetaData1.SetInt("ENPFIX", prefix);
				if (expiration != 0)
					item.MetaData1.SetLong("XPRPFX", DateTime.Now.AddMinutes(expiration));
			}
			if (suffix != 0)
			{
				item.MetaData1.SetInt("ENSFIX", suffix);
				if (expiration != 0)
					item.MetaData1.SetLong("XPRSFX", DateTime.Now.AddMinutes(expiration));
			}

			return item;
		}

		/// <summary>
		/// Creates a key.
		/// </summary>
		/// <param name="itemId">Id of the key, e.g. 70028 for Treasure Chest Key.</param>
		/// <param name="lockName">Name of the lock this key is for.</param>
		/// <param name="ownerEntityId">The entity id of the person who can use this key, set to 0 to ignore.</param>
		/// <returns></returns>
		public static Item CreateKey(int itemId, string lockName, long ownerEntityId = 0)
		{
			// 70028 - Treasure Chest Key
			//005 [................] String : prop_to_unlock:s:chest;
			//006 [................] String : AIEXCLR:8:4503599627466431;

			// 70029 - Dungeon Room Key
			// 70030 - Boss Room Key
			//005 [................] String : prop_to_unlock:s:CF273B4974C7C864AFBB2D8C1D86EB9C;
			//006 [................] String : 

			var item = new Item(itemId);
			item.MetaData1.SetString("prop_to_unlock", lockName);
			if (ownerEntityId != 0)
				item.MetaData1.SetLong("AIEXCLR", ownerEntityId);

			return item;
		}

		/// <summary>
		/// Creates a key with a specific color.
		/// </summary>
		/// <param name="itemId">Id of the key, e.g. 70028 for Treasure Chest Key.</param>
		/// <param name="color">Color of the key.</param>
		/// <param name="lockName">Name of the lock this key is for.</param>
		/// <param name="ownerEntityId">The entity id of the person who can use this key, set to 0 to ignore.</param>
		/// <returns></returns>
		public static Item CreateKey(int itemId, uint color, string lockName, long ownerEntityId = 0)
		{
			var item = new Item(itemId);
			item.Info.Color1 = color;
			item.MetaData1.SetString("prop_to_unlock", lockName);
			if (ownerEntityId != 0)
				item.MetaData1.SetLong("AIEXCLR", ownerEntityId);

			return item;
		}

		/// <summary>
		/// Returns new check with the given amount.
		/// </summary>
		/// <param name="amount"></param>
		/// <returns></returns>
		public static Item CreateCheck(int amount)
		{
			var item = new Item(2004);
			item.MetaData1.SetInt("EVALUE", amount);

			return item;
		}

		/// <summary>
		/// Returns wing to the given portal.
		/// </summary>
		/// <param name="itemId"></param>
		/// <param name="portal"></param>
		/// <returns></returns>
		public static Item CreateWarpScroll(int itemId, string portal)
		{
			var item = new Item(itemId);

			if (portal == "last_town")
				item.MetaData1.SetString("TARGET", "last_town");
			else
				item.MetaData1.SetString("TARGET", "portal@{0}", portal);

			return item;
		}

		/// <summary>
		/// Returns wing to the given location.
		/// </summary>
		/// <param name="itemId"></param>
		/// <param name="regionId"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static Item CreateWarpScroll(int itemId, int regionId, int x, int y)
		{
			var item = new Item(itemId);
			item.MetaData1.SetString("TARGET", "pos@{0},{1},{2}", regionId, x, y);

			return item;
		}

		/// <summary>
		/// Creates quest scroll for the given quest id.
		/// </summary>
		/// <remarks>
		/// During the creation, a quest is created and included with the item.
		/// </remarks>
		/// <param name="questId"></param>
		/// <returns></returns>
		public static Item CreateQuestScroll(int questId)
		{
			// Get quest information
			var questScript = ChannelServer.Instance.ScriptManager.QuestScripts.Get(questId);
			if (questScript == null)
				throw new ArgumentException("Quest '" + questId + "' not found.");

			var quest = new Quest(questId);

			var item = new Item(questScript.ScrollId);
			item.MetaData1.Parse(quest.Data.MetaData.ToString());

			item.Quest = quest;
			quest.QuestItem = item;

			return item;
		}

		/// <summary>
		/// Creates a tailoring pattern/blacksmith manual of the specified form ID and number of uses.
		/// </summary>
		/// <remarks>
		/// It is assumed that a single use subtracts 1000 durability from the pattern. 
		/// (i.e. <paramref name="useCount"/> is multiplied by 1000 and applied to pattern durability.)
		/// </remarks>
		/// <param name="itemId"></param>
		/// <param name="formId"></param>
		/// <param name="useCount"></param>
		/// <returns></returns>
		public static Item CreatePattern(int itemId, int formId, int useCount)
		{
			var item = new Item(itemId);
			item.MetaData1.SetInt("FORMID", formId);
			item.OptionInfo.DurabilityMax = useCount * 1000; // DurabilityMax overwritten to avoid clamping.
			item.Durability = item.OptionInfo.DurabilityMax;

			return item;
		}

		/// <summary>
		/// Returns true if item has the given flags.
		/// </summary>
		/// <param name="flags"></param>
		/// <returns></returns>
		public bool Is(ItemFlags flags)
		{
			return (this.OptionInfo.Flags & flags) != 0;
		}

		/// <summary>
		/// Returns a random drop from the given list as item.
		/// </summary>
		/// <param name="rnd"></param>
		/// <param name="drops"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Item GetRandomDrop(Random rnd, IEnumerable<DropData> drops)
		{
			if (drops == null || !drops.Any())
				throw new ArgumentException("Drops list empty.");

			var item = GetRandomDrop(rnd, drops.Sum(a => a.Chance), drops);

			// Give drop with lowest chance if we didn't get one for some
			// reason. (This might happen due to floating point inaccuracy?)
			if (item == null)
				item = new Item(drops.OrderBy(a => a.Chance).First());

			return item;
		}

		/// <summary>
		/// Returns a random drop from the given list as item.
		/// Returns null if total wasn't reached.
		/// </summary>
		/// <param name="rnd"></param>
		/// <param name="total"></param>
		/// <param name="drops"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Item GetRandomDrop(Random rnd, float total, IEnumerable<DropData> drops)
		{
			if (drops == null || !drops.Any())
				throw new ArgumentException("Drops list empty.");

			var num = rnd.NextDouble() * total;

			var n = 0.0;
			DropData data = null;
			foreach (var drop in drops)
			{
				n += drop.Chance;
				if (num <= n)
				{
					data = drop;
					break;
				}
			}

			if (data == null)
				return null;

			return new Item(data);
		}

		/// <summary>
		/// Returns item's position, based on Info.X and Y.
		/// </summary>
		/// <returns></returns>
		public override Position GetPosition()
		{
			return new Position(this.Info.X, this.Info.Y);
		}

		/// <summary>
		/// Modifies position in inventory.
		/// </summary>
		/// <param name="pocket"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void Move(Pocket pocket, int x, int y)
		{
			this.Info.Pocket = pocket;
			this.Info.Region = 0;
			this.Info.X = x;
			this.Info.Y = y;
		}

		/// <summary>
		/// Modifies pocket and entity location in world.
		/// </summary>
		/// <param name="region"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void Move(int region, int x, int y)
		{
			this.Info.Pocket = Pocket.None;
			this.Info.Region = region;
			this.Info.X = x;
			this.Info.Y = y;
		}

		/// <summary>
		/// Sets entity id to a new, unused one.
		/// </summary>
		public void SetNewEntityId()
		{
			this.EntityId = Interlocked.Increment(ref _itemId);
		}

		/// <summary>
		/// Drops item at location.
		/// </summary>
		/// <param name="region">Region to drop the item in.</param>
		/// <param name="pos">
		/// Center point of the drop, which is slightly randomized in this method.
		/// </param>
		/// <param name="radius">
		/// Radius around position where the item may drop.
		/// </param>
		public void Drop(Region region, Position pos, int radius)
		{
			this.Drop(region, pos, radius, null, false);
		}

		/// <summary>
		/// Drops item at location.
		/// </summary>
		/// <param name="region">Region to drop the item in.</param>
		/// <param name="pos">
		/// Center point of the drop, which is slightly randomized in this method.
		/// </param>
		/// <param name="radius">
		/// Radius around position where the item may drop.
		/// </param>
		/// <param name="owner">
		/// The only entity that is allowed to pick up the item for a
		/// certain period of time. Set to null to not protect item from
		/// being picked up.
		/// </param>
		/// <param name="playerDrop">
		/// Whether the item is being dropped by a player, the owner.
		/// If it is, normal items aren't protected.
		/// </param>
		public void Drop(Region region, Position pos, int radius, Creature owner, bool playerDrop)
		{
			var rnd = RandomProvider.Get();

			// Randomize position if radius was specified
			if (radius > 0)
				pos = pos.GetRandomInRange(radius, rnd);

			var x = pos.X;
			var y = pos.Y;

			//this.SetNewEntityId();
			this.Move(region.Id, x, y);

			// Keys don't disappear (?)
			if (!this.HasTag("/key/"))
				this.DisappearTime = DateTime.Now.AddSeconds(Math.Max(60, (this.OptionInfo.Price / 100) * 60));

			// Specify who can pick up the item when
			if (owner != null)
			{
				this.OwnerId = owner.EntityId;

				// Personal items can never be picked up by anyone else
				var isPersonal =
					(this.Data.Action == ItemAction.StaticItem || this.Data.Action == ItemAction.AccountPersonalItem || this.Data.Action == ItemAction.CharacterPersonalItem)
					|| this.Is(ItemFlags.Personalized);

				// Set protection if item wasn't dropped by a player
				// and it's not a dungeon room key
				var standardProtection = (!isPersonal && !playerDrop && !this.IsDungeonRoomKey);

				if (isPersonal)
				{
					this.ProtectionLimit = DateTime.MaxValue;
				}
				else if (standardProtection)
				{
					var seconds = ChannelServer.Instance.Conf.World.LootStealProtection;
					if (seconds > 0)
						this.ProtectionLimit = DateTime.Now.AddSeconds(seconds);
					else
						this.ProtectionLimit = null;
				}
			}
			else
			{
				this.OwnerId = 0;
				this.ProtectionLimit = null;
			}

			// Random direction
			this.Info.FigureC = (byte)rnd.Next(256);

			// Add item to region
			region.AddItem(this);
		}

		/// <summary>
		/// Loads default item information from data.
		/// </summary>
		public void LoadDefault()
		{
			this.Data = AuraData.ItemDb.Find(this.Info.Id);
			if (this.Data != null)
			{
				this.Info.KnockCount = this.Data.KnockCount;
				this.OptionInfo.KnockCount = this.Data.KnockCount;

				this.OptionInfo.Durability = this.Data.Durability;
				this.OptionInfo.DurabilityMax = this.Data.Durability;
				this.OptionInfo.DurabilityOriginal = this.Data.Durability;
				this.OptionInfo.AttackMin = this.Data.AttackMin;
				this.OptionInfo.AttackMax = this.Data.AttackMax;
				this.OptionInfo.InjuryMin = this.Data.InjuryMin;
				this.OptionInfo.InjuryMax = this.Data.InjuryMax;
				this.OptionInfo.Balance = this.Data.Balance;
				this.OptionInfo.Critical = this.Data.Critical;
				this.OptionInfo.Defense = this.Data.Defense;
				this.OptionInfo.Protection = this.Data.Protection;
				this.OptionInfo.Price = this.Data.Price;
				this.OptionInfo.SellingPrice = this.Data.SellingPrice;
				this.OptionInfo.WeaponType = this.Data.WeaponType;
				this.OptionInfo.AttackSpeed = this.Data.AttackSpeed;
				this.OptionInfo.EffectiveRange = this.Data.Range;
				this.OptionInfo.UpgradeMax = (byte)this.Data.MaxUpgrades;

				var rand = RandomProvider.Get();
				this.Info.Color1 = AuraData.ColorMapDb.GetRandom(this.Data.ColorMap1, rand);
				this.Info.Color2 = AuraData.ColorMapDb.GetRandom(this.Data.ColorMap2, rand);
				this.Info.Color3 = AuraData.ColorMapDb.GetRandom(this.Data.ColorMap3, rand);

				if (this.Data.StackType != StackType.Sac && this.Info.Amount < 1)
					this.Info.Amount = 1;
			}
			else
			{
				Log.Warning("Item.LoadDefault: Item '{0}' couldn't be found in database.", this.Info.Id);
			}

			this.OptionInfo.Flags = ItemFlags.Default;
		}

		/// <summary>
		/// Returns repair cost for the given rate.
		/// </summary>
		/// <param name="repairRate">90~100%</param>
		/// <param name="points">Amount of points to repair, set to 0 to calculate the missing durability points.</param>
		/// <returns></returns>
		public int GetRepairCost(int repairRate, int points)
		{
			var price = 0f;
			var rate = 1f;
			var val = 1000000;

			if (points == 0)
				points = (int)Math.Floor((this.OptionInfo.DurabilityMax - this.OptionInfo.Durability) / 1000f);

			if (this.Data.HasTag("/weapon/edged/") ||
				this.Data.HasTag("/weapon/bow01/") ||
				this.Data.HasTag("/weapon/axe/") ||
				this.Data.HasTag("/weapon/bow/") ||
				this.Data.HasTag("/weapon/blunt/") ||
				this.Data.HasTag("/weapon/crossbow/") ||
				this.Data.HasTag("/weapon/wood/") ||
				this.Data.HasTag("/weapon/knuckle/") ||
				this.Data.HasTag("/weapon/atlatl/") ||
				this.Data.HasTag("/weapon/inverse_transmutator/") ||
				this.Data.HasTag("/weapon/cylinder_turret/") ||
				this.Data.HasTag("/weapon/lance/") ||
				this.Data.HasTag("/weapon/scythe/") ||
				this.Data.HasTag("/weapon/pillow/") ||
				this.Data.HasTag("/weapon/handle/") ||
				this.Data.HasTag("/weapon/dreamcatcher/") ||
				this.Data.HasTag("/weapon/gun/")
			)
			{
				switch (repairRate)
				{
					case 090: val = 100; rate = 0.005f; break;
					case 091: val = 150; rate = 0.010f; break;
					case 092: val = 200; rate = 0.015f; break;
					case 093: val = 250; rate = 0.020f; break;
					case 094: val = 300; rate = 0.025f; break;
					case 095: val = 350; rate = 0.050f; break;
					case 096: val = 400; rate = 0.070f; break;
					case 097: val = 450; rate = 0.100f; break;
					case 098: val = 500; rate = 0.130f; break;
					case 099: val = 550; rate = 0.300f; break;
					case 100: val = 700; rate = 1.000f; break;
				}
			}
			else if (
				this.Data.HasTag("/tool/") ||
				this.Data.HasTag("/shield/") ||
				this.Data.HasTag("/heulwen_tool/") ||
				this.Data.HasTag("/thunderstruck_oak_staff/")
			)
			{
				switch (repairRate)
				{
					case 090: val = 100; rate = 0.005f; break;
					case 091: val = 150; rate = 0.010f; break;
					case 092: val = 200; rate = 0.015f; break;
					case 093: val = 250; rate = 0.020f; break;
					case 094: val = 300; rate = 0.025f; break;
					case 095: val = 350; rate = 0.030f; break;
					case 096: val = 400; rate = 0.035f; break;
					case 097: val = 450; rate = 0.050f; break;
					case 098: val = 500; rate = 0.070f; break;
					case 099: val = 550; rate = 0.100f; break;
					case 100: val = 700; rate = 0.140f; break;
				}
			}
			else if (this.Data.HasTag("/weapon/") && (this.Data.HasTag("/wand/") || this.Data.HasTag("/staff/")))
			{
				switch (repairRate)
				{
					case 090: val = 0100; rate = 0.01f; break;
					case 091: val = 0200; rate = 0.02f; break;
					case 092: val = 0300; rate = 0.03f; break;
					case 093: val = 0400; rate = 0.04f; break;
					case 094: val = 0500; rate = 0.05f; break;
					case 095: val = 0600; rate = 0.06f; break;
					case 096: val = 0700; rate = 0.07f; break;
					case 097: val = 0800; rate = 0.08f; break;
					case 098: val = 1000; rate = 0.09f; break;
					case 099: val = 1200; rate = 0.10f; break;
					case 100: val = 1500; rate = 0.15f; break;
				}
			}
			else if (
				this.Data.HasTag("/armor/cloth/") ||
				this.Data.HasTag("/hand/glove/") ||
				this.Data.HasTag("/hand/bracelet/") ||
				this.Data.HasTag("/foot/shoes/") ||
				this.Data.HasTag("/head/headgear/") ||
				this.Data.HasTag("/robe/") ||
				this.Data.HasTag("/agelimit_robe/") ||
				this.Data.HasTag("/agelimit_cloth/") ||
				this.Data.HasTag("/pouch/bag/") ||
				this.Data.HasTag("/agelimit_glove/") ||
				this.Data.HasTag("/agelimit_shoes/") ||
				this.Data.HasTag("/wing/")
			)
			{
				switch (repairRate)
				{
					case 090: val = 100; rate = 0.0005f; break;
					case 091: val = 110; rate = 0.0010f; break;
					case 092: val = 120; rate = 0.0015f; break;
					case 093: val = 130; rate = 0.0020f; break;
					case 094: val = 140; rate = 0.0025f; break;
					case 095: val = 150; rate = 0.0030f; break;
					case 096: val = 160; rate = 0.0035f; break;
					case 097: val = 170; rate = 0.0040f; break;
					case 098: val = 200; rate = 0.0050f; break;
					case 099: val = 300; rate = 0.0060f; break;
					case 100: val = 500; rate = 0.0100f; break;
				}
			}
			else if (
				this.Data.HasTag("/hand/gauntlet/") ||
				this.Data.HasTag("/agelimit_gauntlet/")
			)
			{
				switch (repairRate)
				{
					case 090: val = 0200; rate = 0.0010f; break;
					case 091: val = 0300; rate = 0.0015f; break;
					case 092: val = 0400; rate = 0.0020f; break;
					case 093: val = 0500; rate = 0.0025f; break;
					case 094: val = 0600; rate = 0.0030f; break;
					case 095: val = 0700; rate = 0.0035f; break;
					case 096: val = 0800; rate = 0.0040f; break;
					case 097: val = 0900; rate = 0.0050f; break;
					case 098: val = 1000; rate = 0.0070f; break;
					case 099: val = 1500; rate = 0.0100f; break;
					case 100: val = 2000; rate = 0.0150f; break;
				}
			}
			else if (
				this.Data.HasTag("/foot/armorboots/") ||
				this.Data.HasTag("/head/helmet/") ||
				this.Data.HasTag("/agelimit_armorboots/")
			)
			{
				switch (repairRate)
				{
					case 090: val = 0400; rate = 0.0010f; break;
					case 091: val = 0600; rate = 0.0015f; break;
					case 092: val = 0800; rate = 0.0020f; break;
					case 093: val = 1000; rate = 0.0025f; break;
					case 094: val = 1500; rate = 0.0030f; break;
					case 095: val = 2000; rate = 0.0035f; break;
					case 096: val = 2500; rate = 0.0040f; break;
					case 097: val = 3000; rate = 0.0060f; break;
					case 098: val = 4000; rate = 0.0090f; break;
					case 099: val = 5000; rate = 0.0150f; break;
					case 100: val = 7000; rate = 0.0200f; break;
				}
			}
			else if (this.Data.HasTag("/armor/lightarmor/"))
			{
				switch (repairRate)
				{
					case 090: val = 0200; rate = 0.0005f; break;
					case 091: val = 0220; rate = 0.0010f; break;
					case 092: val = 0240; rate = 0.0015f; break;
					case 093: val = 0260; rate = 0.0020f; break;
					case 094: val = 0280; rate = 0.0025f; break;
					case 095: val = 0300; rate = 0.0030f; break;
					case 096: val = 0320; rate = 0.0035f; break;
					case 097: val = 0340; rate = 0.0040f; break;
					case 098: val = 0400; rate = 0.0050f; break;
					case 099: val = 0600; rate = 0.0060f; break;
					case 100: val = 1000; rate = 0.0100f; break;
				}
			}
			else if (this.Data.HasTag("/armor/heavyarmor/"))
			{
				switch (repairRate)
				{
					case 090: val = 0700; rate = 0.0005f; break;
					case 091: val = 0770; rate = 0.0010f; break;
					case 092: val = 0840; rate = 0.0015f; break;
					case 093: val = 0910; rate = 0.0020f; break;
					case 094: val = 0980; rate = 0.0025f; break;
					case 095: val = 1050; rate = 0.0030f; break;
					case 096: val = 1120; rate = 0.0035f; break;
					case 097: val = 1190; rate = 0.0040f; break;
					case 098: val = 1400; rate = 0.0050f; break;
					case 099: val = 2100; rate = 0.0060f; break;
					case 100: val = 3500; rate = 0.0100f; break;
				}
			}
			else if (this.Data.HasTag("/equip/accessary/") || this.Data.HasTag("/install_instrument/"))
			{
				switch (repairRate)
				{
					case 090: val = 0100; rate = 0.150f; break;
					case 091: val = 0200; rate = 0.152f; break;
					case 092: val = 0300; rate = 0.154f; break;
					case 093: val = 0400; rate = 0.156f; break;
					case 094: val = 0500; rate = 0.158f; break;
					case 095: val = 0600; rate = 0.160f; break;
					case 096: val = 0700; rate = 0.165f; break;
					case 097: val = 0800; rate = 0.170f; break;
					case 098: val = 0900; rate = 0.200f; break;
					case 099: val = 1000; rate = 0.250f; break;
					case 100: val = 1500; rate = 0.300f; break;
				}
			}
			else if (this.Data.HasTag("/falias_treasure/"))
				return 30000;

			price = this.OptionInfo.Price * rate;
			price += (price <= val ? price : val);

			var duraPoints = Math.Max(5, this.OptionInfo.DurabilityOriginal / 1000);

			var result = (int)(5.0 / Math.Sqrt(duraPoints) * price);
			if (result == 0)
				result = 1;

			// Increase for collection upgrades
			var collectionSpeed = this.MetaData1.GetInt("CTSPEED");
			var collectionBonus = this.MetaData1.GetShort("CTBONUS");

			if (collectionSpeed != 0)
			{
				if (collectionSpeed <= 250)
					result = (int)(result * 1.7f);
				else if (collectionSpeed <= 500)
					result = (int)(result * 1.7f * 1.7f);
				else if (collectionSpeed <= 750)
					result = (int)(result * 1.7f * 1.7f * 1.7f);
				else
					result = (int)(result * 1.7f * 1.7f * 1.7f * 1.7f);
			}

			if (collectionSpeed > 1000 || collectionBonus != 0)
			{
				if (this.Info.Id == 40023) // Gathering Knife
					result *= 10;
				else
					result *= 2;
			}

			return result * points;
		}

		/// <summary>
		///  Returns true if item's data has the tag.
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		public override bool HasTag(string tag)
		{
			if (this.Data == null)
				return false;

			return this.Data.HasTag(tag);
		}

		/// <summary>
		/// Removes item from its current region.
		/// </summary>
		public override void Disappear()
		{
			if (this.Region != Region.Limbo)
				this.Region.RemoveItem(this);

			base.Disappear();
		}

		/// <summary>
		/// Adds upgrade effect to item, does not update client.
		/// </summary>
		/// <param name="effect"></param>
		public void AddUpgradeEffect(params UpgradeEffect[] effects)
		{
			lock (_upgrades)
			{
				if (_upgrades.Count + effects.Length > byte.MaxValue)
					throw new ArgumentException("Adding the effects would cause the item to have >255 effects.");

				_upgrades.AddRange(effects);
			}
		}

		/// <summary>
		/// Removes all upgrade effects of the given type.
		/// Does not update client.
		/// </summary>
		/// <param name="type"></param>
		public void RemoveUpgradeEffects(UpgradeType type)
		{
			lock (_upgrades)
				_upgrades.RemoveAll(a => a.Type == type);
		}

		/// <summary>
		/// Returns a new list with the item's upgrade effects.
		/// </summary>
		/// <returns></returns>
		public UpgradeEffect[] GetUpgradeEffects()
		{
			lock (_upgrades)
				return _upgrades.ToArray();
		}

		/// <summary>
		/// Applies all upgrade effects of option set to item.
		/// </summary>
		/// <param name="optionSetData">Option set to apply.</param>
		/// <param name="clear">Remove existing upgrade effects of the same type?</param>
		public void ApplyOptionSet(OptionSetData optionSetData, bool clear)
		{
			var rnd = RandomProvider.Get();

			lock (_upgrades)
			{
				if (clear)
					_upgrades.RemoveAll(a => a.Type == optionSetData.Type);

				foreach (var effect in optionSetData.Effects)
					this.AddUpgradeEffect(effect.GetUpgradeEffect(rnd));
			}
		}

		/// <summary>
		/// Returns all upgrade effects in one base64 string.
		/// </summary>
		/// <remarks>
		/// Structure:
		///   byte length
		///   UpgradeEffect[length] effects
		/// </remarks>
		/// <returns></returns>
		public string SerializeUpgradeEffects()
		{
			lock (_upgrades)
			{
				if (_upgrades.Count == 0)
					return null;

				// Calculate sizes
				var upgradeEffectSize = Marshal.SizeOf(typeof(UpgradeEffect));
				var totalSize = 1 + (upgradeEffectSize * _upgrades.Count);

				// Prepare result
				var result = new byte[totalSize];
				result[0] = (byte)_upgrades.Count;

				var ptr = IntPtr.Zero;
				try
				{
					// Create one buffer to use for all effects
					ptr = Marshal.AllocHGlobal(upgradeEffectSize);
					var effectBuffer = new byte[upgradeEffectSize];

					// Write all effects to result
					for (int i = 0; i < _upgrades.Count; ++i)
					{
						Marshal.StructureToPtr(_upgrades[i], ptr, true);
						Marshal.Copy(ptr, effectBuffer, 0, upgradeEffectSize);
						Buffer.BlockCopy(effectBuffer, 0, result, 1 + upgradeEffectSize * i, upgradeEffectSize);
					}

					// Return result as base64 string
					return Convert.ToBase64String(result);
				}
				finally
				{
					if (ptr != IntPtr.Zero)
						Marshal.FreeHGlobal(ptr);
				}
			}
		}

		/// <summary>
		/// Deserializes given string and overrides upgrade effect list
		/// with the content.
		/// </summary>
		/// <param name="effectsBase64"></param>
		public void DeserializeUpgradeEffects(string effectsBase64)
		{
			if (string.IsNullOrWhiteSpace(effectsBase64))
				return;

			lock (_upgrades)
			{
				// Remove current effects
				_upgrades.Clear();

				// Get data from string
				var data = Convert.FromBase64String(effectsBase64);

				// Calculate sizes
				var upgradeEffectSize = Marshal.SizeOf(typeof(UpgradeEffect));
				var totalSize = 1 + (upgradeEffectSize * data[0]);

				// Check data size
				if (data.Length != totalSize)
					throw new ArgumentException("The struct size in the data doesn't match the current struct.");

				var ptr = IntPtr.Zero;
				try
				{
					// Create one buffer to use for all effects
					ptr = Marshal.AllocHGlobal(upgradeEffectSize);

					// Read all effects from data
					for (int i = 0; i < data[0]; ++i)
					{
						Marshal.Copy(data, 1 + upgradeEffectSize * i, ptr, upgradeEffectSize);
						var val = (UpgradeEffect)Marshal.PtrToStructure(ptr, typeof(UpgradeEffect));
						_upgrades.Add(val);
					}
				}
				finally
				{
					if (ptr != IntPtr.Zero)
						Marshal.FreeHGlobal(ptr);
				}
			}
		}

		/// <summary>
		/// Sets gold price and selling price based on given value.
		/// </summary>
		/// <param name="price"></param>
		public void SetGoldPrice(int price)
		{
			this.OptionInfo.Price = price;
			this.OptionInfo.SellingPrice = (this.Info.Id != 2000 ? (int)(price * 0.1f) : 1000);
		}

		/// <summary>
		/// Sets gold price and selling price to data values.
		/// </summary>
		public void ResetGoldPrice()
		{
			this.OptionInfo.Price = this.Data.Price;
			this.OptionInfo.SellingPrice = this.Data.SellingPrice;
		}

		/// <summary>
		/// Returns whether the given creature can touch and use the item.
		/// </summary>
		/// <remarks>
		/// Example: Sword of Elsinore can only be used while having
		/// Rosemary Gloves equipped.
		/// </remarks>
		/// <example>
		/// string error;
		/// if (!swordOfElsinore.CanBeTouchedBy(creature))
		///     Send.MsgBox(creature, error)
		/// </example>
		/// <param name="creature">The creature to check.</param>
		/// <param name="error">The error message in case the item can't be touched.</param>
		/// <returns></returns>
		public bool CanBeTouchedBy(Creature creature, out string error)
		{
			error = null;

			// Sword of Elsinore
			if (this.HasTag("/hamlets_sword/"))
			{
				var glove = creature.Inventory.GetItemAt(Pocket.Glove, 0, 0);
				if (glove == null || !glove.HasTag("/ophelia_glove/"))
				{
					error = Localization.Get("The sword is too hot to touch.");
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns the amount of proficiency items gain for the given
		/// age and type.
		/// </summary>
		/// <param name="age"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static int GetProficiencyGain(int age, ProficiencyGainType type)
		{
			switch (type)
			{
				case ProficiencyGainType.Melee:
					if (age >= 10 && age <= 12)
						return 48;
					else if (age >= 13 && age <= 19)
						return 60;
					else
						return 72;

				case ProficiencyGainType.Ranged:
					if (age == 10)
						return 60;
					else if (age >= 11 && age <= 15)
						return 72;
					else if (age >= 16 && age <= 19)
						return 84;
					else if (age >= 20 && age <= 24)
						return 96;
					else
						return 108;

				case ProficiencyGainType.Gathering:
					if (age >= 10 && age <= 15)
						return 96;
					else
						return 114;

				case ProficiencyGainType.Music:
					if (age >= 10 && age <= 15)
						return 6;
					else
						return 12;

				case ProficiencyGainType.Damage:
					if (age >= 10 && age <= 12)
						return 16;
					else if (age >= 13 && age <= 19)
						return 20;
					else
						return 24;

				case ProficiencyGainType.Time:
					if (age >= 10 && age <= 12)
						return 60;
					else if (age >= 13 && age <= 19)
						return 75;
					else
						return 90;

				case ProficiencyGainType.Defend:
					if (age >= 10 && age <= 12)
						return 240;
					else if (age >= 13 && age <= 19)
						return 300;
					else
						return 360;

				default:
					throw new ArgumentException("Unknown type '" + type + "'.");
			}
		}

		/// <summary>
		/// Returns collection list, based on meta data "COLLIST", as char
		/// array of "1" and "0" chars, for easy checking and modification.
		/// </summary>
		/// <remarks>
		/// As the 1s and 0s make up bytes, the array is a multitude of 8,
		/// so make sure to check to check the amount of 1s to check for
		/// completion, and not if all are 1, as that's not necessarily
		/// correct.
		/// </remarks>
		/// <returns></returns>
		public char[] GetCollectionList()
		{
			// We could've tried to use longs, or bool arrays, but really,
			// char arrays and strings offer the easiest conversion and
			// access.

			if (this.Data.CollectionMax == 0)
				throw new InvalidOperationException("Item is not a collection book.");

			string result;

			if (!this.MetaData1.Has("COLLIST"))
			{
				var max = this.Data.CollectionMax;
				var multiple = 8;
				var val = max + multiple - 1;

				result = "".PadLeft((val - (val % multiple)), '0');
			}
			else
			{
				result = "";

				var bin = this.MetaData1.GetBin("COLLIST");
				for (int i = 0; i < bin.Length; ++i)
				{
					var add = (ulong)(bin[i] << (i * 8));
					result += Convert.ToString(bin[i], 2).PadLeft(8, '0');
				}
			}

			return result.ToCharArray();
		}

		/// <summary>
		/// Sets collection list meta data "COLLIST", based on list.
		/// </summary>
		/// <param name="list"></param>
		/// <example>
		/// item.SetCollectionList("10000000".ToCharArray());
		/// = COLLIST : gAAA
		/// = Collected first item
		/// 
		/// item.SetCollectionList("11000000".ToCharArray());
		/// = COLLIST : wAAA
		/// = Collected first and second item
		/// </example>
		public void SetCollectionList(char[] list)
		{
			if (list == null)
				throw new ArgumentNullException("list");

			if (list.Length % 8 != 0)
				throw new ArgumentException("Invalid amount of bits.");

			if (this.Data.CollectionMax == 0)
				throw new InvalidOperationException("Item is not a collection book.");

			var collectionStr = new string(list);
			var byteCount = list.Length / 8;
			var newCollectionList = new byte[byteCount];
			for (int i = 0; i < byteCount; ++i)
				newCollectionList[i] = Convert.ToByte(collectionStr.Substring(i * 8, 8), 2);

			this.MetaData1.SetBin("COLLIST", newCollectionList);
		}

		/// Modifies equip stats. Run one for every dropped item.
		/// </summary>
		/// <param name="rnd"></param>
		public void ModifyEquipStats(Random rnd)
		{
			var item = this;

			// Equip stat modification
			// http://wiki.mabinogiworld.com/view/Category:Weapons
			if (item.HasTag("/righthand/weapon/|/twohand/weapon/"))
			{
				var num = rnd.Next(100);

				// Durability
				if (num == 0)
					item.OptionInfo.DurabilityMax += 4000;
				else if (num <= 5)
					item.OptionInfo.DurabilityMax += 3000;
				else if (num <= 10)
					item.OptionInfo.DurabilityMax += 2000;
				else if (num <= 25)
					item.OptionInfo.DurabilityMax += 1000;

				// Attack
				if (num == 0)
				{
					item.OptionInfo.AttackMin += 3;
					item.OptionInfo.AttackMax += 3;
				}
				else if (num <= 30)
				{
					item.OptionInfo.AttackMin += 2;
					item.OptionInfo.AttackMax += 2;
				}
				else if (num <= 60)
				{
					item.OptionInfo.AttackMin += 1;
					item.OptionInfo.AttackMax += 1;
				}

				// Crit
				if (num == 0)
					item.OptionInfo.Critical += 3;
				else if (num <= 30)
					item.OptionInfo.Critical += 2;
				else if (num <= 60)
					item.OptionInfo.Critical += 1;

				// Balance
				if (num == 0)
					item.OptionInfo.Balance = (byte)Math.Max(0, item.OptionInfo.Balance - 12);
				else if (num <= 10)
					item.OptionInfo.Balance = (byte)Math.Max(0, item.OptionInfo.Balance - 10);
				else if (num <= 30)
					item.OptionInfo.Balance = (byte)Math.Max(0, item.OptionInfo.Balance - 8);
				else if (num <= 50)
					item.OptionInfo.Balance = (byte)Math.Max(0, item.OptionInfo.Balance - 6);
				else if (num <= 70)
					item.OptionInfo.Balance = (byte)Math.Max(0, item.OptionInfo.Balance - 4);
				else if (num <= 90)
					item.OptionInfo.Balance = (byte)Math.Max(0, item.OptionInfo.Balance - 2);
			}
		}

		/// <summary>
		/// Returns item's selling price based on the specified price and
		/// its properties, including stackability and amount.
		/// </summary>
		/// <returns></returns>
		public int GetSellingPrice()
		{
			var sellingPrice = 0;

			if (!this.IsIncomplete)
			{
				sellingPrice = this.OptionInfo.SellingPrice;

				if (this.Data.StackType == StackType.Sac)
				{
					// Add costs of the items inside the sac
					sellingPrice += (int)((this.Info.Amount / (float)this.Data.StackItem.StackMax) * this.Data.StackItem.SellingPrice);
				}
				else if (this.Data.StackType == StackType.Stackable)
				{
					// Individuel price for this stack
					sellingPrice = (int)((this.Amount / (float)this.Data.StackMax) * sellingPrice);
				}
			}

			return sellingPrice;
		}
	}

	public enum ProficiencyGainType
	{
		Melee,
		Ranged,
		Gathering,
		Music,
		Damage,
		Time,
		Defend,
	}
}
