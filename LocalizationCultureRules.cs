using System;

namespace JourneyBuilder
{
    internal static class LocalizationCultureRules
    {
        internal static string PrimaryResource(string culture)
        {
            string normalized = string.IsNullOrWhiteSpace(culture)
                ? ""
                : culture.Trim().Replace('_', '-');

            switch (normalized.ToLowerInvariant())
            {
                case "chinese":
                case "simplifiedchinese":
                case "chinesesimplified":
                case "chinese-simplified":
                case "zh":
                case "zh-cn":
                case "zh-hans":
                    return "zh-Hans";
                case "traditionalchinese":
                case "chinesetraditional":
                case "chinese-traditional":
                case "zh-tw":
                case "zh-hant":
                    return "zh-Hant";
                case "japanese":
                case "ja":
                case "ja-jp":
                    return "ja";
                case "english":
                case "en":
                case "en-us":
                case "en-gb":
                    return "en";
                default:
                    return normalized;
            }
        }
    }
}
