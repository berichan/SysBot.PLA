using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.BasePokeDataOffsetsBS;

namespace SysBot.Pokemon
{
    public abstract class PokeRoutineExecutor8BS : PokeRoutineExecutor<PB8>
    {
        protected const int HidWaitTime = 50;
        protected IPokeDataOffsetsBS Offsets { get; private set; } = new PokeDataOffsetsBS_BD();
        protected PokeRoutineExecutor8BS(PokeBotState cfg) : base(cfg) 
        {
            
        }

        protected async Task PointerPoke(byte[] bytes, IEnumerable<long> jumps, CancellationToken token)
        {
            byte[] command = Encoding.UTF8.GetBytes($"pointerPoke 0x{string.Concat(bytes.Select(z => $"{z:X2}"))}{string.Concat(jumps.Select(z => $" {z}"))}\r\n");
            await SwitchConnection.SendRaw(command, token).ConfigureAwait(false);
        }

        public override async Task<PB8> ReadPokemon(ulong offset, CancellationToken token) => await ReadPokemon(offset, BoxFormatSlotSize, token).ConfigureAwait(false);

        public override async Task<PB8> ReadPokemon(ulong offset, int size, CancellationToken token)
        {
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, size, token).ConfigureAwait(false);
            return new PB8(data);
        }

        public override async Task<PB8> ReadPokemonPointer(IEnumerable<long> jumps, int size, CancellationToken token)
        {
            var (valid, offset) = await ValidatePointerAll(jumps, token).ConfigureAwait(false);
            if (!valid)
                return new PB8();
            return await ReadPokemon(offset, token).ConfigureAwait(false);
        }

        public async Task<bool> ReadIsChanged(uint offset, byte[] original, CancellationToken token)
        {
            var result = await Connection.ReadBytesAsync(offset, original.Length, token).ConfigureAwait(false);
            return !result.SequenceEqual(original);
        }

        public override async Task<PB8> ReadBoxPokemon(int box, int slot, CancellationToken token)
        {
            var jumps = Offsets.BoxStartPokemonPointer.ToArray();
            return await ReadPokemonPointer(jumps, BoxFormatSlotSize, token).ConfigureAwait(false);
        }

        public async Task SetBoxPokemon(PB8 pkm, CancellationToken token, ITrainerInfo? sav = null)
        {
            if (sav != null)
            {
                // Update PKM to the current save's handler data
                DateTime Date = DateTime.Now;
                pkm.Trade(sav, Date.Day, Date.Month, Date.Year);
                pkm.RefreshChecksum();
            }

            pkm.ResetPartyStats();
            await PointerPoke(pkm.EncryptedPartyData, Offsets.BoxStartPokemonPointer, token).ConfigureAwait(false);
        }

        public async Task<SAV8BS> IdentifyTrainer(CancellationToken token)
        {
            // pull title so we know which set of offsets to use
            string title = Encoding.ASCII.GetString(await SwitchConnection.ReadRaw(SwitchCommand.GetTitleID(), 17, token).ConfigureAwait(false)).Trim();
            if (title == BrilliantDiamondID)
                Offsets = new PokeDataOffsetsBS_BD();
            else if (title == ShiningPearlID)
                Offsets = new PokeDataOffsetsBS_SP();
            else throw new Exception($"Title for {title} is unknown.");

            // generate a fake savefile
            var sav = await GetFakeTrainerSAV(token).ConfigureAwait(false);

            InitSaveData(sav);

            return sav;
        }

        public async Task<SAV8BS> GetFakeTrainerSAV(CancellationToken token)
        {
            var sav = new SAV8BS();
            var info = sav.MyStatus;

            // Set the OT.
            var name = await SwitchConnection.PointerPeek(TradePartnerPLA.MaxByteLengthStringObject, Offsets.MyStatusTrainerPointer, token).ConfigureAwait(false);
            info.OT = TradePartnerPLA.ReadStringFromRAMObject(name);

            // Set the TID, SID, and Language
            var id = await SwitchConnection.PointerPeek(10, Offsets.MyStatusTIDPointer, token).ConfigureAwait(false);
            info.TID = BitConverter.ToUInt16(id, 0);
            info.SID = BitConverter.ToUInt16(id, 2);

            var lang = await SwitchConnection.PointerPeek(1, Offsets.ConfigLanguagePointer, token).ConfigureAwait(false);
            sav.Language = lang[0];
            return sav;
        }

