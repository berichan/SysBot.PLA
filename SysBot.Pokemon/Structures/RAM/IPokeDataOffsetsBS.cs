using System.Collections.Generic;

namespace SysBot.Pokemon
{
    public interface IPokeDataOffsetsBS
    {
        public IReadOnlyList<long> BoxStartPokemonPointer { get; }
        public IReadOnlyList<long> LinkTradePartnerPokemonPointer { get; }
        public IReadOnlyList<long> LinkTradePartnerNamePointer { get; }
        public IReadOnlyList<long> LinkTradePartnerIDPointer { get; }
        public IReadOnlyList<long> LinkTradePartnerParamPointer { get; }
        public IReadOnlyList<long> LinkTradePartnerNIDPointer { get; }

        public IReadOnlyList<long> PlayerPositionPointer { get; }
        public IReadOnlyList<long> PlayerRotationPointer { get; }
        public IReadOnlyList<long> PlayerMovementPointer { get; }

        public IReadOnlyList<long> UnitySceneStreamPointer { get; }

        public IReadOnlyList<long> SceneIDPointer { get; }
        public IReadOnlyList<long> UnionWorkIsGamingPointer { get; }
        public IReadOnlyList<long> UnionWorkIsTalkingPointer { get; }
        public IReadOnlyList<long> UnionWorkPenaltyPointer { get; }
        public IReadOnlyList<long> MyStatusTrainerPointer { get; }
        public IReadOnlyList<long> MyStatusTIDPointer { get; }
        public IReadOnlyList<long> ConfigTextSpeedPointer { get; }
        public IReadOnlyList<long> ConfigLanguagePointer { get; }
    }
}
