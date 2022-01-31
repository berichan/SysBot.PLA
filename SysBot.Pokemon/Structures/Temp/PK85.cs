using System;
using static System.Buffers.Binary.BinaryPrimitives;
using PKHeX.Core;

namespace PKHeX.Core
{
    public class PK85 : PKM,
        IHyperTrain, IScaledSize, IGigantamax, IFavorite, IHandlerLanguage, IFormArgument, IHomeTrack, IBattleVersion, ITrainerMemories
    {
        public const int SIZE_PARTYPLA = SIZE_STOREDPLA;
        public const int SIZE_STOREDPLA = 0x168;

        public sealed override int Format => 85;
        public PK85() : base(SIZE_PARTYPLA) { }
        public PK85(byte[] data) : base(DecryptParty(data)) { }

        private static byte[] DecryptParty(byte[] data)
        {
            PlaDecryptor.DecryptIfEncrypted85(ref data);
            Array.Resize(ref data, SIZE_PARTYPLA);
            return data;
        }

        protected override ushort CalculateChecksum()
        {
            ushort chk = 0;
            for (int i = 8; i < SIZE_STOREDPLA; i += 2)
                chk += ReadUInt16LittleEndian(Data.AsSpan(i));
            return chk;
        }

        // Simple Generated Attributes
        public override int CurrentFriendship
        {
            get => CurrentHandler == 0 ? OT_Friendship : HT_Friendship;
            set { if (CurrentHandler == 0) OT_Friendship = value; else HT_Friendship = value; }
        }

        private string GetString(int offset, int count) => StringConverter.GetString7b(Data, offset, count);
        private static byte[] SetString(string value, int maxLength) => StringConverter.SetString7b(value, maxLength);

        public override int SIZE_PARTY => SIZE_PARTYPLA;
        public override int SIZE_STORED => SIZE_STOREDPLA;

        public sealed override bool ChecksumValid => CalculateChecksum() == Checksum;
        public sealed override void RefreshChecksum() => Checksum = CalculateChecksum();
        public sealed override bool Valid { get => Sanity == 0 && ChecksumValid; set { if (!value) return; Sanity = 0; RefreshChecksum(); } }

        // Trash Bytes
        public override Span<byte> Nickname_Trash { get => Data.AsSpan(0x60, 26); set { if (value.Length == 26) value.CopyTo(Data.AsSpan(0x60)); } }
        public override Span<byte> HT_Trash { get => Data.AsSpan(0xB8, 26); set { if (value.Length == 26) value.CopyTo(Data.AsSpan(0xB8)); } }
        public override Span<byte> OT_Trash { get => Data.AsSpan(0x110, 26); set { if (value.Length == 26) value.CopyTo(Data.AsSpan(0x110)); } }

        // Maximums
        public override int MaxIV => 31;
        public override int MaxEV => 252;
        public override int OTLength => 12;
        public override int NickLength => 12;

        public override int PSV => (int)((PID >> 16 ^ (PID & 0xFFFF)) >> 4);
        public override int TSV => (TID ^ SID) >> 4;
        public override bool IsUntraded => Data[0xA8] == 0 && Data[0xA8 + 1] == 0 && Format == Generation; // immediately terminated HT_Name data (\0)

        // Complex Generated Attributes
        public override int Characteristic
        {
            get
            {
                int pm6 = (int)(EncryptionConstant % 6);
                int maxIV = MaximumIV;
                int pm6stat = 0;
                for (int i = 0; i < 6; i++)
                {
                    pm6stat = (pm6 + i) % 6;
                    if (GetIV(pm6stat) == maxIV)
                        break;
                }
                return (pm6stat * 5) + (maxIV % 5);
            }
        }

        // Methods
        protected override byte[] Encrypt()
        {
            RefreshChecksum();
            return PlaDecryptor.EncryptPLA(Data);
        }

        public void FixRelearn()
        {
            while (true)
            {
                if (RelearnMove4 != 0 && RelearnMove3 == 0)
                {
                    RelearnMove3 = RelearnMove4;
                    RelearnMove4 = 0;
                }
                if (RelearnMove3 != 0 && RelearnMove2 == 0)
                {
                    RelearnMove2 = RelearnMove3;
                    RelearnMove3 = 0;
                    continue;
                }
                if (RelearnMove2 != 0 && RelearnMove1 == 0)
                {
                    RelearnMove1 = RelearnMove2;
                    RelearnMove2 = 0;
                    continue;
                }
                break;
            }
        }

        public override uint EncryptionConstant { get => ReadUInt32LittleEndian(Data.AsSpan(0x00)); set => WriteUInt32LittleEndian(Data.AsSpan(0x00), value); }
        public override ushort Sanity { get => ReadUInt16LittleEndian(Data.AsSpan(0x04)); set => WriteUInt16LittleEndian(Data.AsSpan(0x04), value); }
        public override ushort Checksum { get => ReadUInt16LittleEndian(Data.AsSpan(0x06)); set => WriteUInt16LittleEndian(Data.AsSpan(0x06), value); }

        // Structure
        public override int Species { get => ReadUInt16LittleEndian(Data.AsSpan(0x08)); set => WriteUInt16LittleEndian(Data.AsSpan(0x08), (ushort)value); }
        public override int HeldItem { get => ReadUInt16LittleEndian(Data.AsSpan(0x0A)); set => WriteUInt16LittleEndian(Data.AsSpan(0x0A), (ushort)value); }
        public override int TID { get => ReadUInt16LittleEndian(Data.AsSpan(0x0C)); set => WriteUInt16LittleEndian(Data.AsSpan(0x0C), (ushort)value); }
        public override int SID { get => ReadUInt16LittleEndian(Data.AsSpan(0x0E)); set => WriteUInt16LittleEndian(Data.AsSpan(0x0E), (ushort)value); }
        public override uint EXP { get => ReadUInt32LittleEndian(Data.AsSpan(0x10)); set => WriteUInt32LittleEndian(Data.AsSpan(0x10), value); }
        public override int Ability { get => ReadUInt16LittleEndian(Data.AsSpan(0x14)); set => WriteUInt16LittleEndian(Data.AsSpan(0x14), (ushort)value); }
        public override int AbilityNumber { get => Data[0x16] & 7; set => Data[0x16] = (byte)((Data[0x16] & ~7) | (value & 7)); }
        public bool Favorite { get => (Data[0x16] & 8) != 0; set => Data[0x16] = (byte)((Data[0x16] & ~8) | ((value ? 1 : 0) << 3)); } // unused, was in LGPE but not in SWSH
        public bool CanGigantamax { get => (Data[0x16] & 16) != 0; set => Data[0x16] = (byte)((Data[0x16] & ~16) | (value ? 16 : 0)); }
        public override int MarkValue { get => ReadUInt16LittleEndian(Data.AsSpan(0x18)); protected set => WriteUInt16LittleEndian(Data.AsSpan(0x18), (ushort)value); }
        // 0x1A alignment unused
        // 0x1B alignment unused
        public override uint PID { get => ReadUInt32LittleEndian(Data.AsSpan(0x1C)); set => WriteUInt32LittleEndian(Data.AsSpan(0x1C), value); }
        public override int Nature { get => Data[0x20]; set => Data[0x20] = (byte)value; }
        public override int StatNature { get => Data[0x21]; set => Data[0x21] = (byte)value; }
        public override bool FatefulEncounter { get => (Data[0x22] & 1) == 1; set => Data[0x22] = (byte)((Data[0x22] & ~0x01) | (value ? 1 : 0)); }
        public bool Flag2 { get => (Data[0x22] & 2) == 2; set => Data[0x22] = (byte)((Data[0x22] & ~0x02) | (value ? 2 : 0)); }
        public override int Gender { get => (Data[0x22] >> 2) & 0x3; set => Data[0x22] = (byte)((Data[0x22] & 0xF3) | (value << 2)); }
        // 0x23 alignment unused

        public override int Form { get => ReadUInt16LittleEndian(Data.AsSpan(0x24)); set => WriteUInt16LittleEndian(Data.AsSpan(0x24), (ushort)value); }
        public override int EV_HP { get => Data[0x26]; set => Data[0x26] = (byte)value; }
        public override int EV_ATK { get => Data[0x27]; set => Data[0x27] = (byte)value; }
        public override int EV_DEF { get => Data[0x28]; set => Data[0x28] = (byte)value; }
        public override int EV_SPE { get => Data[0x29]; set => Data[0x29] = (byte)value; }
        public override int EV_SPA { get => Data[0x2A]; set => Data[0x2A] = (byte)value; }
        public override int EV_SPD { get => Data[0x2B]; set => Data[0x2B] = (byte)value; }

        private byte PKRS { get => Data[0x32]; set => Data[0x32] = value; }
        public override int PKRS_Days { get => PKRS & 0xF; set => PKRS = (byte)((PKRS & ~0xF) | value); }
        public override int PKRS_Strain { get => PKRS >> 4; set => PKRS = (byte)((PKRS & 0xF) | value << 4); }

        public bool HasMark()
        {
            var d = Data.AsSpan();
            if ((ReadUInt16LittleEndian(d[0x3A..]) & 0xFFE0) != 0)
                return true;
            if (ReadUInt32LittleEndian(d[0x40..]) != 0)
                return true;
            return (d[0x44] & 3) != 0;
        }

        public uint Sociability { get => ReadUInt32LittleEndian(Data.AsSpan(0x48)); set => WriteUInt32LittleEndian(Data.AsSpan(0x48), value); }

        // 0x4C-0x4F unused

        public int HeightScalar { get => Data[0x50]; set => Data[0x50] = (byte)value; }
        public int WeightScalar { get => Data[0x51]; set => Data[0x51] = (byte)value; }

        public override string Nickname
        {
            get => GetString(0x60, 24);
            set => SetString(value, 12).CopyTo(Data, 0x60);
        }

        // 2 bytes for \0, automatically handled above

        public override int Move1 { get => ReadUInt16LittleEndian(Data.AsSpan(0x54)); set => WriteUInt16LittleEndian(Data.AsSpan(0x54), (ushort)value); }
        public override int Move2 { get => ReadUInt16LittleEndian(Data.AsSpan(0x56)); set => WriteUInt16LittleEndian(Data.AsSpan(0x56), (ushort)value); }
        public override int Move3 { get => ReadUInt16LittleEndian(Data.AsSpan(0x58)); set => WriteUInt16LittleEndian(Data.AsSpan(0x58), (ushort)value); }
        public override int Move4 { get => ReadUInt16LittleEndian(Data.AsSpan(0x5A)); set => WriteUInt16LittleEndian(Data.AsSpan(0x5A), (ushort)value); }

        public override int Move1_PP { get => Data[0x5C]; set => Data[0x5C] = (byte)value; }
        public override int Move2_PP { get => Data[0x5D]; set => Data[0x5D] = (byte)value; }
        public override int Move3_PP { get => Data[0x5E]; set => Data[0x5E] = (byte)value; }
        public override int Move4_PP { get => Data[0x5F]; set => Data[0x5F] = (byte)value; }

        public override int Stat_HPCurrent
        {
            get => BitConverter.ToUInt16(Data, 0x92); set => BitConverter.GetBytes((ushort)value).CopyTo(Data, 0x92);
        }

        private uint IV32 { get => BitConverter.ToUInt32(Data, 0x94); set => BitConverter.GetBytes(value).CopyTo(Data, 0x94); }
        public override int IV_HP { get => (int)(IV32 >> 00) & 0x1F; set => IV32 = (IV32 & ~(0x1Fu << 00)) | ((value > 31 ? 31u : (uint)value) << 00); }
        public override int IV_ATK { get => (int)(IV32 >> 05) & 0x1F; set => IV32 = (IV32 & ~(0x1Fu << 05)) | ((value > 31 ? 31u : (uint)value) << 05); }
        public override int IV_DEF { get => (int)(IV32 >> 10) & 0x1F; set => IV32 = (IV32 & ~(0x1Fu << 10)) | ((value > 31 ? 31u : (uint)value) << 10); }
        public override int IV_SPE { get => (int)(IV32 >> 15) & 0x1F; set => IV32 = (IV32 & ~(0x1Fu << 15)) | ((value > 31 ? 31u : (uint)value) << 15); }
        public override int IV_SPA { get => (int)(IV32 >> 20) & 0x1F; set => IV32 = (IV32 & ~(0x1Fu << 20)) | ((value > 31 ? 31u : (uint)value) << 20); }
        public override int IV_SPD { get => (int)(IV32 >> 25) & 0x1F; set => IV32 = (IV32 & ~(0x1Fu << 25)) | ((value > 31 ? 31u : (uint)value) << 25); }
        public override bool IsEgg { get => ((IV32 >> 30) & 1) == 1; set => IV32 = (IV32 & ~0x40000000u) | (value ? 0x40000000u : 0u); }
        public override bool IsNicknamed { get => ((IV32 >> 31) & 1) == 1; set => IV32 = (IV32 & 0x7FFFFFFFu) | (value ? 0x80000000u : 0u); }

        public override int Status_Condition { get => BitConverter.ToInt32(Data, 0x9C); set => BitConverter.GetBytes(value).CopyTo(Data, 0x9C); }

        public override string HT_Name { get => GetString(0xB8, 24); set => SetString(value, 12).CopyTo(Data, 0xB8); }
        public override int HT_Gender { get => Data[0xD2]; set => Data[0xD2] = (byte)value; }
        public int HT_Language { get => Data[0xD3]; set => Data[0xD3] = (byte)value; }
        public override int CurrentHandler { get => Data[0xD4]; set => Data[0xD4] = (byte)value; }
        public int HT_TrainerID { get => BitConverter.ToUInt16(Data, 0xD6); set => BitConverter.GetBytes((ushort)value).CopyTo(Data, 0xD6); } // unused?
        public override int HT_Friendship { get => Data[0xD8]; set => Data[0xD8] = (byte)value; }
        public int HT_Intensity { get => Data[0xD9]; set => Data[0xD9] = (byte)value; }
        public int HT_Memory { get => Data[0xDA]; set => Data[0xDA] = (byte)value; }
        public int HT_Feeling { get => Data[0xDB]; set => Data[0xDB] = (byte)value; }
        public int HT_TextVar { get => BitConverter.ToUInt16(Data, 0xDC); set => BitConverter.GetBytes((ushort)value).CopyTo(Data, 0xDC); }

        public override byte Fullness { get => Data[0xEC]; set => Data[0xEC] = value; }
        public override byte Enjoyment { get => Data[0xED]; set => Data[0xED] = value; }
        public override int Version { get => Data[0xEE]; set => Data[0xEE] = (byte)value; }
        public int BattleVersion { get => Data[0xEF]; set => Data[0xEF] = (byte)value; }
        // public override int Region { get => Data[0xE0]; set => Data[0xE0] = (byte)value; }
        // public override int ConsoleRegion { get => Data[0xE1]; set => Data[0xE1] = (byte)value; }
        public override int Language { get => Data[0xF2]; set => Data[0xF2] = (byte)value; }
        public uint FormArgument { get => BitConverter.ToUInt32(Data, 0xF4); set => BitConverter.GetBytes(value).CopyTo(Data, 0xF4); }
        public byte FormArgumentRemain { get => (byte)FormArgument; set => FormArgument = (FormArgument & ~0xFFu) | value; }
        public byte FormArgumentElapsed { get => (byte)(FormArgument >> 8); set => FormArgument = (FormArgument & ~0xFF00u) | (uint)(value << 8); }
        public byte FormArgumentMaximum { get => (byte)(FormArgument >> 16); set => FormArgument = (FormArgument & ~0xFF0000u) | (uint)(value << 16); }
        public sbyte AffixedRibbon { get => (sbyte)Data[0xF8]; set => Data[0xF8] = (byte)value; } // selected ribbon

        public override string OT_Name { get => GetString(0x110, 24); set => SetString(value, 12).CopyTo(Data, 0x110); }
        public override int OT_Friendship { get => Data[0x12A]; set => Data[0x12A] = (byte)value; }
        public int OT_Intensity { get => Data[0x12B]; set => Data[0x12B] = (byte)value; }
        public int OT_Memory { get => Data[0x12C]; set => Data[0x12C] = (byte)value; }
        // 0x115 unused align
        public int OT_TextVar { get => BitConverter.ToUInt16(Data, 0x12E); set => BitConverter.GetBytes((ushort)value).CopyTo(Data, 0x12E); }
        public int OT_Feeling { get => Data[0x130]; set => Data[0x130] = (byte)value; }
        public override int Egg_Year { get => Data[0x131]; set => Data[0x131] = (byte)value; }
        public override int Egg_Month { get => Data[0x132]; set => Data[0x132] = (byte)value; }
        public override int Egg_Day { get => Data[0x133]; set => Data[0x133] = (byte)value; }
        public override int Met_Year { get => Data[0x134]; set => Data[0x134] = (byte)value; }
        public override int Met_Month { get => Data[0x135]; set => Data[0x135] = (byte)value; }
        public override int Met_Day { get => Data[0x136]; set => Data[0x136] = (byte)value; }
        // 0x11F unused align
        public override int Egg_Location { get => BitConverter.ToUInt16(Data, 0x138); set => BitConverter.GetBytes((ushort)value).CopyTo(Data, 0x138); }
        public override int Met_Location { get => BitConverter.ToUInt16(Data, 0x13A); set => BitConverter.GetBytes((ushort)value).CopyTo(Data, 0x13A); }
        public override int Ball { get => Data[0x137]; set => Data[0x137] = (byte)value; }
        public override int Met_Level { get => Data[0x13D] & ~0x80; set => Data[0x13D] = (byte)((Data[0x13D] & 0x80) | value); }
        public override int OT_Gender { get => Data[0x13D] >> 7; set => Data[0x13D] = (byte)((Data[0x13D] & ~0x80) | (value << 7)); }
        public int HyperTrainFlags { get => Data[0x13E]; set => Data[0x13E] = (byte)value; }
        public bool HT_HP { get => ((HyperTrainFlags >> 0) & 1) == 1; set => HyperTrainFlags = (HyperTrainFlags & ~(1 << 0)) | ((value ? 1 : 0) << 0); }
        public bool HT_ATK { get => ((HyperTrainFlags >> 1) & 1) == 1; set => HyperTrainFlags = (HyperTrainFlags & ~(1 << 1)) | ((value ? 1 : 0) << 1); }
        public bool HT_DEF { get => ((HyperTrainFlags >> 2) & 1) == 1; set => HyperTrainFlags = (HyperTrainFlags & ~(1 << 2)) | ((value ? 1 : 0) << 2); }
        public bool HT_SPA { get => ((HyperTrainFlags >> 3) & 1) == 1; set => HyperTrainFlags = (HyperTrainFlags & ~(1 << 3)) | ((value ? 1 : 0) << 3); }
        public bool HT_SPD { get => ((HyperTrainFlags >> 4) & 1) == 1; set => HyperTrainFlags = (HyperTrainFlags & ~(1 << 4)) | ((value ? 1 : 0) << 4); }
        public bool HT_SPE { get => ((HyperTrainFlags >> 5) & 1) == 1; set => HyperTrainFlags = (HyperTrainFlags & ~(1 << 5)) | ((value ? 1 : 0) << 5); }

        public bool GetMoveRecordFlag(int index)
        {
            if ((uint)index > 112) // 14 bytes, 8 bits
                throw new ArgumentOutOfRangeException(nameof(index));
            int ofs = index >> 3;
            return FlagUtil.GetFlag(Data, 0x13F + ofs, index & 7);
        }

        public void SetMoveRecordFlag(int index, bool value)
        {
            if ((uint)index > 112) // 14 bytes, 8 bits
                throw new ArgumentOutOfRangeException(nameof(index));
            int ofs = index >> 3;
            FlagUtil.SetFlag(Data, 0x13F + ofs, index & 7, value);
        }

        public bool HasAnyMoveRecordFlag() => Array.FindIndex(Data, 0x13F, 14, z => z != 0) >= 0;

        public override PKM Clone()
        {
            return new PK85((byte[])Data.Clone());
        }

        // Why did you mis-align this field, GameFreak?
        public ulong Tracker
        {
            get => ReadUInt64LittleEndian(Data.AsSpan(0x14D));
            set => WriteUInt64LittleEndian(Data.AsSpan(0x14D), value);
        }

        public override int Stat_Level { get => Data[0x168]; set => Data[0x168] = (byte)value; }
        // 0x149 unused alignment
        public override int Stat_HPMax { get => ReadUInt16LittleEndian(Data.AsSpan(0x16A)); set => WriteUInt16LittleEndian(Data.AsSpan(0x16A), (ushort)value); }
        public override int Stat_ATK { get => ReadUInt16LittleEndian(Data.AsSpan(0x16C)); set => WriteUInt16LittleEndian(Data.AsSpan(0x16C), (ushort)value); }
        public override int Stat_DEF { get => ReadUInt16LittleEndian(Data.AsSpan(0x16E)); set => WriteUInt16LittleEndian(Data.AsSpan(0x16E), (ushort)value); }
        public override int Stat_SPE { get => ReadUInt16LittleEndian(Data.AsSpan(0x160)); set => WriteUInt16LittleEndian(Data.AsSpan(0x160), (ushort)value); }
        public override int Stat_SPA { get => ReadUInt16LittleEndian(Data.AsSpan(0x162)); set => WriteUInt16LittleEndian(Data.AsSpan(0x162), (ushort)value); }
        public override int Stat_SPD { get => ReadUInt16LittleEndian(Data.AsSpan(0x164)); set => WriteUInt16LittleEndian(Data.AsSpan(0x164), (ushort)value); }

        public override PersonalInfo PersonalInfo => throw new NotImplementedException();

        public override int Move1_PPUps { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override int Move2_PPUps { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override int Move3_PPUps { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override int Move4_PPUps { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override int MaxMoveID => 999;

        public override int MaxSpeciesID => 999;

        public override int MaxItemID => 999;

        public override int MaxAbilityID => 999;

        public override int MaxBallID => 999;

        public override int MaxGameID => 49;

        private bool TradeOT(ITrainerInfo tr)
        {
            // Check to see if the OT matches the SAV's OT info.
            if (!(tr.OT == OT_Name && tr.TID == TID && tr.SID == SID && tr.Gender == OT_Gender))
                return false;

            CurrentHandler = 0;
            return true;
        }

        private void TradeHT(ITrainerInfo tr)
        {
            if (HT_Name != tr.OT)
            {
                HT_Friendship = 50;
                HT_Name = tr.OT;
            }
            CurrentHandler = 1;
            HT_Gender = tr.Gender;
            HT_Language = tr.Language;
            //this.SetTradeMemoryHT8();
        }

        public void Trade(ITrainerInfo tr, int Day = 1, int Month = 1, int Year = 2015)
        {
            // Process to the HT if the OT of the Pokémon does not match the SAV's OT info.
            if (!TradeOT(tr))
                TradeHT(tr);
        }
    }
}