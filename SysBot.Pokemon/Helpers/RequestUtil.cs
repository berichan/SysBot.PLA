using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SysBot.Pokemon
{
    public static class RequestUtil<T> where T : PKM, new()
    {
        public static string Prefix { get; set; } = "!";

        private const string FilenameRequests = "poprequests.txt";

        private static PopularTracker<string> RequestTracker = new("Popular requests:");

        private static DateTime LastClearedTime = DateTime.Now;
        private static int ClearCount = 0;

        private static object sync = new object();

        public static T? GetPokemonViaNamedRequest(string folder, string name)
        {
            var files = EnumerateSpecificFiles(folder, name).ToArray();

            if (files.Length > 0)
            {
                // get the filename that most closely resembles the one they asked for
                var fileNameGet = files[0];
                int bestDistance = int.MaxValue;

                foreach (string file in files)
                {
                    var thisDistance = LevenshteinDistance.Compute(Path.GetFileNameWithoutExtension(file), name);
                    if (thisDistance < bestDistance)
                    {
                        bestDistance = thisDistance;
                        fileNameGet = file;
                    }
                }

                var pkm = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(fileNameGet));
                pkm?.ResetPartyStats();
                HandleTracking(Path.GetFileNameWithoutExtension(fileNameGet));
                return (T?)pkm;
            }

            LogUtil.LogError($"Not found: {name}.", nameof(RequestUtil<T>));
            return null;
        }

        static IEnumerable<string> EnumerateSpecificFiles(string directory, string initialTextForFileName)
        {
            foreach (string file in Directory.EnumerateFiles(directory))
            {
                var pt = Path.GetFileNameWithoutExtension(file);
                if (pt.StartsWith(initialTextForFileName, StringComparison.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }

        static void HandleTracking(string newItem)
        {
            var span = DateTime.Now - LastClearedTime;
            lock (sync)
            {
                RequestTracker.AddEntryCount(newItem);
                if (span.TotalMinutes > 1)
                {
                    ClearCount++;
                    LastClearedTime = DateTime.Now;
                    if (ClearCount < 20)
                        UpdateTracker(false);
                    else
                    {
                        UpdateTracker(true);
                        ClearCount = 0;
                    }
                }
            }
        }

        static void UpdateTracker(bool alsoClear)
        {
            var mostPopular = RequestTracker.GetMostPopular(7);
            StringBuilder sb = new();
            sb.AppendLine(RequestTracker.Description);
            foreach (var item in mostPopular)
                sb.AppendLine($"{Prefix}request {item.Key}");
            
            File.WriteAllText(FilenameRequests, sb.ToString());
            if (alsoClear)
                RequestTracker.Clear();
        }
    }
}
