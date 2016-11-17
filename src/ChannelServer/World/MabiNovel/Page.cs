// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Data;
using Aura.Data.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Channel.World.MabiNovel
{
	/// <summary>
	/// Represents a page in a <see cref="Novel"/>.
	/// </summary>
	public class Page
	{
		/// <summary>
		/// The index ("page number") of the page.
		/// </summary>
		public int Index { get; set; }

		/// <summary>
		/// The id of the background.
		/// </summary>
		public int BackgroundId { get; set; }

		/// <summary>
		/// The id of the BGM.
		/// </summary>
		public int BgmId { get; set; }

		/// <summary>
		/// The id of the portrait.
		/// </summary>
		public int PortraitId { get; set; }

		/// <summary>
		/// Where to display the portrait.
		/// </summary>
		public PagePortraitOrientation Orientation { get; set; }

		/// <summary>
		/// Portrait expression id.
		/// </summary>
		public int ExpressionId { get; set; }

		/// <summary>
		/// Id of the animation effect.
		/// </summary>
		public int AnimationEffectId { get; set; }

		/// <summary>
		/// Id of the sound effect to be played once.
		/// </summary>
		public int SoundEffectId { get; set; }

		/// <summary>
		/// Text to be displayed.
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// Creates new Page instance.
		/// </summary>
		/// <param name="backgroundId"></param>
		/// <param name="bgmId"></param>
		/// <param name="portraitId"></param>
		/// <param name="orientation"></param>
		/// <param name="expressionId"></param>
		/// <param name="animationEffectId"></param>
		/// <param name="soundEffectId"></param>
		/// <param name="text"></param>
		/// <exception cref="ArgumentException">
		/// Thrown if any of the ids could not be found in the data.
		/// </exception>
		public Page(int backgroundId, int bgmId, int portraitId, PagePortraitOrientation orientation, int expressionId, int animationEffectId, int soundEffectId, string text)
		{
			if (backgroundId != 0 && !AuraData.NovelOptionDb.Exists(NovelOptionType.Background, backgroundId))
				throw new ArgumentException("Unknown background.");

			if (bgmId != 0 && !AuraData.NovelOptionDb.Exists(NovelOptionType.BGM, bgmId))
				throw new ArgumentException("Unknown BGM.");

			if (portraitId != 0 && !AuraData.NovelOptionDb.Exists(NovelOptionType.Portrait, portraitId))
				throw new ArgumentException("Unknown portrait.");

			if (animationEffectId != 0 && !AuraData.NovelOptionDb.Exists(NovelOptionType.Effect, animationEffectId))
				throw new ArgumentException("Unknown animation effect.");

			if (soundEffectId != 0 && !AuraData.NovelOptionDb.Exists(NovelOptionType.SoundEffect, soundEffectId))
				throw new ArgumentException("Unknown sound effect.");

			this.BackgroundId = backgroundId;
			this.BgmId = bgmId;
			this.PortraitId = portraitId;
			this.Orientation = orientation;
			this.ExpressionId = expressionId;
			this.AnimationEffectId = animationEffectId;
			this.SoundEffectId = soundEffectId;
			this.Text = text;
		}
	}

	public enum PagePortraitOrientation : short
	{
		Left,
		Right,
	}
}
