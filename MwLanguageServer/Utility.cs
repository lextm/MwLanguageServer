﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using LanguageServer.VsCode.Contracts;
using Microsoft.Extensions.Configuration;
using MwLanguageServer.Localizable;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace MwLanguageServer
{
    static class Utility
    {

        public static readonly JsonSerializer CamelCaseJsonSerializer = new JsonSerializer
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static void LogException(this ILogger logger, Exception ex, [CallerMemberName] string caller = null)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            logger.Error(ex, "Error in {method}.", caller);
        }

        public static string[] ProcessCommandlineArguments(IEnumerable<string> args)
        {
            return args.Select(a =>
                {
                    if (!a.Contains('=')) a = a + "=true";
                    return a;
                })
                .ToArray();
        }

        public static string ArgumentName(this TemplateArgument arg)
        {
            if (arg.Name != null) return MwParserUtility.NormalizeTemplateArgumentName(arg.Name);
            var parent = arg.ParentNode as Template;
            if (parent == null) return null;
            var unnamedCt = 0;
            foreach (var a in parent.Arguments)
            {
                if (a.Name == null) unnamedCt++;
                if (a == arg) return unnamedCt.ToString();
            }
            return null;
        }

        public static int IndexOf<T>(this IEnumerable<T> source, T item)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            int index = 0;
            foreach (var i in source)
            {
                if (EqualityComparer<T>.Default.Equals(i, item)) return index;
                index++;
            }
            return -1;
        }

        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            int index = 0;
            foreach (var i in source)
            {
                if (predicate(i)) return index;
                index++;
            }
            return -1;
        }

        public static string EscapeMd(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var builder = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (c == '*' || c == '_' || c == '`' || c == '\\')
                    builder.Append('\\');
                builder.Append(c);
            }
            return builder.ToString();
        }

        private static string WikiTitleMd(Node node)
        {
            var text = node?.ToPlainText();
            if (string.IsNullOrEmpty(text)) return "…";
            return EscapeMd(MwParserUtility.NormalizeTitle(node));
        }

        private static string WikiArgumentMd(Node node)
        {
            var text = node?.ToPlainText();
            if (string.IsNullOrEmpty(text)) return "…";
            return EscapeMd(MwParserUtility.NormalizeTemplateArgumentName(node));
        }

        public static string NodeToMd(Node node)
        {
            string label;
            switch (node)
            {
                case Template t:
                    label = $"{{{{**{WikiTitleMd(t.Name)}**}}}}";
                    break;
                case TemplateArgument ta:
                    var tp = ta.ParentNode as Template;
                    label = $"{{{{{WikiTitleMd(tp?.Name)} | **{EscapeMd(ta.ArgumentName())}**=…}}}}";
                    break;
                case ArgumentReference ar:
                    label = $"{{{{{{**{WikiArgumentMd(ar.Name)}**}}}}}}";
                    break;
                case WikiLink wl:
                    label = $"[[{WikiTitleMd(wl.Target)}]]";
                    break;
                case ExternalLink el:
                    label = $"[{EscapeMd(el.Target?.ToString().Trim())}]";
                    break;
                case FormatSwitch fs:
                    label = fs.ToString();
                    break;
                case TagNode tn:
                    label = $"&lt;**{EscapeMd(tn.Name)}**&gt;";
                    break;
                case TagAttribute ta:
                    var tap = ta.ParentNode as TagNode;
                    label = $"&lt;{EscapeMd(tap?.Name?.Trim())} **{EscapeMd(ta.Name?.ToString().Trim())}**=… &gt;";
                    break;
                case Comment c:
                    label = "&lt;!-- … --&gt;";
                    break;
                default:
                    label = node.GetType().Name;
                    break;
            }
            return label;
        }

        public static string ExpandTransclusionTitle(string title)
        {
            if (title == null) throw new ArgumentNullException(nameof(title));
            Debug.Assert(title == MwParserUtility.NormalizeTitle(title));
            if (title.StartsWith(":")) return title.Substring(1);
            if (!title.Contains(':')) return "Template:" + title;
            // Something like {{Test:abcd}}, here we treat it as is with namespace name
            return title;
        }

        public static bool IsTemplateTitle(string title)
        {
            if (title == null) throw new ArgumentNullException(nameof(title));
            Debug.Assert(title == MwParserUtility.NormalizeTitle(title));
            return title.StartsWith("Template:");
        }

        public static Range ToRange(this IWikitextLineInfo thisNode)
        {
            if (thisNode == null) throw new ArgumentNullException(nameof(thisNode));
            Debug.Assert(thisNode.HasLineInfo);
            return new Range(thisNode.StartLineNumber, thisNode.StartLinePosition,
                thisNode.EndLineNumber, thisNode.EndLinePosition);
        }

        public static Range CollapseToEnd(this Range range)
        {
            return new Range(range.End, range.End);
        }
    }
}
