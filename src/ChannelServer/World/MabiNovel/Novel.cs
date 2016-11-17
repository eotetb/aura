// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Channel.World.MabiNovel
{
	/// <summary>
	/// A novel, consisting of a variable amount of pages, which are
	/// displayed similar to a cutscene by the client.
	/// </summary>
	public class Novel
	{
		private List<Page> _pages = new List<Page>();

		/// <summary>
		/// The novel's id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The novel's name.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// The novel's author's name.
		/// </summary>
		public string AuthorName { get; set; }

		/// <summary>
		/// Amount of times this novel was transcriped.
		/// </summary>
		public int TranscriptionsCount { get; set; }

		/// <summary>
		/// Amount of times this novel was read.
		/// </summary>
		public int ViewsCount { get; set; }

		/// <summary>
		/// Date at which the novel expires.
		/// </summary>
		public DateTime ExpirationDate { get; set; }

		/// <summary>
		/// Adds given page to end of novel.
		/// </summary>
		/// <param name="page"></param>
		public void AddPage(Page page)
		{
			lock (_pages)
			{
				page.Index = _pages.Count;
				_pages.Add(page);
			}
		}

		/// <summary>
		/// Returns 
		/// </summary>
		/// <returns></returns>
		public Page[] GetPages()
		{
			lock (_pages)
				return _pages.ToArray();
		}
	}
}
