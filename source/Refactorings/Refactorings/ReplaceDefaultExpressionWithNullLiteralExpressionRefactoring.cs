// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp.Refactorings
{
    internal static class ReplaceDefaultExpressionWithNullLiteralExpressionRefactoring
    {
        public static async Task ComputeRefactoringAsync(RefactoringContext context, DefaultExpressionSyntax defaultExpression)
        {
            if (defaultExpression.Type?.IsMissing == false)
            {
                SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                ITypeSymbol typeSymbol = semanticModel.GetTypeInfo(defaultExpression).ConvertedType;

                if (typeSymbol?.IsReferenceType == true)
                {
                    context.RegisterRefactoring(
                        $"Replace '{defaultExpression}' with 'null'",
                        cancellationToken =>
                        {
                            return RefactorAsync(
                                context.Document,
                                defaultExpression,
                                cancellationToken);
                        });
                }
            }
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            ExpressionSyntax expression,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            SyntaxNode newRoot = root.ReplaceNode(
                expression,
                NullLiteralExpression().WithTriviaFrom(expression));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}

