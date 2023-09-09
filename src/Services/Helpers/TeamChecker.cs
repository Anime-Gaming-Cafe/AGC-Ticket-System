using DisCatSharp.Entities;

namespace AGC_Ticket.Helpers;

public static class TeamChecker
{
    public static bool IsSupporter(DiscordMember member)
    {
        var SupporterRole = ulong.Parse(BotConfig.GetConfig()["SupportConfig"]["TeamRoleId"]);
        if (member.Roles.Any(x => x.Id == SupporterRole))
            return true;
        return false;
    }
}