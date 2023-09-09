using AGC_Ticket;
using AGC_Ticket.Services.DatabaseHandler;
using AGC_Ticket_System.Components;
using AGC_Ticket_System.Enums;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using Npgsql;

namespace AGC_Ticket_System.Helper;





public class TicketManagerHelper
{
    private static readonly Random random = new Random();
    public static async Task<int> GetPreviousTicketCount(TicketType ticketType)
    {
        var con = DatabaseService.GetConnection();
        string query = $"SELECT COUNT(*) FROM ticketstore where tickettype = '{ticketType.ToString().ToLower()}'";
        await using NpgsqlCommand cmd = new(query, con);
        int rowCount = Convert.ToInt32(cmd.ExecuteScalar());
        return rowCount;
    }

    public static async Task<int> GetTicketCountFromThisUser(long user_id)
    {
        var con = DatabaseService.GetConnection();
        string query = $"SELECT COUNT(*) FROM ticketstore where ticket_owner = '{user_id}'";
        await using NpgsqlCommand cmd = new(query, con);
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

    public static async Task Claim_UpdateHeaderComponents(ComponentInteractionCreateEventArgs interaction)
    {
        await interaction.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        var message = await interaction.Channel.GetMessageAsync((ulong)interaction.Message.Id);
        var mb = new DiscordMessageBuilder();
        mb.WithContent(message.Content);
        mb.WithEmbed(message.Embeds[0]);
        var components = TicketComponents.GetTicketClaimedActionRow();
        List<DiscordActionRowComponent> row = new()
        {
            new DiscordActionRowComponent(components)
        };
        mb.AddComponents(row);
        await message.ModifyAsync(mb);
    }

    public static async Task<string> GetTicketIdFromChannel(DiscordChannel channel)
    {
        var con = DatabaseService.GetConnection();
        string query = $"SELECT ticket_id FROM ticketcache where tchannel_id = '{channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        string ticket_id = "";
        while (reader.Read())
        {
            ticket_id = reader.GetString(0);
        }
        return ticket_id;
    }

    public static async Task InsertHeaderIntoTicket(DiscordInteraction interaction, DiscordChannel tchannel, TicketCreator ticketCreator, TicketType ticketType)
    {
        if (ticketType == TicketType.Report)
        {
            if (ticketCreator == TicketCreator.User)
            {
                Console.WriteLine(0);
                var ticket_channel = tchannel;
                int prev_tickets = await TicketManagerHelper.GetTicketCountFromThisUser((long)interaction.User.Id);
                string pingstring = $"{interaction.User.Mention} | <@{BotConfig.GetConfig()["SupportConfig"]["TeamRoleId"]}>";
                var eb = new DiscordEmbedBuilder()
                    .WithAuthor(interaction.User.UsernameWithDiscriminator, interaction.User.AvatarUrl).WithColor(DiscordColor.Blurple).WithFooter($"Nutzer-ID: {interaction.User.Id} • Ticket-ID: {await GetTicketIdFromChannel(tchannel)}")
                    .WithDescription($"**Ticket-Typ: Report-Ticket**");
                var mb = new DiscordMessageBuilder();
                mb.WithContent(pingstring);
                mb.WithEmbed(eb.Build());
                var rowComponents = TicketComponents.GetTicketActionRow();
                List<DiscordActionRowComponent> row = new()
                {
                    new DiscordActionRowComponent(rowComponents)
                };

                mb.AddComponents(row);
                await ticket_channel.SendMessageAsync(mb);
                return;
            }
        }
        else if (ticketType == TicketType.Support)
        {
            if (ticketCreator == TicketCreator.User)
            {
                Console.WriteLine(0);
                var ticket_channel = tchannel;
                int prev_tickets = await TicketManagerHelper.GetTicketCountFromThisUser((long)interaction.User.Id);
                string pingstring = $"{interaction.User.Mention} | <@{BotConfig.GetConfig()["SupportConfig"]["TeamRoleId"]}>";
                var eb = new DiscordEmbedBuilder()
                    .WithAuthor(interaction.User.UsernameWithDiscriminator, interaction.User.AvatarUrl).WithColor(DiscordColor.Blurple).WithFooter($"Nutzer-ID: {interaction.User.Id} • Ticket-ID: {await GetTicketIdFromChannel(tchannel)}")
                    .WithDescription($"**Ticket-Typ: Support-Ticket**");
                var mb = new DiscordMessageBuilder();
                mb.WithContent(pingstring);
                mb.WithEmbed(eb.Build());
                var rowComponents = TicketComponents.GetTicketActionRow();
                List<DiscordActionRowComponent> row = new()
                {
                    new DiscordActionRowComponent(rowComponents)
                };

                mb.AddComponents(row);
                await ticket_channel.SendMessageAsync(mb);
                return;
            }
        }
    }

    public static async Task SendUserNotice(DiscordInteraction interaction, DiscordChannel ticket_channel, TicketType ticketType)
    {
        if (ticketType == TicketType.Report)
        {
            ;
            int prev_tickets = await TicketManagerHelper.GetTicketCountFromThisUser((long)interaction.User.Id);
            var eb = new DiscordEmbedBuilder()
                .WithAuthor(interaction.User.UsernameWithDiscriminator, interaction.User.AvatarUrl).WithColor(DiscordColor.Blurple).WithFooter($"AGC-Support-System")
                .WithDescription($"Hey! Danke fürs öffnen eines Report-Tickets. Ein Teammitglied wird sich gleich um dein Anliegen kümmern. Bitte teile uns in der Zeit alle nötigen Infos mit.\n" +
                               $"1. Um wen geht es (User-ID oder User-Name)\n" +
                                              $"2. Was ist vorgefallen (Bitte versuche die Situation so ausführlich wie möglich zu beschreiben)" +
                                                             $"");
            await ticket_channel.SendMessageAsync(embed: eb);
            return;
        }
    }

    public static async Task ClaimTicket(ComponentInteractionCreateEventArgs interaction)
    {
        var teamler = AGC_Ticket.Helpers.TeamChecker.IsSupporter(await interaction.User.ConvertToMember(interaction.Guild));
        if (!teamler)
        {
            await interaction.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }
        await Claim_UpdateHeaderComponents(interaction);
        var con = DatabaseService.GetConnection();
        string query = $"SELECT ticket_id FROM ticketcache where claimed = False AND tchannel_id = '{(long)interaction.Interaction.ChannelId}'";

        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        string ticket_id = "";
        while (await reader.ReadAsync())
        {
            ticket_id = reader.GetString(0);
        }
        await reader.CloseAsync();
        Console.WriteLine(ticket_id);
        await using NpgsqlCommand cmd2 = new($"UPDATE ticketcache SET claimed = True WHERE ticket_id = '{ticket_id}'", con);
        await cmd2.ExecuteNonQueryAsync();

        await using NpgsqlCommand cmd3 = new($"UPDATE ticketcache SET claimed_from = '{(long)interaction.User.Id}' WHERE tchannel_id = '{(long)interaction.Interaction.ChannelId}'", con);
        await cmd3.ExecuteNonQueryAsync();
    }



    public static async Task AddUserToTicket(DiscordInteraction interaction, DiscordChannel ticket_channel, DiscordUser user, bool addedAfter = false)
    {
        var con = DatabaseService.GetConnection();
        string query = $"SELECT ticket_id FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        string ticket_id = "";
        while (reader.Read())
        {
            ticket_id = reader.GetString(0);

        }
        await reader.CloseAsync();
        await using NpgsqlCommand cmd2 = new($"UPDATE ticketcache SET ticket_users = array_append(ticket_users, '{(long)user.Id}') WHERE ticket_id = '{ticket_id}'", con);
        await cmd2.ExecuteNonQueryAsync();
        // add perms 
        var channel = ticket_channel;
        var member = await interaction.Guild.GetMemberAsync(user.Id);
        await channel.AddOverwriteAsync(member, Permissions.AccessChannels | Permissions.SendMessages | Permissions.AddReactions | Permissions.AttachFiles | Permissions.EmbedLinks);
        if (addedAfter)
        {
            var afteraddembed = new DiscordEmbedBuilder
            {
                Title = "User hinzugefügt",
                Description = $"Der User {user.Mention} ``{user.Id}`` wurde zum Ticket hinzugefügt!",
                Color = DiscordColor.Green
            };
            await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(afteraddembed));
        }
    }
    public static async Task RemoveUserFromTicket(DiscordInteraction interaction, DiscordChannel ticket_channel, DiscordUser user, bool noautomatic = false)
    {
        var con = DatabaseService.GetConnection();
        string query = $"SELECT ticket_id FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        string ticket_id = "";
        while (reader.Read())
        {
            ticket_id = reader.GetString(0);

        }
        await reader.CloseAsync();
        await using NpgsqlCommand cmd2 = new($"UPDATE ticketcache SET ticket_users = array_remove(ticket_users, '{(long)user.Id}') WHERE ticket_id = '{ticket_id}'", con);
        await cmd2.ExecuteNonQueryAsync();
        var channel = ticket_channel;
        var member = await interaction.Guild.GetMemberAsync(user.Id);
        await channel.AddOverwriteAsync(member, Permissions.None);
        if (noautomatic)
        {
            var afteraddembed = new DiscordEmbedBuilder
            {
                Title = "User entfernt",
                Description = $"Der User {user.Mention} ``{user.Id}`` wurde vom Ticket entfernt!",
                Color = DiscordColor.Green
            };
            await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(afteraddembed));

            // TODO: Send transcript to user

            var userembed = new DiscordEmbedBuilder
            {
                Title = "Ticket geschlossen",
                Description = $"Du wurdest aus dem Ticket ``{ticket_channel.Name}`` entfernt!",
                Color = DiscordColor.Green
            };
        }
    }
}

