using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

namespace ZeroTwoBot
{
    public class zeroTwoCommandsUngroupped
    {
        [Command("ping")] // let's define this method as a command
        [Description("Example ping command")] // this will be displayed to tell users what this command does when they invoke help
        [Aliases("pong")] // alternative names for the command
        public async Task Ping(CommandContext ctx) // this command takes no arguments
        {
            //trigger a typing indicator to let users know 
            await ctx.TriggerTypingAsync();

            var emoji = DiscordEmoji.FromName(ctx.Client, ":ping_pong:");

            // respond with ping
            await ctx.RespondAsync($"{emoji} Pong! Ping: {ctx.Client.Ping}ms");
        }

        [Command("greet"), Description("Says hi to specified user."), Aliases("sayhi", "say_hi")]
        public async Task Greet(CommandContext ctx, [Description("The user to say hi to.")] DiscordMember member) // this command takes a member as an argument; you can pass one by username, nickname, id, or mention
        {
            await ctx.TriggerTypingAsync();

            var emoji = DiscordEmoji.FromName(ctx.Client, ":wave:");

            //respond and greet the user.
            await ctx.RespondAsync($"{emoji} Hello, {member.Mention}!");
        }

        [Command("sum"), Description("Sums all given numbers and returns said sum.")]
        public async Task SumOfNumbers(CommandContext ctx, [Description("Integers to sum.")] params int[] args)
        {
            await ctx.TriggerTypingAsync();

            // calculate the sum
            var sum = args.Sum();

            // and send it to the user
            await ctx.RespondAsync($"The sum of these numbers is {sum.ToString("#,##0")}");
        }

        // math example command
        [Command("math"), Description("Does basic math.")]
        public async Task Math(CommandContext ctx, [Description("Operation to perform on the operands.")] MathOperation operation, [Description("First operand.")] double num1, [Description("Second operand.")] double num2)
        {
            var result = 0.0;
            switch (operation)
            {
                case MathOperation.Add:
                    result = num1 + num2;
                    break;

                case MathOperation.Subtract:
                    result = num1 - num2;
                    break;

                case MathOperation.Multiply:
                    result = num1 * num2;
                    break;

                case MathOperation.Divide:
                    result = num1 / num2;
                    break;

                case MathOperation.Percent:
                    result = num1 % num2;
                    break;
            }
            
            await ctx.RespondAsync($"The result is {result.ToString("#,##0.00")}");
        }
    }

    [Group("admin")] //mark this class as a command group
    [Description("Administrative commands.")] // give it a description for help purposes
    [Hidden] //hides this from users
    [RequirePermissions(Permissions.ManageGuild)] //restrict this to users who have appropriate permissions
    public class zeroTwoCommandsGroupped
    {
        // all the commands will need to be executed as <prefix>admin <command> <arguments>

        // this command will be only executable by the bot's owner
        [Command("sudo"), Description("Executes a command as another user."), Hidden, RequireOwner]
        public async Task Sudo(CommandContext ctx, [Description("Member to execute as.")] DiscordMember member, [RemainingText, Description("Command text to execute.")] string command)
        {
            await ctx.TriggerTypingAsync();

            // get the command service
            var cmds = ctx.CommandsNext;

            // perform the sudo
            await cmds.SudoAsync(member, ctx.Channel, command);
        }

        [Command("nick"), Description("Gives someone a new nickname."), RequirePermissions(Permissions.ManageNicknames)]
        public async Task ChangeNickname(CommandContext ctx, [Description("Member to change the nickname for.")] DiscordMember member, [RemainingText, Description("The nickname to give to that user.")] string new_nickname)
        {
            await ctx.TriggerTypingAsync();

            try
            {
                // let's change the nickname, and tell the 
                // audit logs who did it.
                await member.ModifyAsync(new_nickname, reason: $"Changed by {ctx.User.Username} ({ctx.User.Id}).");

                
                var emoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                await ctx.Message.CreateReactionAsync(emoji);
            }
            catch (Exception)
            {
                // oh no, something failed, let the invoker now
                var emoji = DiscordEmoji.FromName(ctx.Client, ":-1:");
                await ctx.Message.CreateReactionAsync(emoji);
            }
        }
    }

    //this is for interactive commands such as polls 
    public class zeroTwoCommandsInteractive
    {
        [Command("poll"), Description("Run a poll w/ reactions")]
        public async Task Poll(CommandContext ctx, [Description("How long will the poll last")] TimeSpan duration, [Description("What options should people have")] params DiscordEmoji[] options)
        {
            // retrieve the interactivity module from the client
            var interactivity = ctx.Client.GetInteractivityModule();
            var poll_options = options.Select(xe => xe.ToString());

            // present the poll
            var embed = new DiscordEmbedBuilder
            {
                Title = "Poll time!",
                Description = string.Join(" ", poll_options)
            };
            var msg = await ctx.RespondAsync(embed: embed);

            // add the options as reactions
            for (var i = 0; i < options.Length; i++)
                await msg.CreateReactionAsync(options[i]);

            // collect and filter responses
            var poll_result = await interactivity.CollectReactionsAsync(msg, duration);
            var results = poll_result.Reactions.Where(xkvp => options.Contains(xkvp.Key))
                .Select(xkvp => $"{xkvp.Key}: {xkvp.Value}");

            // post the results
            await ctx.RespondAsync(string.Join("\n", results));
        }

        [Command("waitforreact"), Description("Waits for a reaction")]
        public async Task WaitForReaction(CommandContext ctx)
        {
            //retreve interactivity module
            var interactivity = ctx.Client.GetInteractivityModule();

            //specify emoji
            DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, ":heart:");

            await ctx.Message.CreateReactionAsync(emoji);
            await ctx.RespondAsync($"React with {emoji} to win.");

            //wait for the reaction
            var em = await interactivity.WaitForReactionAsync(xe => xe == emoji, ctx.User, TimeSpan.FromSeconds(15));
            if (em != null)
            {
                //they win
                var embed = new DiscordEmbedBuilder
                {
                    Color = em.Message.Author is DiscordMember m ? m.Color : new DiscordColor(0xFF00FF),
                    Description = $"{em.Message.Author.Mention} wins",
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = em.Message.Author is DiscordMember mx ? mx.DisplayName : em.Message.Author.Username,
                        IconUrl = em.Message.Author.AvatarUrl
                    }
                };
                await ctx.RespondAsync(embed: embed);
            }
            else
            {
                await ctx.RespondAsync("No one wins");
            }
        }
    }
}
