using System;
using System.Threading;
using System.Threading.Tasks;

using Holo.Managers;
using Holo.Protocol;
using Holo.Data.Repositories.Rooms;

namespace Holo.Virtual.Rooms;

/// <summary>
/// Partial class for virtualRoom containing special publicroom cast methods (disco lights, cameras, etc).
/// </summary>
public partial class virtualRoom
{
    #region Special publicroom additions
    /// <summary>
    /// Async task that handles special casts such as disco lamps etc in the virtual room.
    /// </summary>
    /// <param name="model">The room model name.</param>
    /// <param name="cancellationToken">Cancellation token for stopping the task.</param>
    private async Task HandleSpecialCastsAsync(string model, CancellationToken cancellationToken)
    {
        try
        {
            string Emitter = RoomModelRepository.Instance.GetSpecialCastEmitter(model) ?? "";
            int[] numData = RoomModelRepository.Instance.GetSpecialCastSettings(model);
            int Interval = numData[0];
            int rndMin = numData[1];
            int rndMax = numData[2];

            string prevCast = "";
            var random = new Random();

            while (!cancellationToken.IsCancellationRequested)
            {
                string Cast = "";
                int RND = random.Next(rndMin, rndMax + 1);

                // Generate cast based on emitter type
                while (Cast == "")
                {
                    if (Emitter == "cam1") // User camera system
                    {
                        switch (RND)
                        {
                            case 1:
                                int roomUID = getRandomRoomIdentifier();
                                if (roomUID != -1)
                                    Cast = "targetcamera " + roomUID;
                                break;
                            case 2:
                                Cast = "setcamera 1";
                                break;
                            case 3:
                                Cast = "setcamera 2";
                                break;
                        }
                    }
                    else if (Emitter == "sf") // Flashing dancetiles system
                        Cast = RND.ToString();
                    else if (Emitter == "lamp") // Discolights system
                        Cast = "setlamp " + RND;

                    if (Cast == "")
                        RND = random.Next(rndMin, rndMax + 1);
                }

                if (Cast != prevCast) // Cast is not the same as previous cast
                {
                    sendSpecialCast(Emitter, Cast);
                    prevCast = Cast;
                }

                await Task.Delay(Interval, cancellationToken);
                Out.WriteTrace("Special cast loop");
            }
        }
        catch (OperationCanceledException)
        {
            // Task was cancelled, normal shutdown
        }
        catch { }
    }
    #endregion
}
