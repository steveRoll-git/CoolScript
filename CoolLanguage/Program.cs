using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CoolLanguage.VM;

namespace CoolLanguage
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
                    Parser parser = new Parser(System.IO.File.ReadAllText(args[0]));
                    Chunk chunk = parser.ParseChunk();
                    status = vm.ExecuteChunk(chunk);
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
                        Parser parser = new Parser(input);
                        Chunk chunk = parser.ParseChunk();
                        status = vm.ExecuteChunk(chunk);
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
