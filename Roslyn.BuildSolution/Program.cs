﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslyn.BuildSolution.SyntaxCreator;

namespace Roslyn.BuildSolution
{
    class Program
    {
        private const string Template_Folder_Path = @"C:\Users\karthikn\Source\repos\Roslyn.BuildSolution\Roslyn.BuildSolution\Templates\";
        private const string New_Projects_Repository = @"C:\Users\karthikn\Desktop\TestSolutions\";


        private const string Template_Name = "WebApiTemplate";

        private const string Template_Models_Folder_name = "Models";
        private const string Template_Cotrollers_Folder_Name = "Controllers";
        private const string Template_Controller_Filename = "ValuesController.cs";

        static void Main(string[] args)
        {
            Console.WriteLine("Please provide the Entity name for which the API to be created");
            string entityName = Console.ReadLine();
            string projectName = GetValidFilename(entityName) + ".Api";

            string newWorkSpacePath = CreateNewTemplateBasedWorkspace(Template_Folder_Path, Template_Name, New_Projects_Repository, projectName);

            foreach (var file in GetAllCodeFiles(newWorkSpacePath))
            {
                var document = File.ReadAllText(file);
                //Console.WriteLine(file);
                //Console.WriteLine(document);

                // Update namespace to new service name
                ChangeNamespace(file, document, projectName);

            }
            Console.WriteLine("Project created successfully");
            Console.WriteLine("Please provide a sample json representing the entity");
            string sampleJson = Console.ReadLine();
            JObject obj = JObject.Parse(sampleJson);

            CreateModelClassForEntity(newWorkSpacePath, projectName, entityName, obj);

            CreateController(newWorkSpacePath, projectName, entityName, obj);
            Console.WriteLine("Updated Project files successfully as per the Entity provided");


            Console.ReadLine();
        }

        #region "Copy and Create Project based on template"
        private static string GetValidFilename(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        static List<string> GetAllCodeFiles(string workspacePath)
        {
            string[] files = Directory.GetFiles(workspacePath, "*.cs", SearchOption.AllDirectories);
            return files.ToList<string>();
        }

        static string CreateNewTemplateBasedWorkspace(string templatesFolderPath, string templateName, string destFolderPath, string workspaceName)
        {
            string source_dir = Path.Combine(templatesFolderPath, templateName);
            string destination_dir = Path.Combine(destFolderPath, workspaceName);

            // Create root directory
            if (!Directory.Exists(destination_dir))
                Directory.CreateDirectory(destination_dir);

            // Create subdirectory structure in destination    
            foreach (string dir in System.IO.Directory.GetDirectories(source_dir, "*", SearchOption.AllDirectories))
            {
                string tempDirectoryPathToCreate = Path.Combine(destination_dir, dir.Substring(source_dir.Length + 1));
                string directoryPathToCreate = tempDirectoryPathToCreate.Replace(templateName, workspaceName);
                if (!Directory.Exists(directoryPathToCreate))
                    Directory.CreateDirectory(directoryPathToCreate);
            }

            //copy files while changing any referenced template name
            foreach (string file_name in Directory.GetFiles(source_dir, "*", SearchOption.AllDirectories))
            {
                string tmpFilePath = Path.Combine(destination_dir, file_name.Substring(source_dir.Length + 1));
                string filePathWithNewProjectName = tmpFilePath.Replace(templateName, workspaceName);
                File.Copy(file_name, filePathWithNewProjectName, true);
            }

            return destination_dir;
        }
        #endregion

        #region "Update namespace for new project"

        static void ChangeNamespace(string filePath, string code, string @namespace)
        {
            // Parse the code into a SyntaxTree.
            var tree = CSharpSyntaxTree.ParseText(code);

            // Get the root CompilationUnitSyntax.
            var root = tree.GetRoot() as CompilationUnitSyntax;

            // Get the namespace declaration.
            var oldNamespace = root.Members.FirstOrDefault(m => m is NamespaceDeclarationSyntax) as NamespaceDeclarationSyntax;

            // To handle certain cases where no namespace is declared in a .cs file
            if (oldNamespace == null)
            {
                return;
            }
            // Get all class declarations inside the namespace.
            var classDeclarations = oldNamespace.Members.Where(m => m is ClassDeclarationSyntax);

            // Create a new namespace declaration.
            var newNamespace = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(@namespace)).NormalizeWhitespace();

            // Add the class declarations to the new namespace.
            newNamespace = newNamespace.AddMembers(classDeclarations.Cast<MemberDeclarationSyntax>().ToArray());

            // Replace the oldNamespace with the newNamespace and normailize.
            root = root.ReplaceNode(oldNamespace, newNamespace).NormalizeWhitespace();

            string newCode = root.ToFullString();

            // Write the new file.
            File.WriteAllText(filePath, root.ToFullString());

            //Output new code to the console.
            //Console.WriteLine(newCode);
            //Console.WriteLine("Namespace replaced...");
        }

