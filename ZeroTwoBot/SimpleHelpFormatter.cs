using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;

namespace ZeroTwoBot
{
    //help formatters alter look of default help command, this replaces embed with simple text

    public class SimpleHelpFormatter : IHelpFormatter
    {
        private StringBuilder MessageBuilder { get; }
        public SimpleHelpFormatter()
        {
            this.MessageBuilder = new StringBuilder();
        }

        //this is called first, sets commands name, if no command is processed, wont be called
        public IHelpFormatter WithCommandName(string name)
        {
            this.MessageBuilder.Append("Command: ")
                .AppendLine(Formatter.Bold(name))
                .AppendLine();

            return this;
        }

        //method is called second, sets current commands description, if no command is processed, wont be called
        public IHelpFormatter WithDescription(string description)
        {
            this.MessageBuilder.Append("Description: ")
                .AppendLine(description)
                .AppendLine();

            return this;
        }

        //method is called third, used when currently processed group can be executed as a standalone command, otherwise not called
        public IHelpFormatter WithGroupExecutable()
        {
            this.MessageBuilder.AppendLine("This is a standalone command.")
                .AppendLine();

            return this;
        }

        //method is called fourth, sets the current commands aliases, if no command is processed, wont be called
        public IHelpFormatter WithAliases(IEnumerable<string> aliases)
        {
            this.MessageBuilder.Append("Aliases: ")
                .AppendLine(string.Join(", ", aliases))
                .AppendLine();

            return this;
        }

        //method is called fifth, sets the current commands args, if no command is processed, wont be called
        public IHelpFormatter WithArguments(IEnumerable<CommandArgument> arguments)
        {
            this.MessageBuilder.Append("Arguments: ")
                .AppendLine(string.Join(", ", arguments.Select(xarg => $"{xarg.Name} ({xarg.Type.ToUserFriendlyName()})")))
                .AppendLine();

            return this;
        }

        //method is called sixth, sets the current groups subcommands, if no group is being processed or command is not a group, wont be called
        public IHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            this.MessageBuilder.Append("Subcommands: ")
                .AppendLine(string.Join(", ", subcommands.Select(xc => xc.Name)))
                .AppendLine();

            return this;
        }

        //method is called last, produces final message and returns
        public CommandHelpMessage Build()
        {
            return new CommandHelpMessage(this.MessageBuilder.ToString().Replace("\r\n", "\n"));
        }

    }
}
