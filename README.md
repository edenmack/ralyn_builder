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
  data value are optional,<br>
  as are commas/semicolons
  used to separate values.
}

&lt;strings&gt; : {<br>
  &lt;s1&gt; : "Strings can be simple double quoted"<br>
  &lt;s2&gt; : 'or simple single quoted'<br>
  &lt;s3&gt; : $("or specify the delimiters as<br>
  the first character after '$('")<br>
}<br>
&lt;numbers&gt; : {<br>
  &lt;n1&gt; : -12.3e-5<br>
}<br>
&lt;bool&gt; : {<br>
  &lt;b1&gt; : true<br>
  &lt;b2&gt; : false<br>
  &lt;b3&gt; : null<br>
}<br>
&lt;list&gt;:{<br>
  &lt;nested&gt;:{<br>
    //{as deep as you want!}<br>
  }<br>
}<br>
