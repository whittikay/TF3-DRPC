using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Tmds.DBus.Protocol;

using Tf3Drpc.Linux.Services.Interfaces;

namespace Tf3Drpc.Linux.Services;

/// <summary>
/// Queries the session tray watcher without activating a missing service.
/// </summary>
internal sealed class TrayAvailabilityService : ITrayAvailabilityService
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(1);

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            string? address = DBusAddress.Session;

            if (string.IsNullOrEmpty(address))
                return false;

            using DBusConnection connection = new(address);
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(ProbeTimeout);
            await connection.ConnectAsync().AsTask().WaitAsync(deadline.Token);
            Task<bool> query = connection.CallMethodAsync(CreateQuery(connection), static (message, _) => ReadReply(message));

            return await query.WaitAsync(deadline.Token);
        }
        catch (Exception exception) when (exception is DBusExceptionBase or IOException or SocketException
            or OperationCanceledException or InvalidOperationException or ArgumentException)
        {
            return false;
        }
    }

    private static MessageBuffer CreateQuery(DBusConnection connection)
    {
        using MessageWriter writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader
        (
            destination: "org.kde.StatusNotifierWatcher",
            path: "/StatusNotifierWatcher",
            @interface: "org.freedesktop.DBus.Properties",
            member: "Get",
            signature: "ss",
            flags: MessageFlags.NoAutoStart
        );
        writer.WriteString("org.kde.StatusNotifierWatcher");
        writer.WriteString("IsStatusNotifierHostRegistered");

        return writer.CreateMessage();
    }

    private static bool ReadReply(Message message)
    {
        Reader reader = message.GetBodyReader();
        reader.ReadSignature("b");

        return reader.ReadBool();
    }
}
