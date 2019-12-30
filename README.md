# Emby2JellyfinWatchStatusMigrator
A little helper tool to migrate the watch status of all items and all users from [Emby](https://github.com/MediaBrowser/Emby) to [Jellyfin](https://github.com/jellyfin/jellyfin)

## Motiviation
I'm a long term user of a [(kind of heavily) modified version of Emby](https://github.com/berrnd/Emby). Emby decided to not being Open Source anymore after the 3.5.3 release. Jellyfin is the successor of the Emby Open Source project (you guys rock!). I'm still running Emby 3.5.2, having the watch status migrated was one of my must-haves to migrate to Jellyfin. It's recommended to start with a fresh library/database, because of unknown incompatibilities. The only way which may work to migrate the watch status, is to use  [Trakt](https://trakt.tv/), set it up for all users in both Emby and Jellyfin. A lot to do and nothing for me, because it involves using a cloud service.

## What this does
This assumes that you have the same library (paths) and the same users (user names) in Emby and Jellyfin. It remapps the watch status of all items (Movies and TV Series Episodes) by the items physical path and users by their username from Emby to Jellyfin by directly using the databases (`library.db` and `users.db`).

**Warning:** It deletes/overwrites all watch infos in the Jellyfin database.

Currently tested with Emby `3.5.3` and Jellyfin `10.4.3`.

## How to use
It's a console application, just download the [latest release](https://github.com/berrnd/Emby2JellyfinWatchStatusMigrator/releases) for your OS and follow the instructions or build it manually.

```
Emby2JellyfinWatchStatusMigrator:
  Migrates the watch status for all users from Emby to Jellyfin by directly using the database and remapping library
  items by their physical path and users by their name.

Usage:
  Emby2JellyfinWatchStatusMigrator [options]

Options:
  --emby-library <emby-library>            The path to the Emby library.db
  --jellyfin-library <jellyfin-library>    The path to the Jellyfin library.db
  --emby-users <emby-users>                The path to the Emby users.db
  --jellyfin-users <jellyfin-users>        The path to the Jellyfin users.db
  --version                                Display version information
```

## How to build
This is a .Net Core 3.1 application, so just open the solution in Visual Studio 2019. The self-contained-single-executable for all platforms is created by the script `release.ps1`. Alternatively build the project manually by using standard .Net Core tooling.

## License
The MIT License (MIT)
