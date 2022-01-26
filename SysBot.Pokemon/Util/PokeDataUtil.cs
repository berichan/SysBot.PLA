using System;
using System.Collections.Generic;
using System.Text;
using PKHeX.Core;
using System.Linq;

namespace SysBot.Pokemon
{
    public static class PokeDataUtil
    {
        public static string? CollateSpecies(this PKM[] pokes)
        {
            if (pokes == null || pokes.Length < 1)
                return null;
            string toRet = string.Concat(pokes.Select(z => $", {(Species)z.Species}"));
            return toRet.TrimStart(", ");
        }
    }
}
