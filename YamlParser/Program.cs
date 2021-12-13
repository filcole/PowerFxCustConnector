using YamlDotNet.RepresentationModel;
using System.IO;
using System.Text.RegularExpressions;

const string Document = @"
Visible: =true
X: =34
# Comment
Y: | 
    =   ""He// /*l*/lo"" &  // test
      ""World""   /* tests */
DateRangePicker As CanvasComponent:
  DefaultStart: |-
    =// input property, customizable default for the component instance
    Now()                      
  DefaultEnd: |-
    =// input property, customizable default for the component instance
    DateAdd( Now(), 1, Days )    
  SelectedStart: =DatePicker1.SelectedDate   // output property
  SelectedEnd: =DatePicker2.SelectedDate     // output property
Text: =""Hello #PowerApps""
Record: |
    ={ a: 1, b: 2 }
";

// Setup the input
var input = new StringReader(Document);

// Load the stream
var yaml = new YamlStream();
yaml.Load(input);

// Examine the stream
var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

foreach (var entry in mapping.Children)
{
    Console.WriteLine(((YamlScalarNode)entry.Key).Value);
    //Console.WriteLine(((YamlScalarNode)entry.Value).Value);

    if (entry.Value is YamlScalarNode)
    {
        var expressionYaml = ((YamlScalarNode)entry.Value).Value;
        var expression = removeComments(expressionYaml);

        Console.WriteLine(expression);

        if (!expression.StartsWith("="))
        {
            Console.WriteLine("Invalid formula");
        }
    }
}

// Thank you https://stackoverflow.com/a/3524689
string removeComments(string input)
{
    var blockComments = @"/\*(.*?)\*/";
    var lineComments = @"//(.*?)\r?\n";
    var strings = @"""((\\[^\n]|[^""\n])*)""";
    var verbatimStrings = @"@(""[^""]*"")+";

    return Regex.Replace(input,
        blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
        me => {
            if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
            {
                return me.Value.StartsWith("//") ? Environment.NewLine : "";
            }
            // Keep the literal strings
            return me.Value;
        },
        RegexOptions.Singleline
    );
}