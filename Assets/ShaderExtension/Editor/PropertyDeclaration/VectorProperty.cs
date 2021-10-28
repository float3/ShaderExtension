#region
using System;
using UnityEngine;
#endregion
namespace ShaderExtension
{
    public class Vector : IProperty
    {
        /// <summary>
        /// th
        /// </summary>
        public enum Type
        {
            Vector,
            Color,
        }

        /// <summary>
        /// a material property attribute ex. "[HDR]" tells unity and the inspector
        /// how to draw and interpret a property.
        /// The built-in ones for Vectors are these:
        /// "[Vector3]", "[Vector2]",
        /// Custom attributes are also valid.
        /// </summary>
        public string[] materialPropertyAttributes;

        /// <summary>
        /// Property name should start with a "_".
        /// "_ExampleName", "_Example_Name_2"
        /// </summary>
        public string name;

        /// <summary>
		/// This is the string that Unity displays in the inspector.
        /// "Example Display Name"
        /// </summary>
        public string displayName;

        /// <summary>
		/// Vector, Color
		/// </summary>
        public Type type;

        /// <summary>
		/// HLSL and Unity seem to use xyzw to access the fields of a Vector4 (instead of WXYZ that regular C# seems to use).
        /// This expects a xyzw format.
        /// Even if you declare it as a [Vector2] or [Vector3] it will still technically have 4 fields.
		/// </summary>
        public Vector4 defaultValue;

        /// <summary>
        /// returns the full declaration of the property
        /// </summary>
        /// <returns> example:"        [MainColor] _Color("Color", Color) = (1,1,1,1)"</returns>
        public string GetPropertyDeclaration()
        {
            string propertyDeclaration = "";

            propertyDeclaration += name + " (\"" + displayName + ", " + type.ToString() + ") = (" + defaultValue.x + "," + defaultValue.y + "," + defaultValue.z + "," + defaultValue.w + ")";
            //propertyDeclaration += "{0} (\"{1}, {2}\") = \"{3}"
            return propertyDeclaration;
        }
    }
}
