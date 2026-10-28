using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CrawlLocalization
{
    internal static class MenuTextPolicy
    {
        // Saved preference and effective permission are different when CrawlOnline blocks tutorials.
        internal static string ReplayTutorialText(bool requested, bool allowed)
        {
            return !requested ? "Replay Tutorial: OFF" : allowed ? "Replay Tutorial: ON" : "Replay Tutorial: ON (unavailable)";
        }

        internal static bool MatchesPath(IEnumerable<string> paths, string hierarchy)
        {
            hierarchy = hierarchy.Replace("(Clone)", "");
            hierarchy = Regex.Replace(hierarchy, @"(?<=MenuJoinPlayer)[1-4](?=/)", "*");
            foreach (string path in paths)
                if (hierarchy == path || hierarchy.EndsWith("/" + path, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
