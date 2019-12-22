using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;

namespace Emby2JellyfinWatchStatusMigrator
{
	class Migrator
	{
		public static void Migrate(string embyLibraryPath, string jellyfinLibraryPath, string embyUsersPath, string jellyfinUsersPath)
		{
			if (!File.Exists(embyLibraryPath))
			{
				throw new FileNotFoundException($"Emby library.db path was not found");
			}

			if (!File.Exists(jellyfinLibraryPath))
			{
				throw new FileNotFoundException($"Jellyfin library.db path was not found");
			}


			// Load Emby users
			Console.WriteLine("Loading Emby users");
			Dictionary<int, string> embyUsers = new Dictionary<int, string>(); // Holds mapping of UserId => UserName
			using (SQLiteConnection embyDb = new SQLiteConnection($"Data Source={embyUsersPath}"))
			{
				embyDb.Open();

				SQLiteCommand cmd = new SQLiteCommand(embyDb);
				cmd.CommandText = @"
					SELECT
						u.Id,
						u.data
					FROM LocalUsersv2 u";

				SQLiteDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					JsonDocument jsonData = JsonDocument.Parse(reader.GetString(1));
					embyUsers.Add(reader.GetInt32(0), jsonData.RootElement.GetProperty("Name").GetString());
				}

				embyDb.Close();
			}
			Console.WriteLine($"Found {embyUsers.Count} Emby users");


			// Load Jellyfin users
			Console.WriteLine("Loading Jellyfin users");
			Dictionary<string, int> jellyfinUsers = new Dictionary<string, int>(); // Holds mapping of UserName => UserId
			using (SQLiteConnection jellyfinDb = new SQLiteConnection($"Data Source={jellyfinUsersPath}"))
			{
				jellyfinDb.Open();

				SQLiteCommand cmd = new SQLiteCommand(jellyfinDb);
				cmd.CommandText = @"
					SELECT
						u.Id,
						u.data
					FROM LocalUsersv2 u";

				SQLiteDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					JsonDocument jsonData = JsonDocument.Parse(reader.GetString(1));
					jellyfinUsers.Add(jsonData.RootElement.GetProperty("Name").GetString(), reader.GetInt32(0));
				}

				jellyfinDb.Close();
			}
			Console.WriteLine($"Found {jellyfinUsers.Count} Emby users");


			// Load Emby library & watch infos
			Console.WriteLine("Loading Emby library & watch infos...");
			Dictionary<string, string> embyLibraryItems = new Dictionary<string, string>(); // Holds mapping of Path => UserDataKey
			List<EmbyUserDatasRow> embyWatchInfos = new List<EmbyUserDatasRow>();
			using (SQLiteConnection embyDb = new SQLiteConnection($"Data Source={embyLibraryPath}"))
			{
				embyDb.Open();
				
				SQLiteCommand cmd = new SQLiteCommand(embyDb);
				cmd.CommandText = @"
					SELECT
						d.key,
						d.UserId,
						IFNULL(d.rating, -1),
						d.played,
						d.playCount,
						d.isFavorite,
						d.playbackPositionTicks,
						IFNULL(d.lastPlayedDate, '2999-01-01'),
						IFNULL(d.AudioStreamIndex, -1),
						IFNULL(d.SubtitleStreamIndex, -1),
						l.Path
					FROM UserDatas d
					JOIN TypedBaseItems l
						ON d.key = l.UserDataKey
					WHERE l.type IN('MediaBrowser.Controller.Entities.Movies.Movie', 'MediaBrowser.Controller.Entities.TV.Episode')
						AND l.Path IS NOT NULL";

				SQLiteDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					string userName = null;
					embyUsers.TryGetValue(reader.GetInt32(1), out userName);

					embyWatchInfos.Add(new EmbyUserDatasRow
					{
						key = reader.GetString(0),
						userId = reader.GetInt32(1),
						rating = reader.GetFloat(2),
						played = reader.GetBoolean(3),
						playCount = reader.GetInt32(4),
						isFavorite = reader.GetBoolean(5),
						playbackPositionTicks = reader.GetInt64(6),
						lastPlayedDate = reader.GetDateTime(7),
						AudioStreamIndex = reader.GetInt32(8),
						SubtitleStreamIndex = reader.GetInt32(9),

						_itemPath = reader.GetString(10),
						_userName = userName
					});

					if (!embyLibraryItems.ContainsKey(reader.GetString(10)))
					{
						embyLibraryItems.Add(reader.GetString(10), reader.GetString(0));
					}
				}

