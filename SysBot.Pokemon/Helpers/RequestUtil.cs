using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SysBot.Pokemon
{
    public static class RequestUtil<T> where T : PKM, new()
    {
        public static T? GetPokemonViaNamedRequest(string folder, string name)
        {
            var files = EnumerateSpecificFiles(folder, name).ToArray();

            if (files.Length > 0)
            {
                // get the filename that most closely resembles the one they asked for
                var fileNameGet = files[0];
                int bestDistance = 0;

                foreach (string file in files)
                {
                    var thisDistance = LevenshteinDistance.Compute(file, name);
                    if (thisDistance < bestDistance)
                    {
                        bestDistance = thisDistance;
                        fileNameGet = file;
                    }
                }

                var pkm = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(fileNameGet));
                pkm?.ResetPartyStats();
                return (T?)pkm;
            }

            LogUtil.LogError($"Not found: {name}.", nameof(RequestUtil<T>));
            return null;
        }

        static IEnumerable<string> EnumerateSpecificFiles(string directory, string initialTextForFileName)
        {
            foreach (string file in Directory.EnumerateFiles(directory))
            {
                var pt = Path.GetFileNameWithoutExtension(file);
                if (pt.StartsWith(initialTextForFileName, StringComparison.OrdinalIgnoreCase))
                {
                    yield return file;
                }
            }
        }
    }
}
