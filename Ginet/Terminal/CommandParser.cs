using System.Collections.Generic;
using System.Linq;

namespace Ginet.Terminal
{
    internal class CommandParser : ICommandParser
    {
        private readonly char[] whiteSpace = new[] { ' ' };
        private readonly Dictionary<string, CommandInfo.ContinuationOption> tokens =
            new Dictionary<string, CommandInfo.ContinuationOption>()
            {
                [">"] = CommandInfo.ContinuationOption.Append
            };

        public IEnumerable<CommandInfo> Parse(string text)
        {
            var textWords = text.Split(whiteSpace, System.StringSplitOptions.RemoveEmptyEntries);

            return GetCommandInfo(textWords, 0, new List<CommandInfo>());
        }

        private IEnumerable<CommandInfo> GetCommandInfo(string[] words, int wordCounter, IList<CommandInfo> commands)
        {
            var commandInfo = new CommandInfo();
            commandInfo.Arguments = new List<string>();

            if (wordCounter >= words.Length)
            {
                return commands;
            }
            else
            {
                commands.Add(commandInfo);
            }

            commandInfo.Command = words[wordCounter];

            for (wordCounter++; wordCounter < words.Length; wordCounter++)
            {
                if (tokens.ContainsKey(words[wordCounter]))
                {
                    commandInfo.Continuation = tokens[words[wordCounter]];
                    GetCommandInfo(words, wordCounter + 1, commands);
                    break;
                }
                else 
                {
                    commandInfo.Arguments.Add(words[wordCounter]);
                }
            }
            
            return commands;
        }
    }
}