[EventHandler]
public class TicketManagerEventHandler : BaseCommandModule
{
    [Event]
    public async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            if (e.Interaction.Data.CustomId == "ticket_claim")
            {
                await TicketManagerHelper.ClaimTicket(e);
            }
            else if (e.Interaction.Data.CustomId == "ticket_close")
            {
                await TicketManager.CloseTicket(e, e.Channel);
            }



            return Task.CompletedTask;
        });

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
            var con = DatabaseService.GetConnection();
            await using NpgsqlCommand cmd = new($"INSERT INTO ticketstore (ticket_id, ticket_owner, tickettype, closed) VALUES ('{ticketid}', '{memberid}', '{ticketType.ToString().ToLower()}', False)", con);
            await cmd.ExecuteNonQueryAsync();

            ticket_channel = await interaction.Guild.CreateChannelAsync($"report-{ticket_number}", ChannelType.Text, Ticket_category, $"Ticket erstellt von {interaction.User.UsernameWithDiscriminator}");

            await using NpgsqlCommand cmd2 = new($"INSERT INTO ticketcache (ticket_id, ticket_owner, tchannel_id, claimed) VALUES ('{ticketid}', '{memberid}', '{ticket_channel.Id}', False)", con);
            await cmd2.ExecuteNonQueryAsync();
            await Task.Delay(TimeSpan.FromSeconds(2));
            await TicketManagerHelper.AddUserToTicket(interaction, ticket_channel, interaction.User);
            await TicketManagerHelper.InsertHeaderIntoTicket(interaction, ticket_channel, TicketCreator.User, TicketType.Report);
        }
        else if (ticketType == TicketType.Support)
        {
            var con = DatabaseService.GetConnection();
            await using NpgsqlCommand cmd = new($"INSERT INTO ticketstore (ticket_id, ticket_owner, tickettype, closed) VALUES ('{ticketid}', '{memberid}', '{ticketType.ToString().ToLower()}', False)", con);
            await cmd.ExecuteNonQueryAsync();

            ticket_channel = await interaction.Guild.CreateChannelAsync($"support-{ticket_number}", ChannelType.Text, Ticket_category, $"Ticket erstellt von {interaction.User.UsernameWithDiscriminator}");

            await using NpgsqlCommand cmd2 = new($"INSERT INTO ticketcache (ticket_id, ticket_owner, tchannel_id, claimed) VALUES ('{ticketid}', '{memberid}', '{ticket_channel.Id}', False)", con);
            await cmd2.ExecuteNonQueryAsync();
            await Task.Delay(TimeSpan.FromSeconds(2));
            await TicketManagerHelper.AddUserToTicket(interaction, ticket_channel, interaction.User);
            await TicketManagerHelper.InsertHeaderIntoTicket(interaction, ticket_channel, TicketCreator.User, TicketType.Support);
        }
        var button = new DiscordLinkButtonComponent("https://discord.com/channels/" + guildid + "/" + ticket_channel.Id, "Zum Ticket");
        var teb = new DiscordEmbedBuilder
        {
            Title = "Ticket erstellt",
            Description = $"Dein Ticket wurde erfolgreich erstellt! -> <#{ticket_channel.Id}>",
            Color = DiscordColor.Green,
        };
        // inset header later
        await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(teb).AddComponents(button));
        await TicketManagerHelper.SendUserNotice(interaction, ticket_channel, ticketType);
    }

    public static async Task CloseTicket(ComponentInteractionCreateEventArgs interaction, DiscordChannel ticket_channel)
    {
        var teamler = AGC_Ticket.Helpers.TeamChecker.IsSupporter(await interaction.Interaction.User.ConvertToMember(interaction.Interaction.Guild));
        if (!teamler)
        {
            await interaction.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }

        // disable all buttons
        var message = await ticket_channel.GetMessageAsync((ulong)interaction.Message.Id);
        var umb = new DiscordMessageBuilder();
        umb.WithContent(message.Content);
        umb.WithEmbed(message.Embeds[0]);
        var components = TicketComponents.GetClosedTicketActionRow();
        List<DiscordActionRowComponent> row = new()
    {
        new DiscordActionRowComponent(components)
    };
        umb.AddComponents(row);
        await message.ModifyAsync(umb);

        var con = DatabaseService.GetConnection();
        string query = $"SELECT ticket_id FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        string ticket_id = "";
        while (reader.Read())
        {
            ticket_id = reader.GetString(0);
        }
        await reader.CloseAsync();

        await using NpgsqlCommand cmd2 = new($"UPDATE ticketstore SET closed = True WHERE ticket_id = '{ticket_id}'", con);
        await cmd2.ExecuteNonQueryAsync();

        var con2 = DatabaseService.GetConnection();
        string query2 = $"SELECT ticket_users FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd3 = new(query2, con2);
        await using NpgsqlDataReader reader2 = await cmd3.ExecuteReaderAsync();
        List<List<long>> ticket_usersList = new List<List<long>>();

        while (reader2.Read())
        {
            long[] ticketUsersArray = (long[])reader2.GetValue(0);
            List<long> ticket_users = new List<long>(ticketUsersArray);
            ticket_usersList.Add(ticket_users);
        }

        await reader2.CloseAsync();

        var button = new DiscordLinkButtonComponent("https://discord.com/channels/" + interaction.Guild.Id + "/" + interaction.Channel.Id, "Zum Ticket");

        var del_ticketbutton = new DiscordButtonComponent(ButtonStyle.Danger, $"ticket_delete", "Ticket löschen ❌");
        var teb = new DiscordEmbedBuilder
        {
            Title = "Ticket geschlossen",
            Description = $"Das Ticket wurde erfolgreich geschlossen!\n Geschlossen von {interaction.User.UsernameWithDiscriminator} ``{interaction.User.Id}``",
            Color = DiscordColor.Green,
        };

        await ticket_channel.ModifyAsync(x => x.Name = $"closed-{ticket_channel.Name}");
        var mb = new DiscordMessageBuilder();
        mb.WithContent(interaction.User.Mention);
        mb.WithEmbed(teb.Build());
        mb.AddComponents(del_ticketbutton);
        await interaction.Channel.SendMessageAsync(mb);

        foreach (var users in ticket_usersList)
        {
            foreach (var user in users)
            {
                var member = await interaction.Guild.GetMemberAsync((ulong)user);
                await TicketManagerHelper.RemoveUserFromTicket(interaction.Interaction, ticket_channel, member);
                await SendTranscriptsToUser(member, "sd");
            }
        }
    }

    public static async Task SendTranscriptsToUser(DiscordMember member, string TransscriptURL)
    {
        var eb = new DiscordEmbedBuilder().WithTitle("Transscript").WithDescription($"Hier ist dein Transscript: {TransscriptURL}").WithColor(DiscordColor.Blurple);
        try
        {
            await member.SendMessageAsync(embed: eb);
        }
        catch (Exception)
        {
            await Task.CompletedTask;
        }
    }

    public static async Task GenerateTransscript(DiscordChannel ticket_channel)
    {
        // TODO: Implement later
    }

}
