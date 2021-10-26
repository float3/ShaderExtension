#region
using System.Diagnostics.CodeAnalysis;
#endregion
namespace _3.Editor
{
    public class Texture : IProperty
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum DefaultValue
        {
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
        public DefaultValue defaultValue;
        /// <summary>
        ///     Example Display Name
        /// </summary>
        public string displayName;
        public bool HDR;
        public bool HideInInspector;
        public bool MainTexture;
        public string[] MaterialAtributes;
        /// <summary>
        ///     _Examplename
        /// </summary>
        public string name;
        public bool Normal;
        public bool NoScaleOffset;

        //public bool HideInInspector;
        public bool PerRendererData;
        /// <summary>
        ///     2D,2DArray,3D,Cube,CubeArray
        /// </summary>
        public string type;

        public string GetPropertyDeclaration()
        {
            string propertyDeclaration = "";
            //We add the MaterialAttributes and remove brackets to account for inconsistent input
            foreach (string MaterialAttribute in MaterialAtributes)
                propertyDeclaration += "[" + MaterialAttribute.Trim('[', ']') + "] ";

            propertyDeclaration += "_" + name.Trim('_');


            propertyDeclaration += " (\"" + displayName + ", " + type + ") = \"" + defaultValue + "\" {}";

            return propertyDeclaration;
        }
    }
}
