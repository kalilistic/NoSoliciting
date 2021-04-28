using System;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace NoSoliciting {
    internal static class Util {
        internal static void PreLoadResourcesFromMainAssembly(string resourcesPrefix, string resourcesExtension = ".resources") {
            // get loaded resource sets from resource manager
            var resourceManager = Resources.Language.ResourceManager;

            if ((
                resourceManager.GetType().GetField("_resourceSets", BindingFlags.Instance | BindingFlags.NonPublic) ??
                // ReSharper disable once PossibleNullReferenceException
                resourceManager.GetType().GetField("ResourceSets", BindingFlags.Instance | BindingFlags.NonPublic)
            ).GetValue(resourceManager) is not IDictionary resourceSetByCulture) {
                return;
            }

            var resourceAssembly = typeof(Plugin).Assembly; // get assembly with localization resources
            foreach (var embeddedResourceName in resourceAssembly.GetManifestResourceNames()) {
                if (embeddedResourceName.StartsWith(resourcesPrefix, StringComparison.Ordinal) == false ||
                    embeddedResourceName.EndsWith(resourcesExtension, StringComparison.Ordinal) == false) {
                    continue; // not localized resource
                }

                var locale = embeddedResourceName.Substring(resourcesPrefix.Length, Math.Max(0, embeddedResourceName.Length - resourcesPrefix.Length - resourcesExtension.Length));
                if (string.IsNullOrEmpty(locale)) {
                    continue; // default locale
                }

                var resourceStream = resourceAssembly.GetManifestResourceStream(embeddedResourceName);
                if (resourceStream == null) {
                    continue; // no resource stream
                }

                var resourceSet = new System.Resources.ResourceSet(resourceStream);
                var culture = CultureInfo.GetCultureInfo(locale);
                if (resourceSetByCulture is Hashtable) {
                    resourceSetByCulture.Add(culture, resourceSet);
                } else {
                    resourceSetByCulture.Add(culture.Name, resourceSet);
                }
            }
        }
    }
}
