#region

using System;
using System.Globalization;
using UnityEngine;

//using VRC.SDK3.Avatars.Components;

#endregion

namespace ShaderExtension
{
    #region

    #endregion

    // #if UNITY_EDITOR
    public class Tokenizer
    {
        public const int Eof = -1;

        public const int MAXTokLen = 4096;

        // enum markertype {
        public const int MTSinglelineCommentStart = 0;
        public const int MTMultilineCommentStart = 1;
        public const int MTMultilineCommentEnd = 2;

        public const int MTMAX = MTMultilineCommentEnd;
        // }

        public const int MAXCustomTokens = 32;

        //enum tokentype {
        public const int TtIdentifier = 1;
        public const int TtSqstringLit = 2;
        public const int TtDqstringLit = 3;
        public const int TtEllipsis = 4;
        public const int TtHexINTLit = 5;
        public const int TtOctINTLit = 6;
        public const int TtDecINTLit = 7;
        public const int TtFloatLit = 8;

        public const int TtSep = 9;

        /* errors and similar */
        public const int TtUnknown = 10;
        public const int TtOverflow = 11;
        public const int TtWidecharLit = 12;
        public const int TtWidestringLit = 13;
        public const int TtEof = 14;
        public const int TtCustom = 1000; /* start user defined tokentype values */

        // enum tokenizer_flags {
        public const int TfParseStrings = 1 << 0;
        public const int TfParseWideStrings = 1 << 1;
        private readonly TokenizerGetcBuf _getcBuf = new TokenizerGetcBuf();
        public string Buf = ""; //new StringBuilder();
        public int Column;
        public int CustomCount;
        public string[] CustomTokens = new string[MAXCustomTokens];
        public string Filename;

        public int Flags;
        // };

        public int Line;
        public string[] Marker = new string[MTMAX + 1];
        public bool Peeking;
        public Token PeekToken;

        public void tokenizer_set_filename(string fn)
        {
            Filename = fn;
        }

        public int tokenizer_ftello()
        {
            return _getcBuf.Cnt;
        }

        public int tokenizer_ungetc(int c)
        {
            --_getcBuf.Cnt;
            return c;
        }

        public int tokenizer_getc()
        {
            if (_getcBuf.Cnt >= _getcBuf.Buf.Length)
            {
                return Eof;
            }

            int c = _getcBuf.Buf[_getcBuf.Cnt];
            ++_getcBuf.Cnt;
            return c;
        }

        public int tokenizer_peek()
        {
            if (Peeking) return PeekToken.Value;
            int ret = tokenizer_getc();
            if (ret != Eof) tokenizer_ungetc(ret);
            return ret;
        }

        public bool tokenizer_peek_token(out Token tok)
        {
            if (Peeking)
            {
                // Lyuma bugfix!
                tok = new Token();
                tok.Column = PeekToken.Column;
                tok.Line = PeekToken.Line;
                tok.Type = PeekToken.Type;
                tok.Value = PeekToken.Value;
                return true;
            }

            bool ret = tokenizer_next(out tok);
            PeekToken = new Token();
            PeekToken.Column = tok.Column;
            PeekToken.Line = tok.Line;
            PeekToken.Type = tok.Type;
            PeekToken.Value = tok.Value;
            Peeking = true;
            return ret;
        }

        public void tokenizer_register_custom_token(int tokentype, string str)
        {
            if (!(tokentype >= TtCustom && tokentype < TtCustom + MAXCustomTokens))
            {
                Debug.LogError("Wrong tokentype " + tokentype);
            }

            int pos = tokentype - TtCustom;
            CustomTokens[pos] = str;
            if (pos + 1 > CustomCount) CustomCount = pos + 1;
        }

        public string tokentype_to_str(int tt)
        {
            // tokentype
            switch (tt)
            {
                case TtIdentifier: return "iden";
                case TtWidecharLit: return "widechar";
                case TtWidestringLit: return "widestring";
                case TtSqstringLit: return "single-quoted string";
                case TtDqstringLit: return "double-quoted string";
                case TtEllipsis: return "ellipsis";
                case TtHexINTLit: return "hexint";
                case TtOctINTLit: return "octint";
                case TtDecINTLit: return "decint";
                case TtFloatLit: return "float";
                case TtSep: return "separator";
                case TtUnknown: return "unknown";
                case TtOverflow: return "overflow";
                case TtEof: return "eof";
            }

            return "????";
        }

