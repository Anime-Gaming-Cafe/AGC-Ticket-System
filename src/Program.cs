using AGC_Ticket;
using AGC_Ticket.Services.DatabaseHandler;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Exceptions;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog;
using System.Reflection;

internal class Program : BaseCommandModule
{
    private static void Main(string[] args)
    {
        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        var logger = Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        logger.Information("Starting AGC-TicketSystem...");
        string DcApiToken = "";
        try
        {
            DcApiToken = BotConfig.GetConfig()["MainConfig"]["Discord_API_Token"];
        }
        catch
        {
            Console.WriteLine(
                "Der Discord API Token konnte nicht geladen werden.");
            Console.WriteLine("Drücke eine beliebige Taste um das Programm zu beenden.");
            Console.ReadKey();
            Environment.Exit(0);
        }


        var serviceProvider = new ServiceCollection()
            .AddLogging(lb => lb.AddSerilog())

            .BuildServiceProvider();



        DatabaseService.OpenConnection();
        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = DcApiToken,
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            MinimumLogLevel = LogLevel.Debug,
            Intents = DiscordIntents.All,
            LogTimestampFormat = "MMM dd yyyy - HH:mm:ss tt",
            DeveloperUserId = ulong.Parse(BotConfig.GetConfig()["MainConfig"]["BotOwnerId"]),
            Locale = "de",
            ServiceProvider = serviceProvider
        });
        discord.RegisterEventHandlers(Assembly.GetExecutingAssembly());
        var commands = discord.UseCommandsNext(new CommandsNextConfiguration
        {
            PrefixResolver = GetPrefix,
            EnableDms = false,
            EnableMentionPrefix = false,
            IgnoreExtraArguments = true,
            EnableDefaultHelp = false
        });
        discord.ClientErrored += Discord_ClientErrored;
        discord.UseInteractivity(new InteractivityConfiguration
        {
            Timeout = TimeSpan.FromMinutes(2)
        });
        commands.RegisterCommands(Assembly.GetExecutingAssembly());
        commands.CommandErrored += Commands_CommandErrored;
        await discord.ConnectAsync();

        await StartTasks(discord);
        await Task.Delay(-1);
    }

    private static Task StartTasks(DiscordClient discord)
    {
        UpdatePresence(discord);
        return Task.CompletedTask;
    }


    private static Task<int> GetPrefix(DiscordMessage message)
    {
        return Task.Run(() =>
        {
            string prefix;
            try
            {
                prefix = BotConfig.GetConfig()["MainConfig"]["BotPrefix"];
            }
            catch
            {
                prefix = "!!!"; //Fallback Config
            }


            int CommandStart = -1;
            CommandStart = message.GetStringPrefixLength(prefix);
            return CommandStart;
        });
    }

    private static async Task UpdatePresence(DiscordClient client)
    {
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            while (true)
            {
                int openTickets = 0;
                int closedTickets = 0;
                var con = DatabaseService.GetConnection();
                string query = "SELECT COUNT(*) FROM ticketstore where closed = False";
                await using NpgsqlCommand cmd = new(query, con);
                openTickets = Convert.ToInt32(cmd.ExecuteScalar());

                string query1 = "SELECT COUNT(*) FROM ticketstore where closed = True";
                await using NpgsqlCommand cmd1 = new(query1, con);
                closedTickets = Convert.ToInt32(cmd1.ExecuteScalar());

                await client.UpdateStatusAsync(new DiscordActivity(name: $"Tickets: Offen: {openTickets} | Gesamt: {openTickets + closedTickets}", type: ActivityType.Custom));
                await Task.Delay(TimeSpan.FromMinutes(2));
            }
        }
    }


    private static Task Discord_ClientErrored(DiscordClient sender, ClientErrorEventArgs e)
    {
        sender.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}");
        sender.Logger.LogError($"Stacktrace: {e.Exception.GetType()}: {e.Exception.StackTrace}");
        return Task.CompletedTask;
    }

    private static Task Commands_CommandErrored(CommandsNextExtension cn, CommandErrorEventArgs e)
    {
        // check if error is DisCatSharp.CommandsNext.Exceptions.CommandNotFoundException
        if (e.Exception is CommandNotFoundException)
            return Task.CompletedTask;
        cn.Client.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}");
        cn.Client.Logger.LogError($"Exception occured: {e.Exception.GetType()}: {e.Exception.StackTrace}");

        return Task.CompletedTask;
    }
}

public static class GlobalProperties
{
    // Server Staffrole ID
    public static ulong StaffRoleId { get; } = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["StaffRoleId"]);

    private static bool ParseBoolean(string boolString)
    {
        if (bool.TryParse(boolString, out bool parsedBool))
            return parsedBool;
        return false;
    }
}