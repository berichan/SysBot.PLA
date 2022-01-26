using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    public class PlaDecryptor
    {
        private static readonly byte[] BlockPosition =
        {
            0, 1, 2, 3,
            0, 1, 3, 2,
            0, 2, 1, 3,
            0, 3, 1, 2,
            0, 2, 3, 1,
            0, 3, 2, 1,
            1, 0, 2, 3,
            1, 0, 3, 2,
            2, 0, 1, 3,
            3, 0, 1, 2,
            2, 0, 3, 1,
            3, 0, 2, 1,
            1, 2, 0, 3,
            1, 3, 0, 2,
            2, 1, 0, 3,
            3, 1, 0, 2,
            2, 3, 0, 1,
            3, 2, 0, 1,
            1, 2, 3, 0,
            1, 3, 2, 0,
            2, 1, 3, 0,
            3, 1, 2, 0,
            2, 3, 1, 0,
            3, 2, 1, 0,

            // duplicates of 0-7 to eliminate modulus
            0, 1, 2, 3,
            0, 1, 3, 2,
            0, 2, 1, 3,
            0, 3, 1, 2,
            0, 2, 3, 1,
            0, 3, 2, 1,
            1, 0, 2, 3,
            1, 0, 3, 2,
        };

        internal static readonly byte[] blockPositionInvert =
        {
            0, 1, 2, 4, 3, 5, 6, 7, 12, 18, 13, 19, 8, 10, 14, 20, 16, 22, 9, 11, 15, 21, 17, 23,
            0, 1, 2, 4, 3, 5, 6, 7, // duplicates of 0-7 to eliminate modulus
        };

        public static byte[] ShuffleArray(ReadOnlySpan<byte> data, uint sv, int blockSize)
        {
            byte[] sdata = data.ToArray();
            uint index = sv * 4;
            const int start = 8;
            for (int block = 0; block < 4; block++)
            {
                int ofs = BlockPosition[index + block];
                var src = data.Slice(start + (blockSize * ofs), blockSize);
                var dest = sdata.AsSpan(start + (blockSize * block), blockSize);
                src.CopyTo(dest);
            }
            return sdata;
        }

        public static byte[] DecryptPLA(Span<byte> ekm)
        {
            uint pv = ReadUInt32LittleEndian(ekm);
            uint sv = pv >> 13 & 31;

            CryptPKM(ekm, pv, 88);
            return ShuffleArray(ekm, sv, 88);
        }

        public static byte[] EncryptPLA(Span<byte> pkm)
        {
            ushort check = GetCHK(pkm, 8 + (4 * 88));
            var bytesChk = BitConverter.GetBytes(check);
            pkm[0x06] = bytesChk[0];
            pkm[0x07] = bytesChk[1];

            uint pv = ReadUInt32LittleEndian(pkm);
            uint sv = pv >> 13 & 31;

            byte[] ekm = ShuffleArray(pkm, blockPositionInvert[sv], 88);
            CryptPKM(ekm, pv, 88);
            return ekm;
        }

        private static void CryptPKM(Span<byte> data, uint pv, int blockSize)
        {
            const int start = 8;
            int end = (4 * blockSize) + start;
            CryptArray(data, pv, start, end); // Blocks
            if (data.Length > end)
                CryptArray(data, pv, end, data.Length); // Party Stats
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CryptArray(Span<byte> data, uint seed, int start, int end)
        {
            int i = start;
            do // all block sizes are multiples of 4
            {
                Crypt(data[i..], ref seed); i += 2;
                Crypt(data[i..], ref seed); i += 2;
            }
            while (i < end);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Crypt(Span<byte> data, ref uint seed)
        {
            seed = (0x41C64E6D * seed) + 0x00006073;
            var current = ReadUInt16LittleEndian(data);
            current ^= (ushort)(seed >> 16);
            WriteUInt16LittleEndian(data, current);
        }

        public static ushort GetCHK(ReadOnlySpan<byte> data, int partyStart)
        {
            ushort chk = 0;
            for (int i = 8; i < partyStart; i += 2)
                chk += ReadUInt16LittleEndian(data[i..]);
            return chk;
        }

        //

        public static void DecryptIfEncrypted85(ref byte[] pkm)
        {
            var span = pkm.AsSpan();
            if (ReadUInt16LittleEndian(span[0x0A..]) != 0) // no held item for pla
                pkm = DecryptPLA(span);
        }
    }
}
