﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    /// <summary>
    /// Copies doc comments to items marked with 'inheritdoc' from interfaces and base classes.
    /// </summary>
    public class CopyInherited : IResolverPipeline
    {
        public void Run(MetadataModel yaml, ResolverContext context)
        {
            TreeIterator.Preorder(
                yaml.TocYamlViewModel,
                null,
                s => s.IsInvalid ? null : s.Items,
                (current, parent) =>
                {
                    if (current.IsInheritDoc)
                    {
                        InheritDoc(current, context);
                    }
                    return true;
                });

            RemoveInheritDocOnlyMembers(context);
            RemoveInheritDocOnlyReferenences(context);
        }

        private static void InheritDoc(MetadataItem dest, ResolverContext context)
        {
            dest.IsInheritDoc = false;

            switch (dest.Type)
            {
                case MemberType.Constructor:
                    if (dest.Parent == null || dest.Syntax == null || dest.Syntax.Parameters == null)
                    {
                        return;
                    }
                    Debug.Assert(dest.Parent.Type == MemberType.Class);

                    //try to find the base class
                    if (dest.Parent.Inheritance?.Count == 0)
                    {
                        return;
                    }
                    if (!context.Members.TryGetValue(dest.Parent.Inheritance[dest.Parent.Inheritance.Count - 1], out MetadataItem baseClass))
                    {
                        return;
                    }
                    if (baseClass.Items == null)
                    {
                        return;
                    }

                    //look a constructor in the base class which has a matching signature
                    foreach (var ctor in baseClass.Items)
                    {
                        if (ctor.Type != MemberType.Constructor)
                        {
                            continue;
                        }
                        if (ctor.Syntax == null || ctor.Syntax.Parameters == null)
                        {
                            continue;
                        }
                        if (ctor.Syntax.Parameters.Count != dest.Syntax.Parameters.Count)
                        {
                            continue;
                        }

                        bool parametersMatch = true;
                        for (int ndx = 0; ndx < dest.Syntax.Parameters.Count; ndx++)
                        {
                            var myParam = dest.Syntax.Parameters[ndx];
                            var baseParam = ctor.Syntax.Parameters[ndx];
                            if (myParam.Name != baseParam.Name)
                            {
                                parametersMatch = false;
                            }
                            if (myParam.Type != baseParam.Type)
                            {
                                parametersMatch = false;
                            }
                        }

                        if (parametersMatch)
                        {
                            Copy(dest, ctor, context);
                            return;
                        }
                    }
                    break;

                case MemberType.Method:
                case MemberType.Property:
                case MemberType.Event:
                    Copy(dest, dest.Overridden, context);
                    if (dest.Implements != null)
                    {
                        foreach (var item in dest.Implements)
                        {
                            Copy(dest, item, context);
                        }
                    }
                    break;

                case MemberType.Class:
                    if (dest.Inheritance.Count != 0)
                    {
                        Copy(dest, dest.Inheritance[dest.Inheritance.Count - 1], context);
                    }
                    if (dest.Implements != null)
                    {
                        foreach (var item in dest.Implements)
                        {
                            Copy(dest, item, context);
                        }
                    }
                    break;
            }
        }

        private static void Copy(MetadataItem dest, string srcName, ResolverContext context)
        {
            if (string.IsNullOrEmpty(srcName) || !context.Members.TryGetValue(srcName, out MetadataItem src))
            {
                return;
            }

            Copy(dest, src, context);
        }

        private static void Copy(MetadataItem dest, MetadataItem src, ResolverContext context)
        {
            if (src.IsInheritDoc)
            {
                InheritDoc(src, context);
            }

            dest.CopyInheritedData(src);
        }

        private static void RemoveInheritDocOnlyMembers(ResolverContext context)
        {
            context.Members = context.Members
                .Where(pair => !pair.Value.IsInheritDocOnly)
                .ToDictionary(pair => pair.Key, pair => RemoveInheritDocOnlyMembers(pair.Value));
        }

        private static MetadataItem RemoveInheritDocOnlyMembers(MetadataItem item)
        {
            item.Items = item.Items?
                .Where(childItem => !childItem.IsInheritDocOnly)
                .Select(childItem => RemoveInheritDocOnlyMembers(childItem))
                .ToList();
            return item;
        }

        private static void RemoveInheritDocOnlyReferenences(ResolverContext context)
        {
            HashSet<string> removeReferences = new HashSet<string>(context.References
                .Where(pair => pair.Value.IsInheritDocOnly)
                .Select(pair => pair.Key));

            context.References = context.References
                .Where(pair => !pair.Value.IsInheritDocOnly)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            foreach (var item in context.Members.Values)
                RemoveInheritDocOnlyImplements(item, removeReferences);
        }

        private static void RemoveInheritDocOnlyImplements(MetadataItem item, HashSet<string> removeReferences)
        {
            item.Implements = item.Implements?
                .Where(implement =>
                {
                    return
                        !removeReferences.Contains(implement) &&
                        !removeReferences.Contains($"{implement}*");
                }).ToList();

            item.Items?
                .ForEach(child => RemoveInheritDocOnlyImplements(child, removeReferences));
        }
    }
}
