// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.IO;
using Aura.Data;
using Aura.Shared.Database;
using Aura.Shared.Util.Configuration;
using Aura.Shared.Util;
using Aura.Mabi;

namespace Aura.Shared
{
	/// <summary>
	/// General methods needed by all servers.
	/// </summary>
	public abstract class ServerMain
	{
		/// <summary>
		/// Tries to find aura root folder and changes the working directory to it.
		/// Exits if not successful.
		/// </summary>
		public void NavigateToRoot()
		{
			// Go back max 2 folders, the bins should be in [aura]/bin/(Debug|Release)
			for (int i = 0; i < 3; ++i)
			{
				if (Directory.Exists("system"))
					return;

				Directory.SetCurrentDirectory("..");
			}

			Log.Error("Unable to find root directory.");
			CliUtil.Exit(1);
		}

		/// <summary>
		/// Tries to call conf's load method, exits on error.
		/// </summary>
		public void LoadConf(BaseConf conf)
		{
			Log.Info("Reading configuration...");

			try
			{
				conf.Load();
			}
			catch (Exception ex)
			{
				Log.Exception(ex, "Unable to read configuration. ({0})", ex.Message);
				CliUtil.Exit(1);
			}
		}

		/// <summary>
		/// Tries to initialize database with the information from conf,
		/// exits on error.
		/// </summary>
		public virtual void InitDatabase(AuraDb db, BaseConf conf)
		{
			Log.Info("Initializing database...");

			try
			{
				db.Init(conf.Database.Host, conf.Database.Port, conf.Database.User, conf.Database.Pass, conf.Database.Db);
			}
			catch (Exception ex)
			{
				Log.Error("Unable to open database connection. ({0})", ex.Message);
				CliUtil.Exit(1);
			}
		}

