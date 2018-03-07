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
            Keyword,
            Separator,
            Operator,
            Comment,
            Literal,
            TODO,
            WhiteSpace,
            NewLine
        }

        #region TokenWatcher stuff
        public struct TokenWatcher
        {
            public TokenType Type;
            public List<char> CharacterList;
        }
        public static List<char> listConcat(List<char> a, List<char> b)
        {
            List<char> c = new List<char>();
            c.AddRange(a);
            c.AddRange(b);

            return c;
        }
        public static List<string> keywords = new List<string>() { "in", "var", "let", "for", "foreach", "while", "when", "break", "continue", "use", "using", "import", "data", "link", "type", "class", "struct" };

        static List<char> separatorList = new List<char>() { ',', ':', ';', '$', '{', '}', '[', ']', '(', ')', '.', '"', '\'', '`' };
        static List<char> operatorList = new List<char>() { '!', '%', '^', '&', '*', '-', '+', '+', '|', '?', '/', '<', '>', '~', '@', '#', '\\', '|', '=' };
        static List<char> posIntList = new List<char>() { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
        static List<char> numStartList = listConcat(posIntList, new List<char>() { '-' });
        static List<char> numValidList = listConcat(numStartList, new List<char>() { 'e', 'E', '.' }); 
        static List<char> identStartList = new List<char>() { '_', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        static List<char> indentValidList = listConcat(identStartList, posIntList);
        static List<char> whiteSpaceList = new List<char>() { ' ', '\t', '\n', '\r' };
        static List<char> noneList = new List<char>() { };

        public static bool charListContains(List<char> list, char value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return true;
                }
            }
            return false;
        }
        public static TokenWatcher getTokenCharType(char c)
        {
            var currentList = noneList;
            var currentTokenType = TokenType.None;

            //set token type
            if (charListContains(Token.separatorList, c))
            {
                currentTokenType = Token.TokenType.Separator;
                currentList = Token.separatorList;
            }
            else if (charListContains(Token.operatorList, c))
            {
                currentTokenType = Token.TokenType.Operator;
                currentList = Token.operatorList;
            }
            else if (charListContains(Token.whiteSpaceList, c))
            {
                currentTokenType = Token.TokenType.WhiteSpace;
                currentList = Token.whiteSpaceList;
            }
            else if (charListContains(Token.numStartList, c))
            {
                currentTokenType = Token.TokenType.Literal;
                currentList = Token.numValidList;
            }
            else if (charListContains(Token.identStartList, c))
            {
                currentTokenType = Token.TokenType.Word;
                currentList = Token.indentValidList;
            }

            var tw = new TokenWatcher();
            tw.Type = currentTokenType;
            tw.CharacterList = currentList;
            
            return tw;
        }
        #endregion TokenWatcher stuff

        public TokenType Type;
        public string Value;

        public Token()
        {
            Type = TokenType.None;
            Value = null;
        }
        public Token(TokenType theType, string theValue)
        {
            Type = theType;
            Value = theValue;
        }
        public Token(string theValue)
        {
            Type = TokenType.Word; //maybe try to figure this out based on theValue
            Value = theValue;
        }

        public override string ToString()
        {
            if (this.Type == TokenType.NewLine)
                return String.Format("[{0}: [NEWLINE]]", this.Type);
            else
                return String.Format("[{0}: {1}]", this.Type, this.Value);
        }
    }
    class Program
    {
        //UI
        static void Main(string[] args)
        {
            var testComment =
@"   //this
//comment2

//block comment{
    this is a comment 
    block
    {this is an inner block}
}
//TODO: this is something to do;
";
            var testNumbers =
@"6.022e23 -23 1.3 1ab4";
            var testString =
@"
    var s:string = $(_this is a string_);
    var s1:string = $'this is a string as well';
    var s2:string = 'this is also a string';
    var s3:string = ""this is the final string"";
}
";

            var exit = false;

            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("ralyn Builder");
                Console.WriteLine(" 1. Lexer test");
                Console.WriteLine(" 0. Exit");
                var key = Console.ReadKey();

                switch (key.KeyChar)
                {
                    case '1':
                        //Lexer test
                        Console.WriteLine();
                        try
                        {
                            var result = Lex(testString);
                            foreach (var t in result)
                                Console.WriteLine(t.ToString());
                        }catch(Exception lexEx)
                        {
                            Console.WriteLine(lexEx.Message);
                        }

                        Console.WriteLine("press any key to continue...");
                        Console.ReadKey();
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
            var rawTokens = syntaxSort(code);

            ///Review raw tokens
            /// Tokens -> Comment Literals
            /// Tokens -> hints (ex TODO, DOC, etc.)
            /// Tokens -> String Literals
            /// Word -> Literal|Keyword
            /// remove unnecessary white space
            /// 

            var tokens = new List<Token>();
            var accumulator = new System.Text.StringBuilder();
            var nestLevel = 0;
            var isBlock = false;

            //first pass: COMMENTS
            isBlock = false;
            nestLevel = 0;
            foreach (var t in rawTokens)
            {
                if(t.Value == "//"
                    || isBlock)
                {
                    isBlock = true;

                    if(t.Type == Token.TokenType.NewLine && nestLevel == 0)
                    {
                        //flush single line comment
                        tokens.Add(new Token(Token.TokenType.Comment, accumulator.ToString()));
                        accumulator.Clear();
                        isBlock = false;
                    }
                    else
                    {
                        accumulator.Append(t.Value);

                        if (t.Value == "{")
                            ++nestLevel;
                        if(t.Value == "}")
                        {
                            --nestLevel;
                            if(nestLevel == 0)
                            {
                                //flush multiline comment
                                tokens.Add(new Token(Token.TokenType.Comment, accumulator.ToString()));
                                accumulator.Clear();
                                isBlock = false;
                            }
                        }
                    }
                }
                else
                {
                    //this is not a comment... just flush the token as is
                    tokens.Add(t);
                }
            }

            //second pass: QUOTES
            //TODO:  this doesn't work yet... need to look at finding beginning and ends of strings
            rawTokens.Clear();
            rawTokens.AddRange(tokens);
            tokens.Clear();
            isBlock = false;
            nestLevel = 0;
            accumulator.Clear();
            var endDelimiter = "";
            foreach(var t in rawTokens)
            {
                ///a string begins with $", $', or $(x (where x can be any character) and ends with a matching ", ', or x)
                /// the $ can be ommitted if you are not using parenthetical notation.

                if(t.Value[0] == '$'
                    || t.Value[0] == '"'
                    || t.Value[0] == '\''
                    || isBlock)
                {
                    //this is a string
                    if (!isBlock)
                    {
                        //just discovered the string
                        isBlock = true;
                        accumulator.Append(t.Value);
                    }else
                    {
                        //inside string until we discover exit condition 
                        //TODO: FIX EXIT CONDITIONS!  STILL NOT RIGHT
                        if (accumulator.Length >= 3)
                        {
                            endDelimiter = accumulator.ToString().Substring(0, 3);
                            endDelimiter.TrimStart(new char[] { '$' });
                            if (endDelimiter[0] == '(')
                                endDelimiter = endDelimiter[1] + ")";
                            else
                                endDelimiter = endDelimiter[0].ToString();
                        }
                        
                        if (!String.IsNullOrEmpty(endDelimiter) && t.Value.IndexOf(endDelimiter) != -1)
                        {
                            accumulator.Append(t.Value.Substring(t.Value.IndexOf(endDelimiter)));
                            tokens.Add(new Token(Token.TokenType.Literal, accumulator.ToString()));
                            accumulator.Clear();
                            accumulator.Append(t.Value.Substring(t.Value.IndexOf(endDelimiter)));
                            isBlock = false;
                            endDelimiter = "";
                        }
                        else
                        {
                            accumulator.Append(t.Value);
                        }
                        
                    }
                }
                else
                {
                    //not a string
                    tokens.Add(t);
                }
            }

            return tokens;
        }
        static List<Token> syntaxSort(string code)
        {
            //setup vars
            var tokens = new List<Token>();
            string line;
            using (var _code = new System.IO.StringReader(code))
            {
                var lineNum = 0;
                var charNum = 0;

                while (_code.Peek() > -1)
                {
                    line = _code.ReadLine().Trim();
                    charNum = 0;
                    Token.TokenWatcher currentStatus = new Token.TokenWatcher();
                    currentStatus.Type = Token.TokenType.None;
                    currentStatus.CharacterList = new List<char>() { };
                    var tokenAccumulator = new StringBuilder();

                    foreach (char c in line)
                    {
                        if (currentStatus.Type == Token.TokenType.None)
                        {
                            //set token type
                            currentStatus = Token.getTokenCharType(c);
                            if (currentStatus.CharacterList.Count == 0)
                                throw new Exception($"Unexpected character '{c}' at line {lineNum} character {charNum}{System.Environment.NewLine}---->{line.Substring(0, charNum)} >>{c}<< {line.Substring(charNum + 1)}");

                            //append first character
                            tokenAccumulator.Append(c);
                        }
                        else
                        {
                            if (Token.charListContains(currentStatus.CharacterList, c))
                            {
                                //same token type -- append and move to next character
                                tokenAccumulator.Append(c);
                            }
                            else
                            {
                                //flush
                                tokens.Add(new Token(currentStatus.Type, tokenAccumulator.ToString()));
                                tokenAccumulator.Clear();

                                //get new token type
                                currentStatus = Token.getTokenCharType(c);
                                if (currentStatus.CharacterList.Count == 0)
                                    throw new Exception($"Unexpected character '{c}' at line {lineNum} character {charNum}{System.Environment.NewLine}---->{line.Substring(0, charNum)} >>{c}<< {line.Substring(charNum+1)}");

                                //append first character
                                tokenAccumulator.Append(c);
                            }
                        }
                        ++charNum;
                    }

                    //flush end of line
                    if (tokenAccumulator.Length > 0)
                    {
                        tokens.Add(new Token(currentStatus.Type, tokenAccumulator.ToString()));
                    }
                    tokens.Add(new Token(Token.TokenType.NewLine, System.Environment.NewLine));

                    //reset for next line
                    tokenAccumulator.Clear();
                    currentStatus.Type = Token.TokenType.None;

                    ++lineNum;
                }
            }

            return tokens;
        }
        #endregion Lexical Analysis

        //TODO: Everything below this point is non-exsistant or simple stubs

        #region Parsing
        static void Parse(List<Token> tokens)
        {
            //create Abstract Syntax Tree
            throw new NotImplementedException();
        }
        #endregion Parsing

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
