using AGC_Ticket;
using AGC_Ticket.Helpers;
using AGC_Ticket.Services.DatabaseHandler;
using AGC_Ticket_System.Components;
using AGC_Ticket_System.Enums;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity.Extensions;
using Microsoft.VisualBasic;
using Npgsql;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

public class TicketManagerHelper
{
    private static readonly Random random = new();

    public static async Task<long> GetTicketOwnerFromChannel(DiscordChannel channel)
    {
        var con = DatabaseService.GetConnection();
        string query = $"SELECT ticket_owner FROM ticketcache where tchannel_id = '{channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        long ticket_owner = 0;
        while (reader.Read())
        {
            ticket_owner = reader.GetInt64(0);
        }

        return ticket_owner;
    }

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
        var message = await interaction.Channel.GetMessageAsync(interaction.Message.Id);
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
    public static async Task InsertHeaderIntoTicket(CommandContext ctx, DiscordChannel tchannel, DiscordMember member)
    {
        string pingstring = $"{member.Mention} | {ctx.User.Mention}";
        var ticket_channel = tchannel;
        int prev_tickets = await GetTicketCountFromThisUser((long)ctx.User.Id);
        var eb = new DiscordEmbedBuilder()
            .WithAuthor(member.UsernameWithDiscriminator, ctx.User.AvatarUrl)
            .WithColor(DiscordColor.Blurple)
            .WithFooter(
                $"Nutzer-ID: {member.Id} • Ticket-ID: {await GetTicketIdFromChannel(tchannel)}")
            .WithDescription("**Ticket-Typ: Report-Ticket**");
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
    }

    public static async Task InsertHeaderIntoTicket(DiscordInteraction interaction, DiscordChannel tchannel,
        TicketCreator ticketCreator, TicketType ticketType)
    {
        string pingstring = $"{interaction.User.Mention} | <@&{BotConfig.GetConfig()["SupportConfig"]["TeamRoleId"]}>";
        if (ticketType == TicketType.Report)
        {
            if (ticketCreator == TicketCreator.User)
            {
                Console.WriteLine(0);
                var ticket_channel = tchannel;
                int prev_tickets = await GetTicketCountFromThisUser((long)interaction.User.Id);
                var eb = new DiscordEmbedBuilder()
                    .WithAuthor(interaction.User.UsernameWithDiscriminator, interaction.User.AvatarUrl)
                    .WithColor(DiscordColor.Blurple)
                    .WithFooter(
                        $"Nutzer-ID: {interaction.User.Id} • Ticket-ID: {await GetTicketIdFromChannel(tchannel)}")
                    .WithDescription("**Ticket-Typ: Report-Ticket**");
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
            }
        }
        else if (ticketType == TicketType.Support)
        {
            if (ticketCreator == TicketCreator.User)
            {
                var ticket_channel = tchannel;
                int prev_tickets = await GetTicketCountFromThisUser((long)interaction.User.Id);

                var eb = new DiscordEmbedBuilder()
                    .WithAuthor(interaction.User.UsernameWithDiscriminator, interaction.User.AvatarUrl)
                    .WithColor(DiscordColor.Blurple)
                    .WithFooter(
                        $"Nutzer-ID: {interaction.User.Id} • Ticket-ID: {await GetTicketIdFromChannel(tchannel)}")
                    .WithDescription("**Ticket-Typ: Support-Ticket**");
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
            }
        }
    }
    private static string GenerateAddionalNotes()
    {
        string additionalNotes = "";
        // check if its between 23 pm and 8 am

        var currentHour = DateTime.Now.Hour;
        if (currentHour >= 22 || currentHour <= 8)
        {
            additionalNotes = "\n\nAufgrund der Uhrzeit kann es zu Verzögerungen kommen";
        }
        // check if its christmas
        if (DateTime.Now.Month == 12)
        {
            additionalNotes = "\n\nAufgrund der Weihnachtszeit kann es zu Verzögerungen kommen.";
        }


        if (additionalNotes != "")
        {
            additionalNotes = "NOTE: " + additionalNotes;
        }

        if (additionalNotes != "")
        {
            additionalNotes = additionalNotes + " Danke für deine Geduld";
        }

        return additionalNotes;
    }

