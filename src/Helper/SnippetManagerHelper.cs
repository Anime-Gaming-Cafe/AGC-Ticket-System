using AGC_Ticket.Services.DatabaseHandler;
using Npgsql;

namespace AGC_Ticket_System.Helper;

public class SnippetManagerHelper
{
    public static async Task<string?> GetSnippetAsync(string snippetId)
    {
        if (string.IsNullOrEmpty(snippetId))
        {
            return null;
        }

        var con = DatabaseService.GetConnection();
        await using var cmd = new NpgsqlCommand("SELECT snipped_text FROM snippets WHERE snip_id = @snippetId", con);
        cmd.Parameters.AddWithValue("@snippetId", snippetId);

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();

        while (reader.Read())
        {
            return reader.GetString(0);
        }
        await reader.CloseAsync();

        return null;
    }

}