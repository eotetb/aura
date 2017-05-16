// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aura.Mabi.Const
{
	// No enum because we don't need type safety and string conversion,
	// but something that can be passed to a function. (No casts ftw.)
	public static class Effect
	{
		/// <summary>
		/// Fire effect when shooting arrows at a campfire
		/// byte:1|0 (enable|disable)
		/// </summary>
		public static readonly int FireArrow = 0;

		/// <summary>
		/// 
		/// </summary>
		public static readonly int Enchant = 1;

		/// <summary>
		/// 
		/// </summary>
		public static readonly int Revive = 4;

		/// <summary>
		/// 
		/// </summary>
		public static readonly int Cooking = 7;

		/// <summary>
		/// Displays item above player's head, e.g. keys in dungeons.
		/// int:itemId, int:color1, int:color2, int:color3
		/// </summary>
		public static readonly int PickUpItem = 8;

		/// <summary>
		/// byte:type, byte:? [, long:propId, int:itemId, int:fishSize, string:fishPropname, int:fishPropSize]
		/// </summary>
		public static readonly int Fishing = 10;

		/// <summary>
		/// Logged values: "", "healing", "flashing", "thunder", "icespear"
		/// string:?
		/// </summary>
		public static readonly int SkillInit = 11;

		/// <summary>
		/// Logged values: "healing", "thunder", "icespear"
		/// string:?
		/// </summary>
		public static readonly int HoldMagic = 12;

		/// <summary>
		/// Logged values: "healing_stack"
		/// string:?, byte:amount?, byte:0
		/// </summary>
		public static readonly int StackUpdate = 13;

		/// <summary>
		/// Logged values: "healing_firstaid", "healing", "healing_phoenix", "thunder"
		/// string:?, long:targetCreatureId
		/// Healing Effects?
		/// </summary>
		public static readonly int UseMagic = 14;

		/// <summary>
		/// Removes int magic effects?
		/// string:effectName, short:skillId
		/// </summary>
		public static readonly int CancelMagic = 15;

		/// <summary>
		/// b:type, i|s:song, i:?, si:?, i:?, b:quality?, b:instrument, b:?, b:?, b:loops
		/// </summary>
		public static readonly int PlayMusic = 17;

		/// <summary>
		/// On music complete
		/// </summary>
		public static readonly int StopMusic = 18;

		/// <summary>
		/// Used for various pet actions, like dancing, admiring, etc.
		/// long:masterId?, byte:0~?, byte:0
		/// </summary>
		public static readonly int PetAction = 19;

		/// <summary>
		/// Smoke when burning an item.
		/// long:prop id, byte:0?
		/// </summary>
		public static readonly int BurnItem = 26;

		/// <summary>
		/// White flash.
		/// int:duration, int:0
		/// </summary>
		public static readonly int ScreenFlash = 27;

		/// <summary>
		/// int:region, float:x, float:y, byte:type
		/// type: 0=Glas_summon_appear, 1=pet_summon, 2=pet_unsummon, 3=monster_despawn, 4=Golem_summon_pop01, 5=Golem_summon_disappear
		/// </summary>
		public static readonly int Spawn = 29;

		/// <summary>
		/// Sent by thunder
		/// </summary>
		public static readonly int Lightningbolt = 30;

		/// <summary>
		/// Fireball in the air. int:Region, float:fromx, float:fromy, float:tox, float:toy, int:time, byte:0
		/// </summary>
		public static readonly int FireballFly = 39;

		/// <summary>
		/// The frozen effect of Ice Spear
		/// </summary>
		public static readonly int IcespearFreeze = 65;

		public static readonly int IcespearBoom = 66;

		// [190100, NA201 (2015-02-14)] Something was added somewhere,
		// which increased all following values by 1. Yay enums. Spawn
		// and Flash still worked though, it was afterwards.

		/// <summary>
		/// The teleport effect for Silent Move and Final Hit
		/// </summary>
		public static readonly int SilentMoveTeleport = 68;

		/// <summary>
		/// Effect shown while Final Hit is active.
		/// </summary>
		public static readonly int FinalHit = 70;

		/// <summary>
		/// Seen in Dice Tossing Usually has a string that's sent with the effect
		/// </summary>
		public static readonly int Dice = 82;

		/// <summary>
		/// Chef Owl
		/// </summary>
		public static readonly int ChefOwl = 122;

		/// <summary>
		/// Blue Aura used when activating Mana Shield
		/// </summary>
		/// <remarks>
		/// According to older logs, this should've been 121,
		/// something had been added when we got to implement it.
		/// </remarks>
		public static readonly int ManaShield = 123;

		/// <summary>
		/// Parameters: None
		/// </summary>
		public static readonly int AwakeningOfLight1 = 174;

		/// <summary>
		/// Parameters: None
		/// </summary>
		public static readonly int AwakeningOfLight2 = 177;

		public static readonly int SupportShot = 241;

		/// <summary>
		/// ?
		/// </summary>
		public static readonly int Casting = 248;

		/// <summary>
		/// Shadow Bunshin casting, clones, etc.
		/// </summary>
		public static readonly int ShadowBunshin = 263;

		/// <summary>
		/// Used in thunder's final stage
		/// </summary>
		public static readonly int Thunderbolt = 298;

		/// <summary>
		/// Heal effect (green number)
		/// int:amount
		/// </summary>
		public static readonly int HealLife = 343;

		/// <summary>
		/// Cherry blossoms falling onto the character.
		/// byte:1|0 (on/off)
		/// </summary>
		public static readonly int CherryBlossoms = 346;

		/// <summary>
		/// Used for Outfit Action.
		/// byte:1|0 (on/off)
		/// </summary>
		public static readonly int OutfitAction = 366;

		/// <summary>
		/// Effects for Lightning Rod.
		/// Different bytes cover the skill's spectrum of effects.
		/// int:0|2|3 (cancel | charging effect | shooting effect [position.x, position.y])
		/// </summary>
		public static readonly int LightningRod = 418;

		/// <summary>
		/// Effects for The Fake Spiral Sword
		/// </summary>
		public static readonly int TheFakeSpiralSword = 430;
	}

	public enum SpawnEffect : byte
	{
		Monster = 0,
		Pet = 1,
		PetDespawn = 2,
		MonsterDespawn = 3,
		Golem = 4,
		GolemDespawn = 5,
		//GolemDespawn = 6, // ?
		//Demi? = 7, // ?
		//Demi? = 8, // ?
	}

	public enum FishingEffectType : byte
	{
		Cast = 0,
		// ? = 1, // Nothing?
		// ? = 2, // Nothing?
		ReelIn = 3,
		Fall = 4,
	}

	public enum ChatSticker
	{
		None = 0,
		Morrighan = 1,
		Macha = 2,
		Neamhain = 3,
		Eweca = 4,
		Ladeca = 5,
		RibbonPink = 6,
		RibbonBlue = 7,
		BellGold = 8,
		BellSilver = 9,
		Butterfly = 10,
		ButterflyBlue = 11,
		Dog = 12,
		Cat = 13,
		Owl = 14,
		Cichol = 15,
	}

	public enum EnchantResult : byte
	{
		HugeFail = 1,
		Fail = 2,
		Success = 4,
		HugeSuccess = 8,
	}

	/// <summary>
	/// Effect parameters for Effect "LightningRod"
	/// </summary>
	public static class LightningRodEffect
	{
		/// <summary>
		/// Cancels the entire effect
		/// </summary>
		public const int Cancel = 0;

		/// <summary>
		/// Contains magic circle and lightning ball.
		/// </summary>
		public const int Prepare = 2;

		/// <summary>
		/// Burst of lightning; the effect for the attack.
		/// </summary>
		public const int Attack = 3;
	}

	/// <summary>
	/// Effect parameters for Effect "TheFakeSpiralSword"
	/// </summary>
	public static class TheFakeSpiralSwordEffect
	{
		/// <summary>
		/// Preparation effect for the skill
		/// </summary>
		public const byte Prepare = 1;

		/// <summary>
		/// Unknown effect, but it is used when
		/// readying The Fake Spiral Sword
		/// </summary>
		public const byte Ready = 2;

		/// <summary>
		/// Explosion effect
		/// </summary>
		public const byte Attack = 3;

		/// <summary>
		/// Completion effect
		/// </summary>
		public const byte Complete = 4;

		/// <summary>
		/// Cancels the entire effect
		/// </summary>
		public const byte Cancel = 6;
	}
}
