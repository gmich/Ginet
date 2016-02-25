using System.Collections.Generic;

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
            var arguments = new List<string>();

            if (wordCounter >= words.Length)
            {
                return commands;
            }

            commandInfo.Command = words[wordCounter];

            for (wordCounter++; wordCounter < words.Length; wordCounter++)
            {
                if (tokens.ContainsKey(words[wordCounter]))
                {
                    commandInfo.Continuation = tokens[words[wordCounter]];
                    commandInfo.Arguments = arguments;
                    commands.Add(commandInfo);
                    GetCommandInfo(words, wordCounter + 1, commands);
                }
                arguments.Add(words[wordCounter]);
            }
            commandInfo.Arguments = arguments;
            commands.Add(commandInfo);

            return commands;
        }
    }
}
