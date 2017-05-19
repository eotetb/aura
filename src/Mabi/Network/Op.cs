// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System.Reflection;
namespace Aura.Mabi.Network
{
	/// <summary>
	/// All Op codes
	/// </summary>
	public static class Op
	{
		// Login Server
		// ------------------------------------------------------------------
		public const int ClientIdent = 0x0FD1020A;
		public const int ClientIdentR = 0x1F;
		public const int Login = 0x0FD12002;
		public const int LoginR = 0x23;
		public const int ChannelStatus = 0x26;
		public const int CharacterInfoRequest = 0x29;
		public const int CharacterInfoRequestR = 0x2A;
		public const int CreateCharacter = 0x2B;
		public const int CreateCharacterR = 0x2C;
		public const int DeleteCharacterRequest = 0x2D;
		public const int DeleteCharacterRequestR = 0x2E;
		public const int ChannelInfoRequest = 0x2F;
		public const int ChannelInfoRequestR = 0x30;
		public const int DeleteCharacter = 0x35;
		public const int DeleteCharacterR = 0x36;
		public const int RecoverCharacter = 0x37;
		public const int RecoverCharacterR = 0x38;
		public const int NameCheck = 0x39;
		public const int NameCheckR = 0x3A;
		public const int PetInfoRequest = 0x3B;
		public const int PetInfoRequestR = 0x3C;
		public const int CreatePet = 0x3D;
		public const int CreatePetR = 0x3E;
		public const int DeletePetRequest = 0x3F;
		public const int DeletePetRequestR = 0x40;
		public const int DeletePet = 0x41;
		public const int DeletePetR = 0x42;
		public const int RecoverPet = 0x43;
		public const int RecoverPetR = 0x44;
		public const int CreatePartner = 0x45;
		public const int CreatePartnerR = 0x46;
		public const int AccountInfoRequest = 0x47;
		public const int AccountInfoRequestR = 0x48;
		public const int AcceptGift = 0x49;
		public const int AcceptGiftR = 0x4A;
		public const int RefuseGift = 0x4B;
		public const int RefuseGiftR = 0x4C;
		public const int DisconnectInform = 0x4D;
		public const int PetCreationOptionsRequest = 0x50;
		public const int PetCreationOptionsRequestR = 0x51;
		public const int LoginRedirect = 0x54;
		public const int PartnerCreationOptionsRequest = 0x55;
		public const int PartnerCreationOptionsRequestR = 0x56;
		public const int LoginUnk = 0x5A;  // Sent on login
		public const int LoginUnkR = 0x5B; // ^ Response, only known parameter: 0 byte.
		public const int TradeCard = 0x5C;
		public const int TradeCardR = 0x5D;

		// Channel Server
		// ------------------------------------------------------------------
		public const int ChannelLogin = 0x4E22;
		public const int ChannelLoginR = 0x4E23;
		public const int DisconnectRequest = 0x4E24;
		public const int DisconnectRequestR = 0x4E25;
		public const int RequestClientDisconnect = 0x4E26;
		public const int RequestSecondaryLogin = 0x4E29;
		public const int Disappear = 0x4E2A;
		public const int SwitchChannel = 0x4E32;
		public const int SwitchChannelR = 0x4E33;
		public const int GetChannelList = 0x4E34;
		public const int GetChannelListR = 0x4E35;
		public const int WarpUnk1 = 0x4E39;
		public const int RequestRebirth = 0x4E84;
		public const int RequestRebirthR = 0x4E85;

		// PonsUpdate changed by +1 some time.

		public const int PointsUpdate = 0x4E90;

		public const int ChannelCharacterInfoRequest = 0x5208;
		public const int ChannelCharacterInfoRequestR = 0x5209;
		public const int EntityAppears = 0x520C;
		public const int EntityDisappears = 0x520D;
		public const int CreatureBodyUpdate = 0x520E;
		public const int CreatureFaceUpdate = 0x5210;
		public const int ItemAppears = 0x5211;
		public const int ItemDisappears = 0x5212;
		public const int AssignSittingProp = 0x5215;
		public const int Chat = 0x526C;
		public const int Notice = 0x526D;
		public const int WarpUnk2 = 0x526E;
		public const int MsgBox = 0x526F;
		public const int AcquireInfo = 0x5271;
		public const int WhisperChat = 0x5273;
		public const int BeginnerChat = 0x5275;
		public const int AcquireInfo2 = 0x5278; // ?
		public const int VisualChat = 0x527A;
		public const int PropAppears = 0x52D0;
		public const int PropDisappears = 0x52D1;
		public const int PropUpdate = 0x52D2; // Doors, MGs?
		public const int EntitiesAppear = 0x5334;
		public const int EntitiesDisappear = 0x5335;
		public const int RemoveDeathScreen = 0x53FD;
		public const int IsNowDead = 0x53FC;
		public const int Revive = 0x53FE;
		public const int Revived = 0x53FF;
		public const int DeadMenu = 0x5401;
		public const int DeadMenuR = 0x5402;
		public const int DeadFeather = 0x5403;

		// [190200, NA217 (2015-12-16)]
		// One op was added here, which shifted UseGesture and UseGestureR
		// by one. DeadMenu and NPCs were working fine, it seemingly didn't
		// affect anything else.

		public const int UseGesture = 0x540F;
		public const int UseGestureR = 0x5410;

