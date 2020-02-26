using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoolLanguage
{
    class Tests
    {
        public static void Calculator()
        {
            Console.WriteLine("Enter mathematical expressions:");
            while (true)
            {
                Console.Write("> ");

                Parser parser = new Parser(Console.ReadLine());

                try
                {
                    Tree result = parser.ParseExpression();

                    VM.VMInstruction[] testThings = result.GetInstructions();

                    VM.CoolScriptVM testVM = new VM.CoolScriptVM();

                    testVM.Run(testThings);

                    Console.WriteLine("= " + testVM.getStackLast().value);
                }
                catch (SyntaxErrorException err)
                {
                    Console.WriteLine(err.Message);
                }

                Console.WriteLine();
            }
        }
    }
}
