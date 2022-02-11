using PKHeX.Core;
using SysBot.Base;
using System;
using System.Linq;

namespace SysBot.Pokemon
{
    public class PokeTradeLogNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        public string IdentifierLocator => "BotTrade";

        public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            LogUtil.LogInfo($"Starting trade loop for {info.Trainer.TrainerName}, sending {info.TradeData.CollateSpecies()}", routine.Connection.Label);
        }

        public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            LogUtil.LogInfo($"Searching for trade with {info.Trainer.TrainerName}, sending {info.TradeData.CollateSpecies()}", routine.Connection.Label);
        }

        public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            LogUtil.LogInfo($"Canceling trade with {info.Trainer.TrainerName}, because {msg}.", routine.Connection.Label);
            OnFinish?.Invoke(routine);
        }

        public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
        {
            LogUtil.LogInfo($"Finished trading {info.Trainer.TrainerName} {info.TradeData.CollateSpecies()} for {(Species)result.Species}", routine.Connection.Label);
            OnFinish?.Invoke(routine);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
        {
            LogUtil.LogInfo(message, routine.Connection.Label);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            var msg = message.Summary;
            if (message.Details.Count > 0)
                msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
            LogUtil.LogInfo(msg, routine.Connection.Label);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
        {
            LogUtil.LogInfo($"Notifying {info.Trainer.TrainerName} about their {(Species)result.Species}", routine.Connection.Label);
            LogUtil.LogInfo(message, routine.Connection.Label);
        }

        public void SendReminder(int position, string message)
        {
            
        }

        public Action<PokeRoutineExecutor<T>>? OnFinish { get; set; }

        public int QueueSizeEntry => 1;

        public bool ReminderSent => true;
    }
}