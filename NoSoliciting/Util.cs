using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace NoSoliciting {
    internal static class Util {
        internal static void PreloadEmbeddedResources(Assembly resourceAssembly, ResourceManager resourceManager, string resourcesPrefix, string resourcesExtension = ".resources") {
            // get loaded resource sets from resource manager
            if ((
                resourceManager.GetType().GetField("_resourceSets", BindingFlags.Instance | BindingFlags.NonPublic) ??
                // ReSharper disable once PossibleNullReferenceException
                resourceManager.GetType().GetField("ResourceSets", BindingFlags.Instance | BindingFlags.NonPublic)
            ).GetValue(resourceManager) is not IDictionary resourceSetByCulture) {
                return;
            }

            foreach (var embeddedResourceName in resourceAssembly.GetManifestResourceNames()) {
                if (!embeddedResourceName.StartsWith(resourcesPrefix, StringComparison.Ordinal) ||
                    !embeddedResourceName.EndsWith(resourcesExtension, StringComparison.Ordinal)) {
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

                var key = resourceSetByCulture is Hashtable
                    ? (object) culture
                    : culture.Name;

                // remove any old resources if there somehow are any
                if (resourceSetByCulture.Contains(key)) {
                    resourceSetByCulture.Remove(key);
                }

                resourceSetByCulture.Add(key, resourceSet);
            }
        }
    }
}
