using AGC_Ticket;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using AGC_Ticket_System.Components;
using AGC_Ticket_System.Helper;
using AGC_Ticket_System.Enums;

namespace AGC_Ticket_System.Commands;

public class SupportPanel : BaseCommandModule
{
    [Command("initsupportpanel")]
    [RequireGuildOwner]
    public async Task InitSupportPanel(CommandContext ctx)
    {
        DiscordEmbed embed = new DiscordEmbedBuilder().WithTitle("AGC Support-System").WithDescription("""
            __Benötigst du Hilfe oder Support? Mach ein Ticket auf.__
            
            > Wann sollte ich ein Ticket öffnen?
            Wenn du irgendwelche Fragen hast oder irgendetwas unklar ist, du jemanden wegen Regelverstoß der Server Regeln oder der Discord Richtlinen melden möchtest!


            > Wie öffne ich ein Ticket?
            Wenn du ein Ticket öffnen willst, klicke unten auf "Ticket öffnen" und wähle danach eine der Kategorien aus, um was es geht. Danach wird ein Ticket mit dir erstellt und du kannst dein Anliegen schlidern.
            """).WithColor(BotConfig.GetEmbedColor()).WithFooter("Troll und absichtlicher Abuse ist zu unterlassen!").Build();

        List<DiscordButtonComponent> buttons = new()
        {
            new DiscordButtonComponent(ButtonStyle.Danger, $"selectticketcategory", "Ticket öffnen ✉️"),
        };
        DiscordMessageBuilder msgb = new DiscordMessageBuilder();
        msgb.WithEmbed(embed).AddComponents(buttons);
        var msg = await ctx.Channel.SendMessageAsync(msgb);
        BotConfig.SetConfig("SupportConfig", "SupportPanelMessage", msg.Id.ToString());
        BotConfig.SetConfig("SupportConfig", "SupportPanelChannel", ctx.Channel.Id.ToString());
        BotConfig.SetConfig("SupportConfig", "SupportGuild", ctx.Guild.Id.ToString());
    }
}

[EventHandler]
public class SupportPanelListener : SupportPanel
{
    [Event]
    public async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            var PanelChannelId = ulong.Parse(BotConfig.GetConfig()["SupportConfig"]["SupportPanelChannel"]);
            if (e.Channel.Id == PanelChannelId && e.Interaction.Data.CustomId == "selectticketcategory")
            {
                List<DiscordButtonComponent> buttons = new();

                var sup_cats = await SupportComponents.GetSupportCategories();
                foreach (var cat in sup_cats)
                {
                    buttons.Add(new DiscordButtonComponent(ButtonStyle.Primary, label: $"{cat.Value}", customId: $"ticket_open_{cat.Key.ToString()}"));
                }

                DiscordEmbed embed = new DiscordEmbedBuilder()
                .WithTitle("Wähle eine Supportkategorie aus")
                .WithDescription("Wähle unten eine Supportkategorie aus. Dies hilft uns dein Ticket schneller zuzuordnen." +
                "Nach auswahl der Kategorie wird ein Ticket erstellt, bitte schlilder anschließend im Ticket dein Anliegen.\n\n" +
                "> Report / Melden \n" +
                "Hier kannst du einen Benutzer melden, der gegen Regeln verstößt oder anderweitig auffällt \n\n" +
                "> Support \n" +
                "Hier kannst du dich bei generellen Anliegen melden").WithColor(BotConfig.GetEmbedColor());

                var ib = new DiscordInteractionResponseBuilder().AsEphemeral().AddComponents(buttons).AddEmbed(embed);
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, ib);
            }
            // handle ticket opening 
            if (e.Interaction.Data.CustomId == "ticket_open_report")
            {
                await TicketManager.OpenTicket(e.Interaction, TicketType.Report, client);
            }
            else if (e.Interaction.Data.CustomId == "ticket_open_support")
            {
                await TicketManager.OpenTicket(e.Interaction, TicketType.Support, client);
            }

            return Task.CompletedTask;
        });
    }
}