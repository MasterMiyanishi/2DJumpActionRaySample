using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

/// <summary> 1つのAssetの情報.依存関係にあるScriptや子のオブジェクトを格納しておく</summary>
public class AssetInfo
{
    public string AssetPath { get; set; }
    public UnityEngine.Object AssetObject { get; set; }
    public string[] ScriptDependences { get; set; }
    public List<UnityEngine.Object> ChildrenObjs { get; set; }
}

/// <summary>1つのAssetに対しての検索結果。使われているメソッドのリストと情報を持っている</summary>
public class FindResult
{
    public FindResult()
    {
        MethodNames = new List<string>();
    }
    public List<string> MethodNames { get; set; }
    public AssetInfo AssetInfo { get; set; }
}


/// <summary>
/// メソッド名、参照しているAssetのリストを格納している、表示用のクラス。
/// </summary>
public class DisplayMethod
{
    public bool HasReferenceAsset => ReferencedAssets?.Count > 0;
    public string DisplayMethodName { get; private set; }

    private MethodInfo methodInfo;

    public DisplayMethod()
    {
        ReferencedAssets = new List<AssetInfo>();
    }

    /// <summary>
    /// 表示する関数名の調整。
    /// 「public 返り値の型 Method名 (引数型 引数名)」のようになるように。
    /// </summary>
    private void CreateDisplayMethodName()
    {
        string returnType = MethodInfo.ReturnType.ToString();
        returnType = returnType.Split('.').Last();
        string args = string.Join(" ", MethodInfo.GetParameters().Select(p => p.ParameterType + " " + p.Name));
        DisplayMethodName = $"public {returnType} {methodInfo.Name} ({args})";
    }

    public MethodInfo MethodInfo
    {
        get { return methodInfo; }
        set
        {
            methodInfo = value;
            CreateDisplayMethodName();
        }
    }
    public List<AssetInfo> ReferencedAssets { get; set; }
}


public class FindMethodRefToAsset : EditorWindow
{

    private List<AssetInfo> prefabInfoList;
    private List<DisplayMethod> displayMethodList;
    private UnityEngine.Object targetScript;
    private string targetScriptPath;

    private Vector2 scrollPos;

    /// <summary>m_OnClickはButton、m_DelegatesはEventTriggerで使われているプロパティ名</summary> 
    private string[] findPropertyNames = new string[] { "m_OnClick", "m_Delegates" };

    [MenuItem("Assets/Find Method Ref To Asset")]
    public static void FindMethod()
    {
        var window = CreateInstance<FindMethodRefToAsset>();

        window.targetScript = Selection.activeObject;
        window.targetScriptPath = AssetDatabase.GetAssetPath(window.targetScript);
        window.Init();
        window.Show();
    }

    [MenuItem("Assets/Find Method Ref To Asset", validate = true)]
    private static bool ValidationSelectionType()
    {
        return Selection.activeObject is MonoScript;
    }

    public void Init()
    {
        prefabInfoList = new List<AssetInfo>();
        displayMethodList = new List<DisplayMethod>();
        CreateDisplayMethodList();
        CheckPrefabs();
    }


    /// <summary>
    /// 表示用にprefabのデータの中から使っているものの抽出。
    /// </summary>
    /// <param name="results">prefab内のデータ結果</param>
    private void CreateDisplayData(List<FindResult> results)
    {
        foreach (var displayMethod in displayMethodList)
        {
            foreach (var result in results)
            {
                if (result.MethodNames.Contains(displayMethod.MethodInfo.Name))
                {
                    displayMethod.ReferencedAssets.Add(result.AssetInfo);
                }
            }
        }
    }


    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        GUILayout.Label(targetScript.name);

