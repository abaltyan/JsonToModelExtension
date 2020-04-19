using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonToModelLib
{
    public class CodeExamples
    {
        public const string Directives = "using System; \nusing System.Collections.Generic; \nusing System.Text; \n";

        public const string GetSet = "{ get; set; }";

        public static string GetSetCustom(string fieldName)
        {
            return "\n\t\t{\n" + $"\t\t\tget => {fieldName};\n\t\t\tset => {fieldName} = value;" + "\n\t\t}";
        }

        public static string GetSet_Decrypt(string fieldName)
        {
            return "\n\t\t{\n" + $"\t\t\tget => {fieldName};\n\t\t\tset => {fieldName} = Cryptor.Decrypt(value);" + "\n\t\t}";
        }
    }
}
