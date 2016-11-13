// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.Refactorings
{
    internal static class DefaultExpressionRefactoring
    {
        public static async Task ComputeRefactoringsAsync(RefactoringContext context, DefaultExpressionSyntax defaultExpression)
        {
            if (context.IsRefactoringEnabled(RefactoringIdentifiers.ReplaceDefaultExpressionWithNullLiteralExpression)
                && context.Span.IsContainedInSpanOrBetweenSpans(defaultExpression)
                && context.SupportsSemanticModel)
            {
                await ReplaceDefaultExpressionWithNullLiteralExpressionRefactoring.ComputeRefactoringAsync(context, defaultExpression).ConfigureAwait(false);
            }
        }
    }
}