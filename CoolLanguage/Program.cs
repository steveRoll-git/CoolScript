using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoolScript.Lang;
using CoolScript.VM;

namespace CoolScript
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine(@"
+--+ +--+ +--+ +
|    |  | |  | |
|    |  | |  | |
|    |  | |  | |
+--+ +--+ +--+ +--+

  +--+ +--+ +--+ +++ +--+ +++
  |    |    |  |  |  |  |  |
  +--+ |    +--+  |  +--+  |
     | |    ++    |  |     |
  +--+ +--+ + +  +++ +     +
");

            CoolScriptVM vm = new CoolScriptVM();

            if (args.Length > 0)
            {
                Console.WriteLine("executing file " + args[0] + "\n");
                ExecutionStatus status = new ExecutionStatus();

                try
                {
                    Closure closure = vm.LoadChunk(System.IO.File.ReadAllText(args[0]));
                    status = vm.Run(closure, new ScriptValue[0]);
                }
                catch (SyntaxErrorException err)
                {
                    Console.WriteLine(err.Message);
                    status.success = true; // so we won't get an execution error
                }

                if (!status.success)
                {
                    Console.WriteLine("Error:\n  " + status.errorMessage);
                }
            }
            else
            {

            }
            //Console.WriteLine("Prepend '?' to evaluate expressions\n");

            Console.WriteLine();

            while (true)
            {
                Console.Write(">> ");

                string input = Console.ReadLine();

                if (!String.IsNullOrWhiteSpace(input))
                {

                    ExecutionStatus status = new ExecutionStatus();

                    try
                    {
                        /*if (input[0] == '?')
                        {
                            Parser parser = new Parser(input.Substring(1));
                            Tree expression = parser.ParseExpression();
                            status = vm.Run(expression.GetInstructions());
                            if (status.success)
                            {
                                Console.WriteLine(vm.getStackLast());
                            }
                        }
                        else
                        {*/
                        Closure closure = vm.LoadChunk(input);
                        status = vm.Run(closure, new ScriptValue[0]);
                    }
                    catch (SyntaxErrorException err)
                    {
                        Console.WriteLine(err.Message);
                        status.success = true; // so we won't get an execution error
                    }

                    if (!status.success)
                    {
                        Console.WriteLine("Error:\n  " + status.errorMessage);
                    }

                    vm.ClearStack();
                }

                Console.WriteLine();
            }
        }
    }
}