        public async Task InitializeHardware(IBotStateSettings settings, CancellationToken token)
        {
            Log("Detaching on startup.");
            await DetachController(token).ConfigureAwait(false);
            if (settings.ScreenOff)
            {
                Log("Turning off screen.");
                await SetScreen(ScreenState.Off, token).ConfigureAwait(false);
            }

            Log($"Setting BDSP-specific hid waits");
            await Connection.SendAsync(SwitchCommand.Configure(SwitchConfigureParameter.keySleepTime, HidWaitTime), token).ConfigureAwait(false);
            await Connection.SendAsync(SwitchCommand.Configure(SwitchConfigureParameter.pollRate, HidWaitTime), token).ConfigureAwait(false);
        }

        public async Task CleanExit(IBotStateSettings settings, CancellationToken token)
        {
            if (settings.ScreenOff)
            {
                Log("Turning on screen.");
                await SetScreen(ScreenState.On, token).ConfigureAwait(false);
            }
            Log("Detaching controllers on routine exit.");
            await DetachController(token).ConfigureAwait(false);
        }

        protected virtual async Task EnterLinkCode(int code, PokeTradeHubConfig config, CancellationToken token)
        {
            char[] codeChars = $"{code:00000000}".ToCharArray();
            HidKeyboardKey[] keysToPress = new HidKeyboardKey[codeChars.Length+1];
            for (int i = 0; i < codeChars.Length; ++i)
                keysToPress[i] = (HidKeyboardKey)Enum.Parse(typeof(HidKeyboardKey), (int)codeChars[i] >= (int)'A' && (int)codeChars[i] <= (int)'Z' ? $"{codeChars[i]}" : $"D{codeChars[i]}");
            keysToPress[codeChars.Length] = HidKeyboardKey.Return;

            await Connection.SendAsync(SwitchCommand.TypeMultipleKeys(keysToPress), token).ConfigureAwait(false);
            await Task.Delay((HidWaitTime * 8) + 0_100, token).ConfigureAwait(false);
            // Confirm Code outside of this method (allow synchronization)
        }

        public async Task Unban(CancellationToken token)
        {
            Log("Soft ban detected, unbanning.");
            // Write the float value to 0.
            var data = BitConverter.GetBytes(0);
            await PointerPoke(data, Offsets.UnionWorkPenaltyPointer, token).ConfigureAwait(false);
        }

        public async Task<bool> CheckIfSoftBanned(CancellationToken token)
        {
            var data = await SwitchConnection.PointerPeek(4, Offsets.UnionWorkPenaltyPointer, token).ConfigureAwait(false);
            return BitConverter.ToUInt32(data, 0) != 0;
        }

        public async Task ReOpenGame(PokeTradeHubConfig config, CancellationToken token)
        {
            // Reopen the game if we get soft-banned
            Log("Potential soft-ban detected, reopening game just in case!");
            await CloseGame(config, token).ConfigureAwait(false);
            await StartGame(config, token).ConfigureAwait(false);
        }

        public async Task CloseGame(PokeTradeHubConfig config, CancellationToken token)
        {
            var timing = config.Timings;
            // Close out of the game
            await Click(HOME, 2_000 + timing.ExtraTimeReturnHome, token).ConfigureAwait(false);
            await Click(X, 1_000, token).ConfigureAwait(false);
            await Click(A, 5_000 + timing.ExtraTimeCloseGame, token).ConfigureAwait(false);
            Log("Closed out of the game!");
        }