        #endregion

        #region "Create Model(POCO) class"

        static void CreateModelClassForEntity(string workspacePath, string workspace, string entityName, JObject json)
        {
            string filePath = Path.Combine(workspacePath, Template_Models_Folder_name, entityName + ".cs");
            var @namespace = CreateNamespaceSkeleton(workspace, "System");
            var @class = CreateClassSkeleton(entityName, SyntaxKind.PublicKeyword);

            // loop through json properties and create prop get/setters for same
            foreach (var jProp in json.Properties())
            {
                Console.WriteLine("name: " + jProp.Name + " | type: " + jProp.Value.Type);
                var @property = CreateProperty(jProp);
                @class = @class.AddMembers(@property);
            }

            @namespace = @namespace.AddMembers(@class);
            // Normalize and get code as string.
            var code = @namespace
                .NormalizeWhitespace()
                .ToFullString();

            File.WriteAllText(filePath, code);
        }

        #endregion

        #region "Build Syntax nodes"

        


        #endregion

        #region "Build Controller from ValueController"

        static void CreateController(string workspacePath, string workspaceName, string entityName, JObject entitySource)
        {
            string controllerClassName = entityName + "sController";
            string controllerTemplatePath = Path.Combine(workspacePath, Template_Cotrollers_Folder_Name, Template_Controller_Filename);
            string controllerFilePath = Path.Combine(workspacePath, Template_Cotrollers_Folder_Name, controllerClassName + ".cs");

            string code = File.ReadAllText(controllerTemplatePath);
            var tree = CSharpSyntaxTree.ParseText(code);

            var root = tree.GetRoot() as CompilationUnitSyntax;

            var @namespace = root.Members.Single(m => m is NamespaceDeclarationSyntax) as NamespaceDeclarationSyntax;
            var oldClass = @namespace.Members.Single(m => m is ClassDeclarationSyntax) as ClassDeclarationSyntax;
            var methodDeclarations = oldClass.Members.Where(m => m is MethodDeclarationSyntax);

            //Create class declaration with all needed inheritance and attributes
            var newClass = CreateClassSkeleton(controllerClassName, SyntaxKind.PublicKeyword, "ControllerBase")
                            .WithAttributeLists(List<AttributeListSyntax>(
                                new AttributeListSyntax[] {
                                    CreateAttributeForAnyEntity("Route", "api/[controller]"),
                                    CreateAttributeForAnyEntity("ApiController")
                                }));


            string entitySourceFieldName = entityName.ToCamelCase() + "s";

            // Add constructor to class and initialize a private field to hold source data
            newClass = newClass.AddMembers(
                CreateEnumerableEntityField(entitySourceFieldName, entityName, SyntaxKind.PrivateKeyword),
                CreateConstructorWithInitialization(controllerClassName, entitySourceFieldName, entityName, entitySource));

            // Copy all API methods from template controller class
            newClass = newClass.AddMembers(methodDeclarations.Cast<MemberDeclarationSyntax>().ToArray());

            // Replace the new class created in the whole root
            root = root.ReplaceNode(oldClass, newClass).NormalizeWhitespace();

            var controllerRewriter = new BasicApiControllerWriter(entityName, entitySourceFieldName);
            var newroot = controllerRewriter.Visit(root);

            File.WriteAllText(controllerFilePath, newroot.ToFullString());
        }

