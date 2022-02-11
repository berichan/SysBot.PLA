using PKHeX.Core;
using System;
using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public class TradeQueueManager<T> where T : PKM, new()
    {
        private readonly PokeTradeHub<T> Hub;

        private readonly PokeTradeQueue<T> TradeQueue = new();
        public readonly TradeQueueInfo<T> Info;

        public TradeQueueManager(PokeTradeHub<T> hub)
        {
            Hub = hub;
            Info = new TradeQueueInfo<T>(hub);
            TradeQueue.Queue.Settings = hub.Config.Favoritism;
        }

        public PokeTradeQueue<T> GetQueue() => TradeQueue;

        public void ClearAll()
        {
            TradeQueue.Clear();
        }

        public bool TryDequeueLedy(out PokeTradeDetail<T> detail, bool force = false)
        {
            detail = default!;
            var cfg = Hub.Config.Distribution;
            if (!cfg.DistributeWhileIdle && !force)
                return false;

            if (Hub.Ledy.Pool.Count == 0)
                return false;

            var random = Hub.Ledy.Pool.GetRandomPoke();
            var code = cfg.RandomCode ? Hub.Config.Trade.GetRandomTradeCode() : cfg.TradeCode;
            var trainer = new PokeTradeTrainerInfo("Random Distribution");
            detail = new PokeTradeDetail<T>(random, trainer, PokeTradeHub<T>.LogNotifier, PokeTradeType.Random, code, false);
            return true;
        }

        public bool TryDequeue(out PokeTradeDetail<T> detail, out uint priority)
        {
            return TryDequeueInternal(out detail, out priority);
        }

        private bool TryDequeueInternal(out PokeTradeDetail<T> detail, out uint priority)
        {
            var queue = GetQueue();
            var toRet = queue.TryDequeue(out detail, out priority);
            SendReminders(queue);
            return toRet;
        }

        public void Enqueue(PokeRoutineType type, PokeTradeDetail<T> detail, uint priority)
        {
            var queue = GetQueue();
            queue.Enqueue(detail, priority);
        }

        // hook in here if you want to forward the message elsewhere???
        public readonly List<Action<PokeRoutineExecutorBase, PokeTradeDetail<T>>> Forwarders = new();

        public void StartTrade(PokeRoutineExecutorBase b, PokeTradeDetail<T> detail)
        {
            foreach (var f in Forwarders)
                f.Invoke(b, detail);
        }

        public void SendReminders(PokeTradeQueue<T> queue)
        {
            int queueThreshold = (int)((float)Hub.Config.Queues.ReminderQueueSize * Hub.Config.Queues.ReminderQueueTime);
            foreach (var v in queue.Queue)
            {
                var posInfo = Info.CheckPosition(v.Value.Trainer.ID);
                if (v.Value.Notifier.QueueSizeEntry >= Hub.Config.Queues.ReminderQueueSize && posInfo.Position <= queueThreshold)
                    v.Value.Notifier.SendReminder(posInfo.Position, string.Empty);
            }
        }
    }
}
