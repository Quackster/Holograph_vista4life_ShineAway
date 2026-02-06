using System;
using System.Threading;
using System.Threading.Tasks;

using Holo.Managers;
using Holo.Network;

namespace Holo
{
    /// <summary>
    /// The core of Holograph Emulator codename "Eucalypt", contains Main() void for booting server, plus monitoring thread and shutdown void.
    /// </summary>
    public class Eucalypt
    {
        private static CancellationTokenSource? _monitorCts;
        public delegate void commonDelegate();

        /// <summary>
        /// Starts up Holograph Emulator codename "Eucalypt".
        /// </summary>
        private static async Task Main()
        {
            try
            {
                Console.WindowHeight = Console.LargestWindowHeight - 25;
                Console.WindowWidth = Console.LargestWindowWidth - 25;
            }
            catch
            {
                // Ignore on non-Windows platforms
            }

            Console.CursorVisible = false;
            Console.Title = "Holograph Emulator";
            Out.WritePlain("HOLOGRAPH EMULATOR");
            Out.WritePlain("THE FREE OPEN-SOURCE HABBO HOTEL EMULATOR");
            Out.WritePlain("COPYRIGHT (C) 2007-2008 BY HOLOGRAPH TEAM");
            Out.WritePlain("FOR MORE DETAILS CHECK UPDATE.TXT");
            Out.WriteBlank();
            Out.WritePlain("BUILD");
            Out.WritePlain(" CORE: Eucalypt, .NET 10 + DotNetty");
            Out.WritePlain(" CLIENT: V26");
            Out.WritePlain(" STABLE CLIENT: V26+");
            Out.WritePlain(" Vista4life's & Shine-Away's V26 Source");
            Out.WriteBlank();

            await BootAsync();
        }

        /// <summary>
        /// Boots the emulator asynchronously.
        /// </summary>
        private static async Task BootAsync()
        {
            ThreadPool.SetMaxThreads(300, 400);
            DateTime _START = DateTime.Now;
            Out.WriteLine("Starting up Holograph Emulator for " + Environment.UserName + "...");
            Out.WriteLine("Expanded threadpool.");
            Out.WriteLine(@"Checking for \bin\mysql.ini...");

            string sqlConfigLocation = IO.workingDirectory + @"\bin\mysql.ini";
            if (System.IO.File.Exists(sqlConfigLocation) == false)
            {
                Out.WriteError("mysql.ini not found at " + sqlConfigLocation);
                await ShutdownAsync();
                return;
            }

            Out.WriteLine("mysql.ini found at " + sqlConfigLocation);
            Out.WriteBlank();

            string dbHost = IO.readINI("mysql", "host", sqlConfigLocation);
            int dbPort = int.Parse(IO.readINI("mysql", "port", sqlConfigLocation));
            string dbUsername = IO.readINI("mysql", "username", sqlConfigLocation);
            string dbPassword = IO.readINI("mysql", "password", sqlConfigLocation);
            string dbName = IO.readINI("mysql", "database", sqlConfigLocation);

            if (DB.openConnection(dbHost, dbPort, dbName, dbUsername, dbPassword) == false)
                return;

            Out.WriteBlank();

            int gamePort;
            int gameMaxConnections;
            int musPort;
            string musHost;

            try
            {
                gamePort = int.Parse(Config.getTableEntry("server_game_port"));
                gameMaxConnections = int.Parse(Config.getTableEntry("server_game_maxconnections"));
                musPort = int.Parse(Config.getTableEntry("server_mus_port"));
                musHost = Config.getTableEntry("server_mus_host");
            }
            catch
            {
                Out.WriteError("system_config table contains invalid values for socket server configuration!");
                await ShutdownAsync();
                return;
            }

            string langExt = Config.getTableEntry("lang");
            if (langExt == "")
            {
                Out.WriteError("No valid language extension [field: lang] was set in the system_config table!");
                await ShutdownAsync();
                return;
            }

            stringManager.Init(langExt);
            Out.WriteBlank();

            stringManager.initFilter();
            Out.WriteBlank();

            catalogueManager.Init();
            Out.WriteBlank();

            recyclerManager.Init();
            Out.WriteBlank();

            rankManager.Init();
            Out.WriteBlank();

            Config.Init();
            Out.WriteBlank();

            userManager.Init();
            eventManager.Init();

            if (await GameSocketServer.InitAsync(gamePort, gameMaxConnections) == false)
            {
                await ShutdownAsync();
                return;
            }
            Out.WriteBlank();

            if (await MusSocketServer.InitAsync(musPort, musHost) == false)
            {
                await ShutdownAsync();
                return;
            }
            Out.WriteBlank();

            ResetDynamics();
            Out.WriteBlank();

            PrintDatabaseStats();
            Out.WriteBlank();

            DateTime _STOP = DateTime.Now;
            TimeSpan _TST = _STOP - _START;
            Out.WriteLine("Startup time in fixed milliseconds: " + _TST.TotalMilliseconds.ToString() + ".");

            GC.Collect();
            Out.WriteLine("Holograph Emulator ready. Status: idle");
            Out.WriteBlank();

            Out.minimumImportance = Out.logFlags.MehAction; // All logs

            // Start server monitor as a background task
            _monitorCts = new CancellationTokenSource();
            _ = MonitorServerAsync(_monitorCts.Token);

            // Keep the application running
            Console.WriteLine("Press Ctrl+C to shutdown...");
            var tcs = new TaskCompletionSource<bool>();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                tcs.TrySetResult(true);
            };

