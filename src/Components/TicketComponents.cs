using DisCatSharp.Entities;
using DisCatSharp.Enums;

namespace AGC_Ticket_System.Components;

public class TicketComponents
{
    public static List<DiscordButtonComponent> GetTicketActionRow()
    {
        List<DiscordButtonComponent> buttons = new()
        {
            new DiscordButtonComponent(ButtonStyle.Danger, $"ticket_close", "(Team) Ticket schließen ❌"),
            new DiscordButtonComponent(ButtonStyle.Primary, "ticket_claim", "(Team) Ticket Claimen 👋"),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_add_user", "(Team) User hinzufügen 👥"),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_remove_user", "(Team) User entfernen 👤"),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_add_supporter", "(Team) Supporter hinzufügen 🛠️"),
        };
        return buttons;
    }

    public static List<DiscordButtonComponent> GetTicketClaimedActionRow()
    {
        List<DiscordButtonComponent> buttons = new()
        {
            new DiscordButtonComponent(ButtonStyle.Danger, $"ticket_close", "(Team) Ticket schließen ❌"),
            new DiscordButtonComponent(ButtonStyle.Primary, "ticket_claim", "(Team) Ticket Claimen 👋", disabled:true),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_add_user", "(Team) User hinzufügen 👥"),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_remove_user", "(Team) User entfernen 👤"),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_add_supporter", "(Team) Supporter hinzufügen 🛠️"),
        };
        return buttons;
    }

    public static List<DiscordButtonComponent> GetClosedTicketActionRow()
    {
        List<DiscordButtonComponent> buttons = new()
        {
            new DiscordButtonComponent(ButtonStyle.Danger, $"ticket_close", "(Team) Ticket schließen ❌", disabled: true),
            new DiscordButtonComponent(ButtonStyle.Primary, "ticket_claim", "(Team) Ticket Claimen 👋", disabled: true),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_add_user", "(Team) User hinzufügen 👥", disabled: true),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_remove_user", "(Team) User entfernen 👤", disabled: true),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_add_supporter", "(Team) Supporter hinzufügen 🛠️", disabled: true),
        };
        return buttons;
    }

    public static List<DiscordButtonComponent> GetContactTicketActionRow()
    {
        List<DiscordButtonComponent> buttons = new()
        {
            new DiscordButtonComponent(ButtonStyle.Danger, $"ticket_close", "(Team) Ticket schließen ❌"),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_add_user", "(Team) User hinzufügen 👥"),
            new DiscordButtonComponent(ButtonStyle.Primary, $"ticket_remove_user", "(Team) User entfernen 👤"),
        };
        return buttons;
    }
}