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
&lt;doc&gt; : {&lt;type&gt;:"ralyn"}

//{
 This is a ralyn data
 description program.

  Colons between tag and
  data value are optional,
  as are commas/semicolons
  used to separate values.
}

&lt;strings&gt; : {
  &lt;s1&gt; : "Strings can be simple double quoted"
  &lt;s2&gt; : 'or simple single quoted'
  &lt;s3&gt; : $("or specify the delimiters as
  the first character after '$('")
}
&lt;numbers&gt; : {
  &lt;n1&gt; : -12.3e-5
}
&lt;bool&gt; : {
  &lt;b1&gt; : true
  &lt;b2&gt; : false
  &lt;b3&gt; : null
}
&lt;list&gt;:{
  &lt;nested&gt;:{
    //{as deep as you want!}
  }
}
