using System;
using System.Collections.Generic;
using System.Text;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public static class PokeDataOffsetsPLA
    {
        // Save region
        public static IReadOnlyList<long> BoxStartPokemonPointer = new long[] { 0x427C470, 0x1F0, 0x68 };
        public static IReadOnlyList<long> TrainerIDPointer = new long[] { 0x427C470, 0x218, 0x78 };
        public static IReadOnlyList<long> TradeSoftBanPointer = new long[] { 0x427C470, 0x268, 0x70 }; // 0xA = banned

        // Trade
        public static IReadOnlyList<long> TradePartnerShowingPointer = new long[] { 0x427F458, 0x188, 0xBC, 0xB0, 0x58, 0x00 };
        public static IReadOnlyList<long> TradePartnerIDPointer = new long[] { 0x42AECE0, 0xC8, 0x78 };
        public static IReadOnlyList<long> TradePartnerNIDPointer = new long[] { 0x42675A0, 0x80 };

        // View
        public static IReadOnlyList<long> BoxOpenPointer = new long[] { 0x42AEC30, 0x08, 0xC48, 0x498 };
        public static IReadOnlyList<long> PlayerCanMovePointer = new long[] { 0x42963A0, 0x18, 0x50, 0x78, 0x1E8 };

        public const int BoxFormatSlotSize = 0x168;
    }
}
