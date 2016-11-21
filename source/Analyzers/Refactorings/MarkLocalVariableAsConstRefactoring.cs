// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class MarkLocalVariableAsConstRefactoring
    {
        public static bool CanRefactor(
            LocalDeclarationStatementSyntax localDeclaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!localDeclaration.IsConst)
            {
                SyntaxNode parent = localDeclaration.Parent;

                if (parent?.IsKind(SyntaxKind.Block) == true)
                {
                    var block = (BlockSyntax)parent;

                    SyntaxList<StatementSyntax> statements = block.Statements;

                    if (statements.Count > 1)
                    {
                        int index = statements.IndexOf(localDeclaration);

                        if (index < statements.Count - 1)
                        {
                            VariableDeclarationSyntax variableDeclaration = localDeclaration.Declaration;

                            if (variableDeclaration?.IsMissing == false)
                            {
                                SeparatedSyntaxList<VariableDeclaratorSyntax> variables = variableDeclaration.Variables;

                                if (variables.Any())
                                {
                                    ITypeSymbol typeSymbol = semanticModel.GetTypeSymbol(variableDeclaration.Type, cancellationToken);

                                    if (typeSymbol?.SupportsConstantValue() == true
                                        && variables.All(variable => semanticModel.HasConstantValue(variable, cancellationToken)))
                                    {
                                        DataFlowAnalysis analysis = semanticModel.AnalyzeDataFlow(statements[index + 1], statements.Last());

                                        if (analysis.Succeeded)
                                        {
                                            ImmutableArray<ISymbol> writtenInside = analysis.WrittenInside;
                                            ImmutableArray<ISymbol> readInside = analysis.ReadInside;

                                            return variables.All(f =>
                                            {
                                                ISymbol symbol = semanticModel.GetDeclaredSymbol(f, cancellationToken);

                                                return symbol?.IsErrorType() == false
                                                    && !writtenInside.Contains(symbol)
                                                    && readInside.Contains(symbol);
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            LocalDeclarationStatementSyntax localDeclaration,
            CancellationToken cancellationToken)
        {
            VariableDeclarationSyntax variableDeclaration = localDeclaration.Declaration;
            TypeSyntax type = variableDeclaration.Type;

            LocalDeclarationStatementSyntax newNode = localDeclaration;

            if (type.IsVar)
            {
                SemanticModel semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                ITypeSymbol typeSymbol = semanticModel.GetTypeSymbol(type);

                TypeSyntax newType = Type(typeSymbol)
                    .WithSimplifierAnnotation()
                    .WithTriviaFrom(type);

                newNode = newNode.ReplaceNode(type, newType);
            }

            Debug.Assert(!newNode.Modifiers.Any(), newNode.Modifiers.ToString());

            if (newNode.Modifiers.Any())
            {
                newNode = newNode.AddModifiers(ConstToken());
            }
            else
            {
                newNode = newNode
                    .WithoutLeadingTrivia()
                    .WithModifiers(TokenList(ConstToken().WithLeadingTrivia(newNode.GetLeadingTrivia())));
            }

            return await document.ReplaceNodeAsync(localDeclaration, newNode).ConfigureAwait(false);
        }
    }
}
