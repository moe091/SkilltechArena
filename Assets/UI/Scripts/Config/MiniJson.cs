// Minimal MiniJson (public-domain style). Good enough for Firebase maps.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static class MiniJson
{
    public static object Deserialize(string json) { return Json.Deserialize(json); }
    public static string Serialize(object obj) { return Json.Serialize(obj); }

    sealed class Json
    {
        public static object Deserialize(string json) { var p = new Parser(json); return p.ParseValue(); }
        public static string Serialize(object obj) { var s = new Serializer(); s.SerializeValue(obj); return s.sb.ToString(); }

        class Parser
        {
            string json; int i;
            public Parser(string s) { json = s; }
            char Peek => i < json.Length ? json[i] : '\0';
            char Next() { return i < json.Length ? json[i++] : '\0'; }
            void WS() { while (char.IsWhiteSpace(Peek)) i++; }

            public object ParseValue()
            {
                WS();
                switch (Peek)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': i += 4; return true;
                    case 'f': i += 5; return false;
                    case 'n': i += 4; return null;
                    default: return ParseNumber();
                }
            }
            Dictionary<string, object> ParseObject()
            {
                var d = new Dictionary<string, object>(); Next();
                for (; ; )
                {
                    WS(); if (Peek == '}') { Next(); break; }
                    var key = ParseString(); WS(); Next(); // :
                    var val = ParseValue(); d[key] = val; WS();
                    if (Peek == ',') { Next(); continue; }
                    if (Peek == '}') { Next(); break; }
                }
                return d;
            }
            List<object> ParseArray()
            {
                var l = new List<object>(); Next();
                for (; ; )
                {
                    WS(); if (Peek == ']') { Next(); break; }
                    l.Add(ParseValue()); WS();
                    if (Peek == ',') { Next(); continue; }
                    if (Peek == ']') { Next(); break; }
                }
                return l;
            }
            string ParseString()
            {
                var sb = new StringBuilder(); Next();
                while (true)
                {
                    var c = Next();
                    if (c == '"') break;
                    if (c == '\\')
                    {
                        var n = Next();
                        if (n == '"' || n == '\\' || n == '/') sb.Append(n);
                        else if (n == 'b') sb.Append('\b');
                        else if (n == 'f') sb.Append('\f');
                        else if (n == 'n') sb.Append('\n');
                        else if (n == 'r') sb.Append('\r');
                        else if (n == 't') sb.Append('\t');
                        else if (n == 'u') { var hex = json.Substring(i, 4); sb.Append((char)Convert.ToInt32(hex, 16)); i += 4; }
                    }
                    else sb.Append(c);
                }
                return sb.ToString();
            }
            object ParseNumber()
            {
                int start = i;
                while ("-+0123456789.eE".IndexOf(Peek) >= 0) i++;
                var s = json.Substring(start, i - start);
                if (s.IndexOf('.') >= 0 || s.IndexOf('e') >= 0 || s.IndexOf('E') >= 0) return double.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
                return long.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        class Serializer
        {
            public StringBuilder sb = new StringBuilder();
            public void SerializeValue(object v)
            {
                if (v == null) { sb.Append("null"); return; }
                if (v is string s) { Str(s); return; }
                if (v is bool b) { sb.Append(b ? "true" : "false"); return; }
                if (v is IDictionary d) { Obj(d); return; }
                if (v is IList l) { Arr(l); return; }
                if (v is IFormattable f) { sb.Append(f.ToString(null, System.Globalization.CultureInfo.InvariantCulture)); return; }
                Str(v.ToString());
            }
            void Obj(IDictionary d)
            {
                sb.Append('{'); bool first = true;
                foreach (DictionaryEntry e in d)
                {
                    if (!first) sb.Append(','); first = false;
                    Str(e.Key.ToString()); sb.Append(':'); SerializeValue(e.Value);
                }
                sb.Append('}');
            }
            void Arr(IList a)
            {
                sb.Append('[');
                for (int j = 0; j < a.Count; j++) { if (j > 0) sb.Append(','); SerializeValue(a[j]); }
                sb.Append(']');
            }
            void Str(string s)
            {
                sb.Append('"');
                foreach (var c in s)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < ' ') sb.Append("\\u" + ((int)c).ToString("x4"));
                            else sb.Append(c); break;
                    }
                }
                sb.Append('"');
            }
        }
    }
}
