namespace ShaderExtension.Enums
{
    public enum ParseState
    {
        ShaderName = 0,
        Properties = 1,
        PropertiesBlock = 2,
        SubShader = 3,
        PassBlock = 4,
        SubShaderCg = 6,
        PassCg = 7,
        FallBack = 8
    }
}