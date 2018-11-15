using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ralyn_builder
{
    class ralyn
    {
        public class ralynTag
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

                var parts = tagText.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    this.nameSpace = parts[0];
                    this.name = parts[1];
                }
                else if (parts.Length == 1)
                {
                    this.nameSpace = "";
                    this.name = parts[0];
                }
                else
                {
                    this.nameSpace = "";
                    this.name = "";
                }
            }

            public override string ToString()
            {
                if (this.nameSpace != "")
                    return "<" + this.nameSpace + "|" + this.name + ">";
                else
                    return "<" + this.name + ">";
            }
        }
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

        #region private members
        private ralyn.TypeOf type;
        private ralynTag tag;
        private List<ralyn> children;
        private string sValue;
        private double nValue;      
        private string representation;
        private string error;
        private int lineNumber;
        private int charNumber;
        #endregion private members

        #region GET/SET
        public ralyn.TypeOf Type
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
                        return this.children as List<ralyn>;
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
                            /// <summary>
                            /// ralyn strings come in several varieties:
                            ///   1. "string" -- double quoted
                            ///   2. 'string' -- single quoted
                            ///   3. <string> -- angle quoted for identifiers
                            ///   4. $("string") -- special string where " can be replaced by any character
                            /// </summary>
                            
                            this.nValue = double.NaN;
                            this.children = null;

                            var stringValue = "";
                            var isString = false;

                            var s = this.representation.TrimStart(new char[] { ' ', ':', '\t' }).Trim().ToUpper();

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
                                isString = (s1.IndexOf("\"", 1) == s1.Length - 1);
                                stringValue = s.Substring(1, s.Length - 2);
                            }
                            if (s.Length >= 2 && s[0] == '\'')
                            {
                                var s1 = s.Replace("\\'", ""); //ignore escaped single quotes
                                isString = (s1.IndexOf("'", 1) == s1.Length - 1);
                                stringValue = s.Substring(1, s.Length - 2);
                            }
                            if (s.Length >= 2 && s[0] == '<')
                            {
                                isString = (s.IndexOf(">") == s.Length - 1);
                                stringValue = s.Substring(1, s.Length - 2);
                            }

                            if (isString)
                            {
                                this.sValue = stringValue;
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

                            double v;
                            try
                            {
                                v = double.Parse(this.representation, System.Globalization.NumberStyles.Float);
                            }
                            catch (Exception numEx)
                            {
                                v = double.NaN;
                                //Console.WriteLine("ERROR: "+num+" -> " + numEx.Message);
                            }

                            this.nValue = v;
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
                            this.children = value as List<ralyn>;
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
        public List<ralyn> Children
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
        public ralyn()
        {
            this.type = ralyn.TypeOf.Null;
            this.tag = new ralynTag();
            this.sValue = "";
            this.nValue = double.NaN;
            this.children = null;
            this.representation = "null";
            this.lineNumber = -1;
            this.charNumber = -1;
        }
        public ralyn(object value)
        {
            this.lineNumber = -1;
            this.charNumber = -1;
            Value = value;
        }
        #endregion Constuctors

        public static ralyn.TypeOf looksLike(string s)
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

        #region String output... JSON/XML transformations
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
                        return this.tag + " : " + this.representation;// + " -> String";
                    case TypeOf.Number:
                        return this.tag + " : " + this.nValue.ToString();// + " -> Number";
                    case TypeOf.Comment:
                        return "//" + this.tag + " : " + this.representation;// + " -> Comment";
                    case TypeOf.rList:
                        var ret = new System.Text.StringBuilder();
                        ret.AppendLine(this.tag + " : {");
                        ++indent;
                        foreach (ralyn r in this.children)
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
                return "<ralyn|Exception> : { <line> : " + this.lineNumber.ToString() + " <char> : " + this.charNumber.ToString() + " <message> : $(' " + outputEx.Message + "')}";
            }
        }
        
        public string ToJSON(int indent = 0)
        {
            try
            {
                //single value
                switch (this.type)
                {
                    case TypeOf.Null:
                        return "\"" + (string.IsNullOrEmpty(this.tag.nameSpace)?"": this.tag.nameSpace + "|") + this.tag.name + "\" : " + "null,";// + " -> NULL";
                    case TypeOf.True:
                        return "\"" + (string.IsNullOrEmpty(this.tag.nameSpace) ? "" : this.tag.nameSpace + "|") + this.tag.name + "\" : " + "true,";// + " -> bool";
                    case TypeOf.False:
                        return "\"" + (string.IsNullOrEmpty(this.tag.nameSpace) ? "" : this.tag.nameSpace + "|") + this.tag.name + "\" : " + "false,";// + " -> bool";
                    case TypeOf.String:
                        return "\"" + (string.IsNullOrEmpty(this.tag.nameSpace) ? "" : this.tag.nameSpace + "|") + this.tag.name + "\" : \"" + this.sValue + "\",";// + " -> String";
                    case TypeOf.Number:
                        return "\"" + (string.IsNullOrEmpty(this.tag.nameSpace) ? "" : this.tag.nameSpace + "|") + this.tag.name + "\" : " + this.nValue.ToString() + ",";// + " -> Number";
                    case TypeOf.Comment:
                        return "/*" + "\"" + (string.IsNullOrEmpty(this.tag.nameSpace) ? "" : this.tag.nameSpace + "|") + this.tag.name + "\" : " + this.representation + "*/";// + " -> Comment";
                    case TypeOf.rList:
                        var ret = new System.Text.StringBuilder();
                        ret.AppendLine("\"" + (string.IsNullOrEmpty(this.tag.nameSpace) ? "" : this.tag.nameSpace + "|") + this.tag.name + "\" : {");
                        ++indent;
                        foreach (ralyn r in this.children)
                        {
                            ret.AppendLine(new String('\t', indent) + r.ToJSON(indent));
                        }
                        --indent;
                        ret.AppendLine(new String('\t', indent) + "}");
                        return ret.ToString();
                    default:
                        return "";
                }
            }
            catch (Exception outputEx)
            {
                return "//\"Exception\" : { \"line\" : " + this.lineNumber.ToString() + ", \"char\" : " + this.charNumber.ToString() + ", \"message\" : \" " + outputEx.Message + "\",}";
            }
        }
        public string ToXML(int indent = 0)
        {
            try
            {
                var tagname = (string.IsNullOrEmpty(this.tag.nameSpace) ? "" : this.tag.nameSpace + ":") + this.tag.name;

                //single value
                switch (this.type)
                {
                    case TypeOf.Null:
                        return "<" + tagname + ">" + "null" + "</" + tagname + ">";// + " -> NULL";
                    case TypeOf.True:
                        return "<" + tagname + ">"  + "true" + "</" + tagname + ">";// + " -> bool";
                    case TypeOf.False:
                        return "<" + tagname + this.tag.name + ">"  + "false" + "</" + tagname + ">";// + " -> bool";
                    case TypeOf.String:
                        return "<" + tagname + ">"  + this.sValue + "</" + tagname + ">";// + "-> String";
                    case TypeOf.Number:
                        return "<" + tagname + ">"  + this.nValue.ToString() + "</" + tagname + ">";// + " -> Number";
                    case TypeOf.Comment:
                        return "<!--  <" + tagname + ">" + this.representation + "</" + tagname + ">  -->";// + " -> Comment";
                    case TypeOf.rList:
                        var ret = new System.Text.StringBuilder();
                        ret.AppendLine("<" + tagname + ">");
                        ++indent;
                        foreach (ralyn r in this.children)
                        {
                            ret.AppendLine(new String('\t', indent) + r.ToXML(indent));
                        }
                        --indent;
                        ret.AppendLine(new String('\t', indent) + "</" + tagname + ">");
                        return ret.ToString();
                    default:
                        return "";
                }
            }
            catch (Exception outputEx)
            {
                return "<!-- <Exception><line>" + this.lineNumber.ToString() + "</line><char>" + this.charNumber.ToString() + "</char><message>" + outputEx.Message + "</message></Exception> -->";
            }
        }

        public static List<ralyn> JSONtoRalyn(string json)
        {
            throw new NotImplementedException("parsing JSON directly into ralyn is slated for future development but has not yet been implemented");

            //var r = new List<ralyn>();
            //return r;
        }
        #endregion String output... JSON/XML transformations
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

                        Console.WriteLine("no test currently configured");

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
        static List<ralyn> Lex(string code)
        {
            //setup vars
            var rObj = new List<ralyn>();
            string line;
            using (var _code = new System.IO.StringReader(code))
            {
                #region vars
                var accumulator = new StringBuilder();
                var stringBeginSequence = "";
                var stringEndSequence = "";
                var currentTypeOf = ralyn.TypeOf.Undetermined;
                var currentValue = new ralyn();

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

                        if (currentTypeOf == ralyn.TypeOf.Undetermined && (
                            c == ':' ||
                            c == ' ' )) continue;

                        accumulator.Append(c);                       

                        //Console.WriteLine(lineNum.ToString() + ":" + charNum.ToString() + "  " + accumulator.ToString() + "->" + currentTypeOf.ToString());

                        if (currentTypeOf == ralyn.TypeOf.Undetermined)
                        {
                            currentTypeOf = ralyn.looksLike(accumulator.ToString());
                        }                           
                        if (currentTypeOf == ralyn.TypeOf.Undetermined) continue;

                        //OK, we think we know what we are dealing with
                        switch (currentTypeOf)
                        {
                            #region simple value lexing
                            case ralyn.TypeOf.Null:
                                currentValue.Value = "null";
                                rObj.Add(currentValue);

                                //reset
                                accumulator.Length = 0;
                                currentTypeOf = ralyn.TypeOf.Undetermined;
                                currentValue = new ralyn();
                                break;
                            case ralyn.TypeOf.True:
                                currentValue.Value = "true";
                                rObj.Add(currentValue);

                                //reset
                                accumulator.Length = 0;
                                currentTypeOf = ralyn.TypeOf.Undetermined;
                                currentValue = new ralyn();
                                break;
                            case ralyn.TypeOf.False:
                                currentValue.Value = "false";
                                rObj.Add(currentValue);

                                //reset
                                accumulator.Length = 0;
                                currentTypeOf = ralyn.TypeOf.Undetermined;
                                currentValue = new ralyn();
                                break;
                            case ralyn.TypeOf.Tag:
                                //end condition is >:
                                if (c == '>')
                                {
                                    currentValue.Tag = new ralyn.ralynTag(accumulator.ToString());

                                    //reset -- sort of
                                    accumulator.Length = 0;
                                    currentTypeOf = ralyn.TypeOf.Undetermined;
                                    //currentValue = new ralynValue(); //DON'T REST THE ENTIRE VALUE!
                                }
                                break;
                            case ralyn.TypeOf.Number:
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
                                    currentTypeOf = ralyn.TypeOf.Undetermined;
                                    currentValue = new ralyn();
                                }
                                #endregion Number lexing                        
                                break;
                            case ralyn.TypeOf.String:
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
                                    currentTypeOf = ralyn.TypeOf.Undetermined;
                                    currentValue = new ralyn();
                                    stringBeginSequence = "";
                                    stringEndSequence = "";
                                }
                                #endregion String lexing
                                break;
                            #endregion simple value lexing

                            case ralyn.TypeOf.Comment:
                                isComment = true;
                                accumulator.Length = 0;
                                currentTypeOf = ralyn.TypeOf.Undetermined;
                                break;

                            case ralyn.TypeOf.rList:
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
                                    currentTypeOf = ralyn.TypeOf.Undetermined;
                                    currentValue = new ralyn();
                                    isComment = false;
                                }
                                break;

                            case ralyn.TypeOf.ControlCharacter:
                                //ignore for now

                                //reset
                                accumulator.Length = 0;
                                currentTypeOf = ralyn.TypeOf.Undetermined;
                                currentValue = new ralyn();
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
        static void Parse(List<ralyn> objs)
        {

        }
        #endregion Parsing
    }
}



