﻿using Kermalis.PokemonBattleEngine.Battle;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Kermalis.PokemonBattleEngineDiscord
{
    internal static class ReplaySaver
    {
        private const bool ShouldSaveForfeits = false;
        private const int NumDaysTillRemoval = 30;
        private const string ReplayDirectory = "Replays";
        private const string DateRegexPattern = @"^([0-9]+)\-([0-9]+)\-([0-9]+)$";

        private static DateTime _lastDateTime = DateTime.Today;

        private static string GetTodayFolderPath()
        {
            DateTime today = DateTime.Today;
            if (today != _lastDateTime)
            {
                _lastDateTime = today;
                Console.WriteLine("Date changed; checking old replay directories...");
                RemoveOldReplays();
            }
            return string.Format("{0}-{1}-{2}", today.Year, today.Month, today.Day);
        }

        public static void SaveReplay(PBEBattle battle)
        {
            // Battle winner is null if forfeited
            if (battle.Winner != null || ShouldSaveForfeits)
            {
                string dir = Path.Combine(ReplayDirectory, GetTodayFolderPath());
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                battle.SaveReplayToFolder(dir);
            }
        }

        public static void RemoveOldReplays()
        {
            if (!Directory.Exists(ReplayDirectory))
            {
                Directory.CreateDirectory(ReplayDirectory);
            }
            else
            {
                DateTime today = DateTime.Today;
                foreach (string dir in Directory.EnumerateDirectories(ReplayDirectory))
                {
                    string dirName = new DirectoryInfo(dir).Name;
                    Match m = Regex.Match(dirName, DateRegexPattern);
                    if (m.Success)
                    {
                        TimeSpan timePassed = today - new DateTime(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value));
                        if (timePassed.Days >= NumDaysTillRemoval)
                        {
                            Console.WriteLine("Deleting old replay directory: {0}", dirName);
                            Directory.Delete(dir);
                        }
                    }
                }
            }
        }
    }
}