		public const int IncompatibleUnk = 0x5411;
		public const int NpcTalkStart = 0x55F0;
		public const int NpcTalkStartR = 0x55F1;
		public const int NpcTalkEnd = 0x55F2;
		public const int NpcTalkEndR = 0x55F3;
		public const int NpcTalkEgo = 0x55F4;
		public const int NpcTalkEgoR = 0x55F5;
		public const int NpcInitiateDialog = 0x55F7;
		public const int NpcTalkPartner = 0x55F8;
		public const int NpcTalkPartnerR = 0x55F9;
		public const int ItemBlessed = 0x5BD8;
		public const int ItemMove = 0x59D8;
		public const int ItemMoveR = 0x59D9;
		public const int ItemPickUp = 0x59DA;
		public const int ItemPickUpR = 0x59DB;
		public const int ItemDrop = 0x59DC;
		public const int ItemDropR = 0x59DD;
		public const int ItemMoveInfo = 0x59DE;
		public const int ItemSwitchInfo = 0x59DF;
		public const int ItemNew = 0x59E0;
		public const int ItemRemove = 0x59E1;
		public const int ItemDestroy = 0x59E2;
		public const int ItemDestroyR = 0x59E4;
		public const int EquipmentChanged = 0x59E6;
		public const int EquipmentMoved = 0x59E7;
		public const int ItemSplit = 0x59E8;
		public const int ItemSplitR = 0x59E9;
		public const int ItemAmount = 0x59EA;
		public const int UseItem = 0x59EB;
		public const int UseItemR = 0x59EC;
		public const int GiftItem = 0x59EF;
		public const int GiftItemR = 0x59F0;
		public const int BurnItem = 0x59F2;
		public const int BurnItemR = 0x59F3;
		public const int UnequipBag = 0x59F4;
		public const int UnequipBagR = 0x59F5;

		// [180300, NA166 (18.09.2013)] 2 new ops

		public const int CollectionAddItem = 0x59F6;
		public const int CollectionAddItemR = 0x59F7;
		public const int CollectionGetReward = 0x59F8;
		public const int CollectionGetRewardR = 0x59F9;

		// Does this actually have to do something with Npcs? Sent by the
		// server when selecting "@end", before the actual NpcTalkEnd
		public const int NpcTalkSelectEnd = 0x59FB;

		public const int SwitchSet = 0x5BCD;
		public const int SwitchSetR = 0x5BCE;
		public const int UpdateWeaponSet = 0x5BCF;
		public const int ItemStateChange = 0x5BD0;
		public const int ItemStateChangeR = 0x5BD1;
		public const int ItemUpdate = 0x5BD4;
		public const int ItemDurabilityUpdate = 0x5BD5;
		public const int ItemMaxDurabilityUpdate = 0x5BD6;
		public const int ItemStateChanged = 0x5BD9;
		public const int ItemExpUpdate = 0x5BDA;
		public const int ItemRepairResult = 0x5BDB;
		public const int ItemUpgradeResult = 0x5BDC;
		public const int ViewEquipment = 0x5BDF;
		public const int ViewEquipmentR = 0x5BE0;
		public const int OptionSet = 0x5BE7;
		public const int OptionSetR = 0x5BE8;
		public const int UpdateServiceExpiration = 0x5BE9; // Used to set style tab expiration
		public const int AddKeyword = 0x5DC1;
		public const int RemoveKeyword = 0x5DC3;
		public const int NpcTalkKeyword = 0x5DC4;
		public const int NpcTalkKeywordR = 0x5DC5;

		public const int AddObserverRequest = 0x61A8;
		public const int SetLocation = 0x6594;
		public const int TurnTo = 0x6596;
		public const int EnterRegion = 0x6597;
		public const int EnterRegionRequest = 0x6598;
		public const int WarpRegion = 0x6599; // on warp
		public const int ForceRunTo = 0x659A;
		public const int ForceWalkTo = 0x659B;
		public const int EnterRegionRequestR = 0x659C; // on login
		public const int UrlUpdateChronicle = 0x65A2;
		public const int UrlUpdateAdvertise = 0x65A3;
		public const int UrlUpdateGuestbook = 0x65A4;
		public const int UrlUpdatePvp = 0x65A5;
		public const int UrlUpdateDungeonBoard = 0x65A6;
		public const int TakeOff = 0x65A8;
		public const int TakingOff = 0x65A9;
		public const int TakeOffR = 0x65AA;
		public const int FlyTo = 0x65AE;
		public const int FlyingTo = 0x65AF;
		public const int Land = 0x65AB;
		public const int Landing = 0x65AC;
		public const int CanLand = 0x65AD;

		// SawItemNotification increased by one some time between NA200 and NA204

		public const int SawItemNotification = 0x65D8; // [190100, NA200 (2015-01-15)]

		public const int UnkCutsceneEnd = 0x65DC; // Relatively new? (NA204)

		// [200100, NA229 (2016-06-16)]
		public const int RequestNpcNames = 0x65EE;
		public const int RequestNpcNamesR = 0x65EF;
		public const int SearchNpcName = 0x65F0;

