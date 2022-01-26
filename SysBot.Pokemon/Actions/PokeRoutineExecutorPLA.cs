using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;
using static SysBot.Base.SwitchButton;
using static SysBot.Pokemon.PokeDataOffsetsPLA;

namespace SysBot.Pokemon
{
    public abstract class PokeRoutineExecutorPLA : PokeRoutineExecutor<PK85>
    {
        protected const int HidWaitTime = 50;

        protected PokeRoutineExecutorPLA(PokeBotState cfg) : base(cfg)
        {

        }

        public override async Task<PK85> ReadPokemon(ulong offset, CancellationToken token) => await ReadPokemon(offset, BoxFormatSlotSize, token).ConfigureAwait(false);

        public override async Task<PK85> ReadPokemon(ulong offset, int size, CancellationToken token)
        {
            var data = await SwitchConnection.ReadBytesAbsoluteAsync(offset, size, token).ConfigureAwait(false);
            return new PK85(data);
        }

        public override async Task<PK85> ReadPokemonPointer(IEnumerable<long> jumps, int size, CancellationToken token)
        {
            var (valid, offset) = await ValidatePointerAll(jumps, token).ConfigureAwait(false);
            if (!valid)
                return new PK85();
            return await ReadPokemon(offset, token).ConfigureAwait(false);
        }

        public async Task<bool> ReadIsChanged(uint offset, byte[] original, CancellationToken token)
        {
            var result = await Connection.ReadBytesAsync(offset, original.Length, token).ConfigureAwait(false);
            return !result.SequenceEqual(original);
        }

        public override async Task<PK85> ReadBoxPokemon(int box, int slot, CancellationToken token)
        {
            return await ReadPokemonPointer(BoxStartPokemonPointer, BoxFormatSlotSize, token).ConfigureAwait(false);
        }

        public async Task SetBoxPokemon(PK85 pkm, CancellationToken token, ITrainerInfo? sav = null)
        {
            if (sav != null)
            {
                // Update PKM to the current save's handler data
                DateTime Date = DateTime.Now;
                pkm.Trade(sav, Date.Day, Date.Month, Date.Year);
                pkm.RefreshChecksum();
            }

            pkm.ResetPartyStats();
            await SwitchConnection.PointerPoke(pkm.EncryptedPartyData, BoxStartPokemonPointer, token).ConfigureAwait(false);
        }

        public async Task<SAV85PLA> IdentifyTrainer(CancellationToken token)
        {
            // generate a fake savefile
            var sav = await GetFakeTrainerSAV(token).ConfigureAwait(false);

            InitSaveData(sav);

            return sav;
        }

        public async Task<SAV85PLA> GetFakeTrainerSAV(CancellationToken token)
        {
            return new SAV85PLA();
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

            Log($"Setting PLA-specific hid waits");
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
            HidKeyboardKey[] keysToPress = new HidKeyboardKey[codeChars.Length + 1];
            for (int i = 0; i < codeChars.Length; ++i)
                keysToPress[i] = (HidKeyboardKey)Enum.Parse(typeof(HidKeyboardKey), (int)codeChars[i] >= (int)'A' && (int)codeChars[i] <= (int)'Z' ? $"{codeChars[i]}" : $"D{codeChars[i]}");
            keysToPress[codeChars.Length] = HidKeyboardKey.Return;

            await Connection.SendAsync(SwitchCommand.TypeMultipleKeys(keysToPress), token).ConfigureAwait(false);
            await Task.Delay((HidWaitTime * 8) + 0_100, token).ConfigureAwait(false);
            // Confirm Code outside of this method (allow synchronization)
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
            while (!await IsInGame(token).ConfigureAwait(false))
            {
                await Task.Delay(1_000, token).ConfigureAwait(false);
                timer -= 1_000;
                // We haven't made it back to overworld after a minute, so press A every 6 seconds hoping to restart the game.
                // Don't risk it if hub is set to avoid updates.
                if (timer <= 0 && !timing.AvoidSystemUpdate)
                {
                    Log("Still not in the game, initiating rescue protocol!");
                    while (!await IsInGame(token).ConfigureAwait(false))
                        await Click(A, 6_000, token).ConfigureAwait(false);
                    break;
                }
            }

            await Task.Delay(5_000, token).ConfigureAwait(false);
            Log("Back in the overworld!");
        }

        protected async Task<bool> IsInGame(CancellationToken token)
        {
            (var valid, _) = await ValidatePointerAll(PlayerCanMovePointer, token).ConfigureAwait(false);
            return valid;
        }

        protected async Task<bool> IsInBox(CancellationToken token)
        {
            (var valid, var offs) = await ValidatePointerAll(BoxOpenPointer, token).ConfigureAwait(false);
            if (!valid)
                return false;

            var bytes = await SwitchConnection.ReadBytesAbsoluteAsync(offs, 4, token).ConfigureAwait(false);
            var boxOpenState = BitConverter.ToUInt32(bytes, 0);
            return boxOpenState == 1;
        }

        //public async Task<ulong> GetTradePartnerNID(CancellationToken token) => BitConverter.ToUInt64(await SwitchConnection.PointerPeek(sizeof(ulong), Offsets.LinkTradePartnerNIDPointer, token).ConfigureAwait(false), 0);

        public async Task<bool> IsConnectionPresent(CancellationToken token)
        {
            var bytes = await SwitchConnection.ReadBytesAbsoluteAsync(IsConnectionState, 4, token).ConfigureAwait(false);
            var connectState = BitConverter.ToUInt32(bytes, 0);
            return connectState != 0;
        }

        public async Task<bool> CanPlayerMove(CancellationToken token)
        {
            (var valid, var offs) = await ValidatePointerAll(PlayerCanMovePointer, token).ConfigureAwait(false);
            if (!valid)
                return false;

            var bytes = await SwitchConnection.ReadBytesAbsoluteAsync(offs, 4, token).ConfigureAwait(false);
            var canMoveState = BitConverter.ToUInt32(bytes, 0);
            return canMoveState == 0;
        }

        public async Task<bool> IsKeyboardOpen(CancellationToken token)
        {
            var commandBytes = Encoding.ASCII.GetBytes("isProgramRunning 0x0100000000001008\r\n");
            var isRunning = Encoding.ASCII.GetString(await SwitchConnection.ReadRaw(commandBytes, 17, token).ConfigureAwait(false));
            return ulong.Parse(isRunning.Trim(), System.Globalization.NumberStyles.HexNumber) == 1;
        }
    }
}
