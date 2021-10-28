#region
using System.Diagnostics.CodeAnalysis;
#endregion
namespace ShaderExtension
{
    public class Texture : IProperty
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum DefaultValue
        {
            white,
            black,
            red,
            gray,
            grey,
            linearGray,
            linearGrey,
            grayscaleRamp,
            greyscaleRamp,
            bump,
            blackCube,
            lightmap,
            unity_Lightmap,
            unity_LightmapInd,
            unity_ShadowMask,
            unity_DynamicLightmap,
            unity_DynamicDirectionality,
            unity_DynamicNormal,
            unity_DitherMask,
            _DitherMaskLOD,
            _DitherMaskLOD2D,
            unity_RandomRotation16,
            unity_NHxRoughness,
            unity_SpecCube0,
            unity_SpecCube1
        }

        public enum Type
        {
            _2D,
            _2DArray,
            _3D,
            _Cube,
            _CubeArray
        }

        /// <summary>
        ///     a material property attribute ex. "[HDR]" tells unity and the inspector
		///     how to draw and interpret a property.
		///     The built-in ones for Textures are these:
		///     "[HDR]", "[HideInInspector]", "[MainTexture]", "[Normal]", "[NoScaleOffset]", "[PerRendererData]"
		///     Custom attributes are also valid.
        /// </summary>
        public string[] materialPropertyAttributes;

        /// <summary>
		///     Property name should start with a "_".
        ///     "_ExampleName", "_Example_Name_2"
        /// </summary>
        public string name;

        /// <summary>
		///     This is the string that Unity displays in the inspector.
        ///     "Example Display Name"
        /// </summary>
        public string displayName;

        /// <summary>
        ///     2D,2DArray,3D,Cube,CubeArray
        /// </summary>
        public Type type;

        public DefaultValue defaultValue;

        /// <summary>
        /// returns the full declaration of the property
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

            foreach (string materialPropertyAttribute in materialPropertyAttributes)
            {
                propertyDeclaration += $"{materialPropertyAttribute} ";
            }

            propertyDeclaration += name + " (\"" + displayName + ", " + type.ToString() + ") = \"" + defaultValue.ToString() + "\" {}";

            return propertyDeclaration;
        }
    }
}
