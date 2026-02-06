using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Holo.Network;

/// <summary>
/// Asynchronous socket server for game connections using DotNetty.
/// </summary>
public static class GameSocketServer
{
    private static IEventLoopGroup? _bossGroup;
    private static IEventLoopGroup? _workerGroup;
    private static IChannel? _serverChannel;

    private static int _port;
    private static int _maxConnections;
    private static int _acceptedConnections;
    private static readonly ConcurrentDictionary<int, bool> _activeConnections = new();

    /// <summary>
    /// Initializes the DotNetty socket server for game connections.
    /// </summary>
    /// <param name="bindPort">The port to bind to.</param>
    /// <param name="maxConnections">Maximum simultaneous connections.</param>
    public static async Task<bool> InitAsync(int bindPort, int maxConnections)
    {
        _port = bindPort;
        _maxConnections = maxConnections;

        Out.WriteLine($"Starting up DotNetty socket server for game connections on port {bindPort}...");

        try
        {
            _bossGroup = new MultithreadEventLoopGroup(1);
            _workerGroup = new MultithreadEventLoopGroup();

            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(_bossGroup, _workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100)
                .Option(ChannelOption.SoReuseaddr, true)
                .ChildOption(ChannelOption.TcpNodelay, true)
                .ChildOption(ChannelOption.SoKeepalive, true)
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    int connectionId = GetFreeConnectionId();
                    if (connectionId == 0)
                    {
                        // No free slots, reject connection
                        channel.CloseAsync();
                        return;
                    }

                    _activeConnections.TryAdd(connectionId, true);
                    _acceptedConnections++;

                    IChannelPipeline pipeline = channel.Pipeline;

                    // Add the Habbo packet decoder/encoder
                    pipeline.AddLast("decoder", new HabboPacketDecoder());
                    pipeline.AddLast("encoder", new HabboPacketEncoder());

                    // Add the game client handler
                    pipeline.AddLast("handler", new GameClientHandler(connectionId));
                }));

            _serverChannel = await bootstrap.BindAsync(IPAddress.Any, bindPort);

            Out.WriteLine($"DotNetty socket server for game connections running on port {bindPort}");
            Out.WriteLine($"Max simultaneous connections is {maxConnections}");
            return true;
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error while setting up DotNetty socket server on port {bindPort}");
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
            Out.WriteError($"Error during shutdown: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the next available connection ID.
    /// </summary>
    private static int GetFreeConnectionId()
    {
        for (int i = 1; i <= _maxConnections; i++)
        {
            if (!_activeConnections.ContainsKey(i))
            {
                return i;
            }
        }
        return 0; // No free slots
    }

    /// <summary>
    /// Frees a connection slot.
    /// </summary>
    /// <param name="connectionId">The connection ID to free.</param>
    public static void FreeConnection(int connectionId)
    {
        if (_activeConnections.TryRemove(connectionId, out _))
        {
            Out.WriteLine($"Flagged connection [{connectionId}] as free.");
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of connections.
    /// </summary>
    public static int MaxConnections
    {
        get => _maxConnections;
        set => _maxConnections = value;
    }

    /// <summary>
    /// Gets the total number of accepted connections since init.
    /// </summary>
    public static int AcceptedConnections => _acceptedConnections;

    /// <summary>
    /// Gets the current number of active connections.
    /// </summary>
    public static int ActiveConnectionCount => _activeConnections.Count;
}
