using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
///        To use, stick this under a folder named "Editor", then right click an object/script in the project window and click "Find References"
/// @author Marudice
/// original source by Dave Lloyd- www.powerhoof.com
/// https://www.powerhoof.com/find-object-references-in-unity/
/// https://pastebin.com/AXAcUTMH

/// <summary>
/// Reference finder - Finds prefabs and scene that reference an object in unity
/// </summary>
public class ReferenceFinder : EditorWindow {
    Vector2 m_scrollPosition = Vector2.zero;
    List<GameObject> m_refPrefabs = new List<GameObject>();
    List<SceneAsset> m_refScenes = new List<SceneAsset>();
    List<ScriptableObject> m_refScriptables = new List<ScriptableObject>();
    List<string> m_paths = null;
    bool m_ignoreInnerReference = true;
    Object[] m_toFindObjects = null;

    Object[] m_toFindObjectsAfterLayout = null;


    [MenuItem("Assets/参照先チェック #r", false, 39)]
    static void FindObjectReferences() {
        //Show existing window instance. If one doesn't exist, make one.
        ReferenceFinder window = EditorWindow.GetWindow<ReferenceFinder>(true, "参照先チェック", true);
        if (Selection.objects.Length > 1) {
            window.FindObjectReferencesAll(Selection.objects);
        } else {
            window.FindObjectReferences(Selection.activeObject);
        }
    }

    void OnGUI() {
        GUILayout.Space(5);
        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

        GUILayout.BeginHorizontal();
        m_ignoreInnerReference = GUILayout.Toggle(m_ignoreInnerReference, "内部参照を無視する");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear", EditorStyles.miniButton)) {
            m_refPrefabs.Clear();
            m_refScenes.Clear();
            m_refScriptables.Clear();
            m_toFindObjects = null;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(15);

        if (m_toFindObjects != null) {
            GUILayout.BeginHorizontal();
            if (m_toFindObjects.Length == 1) {
                LayoutObjectButton(m_toFindObjects[0]);
                GUILayout.Label($" の参照先は...");
            } else {
                GUILayout.Label($"{m_toFindObjects.Length} items");
                if (GUILayout.Button("\u25B6", EditorStyles.miniButtonRight, GUILayout.MaxWidth(20))) {
                    m_toFindObjectsAfterLayout = m_toFindObjects;
                }
                GUILayout.Label($" の参照先は...");
            }
            GUILayout.EndHorizontal();
        }

        LayoutGroup(m_refPrefabs, "prefabs");
        LayoutGroup(m_refScriptables, "scriptables");
        LayoutGroup(m_refScenes, "scenes");

        EditorGUILayout.EndScrollView();

        if (m_toFindObjectsAfterLayout != null) {
            FindObjectReferencesAll(m_toFindObjectsAfterLayout);
            m_toFindObjectsAfterLayout = null;
        }
    }

    void LayoutGroup<T>(List<T> references, string footer) where T : Object {
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label($" {references.Count} {footer}");
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        for (int i = references.Count - 1; i >= 0; --i) {
            LayoutItem(i, references[i]);
        }
    }

    void LayoutItem(int i, UnityEngine.Object obj) {
        if (obj != null) {
            GUILayout.BeginHorizontal();
            LayoutObjectButton(obj);
            GUILayout.EndHorizontal();
        }
    }

    private void LayoutObjectButton(Object obj) {
        GUIStyle style = EditorStyles.miniButtonLeft;
        style.alignment = TextAnchor.MiddleLeft;

        if (GUILayout.Button(obj.name, style)) {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }

        // Use "right arrow" unicode character 
        if (GUILayout.Button("\u25B6", EditorStyles.miniButtonRight, GUILayout.MaxWidth(20))) {
            var objs = new Object[1];
            objs[0] = obj;
            m_toFindObjectsAfterLayout = objs;
        }
    }

