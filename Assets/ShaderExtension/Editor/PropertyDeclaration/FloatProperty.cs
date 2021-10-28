#region

using ShaderExtension.Interfaces;
using ShaderExtension.PropertyDeclaration.Enums;
using UnityEngine;

#endregion

namespace ShaderExtension.PropertyDeclaration
{
    public class Float : IProperty
    {
        /// <summary>
        /// </summary>
        public float DefaultValue;

        /// <summary>
        ///     This is the string that Unity displays in the inspector.
        ///     "Example Display Name"
        /// </summary>
        public string DisplayName;

        /// <summary>
        ///     a material property attribute ex. "[HDR]" tells unity and the inspector
        ///     how to draw and interpret a property.
        ///     Some of the built-in ones for Floats are these:
        ///     "[IntRange]", "[Toggle]", "[Toggle(_)]"
        ///     Custom attributes are also valid.
        /// </summary>
        public string[] MaterialPropertyAttributes;

        /// <summary>
        ///     Property name should start with a "_".
        ///     "_ExampleName", "_Example_Name_2"
        /// </summary>
        public string Name;

        /// <summary>
        ///     if the property is of Type "Range" it needs a min and a max
        /// </summary>
        public Vector2 Range;

        /// <summary>
        ///     Float, Int, Range
        /// </summary>
        public FloatType Type;

        /// <summary>
        ///     returns the full declaration of the property
        /// </summary>
        /// <returns> example:"        [MainColor] _Cutoff("Cutoff", Range() = (1,1,1,1)"</returns>
        public string GetPropertyDeclaration()
        {
            string propertyDeclaration = "";

            foreach (string materialPropertyAttribute in MaterialPropertyAttributes)
            {
                propertyDeclaration += $"{materialPropertyAttribute} ";
            }

            string typeformat = Type == FloatType.Range ? $"Range({Range.x},{Range.y})" : typeformat = Type.ToString();

            /*if (type == Type.Range)
			{
                typeformat = $"Range({range.x},{range.y})";
			}
			else
			{
                typeformat = type.ToString();
            }*/

            //propertyDeclaration += name + " (\"" + displayName + ", " + type + ") = (" + defaultValue + ")";
            propertyDeclaration += $"{Name} (\"{DisplayName}, {typeformat}) = {DefaultValue}";

            return propertyDeclaration;
        }
    }
}