using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using FixedAdressOfAnalyzer;

namespace FixedAnalyzer.Test
{
    [TestClass]
    public class FixedAdressOfAnalyzerUnitTests : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMethod2()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public static unsafe void Main()
        {
            var b = new byte[1];
            fixed (byte* mp = &b[0])
            {
                Console.WriteLine(1);
            }

            fixed (byte* mp2 = b)
            {
                Console.WriteLine(2);
            }
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "FixedAdressOfAnalyzer",
                Message = "Fixed statement could be more effective by using AdressOf on first Element",
                Severity = DiagnosticSeverity.Info,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 16, 13)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);



            var preFix = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public static unsafe void Main()
        {
            fixed (byte* mp2 = b)
            {
                Console.WriteLine(2);
            }
        }
    }
}";
            var postFix = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public static unsafe void Main()
        {
            fixed (byte* mp2 = &b[0])
            {
                Console.WriteLine(2);
            }
        }
    }
}";

            VerifyCSharpFix(preFix, postFix);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new FixedAdressOfCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new FixedAdressOfAnalyzerImp();
        }
    }
}
