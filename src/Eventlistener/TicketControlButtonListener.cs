using AGC_Ticket_System.Helper;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;

namespace AGC_Ticket_System.Eventlistener;

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
            else if (e.Interaction.Data.CustomId == "ticket_delete")
            {
                await TicketManagerHelper.DeleteTicket(e);
            }
            else if (e.Interaction.Data.CustomId == "ticket_add_user")
            {
                await TicketManagerHelper.AddUserToTicketSelector(e.Interaction);
            }
            else if (e.Interaction.Data.CustomId == "adduser_selector")
            {
                await TicketManagerHelper.AddUserToTicketSelector_Callback(e);
            }
            else if (e.Interaction.Data.CustomId == "removeuser_selector")
            {
                await TicketManagerHelper.RemoveUserFromTicketSelector_Callback(e);
            }
            else if (e.Interaction.Data.CustomId == "ticket_remove_user")
            {
                await TicketManagerHelper.RemoveUserFromTicketSelector(e.Interaction);
            }
            else if (e.Interaction.Data.CustomId == "ticket_more")
            {
                await TicketManagerHelper.RenderMore(e);
            }
            else if (e.Interaction.Data.CustomId == "ticket_userinfo")
            {
                await TicketManagerHelper.UserInfo(e.Interaction);
            }            
            else if (e.Interaction.Data.CustomId == "userinfo_selector")
            {
                await TicketManagerHelper.UserInfo_Callback(e);
            }
            else if (e.Interaction.Data.CustomId == "ticket_flagtranscript")
            {
                await TicketManagerHelper.GenerateTranscriptAndFlag(e.Interaction);
            }
            else if (e.Interaction.Data.CustomId == "transcript_user_selector")
            {
                await TicketManagerHelper.TranscriptFlag_Callback(e.Interaction, client);
            }



            return Task.CompletedTask;
        });
    }
}