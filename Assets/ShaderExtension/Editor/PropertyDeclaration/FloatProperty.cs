using UnityEngine;

namespace ShaderExtension
{
    public class Float : IProperty
    {
        /// <summary>
		/// Evem if a Shader Property is declared as a int it will still be a float internally
        /// 
		/// </summary>
        public enum Type
		{
            Float,
            Int,
            Range
		}

        /// <summary>
        ///     a material property attribute ex. "[HDR]" tells unity and the inspector
        ///     how to draw and interpret a property.
        ///     Some of the built-in ones for Floats are these:
        ///     "[IntRange]", "[Toggle]", "[Toggle(_)]"
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
		///     Float, Int, Range
		/// </summary>
        public Type type;

        /// <summary>
        /// if the property is of Type "Range" it needs a min and a max
        /// </summary>
        public Vector2 range;

        /// <summary>
        //      defaultValue
        /// </summary>
        public float defaultValue;

        /// <summary>
        /// returns the full declaration of the property
        /// </summary>
        /// <returns> example:"        [MainColor] _Cutoff("Cutoff", Range() = (1,1,1,1)"</returns>
        public string GetPropertyDeclaration()
        {
            string propertyDeclaration = "";

            foreach (string materialPropertyAttribute in materialPropertyAttributes)
			{
                propertyDeclaration += $"{materialPropertyAttribute} ";
            }

            string typeformat = type == Type.Range ? $"Range({range.x},{range.y})" : typeformat = type.ToString();
            
            /*if (type == Type.Range)
			{
                typeformat = $"Range({range.x},{range.y})";
			}
			else
			{
                typeformat = type.ToString();
            }*/

            //propertyDeclaration += name + " (\"" + displayName + ", " + type + ") = (" + defaultValue + ")";
            propertyDeclaration += ($"{name} (\"{displayName}, {typeformat}) = {defaultValue}");

            return propertyDeclaration;
		}
    }
}