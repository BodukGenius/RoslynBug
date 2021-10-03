using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Linq;

namespace RoslynBug.Tests
{ 
    public class SimpleGenerator : ISourceGenerator
    {
        private const string _typeNameFromAnotherAssemblyAttribute = "RoslynBug.FromAnotherAssemblyAttribute";
        private const string _typeNameFromCurrentAssemblyAttribute = "Project.FromCurrentAssemblyAttribute";
        private const string _simpleClassName = "Project.SimpleClass";

        public (INamedTypeSymbol reference, AttributeData data)[] Data { get; private set; }

        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;
            var simpleClass = compilation.GetTypeByMetadataName(_simpleClassName);

            Data = (from reference in new INamedTypeSymbol[]
                    {
                        compilation.GetTypeByMetadataName(_typeNameFromCurrentAssemblyAttribute),
                        compilation.GetTypeByMetadataName(_typeNameFromAnotherAssemblyAttribute)
                    }
                    join data in simpleClass.GetAttributes() on reference.Name equals data.AttributeClass.Name
                    select (reference, data)).ToArray();

        }
    }

    public class Tests
    {
        private const string _source = @"
namespace Project
{
    using RoslynBug;
    using System;

    [FromAnotherAssembly(10)]
    [FromCurrentAssembly(10)]
    public class SimpleClass { }

    public sealed class FromCurrentAssemblyAttribute : Attribute 
    {
        public FromCurrentAssemblyAttribute(int arg) { }
    }
}
";

        static Tests()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(_source);

            var compilation = CSharpCompilation.Create("SourceGeneratorTests",
                                                       new[] { syntaxTree },
                                                       new[] {
                                                           MetadataReference.CreateFromFile(typeof(int).Assembly.Location),
                                                           MetadataReference.CreateFromFile(typeof(FromAnotherAssemblyAttribute).Assembly.Location)
                                                       },
                                                       new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new SimpleGenerator();
            CSharpGeneratorDriver.Create(generator)
                                 .RunGeneratorsAndUpdateCompilation(compilation,
                                                                    out var outputCompilation,
                                                                    out var diagnostics);

            TestData = generator.Data
                .Select(x => new TestCaseData(x.reference, x.data))
                .ToArray();
        }

        public static TestCaseData[] TestData { get; }


        [TestCaseSource(nameof(TestData))]
        public void SymbolEquals(INamedTypeSymbol reference, AttributeData data)
        {
            Assert.IsTrue(SymbolEqualityComparer.Default.Equals(reference, data.AttributeClass));
        }

        [TestCaseSource(nameof(TestData))]
        public void Arguments(INamedTypeSymbol reference, AttributeData data)
        {
            Assert.AreEqual(1, data.ConstructorArguments.Length);
        }
    }
}