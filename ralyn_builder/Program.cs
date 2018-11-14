﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ralyn_builder
{
    class ralynTag
    {
        public string nameSpace;
        public string name;

        public ralynTag()
        {
            this.nameSpace = "";
            this.name = "";
        }
        public ralynTag(string tagText)
        {
            tagText = tagText.Trim();
            tagText = tagText.Trim(new char[] { '<', '>' });

            var parts = tagText.Split(new char[] { '|' },StringSplitOptions.RemoveEmptyEntries);
            if(parts.Length == 2)
            {
                this.nameSpace = parts[0];
                this.name = parts[1];
            }else if(parts.Length == 1)
            {
                this.nameSpace = "";
                this.name = parts[0];
            }else
            {
                this.nameSpace = "";
                this.name = "";
            }
        }

        public override string ToString()
        {
            if(this.nameSpace != "")
                return "<" + this.nameSpace + "|" + this.name + ">";
            else
                return "<" + this.name + ">";
        }
    }
    class ralynValue
    {
        public enum TypeOf
        {
            Undetermined,
            Null,
            True,
            False,
            Number,
            String,
            rList,
            ControlCharacter,
            Tag,
            Comment
        }

        private ralynValue.TypeOf type;
        private ralynTag tag;
        private List<ralynValue> children;
        private string sValue;
        private double nValue;      
        private string representation;
        private string error;
        private int lineNumber;
        private int charNumber;

        #region GET/SET
        public ralynValue.TypeOf Type
        { get { return this.type; } }
        public ralynTag Tag
        {
            get { return this.tag; }
            set { this.tag = value; }
        }
        public object Value
        {
            get
            {
                switch (this.type)
                {
                    case TypeOf.Null:
                        return null;
                    case TypeOf.True:
                        return true as bool?;
                    case TypeOf.False:
                        return false as bool?;
                    case TypeOf.Number:
                        return this.nValue as double?;
                    case TypeOf.String:
                        return this.sValue as string;
                    case TypeOf.rList:
                        return this.children as List<ralynValue>;
                    case TypeOf.Comment:
                        return this.representation as string;
                    default:
                        return null;                    
                }
            }
            set
            {
                try
                {
                    this.representation = value.ToString();
                    this.type = looksLike(this.representation);

                    switch (this.type)
                    {
                        case TypeOf.Null:
                        case TypeOf.True:
                        case TypeOf.False:
                            this.sValue = "";
                            this.nValue = double.NaN;
                            this.children = null;
                            break;
                        case TypeOf.String:
                            this.nValue = double.NaN;
                            this.children = null;

                            var sTest = ralynValue.stringTest(this.representation);
                            if (sTest.Key)
                            {
                                this.sValue = sTest.Value;
                            }
                            else
                            {
                                //bad string/empty string
                                this.sValue = "";
                                //this.representation = "EMPTY STRING";
                            }
                            break;
                        case TypeOf.Number:
                            this.sValue = "";
                            this.children = null;

                            this.nValue = ralynValue.parseNumber(this.representation);
                            break;
                        case TypeOf.Tag:
                            //this really isn't the best way to assign a tag value
                            this.tag = new ralynTag(value.ToString());

                            this.representation = "";
                            this.type = TypeOf.Undetermined;
                            break;
                        case TypeOf.Comment:
                            this.representation = value.ToString().Substring(2);
                            this.sValue = "";
                            this.children = null;
                            this.nValue = double.NaN;
                            break;
                        case TypeOf.rList:
                            //this doesn't work
                            this.children = value as List<ralynValue>;
                            break;
                        default:
                            //shouldn't be able to get here
                            this.sValue = "";
                            this.nValue = double.NaN;
                            this.children = null;
                            break;
                    }
                }catch(Exception valueEx) { this.error = valueEx.Message;this.type = TypeOf.Undetermined; }                        
            }
        }
        public List<ralynValue> Children
        {
            get { return this.children; }
            set
            {
                this.children = value;
                this.type = TypeOf.rList;
            }
        }
        public int LineNumber
        {
            get { return this.lineNumber; }
            set { this.lineNumber = value; }
        }
        public int CharNumber
        {
            get { return this.charNumber; }
            set { this.charNumber = value; }
        }
        #endregion GET/SET

        #region Constructors
        public ralynValue()
        {
            this.type = ralynValue.TypeOf.Null;
            this.tag = new ralynTag();
            this.sValue = "";
            this.nValue = double.NaN;
            this.children = null;
            this.representation = "null";
            this.lineNumber = -1;
            this.charNumber = -1;
        }
        public ralynValue(object value)
        {
            this.lineNumber = -1;
            this.charNumber = -1;
            Value = value;
        }
        #endregion Constuctors

        public static ralynValue.TypeOf looksLike(string s)
        {
            s = s.TrimStart(new char[] { ' ', ':', '\t' }).Trim().ToUpper();
            if(s.Length == 0)
            {
                return TypeOf.Undetermined;
            }
            else if(s.Length >= 2 && 
                s[0] == '/' && 
                s[1] == '/')
            {
                return TypeOf.Comment;
            }
            else if (s == "NULL")
            {
                return TypeOf.Null;
            }
            else if (s == "TRUE")
            {
                return TypeOf.True;
            }
            else if (s == "FALSE")
            {
                return TypeOf.False;
            }
            else if (s.Length > 0 && (
               s[0] == '"' ||
               s[0] == '\'' ||               
               s[0] == '$'))
            {
                return TypeOf.String;
            }else if(s.Length > 0 && 
                s[0] == '<')
            {
                return TypeOf.Tag;
            }
            else if (s.Length > 0 &&
                s[0] == '{')
            {
                return TypeOf.rList;
            }
            else if (s.Length > 0 && (
                s[0] == '1' ||
                s[0] == '2' ||
                s[0] == '3' ||
                s[0] == '4' ||
                s[0] == '5' ||
                s[0] == '6' ||
                s[0] == '7' ||
                s[0] == '8' ||
                s[0] == '9' ||
                s[0] == '0' ||
                s[0] == '-' ||
                s[0] == '+' ||
                s[0] == '.'))
            {
                return TypeOf.Number;
            }
            else if (s.Length > 0 && (
               s[0] == ':' ||
               s[0] == ';' ||
               s[0] == ','))
            {
                return TypeOf.ControlCharacter;
            }
            else
            {
                return TypeOf.Undetermined;
            }

        }

        #region Number stuff
        public static double parseNumber(string num)
        {
            //var re = new System.Text.RegularExpressions.Regex(@"^[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?$");
            //var isNum = re.Match(num).Success;
            double v;
            try
            {
                v = double.Parse(num, System.Globalization.NumberStyles.Float);
            }
            catch (Exception numEx)
            {
                v = double.NaN;
                //Console.WriteLine("ERROR: "+num+" -> " + numEx.Message);
            }
            return v;
        }
        #endregion Number stuff

        #region String stuff
        /// <summary>
        /// ralyn strings come in several varieties:
        ///   1. "string" -- double quoted
        ///   2. 'string' -- single quoted
        ///   3. <string> -- angle quoted for identifiers
        ///   4. $("string") -- special string where " can be replaced by any character
        /// </summary>
        public static KeyValuePair<bool,string> stringTest(string s)
        {
            var stringValue = "";
            var isString = false;

            s = s.TrimStart(new char[] { ' ', ':', '\t' }).Trim().ToUpper();

            if (s.Length >= 5 && s[0] == '$' && s[1] == '(')
            {
                var quote = s[2].ToString();
                if (s.IndexOf(quote + ")") == s.Length - 2)
                {
                    isString = true;
                    stringValue = s.Substring(3, s.Length - 5);
                }
            }
            if (s.Length >= 2 && s[0] == '"')
            {
                var s1 = s.Replace("\\\"", ""); //ignore escaped double quotes
                isString = (s1.IndexOf("\"",1) == s1.Length - 1);
                stringValue = s.Substring(1, s.Length - 2);
            }
            if (s.Length >= 2 && s[0] == '\'')
            {
                var s1 = s.Replace("\\'", ""); //ignore escaped single quotes
                isString = (s1.IndexOf("'",1) == s1.Length - 1);
                stringValue = s.Substring(1, s.Length - 2);
            }
            if (s.Length >= 2 && s[0] == '<')
            {
                isString = (s.IndexOf(">") == s.Length - 1);
                stringValue = s.Substring(1, s.Length - 2);
            }

            return new KeyValuePair<bool, string>(isString, stringValue);
        }
        #endregion String stuff

        public override string ToString()
        {
            return this.ToString(0);
        }
        public string ToString(int indent)
        {
            try
            {                
                //single value
                switch(this.type)
                {
                    case TypeOf.Null:
                        return this.tag + " : " + "Null";// + " -> NULL";
                    case TypeOf.True:
                        return this.tag + " : " + "true";// + " -> bool";
                    case TypeOf.False:
                        return this.tag + " : " + "false";// + " -> bool";
                    case TypeOf.String:
                        return this.tag + " : " + this.sValue;// + " -> String";
                    case TypeOf.Number:
                        return this.tag + " : " + this.nValue.ToString();// + " -> Number";
                    case TypeOf.Comment:
                        return "//" + this.tag + " : " + this.representation;// + " -> Comment";
                    case TypeOf.rList:
                        var ret = new System.Text.StringBuilder();
                        ret.AppendLine(this.tag + " : {");
                        ++indent;
                        foreach (ralynValue r in this.children)
                        {
                            ret.AppendLine(new String('\t', indent) + r.ToString(indent));
                        }
                        --indent;
                        ret.AppendLine(new String('\t', indent) + "}");
                        return ret.ToString();
                    default:
                        return "";
                }
            }catch(Exception outputEx)
            {                
                return "<ralyn|Exception> : { <line> : " + this.lineNumber.ToString() + " <char> : " + this.charNumber.ToString() + " <message> : $(' " + outputEx.Message + "')";
            }
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
                Console.WriteLine(" 2. ad hoc Test");
                Console.WriteLine(" 3. Lex code");
                //Console.WriteLine(" 4. Parser code");
                //Console.WriteLine(" 5. Create Action Tree from code");
                //Console.WriteLine(" 6. Transpile code");
                //Console.WriteLine(" 7. Compile code");
                //Console.WriteLine(" 8. Execute code");
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
                        }
                        else
                        {
                            Console.WriteLine($"Could not locate {tempFile}, reverting to {fileName}");
                            Console.WriteLine("press any key to continue...");
                            Console.ReadKey();
                        }
                        break;
                    case '2':
                        Console.WriteLine();
                        var testValue = "eden";
                        var test = ralynValue.parseNumber(testValue);
                        //Console.WriteLine("no ad hoc test configured");
                        Console.WriteLine($"{testValue}->{test}");

                        Console.WriteLine("press any key to continue...");
                        Console.ReadKey();
                        break;
                    case '3':
                        //Lex
                        Console.WriteLine();
                        try
                        {
                            var codeFromFile = System.IO.File.ReadAllText(fileName);
                            var result = Lex(codeFromFile);
                            foreach(var r in result)
                            Console.WriteLine(r);
                        }
                        catch (Exception lexEx)
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
        static List<ralynValue> Lex(string code)
        {
            //setup vars
            var rObj = new List<ralynValue>();
            string line;
            using (var _code = new System.IO.StringReader(code))
            {
                #region vars
                var accumulator = new StringBuilder();
                var stringBeginSequence = "";
                var stringEndSequence = "";
                var currentTypeOf = ralynValue.TypeOf.Undetermined;
                var currentValue = new ralynValue();

                var nestLevel = 0;
                var isComment = false;
                #endregion vars

                while (_code.Peek() > -1)
                {
                    line = _code.ReadLine();//.Trim();
                    ++currentValue.LineNumber;
                    currentValue.CharNumber = -1;

                    if(accumulator.Length > 0)
                    {
                        accumulator.AppendLine("");
                    }

                    foreach(char c in line)
                    {
                        ++currentValue.CharNumber;

                        accumulator.Append(c);                       

                        //Console.WriteLine(lineNum.ToString() + ":" + charNum.ToString() + "  " + accumulator.ToString() + "->" + currentTypeOf.ToString());

                        if (currentTypeOf == ralynValue.TypeOf.Undetermined)
                        {
                            currentTypeOf = ralynValue.looksLike(accumulator.ToString());
                        }                           
                        if (currentTypeOf == ralynValue.TypeOf.Undetermined) continue;

                        //OK, we think we know what we are dealing with
                        switch (currentTypeOf)
                        {
                            #region simple value lexing
                            case ralynValue.TypeOf.Null:
                                currentValue.Value = "null";
                                rObj.Add(currentValue);

                                //reset
                                accumulator.Length = 0;
                                currentTypeOf = ralynValue.TypeOf.Undetermined;
                                currentValue = new ralynValue();
                                break;
                            case ralynValue.TypeOf.True:
                                currentValue.Value = "true";
                                rObj.Add(currentValue);

                                //reset
                                accumulator.Length = 0;
                                currentTypeOf = ralynValue.TypeOf.Undetermined;
                                currentValue = new ralynValue();
                                break;
                            case ralynValue.TypeOf.False:
                                currentValue.Value = "false";
                                rObj.Add(currentValue);

                                //reset
                                accumulator.Length = 0;
                                currentTypeOf = ralynValue.TypeOf.Undetermined;
                                currentValue = new ralynValue();
                                break;
                            case ralynValue.TypeOf.Tag:
                                //end condition is >:
                                if (c == '>')
                                {
                                    currentValue.Tag = new ralynTag(accumulator.ToString());

                                    //reset -- sort of
                                    accumulator.Length = 0;
                                    currentTypeOf = ralynValue.TypeOf.Undetermined;
                                    //currentValue = new ralynValue(); //DON'T REST THE ENTIRE VALUE!
                                }
                                break;
                            case ralynValue.TypeOf.Number:
                                #region Number lexing
                                //keep accumulating until encouter , or ; 
                                if (c == ',' || c == ';' || c == ' ' || currentValue.CharNumber == line.Length-1)
                                {
                                    if (c == ',' || c == ';' || c == ' ')
                                    {
                                        accumulator.Length--; //nerf delimiter
                                    }

                                    if(accumulator[0] == ':')
                                    {
                                        currentValue.Value = accumulator.ToString().Substring(1);
                                    }else
                                    {
                                        currentValue.Value = accumulator.ToString();
                                    }                                    
                                    rObj.Add(currentValue);

                                    //reset
                                    accumulator.Length = 0;
                                    currentTypeOf = ralynValue.TypeOf.Undetermined;
                                    currentValue = new ralynValue();
                                }
                                #endregion Number lexing                        
                                break;
                            case ralynValue.TypeOf.String:
                                #region String lexing
                                #region String boundary conditions

                                var test = accumulator.ToString().Trim().TrimStart(new char[] { ':', ' ', '\t' });

                                if(stringBeginSequence == ""
                                    && test.Length > 0 
                                    && test[0] == '"')
                                {
                                    stringBeginSequence = "\"";
                                    stringEndSequence = "\"";

                                }else if(stringBeginSequence == ""
                                    && test.Length > 0
                                    && test[0] == '\'')
                                {
                                    stringBeginSequence = "\'";
                                    stringEndSequence = "\'";

                                }
                                else if (stringBeginSequence == "" 
                                    && test.Length > 2 
                                    && test[0] == '$' 
                                    && test[1] == '(')
                                {
                                    stringBeginSequence = "$(" + test[2].ToString();
                                    stringEndSequence = test[2].ToString() + ")";

                                }
                                #endregion String boundary conditions
                                //accumulate until encounter string end condition
                                if (test.Length >= 2 
                                    && stringEndSequence != ""
                                    && test.ToString().EndsWith(stringEndSequence)
                                    && !test.ToString().EndsWith("\\" + stringEndSequence)
                                    )
                                {
                                    currentValue.Value = test.ToString();
                                    rObj.Add(currentValue);

                                    //reset
                                    accumulator.Length = 0;
                                    currentTypeOf = ralynValue.TypeOf.Undetermined;
                                    currentValue = new ralynValue();
                                    stringBeginSequence = "";
                                    stringEndSequence = "";
                                }
                                #endregion String lexing
                                break;
                            #endregion simple value lexing

                            case ralynValue.TypeOf.Comment:
                                isComment = true;
                                accumulator.Length = 0;
                                currentTypeOf = ralynValue.TypeOf.Undetermined;
                                break;

                            case ralynValue.TypeOf.rList:
                                if(c == '{')
                                {
                                    ++nestLevel;
                                }
                                if(c == '}')
                                {
                                    --nestLevel;
                                }
                                if(nestLevel == 0)
                                {
                                    if (!isComment)
                                    {
                                        //currentValue.Value = Lex(accumulator.ToString().Trim(new char[] { '{', '}' }));
                                        currentValue.Children = Lex(accumulator.ToString().Trim(new char[] { '{', '}' }));
                                        rObj.Add(currentValue);
                                    }else
                                    {
                                        //currentValue.Type = ralynValue.TypeOf.Comment;
                                        currentValue.Value = "//" + accumulator.ToString();
                                        rObj.Add(currentValue);
                                    }

                                    //reset
                                    accumulator.Length = 0;
                                    currentTypeOf = ralynValue.TypeOf.Undetermined;
                                    currentValue = new ralynValue();
                                    isComment = false;
                                }
                                break;

                            case ralynValue.TypeOf.ControlCharacter:
                                //ignore for now

                                //reset
                                accumulator.Length = 0;
                                currentTypeOf = ralynValue.TypeOf.Undetermined;
                                currentValue = new ralynValue();
                                break;
                            default:
                                break;
                        }


                    }
                }
            }



            return rObj;
        }
        #endregion Lexical Analysis

        #region Parsing
        static void Parse(List<ralynValue> objs)
        {

        }
        #endregion Parsing
    }
}



