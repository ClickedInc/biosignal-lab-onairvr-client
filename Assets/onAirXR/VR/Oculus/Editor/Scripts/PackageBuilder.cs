/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System.Collections.Generic;
using System.IO;
using UnityEditor;

public class AirVRPackageBuild {
    private const string Version = "2.4.0";

    [MenuItem("onAirXR/VR/Export onAirVR Client (Minimal)...")]
    public static void ExportMinimalPackage() {
        exportPackage("Export onAirVR Client...", "onairvr-client_minimal_" + Version, new string[] {
            "Assets/onAirXR/VR/Base",
            "Assets/onAirXR/VR/Oculus"
        });
    }

    [MenuItem("onAirXR/VR/Export onAirVR Client (Full)...")]
    public static void ExportFullPackage() {
        exportPackage("Export onAirVR Client...", "onairvr-client_full_" + Version, new string[] {
            "Assets/onAirXR/Core",
            "Assets/onAirXR/VR/Base",
            "Assets/onAirXR/VR/Oculus"
        });
    }

    private static void exportPackage(string dialogTitle, string defaultName, string[] assetPaths) {
        var targetPath = EditorUtility.SaveFilePanel(dialogTitle, "", defaultName, "unitypackage");
        if (string.IsNullOrEmpty(targetPath)) { return; }

        if (File.Exists(targetPath)) {
            File.Delete(targetPath);
        }

        var assetids = AssetDatabase.FindAssets("", assetPaths);
        var assets = new List<string>();
        foreach (var assetid in assetids) {
            assets.Add(AssetDatabase.GUIDToAssetPath(assetid));
        }

        AssetDatabase.ExportPackage(assets.ToArray(), targetPath);

        EditorUtility.DisplayDialog("Exported", "Exported successfully.\n\n" + targetPath, "Close");
    }
}