		public const int SkillInfo = 0x6979;
		public const int SkillTrainingUp = 0x697C;
		public const int SkillAdvance = 0x697E;
		public const int SkillRankUp = 0x697F; // Response to Advance
		public const int SkillPrepare = 0x6982;
		public const int SkillReady = 0x6983;
		public const int SkillUse = 0x6986;
		public const int SkillComplete = 0x6987;
		public const int SkillCompleteUnk = 0x6988; // Used in gathering fail and as SkillComplete for Ranged on Aura?
		public const int SkillCancel = 0x6989;
		public const int SkillStart = 0x698A;
		public const int SkillStop = 0x698B;
		public const int SkillPrepareSilentCancel = 0x698C;
		public const int SkillUseSilentCancel = 0x698D;
		//public const int ? = 0x698E; // no parameters, found after a Complete/Cancel with one of the special horses
		public const int SkillStartSilentCancel = 0x698F;
		public const int SkillStopSilentCancel = 0x6990;
		public const int SkillStackSet = 0x6991;
		public const int SkillStackUpdate = 0x6992;
		public const int TailoringMiniGame = 0x6997;
		public const int BlacksmithingMiniGame = 0x6998;
		public const int FishingActionRequired = 0x699A;
		public const int FishingAction = 0x699B;
		public const int ProductionSuccessRequest = 0x699E;
		public const int ProductionSuccessRequestR = 0x699F;
		public const int ResetCooldown = 0x69A7;
		public const int UseMotion = 0x6D62;
		public const int PlayAnimation = 0x6D63; // s:data/.../anim/..., 1:0, 2:0, 1:0
		public const int CancelMotion = 0x6D65;
		public const int MotionCancel2 = 0x6D66; // Delayed?
		public const int SetStandStyle = 0x6D68;
		public const int LevelUp = 0x6D69;
		public const int RankUp = 0x6D6A;
		public const int SitDown = 0x6D6C;
		public const int StandUp = 0x6D6D;
		public const int ArenaHideOn = 0x6D6F;
		public const int ArenaHideOff = 0x6D70;
		public const int SetStandStyleTalking = 0x6D79;
		public const int UnkKnockBack = 0x6D7D;
		public const int ChangeStanceRequest = 0x6E28;
		public const int ChangeStanceRequestR = 0x6E29;
		public const int ChangeStance = 0x6E2A;

		public const int RiseFromTheDead = 0x701D;
		public const int CharacterLock = 0x701E;
		public const int CharacterUnlock = 0x701F;
		public const int CharacterLockUpdate = 0x7020; // ?
		public const int PlayDead = 0x7021;
		public const int OpenUmbrella = 0x7025;
		public const int CloseUmbrella = 0x7026;
		public const int SpreadWingsOn = 0x702E;
		public const int SpreadWingsOff = 0x702F;
		public const int NpcShopBuyItem = 0x7150;
		public const int NpcShopBuyItemR = 0x7151;
		public const int NpcShopSellItem = 0x7152;
		public const int NpcShopSellItemR = 0x7153;
		public const int ClearNpcShop = 0x7158; // Empties tabs
		public const int AddToNpcShop = 0x7159; // Adds items while shop is open, works like open
		public const int CheckDirectBankSelling = 0x715A;
		public const int CheckDirectBankSellingR = 0x715B;
		public const int OpenShopRemotely = 0x715C; // ?
		public const int OpenShopRemotelyR = 0x715D; // ?
		public const int OpenNpcShop = 0x715E;
		public const int RequestBankTabs = 0x7211;
		public const int OpenBank = 0x7212;
		public const int CloseBank = 0x7215;
		public const int CloseBankR = 0x7216;
		public const int BankWithdrawItem = 0x7217;
		public const int BankWithdrawItemR = 0x7218;
		public const int BankDepositItem = 0x7219;
		public const int BankDepositItemR = 0x721A;
		public const int BankDepositGold = 0x721B;
		public const int BankDepositGoldR = 0x721C;
		public const int BankWithdrawGold = 0x721D;
		public const int BankWithdrawGoldR = 0x721E;
		public const int BankUpdateGold = 0x721F;
		public const int BankAddItem = 0x7220;
		public const int BankRemoveItem = 0x7221;
		public const int BankTransferInquiry = 0x7222;
		public const int BankTransferRequest = 0x7223;
		public const int BankTransferRequestR = 0x7224;
		public const int BankLicenseFeeInquiry = 0x7225;
		public const int BankPostLicenseInquiryDeposit = 0x7226;
		public const int BankPostLicenseInquiryDepositR = 0x7227;
		public const int BankTransferInfo = 0x7228;
		public const int OpenMail = 0x7242;
		public const int CloseMail = 0x7243;
		public const int ConfirmMailRecipent = 0x7244;
		public const int ConfirmMailRecipentR = 0x7245;
		public const int SendMail = 0x7246;
		public const int SendMailR = 0x7247;
		public const int GetMails = 0x7248;
		public const int GetMailsR = 0x7249;
		public const int MarkMailRead = 0x724A;
		public const int MarkMailReadR = 0x724B;
		public const int RecieveMailItem = 0x724C;
		public const int ReceiveMailItemR = 0x724D;
		public const int ReturnMail = 0x724E;
		public const int ReturnMailR = 0x724F;
		public const int DeleteMail = 0x7250;
		public const int DeleteMailR = 0x7251;
		public const int RecallMail = 0x7252;
		public const int RecallMailR = 0x7253;
		public const int UnreadMailCount = 0x7255;
		public const int PersonalShopCheck = 0x73A1;
		public const int PersonalShopCheckR = 0x73A2;
		public const int PersonalShopTakeDown = 0x73A3;
		public const int PersonalShopTakeDownR = 0x73A4;
		public const int PersonalShopSetUp = 0x73A5;
		public const int PersonalShopSetUpR = 0x73A6;
		public const int PersonalShopChangeTitle = 0x73A7;
		public const int PersonalShopChangeDescription = 0x73A8;
		public const int PersonalShopSetPrice = 0x73A9;
		public const int PersonalShopSetPriceR = 0x73AA;
		public const int PersonalShopPriceUpdate = 0x73AB;
		public const int PersonalShopPetProtectRequest = 0x73AF;
		public const int PersonalShopPetProtectRequestR = 0x73B0;
		public const int PersonalShopPetProtectEndRequest = 0x73B1;
		public const int PersonalShopPetProtectEndRequestR = 0x73B2;
		public const int PersonalShopOpen = 0x7405;
		public const int PersonalShopOpenR = 0x7406;
		public const int PersonalShopClose = 0x7407;
		public const int PersonalShopCloseR = 0x7408;
		public const int PersonalShopBuy = 0x7409;
		public const int PersonalShopBuyR = 0x740A;
		public const int PersonalShopAddItem = 0x740B;
		public const int PersonalShopRemoveItem = 0x740C;
		public const int PersonalShopCustomerPriceUpdate = 0x740D;
		public const int PersonalShopUpdateDescription = 0x740E;
		public const int PersonalShopCloseWindow = 0x740F;
		public const int Entrustment = 0x7469;
		public const int EntrustmentR = 0x746A;
		public const int EntrustmentRequest = 0x746B;
		public const int EntrustmentEnableRequest = 0x746C;
		public const int EntrustmentDisableRequest = 0x746D;
		public const int EntrustmentFinalizeRequest = 0x746E;
		public const int EntrustmentFinalizeRequestR = 0x746F;
		public const int EntrustmentRequestFinalized = 0x7470;
		public const int EntrustmentCancel = 0x7471;
		public const int EntrustmentRefuse = 0x7473;
		public const int EntrustmentClose = 0x7475;
		public const int EntrustmentEnd = 0x7476;
		public const int EntrustmentAcceptRequest = 0x7477;
		public const int EntrustmentAcceptRequestR = 0x7478;
		public const int EntrustmentFinalizing = 0x7479;
		public const int EntrustmentChanceUpdate = 0x747A;
		public const int EntrustmentAddItem = 0x747B;
		public const int EntrustmentRemoveItem = 0x747C;
		public const int EntrustmentSetMaterial = 0x747D;
		public const int StatUpdatePrivate = 0x7530;
		public const int StatUpdatePublic = 0x7532;
		public const int CombatTargetUpdate = 0x791A; // ?
		public const int UnkCombat = 0x791B; // ?
		public const int UnkCombatR = 0x791C; // ?
		public const int CombatSetAim = 0x791D;
		public const int CombatSetAimR = 0x791E;
		public const int CombatSetAim2 = 0x791F;
		public const int SetCombatTarget = 0x7920;
		public const int SetFinisher = 0x7921;
		public const int SetFinisher2 = 0x7922;
		public const int CombatAction = 0x7924;
		public const int CombatActionEnd = 0x7925;
		public const int CombatActionPack = 0x7926;
		public const int CombatUsedSkill = 0x7927;
		public const int CombatAttackR = 0x7D01;
		public const int TouchMimic = 0x7D03;
		public const int TouchMimicR = 0x7D04;

