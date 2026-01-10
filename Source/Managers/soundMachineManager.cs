using System;
using System.Text;
using HolographEmulator.Infrastructure.DataAccess;

namespace HolographEmulator.Infrastructure.Managers
{
    /// <summary>
    /// Provides management for virtual sound machines and virtual songs.
    /// </summary>
    public static class SoundMachineManager
    {
        private static readonly SoundMachineDataAccess _soundMachineDataAccess = new SoundMachineDataAccess();
        private static readonly UserDataAccess _userDataAccess = new UserDataAccess();
        /// <summary>
        /// Returns the string with all the soundsets in the Hand of a certain user.
        /// </summary>
        /// <param name="userID">The database ID of the user to get the soundsets of.</param>
        public static string getHandSoundsets(int userID)
        {
            var itemIDs = _soundMachineDataAccess.GetUserSoundMachineItemIds(userID);
            StringBuilder Soundsets = new StringBuilder(Encoding.encodeVL64(itemIDs.Count));
            if (itemIDs.Count > 0)
            {
                var soundSets = _soundMachineDataAccess.GetUserSoundMachineSoundsets(userID);
                for (int i = 0; i < itemIDs.Count; i++)
                    Soundsets.Append(Encoding.encodeVL64(soundSets[i]));
            }
            return Soundsets.ToString();
        }
        /// <summary>
        /// Returns the length of a song in seconds as an integer. The length is calculated by counting the notes on the four tracks, if an error occurs here, then -1 is returned as length.
        /// </summary>
        /// <param name="Data">The songdata. (all 4 tracks)</param>
        public static int calculateSongLength(string Data)
        {
            int songLength = 0;
            try
            {
                string[] Track = Data.Split(':');
                for (int i = 1; i < 8; i += 3)
                {
                    int trackLength = 0;
                    string[] Samples = Track[i].Split(';');
                    for (int j = 0; j < Samples.Length; j++)
                        trackLength += int.Parse(Samples[j].Substring(Samples[j].IndexOf(",") + 1));

                    if (trackLength > songLength)
                        songLength = trackLength;
                }
                return songLength;
            }
            catch { return -1; }
        }

        public static string getMachineSongList(int machineID)
        {
            var IDs = _soundMachineDataAccess.GetMachineSongIds(machineID);
            StringBuilder Songs = new StringBuilder(Encoding.encodeVL64(IDs.Count));
            if (IDs.Count > 0)
            {
                var Lengths = _soundMachineDataAccess.GetMachineSongLengths(machineID);
                var Titles = _soundMachineDataAccess.GetMachineSongTitles(machineID);
                var burntFlags = _soundMachineDataAccess.GetMachineSongBurntFlags(machineID);

                for (int i = 0; i < IDs.Count; i++)
                {
                    Songs.Append(Encoding.encodeVL64(IDs[i]) + Encoding.encodeVL64(Lengths[i]) + Titles[i] + Convert.ToChar(2));
                    if (burntFlags[i] == "1")
                        Songs.Append("I");
                    else
                        Songs.Append("H");
                }
            }
            return Songs.ToString();
        }
        public static string getMachinePlaylist(int machineID)
        {
            var IDs = _soundMachineDataAccess.GetMachinePlaylistSongIds(machineID);
            StringBuilder Playlist = new StringBuilder("H" + Encoding.encodeVL64(IDs.Count));
            if (IDs.Count > 0)
            {
                for (int i = 0; i < IDs.Count; i++)
                {
                    string Title = _soundMachineDataAccess.GetSongTitle(IDs[i]);
                    int userId = _soundMachineDataAccess.GetSongUserId(IDs[i]);
                    string Creator = _userDataAccess.GetUsername(userId);
                    Playlist.Append(Encoding.encodeVL64(IDs[i]) + Encoding.encodeVL64(i + 1) + Title + Convert.ToChar(2) + Creator + Convert.ToChar(2));
                }
            }
            return Playlist.ToString();
        }
        public static string getSong(int songID)
        {
            var songData = _soundMachineDataAccess.GetSongData(songID);
            if (songData != null)
                return Encoding.encodeVL64(songID) + songData.Title + Convert.ToChar(2) + songData.Data + Convert.ToChar(2);
            else
                return "holo.cast.soundmachine.song.unknown";
        }
    }
}
