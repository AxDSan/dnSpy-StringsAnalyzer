// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// Copyright (C) de4dot@gmail.com

using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.DnSpy
{
    internal delegate DSEEMethodDebugInfo GetMethodDebugInfo();

    internal struct DSEELocalAndMethod
    {
        public string LocalName;
        public string LocalDisplayName;
        public string MethodName;
        public DkmClrCompilationResultFlags Flags;
        public LocalAndMethodKind Kind;
        public int Index;

        /// <summary>
        /// <see cref="CustomTypeInfo.PayloadTypeId"/> or <see cref="Guid.Empty"/>
        /// </summary>
        public Guid CustomTypeInfoId;

        /// <summary>
        /// null if none
        /// </summary>
        public ReadOnlyCollection<byte>? CustomTypeInfo;

        public DSEELocalAndMethod(string localName, string localDisplayName, string methodName, DkmClrCompilationResultFlags flags, LocalAndMethodKind kind, int index, Guid customTypeInfoId, ReadOnlyCollection<byte>? customTypeInfo)
        {
            LocalName = localName;
            LocalDisplayName = localDisplayName;
            MethodName = methodName;
            Flags = flags;
            Kind = kind;
            Index = index;
            CustomTypeInfoId = customTypeInfoId;
            CustomTypeInfo = customTypeInfo;
        }

        internal static DSEELocalAndMethod[] CreateAndFree(ArrayBuilder<LocalAndMethod> builder)
        {
            var locals = builder.Count == 0 ? Array.Empty<DSEELocalAndMethod>() : new DSEELocalAndMethod[builder.Count];
            for (int i = 0; i < locals.Length; i++)
            {
                var l = builder[i];
                var customTypeInfoId = l.GetCustomTypeInfo(out var customTypeInfo);
                locals[i] = new DSEELocalAndMethod(l.LocalName, l.LocalDisplayName, l.MethodName, l.Flags, l.Kind, l.Index, customTypeInfoId, customTypeInfo);
            }
            builder.Free();
            return locals;
        }
    }

    internal struct DSEEMethodDebugInfo
    {
        public ImmutableArray<HoistedLocalScopeRecord> HoistedLocalScopeRecords;
        public ImmutableDictionary<int, string> HoistedVarFieldTokenToNamesMap;
        // VB: must be 0 or 2 in this order: file-level, project-level
        public ImmutableArray<ImmutableArray<DSEEImportRecord>> ImportRecordGroups;
        public ImmutableArray<DSEEExternAliasRecord> ExternAliasRecords;
        public ImmutableDictionary<int, ImmutableArray<bool>>? DynamicLocalMap;
        public ImmutableDictionary<int, ImmutableArray<string?>>? TupleLocalMap;
        public string DefaultNamespaceName;
        public ImmutableArray<string> LocalVariableNames;
        public ImmutableArray<string?> ParameterNames;
        public ImmutableArray<DSEELocalConstant> LocalConstants;
        public ILSpan ReuseSpan;
        public string? ContainingDocumentName;
        public CompilerKind Compiler;
    }

    internal enum DSEEImportTargetKind
    {
        Namespace,
        Type,
        NamespaceOrType,
        Assembly,
        XmlNamespace,
        MethodToken,
        CurrentNamespace,
        DefaultNamespace,
    }

    internal struct DSEEImportRecord
    {
        public DSEEImportTargetKind TargetKind;
        public string? Alias;
        public string? TargetString;
        public string? TargetAssemblyAlias;

        public DSEEImportRecord(
            DSEEImportTargetKind targetKind,
            string? alias = null,
            string? targetString = null,
            string? targetAssemblyAlias = null)
        {
            TargetKind = targetKind;
            Alias = alias;
            TargetString = targetString;
            TargetAssemblyAlias = targetAssemblyAlias;
        }

        public static DSEEImportRecord CreateType(string serializedTypeName, string? alias = null) => new DSEEImportRecord(DSEEImportTargetKind.Type, targetString: serializedTypeName, alias: alias);
        public static DSEEImportRecord CreateNamespace(string @namespace, string? alias = null, string? targetAssemblyAlias = null) => new DSEEImportRecord(DSEEImportTargetKind.Namespace, targetString: @namespace, alias: alias, targetAssemblyAlias: targetAssemblyAlias);
        public static DSEEImportRecord CreateAssembly(string alias) => new DSEEImportRecord(DSEEImportTargetKind.Assembly, alias: alias);
    }

    internal struct DSEEExternAliasRecord
    {
        public string Alias;
        public string TargetAssembly;

        public DSEEExternAliasRecord(string alias, string targetAssembly)
        {
            Alias = alias ?? throw new ArgumentNullException(nameof(alias));
            TargetAssembly = targetAssembly ?? throw new ArgumentNullException(nameof(targetAssembly));
        }
    }

    internal struct DSEELocalConstant
    {
        //TODO:
    }

    internal static class DSEEMethodDebugInfoUtilities
    {
        public static MethodDebugInfo<TTypeSymbol, TLocalSymbol> ToMethodDebugInfo<TTypeSymbol, TLocalSymbol>(
            this DSEEMethodDebugInfo info,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider)
            where TTypeSymbol : class, ITypeSymbolInternal
            where TLocalSymbol : class, ILocalSymbolInternal
        {
            return new MethodDebugInfo<TTypeSymbol, TLocalSymbol>(
                info.HoistedLocalScopeRecords,
                info.HoistedVarFieldTokenToNamesMap,
                Convert(info.ImportRecordGroups, symbolProvider),
                Convert(info.ExternAliasRecords, symbolProvider),
                info.DynamicLocalMap,
                info.TupleLocalMap,
                info.DefaultNamespaceName,
                info.LocalVariableNames,
                info.ParameterNames,
                Convert(info.LocalConstants, symbolProvider),
                info.ReuseSpan,
                info.ContainingDocumentName,
                info.Compiler);
        }

        private static ImmutableArray<ImmutableArray<ImportRecord>> Convert<TTypeSymbol, TLocalSymbol>(
            ImmutableArray<ImmutableArray<DSEEImportRecord>> importRecordGroups,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider)
            where TTypeSymbol : class, ITypeSymbolInternal
            where TLocalSymbol : class, ILocalSymbolInternal
        {
            if (importRecordGroups.IsDefaultOrEmpty)
            {
                return ImmutableArray<ImmutableArray<ImportRecord>>.Empty;
            }

            var builder = ArrayBuilder<ImmutableArray<ImportRecord>>.GetInstance();

            var b = ArrayBuilder<ImportRecord>.GetInstance();
            foreach (var recs in importRecordGroups)
            {
                b.Clear();
                foreach (var r in recs)
                {
                    switch (r.TargetKind)
                    {
                    case DSEEImportTargetKind.Namespace:
                        Debug.Assert(r.TargetString != null);
                        b.Add(new ImportRecord(ImportTargetKind.Namespace, alias: r.Alias, targetString: r.TargetString, targetAssemblyAlias: r.TargetAssemblyAlias));
                        break;

                    case DSEEImportTargetKind.Type:
                        RoslynDebug.Assert(r.TargetString != null);
                        RoslynDebug.Assert(r.TargetAssemblyAlias == null);
                        var targetType = symbolProvider.GetTypeSymbolForSerializedType(r.TargetString);
                        b.Add(new ImportRecord(ImportTargetKind.Type, alias: r.Alias, targetType: targetType));
                        break;

                    case DSEEImportTargetKind.NamespaceOrType:
                        RoslynDebug.Assert(r.Alias != null);
                        RoslynDebug.Assert(r.TargetString != null);
                        RoslynDebug.Assert(r.TargetAssemblyAlias == null);
                        b.Add(new ImportRecord(ImportTargetKind.NamespaceOrType, alias: r.Alias, targetString: r.TargetString));
                        break;

                    case DSEEImportTargetKind.Assembly:
                        Debug.Assert(r.Alias != null);
                        Debug.Assert(r.TargetString == null);
                        Debug.Assert(r.TargetAssemblyAlias == null);
                        b.Add(new ImportRecord(ImportTargetKind.Assembly, alias: r.Alias));
                        break;

                    case DSEEImportTargetKind.XmlNamespace:
                        Debug.Assert(r.Alias != null);
                        Debug.Assert(r.TargetString != null);
                        Debug.Assert(r.TargetAssemblyAlias == null);
                        b.Add(new ImportRecord(ImportTargetKind.XmlNamespace, alias: r.Alias, targetString: r.TargetString));
                        break;

                    case DSEEImportTargetKind.MethodToken:
                        Debug.Assert(r.Alias == null);
                        Debug.Assert(r.TargetString != null);
                        Debug.Assert(r.TargetAssemblyAlias == null);
                        b.Add(new ImportRecord(ImportTargetKind.MethodToken, targetString: r.TargetString));
                        break;

                    case DSEEImportTargetKind.CurrentNamespace:
                        Debug.Assert(r.Alias == null);
                        Debug.Assert(r.TargetString == string.Empty);
                        Debug.Assert(r.TargetAssemblyAlias == null);
                        b.Add(new ImportRecord(ImportTargetKind.CurrentNamespace, targetString: string.Empty));
                        break;

                    case DSEEImportTargetKind.DefaultNamespace:
                        Debug.Assert(r.Alias == null);
                        Debug.Assert(r.TargetString != null);
                        Debug.Assert(r.TargetAssemblyAlias == null);
                        b.Add(new ImportRecord(ImportTargetKind.DefaultNamespace, targetString: r.TargetString));
                        break;

                    default:
                        Debug.Fail($"Unknown target kind: {r.TargetKind}");
                        break;
                    }
                }
                builder.Add(b.ToImmutable());
            }
            b.Free();
            return builder.ToImmutableAndFree();
        }

        private static ImmutableArray<ExternAliasRecord> Convert<TTypeSymbol, TLocalSymbol>(
            ImmutableArray<DSEEExternAliasRecord> externAliasRecords,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider)
            where TTypeSymbol : class, ITypeSymbolInternal
            where TLocalSymbol : class, ILocalSymbolInternal
        {
            if (externAliasRecords.IsDefaultOrEmpty)
            {
                return ImmutableArray<ExternAliasRecord>.Empty;
            }

            var builder = ArrayBuilder<ExternAliasRecord>.GetInstance();

            foreach (var r in externAliasRecords)
            {
                RoslynDebug.Assert(r.Alias != null);
                RoslynDebug.Assert(r.TargetAssembly != null);
                if (!AssemblyIdentity.TryParseDisplayName(r.TargetAssembly, out var targetIdentity))
                {
                    Debug.Fail($"Couldn't parse assembly name: {r.TargetAssembly}");
                    continue;
                }
                builder.Add(new ExternAliasRecord(r.Alias, targetIdentity));
            }

            return builder.ToImmutableAndFree();
        }

        private static ImmutableArray<TLocalSymbol> Convert<TTypeSymbol, TLocalSymbol>(
            ImmutableArray<DSEELocalConstant> localConstants,
            EESymbolProvider<TTypeSymbol, TLocalSymbol> symbolProvider)
            where TTypeSymbol : class, ITypeSymbolInternal
            where TLocalSymbol : class, ILocalSymbolInternal
        {
            Debug.Assert(localConstants.IsDefaultOrEmpty);
            return ImmutableArray<TLocalSymbol>.Empty;
        }
    }
}