		public const int EventInform = 0x88B8;
		public const int NewQuest = 0x8CA0;
		public const int QuestClear = 0x8CA1;
		public const int QuestUpdate = 0x8CA2;
		public const int CompleteQuest = 0x8CA3;
		public const int CompleteQuestR = 0x8CA4;
		public const int GiveUpQuest = 0x8CA5;
		public const int GiveUpQuestR = 0x8CA6;
		public const int SquadUnk = 0x8D6E;
		public const int SquadUnkR = 0x8D6F;
		public const int QuestStartPtj = 0x8D68;
		public const int QuestEndPtj = 0x8D69;
		public const int QuestUpdatePtj = 0x8D6A;
		public const int PartyCreate = 0x8E94;
		public const int PartyCreateR = 0x8E95;
		public const int PartyCreateUpdate = 0x8E96;
		public const int PartyJoin = 0x8E97;
		public const int PartyJoinR = 0x8E98;
		public const int PartyJoinUpdate = 0x8E99;
		public const int PartyLeave = 0x8E9A;
		public const int PartyLeaveR = 0x8E9B;
		public const int PartyLeaveUpdate = 0x8E9C;
		public const int PartyRemove = 0x8E9D;
		public const int PartyRemoveR = 0x8E9E;
		public const int PartyRemoved = 0x8E9F;
		public const int PartyChangeSetting = 0x8EA0;
		public const int PartyChangeSettingR = 0x8EA1;
		public const int PartySettingUpdate = 0x8EA2;
		public const int PartyChangePassword = 0x8EA3;
		public const int PartyChangePasswordR = 0x8EA4;
		public const int PartyChangeLeader = 0x8EA5;
		public const int PartyChangeLeaderR = 0x8EA6;
		public const int PartyChangeLeaderUpdate = 0x8EA7;
		public const int PartyChat = 0x8EA8;
		public const int PartyWantedShow = 0x8EA9;
		public const int PartyWantedShowR = 0x8EAA;
		public const int PartyWantedOpened = 0x8EAB;
		public const int PartyWantedHide = 0x8EAC;
		public const int PartyWantedHideR = 0x8EAD;
		public const int PartyWantedClosed = 0x8EAE;
		public const int PartySetQuest = 0x8EAF;
		public const int PartySetQuestR = 0x8EB0;
		public const int PartySetActiveQuest = 0x8EB1;
		public const int PartyUnsetQuest = 0x8EB2;
		public const int PartyUnsetQuestR = 0x8EB3;
		public const int PartyUnsetActiveQuest = 0x8EB4;
		public const int PartyChangeFinish = 0x8EB5;
		public const int PartyChangeFinishR = 0x8EB6;
		public const int PartyFinishUpdate = 0x8EB7;
		public const int PartyChangeExp = 0x8EB8;
		public const int PartyChangeExpR = 0x8EB9;
		public const int PartyExpUpdate = 0x8EBA;
		public const int PartyBoardRequest = 0x8EBD;
		public const int PartyBoardRequestR = 0x8EBE;
		public const int GuildCreateRequest = 0x8EF8;
		public const int GuildCreateRequestR = 0x8EF9;
		public const int GuildInfoNoGuild = 0x8EFB;
		public const int GuildPanel = 0x8EFC;
		public const int GuildInfo = 0x8EFD;
		public const int GuildInfoApplied = 0x8EFE;
		public const int GuildApply = 0x8EFF;
		public const int GuildApplyR = 0x8F00;
		public const int GuildUpdateMember = 0x8F01;
		public const int GuildStoneLocation = 0x8F02;
		public const int GuildConvertPlayPoints = 0x8F03;
		public const int GuildConvertPlayPointsR = 0x8F04;
		public const int GuildConvertPlayPointsConfirm = 0x8F05;
		public const int GuildConvertPlayPointsConfirmR = 0x8F06;
		public const int GuildDonate = 0x8F07;
		public const int GuildDonateR = 0x8F08;
		public const int GuildCheckName = 0x8F0B;
		public const int GuildCheckNameR = 0x8F0C;
		public const int GuildMessage = 0x8F0F;
		public const int GuildNameAgreeRequest = 0x8F11;
		public const int GuildNameVote = 0x8F12;
		public const int GuildCreationConfirmRequest = 0x8F13;
		public const int GuildCreationConfirmation = 0x8F14;
		public const int GuildDestroyStone = 0x8F15;
		public const int GuildGoldUpdate = 0x8F16;
		public const int GuildWithdrawGold = 0x8F17;
		public const int GuildWithdrawGoldR = 0x8F18;
		public const int GuildOpenGuildRobeCreation = 0x8F19;
		public const int GuildCreateGuildRobe = 0x8F1A;
		public const int GuildCreateGuildRobeR = 0x8F1B;
		public const int GuildCreateGuildRobeUpdate = 0x8F1C;
		public const int GuildInvite = 0x8F1E;
		public const int GuildListJoinRequest = 0x8F20;
		public const int GuildPermitCheck = 0x8F2A;
		public const int GuildPermitCheckR = 0x8F2B;
		public const int TradeStart = 0x8F5C;
		public const int TradeInfo = 0x8F5D;
		public const int TradeStartR = 0x8F5E;
		public const int TradeRequest = 0x8F5F;
		public const int TradeCancel = 0x8F60;
		public const int TradeCancelR = 0x8F61;
		public const int TradeRequestCanceled = 0x8F62; // ?
		public const int TradeComplete = 0x8F63; // ?
		public const int TradeWait = 0x8F64;
		public const int TradeAcceptRequest = 0x8F65;
		public const int TradeAcceptRequestR = 0x8F66;
		public const int TradePartnerInfo = 0x8F67;
		public const int TradeReady = 0x8F68;
		public const int TradeReadyR = 0x8F69;
		public const int TradeReadied = 0x8F6A;
		public const int TradeItemAdded = 0x8F6E;
		public const int TradeItemRemoved = 0x8F6F;
		public const int TradeExpandWindow = 0x8F70;
		public const int AddTitleKnowledge = 0x8FC0;
		public const int AddTitle = 0x8FC1;
		public const int ChangeTitle = 0x8FC4;
		public const int TitleUpdate = 0x8FC5;
		public const int ChangeTitleR = 0x8FC6;

