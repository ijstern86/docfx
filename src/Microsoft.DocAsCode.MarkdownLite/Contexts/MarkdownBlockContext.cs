﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownBlockContext : IMarkdownContext
    {
        public const string IsTop = "IsTop";

        public MarkdownBlockContext(ImmutableList<IMarkdownRule> rules, IMarkdownContext inlineContext)
            : this (rules, inlineContext, ImmutableDictionary<string, object>.Empty.Add(IsTop, true))
        {
        }


        protected MarkdownBlockContext(ImmutableList<IMarkdownRule> rules, IMarkdownContext inlineContext, ImmutableDictionary<string, object> variables)
        {
            Rules = rules;
            InlineContext = inlineContext;
            Variables = variables;
        }

        public IMarkdownContext InlineContext { get; }

        public ImmutableList<IMarkdownRule> Rules { get; }

        public ImmutableDictionary<string, object> Variables { get; private set; }

        public virtual IMarkdownContext CreateContext(ImmutableDictionary<string, object> variables)
        {
            var clone = (MarkdownBlockContext)MemberwiseClone();
            clone.Variables = variables;
            return clone;
        }
    }
}
