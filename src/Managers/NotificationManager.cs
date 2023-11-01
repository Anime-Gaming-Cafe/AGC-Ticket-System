using AGC_Ticket_System.Enums;
using AGC_Ticket.Services.DatabaseHandler;
using DisCatSharp.Entities;
using Npgsql;

namespace AGC_Ticket_System.Managers;

public class NotificationManager
{
    public static async Task SetMode(NotificationMode mode, ulong channel_id, ulong user_id)
    {
        long cid = (long)channel_id;
        long uid = (long)user_id;
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
}