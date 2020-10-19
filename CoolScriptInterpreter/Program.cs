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
                    vm.LoadChunk(System.IO.File.ReadAllText(args[0]));
                    status = vm.Call(0);
                }
                catch (CompilerException err)
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
                        vm.LoadChunk(input);
                        status = vm.Call(0);
                    }
                    catch (CompilerException err)
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
