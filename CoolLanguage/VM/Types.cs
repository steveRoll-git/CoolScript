using System.Collections.Generic;

namespace CoolLanguage.VM
{
    enum dataType
    {
        Null,
        Boolean,
        Number,
        String,
        Table,
        Function,
        CFunction
    }

    struct ScriptValue
    {
        static string[] typeNames =
        {
            "null",
            "boolean",
            "number",
            "string",
            "table",
            "function",
            "cfunction"
        };

        public dataType type;

        public dynamic value;

        public ScriptValue(dataType t, object v = null)
        {
            type = t;
            value = v;
        }

        public override string ToString()
        {
            if (type == dataType.Table)
            {
                return "table: " + value;
            }
            else if (type == dataType.Function)
            {
                return "function: " + value;
            }
            else if (type == dataType.CFunction)
            {
                return "cfunction: " + value;
            }
            else if (type == dataType.Boolean)
            {
                return value ? "true" : "false";
            }
            else if (type == dataType.Null)
            {
                return "null";
            }

            return value.ToString();
        }

        public string TypeName => typeNames[(int)type];

        public static ScriptValue Null = new ScriptValue(dataType.Null);
    }

    class Table
    {
        public Dictionary<ScriptValue, ScriptValue> dictionary = new Dictionary<ScriptValue, ScriptValue>();
    }

    class FunctionPrototype
    {
        public int stackSize;

        public VMInstruction[] instructions;

        public FunctionPrototype(VMInstruction[] insts, int _stackSize)
        {
            instructions = insts;

            stackSize = _stackSize;
        }
    }

    class Closure
    {
        public FunctionPrototype prototype;

        public Closure(FunctionPrototype prototype)
        {
            this.prototype = prototype;
        }
    }
}