        private bool has_f_tail(string p)
        {
            return p.EndsWith("f", StringComparison.CurrentCultureIgnoreCase) ||
                   p.EndsWith("h", StringComparison.CurrentCultureIgnoreCase);
        }

        private bool has_ul_tail(string p)
        {
            return p.EndsWith("u", StringComparison.CurrentCultureIgnoreCase);
        }

        private bool is_hex_int_literal(string s)
        {
            int i = 0;
            if (is_plus_or_minus(s[0]))
            {
                i++;
            }

            while (i < s.Length && char.IsWhiteSpace(s[i]))
            {
                i++;
            }

            if (i > s.Length - 4)
            {
                return false;
            }

            if (s[i] == '0' && (s[i + 1] == 'x' || s[i + 1] == 'X'))
            {
                long ret;
                return long.TryParse(s.Substring(i + 2, s.Length - (has_ul_tail(s) ? 1 : 0) - (i + 2)),
                    NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ret);
            }

            return false;
        }

        private bool is_plus_or_minus(char c)
        {
            return c == '-' || c == '+';
        }

        private bool is_dec_int_literal(string str)
        {
            string s = has_ul_tail(str) ? str.Substring(0, str.Length - 1) : str;
            long ret;
            return long.TryParse(s, out ret);
        }

        private bool is_float_literal(string str)
        {
            string s = has_f_tail(str) ? str.Substring(0, str.Length - 1) : str;
            float res;
            return float.TryParse(s, out res);
        }

        private int is_valid_float_until(string s, int until)
        {
            bool gotDigits = false, gotDot = false;
            int off = 0;
            while (off < until)
            {
                if (char.IsDigit(s[off])) gotDigits = true;
                else if (s[off] == '.')
                {
                    if (gotDot) return 0;
                    gotDot = true;
                }
                else return 0;

                ++off;
            }

            return (gotDigits ? 1 : 0) | ((gotDot ? 1 : 0) << 1);
        }

        private bool is_oct_int_literal(string s)
        {
            int i = 0;
            if (s[0] == '-') i++;
            if (s[0] != '0') return false;
            while (i < s.Length)
            {
                if (s[i] < '0' || s[i] > '7') return false;
                i++;
            }

            return true;
        }

