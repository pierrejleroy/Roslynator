// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Pihrtsoft.CodeAnalysis.CSharp.Refactorings.Tests
{
    internal static class ReplaceDefaultExpressionWithNullLiteralExpressionRefactoring
    {
        private static class Foo
        {
            public static object Bar(StringBuilder stringBuilder = default(StringBuilder))
            {
                Bar(default(StringBuilder));

                int i = default(int);

                return default(object);
            }
        }
    }
}
