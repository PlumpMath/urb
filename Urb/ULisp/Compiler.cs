using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp;
using System.Reflection;
using System.CodeDom.Compiler;
namespace Urb
{
    public partial class ULisp
    {

        #region Compiling

        /*************************************************
		 *                                               *
		 * :: COMPILER NOTE ::                           *
		 *                                               *
		 * 1) one thing is that we should compile        *
		 * each expression, one by one to be able to     *
		 * interprete, debug and get intellisense        *
		 * in the future.                                *
		 *                                               *
         *                                               *
    /***************************************************************************
     *                                                                         *
     * NOTE:                                                                   *
     *                                                                         *
     * - for new function -> compile into new partial class and load to domain.*
     * - for new variable -> instance save on interpreter environment memory.  *
     *                                                                         *
     ***************************************************************************
		 *************************************************/

        public static void Load(Assembly asm)
        {
            AppDomain.CurrentDomain.Load(asm.GetName());
        }

        public static Assembly Compile(List<Block> blocks)
        {
            var expansion = MacroExpand(blocks);
            var expressions = TokenTree2Expressions(expansion);
            var csharp_source = Expression2CSharp(expressions);
            csharp_source = MakeClass("ULispCompiled", csharp_source);
            csharp_source = _buildMain(_mainBody);
            var asm = _compile_csharp_source(csharp_source, "eval.dll", isInMemory: true);
            return asm;
        }

        public void Compile(string urb_source, string fileName, bool isExe = false, bool isDebugTransform = false, bool isDebugGrammar = false)
        {
            _resetCompiler();
            var _source = CompileIntoCSharp(urb_source, fileName, isDebugTransform, isDebugGrammar);
            _compile_csharp_source(_source, fileName, isExe);
        }

        public string CompileIntoCSharp
        (string source, string className, bool isDebugTransform = false, bool isDebugGrammar = false)
        {
            var expressions = Source2Expressions(source, isDebugTransform, isDebugGrammar);
            var csharp_source = Expression2CSharp(expressions, isDebugTransform);
            csharp_source += _buildMain(_mainBody);
            csharp_source = MakeClass(className, csharp_source);
            /////////////////////////////////////////
            ///                                   ///
            /// Print transformed C# source code. ///
            ///                                   ///
            /////////////////////////////////////////
            if (isDebugTransform) Console.WriteLine("\n\n[Transformed C#] \n");
            if (isDebugTransform) { Console.WriteLine(csharp_source); }

            return csharp_source;
        }

        public void CompileLoad(string urb_source, string output)
        {
            // this part is invoked after we defined new method/class. 
            // reload our interpreter with new compiled part.
            var _csharp = CompileIntoCSharp(urb_source, output);
            var _assembly = _compile_csharp_source(_csharp, output, true);
            AppDomain.CurrentDomain.Load(_assembly.GetName());
        }

        private static readonly List<string> _compilingOptions = new List<string>()
        {
            "executable", "library"
        };
        private static bool _compilingExe = false;
        private static Assembly _compile_csharp_source(string source, string fileName, bool isInMemory = false)
        {
            var compiler_parameter = new CompilerParameters();
            compiler_parameter.GenerateExecutable = _compilingExe;
            compiler_parameter.OutputAssembly = fileName + (_compilingExe ? ".exe" : ".dll");
            compiler_parameter.GenerateInMemory = isInMemory;
            foreach (var name in references)
                compiler_parameter.ReferencedAssemblies.Add(name + ".dll");

            var compiler = new CSharpCodeProvider();

            var result = compiler.CompileAssemblyFromSource(compiler_parameter, new string[] { source });
            if (result.Errors.Count > 0)
            {
                // Display compilation errors.
                Console.WriteLine("Errors building  into {0}:",
                    result.PathToAssembly);
                foreach (CompilerError ce in result.Errors)
                {
                    Console.WriteLine("  {0}", ce.ToString());
                    Console.WriteLine();
                }
                _warning("\nCan't compile code due to error.");
                return null;
            }
            else
            {
                Console.WriteLine("Source built into {0} successfully.",
                result.PathToAssembly);

                return result.CompiledAssembly;
            }
        }

        private void _resetCompiler()
        {
            _clearClassMetaData();
            _csharp_blocks.Clear();
            _tokenTree.Clear();
            _open = _close = 0;
            _beginLevel = 0;
            _token_array = null;
            _token_index = -1;
            _transformerIndex = -1;
            environment = new Dictionary<string, object>();
        }

        #endregion
    }
}
