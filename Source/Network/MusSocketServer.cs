using System;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Holo.Network;

/// <summary>
/// Asynchronous socket server for MUS (Multi User Server) connections using DotNetty.
/// </summary>
public static class MusSocketServer
{
    private static IEventLoopGroup? _bossGroup;
    private static IEventLoopGroup? _workerGroup;
    private static IChannel? _serverChannel;

    private static int _port;
    private static string _musHost = "";

    /// <summary>
    /// Initializes the DotNetty socket server for MUS connections.
    /// </summary>
    /// <param name="bindPort">The port to bind to.</param>
    /// <param name="musHost">The allowed host for MUS connections.</param>
    public static async Task<bool> InitAsync(int bindPort, string musHost)
    {
        _port = bindPort;
        _musHost = musHost;

        Out.WriteLine($"Starting up DotNetty socket server for MUS connections on port {bindPort}...");

        try
        {
            _bossGroup = new MultithreadEventLoopGroup(1);
            _workerGroup = new MultithreadEventLoopGroup();

            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(_bossGroup, _workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 25)
                .Option(ChannelOption.SoReuseaddr, true)
                .ChildOption(ChannelOption.TcpNodelay, true)
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;

                    // MUS uses raw byte buffer handling (no special encoding)
                    pipeline.AddLast("handler", new MusClientHandler(_musHost));
                }));

            _serverChannel = await bootstrap.BindAsync(IPAddress.Any, bindPort);

            Out.WriteLine($"DotNetty socket server for MUS connections running on port {bindPort}");
            Out.WriteLine($"Listening for MUS connections from {musHost}");
            return true;
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error while setting up DotNetty MUS socket server on port {bindPort}");
            Out.WriteError($"Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Shuts down the socket server gracefully.
    /// </summary>
    public static async Task ShutdownAsync()
    {
        try
        {
            if (_serverChannel != null)
            {
                await _serverChannel.CloseAsync();
            }

            var workerShutdown = _workerGroup?.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            var bossShutdown = _bossGroup?.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));

            if (workerShutdown != null) await workerShutdown;
            if (bossShutdown != null) await bossShutdown;
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error during MUS shutdown: {ex.Message}");
        }
    }
}