        foreach (var display in displayMethodList)
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(display.DisplayMethodName);
            }

            EditorGUILayout.BeginVertical();
            if (!display.HasReferenceAsset)
            {
                GUILayout.Label("このメソッドを参照しているprefabはありません。消しても問題ないです。");
                continue;
            }
            foreach (var asset in display.ReferencedAssets)
            {
                GUILayout.Label(asset.AssetPath);
                EditorGUILayout.ObjectField("", asset.AssetObject, typeof(UnityEngine.Object), false);
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }


    /// <summary>
    /// プロジェクト内のPrefabすべてを網羅してスクリプトのメソッドがあるかどうかをチェック。
    /// </summary>
    private void CheckPrefabs()
    {
        if (string.IsNullOrEmpty(targetScriptPath))
        {
            Debug.LogError("選択したコードへのパスが空です");
            return;
        }
        CreatePrefabInfoList(prefabInfoList);

        foreach (var assetInfo in prefabInfoList)
        {
            foreach (var dependencyPath in assetInfo.ScriptDependences)
            {
                if (dependencyPath != targetScriptPath) continue;

                List<long> ids = GetScriptLocalIdsInPrefab(assetInfo, targetScript.name);
                List<FindResult> results = GetFindResults(assetInfo, ids);
                CreateDisplayData(results);
            }
        }
    }


    private void CreatePrefabInfoList(List<AssetInfo> prefabInfoList)
    {
        var prefabList = GetAssetPathList("t:Prefab");
        foreach (var path in prefabList)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            UnityEngine.Object[] children = AssetDatabase.LoadAllAssetsAtPath(path);
            string[] dependences = AssetDatabase.GetDependencies(path);
            var gameObjects = new List<UnityEngine.Object>();
            foreach (var child in children)
            {
                if (child != null && child is GameObject)
                {
                    gameObjects.Add(child);
                }
            }

            var info = new AssetInfo()
            {
                AssetPath = path,
                AssetObject = asset,
                ScriptDependences = dependences.Where(d => d.EndsWith(".cs")).ToArray(),
                ChildrenObjs = gameObjects
            };
            prefabInfoList.Add(info);
        }
    }


    /// <summary>
    /// Prefab内のスクリプトのm_LocalIdentfierInFileを取得する.
    /// </summary>
    /// <param name="assetInfo">対象のPrefab</param>
    /// <param name="scriptName">スクリプト名</param>
    /// <returns></returns>
    private List<long> GetScriptLocalIdsInPrefab(AssetInfo assetInfo, string scriptName)
    {
        var idList = new List<long>();
        foreach (var child in assetInfo.ChildrenObjs)
        {
            MonoBehaviour[] components = (child as GameObject).GetComponents<MonoBehaviour>();
            foreach (var behaviour in components)
            {
                if (behaviour == null) continue;
                if (behaviour.GetType().Name != scriptName) continue;

                long localId = GetObjectLocalIdInFile(behaviour);
                idList.Add(localId);
            }
        }
        return idList;
    }


    private List<FindResult> GetFindResults(AssetInfo assetInfo, List<long> scriptLocalIds)
    {

        var resultList = new List<FindResult>();
        foreach (var child in assetInfo.ChildrenObjs)
        {
            MonoBehaviour[] components = (child as GameObject).GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                var serializeObj = new SerializedObject(component);

                foreach (var propName in findPropertyNames)
                {
                    SerializedProperty prop = serializeObj.FindProperty(propName);
                    if (prop == null) continue;

                    var result = new FindResult();

                    SetFindScriptInProperty(prop, scriptLocalIds, result);
                    result.AssetInfo = assetInfo;
                    resultList.Add(result);
                }
            }
        }
        return resultList;
    }


    private void FindProp(SerializedProperty prop, string targetPropName, Action<SerializedProperty> callback)
    {
        var p = prop.Copy();
        while (p.Next(true))
        {
            if (p.name == targetPropName)
            {
                callback?.Invoke(p);
            }
        }
    }


    /// <summary>
    /// 指定したSerializedPropertyの中のMethodNameをFindResultに入れる.
    /// m_Callsを使っているのはm_Targetとm_MethodNameの1つ上の階層のため.
    /// </summary>
    /// <param name="prop"></param>
    /// <param name="scriptLocalIds">スクリプトのLocalIdentfierInFile</param>
    /// <param name="result">該当のスクリプトがあればこのクラスにデータを入れておく</param>
    private void SetFindScriptInProperty(SerializedProperty prop, List<long> scriptLocalIds, FindResult result)
    {
        FindProp(prop, "m_Calls", (calls) =>
        {
            for (int i = 0; i < calls.arraySize; i++)
            {
                SerializedProperty callProp = calls.GetArrayElementAtIndex(i);
                SerializedProperty target = callProp.FindPropertyRelative("m_Target");

                if (target.objectReferenceValue == null) continue;

                long id = GetObjectLocalIdInFile(target.objectReferenceValue);
                if (!scriptLocalIds.Contains(id)) continue;

                result.MethodNames.Add(callProp.FindPropertyRelative("m_MethodName").stringValue);
            }
        });
    }


    /// <summary>
    /// 選択したスクリプトのなかのpublicメソッドを抽出.
    /// </summary>
    private void CreateDisplayMethodList()
    {
        if (targetScript == null)
        {
            return;
        }

        MethodInfo[] methods = GetPublicMethodInfo((MonoScript)targetScript);
        foreach (var info in methods)
        {
            if (info.Name.Contains("set_") || info.Name.Contains("get_") || info.Name.Contains(".ctor")) continue;

            var displayMethod = new DisplayMethod() { MethodInfo = info };
            displayMethodList.Add(displayMethod);
        }
    }


    private static string[] GetAssetPathList(string searchStr)
    {
        if (string.IsNullOrEmpty(searchStr))
        {
            Debug.LogError("アセット検索用の文字列が空です. t: から始まる文字列を指定して下さい");
            return null;
        }
        var searchResult = AssetDatabase.FindAssets(searchStr);
        for (int i = 0; i < searchResult.Length; i++)
        {
            string guid = searchResult[i];
            searchResult[i] = AssetDatabase.GUIDToAssetPath(guid);
        }
        return searchResult;
    }


    public static long GetObjectLocalIdInFile(UnityEngine.Object obj)
    {
        SerializedObject serialize = new SerializedObject(obj);

        //インスペクタモードをDebugに変えておかないとLocalIdentfierInFileが取得できない
        PropertyInfo inspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
        inspectorModeInfo.SetValue(serialize, InspectorMode.Debug, null);

        SerializedProperty localIdProp = serialize.FindProperty("m_LocalIdentfierInFile");
        return localIdProp.longValue;
    }


    public static MethodInfo[] GetPublicMethodInfo(MonoScript monoScript)
    {
        MonoScript script = monoScript;
        Type classType = script.GetClass();
        MethodInfo[] methods = classType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        return methods;
    }
}