        static ConstructorDeclarationSyntax CreateConstructorWithInitialization(string controllerName, string entityListLocalHolder, string entityName, JObject entitySource)
        {
            string tmpEntityFieldName = "tmp" + entityName;
            List<StatementSyntax> stmts = new List<StatementSyntax>();

            var tmpEntityFieldDeclaration = LocalDeclarationStatement(VariableDeclaration(SyntaxFactory.IdentifierName(entityName))
                                   .WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                       SyntaxFactory.VariableDeclarator(
                                           SyntaxFactory.Identifier(tmpEntityFieldName))
                                       .WithInitializer(
                                           SyntaxFactory.EqualsValueClause(
                                               SyntaxFactory.ObjectCreationExpression(
                                                   SyntaxFactory.IdentifierName(entityName))
                                               .WithArgumentList(
                                                   SyntaxFactory.ArgumentList()))))));
            stmts.Add(tmpEntityFieldDeclaration);

            foreach (var prop in entitySource.Properties())
            {
                stmts.Add(ExpressionStatement(CreateAssignmentExpression(tmpEntityFieldName, prop)));
            }

            var expressionAddEntity = ExpressionStatement(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(entityListLocalHolder),
                                        SyntaxFactory.IdentifierName("Add")))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.IdentifierName(tmpEntityFieldName))))));
            stmts.Add(expressionAddEntity);

            var constructorMember = SyntaxFactory.ConstructorDeclaration(
                                        SyntaxFactory.Identifier(controllerName))
                                    .WithModifiers(
                                        SyntaxFactory.TokenList(
                                            SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                    .WithBody(Block(stmts.ToArray()));

            return constructorMember.NormalizeWhitespace();
        }

        static AssignmentExpressionSyntax CreateAssignmentExpression(string entityVariableName, JProperty jsonProperty)
        {
            LiteralExpressionSyntax assignmentValueExpression;
            switch (jsonProperty.Value.Type)
            {
                case JTokenType.Integer:
                    assignmentValueExpression = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(jsonProperty.Value.ToObject<int>()));
                    break;
                case JTokenType.String:
                    assignmentValueExpression = LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(jsonProperty.Value.ToObject<string>()));
                    break;
                case JTokenType.Float:
                    assignmentValueExpression = LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(jsonProperty.Value.ToObject<int>()));
                    break;
                case JTokenType.Boolean:
                    if (jsonProperty.Value.ToObject<bool>())
                        assignmentValueExpression = LiteralExpression(SyntaxKind.TrueLiteralExpression);
                    else
                        assignmentValueExpression = LiteralExpression(SyntaxKind.FalseLiteralExpression);
                    break;
                default:
                    assignmentValueExpression = LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(jsonProperty.Value.ToObject<string>()));
                    break;
            }

            return SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(entityVariableName),
                            SyntaxFactory.IdentifierName(jsonProperty.Name)),
                        assignmentValueExpression);
        }

        static FieldDeclarationSyntax CreateEnumerableEntityField(string variableName, string entityName, SyntaxKind accessModifier)
        {
            var variable = VariableDeclaration(CreateGenericType("List", entityName))
             .WithVariables(
                 SingletonSeparatedList<VariableDeclaratorSyntax>(
                     VariableDeclarator(
                         Identifier(variableName))
                     .WithInitializer(
                         EqualsValueClause(
                             ObjectCreationExpression(CreateGenericType("List", entityName))
                             .WithArgumentList(
                                 ArgumentList())))));

            var field = SyntaxFactory.FieldDeclaration(variable)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

            return field.NormalizeWhitespace();
        }

        #endregion
    }
}