        public async Task StartGame(PokeTradeHubConfig config, CancellationToken token)
        {
            var timing = config.Timings;
            // Open game.
            await Click(A, 1_000 + timing.ExtraTimeLoadProfile, token).ConfigureAwait(false);

            // Menus here can go in the order: Update Prompt -> Profile -> DLC check -> Unable to use DLC.
            //  The user can optionally turn on the setting if they know of a breaking system update incoming.
            if (timing.AvoidSystemUpdate)
            {
                await Click(DUP, 0_600, token).ConfigureAwait(false);
                await Click(A, 1_000 + timing.ExtraTimeLoadProfile, token).ConfigureAwait(false);
            }

            await Click(A, 1_000 + timing.ExtraTimeCheckDLC, token).ConfigureAwait(false);
            // If they have DLC on the system and can't use it, requires an UP + A to start the game.
            // Should be harmless otherwise since they'll be in loading screen.
            await Click(DUP, 0_600, token).ConfigureAwait(false);
            await Click(A, 0_600, token).ConfigureAwait(false);

            Log("Restarting the game!");

            // Switch Logo lag, skip cutscene, game load screen
            await Task.Delay(22_000 + timing.ExtraTimeLoadGame, token).ConfigureAwait(false);

            for (int i = 0; i < 4; i++)
                await Click(A, 1_000, token).ConfigureAwait(false);

            var timer = 60_000;
            while (!await IsPlayerInstantiated(token).ConfigureAwait(false))
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                timer -= 1_000;
                // We haven't made it back to overworld after a minute, so press A every 6 seconds hoping to restart the game.
                // Don't risk it if hub is set to avoid updates.
                if (timer <= 0 && !timing.AvoidSystemUpdate)
                {
                    Log("Still not in the game, initiating rescue protocol!");
                    while (!await IsPlayerInstantiated(token).ConfigureAwait(false))
                        await Click(A, 6_000, token).ConfigureAwait(false);
                    break;
                }
            }

