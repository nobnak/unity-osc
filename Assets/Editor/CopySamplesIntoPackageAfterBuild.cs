using UnityEditor.Callbacks;
using UnityEditor;
using UnityEngine;
using System.IO;

public class CopySamplesIntoPackageAfterBuild {
    [DidReloadScripts(1)]
    static void OnDidReloadScripts() {
        // Copy the samples from the Assets/Samples folder to the package's Samples folder
        string sourceFolder = "Assets/Samples";
        string destinationFolder = "Packages/jp.nobnak.osc/Samples~";
        if (Directory.Exists(sourceFolder)) {
            if (Directory.Exists(destinationFolder)) {
                Directory.Delete(destinationFolder, true);
            }
            Directory.CreateDirectory(destinationFolder);

            foreach (var srcPath in Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories)) {
                var relativePath = Path.GetRelativePath(sourceFolder, srcPath);
                var  dstPath = Path.Combine(destinationFolder, relativePath);
                var dstDir = Path.GetDirectoryName(dstPath);
                if (!Directory.Exists(dstDir)) {
                    Directory.CreateDirectory(dstDir);
                }
                File.Copy(srcPath, dstPath, true);
            }
        } else {
            Debug.LogWarning("Source samples directory does not exist.");
        }
    }
}