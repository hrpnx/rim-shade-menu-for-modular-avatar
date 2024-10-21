/*
 * インスペクタに表示される変数名を好きな文字に置き換えるカスタムエディタ
 * CustomLabelAttribute.cs : Ver. 1.0.3
 * Written by Takashi Sowa with ChatGPT
 * ▼使い方：以下のように記述すればインスペクタに表示される「variable」が「変数名」に置き換わる
 * [CustomLabel("変数名")]
 * public int variable = 0;//[SerializeField]でも利用可
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace dev.hrpnx.rim_shade_menu_for_modular_avatar.runtime
{

    public class CustomLabelAttribute : PropertyAttribute
    {
        public readonly GUIContent Label;//GUIContent型に変更
        public CustomLabelAttribute(string label)
        {
            Label = new GUIContent(label);//stringからGUIContentに変換
        }
    }

#if UNITY_EDITOR
    //カスタムアトリビュートに関連づけられたプロパティドロワーの宣言
    [CustomPropertyDrawer(typeof(CustomLabelAttribute))]
    public class CustomLabelAttributeDrawer : PropertyDrawer
    {
        //エディタ上でカスタムプロパティを描画
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //カスタムアトリビュートをCustomLabelAttributeとして取得
            var newLabel = attribute as CustomLabelAttribute;
            //カスタムアトリビュートのラベルをプロパティのラベルに設定
            label = newLabel.Label;
            //エディタ上にプロパティを描画
            EditorGUI.PropertyField(position, property, label, true);
        }

        //エディタ上でプロパティの高さを取得
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //プロパティの高さを取得
            return EditorGUI.GetPropertyHeight(property, true);
        }
    }
#endif
}
