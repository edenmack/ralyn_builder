# ralyn
ralyn is an experimental programming language.  It's being built from the ground up, primarily for the edification
of it's designer(s) but also to examine some conceptual ideas.  ralyn aims to be minimalistic and simple without
sacrificing ability.

### Features
* ralyn is a declarative language
* exports easily to JSON or XML
* dynamically typed
* tag system for naming data elements or providing context

### Example
<doc> : {<type>:"ralyn"}

//{
 This is a ralyn data
 description program.

  Colons between tag and
  data value are optional,
  as are commas/semicolons
  used to separate values.
}

<strings> : {
  <s1> : "Strings can be simple double quoted"
  <s2> : 'or simple single quoted'
  <s3> : $("or specify the delimiters as
  the first character after '$('")
}
<numbers> : {
  <n1> : -12.3e-5
}
<bool> : {
  <b1> : true
  <b2> : false
  <b3> : null
}
<list>:{
  <nested>:{
    //{as deep as you want!}
  }
}
