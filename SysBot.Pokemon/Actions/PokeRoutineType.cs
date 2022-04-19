namespace SysBot.Pokemon
{
    /// <summary>
    /// Type of routine the Bot carries out.
    /// </summary>
    public enum PokeRoutineType
    {
        /// <summary> Sits idle waiting to be re-tasked. </summary>
        Idle = 0,

        // Add your own custom bots here so they don't clash for future main-branch bot releases.
        PLAFlexTrade = 10000,
        PLASpecialRequest = 10001,
        PLALinkTrade = 10002,
        PLAClone = 10003,
        PLADump = 10004
    }

    public static class PokeRoutineTypeExtensions
    {
        public static bool IsTradeBot(this PokeRoutineType type) => type is >=PokeRoutineType.PLAFlexTrade and <=PokeRoutineType.PLADump;
    }
}