using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BGLib.Polyglot;
using HarmonyLib;
using UnityEngine;

namespace CrouchAlert.Patches;

[HarmonyPatch(typeof(LocalizationAsyncInstaller), "LoadResourcesBeforeInstall")]
internal class LocalizationLoader
{
    [HarmonyPrefix]
    public static void Prefix(IList<TextAsset> assets)
    {
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CrouchAlert.Resources.translations.csv"))
        using (StreamReader reader = new StreamReader(stream))
        {
            string content = reader.ReadToEnd();
            assets.Add(new TextAsset(content));
        }
    }
}