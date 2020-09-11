using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoolScript.VM
{
    static class Debug
    {
        public static void PrintInstructions(VMInstruction[] instructions)
        {
            foreach (VMInstruction instruction in instructions)
            {
                Console.WriteLine(instruction.type + "\t" + instruction.data);
            }
        }
    }
}