				embyDb.Close();
			}
			Console.WriteLine($"Found {embyLibraryItems.Count} Emby library items");
			Console.WriteLine($"Found {embyWatchInfos.Count} Emby watch infos");


			// Load Jellyfin library
			Console.WriteLine("Loading Jellyfin library...");
			Dictionary<string, string> jellyfinLibraryItems = new Dictionary<string, string>(); // Holds mapping of Path => UserDataKey
			using (SQLiteConnection jellyfinDb = new SQLiteConnection($"Data Source={jellyfinLibraryPath}"))
			{
				jellyfinDb.Open();

				SQLiteCommand cmd = new SQLiteCommand(jellyfinDb);
				cmd.CommandText = @"
					SELECT
						l.UserDataKey,
						l.Path
					FROM TypedBaseItems l
					WHERE l.type IN('MediaBrowser.Controller.Entities.Movies.Movie', 'MediaBrowser.Controller.Entities.TV.Episode')
						AND l.Path IS NOT NULL";

				SQLiteDataReader reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					if (!jellyfinLibraryItems.ContainsKey(reader.GetString(1)))
					{
						jellyfinLibraryItems.Add(reader.GetString(1), reader.GetString(0));
					}
				}

				jellyfinDb.Close();
			}
			Console.WriteLine($"Found {jellyfinLibraryItems.Count} Jellyfin library items");


			// Write Emby watch infos to Jellyfin library.db while remapping UserDataKey by the library items path and userId by the users name
			Console.WriteLine("Writing Emby watch infos to Jellyfin library...");
			int successCount = 0, failedCount = 0;
			using (SQLiteConnection jellyfinDb = new SQLiteConnection($"Data Source={jellyfinLibraryPath}"))
			{
				jellyfinDb.Open();

				// Empty UserDatas table first
				SQLiteCommand cmd0 = new SQLiteCommand(jellyfinDb);
				cmd0.CommandText = "DELETE FROM UserDatas";
				cmd0.ExecuteNonQuery();

				foreach (EmbyUserDatasRow item in embyWatchInfos)
				{
					// Silently ignore items without a user name, those are most probably old users in Emby which not longer exist currently
					if (string.IsNullOrEmpty(item._userName))
					{
						failedCount++;
						continue;
					}

					if (!jellyfinUsers.TryGetValue(item._userName, out int jellyfinUserId))
					{
						Console.WriteLine($"User not found in Jellyfin users.db => ignoring (EmbyUserName={item._userName};Path={item._itemPath};EmbyUserDataKey={item.key})");
						failedCount++;
						continue;
					}

					if (!jellyfinLibraryItems.TryGetValue(item._itemPath, out string jellyfinUserDataKey))
					{
						Console.WriteLine($"Item not found in Jellyfin library.db => ignoring (EmbyUserName={item._userName};Path={item._itemPath};EmbyUserDataKey={item.key})");
						failedCount++;
						continue;
					}

					SQLiteCommand cmd = new SQLiteCommand(jellyfinDb);
					cmd.CommandText = @"
						INSERT INTO UserDatas
							(key, userId, rating, played, playCount, isFavorite, playbackPositionTicks, lastPlayedDate, AudioStreamIndex, SubtitleStreamIndex)
						VALUES
							(@key, @userId, @rating, @played, @playCount, @isFavorite, @playbackPositionTicks, @lastPlayedDate, @AudioStreamIndex, @SubtitleStreamIndex)";
					cmd.Parameters.AddWithValue("@key", jellyfinUserDataKey);
					cmd.Parameters.AddWithValue("@userId", jellyfinUserId);
					if (item.rating != -1)
					{
						cmd.Parameters.AddWithValue("@rating", item.rating);
					}
					else
					{
						cmd.Parameters.AddWithValue("@rating", null);
					}
					cmd.Parameters.AddWithValue("@played", item.played);
					cmd.Parameters.AddWithValue("@playCount", item.playCount);
					cmd.Parameters.AddWithValue("@isFavorite", item.isFavorite);
					cmd.Parameters.AddWithValue("@playbackPositionTicks", item.playbackPositionTicks);
					if (item.lastPlayedDate.Year != 2999)
					{
						cmd.Parameters.AddWithValue("@lastPlayedDate", item.lastPlayedDate);
					}
					else
					{
						cmd.Parameters.AddWithValue("@lastPlayedDate", null);
					}
					if (item.AudioStreamIndex != -1)
					{
						cmd.Parameters.AddWithValue("@AudioStreamIndex", item.AudioStreamIndex);
					}
					else
					{
						cmd.Parameters.AddWithValue("@AudioStreamIndex", null);
					}
					if (item.SubtitleStreamIndex != -1)
					{
						cmd.Parameters.AddWithValue("@SubtitleStreamIndex", item.SubtitleStreamIndex);
					}
					else
					{
						cmd.Parameters.AddWithValue("@SubtitleStreamIndex", null);
					}

					try
					{
						cmd.ExecuteNonQuery();
					}
					catch (SQLiteException ex)
					{
						if (ex.ErrorCode == 19) // Constraint vialotion
						{
							Console.WriteLine($"Item violates constraint in Jellyfin library.db => ignoring (EmbyUserName={item._userName};Path={item._itemPath};EmbyUserDataKey={item.key})");
							failedCount++;
						}
						else
						{
							throw;
						}
					}

					successCount++;
				}

				jellyfinDb.Close();
			}
			Console.WriteLine($"Finished (WatchInfosMigrated={successCount};WatchInfosIgnored={failedCount})");
		}

		class EmbyUserDatasRow
		{
			public string key;
			public int userId;
			public float rating;
			public bool played;
			public int playCount;
			public bool isFavorite;
			public long playbackPositionTicks;
			public DateTime lastPlayedDate;
			public int AudioStreamIndex;
			public int SubtitleStreamIndex;

			public string _itemPath;
			public string _userName;
		}
	}
}
