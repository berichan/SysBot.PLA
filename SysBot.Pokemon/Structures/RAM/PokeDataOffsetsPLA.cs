using System;
using System.Collections.Generic;
using System.Text;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public static class PokeDataOffsetsPLA
    {
        // Save region
        public static IReadOnlyList<long> BoxStartPokemonPointer = new long[] { 0x42BA6B0, 0x1F0, 0x68 };
        public static IReadOnlyList<long> TrainerIDPointer = new long[] { 0x42BA6B0, 0x218, 0x78 };
        public static IReadOnlyList<long> TradeSoftBanPointer = new long[] { 0x42BA6B0, 0x268, 0x70 }; // 0xA = banned

        // Trade
        public static IReadOnlyList<long> TradePartnerShowingPointer = new long[] { 0x42BD698, 0x188, 0xBC, 0xB0, 0x58, 0x00 };
        public static IReadOnlyList<long> TradePartnerIDPointer = new long[] { 0x42ED070, 0xC8, 0x78 };
        public static IReadOnlyList<long> TradePartnerNIDPointer = new long[] { 0x42EA508, 0xE0, 0x08 };

        // View
        public static IReadOnlyList<long> BoxOpenPointer = new long[] { 0x42ECFC0, 0x08, 0xC48, 0x498 };
        public static IReadOnlyList<long> PlayerCanMovePointer = new long[] { 0x42D4720, 0x18, 0x50, 0x78, 0x1E8 };

        public const int BoxFormatSlotSize = 0x168;
    }
}