            await Task.Delay(5_000, token).ConfigureAwait(false);
            Log("Back in the overworld!");
        }

        protected async Task<bool> IsPlayerInstantiated(CancellationToken token)
        {
            var xVal = await SwitchConnection.PointerPeek(4, Offsets.PlayerPositionPointer, token).ConfigureAwait(false); // float but we only care whether or not it is all 0s
            var xParsed = BitConverter.ToUInt32(xVal, 0);
            return xParsed != 0;
        }

        private async Task<uint> GetSceneID(CancellationToken token)
        {
            var xVal = await SwitchConnection.PointerPeek(1, Offsets.SceneIDPointer, token).ConfigureAwait(false);
            var xParsed = BitConverter.ToUInt32(xVal, 0);
            return xParsed;
        }

        private async Task<bool> IsSceneID(uint expected, CancellationToken token) => await GetSceneID(token).ConfigureAwait(false) == expected;

        public async Task<bool> IsTrue(IEnumerable<long> jumps, CancellationToken token)
        {
            var data = await SwitchConnection.PointerPeek(1, jumps, token).ConfigureAwait(false);
            return data[0] == 1;
        }

        // Whenever we're in a trade, this pointer will be loaded, otherwise 0
        public async Task<bool> IsPartnerParamLoaded(CancellationToken token)
        {
            var byt = await SwitchConnection.PointerPeek(8, Offsets.LinkTradePartnerParamPointer, token).ConfigureAwait(false);
            return BitConverter.ToUInt64(byt, 0) != 0;
        }

        public async Task<UnitySceneStream> GetUnitySceneStream(CancellationToken token)
        {
            var byt = await SwitchConnection.PointerPeek(1, Offsets.UnitySceneStreamPointer, token).ConfigureAwait(false);
            return BasePokeDataOffsetsBS.GetUnitySceneStream(byt[0]);
        }

        public async Task<Vector3> GetPosition(CancellationToken token)
        {
            var pos = await SwitchConnection.PointerPeek(12, Offsets.PlayerPositionPointer, token).ConfigureAwait(false); // y is height in Unity
            return new Vector3(BitConverter.ToSingle(pos, 0), BitConverter.ToSingle(pos, 4), BitConverter.ToSingle(pos, 8));
        }

        public async Task SetPosition(Vector3 pos, CancellationToken token)
        {
            var bytesX = BitConverter.GetBytes(pos.X);
            var bytesY = BitConverter.GetBytes(pos.Y);
            var bytesZ = BitConverter.GetBytes(pos.Z);
            var allBytes = bytesX.Concat(bytesY).Concat(bytesZ).ToArray();
            await PointerPoke(allBytes, Offsets.PlayerPositionPointer, token).ConfigureAwait(false);
        }

        public async Task<Vector3> GetRotationEuler(CancellationToken token)
        {
            var pos = await SwitchConnection.PointerPeek(16, Offsets.PlayerRotationPointer, token).ConfigureAwait(false);
            Quaternion rotation = new Quaternion(BitConverter.ToSingle(pos, 0), BitConverter.ToSingle(pos, 4), BitConverter.ToSingle(pos, 8), BitConverter.ToSingle(pos, 12));
            return rotation.ToEulerAngles();
        }

        const float Deg2Rad = (float)Math.PI / 180.0f;
        public async Task SetRotationEuler(Vector3 rotationEuler, CancellationToken token)
        {
            Quaternion rot = Quaternion.FromEuler(rotationEuler.X * Deg2Rad, rotationEuler.Y * Deg2Rad, rotationEuler.Z * Deg2Rad);
            var bytesX = BitConverter.GetBytes(rot.X);
            var bytesY = BitConverter.GetBytes(rot.Y);
            var bytesZ = BitConverter.GetBytes(rot.Z);
            var bytesW = BitConverter.GetBytes(rot.W);
            var allBytes = bytesX.Concat(bytesY).Concat(bytesZ).Concat(bytesW).ToArray();
            await PointerPoke(allBytes, Offsets.PlayerRotationPointer, token).ConfigureAwait(false);
        }

        public async Task<ulong> GetTradePartnerNID(CancellationToken token) => BitConverter.ToUInt64(await SwitchConnection.PointerPeek(sizeof(ulong), Offsets.LinkTradePartnerNIDPointer, token).ConfigureAwait(false), 0);

        public async Task<bool> IsKeyboardOpen(CancellationToken token)
        {
            var commandBytes = Encoding.ASCII.GetBytes("isProgramRunning 0x0100000000001008\r\n");
            var isRunning = Encoding.ASCII.GetString(await SwitchConnection.ReadRaw(commandBytes, 17, token).ConfigureAwait(false));
            return ulong.Parse(isRunning.Trim(), System.Globalization.NumberStyles.HexNumber) == 1;
        }
    }

    public class Vector3
    {
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public float Z { get; set; } = 0;

        public Vector3(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }

        public Vector3() { }

        public override bool Equals(object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Vector3 p = (Vector3)obj;
                return (X == p.X) && (Y == p.Y) && (Z == p.Z);
            }
        }

        public override int GetHashCode() // VS Automatic Generation
        {
            int hashCode = -307843816;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Z.GetHashCode();
            return hashCode;
        }
    }

    public class Quaternion
    {
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public float Z { get; set; } = 0;
        public float W { get; set; } = 1; // for an identity quaternion

        public Quaternion(float x, float y, float z, float w)
        {
            X = x; Y = y; Z = z; W = w;
        }

        public Quaternion() { }

        public Vector3 ToEulerAngles()
        {
            Vector3 angles = new Vector3();

            // roll (x-axis rotation)
            double sinr_cosp = 2 * (W * X + Y * Z);
            double cosr_cosp = 1 - 2 * (X * X + Y * Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            double sinp = 2 * (W * Y - Z * X);
            if (Math.Abs(sinp) >= 1)
                angles.Y = sinp > 0 ? (float)Math.PI / 2f : -((float)Math.PI / 2f); // use 90 degrees if out of range
            else
                angles.Y = (float)Math.Asin(sinp);

            // yaw (z-axis rotation)
            double siny_cosp = 2 * (W * Z + X * Y);
            double cosy_cosp = 1 - 2 * (Y * Y + Z * Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }

        // https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles
        public static Quaternion FromEuler(float x, float y, float z) // yaw (Z), pitch (Y), roll (X)
        {
            float cy = (float)Math.Cos(z * 0.5);
            float sy = (float)Math.Sin(z * 0.5);
            float cp = (float)Math.Cos(y * 0.5);
            float sp = (float)Math.Sin(y * 0.5);
            float cr = (float)Math.Cos(x * 0.5);
            float sr = (float)Math.Sin(x * 0.5);

            Quaternion q = new()
            {
                W = cr * cp * cy + sr * sp * sy,
                X = sr * cp * cy - cr * sp * sy,
                Y = cr * sp * cy + sr * cp * sy,
                Z = cr * cp * sy - sr * sp * cy
            };

            return q;
        }
    }
}
