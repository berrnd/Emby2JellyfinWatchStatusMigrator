using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Reflection;

namespace Emby2JellyfinWatchStatusMigrator
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.BufferWidth = Console.BufferWidth * 2;

			RootCommand rootCommand = new RootCommand
			{
				new Option("--emby-library", "The path to the Emby library.db")
				{
					Argument = new Argument<FileInfo>(),
					Required = true

				},
				new Option("--jellyfin-library", "The path to the Jellyfin library.db")
				{
					Argument = new Argument<FileInfo>(),
					Required = true
				},
				new Option("--emby-users", "The path to the Emby users.db")
				{
					Argument = new Argument<FileInfo>(),
					Required = true

				},
				new Option("--jellyfin-users", "The path to the Jellyfin users.db")
				{
					Argument = new Argument<FileInfo>(),
					Required = true
				}
			};

			rootCommand.Description = $"{Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>().Description}";
			rootCommand.Description += Environment.NewLine + Environment.NewLine;
			rootCommand.Description += "This is a project by Bernd Bestel - https://github.com/berrnd/Emby2JellyfinWatchStatusMigrator";

			rootCommand.Handler = CommandHandler.Create<FileInfo, FileInfo, FileInfo, FileInfo>((embyLibrary, jellyfinLibrary, embyUsers, jellyfinUsers) =>
			{
				Migrator.Migrate(embyLibrary?.FullName, jellyfinLibrary?.FullName, embyUsers?.FullName, jellyfinUsers?.FullName);
			});

			try
			{
				rootCommand.Invoke(args);
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.GetType().Name);
				Console.WriteLine();
				Console.WriteLine(ex.Message);
				Console.WriteLine();
				Console.WriteLine(ex.StackTrace);
			}

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();
		}
	}
}
