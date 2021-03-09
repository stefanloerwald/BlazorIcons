using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Excubo.Generators.Blazor
{
    [Generator]
    public partial class IconGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            var compilation = context.Compilation;
            var semanticModels = context.Compilation.SyntaxTrees.ToDictionary(st => st, st => compilation.GetSemanticModel(st));
            var usedComponents = receiver.CandidateTypeSyntaxes
                .Where(ts => ts != null)
                .Select(ts => semanticModels[ts.SyntaxTree].GetTypeInfo(ts).Type)
                .Where(t => t != null)
                .Distinct(SymbolEqualityComparer.Default)
                .ToList();
            var fontAwesome = compilation.GetTypeByMetadataName("Raw.FontAwesome")!;
            foreach (var member in fontAwesome.GetMembers().OfType<IFieldSymbol>())
            {
                var is_used = usedComponents.Any(type => type!.Name == member.Name && type.ContainingSymbol.Name == "FontAwesome");
                var markup_instruction = $"builder.AddMarkupContent(0, \"{(member.ConstantValue as string)!.Replace("\"", "\\\"")}\");";
                context.AddSource(member.Name, SourceText.From($@"
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace FontAwesome
{{
    public class {member.Name} : ComponentBase
    {{
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {{
            {(is_used ? markup_instruction : string.Empty)}
        }}
    }}
}}
", Encoding.UTF8));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        internal class SyntaxReceiver : ISyntaxReceiver
        {
            public List<TypeSyntax> CandidateTypeSyntaxes { get; } = new List<TypeSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is InvocationExpressionSyntax invocation // we look for __builder.OpenComponent<T>(int)
                    && invocation.Expression is MemberAccessExpressionSyntax maes
                    && maes.Name is GenericNameSyntax gns
                    && gns.Identifier.ValueText == "OpenComponent")
                {
                    var type_argument = gns.TypeArgumentList.Arguments.FirstOrDefault(); // this is T
                    if (type_argument != null)
                    {
                        CandidateTypeSyntaxes.Add(type_argument);
                    }
                }
            }
        }
    }
}