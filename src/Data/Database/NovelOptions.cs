// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Aura.Data.Database
{
	[Serializable]
	public class NovelOptionsData
	{
		public NovelOptionType Type { get; set; }
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public enum NovelOptionType
	{
		Background,
		BGM,
		Effect,
		Portrait,
		SoundEffect,
	}

	/// <summary>
	/// Indexed by motion name.
	/// </summary>
	public class NovelOptionDb : DatabaseJson<NovelOptionsData>
	{
		public bool Exists(NovelOptionType type, int id)
		{
			return this.Entries.Any(a => a.Type == type && a.Id == id);
		}

		public NovelOptionsData Find(NovelOptionType type, int id)
		{
			return this.Entries.FirstOrDefault(a => a.Type == type && a.Id == id);
		}

		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("type", "id", "name");

			var info = new NovelOptionsData();
			info.Id = entry.ReadInt("id");
			info.Name = entry.ReadString("name");

			var typeStr = entry.ReadString("type");

			NovelOptionType type;
			if (!Enum.TryParse(typeStr, out type))
				throw new DatabaseWarningException("Invalid novel option type: '" + typeStr + "'");

			info.Type = type;

			this.Entries.Add(info);
		}
	}
}
