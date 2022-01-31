using System;
using System.Collections.Generic;
using System.Text;
using PKHeX.Core;

namespace SysBot.Pokemon
{
    public static class PokeDataOffsetsPLA
    {
        // Main region offsets
        public const ulong IsConnectionState = 0x605FD58; // 0x02 when connection state is active

        // Pointers
        public static IReadOnlyList<long> BoxStartPokemonPointer = new long[] { 0x427B470, 0x1F0, 0x68 };

        public static IReadOnlyList<long> TradePartnerShowingPointer = new long[] { 0x427F888, 0x188, 0x78, 0xB0, 0x58, 0x00 };
        public static IReadOnlyList<long> TradePartnerIDPointer = new long[] { 0x42ADCE0, 0xC8, 0x78 };
        public static IReadOnlyList<long> TradePartnerNIDPointer = new long[] { 0x42665A0, 0x80 };

        public static IReadOnlyList<long> BoxOpenPointer = new long[] { 0x42ADC30, 0x08, 0xC48, 0x498 };

        public static IReadOnlyList<long> PlayerCanMovePointer = new long[] { 0x42953A0, 0x18, 0x50, 0x78, 0x1E8 };

        public const int BoxFormatSlotSize = PK85.SIZE_PARTYPLA;
    }
}
