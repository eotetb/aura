// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.World.Entities;
using Aura.Channel.World.MabiNovel;
using Aura.Mabi.Network;

namespace Aura.Channel.Network.Sending
{
	public static partial class Send
	{
		/// <summary>
		/// Sends MabiNovel to creature's client.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="novel"></param>
		public static void MabiNovel(Creature creature, Novel novel)
		{
			var pages = novel.GetPages();

			var packet = new Packet(Op.MabiNovel, creature.EntityId);
			packet.PutInt(pages.Length);
			foreach (var page in pages)
			{
				packet.PutShort((short)page.Index);
				packet.PutShort((short)page.BackgroundId);
				packet.PutShort((short)page.BgmId);
				packet.PutShort((short)page.PortraitId);
				packet.PutShort((short)page.Orientation);
				packet.PutShort((short)page.ExpressionId);
				packet.PutShort((short)page.SoundEffectId);
				packet.PutShort((short)page.AnimationEffectId);
				packet.PutString(page.Text);
			}

			packet.PutLong(novel.Id);
			packet.PutInt(8);

			creature.Client.Send(packet);
		}
	}
}
