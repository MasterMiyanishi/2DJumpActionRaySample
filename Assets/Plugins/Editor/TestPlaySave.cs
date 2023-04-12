using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
/// <summary>
/// TestPlaySave
/// シーンを保存してテスト実行することができる
/// 無限ループや実行時にエディタが落ちたときでも
/// 実行前の状態を保持できる
/// 
/// 追加機能
/// 　実行/停止/一時停止/シーンをビルドに追加
/// </summary>
[InitializeOnLoad]
public class TestPlaySave : EditorWindow
{
    //! MenuItem("メニュー名/項目名") のフォーマットで記載してね
    [MenuItem("M/TestPlaySaveWindow")]
    static void ShowWindow()
    {
        // ウィンドウを表示！
        var window = EditorWindow.GetWindow<TestPlaySave>();

        //ウィンドウサイズ設定(minとmaxを=しているのはウィンドウサイズを固定するため)
        window.maxSize = new Vector2(370, 20);
        window.minSize = new Vector2(20, 10);
    }
    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存して実行"))
        {
            EditorSceneManager.SaveOpenScenes();
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }
        if (GUILayout.Button("実行/停止"))
        {
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }
        if (GUILayout.Button("一時停止"))
        {
            EditorApplication.ExecuteMenuItem("Edit/Pause");
        }
        if (GUILayout.Button("シーンをビルドに追加"))
        {
            EditorBuildSettingsScene[] scenes = { new EditorBuildSettingsScene(EditorSceneManager.GetActiveScene().path,true) };
            EditorBuildSettings.scenes = scenes;
        }
        EditorGUILayout.EndHorizontal();
    }
}
