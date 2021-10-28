#region

using ShaderExtension.Interfaces;
using ShaderExtension.PropertyDeclaration.Enums;
using UnityEngine;

#endregion

namespace ShaderExtension.PropertyDeclaration
{
    public class Vector : IProperty
    {
        /// <summary>
        ///     HLSL and Unity seem to use xyzw to access the fields of a Vector4 (instead of WXYZ that regular C# seems to use).
        ///     This expects a xyzw format.
        ///     Even if you declare it as a [Vector2] or [Vector3] it will still technically have 4 fields.
        /// </summary>
        public Vector4 DefaultValue;

        /// <summary>
        ///     This is the string that Unity displays in the inspector.
        ///     "Example Display Name"
        /// </summary>
        public string DisplayName;

        /// <summary>
        ///     a material property attribute ex. "[HDR]" tells unity and the inspector
        ///     how to draw and interpret a property.
        ///     The built-in ones for Vectors are these:
        ///     "[Vector3]", "[Vector2]",
        ///     Custom attributes are also valid.
        /// </summary>
        public string[] MaterialPropertyAttributes;

        /// <summary>
        ///     Property name should start with a "_".
        ///     "_ExampleName", "_Example_Name_2"
        /// </summary>
        public string Name;

        /// <summary>
        ///     Vector, Color
        /// </summary>
        public VectorType Type;

        /// <summary>
        ///     returns the full declaration of the property
        /// </summary>
        /// <returns> example:"        [MainColor] _Color("Color", Color) = (1,1,1,1)"</returns>
        public string GetPropertyDeclaration()
        {
            string propertyDeclaration = "";

            propertyDeclaration += Name + " (\"" + DisplayName + ", " + Type + ") = (" + DefaultValue.x + "," +
                                   DefaultValue.y + "," + DefaultValue.z + "," + DefaultValue.w + ")";
            //propertyDeclaration += "{0} (\"{1}, {2}\") = \"{3}"
            return propertyDeclaration;
        }
    }
}