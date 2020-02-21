using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;

namespace ZeroTwoBot
{
    public class Program
    {
        public DiscordClient bot { get; set; }    
        public CommandsNextExtension Commands { get; set; }
        public VoiceNextExtension Voice { get; set; }

        public static Program instance = new Program();
        static void Main(string[] args)
        {
            //send it to the async task to make it asynchronous
            instance.startBotAsync().GetAwaiter().GetResult();
        }

        // this structure will hold data from config.json
        public struct ConfigJson {
            [JsonProperty("token")]
            public string Token { get; private set; }
            [JsonProperty("prefix")]
            public string CommandPrefix { get; private set; }
        }

        //these tasks will handle events in the bot
        private Task botReady(ReadyEventArgs e)
        {
            //log event occuring
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "Zero Two Bot", "Bot is ready", DateTime.Now);
            
            //method is not async, so return not await so it doesnt try to do more work than needed
            return Task.CompletedTask;
            
        }

        private Task botGuildAvailable(GuildCreateEventArgs e)
        {
            //log name of guild that was sent to bot
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "Zero Two Bot", $"Guild available: {e.Guild.Name}", DateTime.Now);

            //not async, so return not await
            return Task.CompletedTask;
        }

        //this is stupid and doesnt work correctly, need to log specifics of each entry, this isnt called anywhere
        private Task botGuildAuditLog(GuildCreateEventArgs e)
        {
            var auditList = e.Guild.GetAuditLogsAsync();
            string path = "auditLog.txt";
            string append = "";

            foreach (DiscordAuditLogEntry entries in auditList.Result)
            {
                append += $"User: {entries.UserResponsible} | Type: {entries.ActionType} | Reason: {entries.Reason}";
            }
            File.WriteAllText(path, append);
                     
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "Zero Two Bot", $"Audit Log for {e.Guild.Name} listed to {path}", DateTime.Now);

            return Task.CompletedTask;
        }


        private Task botErrorHandler(ClientErrorEventArgs e)
        {
            //log error that occured
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "Zero Two Bot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);

            //not async, return not await
            return Task.CompletedTask;
        }

        //command execution log
        private Task commandsExecuted(CommandExecutionEventArgs e)
        {
            //log name of command and the user
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "Zero Two Bot", $"{e.Context.User.Username} issued the command '{e.Command.QualifiedName}'", DateTime.Now);

            return Task.CompletedTask;
        }
        //command error help
        private async Task commandsError(CommandErrorEventArgs e)
        {
            //log error
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "Zero Two Bot", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);
      
            //check to see if error was lack of permissions
            if (e.Exception is ChecksFailedException ex)
            {
                //yes, lacked perms, let them know
                DiscordEmoji emoji = DiscordEmoji.FromName(e.Context.Client, ":x:");

                //wrap response in an embed
                var embed = new DiscordEmbedBuilder
                {
                    Title = "",
                    Description = $"{emoji} You do not have permission to use this command.",
                    Color = new DiscordColor(0xFF0000) // red

                };

                //respond
                await e.Context.RespondAsync("", embed: embed);
            }
        
        }       
        
        public async Task botStatusAsync(ReadyEventArgs e)
        {
            string[] statusArray = { "test", "test2", "test3" };
            Random rand = new Random();

            DiscordActivity active = new DiscordActivity();
            active.ActivityType = ActivityType.Streaming;
            active.Name = statusArray[rand.Next(0, 2)];
            active.StreamUrl = "https://twitch.tv/ceariusx";
            await this.bot.UpdateStatusAsync(active);
        }

        //main async task
        public async Task startBotAsync()
        {
            //loading configuration from json
            var json = "";
            using (var file = File.OpenRead("config.json"))
            using (var sr = new StreamReader(file, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            //load values from config
            var configjson = JsonConvert.DeserializeObject<ConfigJson>(json);
            var config = new DiscordConfiguration
            {
                Token = configjson.Token,
                TokenType = TokenType.Bot,

                AutoReconnect = true,
                LogLevel = LogLevel.Info,
                UseInternalLogHandler = true
            };

            //instantiate the bot
            this.bot = new DiscordClient(config);
          
            //hook events to the bot
            this.bot.Ready += this.botReady;
            this.bot.Ready += this.botStatusAsync;
            this.bot.GuildAvailable += this.botGuildAvailable;
            this.bot.ClientErrored += this.botErrorHandler;
            


            //command setup
            var commandConfig = new CommandsNextConfiguration
            {
                
                //use prefix from config
                StringPrefixes = new[] { configjson.CommandPrefix },          

                //enable mentioning the bot as a prefix
                EnableMentionPrefix = true
            };

            //hook to bot
            this.Commands = this.bot.UseCommandsNext(commandConfig);

            //hook command events for debugging
            this.Commands.CommandExecuted += this.commandsExecuted;
            this.Commands.CommandErrored += this.commandsError;

            //register commands
            this.Commands.RegisterCommands<zeroTwoCommandsUngroupped>();
            //this.Commands.RegisterCommands<zeroTwoCommandsInteractive>();
            this.Commands.RegisterCommands<zeroTwoCommandsVoice>();


            var voiceConfig = new VoiceNextConfiguration
            {
                AudioFormat = AudioFormat.Default
            };

            this.Voice = this.bot.UseVoiceNext(voiceConfig);

            //connect and log in
            await this.bot.ConnectAsync();

            //when bot is running, <prefix>help will show command list, <prefix>help <command> will show help abbout the command

            //this is to prevent premature quitting
            await Task.Delay(-1);

        }
    }
}
