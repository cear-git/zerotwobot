using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.VoiceNext;

namespace ZeroTwoBot
{
    public class zeroTwoCommandsUngroupped : BaseCommandModule
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

        [Command("close"), Description("closes bot")]
        public async Task closeBot(CommandContext ctx)
        {
            var voiceNext = ctx.Client.GetVoiceNext();
            var vnc = voiceNext.GetConnection(ctx.Guild);
            if (vnc != null)
            {
                vnc.Disconnect();
            }
            await ctx.RespondAsync("Disconnecting");
            await Program.instance.bot.DisconnectAsync();
            Console.WriteLine("bot disconnected");
        }

        public static Dictionary<DiscordMember, int> winCount = new Dictionary<DiscordMember, int>();
        [Command("rps"), Description("rps test")]
        public async Task rpsTest(CommandContext ctx, [Description("ur choice")] string argOne)
        {
            await ctx.TriggerTypingAsync();
            string[] responseArray = { "rock", "paper", "scissors", };


            int rpsConvert(string converter) //convert their response to int for win/loss calc
            {
                int convertedInt = 0;
                switch (converter)
                {
                    case "rock":
                        convertedInt = 0;
                        break;
                    case "paper":
                        convertedInt = 1;
                        break;
                    case "scissors":
                        convertedInt = 2;
                        break;
                }
                return convertedInt;
            }


            string rpsHandler(string userResponse) //main handler
            {
                //0->rock1->paper2->scissors
                int responseInt = new Random().Next(0, 2); //make a random choice for bot
                int userInt = rpsConvert(userResponse); //change their response to an int
                bool userWin = false;
                bool tie = false;
                switch (userInt) //check their choice
                {
                    case 0: //user rock
                        switch (responseInt) //check bot choice
                        {
                            case 0: //tie
                                tie = true;
                                break;
                            case 1: //lose
                                userWin = false;
                                break;
                            case 2: //win
                                userWin = true;
                                break;
                        }
                        break;
                    case 1: //user paper
                        switch (responseInt)
                        {
                            case 0: //win
                                userWin = true;
                                break;
                            case 1:
                                tie = true;
                                break;
                            case 2: //loss
                                userWin = false;
                                break;
                        }
                        break;
                    case 2: //user scissors
                        switch (responseInt)
                        {
                            case 0: //loss
                                userWin = false;
                                break;
                            case 1: //win
                                userWin = true;
                                break;
                            case 2: 
                                tie = true;
                                break;
                        }
                        break;
                }
                return tie ? "Tie." : userWin ? "You win." : "You lose."; //return the string
            }

            string response = responseArray.Any(x => argOne.ToLower().Contains(x)) ? rpsHandler(argOne) : "Please use a correct arg (scissors, rock, or paper)";
            await ctx.RespondAsync(response);
        }

    }

    public class zeroTwoCommandsVoice : BaseCommandModule
    {
        [Command("join"), Description("joins channel")]
        public async Task Join(CommandContext ctx, DiscordChannel channel = null)
        {
            var voiceNext = ctx.Client.GetVoiceNext();
            if (voiceNext == null)
            {
                await ctx.RespondAsync("Voice is not enabled for this bot.");
                return;
            }
            var voiceConnection = voiceNext.GetConnection(ctx.Guild);
            if (voiceConnection != null)
            {
                await ctx.RespondAsync("Already connected to a channel.");
                return;
            }

            var vstat = ctx.Member?.VoiceState;
            if (vstat?.Channel == null && channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            if (channel == null)
                channel = vstat.Channel;

            voiceConnection = await voiceNext.ConnectAsync(channel);
            await ctx.RespondAsync($"Connected to `{channel.Name}");
        }

        [Command("leave"), Description("Leaves a voice channel.")]
        public async Task Leave(CommandContext ctx)
        {
            // check whether VNext is enabled
            var voiceNext = ctx.Client.GetVoiceNext();
            if (voiceNext == null)
            {
                // not enabled
                await ctx.RespondAsync("VNext is not enabled or configured.");
                return;
            }

            // check whether we are connected
            var voiceConnection = voiceNext.GetConnection(ctx.Guild);
            if (voiceConnection == null)
            {
                // not connected
                await ctx.RespondAsync("Not connected in this guild.");
                return;
            }

            string channel = voiceConnection.Channel.Name;
            // disconnect
            voiceConnection.Disconnect();
            await ctx.RespondAsync($"Disconnected from `{channel}`");
        }

        [Command("play"), Description("Plays an audio file.")]
        public async Task Play(CommandContext ctx, [RemainingText, Description("Full path to the file to play.")] string filename)
        {
            // check whether VNext is enabled
            var vnext = ctx.Client.GetVoiceNext();
            if (vnext == null)
            {
                // not enabled
                await ctx.RespondAsync("Voice is not enabled or configured.");
                return;
            }

            // check whether we aren't already connected
            var vnc = vnext.GetConnection(ctx.Guild);
            if (vnc == null)
            {
                // already connected
                await ctx.RespondAsync("I'm not connected to any channel.");
                return;
            }

            // check if file exists
            if (!File.Exists(filename))
            {
                // file does not exist
                await ctx.RespondAsync($"File `{filename}` does not exist.");
                return;
            }

            // wait for current playback to finish
            while (vnc.IsPlaying)
                await vnc.WaitForPlaybackFinishAsync();

            // play
            Exception exc = null;
            await ctx.Message.RespondAsync($"Playing `{filename}`");
            await vnc.SendSpeakingAsync(true);
            try
            {
                // borrowed from
                // https://github.com/RogueException/Discord.Net/blob/5ade1e387bb8ea808a9d858328e2d3db23fe0663/docs/guides/voice/samples/audio_create_ffmpeg.cs

                var ffmpeg_inf = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{filename}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                var ffmpeg = Process.Start(ffmpeg_inf);
                var ffout = ffmpeg.StandardOutput.BaseStream;

                VoiceTransmitStream vStream = vnc.GetTransmitStream(20);
                // let's buffer ffmpeg output
                using (var ms = new MemoryStream())
                {
                    await ffout.CopyToAsync(ms);
                    ms.Position = 0;

                    var buff = new byte[3840]; // buffer to hold the PCM data
                    var br = 0;
                    while ((br = ms.Read(buff, 0, buff.Length)) > 0)
                    {
                        if (br < buff.Length) // it's possible we got less than expected, let's null the remaining part of the buffer
                            for (var i = br; i < buff.Length; i++)
                                buff[i] = 0;

                        //await vnc.SendAsync(buff, 20); // we're sending 20ms of data
                        await vStream.WriteAsync(buff);
                    }
                }
            }
            catch (Exception ex) { exc = ex; }
            finally
            {
                await vnc.SendSpeakingAsync(false);
            }

            if (exc != null)
                await ctx.RespondAsync($"An exception occured during playback: `{exc.GetType()}: {exc.Message}`");
        }

    }

   /* //this is for interactive commands such as polls 
    public class zeroTwoCommandsInteractive : BaseCommandModule
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
    }*/
}
