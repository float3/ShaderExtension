#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ShaderExtension.Enums;
using UnityEditor;
using UnityEngine;

#endregion

namespace ShaderExtension
{
    public class ParsedShader
    {
        //private const string ZipPath = "builtin_shaders-2019.4.28f1.zip";
        private readonly ZipArchive _zipArchive = OpenBuiltinShaderZip();
        private readonly List<CgBlock> _cgBlocks = new List<CgBlock>();
        private readonly string _filePath;
        private readonly List<Block> _passes = new List<Block>();
        public Block PropertiesBlock;
        public readonly Shader Shader;
        private string[] _shaderLines;
        private string _shaderName;

        public string FallBack;
        public Shader FallBackShader;
        public Block FallBackBlock;

        public Block ShaderNameBlock;
        public string SurfaceTempFilePath;

        public ParsedShader(Shader shader)
        {
            Shader = shader;
            _filePath = GETPath(shader);
            Parse();
        }

        public ParsedShader(Shader shader, string realFilePath)
        {
            Shader = shader;
            _filePath = realFilePath;
            Parse();
        }

        private static ZipArchive OpenBuiltinShaderZip()
        {
            string[] zipAssets = AssetDatabase.FindAssets("builtin_shaders");
            string path = "";
            foreach (string guid in zipAssets)
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".zip"))
                {
                    break;
                }
            }

            return ZipFile.OpenRead(path);
        }

        public static string[] GETBuiltinShaderSource(ZipArchive zipArchive, string shaderName, out string shaderPath)
        {
            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                if (entry.FullName.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
                {
                    using (StreamReader s = new StreamReader(entry.Open()))
                    {
                        string fileContents = s.ReadToEnd();
                        if (fileContents.IndexOf("Shader \"" + shaderName + "\"", StringComparison.Ordinal) != -1)
                        {
                            shaderPath = Path.GetFileName(entry.FullName);
                            return fileContents.Split('\n');
                        }
                    }
                }
            }

            shaderPath = "";
            return new string[] { };
        }

        public string[] GETBuiltinShaderSource(string shaderName, out string shaderPath)
        {
            return GETBuiltinShaderSource(_zipArchive, shaderName, out shaderPath);
        }

        public static string GETZipCgincSource(ZipArchive zipArchive, string cgincPath)
        {
            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                if (entry.FullName.Equals("CGIncludes/" + cgincPath, StringComparison.OrdinalIgnoreCase))
                {
                    using (StreamReader s = new StreamReader(entry.Open()))
                    {
                        string fileContents = s.ReadToEnd();
                        return fileContents;
                    }
                }
            }

            return "";
        }

        public string GETCgincSource(string fileContext, ref string fileName)
        {
            if (fileContext.Contains('/'))
            {
                string fsPath = Path.Combine(Path.GetDirectoryName(fileContext), fileName);
                if (File.Exists(fsPath))
                {
                    fileName = fsPath;
                    return File.ReadAllText(fsPath);
                }
            }

            string ret = GETZipCgincSource(_zipArchive, fileName);
            if (ret.Length == 0)
            {
                Debug.LogError("Failed to find include " + fileName + " from " + fileContext);
            }

            return ret;
        }

        private void Parse()
        {
            _shaderLines = File.ReadAllLines(_filePath);
            ParseState state = ParseState.ShaderName;
            ParseState lastState = ParseState.PassBlock;
            int braceLevel = 0;
            int lineNum = -1;
            int beginBraceLineNum = -1;
            int beginBraceSkip = -1;
            int beginCgLineNum = -1;
            bool isOpenQuote = false;
            // bool CisOpenQuote = false;
            BlockType passType = BlockType.None;
            BlockType cgType = BlockType.None;
            Regex programCgRegex = new Regex("\\b(CG|HLSL)PROGRAM\\b|\\b(CG|HLSL)INCLUDE\\b");
            Regex passCgRegex = new Regex("\\bGrabPass\\b|\\bPass\\b|\\b(CG|HLSL)PROGRAM\\b|\\b(CG|HLSL)INCLUDE\\b");
            foreach (string xline in new CommentFreeIterator(_shaderLines))
            {
                string line = xline;
                lineNum++;
                int lineSkip;
                /*
            while (true) {
                //Debug.Log ("Looking for comment " + lineNum);
                int openQuote = line.IndexOf ("\"", lineSkip, StringComparison.CurrentCulture);
                if (CisOpenQuote) {
                    if (openQuote == -1) {
                        //Debug.Log("C-Open quote ignore " + lineSkip);
                        break;
                    } else {
                        lineSkip = openQuote + 1;
                        CisOpenQuote = false;
                    }
                    //Debug.Log("C-Open quote end " + lineSkip);
                    continue;
                }
                if (openQuote != -1) {
                    CisOpenQuote = true;
                    lineSkip = openQuote + 1;
                    //Debug.Log("C-Open quote start " + lineSkip);
                    continue;
                }
            }
            lineSkip = 0;
            */
                bool fallThrough = true;

                while (fallThrough)
                {
                    //Debug.Log("Looking for state " + state + " on line " + lineNum);
                    fallThrough = false;
                    lineSkip = 0; // ???
                    switch (state)
                    {
                        case ParseState.ShaderName:
                        {
                            int shaderOff = line.IndexOf("Shader", lineSkip, StringComparison.Ordinal);
                            if (shaderOff != -1)
                            {
                                int firstQuote = line.IndexOf('\"', shaderOff);
                                int secondQuote = line.IndexOf('\"', firstQuote + 1);
                                if (firstQuote != -1 && secondQuote != -1)
                                {
                                    _shaderName = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                                    ShaderNameBlock = new Block(this, BlockType.ShaderName, lineNum, firstQuote + 1,
                                        lineNum, secondQuote);
                                    fallThrough = true;
                                    state = ParseState.FallBack;
                                }
                            }
                        }
                            break;
                        case ParseState.FallBack:
                            int fallBack = line.IndexOf("FallBack", lineSkip, StringComparison.Ordinal);
                            if (fallBack != -1)
                            {
                                int firstQuote = line.IndexOf('\"', fallBack);
                                int secondQuote = line.IndexOf('\"', firstQuote + 1);
                                if (firstQuote != -1 && secondQuote != -1)
                                {
                                    FallBack = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                                    FallBackBlock = new Block(this, BlockType.FallBack, lineNum, firstQuote + 1,
                                        lineNum, secondQuote);
                                    fallThrough = true;
                                    state = ParseState.Properties;
                                }
                            }

                            break;
                        case ParseState.Properties:
                        {
                            // Find beginning of Properties block
                            int shaderOff = line.IndexOf("Properties", lineSkip, StringComparison.Ordinal);
                            if (shaderOff != -1)
                            {
                                state = ParseState.PropertiesBlock;
                                passType = BlockType.Properties;
                                fallThrough = true;
                            }
                        }
                            break;
                        case ParseState.PropertiesBlock:
                        case ParseState.PassBlock:
                        {
                            // Find end of Properties block
                            int i = 0;
                            while (lineSkip < line.Length && i < 10000)
                            {
                                i++;
                                int openQuote = line.IndexOf("\"", lineSkip, StringComparison.Ordinal);
                                if (isOpenQuote)
                                {
                                    if (openQuote == -1)
                                    {
                                        Debug.Log("Open quote ignore " + lineSkip);
                                        break;
                                    }

                                    lineSkip = openQuote + 1;
                                    bool esc = false;
                                    int xi = lineSkip - 1;
                                    while (xi > 0 && line[xi] == '\\')
                                    {
                                        esc = !esc;
                                        xi--;
                                    }

                                    if (!esc)
                                    {
                                        isOpenQuote = false;
                                    }

                                    Debug.Log("Open quote end " + lineSkip);
                                    continue;
                                }

                                int openBrace = line.IndexOf("{", lineSkip, StringComparison.Ordinal);
                                int closeBrace = line.IndexOf("}", lineSkip, StringComparison.Ordinal);
                                if (openQuote != -1 && (openQuote < openBrace || openBrace == -1) &&
                                    (openQuote < closeBrace || closeBrace == -1))
                                {
                                    isOpenQuote = true;
                                    lineSkip = openQuote + 1;
                                    Debug.Log("Open quote start " + lineSkip);
                                    continue;
                                }

                                Match m = null;
                                if (state == ParseState.PassBlock)
                                {
                                    m = programCgRegex.Match(line, lineSkip);
                                }

                                Debug.Log("Looking for braces state " + state + " on line " + lineNum + "/" + lineSkip +
                                          " {}" + braceLevel + " open:" + openBrace + "/ close:" + closeBrace +
                                          " m.index " + (m == null ? -2 : m.Index));
                                if (m != null && m.Success && (closeBrace == -1 || m.Index < closeBrace) &&
                                    (openBrace == -1 || m.Index < openBrace))
                                {
                                    string match = m.Value;
                                    cgType = match.Equals("HLSLINCLUDE") || match.Equals("CGINCLUDE")
                                        ? BlockType.CgInclude
                                        : BlockType.CgProgram;
                                    state = ParseState.PassCg;
                                    fallThrough = false;
                                    lineSkip = line.Length;
                                    beginCgLineNum = lineNum + 1;
                                    break;
                                }

                                if (closeBrace != -1 && (openBrace > closeBrace || openBrace == -1))
                                {
                                    lineSkip = closeBrace + 1;
                                    braceLevel--;
                                    if (braceLevel == 0)
                                    {
                                        Block b = new Block(this, passType, beginBraceLineNum, beginBraceSkip, lineNum,
                                            closeBrace);
                                        if (state == ParseState.PropertiesBlock)
                                        {
                                            PropertiesBlock = b;
                                        }
                                        else if (state == ParseState.PassBlock)
                                        {
                                            _passes.Add(b);
                                        }

                                        state = ParseState.SubShader;
                                        fallThrough = true;
                                        break;
                                    }
                                }
                                else if (openBrace != -1 && (openBrace < closeBrace || closeBrace == -1))
                                {
                                    if (braceLevel == 0)
                                    {
                                        beginBraceLineNum = lineNum;
                                        beginBraceSkip = openBrace + 1;
                                    }

                                    braceLevel++;
                                    lineSkip = openBrace + 1;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (i >= 9999)
                            {
                                throw new Exception("Loop overflow " + i + "in braces search " + lineNum + "/" +
                                                    lineSkip + ":" + braceLevel);
                            }
                        }
                            break;
                        case ParseState.SubShader:
                        {
                            Match m;
                            m = passCgRegex.Match(line, lineSkip);
                            if (m != null && m.Success)
                            {
                                string match = m.Value;
                                if (match.Equals("HLSLINCLUDE") || match.Equals("HLSLPROGRAM") ||
                                    match.Equals("CGINCLUDE") || match.Equals("CGPROGRAM"))
                                {
                                    cgType = match.Equals("HLSLINCLUDE") || match.Equals("CGINCLUDE")
                                        ? BlockType.CgInclude
                                        : BlockType.CgProgram;
                                    state = ParseState.SubShaderCg;
                                    fallThrough = true;
                                    beginCgLineNum = lineNum + 1;
                                }
                                else if (match.Equals("GrabPass") || match.Equals("Pass"))
                                {
                                    state = ParseState.PassBlock;
                                    fallThrough = true;
                                    passType = match.Equals("Pass") ? BlockType.Pass : BlockType.GrabPass;
                                }
                            }
                        }
                            break;
                        case ParseState.SubShaderCg:
                        case ParseState.PassCg:
                            int endCg = line.IndexOf("ENDCG", lineSkip, StringComparison.Ordinal);
                            if (endCg != -1)
                            {
                                string buf = "";
                                if (cgType == BlockType.CgProgram)
                                {
                                    int whichBlock = 0;
                                    for (int i = 0; i < beginCgLineNum; i++)
                                    {
                                        // if (i == cgBlocks[whichBlock].beginLineNum) {
                                        //     buf += shaderLines[i].Substring(cgBlocks[whichBlock].beginSkip) + "\n";
                                        // } else
                                        if (whichBlock >= _cgBlocks.Count)
                                        {
                                            buf += "\n";
                                        }
                                        else if (i >= _cgBlocks[whichBlock].BeginLineNum &&
                                                 i < _cgBlocks[whichBlock].EndLineNum)
                                        {
                                            buf += _shaderLines[i] + "\n";
                                            // } else if (i == cgBlocks[whichBlock].endLineNum) {
                                            //     buf += shaderLines[i].Substring(0, cgBlocks[whichBlock].endSkip) + "\n";
                                            //     whichBlock += 1;
                                        }
                                        else
                                        {
                                            buf += "\n";
                                        }
                                    }

                                    for (int i = beginCgLineNum; i < lineNum; i++)
                                    {
                                        buf += _shaderLines[i] + "\n";
                                    }
                                }

                                CgBlock b = new CgBlock(this, cgType, beginCgLineNum, lineNum, buf);
                                _passes.Add(b);
                                _cgBlocks.Add(b);
                                state = state == ParseState.SubShaderCg ? ParseState.SubShader : ParseState.PassBlock;
                            }

                            // Look for modified tag, or end of shader, or custom editor.
                            break;
                    }
                }
            }

            foreach (Block b in _passes)
            {
                CgBlock cgb = b as CgBlock;
                if (cgb != null || b.Type == BlockType.CgInclude || b.Type == BlockType.CgProgram)
                {
                    Debug.Log("Shader has a " + b.Type + " on lines " + b.BeginLineNum + "-" + b.EndLineNum +
                              " with vert:" + cgb.VertFunction + " geom:" + cgb.GeomFunction + " surf:" +
                              cgb.SurfFunction +
                              " | vert accepts input " + cgb.VertInputType + " output " + cgb.VertReturnType);
                }
                else
                {
                    Debug.Log("Shader has " + b.Type + " block on lines " + b.BeginLineNum + "-" + b.EndLineNum);
                }
            }
        }

        public bool HasSurfaceShader()
        {
            foreach (CgBlock b in _cgBlocks)
            {
                if (b.IsSurface())
                {
                    return true;
                }
            }

            return false;
        }

        public static string GETPath(Shader shader)
        {
            if (shader == null)
            {
                return null;
            }

            string path = AssetDatabase.GetAssetPath(shader);
            if (path.StartsWith("Resources/unity_builtin_extra", StringComparison.CurrentCulture) &&
                "Standard".Equals(shader.name))
            {
                string[] tmpassets = AssetDatabase.FindAssets("StandardSimple");
                foreach (string guid in tmpassets)
                {
                    path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.IndexOf(".shader", StringComparison.CurrentCulture) != -1)
                    {
                        break;
                    }
                }
            }

            // TODO: same for Legacy Shaders/Diffuse etc.
            return path;
        }

        public static string GETTempPath(Shader shader, out string error)
        {
            string assetPath = GETPath(shader);
            if (assetPath == null)
            {
                error = "No shader selected.";
                return null;
            }

            error = null;
            if (!assetPath.StartsWith("Assets/", StringComparison.CurrentCulture))
            {
                error = "Asset " + shader + " at path " + assetPath + " is not inside the Assets folder.";
                //EditorUtility.DisplayDialog ("GeomShaderGenerator", error, "OK", "");
                return null;
            }

            if (!File.Exists(assetPath))
            {
                error = "Asset " + shader + " at path " + assetPath + " file does not exist.";
                return null;
            }

            // However, no public API is exposed to give us access to this file.
            string tempPath = "Temp/GeneratedFromSurface-" + shader.name.Replace("/", "-") + ".shader";
            if (!File.Exists(tempPath))
            {
                error = "Generated surface shader " + tempPath +
                        " does not exist.\n\nPlease find the Surface shader line and click \"Show generated code\" before converting here.";
                return null;
            }

            return tempPath;
        }

        [MenuItem("CONTEXT/Shader/TestShaderParser")]
        private static void TestShaderParser(MenuCommand command)
        {
            Shader s = command.context as Shader;
            // ReSharper disable once ObjectCreationAsStatement
            new ParsedShader(s);
        }


        public class Block
        {
            public int BeginLineNum;
            public int BeginSkip;
            public int EndLineNum;
            public int EndSkip;
            public ParsedShader Shader;
            public BlockType Type;

            public Block(ParsedShader shader, BlockType type, int beginLine, int beginSkip, int endLine, int endSkip)
            {
                Shader = shader;
                EndSkip = -1;
                EndLineNum = -1;
                BeginSkip = -1;
                BeginLineNum = -1;
                Type = type;
                BeginLineNum = beginLine;
                BeginSkip = beginSkip;
                EndLineNum = endLine;
                EndSkip = endSkip;
            }
        }

        public class CgBlock : Block
        {
            public string DomainFunction;
            public string FragFunction;
            public string GeomFunction;
            public int GeomFunctionPragmaLine = -1;
            public string HullFunction;
            public bool OriginalSurfaceShader;
            private readonly Dictionary<int, string> _pragmas = new Dictionary<int, string>();
            public int ShaderTarget;
            public string SurfFunction;
            public string SurfVertFunction;
            public string VertFunction;
            public int VertFunctionPragmaLine = -1;
            public string VertInputType;
            public string VertReturnType;

            public CgBlock(ParsedShader shader, BlockType type, int beginLine, int endLine, string cgProgramSource) :
                base(shader, type, beginLine, 0, endLine, 0)
            {
                if (Type != BlockType.CgProgram)
                {
                    return;
                }

                //for (int i = beginLine; i < endLine; i++) {
                Regex re = new Regex(
                    "^\\s*(vertex|fragment|geometry|surface|domain|hull|target)\\s*(\\S+)\\s*.*(\\bvertex:(\\S+))?\\s*.*$");
                foreach (var pragmaLine in new PragmaIterator(
                    shader._shaderLines.Skip(beginLine).Take(endLine - beginLine), beginLine))
                {
                    Match m = re.Match(pragmaLine.Key);
                    if (m.Success)
                    {
                        string funcType = m.Groups[1].Value;
                        if (funcType.Equals("surface"))
                        {
                            SurfFunction = m.Groups[2].Value;
                            SurfVertFunction = m.Groups[4].Value;
                            OriginalSurfaceShader = true;
                        }
                        else if (funcType.Equals("vertex"))
                        {
                            VertFunction = m.Groups[2].Value;
                            VertFunctionPragmaLine = pragmaLine.Value;
                        }
                        else if (funcType.Equals("fragment"))
                        {
                            FragFunction = m.Groups[2].Value;
                        }
                        else if (funcType.Equals("geometry"))
                        {
                            GeomFunction = m.Groups[2].Value;
                            GeomFunctionPragmaLine = pragmaLine.Value;
                            if (ShaderTarget <= 0)
                            {
                                ShaderTarget = -40;
                            }
                        }
                        else if (funcType.Equals("domain"))
                        {
                            DomainFunction = m.Groups[2].Value;
                            if (ShaderTarget <= 0)
                            {
                                ShaderTarget = -50;
                            }
                        }
                        else if (funcType.Equals("hull"))
                        {
                            HullFunction = m.Groups[2].Value;
                            if (ShaderTarget <= 0)
                            {
                                ShaderTarget = -50;
                            }
                        }
                        else if (funcType.Equals("target"))
                        {
                            ShaderTarget = Mathf.RoundToInt(float.Parse(m.Groups[2].Value) * 10.0f);
                        }
                    }

                    _pragmas.Add(pragmaLine.Value, pragmaLine.Key);
                }

                if (ShaderTarget < 0)
                {
                    ShaderTarget = -ShaderTarget;
                }

                if (ShaderTarget == 0)
                {
                    Debug.Log("Note: shader " + Shader._shaderName + " using old shader target " + ShaderTarget / 10 +
                              "." + ShaderTarget % 10);
                    ShaderTarget = 20;
                }

                Preproc pp = new Preproc();
                CgProgramOutputCollector cgpo = new CgProgramOutputCollector(shader);
                pp.set_output_interface(cgpo);
                pp.cpp_add_define("SHADER_API_D3D11 1");
                pp.cpp_add_define("SHADER_TARGET " + ShaderTarget);
                pp.cpp_add_define("SHADER_TARGET_SURFACE_ANALYSIS 1"); // maybe?
                pp.cpp_add_define("UNITY_VERSION " + 2018420);
                pp.cpp_add_define(
                    "UNITY_PASS_SHADOWCASTER 1"); // FIXME: this is wrong. WE need to get the LightMode from tags. so we should parse tags, too.
                pp.cpp_add_define("UNITY_INSTANCING_ENABLED 1");
                //pp.cpp_add_define("UNITY_STEREO_INSTANCING_ENABLED 1");
                pp.parse_file(shader._filePath, cgProgramSource);
                string code = cgpo.GetOutputCode();
                File.WriteAllText("output_code.txt", code);

                if (SurfFunction != null)
                {
                    // TODO: for non-surface shaders, find the vertFUnction and pick out the return type.
                    VertReturnType = "v2f_" + SurfFunction;
                    VertInputType = "appdata_full";
                    VertFunction = "vert_" + SurfFunction; // will be autogenerated.
                }
                else
                {
                    Regex vertRe = new Regex("\\b(\\S+)\\b\\s+" + VertFunction + "\\s*\\(\\s*(\\S*)\\b");
                    foreach (string lin in new CommentFreeIterator(shader._shaderLines.Skip(beginLine)
                        .Take(endLine - beginLine)))
                    {
                        if (lin.IndexOf("// Surface shader code generated based on:",
                            StringComparison.CurrentCulture) != -1)
                        {
                            OriginalSurfaceShader = true;
                            /*vertReturnType = "v2f_surf";
                        vertInputType = "appdata_full";
                        break;*/
                        }

                        /*
                    int vertIndex = lin.IndexOf (" " + vertFunction + " ", StringComparison.CurrentCulture);
                    if (vertIndex != -1) {
                        vertReturnType = lin.Substring (0, vertIndex).Trim ();
                        int paren = lin.IndexOf ("(", vertIndex, StringComparison.CurrentCulture);
                        if (paren != -1) {
                            int space = lin.IndexOf (" ", paren);
                            if (space != -1) {
                                vertInputType = lin.Substring (paren + 1, space - paren - 1).Trim();
                            }
                        }
                    }
                    */
                        Match m = vertRe.Match(lin);
                        if (m.Success)
                        {
                            VertReturnType = m.Groups[1].Value;
                            VertInputType = m.Groups[2].Value;
                        }
                    }
                }
            }

            public string GETVertReturnType()
            {
                return VertReturnType;
            }

            public bool IsSurface()
            {
                return SurfFunction != null;
            }

            public bool HasGeometry()
            {
                return GeomFunction != null;
            }

            private class CgProgramOutputCollector : Preproc.IOutputInterface
            {
                private readonly StringBuilder _outputCode = new StringBuilder();
                private readonly ParsedShader _parsedShader;
                private bool _wasNewline;

                public CgProgramOutputCollector(ParsedShader ps)
                {
                    _parsedShader = ps;
                }

                public void Emit(string s, string file, int line, int column)
                {
                    if (_wasNewline && s.Trim() == "" && s.EndsWith("\n"))
                    {
                        return;
                    }

                    if (s.Trim() != "" || s.EndsWith("\n"))
                    {
                        _wasNewline = s.EndsWith("\n");
                    }

                    _outputCode.Append(s);
                }

                public void EmitError(string msg)
                {
                    Debug.LogError(msg);
                }

                public void EmitWarning(string msg)
                {
                    Debug.LogWarning(msg);
                }

                public string IncludeFile(string fileContext, ref string filename)
                {
                    Debug.Log("Found a pound include " + fileContext + "," + filename);
                    return _parsedShader.GETCgincSource(fileContext, ref filename);
                }

                public string GetOutputCode()
                {
                    return _outputCode.ToString();
                }
            }
        }

        public class CommentFreeIterator : IEnumerable<string>
        {
            private readonly IEnumerable<string> _sourceLines;

            public CommentFreeIterator(IEnumerable<string> sourceLines)
            {
                _sourceLines = sourceLines;
            }

            public IEnumerator<string> GetEnumerator()
            {
                int comment = 0;
                foreach (string xline in _sourceLines)
                {
                    string line = ParserRemoveComments(xline, ref comment);
                    yield return line;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public static string ParserRemoveComments(string line, ref int comment)
            {
                int lineSkip = 0;
                bool cisOpenQuote = false;


                while (true)
                {
                    //Debug.Log ("Looking for comment " + lineNum);
                    int openQuote = line.IndexOf("\"", lineSkip, StringComparison.CurrentCulture);
                    if (cisOpenQuote)
                    {
                        if (openQuote == -1)
                        {
                            //Debug.Log("C-Open quote ignore " + lineSkip);
                            break;
                        }

                        lineSkip = openQuote + 1;
                        bool esc = false;
                        int i = lineSkip - 1;
                        while (i > 0 && line[i] == '\\')
                        {
                            esc = !esc;
                            i--;
                        }

                        if (!esc)
                        {
                            cisOpenQuote = false;
                        }

                        //Debug.Log("C-Open quote end " + lineSkip);
                        continue;
                    }

                    //Debug.Log ("Looking for comment " + lineSkip);
                    int commentIdx;
                    if (comment == 1)
                    {
                        commentIdx = line.IndexOf("*/", lineSkip, StringComparison.CurrentCulture);
                        if (commentIdx != -1)
                        {
                            line = new string(' ', commentIdx + 2) + line.Substring(commentIdx + 2);
                            lineSkip = commentIdx + 2;
                            comment = 0;
                        }
                        else
                        {
                            line = "";
                            break;
                        }
                    }

                    commentIdx = line.IndexOf("//", lineSkip, StringComparison.CurrentCulture);
                    int commentIdx2 = line.IndexOf("/*", lineSkip, StringComparison.CurrentCulture);
                    if (commentIdx2 != -1 && (commentIdx == -1 || commentIdx > commentIdx2))
                    {
                        commentIdx = -1;
                    }

                    if (openQuote != -1 && (openQuote < commentIdx || commentIdx == -1) &&
                        (openQuote < commentIdx2 || commentIdx2 == -1))
                    {
                        cisOpenQuote = true;
                        lineSkip = openQuote + 1;
                        //Debug.Log("C-Open quote start " + lineSkip);
                        continue;
                    }

                    if (commentIdx != -1)
                    {
                        line = line.Substring(0, commentIdx);
                        break;
                    }

                    commentIdx = commentIdx2;
                    if (commentIdx != -1)
                    {
                        int endCommentIdx = line.IndexOf("*/", lineSkip, StringComparison.CurrentCulture);
                        if (endCommentIdx != -1)
                        {
                            line = line.Substring(0, commentIdx) + new string(' ', endCommentIdx + 2 - commentIdx) +
                                   line.Substring(endCommentIdx + 2);
                            lineSkip = endCommentIdx + 2;
                        }
                        else
                        {
                            line = line.Substring(0, commentIdx);
                            comment = 1;
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                return line;
            }
        }

        public class PragmaIterator : IEnumerable<KeyValuePair<string, int>>
        {
            private readonly IEnumerable<string> _sourceLines;
            private readonly int _startLine;

            public PragmaIterator(IEnumerable<string> sourceLines, int startLine)
            {
                _sourceLines = sourceLines;
                _startLine = startLine;
            }

            public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
            {
                Regex re = new Regex("^\\s*#\\s*pragma\\s+(.*)$");
                //Regex re = new Regex ("^\\s*#\\s*pragma\\s+geometry\\s+\(\\S*\)\\s*$");
                int ln = _startLine - 1;
                foreach (string xline in _sourceLines)
                {
                    string line = xline;
                    ln++;
                    /*if (ln < startLine + 10) { Debug.Log ("Check line " + ln +"/" + line); }
                line = line.Trim ();
                if (line.StartsWith("#", StringComparison.CurrentCulture)) {
                    Debug.Log ("Check pragma " + ln + "/" + line);
                }*/
                    if (re.IsMatch(line))
                    {
                        //Debug.Log ("Matched pragma " + line);
                        yield return new KeyValuePair<string, int>(re.Replace(line, match => match.Groups[1].Value),
                            ln);
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}