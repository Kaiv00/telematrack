using System.Net.Sockets;
using Xunit;

namespace FunctionsTests;

// Like [Fact], but skips itself if the Cosmos emulator isn't reachable
public sealed class EmulatorFactAttribute : FactAttribute
{
    public EmulatorFactAttribute()
    {
        if (!EmulatorIsUp())
            Skip = "Cosmos emulator not reachable on localhost:8081 (docker start cosmos-emulator)";
    }

    private static bool EmulatorIsUp()
    {
        try
        {
            using var tcp = new TcpClient();
            return tcp.ConnectAsync("localhost", 8081).Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            return false;
        }
    }
}