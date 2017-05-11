﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using LanguageServer.VsCode.Contracts;
using MwLanguageServer.Localizable;

namespace MwLanguageServer.Store
{

    public class PageInfo
    {

        public PageInfo(string name, string summary, IReadOnlyList<TemplateArgumentInfo> arguments, bool isInferred)
        {
            Name = name;
            Summary = summary;
            Arguments = arguments;
            IsInferred = isInferred;
        }

        public string Name { get; }

        public string Summary { get; }

        public IReadOnlyList<TemplateArgumentInfo> Arguments { get; }

        public bool IsInferred { get; }

        private volatile SignatureInformation signatureCache;

        public SignatureInformation ToSignatureInformation()
        {
            if (signatureCache == null)
            {
                var labelBuilder = new StringBuilder("{{");
                labelBuilder.Append(Name);
                var sig = new SignatureInformation();
                if (Arguments.Count > 0)
                {
                    sig.Parameters = Arguments.Select(a => a.ToParameterInformation()).ToImmutableArray();
                    foreach (var a in Arguments)
                    {
                        labelBuilder.Append(" |");
                        labelBuilder.Append(a.Name);
                        labelBuilder.Append("=…");
                    }
                }
                labelBuilder.Append("}}");
                sig.Label = labelBuilder.ToString();
                sig.Documentation = Summary;
                signatureCache = sig;
            }
            return signatureCache;
        }
    }

    public class TemplateArgumentInfo
    {
        public TemplateArgumentInfo(string name, string summary)
        {
            Name = name;
            Summary = summary;
        }

        public string Name { get; }

        public string Summary { get; }

        /// <inheritdoc />
        public override string ToString() => "{{{" + Name + "}}}";

        public ParameterInformation ToParameterInformation()
        {
            var summary = Summary;
            if (!string.IsNullOrEmpty(summary)) summary += "\n\n";
            summary += Prompts.UsageColon;
            summary += "|" + Name + "=…";
            return new ParameterInformation(Name, summary);
        }
    }
}
