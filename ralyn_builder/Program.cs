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
            Root,
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

                            var s = this.representation.TrimStart(new char[] { ' ', ':', '\t' }).Trim();

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
                                var ignoreWarning = numEx;
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
        public ralyn(string ralynString)
        {            
            this.type = ralyn.TypeOf.Root;
            this.tag = new ralynTag();
            this.sValue = "";
            this.nValue = double.NaN;
            this.children = ralyn.Lex(ralynString);
            this.representation = "null";
            this.lineNumber = -1;
            this.charNumber = -1;
        }
        #endregion Constuctors

        #region Lexer
        private static List<ralyn> Lex(string code)
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

                var ids = new List<string>();

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
                                    if(currentValue.Tag.name.Contains('*')
                                        || currentValue.Tag.name.Contains('[')
                                        || currentValue.Tag.name.Contains(']')
                                        || currentValue.Tag.name.Contains('.')
                                        || currentValue.Tag.name.Contains('<')
                                        || currentValue.Tag.name.Contains('-')
                                        || currentValue.Tag.name.Contains('!')
                                        || currentValue.Tag.name.Contains('@')
                                        || currentValue.Tag.name.Contains('#')
                                        || currentValue.Tag.name.Contains('$')
                                        )
                                        {
                                            currentValue.error = $"Tag name <{currentValue.Tag.name}> contains illegal characters: ['*', '[', ']', '.', '<', '-', '!', '@', '#', '$']";
                                        }

                                    if(ids.Contains(currentValue.Tag.name))
                                    {
                                        //this tag name already exists at this level... trigger error
                                        currentValue.error = $"Tag name <{currentValue.Tag.name}> appears multiple times";
                                    }else{
                                        ids.Add(currentValue.Tag.name);
                                    }

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

            //check for mixed mode
            var mode = "";
            foreach(var r in rObj)
            {
                if(r.Type == ralyn.TypeOf.Comment) continue;
                
                var tagCheck = false;
                var noTagCheck = false;

                if(String.IsNullOrEmpty(r.Tag.name))
                {
                    noTagCheck = true;
                    if(mode == "") mode = "non-associative";
                }else{
                    tagCheck = true;
                    if(mode == "") mode = "associative";
                }

                if(tagCheck && mode == "non-associative")
                {
                    r.error = "unexpected tag in non-associative object";                    
                }
                if(noTagCheck && mode == "associative")
                {
                    r.error = "missing tag in associative object";                    
                }
            }
            

            return rObj;
        }        

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
        #endregion Lexer

        #region Ralyn output
        public override string ToString()
       {
           switch(this.type)
                {
                    case TypeOf.Null:
                        return "Null";// + " -> NULL";
                    case TypeOf.True:
                        return "true";// + " -> bool";
                    case TypeOf.False:
                        return "false";// + " -> bool";
                    case TypeOf.String:
                        return this.sValue;// + " -> String";
                    case TypeOf.Number:
                        return this.nValue.ToString();// + " -> Number";
                    case TypeOf.Comment:
                        return "//" 
                            + this.representation;// + " -> Comment";
                    case TypeOf.Root:    
                        return this.ToStructure();
                    case TypeOf.rList:
                        var r = new ralyn();
                        r.Children = this.Children;
                        return r.ToStructure();
                    default:
                        return "";
                }
       }
        private string ToStructure()
        {
            return this.ToStructure(0);
        }
        private string ToStructure(int indent)
        {
            try
            {  
                var tagname = (String.IsNullOrEmpty(this.tag.nameSpace) && String.IsNullOrEmpty(this.tag.name) ? "" : this.tag + " : ");

                if(!String.IsNullOrEmpty(this.error))
                {
                    //return "<Exception|> : { <line> : " + this.lineNumber.ToString() + " <char> : " + this.charNumber.ToString() + " <message> : $(' " + this.error + "')}";
                    return "//{Exception: " + this.error + "}";
                }else
                //single value
                switch(this.type)
                {
                    case TypeOf.Null:
                        return tagname + "Null";// + " -> NULL";
                    case TypeOf.True:
                        return tagname + "true";// + " -> bool";
                    case TypeOf.False:
                        return tagname + "false";// + " -> bool";
                    case TypeOf.String:
                        return tagname + this.representation;// + " -> String";
                    case TypeOf.Number:
                        return tagname + this.nValue.ToString();// + " -> Number";
                    case TypeOf.Comment:
                        return "//" 
                            + tagname
                            + this.representation;// + " -> Comment";
                    case TypeOf.Root:
                        var rootString = new System.Text.StringBuilder();
                        foreach (ralyn r in this.children)
                        {
                            rootString.AppendLine(new String('\t', indent) + r.ToStructure(indent));
                        }
                        return rootString.ToString();
                    case TypeOf.rList:
                        var ret = new System.Text.StringBuilder();
                        ret.AppendLine(tagname + "{");
                        ++indent;
                        foreach (ralyn r in this.children)
                        {
                            ret.AppendLine(new String('\t', indent) + r.ToStructure(indent));
                        }
                        --indent;
                        ret.AppendLine(new String('\t', indent) + "}");
                        return ret.ToString();
                    default:
                        return "";
                }
            }catch(Exception outputEx)
            {                
                //return "<Exception|> : { <line> : " + this.lineNumber.ToString() + " <char> : " + this.charNumber.ToString() + " <message> : $(' " + outputEx.Message + "')}";
                return "//{Exception: " + outputEx.Message + "}";
            }
        }       
        #endregion Ralyn output

        #region String output... JSON/XML transformations

        public string Transform(string transformDocument)
        {
            var resultDocument = new StringBuilder();

            return resultDocument.ToString();
        }

        public string ToJSON(int indent = 0)
        {
                var tagname = "\"" + (string.IsNullOrEmpty(this.tag.nameSpace) ? "" : this.tag.nameSpace + "|") + this.tag.name + "\" : ";
                if(tagname == "\"\" : ") tagname="";

            try
            {
                //single value
                switch (this.type)
                {
                    case TypeOf.Null:
                        return tagname + "null,";// + " -> NULL";
                    case TypeOf.True:
                        return tagname + "true,";// + " -> bool";
                    case TypeOf.False:
                        return tagname + "false,";// + " -> bool";
                    case TypeOf.String:
                        return tagname + "\"" + this.sValue + "\",";// + " -> String";
                    case TypeOf.Number:
                        return tagname + this.nValue.ToString() + ",";// + " -> Number";
                    case TypeOf.Comment:
                        return "/*" + tagname + this.representation + "*/";// + " -> Comment";
                    case TypeOf.Root:
                        var rootString = new System.Text.StringBuilder();
                        rootString.AppendLine("[" );
                        ++indent;
                        foreach (ralyn r in this.children)
                        {
                            rootString.AppendLine(new String('\t', indent) + r.ToJSON(indent));
                        }
                        --indent;
                        rootString.AppendLine("]");
                        return rootString.ToString();
                    case TypeOf.rList:
                        var ret = new System.Text.StringBuilder();
                        ret.AppendLine("\"" + (string.IsNullOrEmpty(this.tag.nameSpace) ? "" : this.tag.nameSpace + "|") 
                            + this.tag.name + "\" : {");
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
                var tagname = (string.IsNullOrEmpty(this.tag.nameSpace) ? "" : this.tag.nameSpace + ":") 
                    + this.tag.name.ToUpper();

                if(String.IsNullOrEmpty(tagname)) tagname="VALUE";

                //single value
                switch (this.type)
                {
                    case TypeOf.Null:
                        return "<" + tagname + ">" + "null" + "</" + tagname + ">";// + " -> NULL";
                    case TypeOf.True:
                        return "<" + tagname + ">"  + "true" + "</" + tagname + ">";// + " -> bool";
                    case TypeOf.False:
                        return "<" + tagname + ">"  + "false" + "</" + tagname + ">";// + " -> bool";
                    case TypeOf.String:
                        return "<" + tagname + ">"  + this.sValue + "</" + tagname + ">";// + "-> String";
                    case TypeOf.Number:
                        return "<" + tagname + ">"  + this.nValue.ToString() + "</" + tagname + ">";// + " -> Number";
                    case TypeOf.Comment:
                        var commentReturn = "";
                        if(tagname == "VALUE") commentReturn =  "<!-- " + this.representation + " -->";
                        else commentReturn =  "<!--  " + tagname + " - " + this.representation + "</" + tagname + ">  -->";// + " -> Comment";
                        return commentReturn;
                    case TypeOf.Root:
                        var rootString = new System.Text.StringBuilder();
                        rootString.AppendLine("<ROOT>");
                        ++indent;
                        foreach (ralyn r in this.children)
                        {
                            rootString.AppendLine(new String('\t', indent) + r.ToXML(indent));
                        }
                        --indent;
                        rootString.AppendLine(new String('\t', indent) + "</ROOT>");
                        return rootString.ToString();
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
    
        #region ralyn path
        public ralyn Path(string path)
        {

            //Console.WriteLine($"searching {this.Tag.name} for {path}");
            ralyn result = this;
            var directions = path.Split(new char[]{'>'});
            var count = 0;
            
            foreach(var step in directions)
            {
                //Console.WriteLine($"step:{step}");
                ++count;
                if(step == "*")
                {
                    //create list of ralyn elements 
                    var subresult = new ralyn();
                    var subresultChildren = new List<ralyn>();
                    foreach(var item in result.Children)
                    {                        
                        var newDirections = String.Join(">",directions.Skip(count).Take(directions.Length - count).ToArray());
                        
                        
                        if(item.Type!=ralyn.TypeOf.Comment && String.IsNullOrEmpty(item.error))
                        {
                            var subPathReturns = item.Path(newDirections);
                            
                            if(subPathReturns != null) 
                            {
                                subPathReturns.Tag.name = "";
                                subresultChildren.Add(subPathReturns);
                            }
                        }
                    }
                    subresult.Children = subresultChildren;
                    return subresult;
                }else{

                    if(step.StartsWith("[") && step.EndsWith("]"))
                    {
                        int index;
                        bool success = Int32.TryParse(step.Trim(new char[] {'[',']'}), out index);
                        if(!success){
                            //Console.WriteLine("INVALID PATH - index NaN");   
                            return null;
                        }

                        foreach(var item in result.Children)
                        {
                            if(index<0){
                                //Console.WriteLine("INVALID PATH - index less than Zero");   
                                return null;
                            }
                            if(item.Type!=ralyn.TypeOf.Comment && String.IsNullOrEmpty(item.error))
                            {
                                if(index==0)
                                {
                                    result = item;
                                    break;
                                }else --index;             
                            }
                        }
                        if(index>0){
                            //Console.WriteLine("INVALID PATH - index out of range");   
                            return null;
                        }
                        
                        continue;
                    }

                    var foundPath = false;
                    foreach(var item in result.Children)
                    {
                        //Console.WriteLine($"\titem:{item.Tag.name}");
                        if(item.Type==ralyn.TypeOf.Comment || !String.IsNullOrEmpty(item.error))continue;
                        if(item.Tag.name == step)
                        {
                            foundPath = true;
                            result = item;                       
                            break;
                        }                    
                    }
                    if(!foundPath){
                        //Console.WriteLine($"INVALID PATH - could not find {step}");
                        return null;
                    }
                }
            }


            return result;
        }
        
        #endregion ralyn path
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
                Console.WriteLine(" 2. Format RALYN");
                Console.WriteLine(" 3. Transpile to JSON");
                Console.WriteLine(" 4. Transpile to XML");
                Console.WriteLine(" 5. Path");
                Console.WriteLine(" 0. Exit");
                var key = Console.ReadKey();
                string codeFromFile = System.IO.File.ReadAllText(fileName);
                ralyn ralynDoc = new ralyn();

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
                        //Format RALYN
                        Console.WriteLine();

                        ralynDoc = new ralyn(codeFromFile);
                        Console.WriteLine(ralynDoc);

                        Console.WriteLine("press any key to continue...");
                        Console.ReadKey();
                        break;
                    case '3':
                        //Transpile to JSON
                        Console.WriteLine();
                                              
                        ralynDoc = new ralyn(codeFromFile);
                        Console.WriteLine(ralynDoc.ToJSON());

                        Console.WriteLine("press any key to continue...");
                        Console.ReadKey();
                        break;
                    case '4':
                        //Transpile to XML
                        Console.WriteLine();
                                               
                        ralynDoc = new ralyn(codeFromFile);
                        Console.WriteLine(ralynDoc.ToXML());

                        Console.WriteLine("press any key to continue...");
                        Console.ReadKey();
                        break;
                    case '5':
                        //Path
                        Console.WriteLine();
                        Console.WriteLine("Enter path... something like ralyn>record-1>a");
                        var path = Console.ReadLine();

                        ralynDoc = new ralyn(codeFromFile);
                        Console.WriteLine(ralynDoc.Path(path));
                        
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
        #region Parsing
        static void Parse(List<ralyn> objs)
        {
            Console.WriteLine("Parsing is not yet implemented");
        }
        #endregion Parsing

        #region Action Tree
        static void ActionTree(List<ralyn> objs)
        {
            Console.WriteLine("Action Tree is not yet implemented");
        }
        #endregion Action Tree
            
    }
}



