using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Roslyn.BuildSolution
{
    public static class SyntaxCreator
    {
        public static NamespaceDeclarationSyntax CreateNamespaceSkeleton(string namespaceName, params string[] namespaceToImport)
        {
            // Create a namespace: (namespace CodeGenerationSample)
            var @namespace = NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName)).NormalizeWhitespace();

            // Add System using statement: (using System)
            foreach (var nmspace in namespaceToImport)
                @namespace = @namespace.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nmspace)));

            return @namespace;
        }

        public static ClassDeclarationSyntax CreateClassSkeleton(string className, SyntaxKind accessModifier, params string[] baseTypes)
        {
            //  Create a class: (class Order)
            var classDeclaration = SyntaxFactory.ClassDeclaration(className);

            // Add the public modifier: (public class Order)
            classDeclaration = classDeclaration.AddModifiers(SyntaxFactory.Token(accessModifier));

            // Inherit any base classes if needed
            if (baseTypes.Length > 0)
            {
                List<BaseTypeSyntax> baseTypesToInherit = new List<BaseTypeSyntax>();
                foreach (var baseType in baseTypes)
                {
                    baseTypesToInherit.Add(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(baseType)));
                }
                classDeclaration = classDeclaration.AddBaseListTypes(baseTypesToInherit.ToArray());
            }
            return classDeclaration;
        }

        public static AttributeListSyntax CreateAttributeForAnyEntity(string attributeName, params string[] arguments)
        {
            var attr = Attribute(IdentifierName(attributeName));

            if (arguments.Length > 0)
            {
                foreach (var arg in arguments)
                {
                    attr = attr.AddArgumentListArguments(AttributeArgument(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(arg))));
                }
            }
            var attrList = AttributeList(SingletonSeparatedList<AttributeSyntax>(attr));
            return attrList;
        }

        public static PropertyDeclarationSyntax CreateProperty(JProperty jsonProperty)
        {
            string type = string.Empty;
            string name = jsonProperty.Name;
            switch (jsonProperty.Value.Type)
            {
                case JTokenType.Integer:
                    type = "int"; break;
                case JTokenType.String:
                    type = "string"; break;
                case JTokenType.Float:
                    type = "double"; break;
                case JTokenType.Boolean:
                    type = "bool"; break;
                default:
                    type = "Object"; break;
            }
            return CreateProperty(type, name, SyntaxKind.PublicKeyword);
        }

        public static PropertyDeclarationSyntax CreateProperty(string type, string name, SyntaxKind accessModifier)
        {
            // Create a Property: (public int Quantity { get; set; })
            var propertyDeclaration = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(type), name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));


            return propertyDeclaration;
        }

        public static GenericNameSyntax CreateGenericType(string genericName, string typeName)
        {
            return GenericName(
                    Identifier(genericName))
                .WithTypeArgumentList(
                    TypeArgumentList(
                        SingletonSeparatedList<TypeSyntax>(
                            IdentifierName(typeName))));
        }
    }
}
