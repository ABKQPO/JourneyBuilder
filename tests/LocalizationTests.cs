using System;
using TerrariaModder.Core.Localization;

static class LocalizationTests
{
    public static void Run(Action<string, bool> check)
    {
        var candidates = CultureFallback.Candidates("zh-Hans");
        check("culture keeps exact then language then English", candidates.Count == 3 &&
            candidates[0] == "zh-Hans" && candidates[1] == "zh" && candidates[2] == "en");

        candidates = CultureFallback.Candidates("ja-JP");
        check("culture falls back from Japanese region", candidates.Count == 3 &&
            candidates[0] == "ja-JP" && candidates[1] == "ja" && candidates[2] == "en");

        check("empty culture falls back to English", CultureFallback.Candidates(null).Count == 1 &&
            CultureFallback.Candidates(null)[0] == "en");
    }
}
