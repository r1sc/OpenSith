using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Sith.Gui
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]

    public sealed class NotifyAttribute : PropertyAttribute
    {
    }

    [CustomPropertyDrawer(typeof(NotifyAttribute))]
    public class NotifyAttributeyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Rely on the default inspector GUI
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label);

            // Update only when necessary
            NotifyAttribute setProperty = attribute as NotifyAttribute;
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                Debug.Log("NotifyAttributeyDrawer::changeeeed" + property.ToString());
                foreach (var t in property.serializedObject.targetObjects)
                {
                    //((Game.World.SithSector)t).testChanged();
                    var m = property.name + "Changed";
                    var aa = t.GetType();
                    var notify = t.GetType().GetMethod(m);


                   // var uu = Convert.ChangeType(property.serializedObject, aa);
                    if (notify != null)
                        notify.Invoke(null, null);
                }
            }
        }
    }
}
