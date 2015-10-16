﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Immutable;

    public interface IHostService
    {
        MarkupResult Markup(string markdown, FileAndType ft);
        ImmutableHashSet<string> SourceFiles { get; }
        ImmutableHashSet<string> GetAllUids();
        ImmutableArray<FileModel> GetModels(DocumentType? type = null);
        ImmutableArray<FileModel> LookupByUid(string uid);
    }
}
