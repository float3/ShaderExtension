#region

using System.Diagnostics.CodeAnalysis;

#endregion

namespace ShaderExtension.PropertyDeclaration.Enums
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum DefaultTexture
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
}