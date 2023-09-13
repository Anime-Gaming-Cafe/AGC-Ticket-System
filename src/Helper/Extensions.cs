using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.Exceptions;

namespace AGC_Ticket_System.Helper;

internal static class Extensions
{
    internal static async Task<DiscordUser?> TryGetUserAsync(this DiscordClient client, ulong userId, bool fetch = true)
    {
        try
        {
            return await client.GetUserAsync(userId, fetch).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }
}