            await tcs.Task;
            await ShutdownAsync();
        }

        /// <summary>
        /// Safely shutdowns Holograph Emulator, closing database and socket connections.
        /// </summary>
        public static async Task ShutdownAsync()
        {
            Out.WriteBlank();

            // Cancel the monitor
            _monitorCts?.Cancel();

            // Shutdown socket servers
            await GameSocketServer.ShutdownAsync();
            await MusSocketServer.ShutdownAsync();

            DB.closeConnection();
            Out.WriteLine("Shutdown completed.");

            try
            {
                Console.Beep(1400, 1000);
            }
            catch
            {
                // Ignore on non-Windows platforms
            }

            Environment.Exit(0);
        }

        /// <summary>
        /// Legacy shutdown method for compatibility.
        /// </summary>
        public static void Shutdown()
        {
            ShutdownAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Prints the usercount, guestroomcount and furniturecount in database to console.
        /// </summary>
        private static void PrintDatabaseStats()
        {
            int userCount = DB.runRead("SELECT COUNT(*) FROM users", null);
            int roomCount = DB.runRead("SELECT COUNT(*) FROM rooms", null);
            int itemCount = DB.runRead("SELECT COUNT(*) FROM furniture", null);
            Out.WriteLine("Result: " + userCount + " users, " + roomCount + " rooms and " + itemCount + " furnitures.");
        }

        private static void ResetDynamics()
        {
            DB.runQuery("UPDATE system SET onlinecount = '0',onlinecount_peak = '0',connections_accepted = '0',activerooms = '0'");
            DB.runQuery("UPDATE users SET ticket_sso = NULL");
            DB.runQuery("UPDATE rooms SET visitors_now = '0'");

            Out.WriteLine("Client connection statistics reset.");
            Out.WriteLine("Room inside counts reset.");
            Out.WriteLine("Login tickets nulled.");
        }

        /// <summary>
        /// Async server monitor. Updates console title and statistics in database.
        /// </summary>
        private static async Task MonitorServerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    int onlineCount = userManager.userCount;
                    int peakOnlineCount = userManager.peakUserCount;
                    int roomCount = roomManager.roomCount;
                    int peakRoomCount = roomManager.peakRoomCount;
                    int acceptedConnections = GameSocketServer.AcceptedConnections;
                    long memUsage = GC.GetTotalMemory(false) / 1024;

                    Console.Title = "Holograph Emulator 26 | online users: " + onlineCount + " | loaded rooms " + roomCount + " | RAM usage: " + memUsage + "KB";
                    DB.runQuery("UPDATE system SET onlinecount = '" + onlineCount + "',onlinecount_peak = '" + peakOnlineCount + "',activerooms = '" + roomCount + "',activerooms_peak = '" + peakRoomCount + "',connections_accepted = '" + acceptedConnections + "'");

                    await Task.Delay(6000, cancellationToken);
                    Out.WriteTrace("Servermonitor loop");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Out.WriteError($"Monitor error: {ex.Message}");
                }
            }
        }
    }
}
