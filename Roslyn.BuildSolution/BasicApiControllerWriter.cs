using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.BuildSolution
{
    public class BasicApiControllerWriter: CSharpSyntaxRewriter
    {
        private readonly string entityName = string.Empty;
        private readonly string entitySource = string.Empty;

        public BasicApiControllerWriter(string entityName, string entitySource): base()
        {
            this.entityName = entityName;
            this.entitySource = entitySource;
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            var isTypeGenericAgain = node.DescendantNodes().Any(m => m is GenericNameSyntax);

            // Check if Type T under Geeric is again generic and change only the primitive string
            if (!isTypeGenericAgain)
            {
                var newGenericTypeNode = node.WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                        SyntaxFactory.IdentifierName(this.entityName))))
                                .NormalizeWhitespace();
                return base.VisitGenericName(newGenericTypeNode);
            }

            return base.VisitGenericName(node);
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            var paramWithAttriute = node.DescendantNodes().FirstOrDefault(n => n is AttributeSyntax);
            if (paramWithAttriute != null && (paramWithAttriute as AttributeSyntax).Name.ToString() == "FromBody")
            {
                var updateParamTypeNode = SyntaxFactory.Parameter(
                        SyntaxFactory.Identifier("value"))
                    .WithAttributeLists(
                        SyntaxFactory.SingletonList<AttributeListSyntax>(
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                                    SyntaxFactory.Attribute(
                                        SyntaxFactory.IdentifierName("FromBody"))))))
                    .WithType(
                        SyntaxFactory.IdentifierName(this.entityName))
                    .NormalizeWhitespace();

                return base.VisitParameter(updateParamTypeNode);
            }

            return base.VisitParameter(node);
        }

        public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
        {
            var isReturningCollection = node.DescendantNodes().Any(n => n is ArrayCreationExpressionSyntax);
            if (isReturningCollection)
            {
                var collectionNode = SyntaxFactory.ReturnStatement(
                        SyntaxFactory.IdentifierName(this.entitySource))
                        .NormalizeWhitespace();

                return base.VisitReturnStatement(collectionNode);
            }
            var isReturningSingleVal = node.DescendantNodes().Any(n => n is LiteralExpressionSyntax);
            if (isReturningSingleVal)
            {
                var singleEntityNode = SyntaxFactory.ReturnStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(this.entitySource),
                                SyntaxFactory.IdentifierName("FirstOrDefault"))))
                        .NormalizeWhitespace();

                return base.VisitReturnStatement(singleEntityNode);
            }
            return base.VisitReturnStatement(node);
        }
    }
}