		public const int PetRegister = 0x9024;
		public const int PetUnregister = 0x9025;
		public const int StartRP = 0x902A;
		public const int EndRP = 0x902B;
		public const int SummonPet = 0x902C;
		public const int SummonPetR = 0x902D;
		public const int PersonalShopPetProtectStart = 0x902F;
		public const int PersonalShopPetProtectStop = 0x9030;

		// [200200, NA246 (2017-02-16)] UnsummonPet ~ TakeItemFromPetInvR shifted by +1.
		public const int UnsummonPet = 0x9032;
		public const int UnsummonPetR = 0x9033;
		public const int TelePet = 0x9034;
		public const int TelePetR = 0x9035;
		public const int PutItemIntoPetInv = 0x9036;
		public const int PutItemIntoPetInvR = 0x9037;
		public const int TakeItemFromPetInv = 0x9038;
		public const int TakeItemFromPetInvR = 0x9039;

		public const int HitProp = 0x9088;
		public const int HitPropR = 0x9089;
		public const int HittingProp = 0x908A;
		public const int TouchProp = 0x908B;
		public const int TouchPropR = 0x908C;
		public const int AddPropExtension = 0x908D;
		public const int RemovePropExtension = 0x908E;
		public const int PlaySound = 0x908F;

		// [190100, NA198 (11.12.2014)] Something added here?
		// Effect~NaoRevivalEntrance definitely shifted by 1,
		// prop hitting was still the same.

		public const int Effect = 0x9091;
		public const int EffectDelayed = 0x9092;
		public const int QuestOwlComplete = 0x9094;
		public const int QuestOwlNew = 0x9095;
		public const int PartyWantedUpdate = 0x9096;
		public const int PvPInformation = 0x9097;
		public const int NaoRevivalExit = 0x9099;
		public const int NaoRevivalEntrance = 0x909D;

		// [190100, NA198 (11.12.2014)] End of above's shift?

