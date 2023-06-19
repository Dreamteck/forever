using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public static class ForeverSegmentBackup
{
    public static string backupPath = "";

    [SettingsProvider]
    public static SettingsProvider SplinesSettingsProvider()
    {
        SettingsProvider provider = new SettingsProvider("Dreamteck/Backup", SettingsScope.User)
        {
            label = "Backup",
            guiHandler = (searchContext) =>
            {
                OnGUI();
            },
            keywords = new HashSet<string>(new[] { "Dreamteck", "Backup", "Forever" })
        };

        return provider;
    }

    private static void OnGUI()
    {
        LoadPrefs();
        backupPath = EditorGUILayout.TextField("Backup Path", backupPath);
        SavePrefs();
    }

    public static void BackupAsset(string assetPath)
    {
        if (Directory.Exists(backupPath))
        {
            string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - ("/Assets").Length);
            string prefabLocalPath = assetPath;
            string sourcePath = projectPath + "/" + prefabLocalPath;
            string prefabFileName = Path.GetFileName(sourcePath);
            string destinationPath = backupPath + "/" + prefabLocalPath.Substring(("Assets/").Length);
            string destinationDir = Path.GetDirectoryName(destinationPath);

            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Copy(sourcePath, destinationPath);
        }
    }

    static ForeverSegmentBackup() {
        LoadPrefs();
    }


    private static void LoadPrefs()
    {
        backupPath = EditorPrefs.GetString("ForeverBackupPrefs.backupPath", "");
    }

    private static void SavePrefs()
    {
        EditorPrefs.SetString("ForeverBackupPrefs.backupPath", backupPath);
    }
}
