using PKHeX.Core;
using System;
using System.Threading;

namespace SysBot.Pokemon
{
    public class PokeTradeDetail<TPoke> : IEquatable<PokeTradeDetail<TPoke>>, IFavoredEntry where TPoke : PKM, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        /// <summary> Global variable indicating the amount of trades created. </summary>
        private static int CreatedCount;
        /// <summary> Indicates if this trade data should be given priority for queue insertion. </summary>
        public bool IsFavored { get; }

        /// <summary>
        /// Trade Code
        /// </summary>
        public int Code;

        /// <summary> Data to be traded </summary>
        public TPoke[] TradeData;

        /// <summary> First data to be traded </summary>
        public TPoke FirstData { get => TradeData[0]; set => TradeData[0] = value; }

        /// <summary> Trainer details </summary>
        public readonly PokeTradeTrainerInfo Trainer;

        /// <summary> Destination to be notified for status updates </summary>
        public readonly IPokeTradeNotifier<TPoke> Notifier;

        /// <summary> Type of trade this object is for </summary>
        public readonly PokeTradeType Type;

        /// <summary> Time the object was created at </summary>
        public readonly DateTime Time;
        /// <summary> Unique incremented ID </summary>
        public readonly int ID;

        /// <summary> Indicates if the trade data should be synchronized with other bots. </summary>
        public bool IsSynchronized => Type == PokeTradeType.Random;

        /// <summary> Indicates if the trade failed at least once and is being tried again. </summary>
        public bool IsRetry;

        /// <summary> Indicates if the trade data is currently being traded. </summary>
        public bool IsProcessing;

        /// <summary> Indicated whether we should attempt to switch out the requests OTData </summary>
        public bool UseInTradeTrainerData;

        public PokeTradeDetail(TPoke pkm, PokeTradeTrainerInfo info, IPokeTradeNotifier<TPoke> notifier, PokeTradeType type, int code, bool favored = false, bool inTradeID = false)
            :this(new TPoke[1] { pkm }, info, notifier, type, code, favored, inTradeID)
        {
        }

        public PokeTradeDetail(TPoke[] pkm, PokeTradeTrainerInfo info, IPokeTradeNotifier<TPoke> notifier, PokeTradeType type, int code, bool favored = false, bool inTradeID = false)
        {
            if (pkm == null || pkm.Length < 1)
                throw new Exception("Sending data is empty.");
            ID = Interlocked.Increment(ref CreatedCount) % 3000;
            Code = code;
            TradeData = pkm;
            Trainer = info;
            Notifier = notifier;
            Type = type;
            Time = DateTime.Now;
            IsFavored = favored;
            UseInTradeTrainerData = inTradeID;
        }

        public void TradeInitialize(PokeRoutineExecutor<TPoke> routine) => Notifier.TradeInitialize(routine, this);
        public void TradeSearching(PokeRoutineExecutor<TPoke> routine) => Notifier.TradeSearching(routine, this);
        public void TradeCanceled(PokeRoutineExecutor<TPoke> routine, PokeTradeResult msg) => Notifier.TradeCanceled(routine, this, msg);

        public virtual void TradeFinished(PokeRoutineExecutor<TPoke> routine, TPoke result)
        {
            Notifier.TradeFinished(routine, this, result);
        }

        public void SendNotification(PokeRoutineExecutor<TPoke> routine, string message) => Notifier.SendNotification(routine, this, message);
        public void SendNotification(PokeRoutineExecutor<TPoke> routine, PokeTradeSummary obj) => Notifier.SendNotification(routine, this, obj);
        public void SendNotification(PokeRoutineExecutor<TPoke> routine, TPoke obj, string message) => Notifier.SendNotification(routine, this, obj, message);

        public bool Equals(PokeTradeDetail<TPoke>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ReferenceEquals(Trainer, other.Trainer);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((PokeTradeDetail<TPoke>)obj);
        }

        public override int GetHashCode() => Trainer.GetHashCode();
        public override string ToString() => $"{Trainer.TrainerName} - {Code}";

        public string Summary(int queuePosition)
        {
            if (FirstData.Species == 0)
                return $"{queuePosition:00}: {Trainer.TrainerName}";
            return $"{queuePosition:00}: {Trainer.TrainerName}, {TradeData.CollateSpecies()}";
        }
    }
}