    /// Finds references to passed objects and puts them in m_references
    void FindObjectReferencesAll(Object[] toFindObjects) {
        EditorUtility.DisplayProgressBar("Searching", "Generating file paths", 0.0f);

        m_toFindObjects = toFindObjects;

        // Get all prefabs in the project
        //
        if (m_paths == null) {
            m_paths = new List<string>();
            GetFilePaths("Assets", ref m_paths, ".prefab", ".unity");
        }

        float progressBarPos = 0;
        int numPaths = m_paths.Count;
        int hundredthIteration = Mathf.Max(1, numPaths / 100); // So we only update progress bar 100 times, not for every item

        //string toFindName = AssetDatabase.GetAssetPath(toFind);
        //toFindName = System.IO.Path.GetFileNameWithoutExtension(toFindName);
        Object[] tmpArray = new Object[1];
        m_refPrefabs.Clear();
        m_refScriptables.Clear();
        m_refScenes.Clear();

        //
        // Loop through all files, and add any that have the selected object in it's list of dependencies
        //
        for (int i = 0; i < numPaths; ++i) {
            tmpArray[0] = AssetDatabase.LoadMainAssetAtPath(m_paths[i]);
            foreach (Object toFind in toFindObjects) {
                if (tmpArray != null && tmpArray.Length > 0 && tmpArray[0] != toFind) // Don't add self
                {
                    Object[] dependencies = EditorUtility.CollectDependencies(tmpArray);
                    if (System.Array.Exists(dependencies, item => item == toFind)) {
                        // Don't add if another of the dependencies is already in there
                        if (tmpArray[0] is GameObject) {
                            m_refPrefabs.Add(tmpArray[0] as GameObject);
                        } else if (tmpArray[0] is SceneAsset) {
                            m_refScenes.Add(tmpArray[0] as SceneAsset);
                        } else {
                            Debug.LogErrorFormat($"Unexpected. found object [{tmpArray[0]}] is {tmpArray[0].GetType()}");
                        }
                    }

                }
            }
            if (i % hundredthIteration == 0) {
                progressBarPos += 0.01f;
                EditorUtility.DisplayProgressBar("Searching", "Searching dependencies", progressBarPos);
            }
        }


        if (m_ignoreInnerReference) {
            EditorUtility.DisplayProgressBar("Searching", "Removing inner prefab references", 0);
            RemoveInnerReferences(ref m_refPrefabs, m_refPrefabs, m_refScriptables);
            EditorUtility.DisplayProgressBar("Searching", "Removing inner scriptable references", 1.0f / 3.0f);
            RemoveInnerReferences(ref m_refScriptables, m_refPrefabs, m_refScriptables);
            EditorUtility.DisplayProgressBar("Searching", "Removing inner scene references", 2.0f / 3.0f);
            RemoveInnerReferences(ref m_refScenes, m_refPrefabs, m_refScriptables);
            EditorUtility.DisplayProgressBar("Searching", "Removing inner references", 1);
        }
        EditorUtility.ClearProgressBar();
    }

    /// Finds references to passed objects and puts them in m_references
    void FindObjectReferences(Object toFind) {
        Object[] toFindObjects = new Object[1];
        toFindObjects[0] = toFind;
        FindObjectReferencesAll(toFindObjects);
    }

    private static void RemoveInnerReferences<T>(ref List<T> check_references
        , List<GameObject> prefab_references
        , List<ScriptableObject> scriptable_references
        )
        where T : Object {
        //
        // Go through the references, get dependencies of each and remove any that have another dependency on the match list. We only want direct dependencies.
        //
        Object[] tmpArray = new Object[1];
        for (int i = check_references.Count - 1; i >= 0; i--) {
            tmpArray[0] = check_references[i];
            Object[] dependencies = EditorUtility.CollectDependencies(tmpArray);

            bool shouldRemove = false;

            for (int j = 0; j < dependencies.Length && shouldRemove == false; ++j) {
                Object dependency = dependencies[j];
                shouldRemove =
                    (prefab_references.Find(item => item == dependency && item != tmpArray[0]) != null) ||
                    (scriptable_references.Find(item => item == dependency && item != tmpArray[0]) != null);
            }

            if (shouldRemove)
                check_references.RemoveAt(i);
        }
    }

    /// Recursively find all file paths with particular extention in a directory
    static void GetFilePaths(string startingDirectory, ref List<string> paths, params string[] extensions) {
        try {
            // Add any file paths with the correct extention
            string[] files = Directory.GetFiles(startingDirectory);
            for (int i = 0; i < files.Length; ++i) {
                string file = files[i];
                foreach (var extension in extensions) {
                    if (file.EndsWith(extension)) {
                        paths.Add(file);
                        break;
                    }
                }
            }

            // Recurse for all directories
            string[] directories = Directory.GetDirectories(startingDirectory);
            for (int i = 0; i < directories.Length; ++i) {
                GetFilePaths(directories[i], ref paths, extensions);
            }
        } catch (System.Exception excpt) {
            Debug.LogError(excpt.Message);
        }
    }

}