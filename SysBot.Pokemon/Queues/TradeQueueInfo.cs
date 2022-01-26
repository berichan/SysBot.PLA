using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Contains a queue of users to be processed.
    /// </summary>
    /// <typeparam name="T">Type of data to be transmitted to the users</typeparam>
    public sealed record TradeQueueInfo<T> where T : PKM, new()
    {
        private readonly object _sync = new();
        private readonly List<TradeEntry<T>> UsersInQueue = new();
        public readonly PokeTradeHub<T> Hub;

        public TradeQueueInfo(PokeTradeHub<T> hub) => Hub = hub;

        public int Count => UsersInQueue.Count;

        public bool ToggleQueue() => Hub.Config.Queues.CanQueue ^= true;
        public bool GetCanQueue() => Hub.Config.Queues.CanQueue && UsersInQueue.Count < Hub.Config.Queues.MaxQueueCount && Hub.TradeBotsReady;

        public TradeEntry<T>? GetDetail(ulong uid) => UsersInQueue.Find(z => z.UserID == uid);

        public QueueCheckResult<T> CheckPosition(ulong uid)
        {
            lock (_sync)
            {
                var index = UsersInQueue.FindIndex(z => z.Equals(uid));
                if (index < 0)
                    return QueueCheckResult<T>.None;

                var entry = UsersInQueue[index];
                var inQueue = UsersInQueue.Count(z => z.Type == entry.Type); //incase I change it back ig

                return new QueueCheckResult<T>(true, entry, index+1, inQueue);
            }
        }

        public string GetPositionString(ulong uid)
        {
            var check = CheckPosition(uid);
            return check.GetMessage();
        }

        public string GetTradeList()
        {
            lock (_sync)
            {
                var queue = Hub.Queues.GetQueue();
                if (queue.Count == 0)
                    return "Nobody in queue.";
                return queue.Summary();
            }
        }

        public void ClearAllQueues()
        {
            lock (_sync)
            {
                Hub.Queues.ClearAll();
                UsersInQueue.Clear();
            }
        }

        public QueueResultRemove ClearTrade(string userName)
        {
            var details = GetIsUserQueued(z => z.Username == userName);
            return ClearTrade(details);
        }

        public QueueResultRemove ClearTrade(ulong userID)
        {
            var details = GetIsUserQueued(z => z.UserID == userID);
            return ClearTrade(details);
        }

        private QueueResultRemove ClearTrade(ICollection<TradeEntry<T>> details)
        {
            if (details.Count == 0)
                return QueueResultRemove.NotInQueue;

            int removedCount = ClearTrade(details, Hub);

            if (removedCount == details.Count)
                return QueueResultRemove.Removed;

            bool canRemoveWhileProcessing = Hub.Config.Queues.CanDequeueIfProcessing;
            foreach (var detail in details)
            {
                if (detail.Trade.IsProcessing && !canRemoveWhileProcessing)
                    continue;
                Remove(detail);
            }

            return canRemoveWhileProcessing
                ? QueueResultRemove.CurrentlyProcessingRemoved
                : QueueResultRemove.CurrentlyProcessing;
        }

        public int ClearTrade(IEnumerable<TradeEntry<T>> details, PokeTradeHub<T> hub)
        {
            int removedCount = 0;
            lock (_sync)
            {
                var queue = hub.Queues.GetQueue();
                foreach (var detail in details)
                {
                    if (detail.Trade.IsProcessing && !Hub.Config.Queues.CanDequeueIfProcessing)
                        continue;
                    int removed = queue.Remove(detail.Trade);
                    if (removed != 0)
                        UsersInQueue.Remove(detail);
                    removedCount += removed;
                }
            }

            return removedCount;
        }

        public IEnumerable<string> GetUserList(string fmt)
        {
            return UsersInQueue.Select(z => string.Format(fmt, z.Trade.ID, z.Trade.Code, z.Trade.Type, z.Username, z.Trade.TradeData.CollateSpecies()));
        }

        public IList<TradeEntry<T>> GetIsUserQueued(Func<TradeEntry<T>, bool> match)
        {
            lock (_sync)
            {
                return UsersInQueue.Where(match).ToArray();
            }
        }

        public bool Remove(TradeEntry<T> detail)
        {
            lock (_sync)
            {
                LogUtil.LogInfo($"Removing {detail.Trade.Trainer.TrainerName}", nameof(TradeQueueInfo<T>));
                return UsersInQueue.Remove(detail);
            }
        }

        public QueueResultAdd AddToTradeQueue(TradeEntry<T> trade, ulong userID, bool sudo = false)
        {
            lock (_sync)
            {
                if (UsersInQueue.Any(z => z.UserID == userID) && !sudo)
                    return QueueResultAdd.AlreadyInQueue;

                if (Hub.Config.Legality.ResetHOMETracker && trade.Trade.TradeData is IHomeTrack t)
                    t.Tracker = 0;

                var priority = sudo ? PokeTradePriorities.Tier1 : PokeTradePriorities.TierFree;
                var queue = Hub.Queues.GetQueue();

                queue.Enqueue(trade.Trade, priority);
                UsersInQueue.Add(trade);

                trade.Trade.Notifier.OnFinish = _ => Remove(trade);
                return QueueResultAdd.Added;
            }
        }

        public int GetRandomTradeCode() => Hub.Config.Trade.GetRandomTradeCode();

        public int UserCount(Func<TradeEntry<T>, bool> func)
        {
            lock (_sync)
                return UsersInQueue.Count(func);
        }

        public int UserCount()
        {
            lock (_sync)
                return UsersInQueue.Count();
        }
    }
}