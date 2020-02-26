namespace CoolLanguage
{
    class ParserDebug
    {
        static string parens(string thing, bool doParens)
        {
            return (doParens ? "(" : "") + thing + (doParens ? ")" : "");
        }

        static string printTree(Tree tree)
        {
            if (tree.type == TreeType.BinaryOperator)
            {
                BinaryOperatorTree newTree = tree as BinaryOperatorTree;
                return
                    parens(printTree(newTree.left), newTree.left is BinaryOperatorTree && (newTree.left as BinaryOperatorTree).op.getPrecedence() < newTree.op.getPrecedence())
                    + " " + newTree.op.value + " " +
                    parens(printTree(newTree.right), newTree.right is BinaryOperatorTree && (newTree.right as BinaryOperatorTree).op.getPrecedence() < newTree.op.getPrecedence());
            }
            else if (tree.type == TreeType.PrimitiveNumber)
            {
                return (tree as NumberTree).value;
            }
            return "";
        }
    }
}
