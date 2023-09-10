﻿using AGC_Ticket;
using AGC_Ticket.Helpers;
using AGC_Ticket.Services.DatabaseHandler;
using AGC_Ticket_System.Components;
using AGC_Ticket_System.Enums;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using Npgsql;

namespace AGC_Ticket_System.Helper;

public class TicketManager
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
            var tbutton = new DiscordLinkButtonComponent("https://discord.com/channels/" + guildid + "/" + tchannelId,
                "Zum Ticket");
            var eb = new DiscordEmbedBuilder
            {
                Title = "Fehler | Bereits ein Ticket geöffnet!",
                Description =
                    $"Du hast bereits ein geöffnetes Ticket! -> <#{await TicketManagerHelper.GetOpenTicketChannel((long)interaction.User.Id)}>",
                Color = DiscordColor.Red
            };
            await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(eb).AddComponents(tbutton).AsEphemeral());
            return;
        }

        var cre_emb = new DiscordEmbedBuilder
        {
            Title = "Ticket erstellen",
            Description = "Du hast ein Ticket erstellt! Bitte warte einen Augenblick...",
            Color = DiscordColor.Blurple
        };
        await interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AddEmbed(cre_emb).AsEphemeral());
        int ticket_number = await TicketManagerHelper.GetPreviousTicketCount(ticketType) + 1;
        DiscordChannel Ticket_category =
            interaction.Guild.GetChannel(ulong.Parse(BotConfig.GetConfig()["SupportConfig"]["SupportCategoryId"]));
        DiscordChannel? ticket_channel = null;
        if (ticketType == TicketType.Report)
        {
            var con = DatabaseService.GetConnection();
            await using NpgsqlCommand cmd =
                new(
                    $"INSERT INTO ticketstore (ticket_id, ticket_owner, tickettype, closed) VALUES ('{ticketid}', '{memberid}', '{ticketType.ToString().ToLower()}', False)",
                    con);
            await cmd.ExecuteNonQueryAsync();

            ticket_channel = await interaction.Guild.CreateChannelAsync($"report-{ticket_number}", ChannelType.Text,
                Ticket_category, $"Ticket erstellt von {interaction.User.UsernameWithDiscriminator}");

            await using NpgsqlCommand cmd2 =
                new(
                    $"INSERT INTO ticketcache (ticket_id, ticket_owner, tchannel_id, claimed) VALUES ('{ticketid}', '{memberid}', '{ticket_channel.Id}', False)",
                    con);
            await cmd2.ExecuteNonQueryAsync();
            await Task.Delay(TimeSpan.FromSeconds(2));
            await TicketManagerHelper.AddUserToTicket(interaction, ticket_channel, interaction.User);
            await TicketManagerHelper.InsertHeaderIntoTicket(interaction, ticket_channel, TicketCreator.User,
                TicketType.Report);
        }
        else if (ticketType == TicketType.Support)
        {
            var con = DatabaseService.GetConnection();
            await using NpgsqlCommand cmd =
                new(
                    $"INSERT INTO ticketstore (ticket_id, ticket_owner, tickettype, closed) VALUES ('{ticketid}', '{memberid}', '{ticketType.ToString().ToLower()}', False)",
                    con);
            await cmd.ExecuteNonQueryAsync();

            ticket_channel = await interaction.Guild.CreateChannelAsync($"support-{ticket_number}", ChannelType.Text,
                Ticket_category, $"Ticket erstellt von {interaction.User.UsernameWithDiscriminator}");

            await using NpgsqlCommand cmd2 =
                new(
                    $"INSERT INTO ticketcache (ticket_id, ticket_owner, tchannel_id, claimed) VALUES ('{ticketid}', '{memberid}', '{ticket_channel.Id}', False)",
                    con);
            await cmd2.ExecuteNonQueryAsync();
            await Task.Delay(TimeSpan.FromSeconds(2));
            await TicketManagerHelper.AddUserToTicket(interaction, ticket_channel, interaction.User);
            await TicketManagerHelper.InsertHeaderIntoTicket(interaction, ticket_channel, TicketCreator.User,
                TicketType.Support);
        }

        var button = new DiscordLinkButtonComponent("https://discord.com/channels/" + guildid + "/" + ticket_channel.Id,
            "Zum Ticket");
        var teb = new DiscordEmbedBuilder
        {
            Title = "Ticket erstellt",
            Description = $"Dein Ticket wurde erfolgreich erstellt! -> <#{ticket_channel.Id}>",
            Color = DiscordColor.Green
        };
        // inset header later
        await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().AddEmbed(teb)
            .AddComponents(button));
        await TicketManagerHelper.SendUserNotice(interaction, ticket_channel, ticketType);
    }

    public static async Task CloseTicket(ComponentInteractionCreateEventArgs interaction, DiscordChannel ticket_channel)
    {
        var teamler =
            TeamChecker.IsSupporter(await interaction.Interaction.User.ConvertToMember(interaction.Interaction.Guild));
        if (!teamler)
        {
            await interaction.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Du bist kein Teammitglied!").AsEphemeral());
            return;
        }

        await interaction.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        var message = await ticket_channel.GetMessageAsync(interaction.Message.Id);
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
        var ceb = new DiscordEmbedBuilder
        {
            Description = "Ticket wird geschlossen..",
            Color = DiscordColor.Yellow
        };
        await ticket_channel.SendMessageAsync(ceb);


        var eb1 = new DiscordEmbedBuilder
        {
            Description = "Transcript wird gespeichert....",
            Color = DiscordColor.Yellow
        };
        var msg = await interaction.Channel.SendMessageAsync(eb1.Build());

        eb1 = new DiscordEmbedBuilder
        {
            Description = "Transcript wurde gespeichert",
            Color = DiscordColor.Green
        };

        string transcriptURL = await TicketManagerHelper.GenerateTranscript(ticket_channel);
        await TicketManagerHelper.InsertTransscriptIntoDB(ticket_channel, TranscriptType.User, transcriptURL);

        await msg.ModifyAsync(eb1.Build());


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

        await using NpgsqlCommand cmd2 = new($"UPDATE ticketstore SET closed = True WHERE ticket_id = '{ticket_id}'",
            con);
        await cmd2.ExecuteNonQueryAsync();

        var con2 = DatabaseService.GetConnection();
        string query2 = $"SELECT ticket_users FROM ticketcache where tchannel_id = '{(long)ticket_channel.Id}'";
        await using NpgsqlCommand cmd3 = new(query2, con2);
        await using NpgsqlDataReader reader2 = await cmd3.ExecuteReaderAsync();
        List<List<long>> ticket_usersList = new();

        while (reader2.Read())
        {
            long[] ticketUsersArray = (long[])reader2.GetValue(0);
            List<long> ticket_users = new(ticketUsersArray);
            ticket_usersList.Add(ticket_users);
        }

        await reader2.CloseAsync();

        var button = new DiscordLinkButtonComponent(
            "https://discord.com/channels/" + interaction.Guild.Id + "/" + interaction.Channel.Id, "Zum Ticket");

        var del_ticketbutton = new DiscordButtonComponent(ButtonStyle.Danger, "ticket_delete", "Ticket löschen ❌");
        var teb = new DiscordEmbedBuilder
        {
            Title = "Ticket geschlossen",
            Description =
                $"Das Ticket wurde erfolgreich geschlossen!\n Geschlossen von {interaction.User.UsernameWithDiscriminator} ``{interaction.User.Id}``",
            Color = DiscordColor.Green
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
                await TicketManagerHelper.SendTranscriptsToUser(member, transcriptURL);
            }
        }
    }
}