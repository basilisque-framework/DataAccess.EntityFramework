/*
   Copyright 2025 Alexander Stärk

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;

namespace Basilisque.DataAccess.EntityFramework.CodeAnalysis.Generators.DesignTimeServicesAttributeGenerator;

internal static class DesignTimeServicesAttributeGeneratorSelectors
{
    internal static IncrementalValuesProvider<string?> GetExistingAttributes(IncrementalGeneratorInitializationContext context)
    {
        var existingAttributes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (s, _) => isRelevantAttribute(s),
            transform: getAlreadyExistingAttributes
            );

        return existingAttributes;
    }

    private static bool isRelevantAttribute(SyntaxNode node)
    {
        if (node is not AttributeSyntax attr)
            return false;

        var attributeName = attr.Name.ToString();

        if (!attributeName.Contains("DesignTimeServicesReference"))
            return false;

        if (attr.ArgumentList?.Arguments is not { Count: 1 })
            return false;

        var argument = attr.ArgumentList.Arguments[0].GetText().ToString();
        if (argument != "\"Basilisque.DataAccess.EntityFramework.Relational.Design.RelationalDesignTimeServices, Basilisque.DataAccess.EntityFramework.Relational\"")
            return false;

        return true;
    }

    private static string? getAlreadyExistingAttributes(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var attributeSyntax = (AttributeSyntax)context.Node;

        var typeInfo = context.SemanticModel.GetTypeInfo(attributeSyntax);
        var attrType = typeInfo.Type as INamedTypeSymbol;

        return attrType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}