		public const int DungeonInfo = 0x9470;
		public const int EnterDynamicRegion = 0x9571; // Creates one dynamic region and warps there
		public const int RemoveDynamicRegion = 0x9572;
		public const int Inquiry = 0x9664;
		public const int InquiryResponse = 0x9665;
		public const int InquiryResponseR = 0x9666;
		public const int ArenaRoundInfo = 0x9667;
		public const int ArenaRoundInfoCancel = 0x9668;
		public const int DressingRoomOpen = 0x96C9;
		public const int DressingRoomOpenR = 0x96CA;
		public const int DressingRoomClose = 0x96CB;
		public const int DressingRoomCloseR = 0x96CC;
		public const int PurchaseMerchandise = 0x96D5;
		public const int PurchaseMerchandiseR = 0x96D6;
		public const int AgeUpEffect = 0x9858;

		public const int ConditionUpdate = 0xA028;
		public const int CollectAnimation = 0xA415;
		public const int CollectAnimationCancel = 0xA416;
		public const int SkillPrepareCancellation = 0xA417;
		public const int DyePaletteReq = 0xA418;
		public const int DyePaletteReqR = 0xA419;
		public const int DyePickColor = 0xA41A;
		public const int DyePickColorR = 0xA41B;
		public const int Transformation = 0xA41C;
		public const int PetAction = 0xA41D;
		public const int SharpMind = 0xA41E;
		public const int MoonGateInfoRequest = 0xA428;
		public const int MoonGateInfoRequestR = 0xA429;
		public const int MoonGateMap = 0xA42D;
		public const int MoonGateUse = 0xA42E;
		public const int MoonGateUseR = 0xA42F;
		public const int MusicEventInform = 0xA431;
		public const int ItemShopInfo = 0xA436;
		public const int PartyWindowUpdate = 0xA43C;
		public const int ContinentWarpCoolDown = 0xA43D;
		public const int ContinentWarpCoolDownR = 0xA43E;
		public const int PersonalShopUpdateBrownie = 0xA44A;
		public const int PartyTypeUpdate = 0xA44B;
		public const int OpenItemShop = 0xA44D;
		public const int OpenItemShopR = 0xA44E;

		// [150000~180000] Something was removed here

		public const int UnkOrdinaryChest = 0xA803;
		public const int UnkOrdinaryChestR = 0xA804;
		public const int GameEventStateUpdate = 0xA805;
		public const int MailsRequest = 0xA897;
		public const int MailsRequestR = 0xA898;
		public const int SetPetAi = 0xA8A1;
		public const int GetPetAi = 0xA8A2;
		public const int GetPetAiR = 0xA8A3;
		public const int GuildChangeStone = 0xA8AC;
		public const int WarpUnk3 = 0xA8AF;
		public const int SetQuestTimer = 0xA8CF; // Was 0xA8D0 on RE (G13)
		public const int RemoveQuestTimer = 0xA8D0;
		public const int UpdateQuestTimer = 0xA8D1;
		public const int UmbrellaJump = 0xA8E0;
		public const int UmbrellaJumpR = 0xA8E1;
		public const int UmbrellaLand = 0xA8E2;

		// [200100, NA229 (2016-06-16)]
		// Presumably 4 ops were added, which shifted SetBgm~SkillTeleport.
		// The PetAi ops were unchanged, but others in this area might've
		// changed as well.

		// [200200, NA242 (2016-06-16)]
		// Presumably 2 ops were added, which shifted SetBgm?~DestroyExpiredItemsR.

		// [200200, NA249 (2017-04-13)]
		// Presumably 2 ops were added, which shifted SetBgm?~DcUnkR?.

		public const int SetBgm = 0xA916;
		public const int UnsetBgm = 0xA917;

		public const int EnterDynamicRegionExtended = 0xA986; // Creates multiple dynamic regions and warps to one
		public const int EnableRoyalAlchemist = 0xA9AB;
		public const int SpinColorWheel = 0xA9AD;
		public const int SpinColorWheelR = 0xA9AE;
		public const int ChangeNameColor = 0xA9AF;

		public const int SosButtonRequest = 0xA9B1;
		public const int SosButtonRequestR = 0xA9B2;
		public const int PersonalShopSetPriceForAll = 0xA9B8;
		public const int PersonalShopSetPriceForAllR = 0xA9B9;
		public const int SkillTeleport = 0xA9F8;
		public const int SetCamera = 0xA9FC;
		public const int EnterRebirth = 0xAA01;
		public const int EnterRebirthR = 0xAA02;

		// [150000~180000] Something was added? Next two ops changed.
		// [180800, NA196] Something was added? Ops 0xAAXX - 0xABXX increased by 4.

		// [200100, NA209 (2016-06-16)]
		// 4 new ops somewhere here, that shifted the SubsribeStabilityMeter~ChannelLoginUnkR ops by 4.

		public const int SubscribeStabilityMeter = 0xAA29;
		public const int StabilityMeterInit = 0xAA2A;
		public const int StabilityMeterUpdate = 0xAA2B;

		public const int HomesteadInfoRequest = 0xAA60;
		public const int HomesteadInfoRequestR = 0xAA61;
		public const int HomesteadEnterRequest = 0xAA62;
		public const int HomesteadEnterRequestR = 0xAA63;

		// [180300, NA166 (18.09.2013)] 2 new ops somewhere here, possibly the two below

		public const int CollectionRequest = 0xAA8D;
		public const int CollectionRequestR = 0xAA8E;

