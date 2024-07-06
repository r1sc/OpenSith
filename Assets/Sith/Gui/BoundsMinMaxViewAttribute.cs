using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Sith.Gui
{
    // TODO: Constrain attribute to Bounds class
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property |
        AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class BoundsMinMaxViewAttribute : PropertyAttribute {}

    [CustomPropertyDrawer(typeof(BoundsMinMaxViewAttribute))]
    public class BoundsMinMaxViewDrawer : PropertyDrawer
    {
        private bool _showValues = true;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //label = EditorGUI.BeginProperty(position, label, property);
            _showValues = EditorGUI.BeginFoldoutHeaderGroup(position, _showValues, label);
            if(_showValues)
            {
                bool wasEnabled = GUI.enabled;
                GUI.enabled = false;

                ++EditorGUI.indentLevel;
                position.y += 5 + EditorGUI.GetPropertyHeight(SerializedPropertyType.String, label);
                EditorGUI.Vector3Field(position, "Min", property.boundsValue.min);

                position.y += 20;
                EditorGUI.Vector3Field(position, "Max", property.boundsValue.max);
                GUI.enabled = true;
            }
            EditorGUI.EndFoldoutHeaderGroup();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return _showValues
                ? EditorGUI.GetPropertyHeight(property, label) + 10
                : EditorGUI.GetPropertyHeight(SerializedPropertyType.String, label);
        }
    }
}
