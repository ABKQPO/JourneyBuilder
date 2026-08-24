using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria.Localization;
using TerrariaModder.Core;
using TerrariaModder.Core.Config;
using TerrariaModder.Core.Logging;
using TerrariaModder.Core.Manifest;

namespace JourneyBuilder
{
    /// <summary>
    /// Mod-owned localization fallback. This keeps JourneyBuilder usable with
    /// loaders that do not provide TerrariaModder.Core.Localization.
    /// </summary>
    internal static class JourneyBuilderLocalization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Resources =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private static ILogger _log;

        public static void Initialize(ModContext context, JourneyBuilderConfig config)
        {
            _log = context?.Logger;
            Resources.Clear();
            LoadFolder(Path.Combine(context?.ModFolder ?? "", "localization"));

            if (context?.Manifest != null)
            {
                context.Manifest.Name = Get("manifest.name", context.Manifest.Name);
                context.Manifest.Description = Get("manifest.description", context.Manifest.Description);
            }

            ApplyConfigMetadata(config);
        }

        public static string Get(string key, string fallback = null)
        {
            foreach (string culture in CultureCandidates(CurrentCulture()))
            {
                if (Resources.TryGetValue(culture, out var values) && values.TryGetValue(key, out string value))
                    return value;
            }

            return fallback ?? key ?? "";
        }

        private static void ApplyConfigMetadata(JourneyBuilderConfig config)
        {
            if (config == null) return;

            foreach (ConfigPropertyMeta meta in config.GetPropertyMetadata())
            {
                SetMetadata(meta, "Label", Get("config." + meta.Key + ".label", meta.Label));
                SetMetadata(meta, "Description", Get("config." + meta.Key + ".description", meta.Description));
            }
        }

        private static void SetMetadata(ConfigPropertyMeta meta, string propertyName, string value)
        {
            try
            {
                PropertyInfo property = meta.GetType().GetProperty(
                    propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                property?.SetValue(meta, value, null);
            }
            catch (Exception ex)
            {
                _log?.Debug("JourneyBuilder: failed to localize config metadata: " + ex.Message);
            }
        }

        private static void LoadFolder(string folder)
        {
            if (!Directory.Exists(folder)) return;

            foreach (string file in Directory.GetFiles(folder, "*.json", SearchOption.TopDirectoryOnly))
            {
                string culture = Normalize(Path.GetFileNameWithoutExtension(file));
                if (string.IsNullOrEmpty(culture)) continue;

                try
                {
                    Type serializerType = Type.GetType(
                        "System.Web.Script.Serialization.JavaScriptSerializer, System.Web.Extensions");
                    if (serializerType == null) return;

                    object serializer = Activator.CreateInstance(serializerType);
                    object parsed = serializerType.GetMethod("DeserializeObject", new[] { typeof(string) })
                        ?.Invoke(serializer, new object[] { File.ReadAllText(file) });
                    if (!(parsed is IDictionary map)) continue;

                    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (DictionaryEntry entry in map)
                    {
                        if (entry.Key != null && entry.Value != null)
                            values[Convert.ToString(entry.Key)] = Convert.ToString(entry.Value);
                    }

                    Resources[culture] = values;
                }
                catch (Exception ex)
                {
                    _log?.Warn("JourneyBuilder: failed to load localization " + file + ": " + ex.Message);
                }
            }
        }

        private static IEnumerable<string> CultureCandidates(string culture)
        {
            string normalized = Normalize(culture);
            if (!string.IsNullOrEmpty(normalized))
            {
                yield return normalized;
                int separator = normalized.IndexOf('-');
                if (separator > 0)
                    yield return normalized.Substring(0, separator);
            }

            yield return "en";
        }

        private static string CurrentCulture()
        {
            try { return Language.ActiveCulture?.Name ?? "en"; }
            catch { return "en"; }
        }

        private static string Normalize(string culture)
            => string.IsNullOrWhiteSpace(culture) ? "" : culture.Trim().Replace('_', '-');
    }
}
