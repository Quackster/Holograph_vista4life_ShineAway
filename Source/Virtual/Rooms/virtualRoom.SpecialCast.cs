using System;
using System.Threading;

using Holo.Managers;
using Holo.Protocol;

namespace Holo.Virtual.Rooms;

/// <summary>
/// Partial class for virtualRoom containing special publicroom cast methods (disco lights, cameras, etc).
/// </summary>
public partial class virtualRoom
{
    #region Special publicroom additions
    /// <summary>
    /// Threaded. Handles special casts such as disco lamps etc in the virtual room.
    /// </summary>
    /// <param name="o">The room model name as a System.Object.</param>
    private void handleSpecialCasts(object o)
    {
        try
        {
            string Emitter = DB.runRead("SELECT specialcast_emitter FROM room_modeldata WHERE model = '" + (string)o + "'");
            int[] numData = DB.runReadRow("SELECT specialcast_interval,specialcast_rnd_min,specialcast_rnd_max FROM room_modeldata WHERE model = '" + (string)o + "'", null);
            int Interval = numData[0];
            int rndMin = numData[1];
            int rndMax = numData[2];
            numData = null;

            string prevCast = "";
            while (true)
            {
                string Cast = "";
                int RND = new Random().Next(rndMin, rndMax + 1);

            reCast:
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
                    goto reCast;
                if (Cast != prevCast) // Cast is not the same as previous cast
                {
                    sendSpecialCast(Emitter, Cast);
                    prevCast = Cast;
                }
                Thread.Sleep(Interval);
                Out.WriteTrace("Special cast loop");
            }
        }
        catch { }
    }
    #endregion
}