    public static async Task SendStaffNotice(CommandContext ctx, DiscordChannel ticket_channel, DiscordMember user)
    {
        var eb = new DiscordEmbedBuilder()
            .WithAuthor(ctx.User.UsernameWithDiscriminator, ctx.User.AvatarUrl)
            .WithColor(DiscordColor.Blurple).WithFooter("AGC-Support-System")
            .WithDescription(
                           $"Hey {user.Mention}. Ein Ticket wurde von {ctx.User.Mention} mit dir erstellt. Bitte warte ab, bis sich das Teammitglied bei dir meldet.");
        await ticket_channel.SendMessageAsync(eb);
    }

    public static async Task SendUserNotice(DiscordInteraction interaction, DiscordChannel ticket_channel,
        TicketType ticketType)
    {
        if (ticketType == TicketType.Report)
        {
            var eb = new DiscordEmbedBuilder()
                .WithAuthor(interaction.User.UsernameWithDiscriminator, interaction.User.AvatarUrl)
                .WithColor(DiscordColor.Blurple).WithFooter("AGC-Support-System")
                .WithDescription(
                    $"Hey! Danke fürs öffnen eines Report-Tickets. Ein Teammitglied wird sich gleich um dein Anliegen kümmern. Bitte teile uns in der Zeit alle nötigen Infos mit.\n" +
                    $"1. Um wen geht es (User-ID oder User-Name)\n" +
                    $"2. Was ist vorgefallen (Bitte versuche die Situation so ausführlich wie möglich zu beschreiben) {GenerateAddionalNotes()}");
            await ticket_channel.SendMessageAsync(eb);
        }
        else if (ticketType == TicketType.Support)
        {
            var eb = new DiscordEmbedBuilder()
                .WithAuthor(interaction.User.UsernameWithDiscriminator, interaction.User.AvatarUrl)
                .WithColor(DiscordColor.Blurple).WithFooter("AGC-Support-System")
                .WithDescription($"Hey! Danke fürs öffnen eines Support-Tickets. Ein Teammitglied wird sich gleich um dein Anliegen kümmern. Bitte teile uns in der Zeit alle nötigen Infos mit. {GenerateAddionalNotes()}");
        }
    }

    public static async Task DeleteTicket(ComponentInteractionCreateEventArgs interaction)
    {
        
        var teamler = TeamChecker.IsSupporter(await interaction.User.ConvertToMember(interaction.Guild));
        if (!teamler)
        {
            await interaction.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }
        await interaction.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);


        var del_ticketbutton =
            new DiscordButtonComponent(ButtonStyle.Danger, "ticket_delete", "Ticket löschen ❌", true);
        var imsg = await interaction.Channel.GetMessageAsync(interaction.Message.Id);
        var imsgmb = new DiscordMessageBuilder();
        imsgmb.WithContent(imsg.Content);
        imsgmb.WithEmbed(imsg.Embeds[0]);
        imsgmb.AddComponents(del_ticketbutton);
        await imsg.ModifyAsync(imsgmb);
        DiscordEmbed ebct = new DiscordEmbedBuilder()
            .WithTitle("Ticket wird gelöscht")
            .WithDescription(
                $"Löschen eingeleitet von {interaction.User.Mention} {interaction.User.UsernameWithDiscriminator} ``{interaction.User.Id}`` \nTicket wird in __5__ Sekunden gelöscht.")
            .WithColor(BotConfig.GetEmbedColor())
            .WithFooter("AGC-Support-System").Build();
        var mb = new DiscordMessageBuilder();
        mb.WithEmbed(ebct);
        var ms = await interaction.Channel.SendMessageAsync("Transcript wird generiert...");
        string transcriptURL = await GenerateTranscript(interaction.Channel);
        await InsertTransscriptIntoDB(interaction.Channel, TranscriptType.Team, transcriptURL);
        await ms.ModifyAsync("Transcript wurde generiert....");
        await ms.DeleteAsync();

