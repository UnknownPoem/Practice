using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SetShaderBundle
{
    public static readonly string bundleSuffix = ".assetbundle";
    public static Dictionary<string, List<string>> data = new Dictionary<string, List<string>>();
    static string path = Application.dataPath.Replace("Assets", "Temp/ShaderFolder").Replace("\\", "/");
    static string battlePath = Path.Combine(path, "Battle.txt");
    static string showPath = Path.Combine(path, "Show.txt");

    [MenuItem("CustomSVC/Set Shader Bundle")]
    public static void SetShaderBundlerNames()
    {
        string dataPath = Application.dataPath.Replace("\\", "/");
        string permanentShaderFolder = string.Concat(dataPath, "/", "Shaders/Permanent");
        if (!Directory.Exists(permanentShaderFolder))
        {
            DebugUtils.LogError("Can not find shader folder ");
            return;
        }
        var directoryInfo = new DirectoryInfo(permanentShaderFolder);
        var files = directoryInfo.GetFiles("*.shader", SearchOption.AllDirectories);
        string bundleName = string.Concat("shaders", bundleSuffix);
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = files[i].Name;
            string assetPath = files[i].FullName.Replace("\\", "/").Replace(dataPath, "Assets");
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            importer.assetBundleName = bundleName;
        }
        var files_var = directoryInfo.GetFiles("*.shadervariants", SearchOption.AllDirectories);
        string bundleName_var = string.Concat("shaders", bundleSuffix);
        for (int i = 0; i < files_var.Length; i++)
        {
            string fileName = files_var[i].Name;
            string assetPath = files_var[i].FullName.Replace("\\", "/").Replace(dataPath, "Assets");
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            Debug.LogError($"bundleName_var:{bundleName_var},path :{assetPath}");
            importer.assetBundleName = bundleName_var;
        }
        string optionalShaderFolder = string.Concat(dataPath, "/", "Shaders/Optional");
        if (!Directory.Exists(optionalShaderFolder))
        {
            DebugUtils.LogError("Can not find shader folder ");
            return;
        }
        directoryInfo = new DirectoryInfo(optionalShaderFolder);
        files = directoryInfo.GetFiles("*.shader", SearchOption.AllDirectories);
        bundleName = string.Concat("shaders_optional", bundleSuffix);
        for (int i = 0; i < files.Length; i++)
        {
            string fileName = files[i].Name;
            string assetPath = files[i].FullName.Replace("\\", "/").Replace(dataPath, "Assets");
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            importer.assetBundleName = bundleName;
        }
    }
    [MenuItem("CustomSVC/Get Show Resources Folder")]
    public static void GetShow()
    {
        data.Clear();
        var folders = Directory.GetDirectories("Assets/Art/Hero", "*.*", SearchOption.AllDirectories);
        var list = new List<string>();
        foreach (var folder in folders)
        {
            if (folder.EndsWith("Show"))
            {
                list.Add(folder.Replace("\\", "/"));
            }
        }
        list.Add("Assets/Art/Map/Scene_Lobby");
        list.Add("Assets/Art/Stages");
        list.Add("Assets/AvProVideo");
        list.Add("Assets/Spine");

        data.Add("Show", list);
        OutPutResult(showPath, data);
        
    }
    [MenuItem("CustomSVC/Get Battle Resources Folder")]
    public static void GetBattle()
    {
        data.Clear();
        var folders = Directory.GetDirectories("Assets/Art/Hero", "*.*", SearchOption.AllDirectories);
        var list = new List<string>();
        foreach (var folder in folders)
        {
            if (folder.EndsWith("Battle"))
            {
                list.Add(folder.Replace("\\","/"));
            }
        }
        list.Add("Assets/Art/Building");
        list.Add("Assets/Art/CommonFx");
        list.Add("Assets/Art/Map/Scene_1v1");
        list.Add("Assets/Art/Map/Scene_5v5");
        list.Add("Assets/Art/Map/Scene_5v5_Melee");
        list.Add("Assets/Art/Map/SharedResource");
        list.Add("Assets/Art/Monster");
        list.Add("Assets/Art/Soldier");
        data.Add("Battle", list);
        OutPutResult(battlePath, data);
    }
    public static void OutPutResult(string path, object result)
    {
        if (!Directory.GetParent(path).Exists)
        {
            Directory.GetParent(path).Create();
        }

        FileStream fileStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
        fileStream.Dispose();
        StreamWriter streamWriter = new StreamWriter(path, true);
        StringBuilder returnStringBuilder = new StringBuilder();
        JsonWriter writer = new JsonWriter(returnStringBuilder);
        writer.PrettyPrint = true;
        JsonMapper.ToJson(result, writer);
        streamWriter.Write(returnStringBuilder.ToString());
        streamWriter.Flush();
        fileStream.Dispose();
    }

}
