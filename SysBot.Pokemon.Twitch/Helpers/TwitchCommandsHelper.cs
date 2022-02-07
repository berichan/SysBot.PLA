using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Twitch
{
    public static class TwitchCommandsHelper<T> where T : PKM, new()
    {
        // Helper functions for commands
        public static bool AddToWaitingList(string setstring, string display, string username, ulong mUserId, bool sub, bool isRequest, out string msg)
        {
            if (!TwitchBot<T>.Info.GetCanQueue())
            {
                msg = "Sorry, I am not currently accepting queue requests!";
                return false;
            }

            if (string.IsNullOrWhiteSpace(setstring))
            {
                msg = $"@{username}: You need to request something!{TwitchBot<T>.Hub.Config.Twitch.AdditionalRequestFailedMessage}";
                return false;
            }

            try
            {
                PKM? pkm = null;
                string result = string.Empty;
                if (!isRequest)
                {
                    var set = ShowdownUtil.ConvertToShowdown(setstring);
                    if (set == null)
                    {
                        msg = $"Skipping trade, @{username}: Empty nickname provided for the species.";
                        return false;
                    }
                    var template = AutoLegalityWrapper.GetTemplate(set);
                    if (template.Species < 1)
                    {
                        msg = $"Skipping trade, @{username}: Please read what you are supposed to type as the command argument.";
                        return false;
                    }

                    if (set.InvalidLines.Count != 0)
                    {
                        msg = $"Skipping trade, @{username}: Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                        return false;
                    }

                    var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                    pkm = sav.GetLegal(template, out result);
                }
                else
                {
                    var files = EnumerateSpecificFiles(TwitchBot<T>.Hub.Config.Folder.DistributeFolder, setstring).ToArray();

                    if (files.Length > 0)
                        pkm = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(files[0]));
                    else
                        LogUtil.LogError($"Not found: {setstring}.", nameof(TwitchCommandsHelper<T>));
                    //var path = Path.Combine(TwitchBot<T>.Hub.Config.Folder.DistributeFolder, setstring);
                    //if (File.Exists(path))
                    //    pkm = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(path));
                }

                if (pkm == null)
                {
                    msg = $"Skipping trade, @{username}: Unable to find this request!{TwitchBot<T>.Hub.Config.Twitch.AdditionalRequestFailedMessage}";
                    return false;
                }

                if (!pkm.CanBeTraded())
                {
                    msg = $"Skipping trade, @{username}: Provided Pokémon content is blocked from trading!";
                    return false;
                }

                if (pkm is T pk)
                {
                    var valid = new LegalityAnalysis(pkm).Valid;
                    if (valid)
                    {
                        var tq = new TwitchQueue<T>(pk, new PokeTradeTrainerInfo(display, mUserId), username, sub, isRequest);
                        TwitchBot<T>.QueuePool.RemoveAll(z => z.UserName == username); // remove old requests if any
                        TwitchBot<T>.QueuePool.Add(tq);
                        msg = $"NICE! @{username} - added to the waiting list. Please whisper your 8-digit trade code to me! (whisper this bot, not the streamer)";
                        return true;
                    }
                }

                var reason = result == "Timeout" ? "Set took too long to generate." : "Unable to legalize the Pokémon.";
                msg = $"Skipping trade, @{username}: {reason}";
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                LogUtil.LogSafe(ex, nameof(TwitchCommandsHelper<T>));
                msg = $"Skipping trade, @{username}: An unexpected problem occurred.";
            }
            return false;
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

        public static string ClearTrade(string user)
        {
            var result = TwitchBot<T>.Info.ClearTrade(user);
            return GetClearTradeMessage(result);
        }

        public static string ClearTrade(ulong userID)
        {
            var result = TwitchBot<T>.Info.ClearTrade(userID);
            return GetClearTradeMessage(result);
        }

        private static string GetClearTradeMessage(QueueResultRemove result)
        {
            return result switch
            {
                QueueResultRemove.CurrentlyProcessing => "Looks like you're currently being processed! Did not remove from queue.",
                QueueResultRemove.CurrentlyProcessingRemoved => "Looks like you're currently being processed! Removed from queue.",
                QueueResultRemove.Removed => "Removed you from the queue.",
                _ => "Sorry, you are not currently in the queue.",
            };
        }

        public static string GetCode(ulong parse)
        {
            var detail = TwitchBot<T>.Info.GetDetail(parse);
            return detail == null
                ? "Sorry, you are not currently in the queue."
                : $"Your trade code is {detail.Trade.Code:0000 0000}";
        }
    }
}
