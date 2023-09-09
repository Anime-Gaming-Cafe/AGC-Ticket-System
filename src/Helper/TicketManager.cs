using AGC_Ticket.Services.DatabaseHandler;
using DisCatSharp.Entities;
using Npgsql;
using System.Net.Sockets;
using DisCatSharp.Enums;
using AGC_Ticket;
using DisCatSharp;

namespace AGC_Ticket_System.Helper;

public enum TicketType
{
    Support,
    Report
}

public class TicketManagerHelper
{
    private static readonly Random random = new Random();
    public static async Task<int> GetPreviousTicketCount(TicketType ticketType)
    {
        var con = DatabaseService.GetConnection();
        string query = $"SELECT COUNT(*) FROM ticketstore where tickettype = '{ticketType.ToString().ToLower()}'";
        await using NpgsqlCommand cmd = new (query, con);
        int rowCount = Convert.ToInt32(cmd.ExecuteScalar());
        return rowCount;
    }

    public static string GenerateTicketID(int length = 9)
    {
        const string chars = "0123456789abcdef";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static async Task<bool> CheckForOpenTicket(long user_id)
    {
        bool isTicketOpen = false;
        var con = DatabaseService.GetConnection();
        string query = $"SELECT COUNT(*) FROM ticketstore where ticket_owner = '{user_id}' AND closed = False";
        await using NpgsqlCommand cmd = new(query, con);
        int rowCount = Convert.ToInt32(cmd.ExecuteScalar());
        if (rowCount > 0)
        {
            isTicketOpen = true;
        }
        return isTicketOpen;
    }

    public static async Task<long> GetOpenTicketChannel(long user_id)
    {
        long channel_id = 0;
        var con = DatabaseService.GetConnection();
        string query = $"SELECT tchannel_id FROM ticketcache where ticket_owner = '{user_id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (reader.Read())
        {
            channel_id = reader.GetInt64(0);
        }
        
        return channel_id;
    }
}


public static class TicketManager
{
    public static async Task OpenTicket(DiscordInteraction interaction, TicketType ticketType, DiscordClient client)
    {
        long memberid = (long)interaction.User.Id;
        long guildid = (long)interaction.Guild.Id;
        string ticketid = TicketManagerHelper.GenerateTicketID();
        bool existing_ticket = await TicketManagerHelper.CheckForOpenTicket(memberid);
        if (existing_ticket) 
        {
            long tchannelId = await TicketManagerHelper.GetOpenTicketChannel(memberid);
            var tbutton = new DiscordLinkButtonComponent("https://discord.com/channels/" + guildid + "/" + tchannelId, "Zum Ticket");
            var eb = new DiscordEmbedBuilder
            {
                Title = "Fehler | Bereits ein Ticket geöffnet!",
                Description = $"Du hast bereits ein geöffnetes Ticket! -> <#{await TicketManagerHelper.GetOpenTicketChannel((long)interaction.User.Id)}>",
                Color = DiscordColor.Red
            };
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(eb).AddComponents(tbutton).AsEphemeral());
            return;
        }
        var cre_emb = new DiscordEmbedBuilder
        {
            Title = "Ticket erstellen",
            Description = $"Du hast ein Ticket erstellt! Bitte warte einen Augenblick...",
            Color = DiscordColor.Blurple
        };
        await interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(cre_emb).AsEphemeral());
        int ticket_number = await TicketManagerHelper.GetPreviousTicketCount(ticketType) + 1;
        DiscordChannel Ticket_category = interaction.Guild.GetChannel(ulong.Parse(BotConfig.GetConfig()["SupportConfig"]["SupportCategoryId"]));
        DiscordChannel? ticket_channel = null;
        if (ticketType == TicketType.Report)
        {
            await using var con = DatabaseService.GetConnection();
            await using NpgsqlCommand cmd = new($"INSERT INTO ticketstore (ticket_id, ticket_owner, tickettype, closed) VALUES ('{ticketid}', '{memberid}', '{ticketType.ToString().ToLower()}', False)", con);
            await cmd.ExecuteNonQueryAsync();

            ticket_channel = await interaction.Guild.CreateChannelAsync($"report-{ticket_number}", ChannelType.Text, Ticket_category, $"Ticket erstellt von {interaction.User.UsernameWithDiscriminator}");
           
            await using NpgsqlCommand cmd2 = new($"INSERT INTO ticketcache (ticket_id, ticket_owner, tchannel_id) VALUES ('{ticketid}', '{memberid}', '{ticket_channel.Id}')", con);
            await cmd2.ExecuteNonQueryAsync();
        }
        else if (ticketType == TicketType.Support)
        {
            await using var con = DatabaseService.GetConnection();
            await using NpgsqlCommand cmd = new($"INSERT INTO ticketstore (ticket_id, ticket_owner, tickettype, closed) VALUES ('{ticketid}', '{memberid}', '{ticketType.ToString().ToLower()}', False)", con);
            await cmd.ExecuteNonQueryAsync();

            ticket_channel = await interaction.Guild.CreateChannelAsync($"support-{ticket_number}", ChannelType.Text, Ticket_category, $"Ticket erstellt von {interaction.User.UsernameWithDiscriminator}");

            await using NpgsqlCommand cmd2 = new($"INSERT INTO ticketcache (ticket_id, ticket_owner, tchannel_id) VALUES ('{ticketid}', '{memberid}', '{ticket_channel.Id}')", con);
            await cmd2.ExecuteNonQueryAsync();
        }
        var button = new DiscordLinkButtonComponent("https://discord.com/channels/" + guildid + "/" + ticket_channel.Id, "Zum Ticket");
        var teb = new DiscordEmbedBuilder
        {
            Title = "Ticket erstellt",
            Description = $"Dein Ticket wurde erfolgreich erstellt! -> <#{ticket_channel.Id}>",
            Color = DiscordColor.Green,
        };
        await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(teb).AddComponents(button));
    }    
}