		/// <summary>
		/// (Re-)Loads data files (db), exits on error.
		/// </summary>
		/// <remarks>
		/// Called on server start and with some reload commands.
		/// Should only load required data, e.g. Msgr Server doesn't
		/// need race data.
		/// </remarks>
		public void LoadData(DataLoad toLoad, bool reload)
		{
			Log.Info("Loading data...");

			try
			{
				if ((toLoad & DataLoad.Features) != 0)
				{
					this.LoadDb(AuraData.FeaturesDb, "db/features.txt", reload);
				}

				if ((toLoad & DataLoad.Races) != 0)
				{
					this.LoadDb(AuraData.AncientDropDb, "db/ancient_drops.txt", reload);
					this.LoadDb(AuraData.SpeedDb, "db/speed.txt", reload, false);
					this.LoadDb(AuraData.RaceDb, "db/races.txt", reload);
				}

				if ((toLoad & DataLoad.StatsBase) != 0)
				{
					this.LoadDb(AuraData.StatsBaseDb, "db/stats_base.txt", reload);
				}

				if ((toLoad & DataLoad.StatsLevel) != 0)
				{
					this.LoadDb(AuraData.StatsLevelUpDb, "db/stats_levelup.txt", reload);
				}

				if ((toLoad & DataLoad.StatsAge) != 0)
				{
					this.LoadDb(AuraData.StatsAgeUpDb, "db/stats_ageup.txt", reload);
				}

				if ((toLoad & DataLoad.Motions) != 0)
				{
					this.LoadDb(AuraData.MotionDb, "db/motions.txt", reload);
				}

				if ((toLoad & DataLoad.Cards) != 0)
				{
					this.LoadDb(AuraData.CharCardSetDb, "db/charcardsets.txt", reload, false);
					this.LoadDb(AuraData.CharCardDb, "db/charcards.txt", reload);
				}

				if ((toLoad & DataLoad.Colors) != 0)
				{
					this.LoadDb(AuraData.ColorMapDb, "db/colormap.dat", reload);
				}

				if ((toLoad & DataLoad.Items) != 0)
				{
					this.LoadDb(AuraData.ItemDb, "db/items.txt", reload);
					this.LoadDb(AuraData.ChairDb, "db/chairs.txt", reload);
				}

				if ((toLoad & DataLoad.Skills) != 0)
				{
					this.LoadDb(AuraData.SkillDb, "db/skills.txt", reload);
				}

				if ((toLoad & DataLoad.Regions) != 0)
				{
					this.LoadDb(AuraData.RegionDb, "db/regions.txt", reload);
					this.LoadDb(AuraData.RegionInfoDb, "db/regioninfo.dat", reload);
				}

				if ((toLoad & DataLoad.Shamala) != 0)
				{
					this.LoadDb(AuraData.ShamalaDb, "db/shamala.txt", reload);
				}

				if ((toLoad & DataLoad.PropDrops) != 0)
				{
					this.LoadDb(AuraData.PropDropDb, "db/prop_drops.txt", reload);
				}

				if ((toLoad & DataLoad.Exp) != 0)
				{
					this.LoadDb(AuraData.ExpDb, "db/exp.txt", reload);
				}

				if ((toLoad & DataLoad.Pets) != 0)
				{
					this.LoadDb(AuraData.PetDb, "db/pets.txt", reload);
				}

				if ((toLoad & DataLoad.Weather) != 0)
				{
					this.LoadDb(AuraData.WeatherTableDb, "db/weathertables.txt", reload);
					this.LoadDb(AuraData.WeatherDb, "db/weather.txt", reload);
				}

				if ((toLoad & DataLoad.Keywords) != 0)
				{
					this.LoadDb(AuraData.KeywordDb, "db/keywords.txt", reload);
				}

				if ((toLoad & DataLoad.Titles) != 0)
				{
					this.LoadDb(AuraData.TitleDb, "db/titles.txt", reload);
				}

				if ((toLoad & DataLoad.ItemUpgrades) != 0)
				{
					this.LoadDb(AuraData.ItemUpgradesDb, "db/itemupgrades.txt", reload);
				}

				if ((toLoad & DataLoad.Props) != 0)
				{
					this.LoadDb(AuraData.PropsDb, "db/props.txt", reload);
					this.LoadDb(AuraData.PropDefaultsDb, "db/prop_defaults.dat", reload);
				}

				if ((toLoad & DataLoad.Collecting) != 0)
				{
					this.LoadDb(AuraData.CollectingDb, "db/collecting.txt", reload);
				}

				if ((toLoad & DataLoad.Fishing) != 0)
				{
					this.LoadDb(AuraData.FishDb, "db/fish.txt", reload);
					this.LoadDb(AuraData.FishingGroundsDb, "db/fishing_grounds.txt", reload);
				}

				if ((toLoad & DataLoad.Dungeons) != 0)
				{
					this.LoadDb(AuraData.DungeonBlocksDb, "db/dungeon_blocks.txt", reload);
					this.LoadDb(AuraData.DungeonDb, "db/dungeons.txt", reload);
				}

				if ((toLoad & DataLoad.Cutscenes) != 0)
				{
					this.LoadDb(AuraData.CutscenesDb, "db/cutscenes.txt", reload);
					this.LoadDb(AuraData.ActorDb, "db/actors.txt", reload);
				}

				if ((toLoad & DataLoad.Production) != 0)
				{
					this.LoadDb(AuraData.ProductionDb, "db/production.txt", reload);
					this.LoadDb(AuraData.ManualDb, "db/manuals.txt", reload);
				}

				if ((toLoad & DataLoad.Cooking) != 0)
				{
					this.LoadDb(AuraData.CookingDb, "db/cooking.txt", reload);
				}

				if ((toLoad & DataLoad.Portals) != 0)
				{
					this.LoadDb(AuraData.PortalDb, "db/portals.txt", reload);
				}

				if ((toLoad & DataLoad.CharacterStyles) != 0)
				{
					this.LoadDb(AuraData.CharacterStyleDb, "db/characterstyles.txt", reload);
				}

				if ((toLoad & DataLoad.OptionSets) != 0)
				{
					this.LoadDb(AuraData.OptionSetDb, "db/optionsets.txt", reload);
				}

				if ((toLoad & DataLoad.Banks) != 0)
				{
					this.LoadDb(AuraData.BankDb, "db/banks.txt", reload);
				}

				if ((toLoad & DataLoad.ShopLicenses) != 0)
				{
					this.LoadDb(AuraData.ShopLicenseDb, "db/shop_licenses.txt", reload);
				}

				if ((toLoad & DataLoad.NovelOptions) != 0)
				{
					this.LoadDb(AuraData.NovelOptionDb, "db/noveloptions.txt", reload);
				}
			}
			catch (DatabaseErrorException ex)
			{
				Log.Error("{0}", ex.ToString());
				CliUtil.Exit(1);
			}
			catch (FileNotFoundException ex)
			{
				Log.Error(ex.Message);
				CliUtil.Exit(1);
			}
			catch (Exception ex)
			{
				Log.Exception(ex, "Error while loading data.");
				CliUtil.Exit(1);
			}
		}

