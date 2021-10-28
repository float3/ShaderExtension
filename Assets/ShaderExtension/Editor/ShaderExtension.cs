#region

using System.IO;
using ShaderExtension.Interfaces;
using UnityEditor;
using UnityEngine;

#endregion

namespace ShaderExtension
{
    //TODO Test
    public static class ShaderExtension
    {
        /// <summary>
        ///     Adds a Property to the Bottom of a Shaders Block
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="property"></param>
        public static void AddProperty(this Shader shader, IProperty property)
        {
            ParsedShader parsedShader = new ParsedShader(shader);
            int propertiesEnd = parsedShader.PropertiesBlock.EndLineNum;

            string propertyString = property.GetPropertyDeclaration();

            string path = AssetDatabase.GetAssetPath(parsedShader.Shader);

            InsertLineInFile(path, propertyString, propertiesEnd);
            AssetDatabase.ImportAsset(path);
        }

        private static void InsertLineInFile(string path, string line, int position)
        {
            string[] lines = File.ReadAllLines(path);
            using (StreamWriter writer = new StreamWriter(path))
            {
                for (int i = 0; i < position; i++)
                    writer.WriteLine(lines[i]);
                writer.WriteLine(line);
                for (int i = position; i < lines.Length; i++)
                    writer.WriteLine(lines[i]);
            }
        }
    }
}