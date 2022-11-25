using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Interfaces;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.ParsedComponents;
using Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens.VueDirectives;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VueFileParser.Tokenization.ParsedComponents.VueDirectives;

namespace Org.Reddragonit.VueJSMVCDotNet.VueFiles.Tokenization.Tokens
{
    internal static class VueDirective
    {
        public static IVueDirective ConstructDirective(string command,string value)
        {
            if (command.StartsWith(":"))
                command="v-bind"+command;
            IVueDirective directive = null;
            switch (command)
            {
                case "v-for":
                    directive = new ForDirective(value);
                    break;
                case "v-if":
                    directive=new IfDirective(value);
                    break;
                case "v-else":
                    directive = new ElseDirective();
                    break;
                case "v-else-if":
                    directive = new ElseIfDirective(value);
                    break;
                case "v-bind":
                    directive = new FullBindDirective(value);
                    break;
                case "v-bind:key":
                    directive=new KeyDirective(value);
                    break;
                case "v-show":
                    directive=new ShowDirective(value);
                    break;
                case "v-model":
                    directive=new ModelDirective(value);
                    break;
                default:
                    if (command.StartsWith("v-bind:"))
                        directive = new BindDirective(command.Split(':')[1], value);
                    else if (command.StartsWith("v-on:"))
                        directive=new EventDirective(command.Split(':')[1], value);
                    break;
            }
            return directive;
        }
    }
}
