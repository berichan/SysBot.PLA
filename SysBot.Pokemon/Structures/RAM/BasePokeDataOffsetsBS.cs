using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Pokemon
{
    public abstract class BasePokeDataOffsetsBS : IPokeDataOffsetsBS
    {
        public const string ShiningPearlID = "010018E011D92000";
        public const string BrilliantDiamondID = "0100000011D90000";

        public abstract IReadOnlyList<long> BoxStartPokemonPointer { get; }
        public abstract IReadOnlyList<long> LinkTradePartnerPokemonPointer { get; }
        public abstract IReadOnlyList<long> LinkTradePartnerNamePointer { get; }
        public abstract IReadOnlyList<long> LinkTradePartnerIDPointer { get; }
        public abstract IReadOnlyList<long> LinkTradePartnerParamPointer { get; }
        public abstract IReadOnlyList<long> LinkTradePartnerNIDPointer { get; }

        public abstract IReadOnlyList<long> PlayerPositionPointer { get; }
        public abstract IReadOnlyList<long> PlayerRotationPointer { get; }
        public abstract IReadOnlyList<long> PlayerMovementPointer { get; }

        public abstract IReadOnlyList<long> UnitySceneStreamPointer { get; }

        public abstract IReadOnlyList<long> SceneIDPointer { get; }
        public abstract IReadOnlyList<long> UnionWorkIsGamingPointer { get; }
        public abstract IReadOnlyList<long> UnionWorkIsTalkingPointer { get; }
        public abstract IReadOnlyList<long> UnionWorkPenaltyPointer { get; }
        public abstract IReadOnlyList<long> MyStatusTrainerPointer { get; }
        public abstract IReadOnlyList<long> MyStatusTIDPointer { get; }
        public abstract IReadOnlyList<long> ConfigTextSpeedPointer { get; }
        public abstract IReadOnlyList<long> ConfigLanguagePointer { get; }

        // Scene stream bytes
        public const byte UnitySceneStream_LocalUnionRoom = 0x09;
        public const byte UnitySceneStream_PokeCentreUpstairsLocal = 0x26;
        public const byte UnitySceneStream_PokeCentreMain = 0x22;
        public const byte UnitySceneStream_PokeCentreDownstairsGlobal = 0x24;
        public const byte UnitySceneStream_FullScene = 0x01;

        // SceneID enums
        public const byte SceneID_Field = 0;
        public const byte SceneID_Room = 1;
        public const byte SceneID_Battle = 2;
        public const byte SceneID_Title = 3;
        public const byte SceneID_Opening = 4;
        public const byte SceneID_Contest = 5;
        public const byte SceneID_DigFossil = 6;
        public const byte SceneID_SealPreview = 7;
        public const byte SceneID_EvolveDemo = 8;
        public const byte SceneID_HatchDemo = 9;
        public const byte SceneID_GMS = 10;

        public const byte SubMenuState_TitleScreen = 0x41;
        public const byte SubMenuState_NonBox = 0x46;
        public const byte SubMenuState_PauseMenu = 0x47;
        public const byte SubMenuState_Box = 0x79;


        public const int BoxFormatSlotSize = 0x158;

        public static readonly Vector3 LocalUnionRoomCenter = new(-7f, 0, 5.5f);
        public static readonly Vector3 GlobalUnionRoomCenter = new(-14.5f, 0, 10.5f);

        public static UnitySceneStream GetUnitySceneStream(byte val)
        {
            return val switch
            {
                UnitySceneStream_LocalUnionRoom => UnitySceneStream.LocalUnionRoom,
                UnitySceneStream_PokeCentreUpstairsLocal => UnitySceneStream.PokeCentreUpstairsLocal,
                UnitySceneStream_PokeCentreDownstairsGlobal => UnitySceneStream.PokeCentreDownstairsGlobal,
                UnitySceneStream_FullScene => UnitySceneStream.FullScene,
                _ => UnitySceneStream.Unknown,
            };
        }

        public static SubMenuState GetSubMenuState(byte val)
        {
            return val switch
            {
                SubMenuState_TitleScreen => SubMenuState.TitleScreen,
                SubMenuState_NonBox => SubMenuState.NonBox,
                SubMenuState_Box => SubMenuState.Box,
                _ => SubMenuState.Unknown,
            };
        }

        public static Vector3 GetUnionRoomCenter(UnitySceneStream destination)
        {
            return destination switch
            {
                UnitySceneStream.PokeCentreUpstairsLocal => LocalUnionRoomCenter,
                UnitySceneStream.PokeCentreDownstairsGlobal => GlobalUnionRoomCenter,
                _ => new Vector3(),
            };
        }
    }

    public enum UnitySceneStream : byte
    {
        LocalUnionRoom,
        PokeCentreUpstairsLocal,
        PokeCentreDownstairsGlobal,
        FullScene,
        Unknown
    }

    public enum SubMenuState : byte
    {
        TitleScreen,
        NonBox,
        Box,
        Unknown
    }
}