		// [200200, NA249 (2017-04-13)]
		// UnkRequest(R) is most likely part of the 7 added ops.
		public const int UnkRequest = 0xAA94; // Potentially "Potential System" related (sent with homestead info req)
		public const int UnkRequestR = 0xAA95;

		// [200100, NA209 (2016-06-16)]
		// 4 new ops somewhere here, that shifted the UnkEsc~? ops by 4.

		// [200200, NA246 (2017-02-16)]
		// 1 new op somewhere here, shifting UnkEsc~DcUnkR? by +1.

		// [200200, NA249 (2017-04-13)]
		// Presumably 7 ops were added, in addition to 2 more above,
		// which shifted UnkEsc?~DcUnkR?.

		// [200200, NA252 (2017-04-18)]
		// 2 ops were added, which shifted UnkEsc~DcUnkR?

		public const int UnkEsc = 0xAB03;

		//public const int GoBeautyShop = 0xAAF8;
		//public const int GoBeautyShopR = 0xAAF9;
		//public const int LeaveBeautyShop = 0xAAFA;
		//public const int LeaveBeautyShopR = 0xAAFB;
		//public const int OpenBeautyShop = 0xAAFC;
		//public const int ? = 0xAAFD;	// Buy looks?
		//public const int ? = 0xAAFE;	// Buy looks R?
		//public const int CancelBeautyShop = 0xAAFF;
		//public const int CancelBeautyShopR = 0xAB00;

		//public const int TalentInfoUpdate = 0xAB17;
		//public const int TalentTitleChange = 0xAB18;
		//public const int TalentTitleUpdate = 0xAB19;

		//public const int ShamalaTransformationUpdate = 0xAB1B;
		//public const int ShamalaTransformationUse = 0xAB1C;
		//public const int ShamalaTransformation = 0xAB1D;
		//public const int ShamalaTransformationEnd = 0xAB1E;
		//public const int ShamalaTransformationEndR = 0xAB1F;

		// [200100, NA209 (2016-06-16)]
		// 5 new ops somewhere here, that shifted BeginnerWarpBook by +5, to ABA8.

		// Added to Aura after ItemMagnet, original op unknown, if different.
		// [200200, NA252 (2017-04-18)] Shifted from ABB2 to 0xABB7?
		// When exactly this happened is unknown.
		public const int BeginnerWarpBook = 0xABB7;

		// [200200, NA252 (2017-04-18)]
		// 1 op was added, which shifted ItemMagnet~DcUnkR?

		// ItemMagnet got increased by one, some time between NA200 and NA204.
		// [200100, NA229 (2016-06-16)] ItemMagnet~AmmoRequired shifted by +4.

		// [190100, NA200 (2015-01-15)] Added
		// [190200, NA221 (2016-02-17)] Increased by one, ABAC->ABAD
		public const int ItemMagnet = 0xABC0;

		// [190200, NA221 (2016-02-17)] Added
		// [200100, NA229 (2016-06-16)] DestroyExpired* increased by four, ABAE->ABB2, ABAF->ABB3, ABB0->ABB4
		public const int DestroyExpiredItems = 0xABC1;
		public const int DestroyExpiredItemsConfirm = 0xABC2;
		public const int DestroyExpiredItemsR = 0xABC3;

		// [200100, NA229 (2016-04-16)] Shifted by +8, from ABBB to ABC3.
		// [200200, NA242 (2016-12-15)] Shifted by +2, from ABC3 to ABC5.
		public const int AmmoRequired = 0xABD2;

		// [200100, NA226 (2016-04-14)] Shifted by 4, from AC0A to AC0E.
		// [200100, NA229 (2016-06-16)] Shifted by +8, from AC0E to AC16.
		public const int ChatSticker = 0xAC23;

		// [200200, NA252 (2017-04-18)]
		// 3 ops were added, which shifted DcUnk~DcUnkR?

		// [190200, NA221 (2016-02-17)] Added
		// DcUnk, purpose unknown, requires answer on disconnect,
		// or the player gets stuck.
		// [190200, NA223 (2016-03-17)] Shifted by 6, from AC1D to AC23.
		// [200100, NA226 (2016-04-14)] Shifted by 9, from AC23 to AC1A.
		// [200100, NA229 (2016-06-16)] Shifted by 5, from AC1A to AC1F.
		// [200200, NA229 (2016-10-13)] Shifted by 1, from AC1F to AC20.
		// [200200, NA242 (2016-12-15)] Shifted by 4, from AC20 to AC24.
		public const int DcUnk = 0xAC34;
		public const int DcUnkR = 0xAC35;

		public const int RebirthEventInfoRequest = 0xAC5E;
		public const int RebirthEventInfo = 0xAC5F;
		public const int RebirthEventReceivePotion = 0xAC61;

		// [200200, NA242 (2016-06-16)] SwitchToPureMusicMode added
		// Sent when clicking headset icon I had above my head for some
		// reason.
		public const int SwitchToPureMusicMode = 0xAC8B;

		public const int UnkCharWindow = 0xACAE; // [200200, NA249 (2017-04-13)]

		// [200200, NA252 (2017-05-18)]
		public const int SkillApTraining = 0xACB8;
		public const int SkillApTrainingR = 0xACB9;

		public const int NpcTalk = 0x13882;
		public const int NpcTalkSelect = 0x13883;

		public const int SpecialLogin = 0x15F90; // ?
		public const int EnterSoulStream = 0x15F91;
		//public const int ? = 0x15F92;
		public const int LeaveSoulStream = 0x15F93;
		public const int LeaveSoulStreamR = 0x15F94;

