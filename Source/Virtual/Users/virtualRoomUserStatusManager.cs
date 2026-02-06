using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Holo.Managers;
using Holo.Virtual.Rooms;

namespace Holo.Virtual.Users;

/// <summary>
/// Provides management for the statuses of a virtual user.
/// </summary>
public class virtualRoomUserStatusManager
{
    #region Declares
    /// <summary>
    /// The ID of the user that uses this status manager.
    /// </summary>
    private int userID;
    /// <summary>
    /// The ID of the room the user that uses this status manager is in.
    /// </summary>
    private int roomID;
    /// <summary>
    /// Contains the status strings.
    /// </summary>
    private Dictionary<string, string> _Statuses;
    /// <summary>
    /// Cancellation token source for the item carrier task.
    /// </summary>
    private CancellationTokenSource? _itemCarrierCts;
    #endregion

    #region Constructors/destructors
    public virtualRoomUserStatusManager(int userID, int roomID)
    {
        this.userID = userID;
        this.roomID = roomID;
        _Statuses = new Dictionary<string, string>();
    }
    /// <summary>
    /// Empties the status manager and destructs all inside objects.
    /// </summary>
    internal void Clear()
    {
        try
        {
            _itemCarrierCts?.Cancel();
            _Statuses.Clear();
            _Statuses = null;
        }
        catch { }
    }
    #endregion

    #region Partner objects
    /// <summary>
    /// The parent virtualUser object of this status manager.
    /// </summary>
    private virtualUser User
    {
        get
        {
            return userManager.getUser(userID);
        }
    }
    /// <summary>
    /// The virtualRoom object where the parent virtual user of this status manager is in.
    /// </summary>
    private virtualRoom Room
    {
        get
        {
            return roomManager.getRoom(roomID);
        }
    }
    /// <summary>
    /// The virtualRoomUser object of the parent virtual user of this status manager.
    /// </summary>
    private virtualRoomUser roomUser
    {
        get
        {
            return userManager.getUser(userID).roomUser;
        }
    }
    #endregion

    #region Status management
    /// <summary>
    /// Adds a status key and a value to the status manager. If the status is already inside, then the previous one will be removed.
    /// </summary>
    /// <param name="Key">The key of the status.</param>
    /// <param name="Value">The value of the status.</param>
    internal void addStatus(string Key, string Value)
    {
        if (_Statuses.ContainsKey(Key))
            _Statuses.Remove(Key);
        _Statuses.Add(Key, Value);
    }
    /// <summary>
    /// Removes a certain status from the status manager.
    /// </summary>
    /// <param name="Key">The key of the status to remove.</param>
    internal void removeStatus(string Key )
    {
        try
        {
            if (_Statuses.ContainsKey(Key))
                _Statuses.Remove(Key);
        }
        catch { }
    }
    /// <summary>
    /// Returns a bool that indicates if a certain status is in the status manager.
    /// </summary>
    /// <param name="Key">The key of the status to check.</param>
    internal bool containsStatus(string Key)
    {
        return _Statuses.ContainsKey(Key);
    }
    /// <summary>
    /// Refreshes the status of the parent virtual user in the virtual room.
    /// </summary>
    internal void Refresh()
    {
        roomUser.Refresh();
    }
    /// <summary>
    /// Returns the status string of all the statuses currently in the status manager.
    /// </summary>
    public override string ToString()
    {
        string Output = "";
        foreach(string Key in _Statuses.Keys)
        {
            Output += Key;
            string Value = _Statuses[Key];
            if(Value != "")
                Output += " " + Value;
            Output += "/";
        }

        return Output;
    }
    #endregion

    #region Statuses
    /// <summary>
    /// Makes the user carry a drink/item in the virtual room. Starts an async task that uses config-defined values. The task will handle the animations of the sips etc, and finally the drop.
    /// </summary>
    /// <param name="Item">The item to carry.</param>
    internal void carryItem(string Item)
    {
        dropCarrydItem();
        _Statuses.Remove("dance");
        _itemCarrierCts = new CancellationTokenSource();
        _ = ItemCarrierLoopAsync(Item, _itemCarrierCts.Token);
    }
    /// <summary>
    /// Immediately stops carrying an item.
    /// </summary>
    internal void dropCarrydItem()
    {
        _itemCarrierCts?.Cancel();
        removeStatus("carryd");
        removeStatus("drink");
    }

    #endregion

    #region Status handlers
    /// <summary>
    /// Adds a status, keeps it for a specified amount of time [in ms] and removes the status again. Refreshes at add and remove.
    /// </summary>
    /// <param name="Key"></param>
    /// <param name="Value"></param>
    /// <param name="Length"></param>
    internal void handleStatus(string Key, string Value, int Length)
    {
        if (_Statuses.ContainsKey(Key))
            _Statuses.Remove(Key);
        _ = HandleStatusAsync(Key, Value, Length);
    }

    private async Task HandleStatusAsync(string Key, string Value, int Length)
    {
        try
        {
            _Statuses.Add(Key, Value);
            roomUser.Refresh();
            await Task.Delay(Length);
            _Statuses.Remove(Key);
            roomUser.Refresh();
        }
        catch { }
    }

    /// <summary>
    /// Async task that handles the carrying and drinking of an item in the virtual room.
    /// </summary>
    /// <param name="carrydItem">The item being carried.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task ItemCarrierLoopAsync(string carrydItem, CancellationToken cancellationToken)
    {
        try
        {
            for (int i = 1; i <= Config.Statuses_itemCarrying_SipAmount && !cancellationToken.IsCancellationRequested; i++)
            {
                addStatus("carryd", carrydItem);
                roomUser.Refresh();
                await Task.Delay(Config.Statuses_itemCarrying_SipInterval, cancellationToken);

                _Statuses.Remove("carryd");

                addStatus("drink", carrydItem);
                roomUser.Refresh();
                await Task.Delay(Config.Statuses_itemCarrying_SipDuration, cancellationToken);

                _Statuses.Remove("drink");
            }
            roomUser.Refresh();
        }
        catch (OperationCanceledException)
        {
            // Item was dropped
        }
        catch { }
    }

    internal void showTalkAnimation(int talkTime, string talkGesture)
    {
        _ = HandleStatusAsync("talk", "", talkTime);
        if (!string.IsNullOrEmpty(talkGesture))
            showGesture(talkGesture, talkTime + 3000);
    }

    internal void showGesture(string theGesture, int timeToShow)
    {
        _ = HandleStatusAsync("gest", theGesture, timeToShow);
    }
    #endregion
}