        private bool is_identifier(string s)
        {
            if (!char.IsLetter(s[0]) && s[0] != '_')
            {
                return false;
            }

            for (int i = 1; i < s.Length; i++)
            {
                if (!char.IsLetterOrDigit(s[i]) && s[i] != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private int Categorize(string s)
        {
            // tokentype
            if (is_hex_int_literal(s)) return TtHexINTLit;
            if (is_dec_int_literal(s)) return TtDecINTLit;
            if (is_oct_int_literal(s)) return TtOctINTLit;
            if (is_float_literal(s)) return TtFloatLit;
            if (is_identifier(s)) return TtIdentifier;
            return TtUnknown;
        }


        private bool is_sep(char c)
        {
            if (c == '_')
            {
                return false;
            }

            return !char.IsLetterOrDigit(c);
            // return Char.IsPunctuation(c) || Char.IsSeparator(c) || Char.IsWhiteSpace(c);
        }

        private bool apply_coords(ref Token outt, int end, bool retval)
        {
            outt.Line = Line;
            int len = end;
            outt.Column = Column - len;
            // if(len + 1 >= bufsize) {
            //     outt.type = TT_OVERFLOW;
            //     return false;
            // }
            return retval;
        }

        private int assign_bufchar(int s, int c)
        {
            Column++;
            Buf += (char)c;
            return s + 1;
        }

        private bool get_string(char quoteChar, out Token tout, bool wide)
        {
            int s = 1;
            bool escaped = false;
            int end = MAXTokLen - 2;
            tout = new Token();
            while (s < end)
            {
                int c = tokenizer_getc();
                if (c == Eof)
                {
                    tout.Type = TtEof;
                    // buf = 0;
                    return apply_coords(ref tout, s, false);
                }

                if (c == '\\')
                {
                    c = tokenizer_getc();
                    if (c == '\n') continue;
                    tokenizer_ungetc(c);
                    c = '\\';
                }

                if (c == '\n')
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    tokenizer_ungetc(c);
                    tout.Type = TtUnknown;
                    s = assign_bufchar(s, 0);
                    return apply_coords(ref tout, s, false);
                }

                if (!escaped)
                {
                    if (c == quoteChar)
                    {
                        s = assign_bufchar(s, c);
                        // *s = 0;
                        //s = assign_bufchar(t, s, 0);
                        if (!wide)
                            tout.Type = quoteChar == '"' ? TtDqstringLit : TtSqstringLit;
                        else
                            tout.Type = quoteChar == '"' ? TtWidestringLit : TtWidecharLit;
                        return apply_coords(ref tout, s, true);
                    }

                    if (c == '\\') escaped = true;
                }
                else
                {
                    escaped = false;
                }

                s = assign_bufchar(s, c);
            }

            // buf[MAX_TOK_LEN-1] = 0;
            tout.Type = TtOverflow;
            return apply_coords(ref tout, s, false);
        }

        /* if sequence found, next tokenizer call will point after the sequence */
        private bool sequence_follows(int c, string which)
        {
            if (which.Length == 0) return false;
            int i = 0;
            while (i < which.Length && c == which[i])
            {
                if (++i >= which.Length) break;
                c = tokenizer_getc();
            }

            if (i >= which.Length) return true;
            while (i > 0)
            {
                tokenizer_ungetc(c);
                c = which[--i];
            }

            return false;
        }

        public bool tokenizer_skip_chars(string chars, out int count)
        {
            if (Peeking)
            {
                Debug.LogError("Assertion failure: skip chars while peeking");
            }

            int c;
            count = 0;
            while (true)
            {
                c = tokenizer_getc();
                if (c == Eof) return false;
                int s = 0;
                bool match = false;
                while (s < chars.Length)
                {
                    if (c == chars[s])
                    {
                        ++count;
                        match = true;
                        break;
                    }

                    ++s;
                }

                if (!match)
                {
                    tokenizer_ungetc(c);
                    return true;
                }
            }
        }

        public bool tokenizer_read_until(string marker, bool stopAtNl)
        {
            int c;
            bool markerIsNl = marker == "\n";
            int s = 0;
            Buf = ""; //.Clear();
            while (true)
            {
                c = tokenizer_getc();
                if (c == Eof)
                {
                    // *s = 0;
                    return false;
                }

                if (c == '\n')
                {
                    Line++;
                    Column = 0;
                    if (stopAtNl)
                    {
                        // *s = 0;
                        if (markerIsNl) return true;
                        return false;
                    }
                }

                if (!sequence_follows(c, marker))
                    s = assign_bufchar(s, c);
                else
                    break;
            }

            // *s = 0;
            int i;
            for (i = marker.Length; i > 0;)
                tokenizer_ungetc(marker[--i]);
            return true;
        }

        private bool ignore_until(string marker, int colAdvance, bool stopAtNewline = false)
        {
            Column += colAdvance;
            int c;
            do
            {
                c = tokenizer_getc();
                if (c == Eof) return false;
                if (c == '\n')
                {
                    if (stopAtNewline)
                    {
                        tokenizer_ungetc(c); // Lyuma bugfix
                        /*
                        #define FOO bar // some comment
                        #endif
                        */
                        return true;
                    }

                    Line++;
                    Column = 0;
                }
                else Column++;
            } while (!sequence_follows(c, marker));

            Column += marker.Length - 1;
            return true;
        }

        public void tokenizer_skip_until(string marker)
        {
            ignore_until(marker, 0, marker == "\n");
        }

        public bool tokenizer_next(out Token tout)
        {
            int s = 0;
            int c = 0;
            if (Peeking)
            {
                tout = new Token();
                tout.Value = PeekToken.Value;
                tout.Column = PeekToken.Column;
                tout.Line = PeekToken.Line;
                tout.Type = PeekToken.Type;
                Peeking = false;
                PeekToken = new Token();
                return true;
            }

            Buf = ""; //.Clear();
            tout = new Token();
            tout.Value = 0;
            while (true)
            {
                c = tokenizer_getc();
                if (c == Eof)
                {
                    break;
                }

                /* components of multi-line comment marker might be terminals themselves */
                if (sequence_follows(c, Marker[MTMultilineCommentStart]))
                {
                    ignore_until(Marker[MTMultilineCommentEnd], Marker[MTMultilineCommentStart].Length);
                    continue;
                }

                if (sequence_follows(c, Marker[MTSinglelineCommentStart]))
                {
                    ignore_until("\n", Marker[MTSinglelineCommentStart].Length, true);
                    continue;
                }

                if (is_sep((char)c))
                {
                    if (s != 0 && c == '\\' && !char.IsWhiteSpace(Buf[s - 1]))
                    {
                        c = tokenizer_getc();
                        if (c == '\n') continue;
                        tokenizer_ungetc(c);
                        c = '\\';
                    }
                    else if (is_plus_or_minus((char)c) && s > 1 &&
                             (Buf[s - 1] == 'E' || Buf[s - 1] == 'e' || Buf[s - 1] == 'F' || Buf[s - 1] == 'f' ||
                              Buf[s - 1] == 'H' || Buf[s - 1] == 'h') && is_valid_float_until(Buf, s - 1) != 0)
                    {
                        goto process_char;
                    }
                    else if (c == '.' && s != 0 && is_valid_float_until(Buf, s) == 1)
                    {
                        goto process_char;
                    }
                    else if (c == '.' && s == 0)
                    {
                        bool jump = false;
                        c = tokenizer_getc();
                        if (char.IsDigit((char)c)) jump = true;
                        tokenizer_ungetc(c);
                        c = '.';
                        if (jump) goto process_char;
                    }

                    tokenizer_ungetc(c);
                    break;
                }

                if ((Flags & TfParseWideStrings) != 0 && s == 0 && c == 'L')
                {
                    c = tokenizer_getc();
                    tokenizer_ungetc(c);
                    tokenizer_ungetc('L');
                    if (c == '\'' || c == '\"') break;
                }

                process_char: ;
                s = assign_bufchar(s, c);
                if (Column + 1 >= MAXTokLen)
                {
                    tout.Type = TtOverflow;
                    return apply_coords(ref tout, s, false);
                }
            }

            if (s == 0)
            {
                if (c == Eof)
                {
                    tout.Type = TtEof;
                    return apply_coords(ref tout, s, true);
                }

                bool wide = false;
                c = tokenizer_getc();
                if ((Flags & TfParseWideStrings) != 0 && c == 'L')
                {
                    c = tokenizer_getc();
                    if (!(c == '\'' || c == '\"'))
                    {
                        Debug.LogError("bad c " + c);
                    }

                    wide = true;
                    goto string_handling;
                }

                if (c == '.' && sequence_follows(c, "..."))
                {
                    Buf = "..."; //.Clear();
                    //buf.Append("...");
                    tout.Type = TtEllipsis;
                    return apply_coords(ref tout, s + 3, true);
                }

                {
                    int i;
                    for (i = 0; i < CustomCount; i++)
                        if (sequence_follows(c, CustomTokens[i]))
                        {
                            string p = CustomTokens[i];
                            int pi = 0;
                            while (pi < p.Length)
                            {
                                s = assign_bufchar(s, p[pi]);
                                pi++;
                            }

                            // *s = 0;
                            tout.Type = TtCustom + i;
                            return apply_coords(ref tout, s, true);
                        }
                }

                string_handling:
                s = assign_bufchar(s, c);
                // *s = 0;
                //s = assign_bufchar(t, s, 0);
                if (c == '"' || c == '\'')
                    if ((Flags & TfParseStrings) != 0)
                        return get_string((char)c, out tout, wide);
                tout.Type = TtSep;
                tout.Value = c;
                if (c == '\n')
                {
                    apply_coords(ref tout, s, true);
                    Line++;
                    Column = 0;
                    return true;
                }

                return apply_coords(ref tout, s, true);
            }

            //s = assign_bufchar(t, s, 0);
            // s = 0;
            tout.Type = Categorize(Buf);
            return apply_coords(ref tout, s, tout.Type != TtUnknown);
        }

        public void tokenizer_set_flags(int flags)
        {
            Flags = flags;
        }

        public int tokenizer_get_flags()
        {
            return Flags;
        }

        public void tokenizer_init(string fileContents, int flags)
        {
            if (fileContents.Length < -1)
            {
                Debug.Log("wat");
            }

            for (int i = 0; i < Marker.Length; i++)
            {
                Marker[i] = "";
            }

            _getcBuf.Buf = fileContents;
            _getcBuf.Cnt = 0;
            Line = 1;
            Flags = flags;
        }

        public void tokenizer_register_marker(int mt, string marker)
        {
            Marker[mt] = marker;
        }

        public void tokenizer_rewind()
        {
            int flags = Flags;
            string fn = Filename;
            tokenizer_init(_getcBuf.Buf, flags);
            tokenizer_set_filename(fn);
        }

        private class TokenizerGetcBuf
        {
            public string Buf = "";
            public int Cnt;
        }

        public struct Token
        {
            public int Type; // tokentype
            public int Line;
            public int Column;
            public int Value;
        }
    }
}