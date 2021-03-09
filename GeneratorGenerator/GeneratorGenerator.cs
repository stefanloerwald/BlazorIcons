using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace Excubo.Generators.Generator
{
    [Generator]
    public partial class GeneratorGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var file in context.AdditionalFiles.Where(f => f.Path.EndsWith("svg")))
            {
                context.AddSource(System.IO.Path.GetFileName(file.Path), SourceText.From($@"
namespace Raw
{{
    public static partial class FontAwesome
    {{
        public const string {System.IO.Path.GetFileNameWithoutExtension(file.Path)} = ""{System.IO.File.ReadAllText(file.Path).Replace("\"", "\\\"")}"";
    }}
}}
", Encoding.UTF8));
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // nothing to do.
        }
    }
}