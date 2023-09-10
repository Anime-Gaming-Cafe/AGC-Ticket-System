using AGC_Ticket_System.Enums;
using AGC_Ticket_System.Helper;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;

namespace AGC_Ticket_System.Commands;

public class TicketCommands : BaseCommandModule
{
    [Command("transcript")]
    [RequireStaffRole]
    public async Task GetTranscript(CommandContext ctx)
    {
        bool isticket = await TicketManagerHelper.IsTicket(ctx.Channel);
        if (!isticket)
        {
            await ctx.RespondAsync("Dieser Channel ist kein Ticket!");
            return;
        }

        var eb = new DiscordEmbedBuilder()
            .WithDescription("Transcript wird gespeichert...").WithColor(DiscordColor.Yellow).Build();
        var eb1 = new DiscordEmbedBuilder().WithColor(DiscordColor.Green).WithDescription("Transcript wurde gespeichert!").Build();
        var eberror = new DiscordEmbedBuilder().WithColor(DiscordColor.Red).WithDescription("Fehler beim generieren des Transcripts!").Build();
        var msg = await ctx.RespondAsync(eb);
        var ticket_channel = ctx.Channel;
        var s = await TicketManagerHelper.GenerateTranscript(ticket_channel);
        if (s == null)
        {
            await msg.ModifyAsync(embed: eberror);
            return;
        }
        await msg.ModifyAsync(eb1);
        await TicketManagerHelper.SendTranscriptToLog(ctx, s, ctx.Client);
    }

    [Command("contact")]
    [RequireGuild]
    [RequireStaffRole]
    public async Task ContactUser(CommandContext ctx, DiscordMember member)
    {
        var ticket_channel = await TicketManager.OpenTicket(ctx, TicketType.Support, TicketCreator.Staff, member);
        var eb = new DiscordEmbedBuilder().WithColor(DiscordColor.Green).WithTitle(ctx.Guild.Name).WithDescription($"Du wurdest von {ctx.Member.Mention} kontaktiert! -> {ticket_channel.Mention}").Build();
        var channellink = $"https://discord.com/channels/{ctx.Guild.Id}/{ticket_channel.Id}";
        var button = new DiscordLinkButtonComponent(channellink, "Zum Ticket");
        var mb = new DiscordMessageBuilder().WithEmbed(eb).AddComponents(button);
        await member.SendMessageAsync(mb);
        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":envelope:"));
    }


}