/*
    Copyright (C) 2022 ElektroKill

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;

namespace dnSpy.Roslyn.Internal.SignatureHelp.CSharp {
	[Export(typeof(IPythiaSignatureHelpProviderImplementation))]
	class PythiaSignatureHelpProviderImplementation : IPythiaSignatureHelpProviderImplementation {
		public Task<(ImmutableArray<PythiaSignatureHelpItemWrapper> items, int? selectedItemIndex)>
			GetMethodGroupItemsAndSelectionAsync(ImmutableArray<IMethodSymbol> accessibleMethods, Document document,
				InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, SymbolInfo currentSymbol,
				CancellationToken cancellationToken) =>
			Task.FromResult((ImmutableArray<PythiaSignatureHelpItemWrapper>.Empty, (int?)null));
	}
}
