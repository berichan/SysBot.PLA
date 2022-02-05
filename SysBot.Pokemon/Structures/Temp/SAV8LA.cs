using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace PKHeX.Core
{
    public class SAV8LA : SaveFile
    {
        public override string Extension => string.Empty;

        public override bool ChecksumsValid => true;

        public override string ChecksumInfo => string.Empty;

        public override int Generation => 85;

        public override PersonalTable Personal => throw new NotImplementedException();

        public override int OTLength => 12;

        public override int NickLength => 10;

        public override int MaxMoveID => 999;

        public override int MaxSpeciesID => 999;

        public override int MaxAbilityID => 999;

        public override int MaxItemID => 9999;

        public override int MaxBallID => 999;

        public override int MaxGameID => 49;

        public override int BoxCount => 32;

        public override Type PKMType => typeof(PA8);

        public override PKM BlankPKM => new PA8();

        public override int MaxEV => throw new NotImplementedException();

        public override IReadOnlyList<ushort> HeldItems => throw new NotImplementedException();

        protected override string ShortSummary => String.Empty;

        protected override int SIZE_STORED => PA8.SIZE_STOREDPLA;

        protected override int SIZE_PARTY => PA8.SIZE_PARTYPLA;

        public override string GetBoxName(int box)
        {
            throw new NotImplementedException();
        }

        public override int GetBoxOffset(int box)
        {
            throw new NotImplementedException();
        }

        public override int GetPartyOffset(int slot)
        {
            throw new NotImplementedException();
        }

        public override string GetString(byte[] data, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public override void SetBoxName(int box, string value)
        {
            throw new NotImplementedException();
        }

        public override byte[] SetString(string value, int maxLength, int PadToSize = 0, ushort PadWith = 0)
        {
            throw new NotImplementedException();
        }

        protected override SaveFile CloneInternal()
        {
            throw new NotImplementedException();
        }

        protected override byte[] DecryptPKM(byte[] data)
        {
            PlaDecryptor.DecryptIfEncrypted85(ref data);
            Array.Resize(ref data, SIZE_PARTY);
            return data;
        }

        protected override PKM GetPKM(byte[] data)
        {
            return new PA8(data);
        }

        protected override void SetChecksums()
        {
            throw new NotImplementedException();
        }

        public override string OT => "berichan";

        public override int TID => 420;
        public override int SID => 69;
    }
}