        DiscordChannel channel = interaction.Channel;
        await channel.SendMessageAsync(mb);
        await Task.Delay(TimeSpan.FromSeconds(5));
        await SendTranscriptToLog(channel, transcriptURL, interaction.Interaction);
        await channel.DeleteAsync("Ticket wurde gelöscht");
        await DeleteCache(channel);
    }

    public static async Task ClaimTicket(ComponentInteractionCreateEventArgs interaction)
    {
        var teamler = TeamChecker.IsSupporter(await interaction.User.ConvertToMember(interaction.Guild));
        if (!teamler)
        {
            await interaction.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }

        await Claim_UpdateHeaderComponents(interaction);
        var con = DatabaseService.GetConnection();
        string query =
            $"SELECT ticket_id FROM ticketcache where claimed = False AND tchannel_id = '{(long)interaction.Interaction.ChannelId}'";

        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        string ticket_id = "";
        while (await reader.ReadAsync())
        {
            ticket_id = reader.GetString(0);
        }

        var claimembed = new DiscordEmbedBuilder
        {
            Title = "Ticket geclaimed",
            Description = $"Das Ticket wurde von {interaction.User.Mention} ``{interaction.User.Id}`` geclaimed!",
            Color = DiscordColor.Green
        };
        claimembed.WithFooter(
            $"{interaction.User.UsernameWithDiscriminator} wird sich um dein Anliegen kümmern | {ticket_id}");

        await reader.CloseAsync();
        await using NpgsqlCommand cmd2 = new($"UPDATE ticketcache SET claimed = True WHERE ticket_id = '{ticket_id}'",
            con);
        await cmd2.ExecuteNonQueryAsync();

        await using NpgsqlCommand cmd3 =
            new(
                $"UPDATE ticketcache SET claimed_from = '{(long)interaction.User.Id}' WHERE tchannel_id = '{(long)interaction.Interaction.ChannelId}'",
                con);
        await cmd3.ExecuteNonQueryAsync();
    }

    public static async Task AddUserToTicket(CommandContext ctx, DiscordChannel ticket_channel, DiscordUser user, bool addedAfter = false)
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
        await using NpgsqlCommand cmd2 =
            new(
                $"UPDATE ticketcache SET ticket_users = array_append(ticket_users, '{(long)user.Id}') WHERE ticket_id = '{ticket_id}'",
                con);
        await cmd2.ExecuteNonQueryAsync();
        // add perms 
        var channel = ticket_channel;
        var member = await ctx.Guild.GetMemberAsync(user.Id);
        await channel.AddOverwriteAsync(member,
            Permissions.AccessChannels | Permissions.SendMessages | Permissions.AddReactions | Permissions.AttachFiles |
            Permissions.EmbedLinks);
        if (addedAfter)
        {
            var afteraddembed = new DiscordEmbedBuilder
            {
                Title = "User hinzugefügt",
                Description = $"Der User {user.Mention} ``{user.Id}`` wurde zum Ticket hinzugefügt!",
                Color = DiscordColor.Green
            };
            await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(afteraddembed));
        }
    }

    public static async Task AddUserToTicket(DiscordInteraction interaction, DiscordChannel ticket_channel,
        DiscordUser user, bool addedAfter = false)
    {
        var teamler = TeamChecker.IsSupporter(await interaction.User.ConvertToMember(interaction.Guild));
        if (!teamler && addedAfter)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }
        if (addedAfter)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
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
        await using NpgsqlCommand cmd2 =
            new(
                $"UPDATE ticketcache SET ticket_users = array_append(ticket_users, '{(long)user.Id}') WHERE ticket_id = '{ticket_id}'",
                con);
        await cmd2.ExecuteNonQueryAsync();
        // add perms 
        var channel = ticket_channel;
        var member = await interaction.Guild.GetMemberAsync(user.Id);
        await channel.AddOverwriteAsync(member,
            Permissions.AccessChannels | Permissions.SendMessages | Permissions.AddReactions | Permissions.AttachFiles |
            Permissions.EmbedLinks);
        if (addedAfter)
        {
            var afteraddembed = new DiscordEmbedBuilder
            {
                Title = "User hinzugefügt",
                Description = $"Der User {user.Mention} ``{user.Id}`` wurde zum Ticket hinzugefügt!",
                Color = DiscordColor.Green
            };
            await interaction.Channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(afteraddembed));
        }
    }

    public static async Task GenerateTranscriptAndFlag(DiscordInteraction interaction)
    {
        // user selector
        var teamler = TeamChecker.IsSupporter(await interaction.User.ConvertToMember(interaction.Guild));
        if (!teamler)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                               new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }

        DiscordEmbedBuilder eb = new()
        {
            Title = "Transscript",
            Description = "Bitte wähle den User aus, bei dem du dieses Ticket anhängen möchtest (Auto-Flag)!",
            Color = DiscordColor.Blurple
        };

        var usersel = new DiscordUserSelectComponent("Wähle einen User", "transcript_user_selector", 1, 1);

        var irb = new DiscordInteractionResponseBuilder().AddEmbed(eb).AddComponents(usersel).AsEphemeral();
        await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, irb);
    }

    public static async Task TranscriptFlag_Callback(DiscordInteraction interaction, DiscordClient client)
    {
        var users = interaction.Data.Values[0];
        var user = await interaction.Guild.GetMemberAsync(ulong.Parse(users));
        var channel = interaction.Channel;

        var idstring = $"FlagModal-{GenerateTicketID(3)}";
        DiscordInteractionModalBuilder modal = new();
        modal.WithTitle("Weitere Notizen zum Flag");
        modal.CustomId = idstring;
        modal.AddTextComponent(new DiscordTextComponent(TextComponentStyle.Small, label: "Notiz:"));
        await interaction.CreateInteractionModalResponseAsync(modal);
        var interactivity = client.GetInteractivity();
        var result = await interactivity.WaitForModalAsync(idstring, TimeSpan.FromMinutes(5));
        if (result.TimedOut)
        {
            return;
        }
        var notes = result.Result.Interaction.Data.Components[0].Value;
        await result.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        var ticket_id = await GetTicketIdFromChannel(channel);
        var ticket_owner = await GetTicketOwnerFromChannel(channel);
        await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Transcript wird generiert..."));
        var transcriptURL = await GenerateTranscript(channel);
        await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Transcript wird in die Datenbank eingetragen..."));
        var con_db = BotConfig.GetConfig()["DatabaseCfg"]["MainBotDatabase"];
        var con_host = BotConfig.GetConfig()["DatabaseCfg"]["Database_Host"];
        var con_pass = BotConfig.GetConfig()["DatabaseCfg"]["Database_Password"];
        var con_user = BotConfig.GetConfig()["DatabaseCfg"]["Database_User"];
        var currentappid = client.CurrentApplication.Id;
        string caseid = Guid.NewGuid().ToString("N").Substring(0, 8);
        var constring = $"Host={con_host};Username={con_user};Password={con_pass};Database={con_db}";
        var current_unix_timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        await using var con = new NpgsqlConnection(constring);
        await con.OpenAsync();
        var cmd = new NpgsqlCommand("INSERT INTO flags (description, userid, punisherid, datum, caseid) VALUES (@description, @userid, @punisherid, @datum, @caseid)", con);
        cmd.Parameters.AddWithValue("@description", $"Angehängtes Transcript aus {ticket_id} (Von User: {ticket_owner} -> {transcriptURL}  |  Dazugehörige Notiz: {notes}");
        cmd.Parameters.AddWithValue("@userid", (long)user.Id);
        cmd.Parameters.AddWithValue("@punisherid", (long)interaction.User.Id);
        cmd.Parameters.AddWithValue("@datum", current_unix_timestamp);
        cmd.Parameters.AddWithValue("@caseid", caseid);
        await cmd.ExecuteNonQueryAsync();
        await con.CloseAsync();
        await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Transcript wurde in die Datenbank eingetragen bei {user.UsernameWithDiscriminator} ``{user.Id}`` eingetragen!"));
    }

    public static async Task RenderMore(InteractionCreateEventArgs interactionCreateEvent)
    {
        // check if team
        var teamler = TeamChecker.IsSupporter(await interactionCreateEvent.Interaction.User.ConvertToMember(interactionCreateEvent.Interaction.Guild));
        if (!teamler)
        {
            await interactionCreateEvent.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                               new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }
        var buttons = new List<DiscordButtonComponent>();
        // userinfo button 
        var userinfo = new DiscordButtonComponent(ButtonStyle.Primary, "ticket_userinfo", "Userinfo");
        buttons.Add(userinfo);
        // flag transcript button
        var flagtranscript = new DiscordButtonComponent(ButtonStyle.Primary, "ticket_flagtranscript", "Transcript Flaggen");
        buttons.Add(flagtranscript);
        // render 
        var imb = new DiscordInteractionResponseBuilder().AddComponents(buttons).AsEphemeral();
        await interactionCreateEvent.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, imb);
    }

    public static async Task UserInfo(DiscordInteraction interaction)
    {
        var users = await GetTicketUsers(interaction);
        // generate stringselector
        var options = new List<DiscordStringSelectComponentOption>();
        foreach (var user in users)
        {
            options.Add(new DiscordStringSelectComponentOption(user.UsernameWithDiscriminator + " ( " + user.Id.ToString() + " )", user.Id.ToString()));
        }
        var selector = new DiscordStringSelectComponent("Wähle einen User", "Wähle einen User", options, maxOptions:1, minOptions: 1, customId: "userinfo_selector");
        var irb = new DiscordInteractionResponseBuilder().WithContent("Wähle ein User aus dessen infos du sehen willst.").AddComponents(selector).AsEphemeral();
        // Update original
        await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, irb);

    }
    public static async Task UserInfo_Callback(ComponentInteractionCreateEventArgs args)
    {
        // get user
        var user = args.Interaction.Data.Values[0];
        var member = await args.Guild.GetMemberAsync(ulong.Parse(user));
        // gather infos
        var joined_at = member.JoinedAt.Timestamp();
        var created_at = member.CreationTimestamp.Timestamp();
        var toprole_color = member.Color;
        var toprole = member.Roles?.FirstOrDefault();
        var rolemention = toprole?.Mention ?? "Keine Rolle";
        // get prev ticketcount
        var prev_tickets = await GetTicketCountFromThisUser((long)member.Id) - 1;
        Console.WriteLine(12);
        // generate embed
        var eb = new DiscordEmbedBuilder()
            .WithTitle("Userinfo")
            .WithDescription($"Userinfo für {member.Mention} ``{member.Id}``")
            .WithColor(toprole_color)
            .AddField(new DiscordEmbedField("Beigetreten am", joined_at, false)
            ).AddField(new DiscordEmbedField("Erstellt am", created_at, false)
            ).AddField(new DiscordEmbedField("Höchste Rolle", toprole != null ? toprole.Mention : "Keine Rolle", false)
            ).AddField(new DiscordEmbedField("Ticketcount", prev_tickets.ToString())).WithFooter("AGC-Support-System")
            .WithThumbnail(member.AvatarUrl)
            .WithImageUrl(member.BannerUrl);
        var irb = new DiscordInteractionResponseBuilder().AddEmbed(eb).AsEphemeral();
        await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, irb);
    }


    public static async Task<List<DiscordUser>> GetTicketUsers(DiscordInteraction interaction)
    {
        // get them to list 
        var con = DatabaseService.GetConnection();
        string query = $"SELECT ticket_users FROM ticketcache where tchannel_id = '{(long)interaction.Channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        List<long> ticket_users = new();
        while (reader.Read())
        {
            long[] ticketUsersArray = (long[])reader.GetValue(0);
            ticket_users = new List<long>(ticketUsersArray);
        }
        await reader.CloseAsync();
        List<DiscordUser> ticket_users_discord = new();
        foreach (var user in ticket_users)
        {
            var u = await interaction.Guild.GetMemberAsync((ulong)user);
            ticket_users_discord.Add(u);
        }
        return ticket_users_discord;
    }

    public static async Task RemoveUserFromTicket(DiscordInteraction interaction, DiscordChannel ticket_channel,
        DiscordUser user, bool noautomatic = false)
    {
        var teamler = TeamChecker.IsSupporter(await interaction.User.ConvertToMember(interaction.Guild));
        if (!teamler)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }
        if (noautomatic)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }
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
        await using NpgsqlCommand cmd2 =
            new(
                $"UPDATE ticketcache SET ticket_users = array_remove(ticket_users, '{(long)user.Id}') WHERE ticket_id = '{ticket_id}'",
                con);
        await cmd2.ExecuteNonQueryAsync();
        var channel = ticket_channel;
        var member = await interaction.Guild.GetMemberAsync(user.Id);
        await channel.AddOverwriteAsync(member);
        if (noautomatic)
        {
            var afteraddembed = new DiscordEmbedBuilder
            {
                Title = "User entfernt",
                Description = $"Der User {user.Mention} ``{member.Id}`` wurde vom Ticket entfernt!",
                Color = DiscordColor.Red
            };
            await interaction.Channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(afteraddembed));

            var tr = await GenerateTranscript(interaction.Channel);

            var userembed = new DiscordEmbedBuilder
            {
                Title = ticket_channel.Name,
                Description = $"Du wurdest aus dem Ticket ``{ticket_channel.Name}`` entfernt!",
                Color = DiscordColor.Green
            };
        }
    }


    public static async Task<bool> CheckIfUserIsInTicket(DiscordInteraction interaction, DiscordChannel ticket_channel,
        DiscordUser user)
    {
        var con = DatabaseService.GetConnection();
        string query = $"SELECT ticket_users FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        List<long> ticket_users = new();
        while (reader.Read())
        {
            long[] ticketUsersArray = (long[])reader.GetValue(0);
            ticket_users = new List<long>(ticketUsersArray);
        }

        await reader.CloseAsync();
        if (ticket_users.Contains((long)user.Id))
        {
            return true;
        }

        return false;
    }

    public static async Task AddUserToTicketSelector(DiscordInteraction interaction)
    {
        var teamler = TeamChecker.IsSupporter(await interaction.User.ConvertToMember(interaction.Guild));
        if (!teamler)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }
        DiscordEmbedBuilder eb = new()
        {
            Title = "User hinzufügen",
            Description = "Bitte wähle den User aus, den du hinzufügen möchtest!",
            Color = DiscordColor.Blurple
        };
        var uoptions = new DiscordUserSelectComponent[]
        {
            new("Wähle den User aus den du zum Ticket hinzufügen willst.", "adduser_selector", 1)
        };
        DiscordInteractionResponseBuilder irb = new DiscordInteractionResponseBuilder().AddEmbed(eb)
            .AddComponents(uoptions).AsEphemeral();
        await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, irb);
    }

    public static async Task RemoveUserFromTicketSelector(DiscordInteraction interaction)
    {
        var teamler = TeamChecker.IsSupporter(await interaction.User.ConvertToMember(interaction.Guild));
        if (!teamler)
        {
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }
        DiscordEmbedBuilder eb = new()
        {
            Title = "User entfernen",
            Description = "Bitte wähle den User aus, den du entfernen möchtest!",
            Color = DiscordColor.Blurple
        };
        var uoptions = new DiscordUserSelectComponent[]
        {
            new("Wähle den User aus den du vom Ticket entfernen willst.", "removeuser_selector", 1)
        };
        DiscordInteractionResponseBuilder irb = new DiscordInteractionResponseBuilder().AddEmbed(eb)
            .AddComponents(uoptions).AsEphemeral();
        await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, irb);
    }

    public static async Task AddUserToTicketSelector_Callback(ComponentInteractionCreateEventArgs interaction)
    {
        var values = interaction.Interaction.Data.Values;
        var user = values[0];
        var member = await interaction.Guild.GetMemberAsync(ulong.Parse(user));
        var ticket_channel = interaction.Channel;
        if (await CheckIfUserIsInTicket(interaction.Interaction, ticket_channel, member))
        {
            var alreadyinembed = new DiscordEmbedBuilder
            {
                Title = "User bereits im Ticket",
                Description = $"Der User {member.Mention} ``{member.Id}`` ist bereits im Ticket!",
                Color = DiscordColor.Red
            };
            await interaction.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(alreadyinembed).AsEphemeral());
            return;
        }

        await AddUserToTicket(interaction.Interaction, ticket_channel, member, true);
    }

    public static async Task RemoveUserFromTicketSelector_Callback(ComponentInteractionCreateEventArgs interaction)
    {
        var values = interaction.Interaction.Data.Values;
        var user = values[0];
        var member = await interaction.Guild.GetMemberAsync(ulong.Parse(user));
        var ticket_channel = interaction.Channel;
        if (!await CheckIfUserIsInTicket(interaction.Interaction, ticket_channel, member))
        {
            var alreadyinembed = new DiscordEmbedBuilder
            {
                Title = "User nicht im Ticket",
                Description = $"Der User {member.Mention} ``{member.Id}`` ist nicht im Ticket!",
                Color = DiscordColor.Red
            };
            await interaction.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(alreadyinembed).AsEphemeral());
            return;
        }

        await RemoveUserFromTicket(interaction.Interaction, ticket_channel, member, true);
    }

    public static async Task SendTranscriptsToUser(DiscordMember member, string TransscriptURL)
    {
        var eb = new DiscordEmbedBuilder().WithTitle("Transscript")
            .WithDescription($"Hier ist dein Transscript: {TransscriptURL}").WithColor(DiscordColor.Blurple);
        try
        {
            if (member.IsBot) return;
            await member.SendMessageAsync(eb);
        }
        catch (Exception)
        {
            await Task.CompletedTask;
        }
    }

    public static async Task DeleteCache(DiscordChannel ticket_channel)
    {
        var con = DatabaseService.GetConnection();
        string query = $"DELETE FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<bool> IsTicket(DiscordChannel channel)
    {
        var con = DatabaseService.GetConnection();
        string query = $"SELECT COUNT(*) FROM ticketcache where tchannel_id = '{(long)channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        int rowCount = Convert.ToInt32(cmd.ExecuteScalar());
        if (rowCount > 0)
        {
            return true;
        }

        return false;
    }

    public static async Task<string> GenerateTranscript(DiscordChannel ticket_channel)
    {
        var psi = new ProcessStartInfo();
        var BotToken = BotConfig.GetConfig()["MainConfig"]["Discord_API_Token"];
        var tick = await GetTicketIdFromChannel(ticket_channel);
        var id = GenerateTicketID(3);
        psi.FileName = "DiscordChatExporter.Cli.exe";
        psi.Arguments =
            $"export -t \"{BotToken}\" -c {ticket_channel.Id} --media --reuse-media --media-dir transcripts\\Assets -o transcripts\\{tick}-{id}.html";
        psi.RedirectStandardOutput = true;
        var process = new Process();
        process.StartInfo = psi;
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        var baselink = $"https://ticketsystem.animegamingcafe.de/tickets/" + $"{tick}-{id}.html";

        return baselink;
    }

    public static async Task InsertTransscriptIntoDB(DiscordChannel ticket_channel, TranscriptType transcriptType,
        string transcript_url)
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

        if (transcriptType == TranscriptType.User)
        {
            // insert
            await using NpgsqlCommand cmd2 =
                new(
                    $"UPDATE ticketstore SET user_transscript_url = '{transcript_url}' WHERE ticket_id = '{ticket_id}'",
                    con);
            await cmd2.ExecuteNonQueryAsync();
        }
        else if (transcriptType == TranscriptType.Team)
        {
            // insert
            await using NpgsqlCommand cmd2 =
                new(
                    $"UPDATE ticketstore SET team_transscript_url = '{transcript_url}' WHERE ticket_id = '{ticket_id}'",
                    con);
            await cmd2.ExecuteNonQueryAsync();
        }
    }

    public static async Task SendTranscriptToLog(DiscordChannel channel, string ticket_url, DiscordInteraction interaction)
    {
        DiscordEmbedBuilder eb = new();
        var ticket_owner = await GetTicketOwnerFromChannel(channel);
        var staff = interaction.User.Mention;

        eb.AddField(new DiscordEmbedField("Ticket Owner", $"<@{ticket_owner}>", true));
        eb.AddField(new DiscordEmbedField("Ticket Name", channel.Name, true));

        List<DiscordUser> users = new();
        var con = DatabaseService.GetConnection();
        string query = $"SELECT ticket_users FROM ticketcache where tchannel_id = '{(long)channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        List<long> ticket_users = new();
        while (reader.Read())
        {
            long[] ticketUsersArray = (long[])reader.GetValue(0);
            ticket_users = new List<long>(ticketUsersArray);
        }

        await reader.CloseAsync();
        var cusers = "";
        var messages = await channel.GetMessagesAsync();
        HashSet<DiscordUser> userSet = new();
        foreach (var message in messages)
        {
            userSet.Add(message.Author);
        }

        foreach (var user in userSet)
        {
            cusers += $"{user.Mention} ``{user.Id}``\n";
        }

        eb.AddField(new DiscordEmbedField("Nutzer im Ticket", cusers, true));
        eb.AddField(new DiscordEmbedField("Ticket URL", $"[Transcript Link]({ticket_url})", true));
        eb.AddField(new DiscordEmbedField("Ticket ID", await GetTicketIdFromChannel(channel), true));
        eb.AddField(new DiscordEmbedField("Staff", staff, true));
        eb.WithColor(DiscordColor.Blurple);
        eb.WithFooter($"User-ID = {ticket_owner}");
        eb.WithTimestamp(DateTime.Now);
        var logchannel = channel.Guild.GetChannel(ulong.Parse(BotConfig.GetConfig()["SupportConfig"]["LogChannelId"]));
        await logchannel.SendMessageAsync(eb);
    }

    public static async Task SendTranscriptToLog(CommandContext ctx, string ticket_url)
    {
        DiscordEmbedBuilder eb = new();
        var ticket_owner = await GetTicketOwnerFromChannel(ctx.Channel);

        eb.AddField(new DiscordEmbedField("Ticket Owner", $"<@{ticket_owner}>", true));
        eb.AddField(new DiscordEmbedField("Ticket Name", ctx.Channel.Name, true));

        List<DiscordUser> users = new();
        var con = DatabaseService.GetConnection();
        string query = $"SELECT ticket_users FROM ticketcache where tchannel_id = '{(long)ctx.Channel.Id}'";
        await using NpgsqlCommand cmd = new(query, con);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        List<long> ticket_users = new();
        while (reader.Read())
        {
            long[] ticketUsersArray = (long[])reader.GetValue(0);
            ticket_users = new List<long>(ticketUsersArray);
        }

        await reader.CloseAsync();
        var cusers = "";
        var messages = await ctx.Channel.GetMessagesAsync();
        HashSet<DiscordUser> userSet = new();
        foreach (var message in messages)
        {
            userSet.Add(message.Author);
        }

        foreach (var user in userSet)
        {
            cusers += $"{user.Mention} ``{user.Id}``\n";
        }

        eb.AddField(new DiscordEmbedField("Nutzer im Ticket", cusers, true));
        eb.AddField(new DiscordEmbedField("Ticket URL", $"[Transcript Link]({ticket_url})", true));
        eb.AddField(new DiscordEmbedField("Ticket ID", await GetTicketIdFromChannel(ctx.Channel), true));
        eb.AddField(new DiscordEmbedField("Staff", ctx.Message.Author.Mention, true));
        eb.WithColor(DiscordColor.Blurple);
        eb.WithFooter($"User-ID = {ticket_owner}");
        eb.WithTimestamp(DateTime.Now);
        var logchannel = ctx.Guild.GetChannel(ulong.Parse(BotConfig.GetConfig()["SupportConfig"]["LogChannelId"]));
        await logchannel.SendMessageAsync(eb);
    }
}