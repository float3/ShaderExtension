#region

using ShaderExtension.Interfaces;
using ShaderExtension.PropertyDeclaration.Enums;

#endregion

namespace ShaderExtension.PropertyDeclaration
{
    public class Texture : IProperty
    {
        public DefaultTexture DefaultTexture;

        /// <summary>
        ///     This is the string that Unity displays in the inspector.
        ///     "Example Display Name"
        /// </summary>
        public string DisplayName;

        /// <summary>
        ///     a material property attribute ex. "[HDR]" tells unity and the inspector
        ///     how to draw and interpret a property.
        ///     The built-in ones for Textures are these:
        ///     "[HDR]", "[HideInInspector]", "[MainTexture]", "[Normal]", "[NoScaleOffset]", "[PerRendererData]"
        ///     Custom attributes are also valid.
        /// </summary>
        public string[] MaterialPropertyAttributes;

        /// <summary>
        ///     Property name should start with a "_".
        ///     "_ExampleName", "_Example_Name_2"
        /// </summary>
        public string Name;

        /// <summary>
        ///     2D,2DArray,3D,Cube,CubeArray
        /// </summary>
        public TextureType Type;

        /// <summary>
        ///     returns the full declaration of the property
        /// </summary>
        /// <returns> example:"        [MainTexture] _MainTex ("Texture", 2D) = "white" {}"</returns>
        public string GetPropertyDeclaration()
        {
            string propertyDeclaration = "";

            //We don't!
            //We add the MaterialAttributes and remove brackets to account for inconsistent input
            //foreach (string materialPropertyAttribute in materialPropertyAttributes)
            //    propertyDeclaration += "[" + materialPropertyAttribute.Trim('[', ']') + "] ";
            //We don't!

            foreach (string materialPropertyAttribute in MaterialPropertyAttributes)
            {
                propertyDeclaration += $"{materialPropertyAttribute} ";
            }

            propertyDeclaration += Name + " (\"" + DisplayName + ", " + Type + ") = \"" + DefaultTexture + "\" {}";

            return propertyDeclaration;
        }
    }
}