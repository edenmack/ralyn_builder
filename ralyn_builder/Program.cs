﻿using System;
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
            Literal_String,
            Literal_Num,
            TODO,
            DOC,
            NOTE,
            DEBUG,
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

        static List<char> separatorList = new List<char>() { ',', ':', ';', '{', '}', '[', ']', '(', ')', '.', '`' };
        static List<char> stringStartList = new List<char>() { '$', '\'', '"', '(' };
        static List<char> operatorList = new List<char>() { '!', '%', '^', '&', '*', '-', '+', '|', '?', '/', '<', '>', '~', '@', '#', '\\', '|', '=' };
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
            if (charListContains(Token.numStartList, c))
            {
                currentTokenType = TokenType.Literal_Num;
                currentList = Token.numValidList;
            }
            else if (charListContains(Token.separatorList, c))
            {
                currentTokenType = TokenType.Separator;
                currentList = Token.separatorList;
            }
            else if (charListContains(Token.stringStartList, c))
            {
                currentTokenType = TokenType.Literal_String;
                currentList = Token.stringStartList;
            }
            else if (charListContains(Token.operatorList, c))
            {
                currentTokenType = TokenType.Operator;
                currentList = Token.operatorList;
            }
            else if (charListContains(Token.whiteSpaceList, c))
            {
                currentTokenType = TokenType.WhiteSpace;
                currentList = Token.whiteSpaceList;
            }
            else if (charListContains(Token.identStartList, c))
            {
                currentTokenType = TokenType.Word;
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
        public int Line;
        public int Char;

        public Token()
        {
            Type = TokenType.None;
            Value = null;
            Line = -1;
            Char = -1;
        }
        public Token(TokenType theType, string theValue)
        {
            Type = theType;
            Value = theValue;
            Line = -1;
            Char = -1;
        }
        public Token(TokenType theType, string theValue, int theLine, int theChar)
        {
            Type = theType;
            Value = theValue;
            Line = theLine;
            Char = theChar;
        }
        public Token(string theValue)
        {
            Type = TokenType.Word; //maybe try to figure this out based on theValue
            Value = theValue;
            Line = -1;
            Char = -1;
        }

        public override string ToString()
        {
            if (this.Type == TokenType.NewLine)
                return $"[({this.Line},{this.Char}){this.Type}: [NEWLINE]]";
            else
                return $"[({this.Line},{this.Char}){this.Type}: {this.Value}]";
        }
    }
    class Program
    {
        //UI
        static void Main(string[] args)
        {
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
                            var codeFromFile = System.IO.File.ReadAllText(@"..\..\TestCode.ralyn");
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
                Token.TokenWatcher currentStatus = new Token.TokenWatcher();
                currentStatus.Type = Token.TokenType.None;
                currentStatus.CharacterList = new List<char>() { };
                var tokenAccumulator = new StringBuilder();
                var nestLevel = 0;
                var stringBeginSequence = "";
                var stringEndSequence = "";
                #endregion vars

                while (_code.Peek() > -1)
                {
                    line = _code.ReadLine().Trim();
                    if(lineNum > 0 && currentStatus.Type == Token.TokenType.None)
                    {
                        tokens.Add(new Token(Token.TokenType.NewLine, Environment.NewLine, lineNum + 1, 0));
                    }
                    ++lineNum;
                    charNum = 0;
                    

                    foreach (char c in line)
                    {
                        ++charNum;

                        #region meta code lexing
                        //TODO, DOC, NOTE, DEBUG
                        if (currentStatus.Type == Token.TokenType.TODO
                            || currentStatus.Type == Token.TokenType.DOC
                            || currentStatus.Type == Token.TokenType.NOTE
                            || currentStatus.Type == Token.TokenType.DEBUG
                            || tokenAccumulator.ToString().IndexOf("DOC") == 0
                            || tokenAccumulator.ToString().IndexOf("NOTE") == 0
                            || tokenAccumulator.ToString().IndexOf("DEBUG") == 0
                            || tokenAccumulator.ToString().IndexOf("TODO") == 0)
                        {
                            if (tokenAccumulator.ToString().IndexOf("DOC") == 0) currentStatus.Type = Token.TokenType.DOC;
                            if (tokenAccumulator.ToString().IndexOf("NOTE") == 0) currentStatus.Type = Token.TokenType.NOTE;
                            if (tokenAccumulator.ToString().IndexOf("DEBUG") == 0) currentStatus.Type = Token.TokenType.DEBUG;
                            if (tokenAccumulator.ToString().IndexOf("TODO") == 0) currentStatus.Type = Token.TokenType.TODO;

                            tokenAccumulator.Append(c);

                            if (charNum == line.Length)
                            {
                                //flush meta
                                tokenAccumulator.Remove(0, currentStatus.Type.ToString().Length + 1);
                                tokens.Add(new Token(currentStatus.Type, tokenAccumulator.ToString(), lineNum, charNum));
                                tokenAccumulator.Clear();
                                currentStatus.Type = Token.TokenType.None;
                            }

                            continue;
                        }
                        #endregion meta code lexing
                        #region string lexing
                        if (currentStatus.Type == Token.TokenType.Literal_String)
                        {
                            //this is a string

                            tokenAccumulator.Append(c);

                            if (tokenAccumulator[0] == '"') stringBeginSequence = stringEndSequence = "\"";
                            if (tokenAccumulator[0] == '\'') stringBeginSequence = stringEndSequence = "'";
                            if (tokenAccumulator.Length > 2 && tokenAccumulator[0] == '$' && tokenAccumulator[1] == '(')
                            {
                                stringBeginSequence = "$(" + tokenAccumulator[2];
                                stringEndSequence = tokenAccumulator[2] + ")";
                            }

                            if(stringEndSequence.Length > 0
                                && tokenAccumulator.Length>=stringEndSequence.Length*2 
                                && tokenAccumulator.ToString().Substring(tokenAccumulator.Length - stringEndSequence.Length) == stringEndSequence)
                            {
                                //found end of string -- flush
                                tokenAccumulator.Remove(0, stringBeginSequence.Length);
                                tokenAccumulator.Replace(stringEndSequence, "");
                                tokens.Add(new Token(currentStatus.Type, tokenAccumulator.ToString(),lineNum,charNum));
                                tokenAccumulator.Clear();
                                currentStatus.Type = Token.TokenType.None;
                                stringEndSequence = "";
                                stringBeginSequence = "";
                                continue;
                            }
                            //if (charNum == line.Length) tokenAccumulator.Append(Environment.NewLine);

                            continue;
                        }
                        #endregion string lexing
                        #region comment lexing
                        else if (tokenAccumulator.Length > 1 && tokenAccumulator[0] == '/' && tokenAccumulator[1] == '/')
                        {
                            //this is a comment
                            currentStatus.Type = Token.TokenType.Comment;

                            tokenAccumulator.Append(c);

                            if (c == '{')
                                ++nestLevel;
                            if (c == '}')
                            {
                                --nestLevel;
                                if (nestLevel == 0)
                                {
                                    //flush block comment
                                    if (tokenAccumulator.Length > 1)
                                    {
                                        tokenAccumulator.Remove(0, 2);
                                        tokens.Add(new Token(currentStatus.Type, tokenAccumulator.ToString(),lineNum,charNum));
                                        tokenAccumulator.Clear();
                                        currentStatus.Type = Token.TokenType.None;
                                    }
                                    continue;
                                }
                            }

                            if(charNum == line.Length)
                            {                                
                                if(nestLevel == 0)
                                {
                                    //flush single line comment
                                    if (tokenAccumulator.Length > 1)
                                    {
                                        tokenAccumulator.Remove(0, 2);
                                        tokens.Add(new Token(currentStatus.Type, tokenAccumulator.ToString(), lineNum, charNum));
                                        tokenAccumulator.Clear();
                                        currentStatus.Type = Token.TokenType.None;
                                    }
                                    //tokens.Add(new Token(Token.TokenType.NewLine, Environment.NewLine, lineNum, line.Length));
                                    break;
                                }
                                else
                                {
                                    tokenAccumulator.Append(Environment.NewLine);
                                    break;
                                }
                                
                            }
                        }
                        #endregion comment lexing
                        #region general lexing
                        else if (currentStatus.Type == Token.TokenType.None)
                        {
                            //set token type
                            currentStatus = Token.getTokenCharType(c);
                            if (currentStatus.CharacterList.Count == 0)
                                throw new Exception($"Unexpected character '{c}' at line {lineNum} character {charNum}{Environment.NewLine}---->{line.Substring(0, charNum-1)} >>{c}<<");

                            if(currentStatus.Type == Token.TokenType.Separator)
                            {
                                //add single char as token
                                tokens.Add(new Token(currentStatus.Type, c.ToString(), lineNum, charNum));
                                currentStatus.Type = Token.TokenType.None;
                            }else
                            {
                                //append first character
                                tokenAccumulator.Append(c);
                            }
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
                                tokens.Add(new Token(currentStatus.Type, tokenAccumulator.ToString(), lineNum, charNum));
                                tokenAccumulator.Clear();

                                //get new token type
                                currentStatus = Token.getTokenCharType(c);
                                if (currentStatus.CharacterList.Count == 0)
                                    throw new Exception($"Unexpected character '{c}' at line {lineNum} character {charNum}{Environment.NewLine}---->{line.Substring(0, charNum-1)} >>{c}<<");

                                if (currentStatus.Type == Token.TokenType.Separator)
                                {
                                    //add single char as token
                                    tokens.Add(new Token(currentStatus.Type, c.ToString(), lineNum, charNum));
                                    currentStatus.Type = Token.TokenType.None;
                                }
                                else
                                {
                                    //append first character
                                    tokenAccumulator.Append(c);
                                }
                            }

                            //if end of line flush
                            if (charNum == line.Length)
                            {
                                tokens.Add(new Token(currentStatus.Type, tokenAccumulator.ToString(), lineNum, charNum));
                                tokenAccumulator.Clear();
                                currentStatus.Type = Token.TokenType.None;
                                break;
                            }
                        }
                        #endregion general lexing
                    }
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