		public const int FinishedCutscene = 0x186A0;
		public const int PlayCutscene = 0x186A6;
		public const int CutsceneEnd = 0x186A7;
		public const int CutsceneUnk = 0x186A8;

		public const int Weather = 0x1ADB0;

		// Nexon Game Security?
		public const int NGS1 = 0x1D4C2;
		public const int NGS2 = 0x1D4C3;

		public const int GmcpOpen = 0x1D589;
		public const int GmcpClose = 0x1D58A;
		public const int GmcpSummon = 0x1D58B;
		public const int GmcpMoveToChar = 0x1D58C;
		public const int GmcpWarp = 0x1D58D;
		public const int GmcpRevive = 0x1D58E;
		public const int GmcpInvisibility = 0x1D58F;
		public const int GmcpInvisibilityR = 0x1D590;
		public const int GmcpSearch = 0x1D595;
		public const int GmcpExpel = 0x1D596;
		public const int GmcpBan = 0x1D597;
		public const int GmcpNpcList = 0x1D59F;
		public const int GmcpNpcListR = 0x1D5A0;
		public const int GmcpBoost = 0x1D5A1; // [190200, NA203 (22.04.2015)] New? Or did I miss that feature?

		public const int PetMount = 0x1FBD0;
		public const int PetMountR = 0x1FBD1;
		public const int PetUnmount = 0x1FBD2;
		public const int PetUnmountR = 0x1FBD3;
		public const int VehicleInfo = 0x1FBD4;

		// [200100, NA226 (2016-04-14)] Added
		public const int SpecialUnitInfoRequest = 0x20F86;
		public const int SpecialUnitInfoRequestR = 0x20F87;

		public const int ErinnLandAchievementsRequest = 0x211DD;
		public const int ErinnLandAchievementsList = 0x211DE;

		public const int Run = 0x0F213303;
		public const int Running = 0x0F44BBA3;
		public const int CombatAttack = 0x0FCC3231;
		public const int Walking = 0x0FD13021;
		public const int Walk = 0x0FF23431;

		// Messenger Server
		// ------------------------------------------------------------------
		public static class Msgr
		{
			public const int Login = 0xC350;
			public const int LoginR = 0xC351;
			public const int FriendInvite = 0xC352;
			public const int FriendInviteR = 0xC353;
			public const int FriendConfirm = 0xC354;
			public const int FriendReply = 0xC355;
			public const int ChatInvite = 0xC356;
			public const int FriendListRequest = 0xC358;
			public const int FriendListRequestR = 0xC359;
			public const int FriendBlock = 0xC35A;
			public const int FriendBlockR = 0xC35B;
			public const int FriendUnblock = 0xC35C;
			public const int FriendUnblockR = 0xC35D;
			public const int FriendOnline = 0xC35E;
			public const int FriendOffline = 0xC35F;

			public const int ChatBegin = 0xC360;
			public const int ChatBeginR = 0xC361;
			public const int ChatEnd = 0xC362;
			public const int ChatInviteR = 0xC366;
			public const int ChatLeave = 0xC367;
			public const int Chat = 0xC368;
			public const int ChatR = 0xC36A;
			public const int DeleteFriend = 0xC36B;
			public const int ChatJoin = 0xC36C;
			public const int GuildChat = 0xC36E;
			public const int GuildChatMsg = 0xC36F;

			public const int ChangeOptions = 0xC370;
			public const int ChangeOptionsR = 0xC371;
			public const int FriendOptionChanged = 0xC372;
			public const int GroupList = 0xC376;
			public const int AddGroup = 0xC377;
			public const int DeleteGroup = 0xC379;
			public const int RenameGroup = 0xC37B;
			public const int ChangeGroup = 0xC37D;
			public const int SendNote = 0xC37E;
			public const int SendNoteR = 0xC37F;

			public const int NoteListRequest = 0xC380;
			public const int NoteListRequestR = 0xC381;
			public const int DeleteNote = 0xC382;
			public const int CheckNotes = 0xC384;
			public const int YouGotNote = 0xC385;
			public const int ReadNote = 0xC386;
			public const int ReadNoteR = 0xC387;
			public const int GuildJoin = 0xC388;
			public const int ChangeChannel = 0xC389;
			public const int FriendChannelChanged = 0xC38A;
			public const int GuildMemberList = 0xC38B;
			public const int GuildMemberListR = 0xC38C;
			public const int GuildMemberState = 0xC38D;
			public const int GuildMemberRemove = 0xC38E;
			public const int GuildMemberListUpdate = 0xC38F;

			public const int PlayerBlock = 0xC392;
		}

		// Internal communication
		// ------------------------------------------------------------------
		public static class Internal
		{
			public const int ServerIdentify = 0x42420001;
			public const int ServerIdentifyR = 0x42420002;

			public const int ChannelStatus = 0x42420101;

			public const int BroadcastNotice = 0x42420201;

			public const int ChannelShutdown = 0x42420301;

			public const int RequestDisconnect = 0x42420401;
		}

		/// <summary>
		/// Returns name of op code, if it's defined.
		/// </summary>
		/// <param name="op"></param>
		/// <returns></returns>
		public static string GetName(int op)
		{
			// Login/Channel
			foreach (var field in typeof(Op).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				if ((int)field.GetValue(null) == op)
					return field.Name;
			}

			// Msgr
			foreach (var field in typeof(Op.Msgr).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				if ((int)field.GetValue(null) == op)
					return "Msgr." + field.Name;
			}

			return "?";
		}
	}

	// public const int MailsRequest = 0xA898;
	// public const int MailsRequestR = 0xA899;
}
