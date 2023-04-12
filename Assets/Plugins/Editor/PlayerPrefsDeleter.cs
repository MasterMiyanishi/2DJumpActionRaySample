// PlayerPrefsDeleter.cs
// https://www.hanachiru-blog.com/entry/2019/05/25/170042
//
// Created by hanachiru on 2019.05.26

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace PlayerPrefsEditor
{

    public class PlayerPrefsDeleter : EditorWindow
    {
        private List<PlayerPrefsData> _playerPrefData = null;


        [MenuItem("Tools/PlayerPrefsDeleter")]
        private static void Open()
        {
            var window = GetWindow<PlayerPrefsDeleter>("PlayerPrefsDeleter");

            window.minSize = new Vector2(500, 180);

            window.LoadPlayerPrefs();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_playerPrefData == null || _playerPrefData.Count() == 0)
            {
                EditorGUILayout.LabelField("PlayerPrefs do not exist");
                return;
            }

            DrawPlayerPrefsList();
        }

        /// <summary>
        /// PlayerPrefsを読み込む
        /// </summary>
        private void LoadPlayerPrefs()
        {

#if UNITY_EDITOR
            //リストの初期化
            if (_playerPrefData != null)
            {
                _playerPrefData.Clear();
            }
            else
            {
                _playerPrefData = new List<PlayerPrefsData>();
            }

            //PlayerPrefsのすべてのキーを検索する
            var keys = new List<string>();
            PlayerPrefsTools.GetAllPlayerPrefKeys(ref keys);

            //リストの作成
            foreach (var key in keys)
            {
                _playerPrefData.Add(new PlayerPrefsData(key));
            }
#else
            _playerPrefData = null;
#endif

        }

        /// <summary>
        /// PlayerPrefsを全て削除する
        /// </summary>
        private void ResetPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            _playerPrefData = null;
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true)))
            {
                if (GUILayout.Button("DeleteAll...", EditorStyles.toolbarButton))
                {
                    Debug.Log("Delete All PlayerPrefs");
                    ResetPlayerPrefs();
                }
                if (GUILayout.Button("Reload...", EditorStyles.toolbarButton))
                {
                    Debug.Log("Reload PlayerPrefs");
                    LoadPlayerPrefs();
                }
            }
        }
        private void DrawPlayerPrefsList()
        {
            bool isDeleted = false;

            EditorGUILayout.BeginVertical();

            foreach (var item in _playerPrefData)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Key: " + item.Key);
                EditorGUILayout.LabelField("Value: " + item.Value);

                if (GUILayout.Button("Delete", GUILayout.Width(64), GUILayout.Height(16)))
                {
                    item.Delete();
                    isDeleted = true;
                    Debug.Log(item.Key + " is deleted");
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            if (isDeleted) LoadPlayerPrefs();
        }
    }

    public class PlayerPrefsData
    {
        public string Key { get; }
        public string Value { get; }

        public PlayerPrefsData(string key)
        {
            Key = key;
            Value = GetValue(key);
        }

        public void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
        }

        /// <summary>
        /// PlayerPrefsのstring,int,floatのどれかを返す
        /// </summary>
        private string GetValue(string key)
        {
            string value = PlayerPrefs.GetString(key, null);

            if (string.IsNullOrEmpty(value))
            {
                value = PlayerPrefs.GetInt(key, 0).ToString();

                if (value == "0")
                {
                    value = PlayerPrefs.GetFloat(key, 0).ToString();
                }
            }

            return value;
        }
    }
}