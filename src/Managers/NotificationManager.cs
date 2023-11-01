﻿using System.Text;
using AGC_Ticket_System.Components;
using AGC_Ticket_System.Enums;
using AGC_Ticket.Services.DatabaseHandler;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using Npgsql;

namespace AGC_Ticket_System.Managers;

public class NotificationManager
{
    public static async Task SetMode(NotificationMode mode, ulong channel_id, ulong user_id)
    {
        long cid = (long)channel_id;
        long uid = (long)user_id;
        await RemoveMode(channel_id, user_id);
        var constring = DatabaseService.GetConnectionString();
        await using var con = new NpgsqlConnection(constring);
        await con.OpenAsync();
        await using var cmd = new NpgsqlCommand("INSERT INTO subscriptions (user_id, channel_id, mode) VALUES (@uid, @cid, @mode)", con);
        cmd.Parameters.AddWithValue("mode", (int)mode);
        cmd.Parameters.AddWithValue("uid", uid);
        cmd.Parameters.AddWithValue("cid", cid);
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task RemoveMode(ulong channel_id, ulong user_id)
    {
        long cid = (long)channel_id;
        long uid = (long)user_id;
        var constring = DatabaseService.GetConnectionString();
        await using var con = new NpgsqlConnection(constring);
        await con.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM subscriptions WHERE user_id = @uid AND channel_id = @cid", con);
        cmd.Parameters.AddWithValue("uid", uid);
        cmd.Parameters.AddWithValue("cid", cid);
        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<NotificationMode> GetCurrentMode(ulong channel_id, ulong user_id)
    {
        long cid = (long)channel_id;
        long uid = (long)user_id;
        var constring = DatabaseService.GetConnectionString();
        await using var con = new NpgsqlConnection(constring);
        await con.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT mode FROM subscriptions WHERE user_id = @uid AND channel_id = @cid", con);
        cmd.Parameters.AddWithValue("uid", uid);
        cmd.Parameters.AddWithValue("cid", cid);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (reader.HasRows)
        {
            await reader.ReadAsync();
            return (NotificationMode)reader.GetInt32(0);
        }
        else
        {
            return NotificationMode.Disabled;
        }
        
    }

    private static List<DiscordActionRowComponent> GetPhase1Row(NotificationMode mode)
    {
        if (mode == NotificationMode.Disabled)
        {
            return TicketComponents.GetNotificationManagerButtons();
        }
        else
        {
            return TicketComponents.GetNotificationManagerButtonsEnabledNotify();
        }
    }

    public static async Task RenderNotificationManager(DiscordInteraction interaction)
    {
        var customid = interaction.Data.CustomId;
        NotificationMode mode = await GetCurrentMode(interaction.Channel.Id, interaction.User.Id);
        string enabled = mode != NotificationMode.Disabled ? "✅" : "❌";

        StringBuilder content = new StringBuilder();
        content.Append($"**Benachrichtigungen für <#{interaction.Channel.Id}> / <@{interaction.User.Id}>**");
        content.Append("\n\n");
        content.Append($"**Status:** {enabled}\n");
        content.Append($"**Gesetzter Modus:** {mode}");
        var rows = GetPhase1Row(mode);
        var irb = new DiscordInteractionResponseBuilder();
        irb.AddEmbed(new DiscordEmbedBuilder().WithColor(DiscordColor.Blurple).WithDescription(content.ToString()));
        irb.AddComponents(rows).AsEphemeral();
        await interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, irb);
    }
    
    public static async Task RenderNotificationManagerWithUpdate(DiscordInteraction interaction)
    {
        var customid = interaction.Data.CustomId;
        NotificationMode mode = await GetCurrentMode(interaction.Channel.Id, interaction.User.Id);
        string enabled = mode != NotificationMode.Disabled ? "✅" : "❌";

        StringBuilder content = new StringBuilder();
        content.Append($"**Benachrichtigungen für <#{interaction.Channel.Id}> / <@{interaction.User.Id}>**");
        content.Append("\n\n");
        content.Append($"**Status:** {enabled}\n");
        content.Append($"**Gesetzter Modus:** {mode}");
        var rows = GetPhase1Row(mode);
        var irb = new DiscordInteractionResponseBuilder();
        irb.AddEmbed(new DiscordEmbedBuilder().WithColor(DiscordColor.Blurple).WithDescription(content.ToString()));
        irb.AddComponents(rows).AsEphemeral();
        await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, irb);
    }
    

    public static async Task ChangeMode(DiscordInteraction interaction)
    {
        var customid = interaction.Data.CustomId;
        Console.WriteLine(customid == "enable_noti_mode1");
        if (customid == "disable_notification")
        {
            await RemoveMode(interaction.Channel.Id, interaction.User.Id);
            await RenderNotificationManagerWithUpdate(interaction);
        }
        else if (customid == "enable_noti_mode1")
        {
            Console.WriteLine("SetMode");
            await SetMode(NotificationMode.OnceMention, interaction.Channel.Id, interaction.User.Id);
            await RenderNotificationManagerWithUpdate(interaction);
        }
        else if (customid == "enable_noti_mode2")
        {
            await SetMode(NotificationMode.OnceDM, interaction.Channel.Id, interaction.User.Id);
            await RenderNotificationManagerWithUpdate(interaction);
        }
        else if (customid == "enable_noti_mode3")
        {
            await SetMode(NotificationMode.OnceBoth, interaction.Channel.Id, interaction.User.Id);
            await RenderNotificationManagerWithUpdate(interaction);
        }
        else if (customid == "enable_noti_mode4")
        {
            await SetMode(NotificationMode.AlwaysMention, interaction.Channel.Id, interaction.User.Id);
            await RenderNotificationManagerWithUpdate(interaction);
        }
        else if (customid == "enable_noti_mode5")
        {
            await SetMode(NotificationMode.AlwaysDM, interaction.Channel.Id, interaction.User.Id);
            await RenderNotificationManagerWithUpdate(interaction);
        }
        else if (customid == "enable_noti_mode6")
        {
            await SetMode(NotificationMode.AlwaysBoth, interaction.Channel.Id, interaction.User.Id);
            await RenderNotificationManagerWithUpdate(interaction);
        }
    }
    
}