using System;

#if UNITY_EDITOR
namespace All4thlulz.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class ADBOuputLogWindow : EditorWindow
    {
        private static string[] categorisedStringArray;
        private static int[] errorCountPerCategory = new int[10]; //same amount of output log categories
        private bool[] expandCategoriesBools = new bool[10]; //same amount of output log categories
        private Vector2 _mainScrollPos = Vector2.zero;
        private static readonly float logPixelHeight = 15f;
        private static readonly float minWindowHeight = 50f;

        public static void Init(string[] textArray, int[] errorCountArray, int logCount)
        {
            categorisedStringArray = textArray;
            errorCountPerCategory = errorCountArray;
            //EditorWindow.GetWindow<ADBOuputLogWindow>();
            float windowHeight = minWindowHeight + Mathf.Clamp(logCount * logPixelHeight, logPixelHeight, Screen.currentResolution.height / 1.5f);
            EditorWindow.GetWindowWithRect<ADBOuputLogWindow>(new Rect(Screen.width / 2.0f, Screen.height / 2.0f, Screen.currentResolution.width / 2.0f, windowHeight), true, "ADB Editor Outputlog", true);
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return))
            {
                CopyToClipboard();
            }

            _mainScrollPos = GUILayout.BeginScrollView(_mainScrollPos);
            for (var i = 0; i < categorisedStringArray.Length; i++)
            {
                //expand the begin and end log categories
                if (i == 0 || i == categorisedStringArray.Length - 1)
                {
                    expandCategoriesBools[i] = true;
                }

                bool containsErrors = errorCountPerCategory[i] > 0 ? true : false;
                var str = categorisedStringArray[i];
                ADBEditor.SimpleOutputLogCategories category = (ADBEditor.SimpleOutputLogCategories)i;
                string foldoutTitle = containsErrors == true ? category.ToString() + " - [Contains (" + errorCountPerCategory[i] + ") Logged Errors That May Need User Attention] :" : category.ToString();
                expandCategoriesBools[i] = EditorGUILayout.Foldout(containsErrors == true ? true : expandCategoriesBools[i], foldoutTitle);
                if (expandCategoriesBools[i])
                {
                    GUILayout.Label(str, EditorStyles.helpBox);
                }
            }

            GUILayout.EndScrollView();

            GUI.skin.box = GUI.skin.box;

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copy To Clipboard", GUILayout.Width(130), GUILayout.Height(25)))
            {
                CopyToClipboard();
            }

            GUILayout.Label(" ");

            if (GUILayout.Button("Close", GUILayout.Width(60), GUILayout.Height(25)))
            {
                Close();
            }

            //GUILayout.Label(" ");
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void CopyToClipboard()
        { 
            this.ShowNotification(new GUIContent("Copied To Cipboard"));
            string allOutputLogsInOne = string.Empty;
            foreach (var str in categorisedStringArray)
            {
                allOutputLogsInOne += str;
            }
            GUIUtility.systemCopyBuffer = allOutputLogsInOne;
        }
    }
}
#endif