using UnityEditor.Callbacks;
using UnityEditor;
using UnityEngine;
using System.IO;

public class CopySamplesIntoPackageAfterBuild {
    [DidReloadScripts(1)]
    static void OnDidReloadScripts() {
        // Copy the samples from the Assets/Samples folder to the package's Samples folder
        string sourcePath = "Assets/Samples";
        string destinationPath = "Packages/jp.nobnak.osc/Samples~";
        if (Directory.Exists(sourcePath)) {
            if (Directory.Exists(destinationPath)) {
                Directory.Delete(destinationPath, true);
            }
            Directory.CreateDirectory(destinationPath);

            foreach (var file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)) {
                var srcFile = new FileInfo(file);
                var  destFile = new FileInfo(Path.Combine(destinationPath, srcFile.Name));
                File.Copy(file, destFile.FullName, true);
            }
        } else {
            Debug.LogWarning("Source samples directory does not exist.");
        }
    }
}