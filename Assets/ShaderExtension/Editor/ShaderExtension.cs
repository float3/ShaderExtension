#region
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
#endregion
namespace ShaderExtension
{
    //TODO Test
    public static class ShaderExtensions
    {
        /// <summary>
        ///     Get the start of the Property Block
        /// </summary>
        /// <param name="Shader">Shader</param>
        /// <returns>
        ///     returns the line at which the Property Block starts (counting from 0)
        ///     returns -1 if there is no Property Block
        /// </returns>
        public static int GetStartOfPropertyBlock(this Shader shader)
        {
            string path = AssetDatabase.GetAssetPath(shader);
            string[] shaderData = File.ReadAllLines(path);
            int properties = -1;
            int lineNum = -1;
            foreach (string line in shaderData)
            {
                lineNum++;
                properties = line.IndexOf("Properties", StringComparison.Ordinal);
                if (properties != -1)
                    return lineNum;
            }
            Debug.Log("no Property Block was found");
            return -1;
        }

        /// <summary>
        ///     Get the end of the Property Block
        /// </summary>
        /// <param name="shader">Shader</param>
        /// <returns>
        ///     returns the line at which the Property Block ends (counting from 0),
        ///     returns -1 if there is no property Block
        /// </returns>
        public static int GetEndOfPropertyBlock(this Shader shader)
        {
            string path = AssetDatabase.GetAssetPath(shader);
            string[] shaderData = File.ReadAllLines(path);
            int propertiesStart = shader.GetStartOfPropertyBlock();
            if (propertiesStart == -1)
                return -1;
            int lineNum = -1;
            bool enteredBlock = false;
            int bracketLevel = 0;
            foreach (string line in shaderData)
            {
                lineNum++;
                if (lineNum >= propertiesStart)
                {
                    int openBrackets = line.IndexOf("{", StringComparison.Ordinal);
                    int closingBrackets = line.IndexOf("}", StringComparison.Ordinal);

                    if (openBrackets != -1)
                    {
                        bracketLevel++;
                        enteredBlock = true;
                    }
                    if (closingBrackets != -1)
                        bracketLevel--;
                    if (bracketLevel == 0 && enteredBlock)
                        return lineNum;
                }
            }
            return -1;
        }

        /// <summary>
        ///     Get the line at which each Pass starts
        /// </summary>
        /// <param name="shader">Shader</param>
        /// <returns>
        ///     returns a int[] where the index is the number of the Pass and the value is the line at which it starts (counting
        ///     from 0),
        ///     returns int[1] with value -1 if no Pass is found
        /// </returns>
        public static int[] GetPassLines(this Shader shader)
        {
            string path = AssetDatabase.GetAssetPath(shader);
            string[] shaderData = File.ReadAllLines(path);
            int passes = shader.passCount;
            int[] passLines;
            if (passes == 0)
            {
                passLines = new int[1];
                passLines[0] = -1;
                return passLines;
            }
            passLines = new int[passes];
            int pass = 0;
            //int grabPass = -1;
            int lineNum = -1;

            foreach (string line in shaderData)
            {
                //TODO Test
                //TODO Check if After SubShader 
                //GrabPass probably works; untested
                lineNum++;

                int _pass = line.IndexOf("Pass", StringComparison.Ordinal);

                if (_pass != -1)
                {
                    passLines[pass] = lineNum;
                    pass++;
                }
                if (pass == passes)
                    return passLines;
            }
            Debug.Log("no Pass was found");
            return passLines;
        }

        /// <summary>
        /// Adds a Property to the Bottom of a Shaders Block
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="property"></param>
        public static void AddProperty(this Shader shader, IProperty property)
        {
            string path = AssetDatabase.GetAssetPath(shader);
            string shaderData = File.ReadAllText(path);
            string[] shaderLines = StripComments(shaderData);
            List<string> oldShader = shaderLines.ToList();
            int propertiesEnd = shader.GetEndOfPropertyBlock();
            string propertyString = property.GetPropertyDeclaration();

            oldShader.Insert(propertiesEnd, propertyString);
            File.WriteAllLines(path, oldShader);

            AssetDatabase.ImportAsset(path);
        }

        public static int IterateShader (this string [] shaderData, string searchstring)
		{
            int lineNum = -1;
            foreach (string line in shaderData)
            {
                lineNum++;
                int found = line.IndexOf(searchstring, StringComparison.Ordinal);
                if (found != -1)
				{
                    return lineNum;
				}
            }
            Debug.Log($"{searchstring} not found in Shader");
            return -1;
        }

        public static string[] StripComments (string shaderData)
		{
            string blockComments = @"/\*(.*?)\*/";
            string lineComments = @"//(.*?)\r?\n";
            string strings = @"""((\\[^\n]|[^""\n])*)""";
            string verbatimStrings = @"@(""[^""]*"")+";

			string noComments = Regex.Replace(shaderData,
            blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
            me => {
            if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
            return me.Value.StartsWith("//") ? Environment.NewLine : "";
            // Keep the literal strings
            return me.Value;
            },
            RegexOptions.Singleline);

            return Regex.Split(noComments, "\r\n|\r|\n");
        }
    }
}
