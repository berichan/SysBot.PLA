﻿using PKHeX.Core;
using SysBot.Pokemon.Discord;
using SysBot.Pokemon.Twitch;
using SysBot.Pokemon.WinForms;
using SysBot.Pokemon.Web;
using System.Threading;
using System.Threading.Tasks;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Bot Environment implementation with Integrations added.
    /// </summary>
    public class PokeBotRunnerImpl<T> : PokeBotRunner<T> where T : PKM, new()
    {
        public PokeBotRunnerImpl(PokeTradeHub<T> hub, BotFactory<T> fac) : base(hub, fac) { }
        public PokeBotRunnerImpl(PokeTradeHubConfig config, BotFactory<T> fac) : base(config, fac) { }

        private TwitchBot<T>? Twitch;
        private static WebBot<T>? Web;

        protected override void AddIntegrations()
        {
            AddDiscordBot(Hub.Config.Discord.Token);
            AddTwitchBot(Hub.Config.Twitch);
            AddWebBot(Hub.Config.Web);
        }

        private void AddTwitchBot(TwitchSettings config)
        {
            if (string.IsNullOrWhiteSpace(config.Token))
                return;
            if (Twitch != null)
                return; // already created

            if (string.IsNullOrWhiteSpace(config.Channel))
                return;
            if (string.IsNullOrWhiteSpace(config.Username))
                return;
            if (string.IsNullOrWhiteSpace(config.Token))
                return;

            Twitch = new TwitchBot<T>(Hub.Config.Twitch, Hub);
            if (Hub.Config.Twitch.DistributionCountDown)
                Hub.BotSync.BarrierReleasingActions.Add(() => Twitch.StartingDistribution(config.MessageStart));
        }

        private void AddDiscordBot(string apiToken)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
                return;
            var bot = new SysCord<T>(this);
            Task.Run(() => bot.MainAsync(apiToken, CancellationToken.None));
        }

        private void AddWebBot(WebSettings config)
        {
            if (string.IsNullOrWhiteSpace(config.URIEndpoint))
                return;
            if (Web != null)
                return; // already created

            if (string.IsNullOrEmpty(config.URIEndpoint))
                return;

            Web = new WebBot<T>(Hub.Config.Web, Hub);
        }
    }
}
