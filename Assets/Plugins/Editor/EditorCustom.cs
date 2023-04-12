using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EditorCustom
{
    private static readonly Color mDisabledColor = new Color(1, 1, 1, 0.5f);

    private const int WIDTH = 16;

    private static readonly MethodInfo mGetIconForObject = typeof(EditorGUIUtility)
        .GetMethod("GetIconForObject", BindingFlags.NonPublic | BindingFlags.Static);

    private const int HEIGHT = 16;

    
    private const string REMOVE_STR = "Assets";

    private static readonly int mRemoveCount = REMOVE_STR.Length;
    private static readonly Color mColor = new Color(0.635f, 0.635f, 0.635f, 1);

    [MenuItem("CONTEXT/Component/Find Component")]
    private static void FindComponent(MenuCommand command) {
        var type = command.context.GetType();
        SceneModeUtility.SearchForType(type);
    }

    [InitializeOnLoadMethod]
    private static void Example()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnGUI;

        //EditorApplication.projectWindowItemOnGUI += OnGUIp;

    }

    private static void OnGUI(int instanceID, Rect selectionRect)
    {
        var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (go == null)
        {
            return;
        }
        var pos = selectionRect;
        
        // ポジションを一番後ろへ移動
        pos = selectionRect;
        pos.x = pos.xMax - WIDTH;
        pos.width = WIDTH;
        pos.height = HEIGHT;

        // Hierarchyにゲームオブジェクトが持つコンポーネントの一覧を表示する
        var components = go
            .GetComponents<Component>()
            .Where(c => c != null)
            .Where(c => !(c is Transform))
            .Reverse();

        var current = Event.current;

        var path = "";
        Color color;

        foreach (var c in components)
        {
            Texture image = AssetPreview.GetMiniThumbnail(c);

            if (image == null && c is MonoBehaviour)
            {
                var ms = MonoScript.FromMonoBehaviour(c as MonoBehaviour);
                path = AssetDatabase.GetAssetPath(ms);
                image = AssetDatabase.GetCachedIcon(path);
            }

            if (image == null)
            {
                continue;
            }

            if (current.type == EventType.MouseDown &&
                 pos.Contains(current.mousePosition))
            {
                c.SetEnable(!c.IsEnabled());
            }

            color = GUI.color;
            GUI.color = c.IsEnabled() ? Color.white : mDisabledColor;
            GUI.DrawTexture(pos, image, ScaleMode.ScaleToFit);
            GUI.color = color;
            pos.x -= pos.width;
        }

        // Hierarchyにゲームオブジェクトがアクティブかどうかを変更するトグルを表示する

        var newActive = GUI.Toggle(pos, go.activeSelf, string.Empty);

        if (newActive == go.activeSelf)
        {
            //return;
        }

        go.SetActive(newActive);

        // ヒエラルキーにアイコンを表示する
        var parameters = new object[] { go };
        var icon = mGetIconForObject.Invoke(null, parameters) as Texture;

        if (icon == null)
        {
            return;
        }

        pos = selectionRect;
        //pos.x = pos.xMax - WIDTH;
        pos.x = pos.xMin - WIDTH * 2;
        pos.width = WIDTH;

        GUI.DrawTexture(pos, icon, ScaleMode.ScaleToFit, true);

    }

    public static bool IsEnabled(this Component self)
    {
        if (self == null)
        {
            return true;
        }

        var type = self.GetType();
        var property = type.GetProperty("enabled", typeof(bool));

        if (property == null)
        {
            return true;
        }

        return (bool)property.GetValue(self, null);
    }

    public static void SetEnable(this Component self, bool isEnabled)
    {
        if (self == null)
        {
            return;
        }

        var type = self.GetType();
        var property = type.GetProperty("enabled", typeof(bool));

        if (property == null)
        {
            return;
        }

        property.SetValue(self, isEnabled, null);
    }
    // プロジェクトに容量表示
    private static void OnGUIp(string guid, Rect selectionRect)
    {
        var dataPath = Application.dataPath;
        var startIndex = dataPath.LastIndexOf(REMOVE_STR);
        var dir = dataPath.Remove(startIndex, mRemoveCount);
        var path = dir + AssetDatabase.GUIDToAssetPath(guid);

        if (!File.Exists(path))
        {
            return;
        }

        var fileInfo = new FileInfo(path);
        var fileSize = fileInfo.Length;
        var text = GetFormatSizeString((int)fileSize);

        var label = EditorStyles.label;
        var content = new GUIContent(text);
        var width = label.CalcSize(content).x;

        var pos = selectionRect;
        pos.x = pos.xMax - width;
        pos.width = width;
        pos.yMin++;

        var color = GUI.color;
        GUI.color = mColor;
        GUI.DrawTexture(pos, EditorGUIUtility.whiteTexture);
        GUI.color = color;
        GUI.Label(pos, text);
    }
    private static string GetFormatSizeString(int size)
    {
        return GetFormatSizeString(size, 1024);
    }

    private static string GetFormatSizeString(int size, int p)
    {
        return GetFormatSizeString(size, p, "#,##0.##");
    }

    private static string GetFormatSizeString(int size, int p, string specifier)
    {
        var suffix = new[] { "", "K", "M", "G", "T", "P", "E", "Z", "Y" };
        int index = 0;

        while (size >= p)
        {
            size /= p;
            index++;
        }

        return string.Format(
            "{0}{1}B",
            size.ToString(specifier),
            index < suffix.Length ? suffix[index] : "-"
        );
    }
}