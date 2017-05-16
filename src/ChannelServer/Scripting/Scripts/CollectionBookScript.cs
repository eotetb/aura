// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using Aura.Channel.World.Entities;
using Aura.Shared.Util;
using System;
using System.Reflection;

namespace Aura.Channel.Scripting.Scripts
{
	/// <summary>
	/// Handles collection books.
	/// </summary>
	public abstract class CollectionBookScript : GeneralScript
	{
		/// <summary>
		/// Called to initialize the script.
		/// </summary>
		/// <returns></returns>
		public override bool Init()
		{
			var attr = this.GetType().GetCustomAttribute<CollectionBookAttribute>();
			if (attr == null)
			{
				Log.Error("CollectionBookScript.Init: Missing CollectionBook attribute.");
				return false;
			}

			this.Load();

			foreach (var itemId in attr.ItemIds)
				ChannelServer.Instance.ScriptManager.CollectionBookScripts.Add(itemId, this);

			return true;
		}

		/// <summary>
		/// Returns the index of the item in the collection, or -1 if it
		/// doesn't belong in the book.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public abstract int GetIndex(Item item);

		/// <summary>
		/// Called when a player attempts to add an item to a book.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="book"></param>
		/// <param name="item"></param>
		public virtual void OnAdd(Creature creature, Item book, Item item)
		{
		}

		/// <summary>
		/// Called when a player completes the book.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="book"></param>
		/// <returns></returns>
		public virtual void OnComplete(Creature creature, Item book)
		{
		}

		/// <summary>
		/// Called when a player completes the book.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="book"></param>
		/// <returns></returns>
		public virtual void OnReward(Creature creature, Item book)
		{
		}
	}

	public class CollectionBookAttribute : Attribute
	{
		public int[] ItemIds { get; private set; }

		public CollectionBookAttribute(params int[] itemIds)
		{
			this.ItemIds = itemIds;
		}
	}
}
