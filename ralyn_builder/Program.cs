using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ralyn_builder
{
    class Token
    {
        public enum TokenType
        {
            None,
            Word,
            Identifier,
            Identifier_Var,
            Identifier_Fn,
            Keyword,
            Separator,
            Group_Delimiter,
            Operator,
            Comment,
            Literal,
            Literal_String,
            Literal_Num,
            META_CODE,
            SECTION,
            TODO,
            DOC,
            NOTE,
            DEBUG,
            WhiteSpace
        }
        public static List<string> keywords = new List<string>() { "in", "var", "let", "for", "foreach", "while", "when", "break", "continue", "use", "using", "import", "data", "link", "type", "class", "struct" };
        
        public TokenType Type;
        public string Value;
        public int Line;
        public int Char;
        public int OrderOfOperations;

        private void getTokenType(TokenType t)
        {
            this.Type = t;

            if (this.Type == TokenType.Word)
            {
                if (keywords.Contains(this.Value))
                {
                    this.Type = TokenType.Keyword;
                }
                else
                {
                    this.Type = TokenType.Identifier;
                }
            }
        }
        private void getOrderOfOperations()
        {
            //default
            this.OrderOfOperations = -1;

            if (String.IsNullOrEmpty(this.Value)) return;

            if(this.Type == TokenType.Operator)
            {
                //https://en.wikipedia.org/wiki/Order_of_operations

                //assignment
                if (this.Value == "="
                    || this.Value == "+="
                    || this.Value == "-="
                    || this.Value == "*="
                    || this.Value == "/="
                    || this.Value == "%="
                    || this.Value == "&="
                    || this.Value == "|="
                    || this.Value == "^="
                    || this.Value == "<<="
                    || this.Value == ">>=") this.OrderOfOperations = 14;

                //?: temary conditional would be 13... probably not supported in ralyn

                //Logical
                if (this.Value == "||") this.OrderOfOperations = 12;
                if (this.Value == "&&") this.OrderOfOperations = 11;

                //Bitwise Joins
                if (this.Value == "|") this.OrderOfOperations = 10; //OR
                if (this.Value == "^") this.OrderOfOperations = 9; //XOR
                if (this.Value == "&") this.OrderOfOperations = 8; //AND

                //Comparison
                if (this.Value == "=="
                    || this.Value == "!=") this.OrderOfOperations = 7;
                if (this.Value == "<"
                    || this.Value == "<="
                    || this.Value == ">"
                    || this.Value == ">=") this.OrderOfOperations = 6;

                //Bitwise Shift
                if (this.Value == "<<"
                    || this.Value == ">>") this.OrderOfOperations = 5;

                //Addition/Subtraction
                if (this.Value == "+"
                    || this.Value == "-") this.OrderOfOperations = 4;

                //Multiplication/Division/Modulo
                if (this.Value == "*"
                    || this.Value == "/"
                    || this.Value == "%") this.OrderOfOperations = 3;

                //unary operators
                if (this.Value == "!"
                    || this.Value == "++"
                    || this.Value == "--") this.OrderOfOperations = 2;


                //fn call, scope, array/member access would be 1

                if(this.OrderOfOperations == -1)
                {
                    //didn't find this operator
                    throw new Exception($"Unknown operator {this.Value} at line {this.Line}");
                }
            }
            else
            {
                //don't worry about this for now
            }
        }
        public Token()
        {
            Type = TokenType.None;
            Value = null;
            Line = -1;
            Char = -1;
            OrderOfOperations = -1;
        }
        public Token(TokenType theType, string theValue, int theLine, int theChar)
        {
            getTokenType(theType);
            getOrderOfOperations();
            Value = theValue;
            Line = theLine;
            Char = theChar;
        }

        public override string ToString()
        {
            if (this.Value == Environment.NewLine)
                return $"[{this.Type}: [NEWLINE]]";
            else
                return $"[{this.Type}: {this.Value}]";
        }
    }
    class Tree
    {
        public class Node
        {
            private Token m_token;
            private Node m_parent;
            private List<Node> m_children;

            private Node(Token t)
            {
                m_token = t;
                m_parent = null;
            }
            public static Node CreateRootNode()
            {
                Node n = new Node(new Token(Token.TokenType.None, "ROOT",-1,-1));
                return n;
            }
            public Node Add(Token t)
            {
                Node n = new Node(t);
                n.m_parent = this;
                m_children.Add(n);

                return n;
            }

            public Node Parent
            {
                get
                {
                    return m_parent;
                }
            }
            public Node Child(int i)
            {
                return m_children[i];
            }
            public Node Sibling(int i)
            {
                return this.m_parent.m_children[i];
            }

        }

        public Node root;

        public Tree()
        {
            root = Tree.Node.CreateRootNode();
        }
        public override string ToString()
        {
            throw new NotImplementedException();
        }

    }
    class Program
    {
        //UI
        static void Main(string[] args)
        {
            var exit = false;
            var fileName = @"..\..\TestCode.ralyn";

            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("ralyn Builder");
                Console.WriteLine($" 1. Select File (current file->{fileName})");
                Console.WriteLine(" 2. Lex code");
                //Console.WriteLine(" 3. Parser code");
                //Console.WriteLine(" 4. Create Action Tree from code");
                //Console.WriteLine(" 5. Transpile code");
                //Console.WriteLine(" 6. Compile code");
                //Console.WriteLine(" 7. Execute code");
                Console.WriteLine(" 0. Exit");
                var key = Console.ReadKey();

                switch (key.KeyChar)
                {
                    case '1':
                        Console.WriteLine();
                        Console.WriteLine("Enter new file path");
                        var tempFile = Console.ReadLine();
                        var fi = new System.IO.FileInfo(tempFile);
                        if (fi.Exists)
                        {
                            fileName = tempFile;
                        }else
                        {
                            Console.WriteLine($"Could not locate {tempFile}, reverting to {fileName}");
                            Console.WriteLine("press any key to continue...");
                            Console.ReadKey();
                        }
                        break;
                    case '2':
                        //Lex
                        Console.WriteLine();
                        try
                        {
                            var codeFromFile = System.IO.File.ReadAllText(fileName);
                            var result = Lex(codeFromFile);
                            foreach (var t in result)
                                Console.WriteLine(t.ToString());
                        }catch(Exception lexEx)
                        {
                            Console.WriteLine(lexEx.Message);
                        }

                        Console.WriteLine("press any key to continue...");
                        Console.ReadKey();
                        break;
                    case '4':
                        //Parse
                        Console.WriteLine();
                        try
                        {
                            var codeFromFile = System.IO.File.ReadAllText(@"..\..\TestCode.ralyn");
                            var result = Lex(codeFromFile);
                            Parse(result);
                        }
                        catch (Exception parseEx)
                        {
                            Console.WriteLine(parseEx.Message);
                        }
                        break;
                    case '0':
                        exit = true;
                        break;
                    default:
                        break;
                }
            }
        }

        //Builder
        #region Lexical Analysis        
        static List<Token> Lex(string code)
        {
            //setup vars
            var tokens = new List<Token>();
            string line;
            using (var _code = new System.IO.StringReader(code))
            {
                #region vars
                var lineNum = 0;
                var charNum = 0;                
                Token.TokenType currentType = new Token.TokenType();
                currentType = Token.TokenType.None;
                var tokenAccumulator = new StringBuilder();
                var nestLevel = 0;
                var stringBeginSequence = "";
                var stringEndSequence = "";
                var blockAllowed = false;
                var bockTrimChar = 0;
                var recurse = false;
                #endregion vars

                while (_code.Peek() > -1)
                {
                    line = _code.ReadLine().Trim();
                    if (lineNum > 0 && currentType == Token.TokenType.None)
                    {
                        tokens.Add(new Token(Token.TokenType.WhiteSpace, Environment.NewLine, lineNum + 1, 0));
                    }
                    ++lineNum;
                    charNum = 0;

                    foreach (char c in line)
                    {
                        ++charNum;
                        var cLength = tokenAccumulator.Length;

                        #region meta code
                        ///with the exception of SECTION these currently act as comments
                        ///eventually I'd like to add some additional functionality... probably
                        ///on the developer side of things.
                        
                        if ((tokenAccumulator.ToString()).ToUpper() == "TODO:")
                        {
                            blockAllowed = true;
                            recurse = false;
                            bockTrimChar = 5;
                            currentType = Token.TokenType.TODO;
                        }
                        if ((tokenAccumulator.ToString()).ToUpper() == "DEBUG:")
                        {
                            blockAllowed = true;
                            recurse = false;
                            bockTrimChar = 6;
                            currentType = Token.TokenType.DEBUG;
                        }
                        if ((tokenAccumulator.ToString()).ToUpper() == "DOC:")
                        {
                            blockAllowed = true;
                            recurse = false;
                            bockTrimChar = 4;
                            currentType = Token.TokenType.DOC;
                        }
                        if ((tokenAccumulator.ToString()).ToUpper() == "NOTE:")
                        {
                            blockAllowed = true;
                            recurse = false;
                            bockTrimChar = 5;
                            currentType = Token.TokenType.NOTE;
                        }
                        if ((tokenAccumulator.ToString()).ToUpper() == "SECTION:")
                        {
                            blockAllowed = true;
                            recurse = true;
                            bockTrimChar = 8;
                            currentType = Token.TokenType.SECTION;
                        }
                        if (tokenAccumulator.ToString() == "//")
                        {
                            blockAllowed = true;
                            recurse = false;
                            bockTrimChar = 2;
                            currentType = Token.TokenType.Comment;
                        }
                        #endregion meta code
                        #region string lexing
                        ///strings can look like the following:
                        ///1. "surrounded by double quotes"
                        ///2. 'surrounded by single quotes'
                        ///3. $(/surrounded by user defined character/)
                        ///note that you can do stuff like this: $(""Hello," she said.")
                        
                        if (currentType == Token.TokenType.Literal_String
                            || tokenAccumulator.ToString() == "'"
                            || tokenAccumulator.ToString() == "\""
                            || (cLength == 1
                                && tokenAccumulator[0] == '$'
                                && c == '(')
                                )
                        {
                            //this is a string
                            currentType = Token.TokenType.Literal_String;

                            tokenAccumulator.Append(c);

                            if (tokenAccumulator[0] == '"') stringBeginSequence = stringEndSequence = "\"";
                            if (tokenAccumulator[0] == '\'') stringBeginSequence = stringEndSequence = "'";
                            if (tokenAccumulator.Length > 2 && tokenAccumulator[0] == '$' && tokenAccumulator[1] == '(')
                            {
                                stringBeginSequence = "$(" + tokenAccumulator[2];
                                stringEndSequence = tokenAccumulator[2] + ")";
                            }

                            if (stringEndSequence.Length > 0
                                && tokenAccumulator.Length >= stringEndSequence.Length * 2
                                && tokenAccumulator.ToString().Substring(tokenAccumulator.Length - stringEndSequence.Length) == stringEndSequence)
                            {
                                //found end of string -- flush
                                tokenAccumulator.Remove(0, stringBeginSequence.Length);
                                tokenAccumulator.Replace(stringEndSequence, "");
                                tokens.Add(new Token(currentType, tokenAccumulator.ToString(), lineNum, charNum));
                                tokenAccumulator.Clear();
                                currentType = Token.TokenType.None;
                                stringEndSequence = "";
                                stringBeginSequence = "";
                                continue;
                            }
                            //if (charNum == line.Length) tokenAccumulator.Append(Environment.NewLine);

                            continue;
                        }
                        #endregion string lexing
                        #region number lexing
                        ///all numbers start with 0-9 pr -
                        ///maybe use prefixes: 0x hex, 0o octal, 0b binary

                        if (currentType == Token.TokenType.Literal_Num
                            || cLength > 1
                            && (
                                (
                                    tokenAccumulator[0] > 47
                                    && tokenAccumulator[0] < 58
                                )
                                || tokenAccumulator[0] == '-'
                               )
                            )
                        {
                            //this looks like a number
                            currentType = Token.TokenType.Literal_Num;

                            //if we are going implement hex, octal, binary etc. add those indicators in here
                            if (charNum != line.Length
                                && ( c == '0'
                                || c == '1'
                                || c == '2'
                                || c == '3'
                                || c == '4'
                                || c == '5'
                                || c == '6'
                                || c == '7'
                                || c == '8'
                                || c == '9'
                                || c == '.'
                                || c == 'e'
                                || c == 'E'
                                || c == '-')
                                )
                            {
                                tokenAccumulator.Append(c);
                                //make sure this is a VALID number
                                var isValid = true;
                                var numValidator = tokenAccumulator.ToString().Split(new char[] { 'e', 'E' }, StringSplitOptions.None);

                                if (numValidator.Length > 2) isValid = false; //only one 'e'
                                foreach(var s in numValidator)
                                {
                                    if(s.Split(new char[] { '.' }, StringSplitOptions.None).Length > 2) isValid = false; //only one '.' per section                                    
                                    if (s.LastIndexOf('-') > 0) isValid = false; //negation must be first char in section if exists
                                }

                                if (!isValid)
                                {
                                    throw new Exception($"Invalid number sequence {tokenAccumulator.ToString()} detected at line {lineNum} character {charNum}");
                                }

                            }else
                            {
                                //flush
                                if (charNum == line.Length) tokenAccumulator.Append(c);
                                tokens.Add(new Token(currentType, tokenAccumulator.ToString(), lineNum, charNum));
                                tokenAccumulator.Clear();
                                currentType = Token.TokenType.None;
                                blockAllowed = false;
                                recurse = false;
                            }
                            
                        }
                        #endregion number lexing
                        #region block
                        if (blockAllowed)
                        {
                            tokenAccumulator.Append(c);
                            if (c == '{')
                                ++nestLevel;
                            if (c == '}')
                            {
                                --nestLevel;
                                if (nestLevel == 0)
                                {
                                    //flush block 
                                    if (tokenAccumulator.Length > 1)
                                    {
                                        tokenAccumulator.Remove(0, bockTrimChar);
                                        tokens.Add(new Token(currentType, tokenAccumulator.ToString(), lineNum, charNum));
                                        tokenAccumulator.Clear();
                                        currentType = Token.TokenType.None;
                                        blockAllowed = false;
                                    }
                                    continue;
                                }
                            }

                            if (charNum == line.Length)
                            {
                                if (nestLevel == 0)
                                {
                                    //flush single line
                                    if (tokenAccumulator.Length > 1)
                                    {
                                        tokenAccumulator.Remove(0, bockTrimChar);
                                        tokens.Add(new Token(currentType, tokenAccumulator.ToString(), lineNum, charNum));
                                        tokenAccumulator.Clear();
                                        currentType = Token.TokenType.None;
                                        blockAllowed = false;
                                    }
                                    //tokens.Add(new Token(Token.TokenType.WhiteSpace, Environment.NewLine, lineNum, line.Length));
                                    break;
                                }
                                else
                                {
                                    tokenAccumulator.Append(Environment.NewLine);
                                    break;
                                }

                            }
                        }
                        #endregion block

                        //we don't know what this is yet
                        if (currentType == Token.TokenType.None)
                        {
                            if (c == '\n'
                                || c == '\r'
                                || c == '\t'
                                || c == ' '
                                || c == ';'
                                || c == '('
                                || c == ')'
                                || charNum == line.Length)
                            {
                                //flush
                                if(String.IsNullOrWhiteSpace(tokenAccumulator.ToString()))
                                    tokens.Add(new Token(Token.TokenType.WhiteSpace, tokenAccumulator.ToString(), lineNum, charNum));
                                else
                                    tokens.Add(new Token(Token.TokenType.Identifier, tokenAccumulator.ToString(), lineNum, charNum));
                                tokenAccumulator.Clear();
                                currentType = Token.TokenType.None;
                                blockAllowed = false;
                                recurse = false;
                            }
                            else
                            {
                                tokenAccumulator.Append(c);
                            }
                        }


                    }
                }
            }
            return tokens;
        }
        #endregion Lexical Analysis

        //TODO: Everything below this point is work in progress

        #region Parsing
        static void Parse(List<Token> tokens)
        {
            //create Abstract Syntax Tree
            var ast = new Tree();
            var currentNode = ast.root;
            var tokenStack = new Stack<Token>();//make a guess at the capacity?  some fraction of tokens.Count perhaps?
            var parenStack = new Stack<string>();

            foreach (var t in tokens)
            {
                //ignore the following token types
                if (t.Type == Token.TokenType.Comment
                    || t.Type == Token.TokenType.DEBUG
                    || t.Type == Token.TokenType.DOC
                    || t.Type == Token.TokenType.TODO
                    || t.Type == Token.TokenType.NOTE
                    || t.Type == Token.TokenType.WhiteSpace
                    ) continue;

                if(t.Type == Token.TokenType.Identifier_Fn)
                {                    
                    currentNode = currentNode.Add(t);
                    continue;
                }

                if(t.Type == Token.TokenType.Group_Delimiter)
                {
                    if ( parenStack.Count > 0 
                        && (t.Value == ")" && parenStack.Peek() == "(")
                        || (t.Value == "}" && parenStack.Peek() == "{")
                        || (t.Value == "]" && parenStack.Peek() == "["))
                    {
                        parenStack.Pop();
                    }else
                    {
                        parenStack.Push(t.Value);
                    }
                }

                if(t.Type == Token.TokenType.Operator)
                {
                    //orders of operation
                }


            }

        }
        #endregion Parsing

        //TODO: Everything below this point is non-exsistant or simple stubs

        #region Abstract Syntax Tree -> Action Tree
        static void CreateActionTree()
        {
            //create Abstract Syntax Tree
            throw new NotImplementedException();
        }
        #endregion Abstract Syntax Tree -> Action Tree

        #region Transpiling
        static string Transpile()
        {
            //output machine code in another language... JavaScript?
            var jsCode = new System.Text.StringBuilder();

            throw new NotImplementedException();

            return jsCode.ToString();
        }
        #endregion Transpiling

        #region Compiling
        static void Compile()
        {
            //output machine code
            var machineCode = new System.Text.StringBuilder();

            throw new NotImplementedException();            
        }
        #endregion Compiling

        #region Interpreting
        static void Interpret()
        {
            //run program on the fly
            throw new NotImplementedException();
        }
        #endregion Interpreting
    }
}
