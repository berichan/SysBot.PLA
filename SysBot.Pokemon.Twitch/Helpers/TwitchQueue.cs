﻿using PKHeX.Core;

namespace SysBot.Pokemon.Twitch
{
    public class TwitchQueue<T> where T : PKM, new()
    {
        public T Pokemon { get; }
        public PokeTradeTrainerInfo Trainer { get; }
        public string UserName { get; }
        public string DisplayName => Trainer.TrainerName;
        public bool IsSubscriber { get; }
        public bool UseTradeID { get; }

        public TwitchQueue(T pkm, PokeTradeTrainerInfo trainer, string username, bool subscriber, bool useTradeID)
        {
            Pokemon = pkm;
            Trainer = trainer;
            UserName = username;
            IsSubscriber = subscriber;
            UseTradeID = useTradeID;
        }
    }
}
