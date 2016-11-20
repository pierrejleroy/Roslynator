﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.Refactorings
{
    internal static class RemoveRedundantDelegateCreationRefactoring
    {
        public static void Analyze(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment)
        {
            ExpressionSyntax right = assignment.Right;

            if (right?.IsKind(SyntaxKind.ObjectCreationExpression) == true)
            {
                var objectCreation = (ObjectCreationExpressionSyntax)right;

                SemanticModel semanticModel = context.SemanticModel;
                CancellationToken cancellationToken = context.CancellationToken;

                if (SyntaxAnalyzer.IsEventHandlerOrGenericEventHandler(objectCreation, semanticModel, cancellationToken))
                {
                    ArgumentListSyntax argumentList = objectCreation.ArgumentList;

                    if (argumentList != null)
                    {
                        SeparatedSyntaxList<ArgumentSyntax> arguments = argumentList.Arguments;

                        if (arguments.Count == 1)
                        {
                            ArgumentSyntax argument = arguments.First();

                            ExpressionSyntax expression = argument.Expression;

                            if (expression != null)
                            {
                                IMethodSymbol methodSymbol = semanticModel.GetMethodSymbol(expression, cancellationToken);

                                if (methodSymbol != null)
                                {
                                    ExpressionSyntax left = assignment.Left;

                                    if (left?.IsMissing == false
                                        && SyntaxAnalyzer.IsEvent(left, semanticModel, cancellationToken)
                                        && !objectCreation.SpanContainsDirectives())
                                    {
                                        context.ReportDiagnostic(DiagnosticDescriptors.RemoveRedundantDelegateCreation, right.GetLocation());

                                        context.FadeOutToken(DiagnosticDescriptors.RemoveRedundantDelegateCreationFadeOut, objectCreation.NewKeyword);
                                        context.FadeOutNode(DiagnosticDescriptors.RemoveRedundantDelegateCreationFadeOut, objectCreation.Type);
                                        context.FadeOutParentheses(DiagnosticDescriptors.RemoveRedundantDelegateCreationFadeOut, objectCreation.ArgumentList);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static async Task<Document> RefactorAsync(
            Document document,
            ObjectCreationExpressionSyntax objectCreation,
            CancellationToken cancellationToken)
        {
            ExpressionSyntax expression = objectCreation
                .ArgumentList
                .Arguments
                .First()
                .Expression;

            IEnumerable<SyntaxTrivia> leadingTrivia = objectCreation
                .DescendantTrivia(TextSpan.FromBounds(objectCreation.FullSpan.Start, expression.Span.Start));

            IEnumerable<SyntaxTrivia> trailingTrivia = objectCreation
                .DescendantTrivia(TextSpan.FromBounds(expression.Span.End, objectCreation.FullSpan.End));

            ExpressionSyntax newExpression = expression
                .WithLeadingTrivia(leadingTrivia)
                .WithTrailingTrivia(trailingTrivia)
                .WithFormatterAnnotation();

            return await document.ReplaceNodeAsync(objectCreation, newExpression, cancellationToken).ConfigureAwait(false);
        }
    }
}
