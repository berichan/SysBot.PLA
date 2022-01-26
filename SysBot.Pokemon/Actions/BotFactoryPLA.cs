using System;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public sealed class BotFactoryPLA : BotFactory<PK85>
    {
        public override PokeRoutineExecutorBase CreateBot(PokeTradeHub<PK85> Hub, PokeBotState cfg) => cfg.NextRoutineType switch
        {
            PokeRoutineType.PLAFlexTrade or PokeRoutineType.Idle
            or PokeRoutineType.PLAClone
            or PokeRoutineType.PLALinkTrade
            or PokeRoutineType.PLASpecialRequest
            => new PokeTradeBotPLA(Hub, cfg),

            _ => throw new ArgumentException(nameof(cfg.NextRoutineType)),
        };
    }
}
