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
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TwoFixedVariableDeclarations_TwoDiagnostics()
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

            fixed (byte* mp2 = b, mp3 = b)
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
                            new DiagnosticResultLocation("Test0.cs", 16, 26)
                       }
            };
            var expected2 = new DiagnosticResult
            {
                Id = "FixedAdressOfAnalyzer",
                Message = "Fixed statement could be more effective by using AdressOf on first Element",
                Severity = DiagnosticSeverity.Info,
                Locations =
                   new[] {
                            new DiagnosticResultLocation("Test0.cs", 16, 35)
                       }
            };
            VerifyCSharpDiagnostic(test, expected, expected2);
        }

        [TestMethod]
        public void FixedDeclaration_NotArrayType()
        {
            var test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
class TestStruct {public TestStruct(byte x) { b = x;}public byte b;}
        public static unsafe void Main()
        {
            var b = new TestStruct(1);
            fixed (byte* mp = &b.b)
            {
                Console.WriteLine(1);
            }
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void TwoFixedVariableDeclarations_TwoFixes()
        {
            var preFix = @"
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

            fixed (byte* mp2 = b, mp3 = b)
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
            var b = new byte[1];
            fixed (byte* mp = &b[0])
            {
                Console.WriteLine(1);
            }

            fixed (byte* mp2 = &b[0], mp3 = &b[0])
            {
                Console.WriteLine(2);
            }
        }
    }
}";

            VerifyCSharpFix(preFix, postFix);
        }
        
        [TestMethod]
        public void SingleFixedSTatement_CodeFix_ArrayType()
        {
            var preFix = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public static unsafe void Main()
        {
            var b = new byte[1];
            fixed (byte* mp = b)
            {
                Console.WriteLine(1);
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
            var b = new byte[1];
            fixed (byte* mp = &b[0])
            {
                Console.WriteLine(1);
            }
        }
    }
}";


            VerifyCSharpFix(preFix, postFix);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void SingleFixedStatement_Diagnostic_ArrayType()
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
                            new DiagnosticResultLocation("Test0.cs", 16, 26)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
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
