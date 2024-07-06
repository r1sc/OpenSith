using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Assets.Sith.Gui
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class HexFieldAttribute : PropertyAttribute
    {
        public int digits;
        public string FormatString
        {
            get
            {
                if (digits == 0)
                {
                    return "X";
                }
                else
                {
                    return string.Format("X{0}", digits);
                }
            }
        }

        public HexFieldAttribute(int digits = 2)
        {
            if (digits < 0)
                throw new ArgumentOutOfRangeException("Digits cannot be negative");
            this.digits = digits;
        }

        public HexFieldAttribute() : this(0)
        {}
    }


    // The following class should be placed in a script file at the path "Assets/Editor"

    // Here is where you create the custom PropertyDrawer. The magic happens in the
    // OnGUI method where we create a TextField in the inspector and set it's value
    // to the SerializedProperty's value as a long, read it back as a string and
    // try to parse it to a number again. If the parsing fails at any point, the
    // number is just set to 0.
    [CustomPropertyDrawer(typeof(HexFieldAttribute))]
    public class HexFieldDrawer : PropertyDrawer
    {
        public HexFieldAttribute hexFieldAttribute
        {
            get { return ((HexFieldAttribute)attribute); }
        }

        public override void OnGUI(Rect position,
                                   SerializedProperty property,
                                   GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            string hexValue = EditorGUI.TextField(position, label,
                 "0x" + property.longValue.ToString(hexFieldAttribute.FormatString));

            long value = 0;
            if (hexValue.ToLower().StartsWith("0x"))
            {
                try
                {
                    value = Convert.ToInt64(hexValue, 16);
                }
                catch (FormatException)
                {
                    value = 0;
                }
            }
            else
            {
                bool parsed = long.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber, null, out value);
                if (!parsed)
                {
                    value = 0;
                }
            }

            if (EditorGUI.EndChangeCheck())
                property.longValue = value;
        }
    }
}