		/// <summary>
		/// Loads db, first from system, then from user.
		/// Logs problems as warnings.
		/// </summary>
		private void LoadDb(IDatabase db, string path, bool reload, bool log = true)
		{
			var systemPath = Path.Combine("system", path).Replace('\\', '/');
			var userPath = Path.Combine("user", path).Replace('\\', '/');

			var cachePath = Path.Combine("cache", path).Replace('\\', '/');
			cachePath = Path.ChangeExtension(cachePath, "mpk");
			var cacheDir = Path.GetDirectoryName(cachePath);
			if (!Directory.Exists(cacheDir))
				Directory.CreateDirectory(cacheDir);

			if (!File.Exists(systemPath))
				throw new FileNotFoundException("Data file '" + systemPath + "' couldn't be found.", systemPath);

			db.Load(new string[] { systemPath, userPath }, cachePath, reload);

			foreach (var ex in db.Warnings)
				Log.Warning("{0}", ex.ToString());

			if (log)
				Log.Info("  done loading {0} entries from {1}", db.Count, Path.GetFileName(path));
		}

		/// <summary>
		/// Loads system and user localization files.
		/// </summary>
		public void LoadLocalization(BaseConf conf)
		{
			var language = conf.Localization.Language;
			var path = Path.Combine("localization", language + ".po");

			Log.Info("Loading localization ({0})...", language);

			// Try user first
			try
			{
				Localization.Load(Path.Combine("user", path));
			}
			catch (FileNotFoundException)
			{
				// Try system second, if the file wasn't in user
				try
				{
					Localization.Load(Path.Combine("system", path));
				}
				catch (FileNotFoundException)
				{
					// Warn if language wasn't the default
					if (language != "en-US")
						Log.Warning("Localization file '{0}.po' not found.", language);
				}
			}

			ErinnTime.SetMonthNames(
				Localization.Get("Imbolic"),
				Localization.Get("Alban Eiler"),
				Localization.Get("Baltane"),
				Localization.Get("Alban Heruin"),
				Localization.Get("Lughnasadh"),
				Localization.Get("Alban Elved"),
				Localization.Get("Samhain")
			);
		}
	}

	/// <summary>
	/// Used in LoadData, to specify which db files should be loaded.
	/// </summary>
	[Flags]
	public enum DataLoad : ulong
	{
		//Spawns = 0x01,
		Skills = 0x02,
		Races = 0x04,
		StatsBase = 0x08,
		StatsLevel = 0x10,
		Motions = 0x20,
		Cards = 0x40,
		Colors = 0x80,
		Items = 0x100,
		Regions = 0x200,
		Shamala = 0x400,
		PropDrops = 0x800,
		Exp = 0x1000,
		Pets = 0x2000,
		Weather = 0x4000,
		Keywords = 0x8000,
		Titles = 0x10000,
		StatsAge = 0x20000,
		ItemUpgrades = 0x40000,
		Props = 0x80000,
		Collecting = 0x100000,
		Fishing = 0x200000,
		Dungeons = 0x400000,
		Features = 0x800000,
		Cutscenes = 0x1000000,
		Production = 0x2000000,
		Cooking = 0x4000000,
		Portals = 0x8000000,
		CharacterStyles = 0x10000000,
		OptionSets = 0x20000000,
		Banks = 0x40000000,
		ShopLicenses = 0x80000000,
		NovelOptions = 0x100000000,

		All = 0xFFFFFFFFFFFFFFFF,

		LoginServer = Features | Races | StatsBase | Cards | Colors | Items | Pets | CharacterStyles,
		ChannelServer = All,
		Npcs = Races,
	}
}
