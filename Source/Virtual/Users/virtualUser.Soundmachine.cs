using System;
using System.Text;

using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms;
using Holo.Virtual.Users.Items;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Partial class containing soundmachine-related packet processing.
    /// </summary>
    public partial class virtualUser
    {
        /// <summary>
        /// Processes soundmachine-related packets.
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        /// <returns>True if the packet was handled, false otherwise.</returns>
        private bool processSoundmachinePackets(string currentPacket)
        {
            switch (currentPacket.Substring(0, 2))
            {
                #region Soundmachines
                case "Ct": // Soundmachine - initialize songs in soundmachine
                    {
                        if (_isOwner && Room != null && _isOwner == true && Room.floorItemManager.soundMachineID > 0)
                            sendData(new HabboPacketBuilder("EB").Append(soundMachineManager.getMachineSongList(Room.floorItemManager.soundMachineID)).Build());
                        return true;
                    }

                case "Cu": // Soundmachine - enter room initialize playlist
                    {
                        if (Room != null && Room.floorItemManager.soundMachineID > 0)
                            sendData(new HabboPacketBuilder("EC").Append(soundMachineManager.getMachinePlaylist(Room.floorItemManager.soundMachineID)).Build());
                        return true;
                    }

                case "C]": // Soundmachine - get song title and data of certain song
                    {
                        if (Room != null && Room.floorItemManager.soundMachineID > 0)
                        {
                            int songID = Encoding.decodeVL64(currentPacket.Substring(2));
                            sendData(new HabboPacketBuilder("Dl").Append(soundMachineManager.getSong(songID)).Build());
                        }
                        return true;
                    }

                case "Cs": // Soundmachine - save playlist
                    {
                        if (_isOwner && Room != null && _isOwner == true && Room.floorItemManager.soundMachineID > 0)
                        {
                            int Amount = Encoding.decodeVL64(currentPacket.Substring(2));
                            if (Amount < 6) // Max playlist size
                            {
                                currentPacket = currentPacket.Substring(Encoding.encodeVL64(Amount).Length + 2);
                                DB.runQuery("DELETE FROM soundmachine_playlists WHERE machineid = '" + Room.floorItemManager.soundMachineID + "'");
                                for (int i = 0; i < Amount; i++)
                                {
                                    int songID = Encoding.decodeVL64(currentPacket);
                                    DB.runQuery("INSERT INTO soundmachine_playlists(machineid,songid,pos) VALUES ('" + Room.floorItemManager.soundMachineID + "','" + songID + "','" + i + "')");
                                    currentPacket = currentPacket.Substring(Encoding.encodeVL64(songID).Length);
                                }
                                Room.sendData(new HabboPacketBuilder("EC").Append(soundMachineManager.getMachinePlaylist(Room.floorItemManager.soundMachineID)).Build()); // Refresh playlist
                            }
                        }
                        return true;
                    }

                case "C~": // Sound machine - burn song to disk
                    {
                        if (_isOwner && Room != null && _isOwner == true && Room.floorItemManager.soundMachineID > 0)
                        {
                            int songID = Encoding.decodeVL64(currentPacket.Substring(2));
                            if (_Credits > 0 && DB.checkExists("SELECT id FROM soundmachine_songs WHERE id = '" + songID + "' AND userid = '" + userID + "' AND machineid = '" + Room.floorItemManager.soundMachineID + "'"))
                            {
                                string[] songData = DB.runReadRow("SELECT title,length FROM soundmachine_songs WHERE id = '" + songID + "'");
                                int Length = DB.runRead("SELECT length FROM soundmachine_songs WHERE id = '" + songID + "'", null);
                                string Status = Encoding.encodeVL64(songID) + _Username + Convert.ToChar(10) + DateTime.Today.Day + Convert.ToChar(10) + DateTime.Today.Month + Convert.ToChar(10) + DateTime.Today.Year + Convert.ToChar(10) + songData[1] + Convert.ToChar(10) + songData[0];

                                DB.runQuery("INSERT INTO furniture(tid,ownerid,var) VALUES ('" + Config.Soundmachine_burnToDisk_diskTemplateID + "','" + userID + "','" + Status + "')");
                                DB.runQuery("UPDATE soundmachine_songs SET burnt = '1' WHERE id = '" + songID + "' LIMIT 1");
                                DB.runQuery("UPDATE users SET credits = credits - 1 WHERE id = '" + userID + "' LIMIT 1");

                                _Credits--;
                                sendData(new HabboPacketBuilder("@F").Append(_Credits).Build());
                                sendData(new HabboPacketBuilder("EB").Append(soundMachineManager.getMachineSongList(Room.floorItemManager.soundMachineID)).Build());
                                refreshHand("last");
                            }
                            else // Virtual user doesn't has enough credits to burn this song to disk, or this song doesn't exist in his/her soundmachine
                                sendData("AD");
                        }
                        return true;
                    }

                case "Cx": // Sound machine - delete song
                    {
                        if (_isOwner && Room != null && _isOwner == true && Room.floorItemManager.soundMachineID > 0)
                        {
                            int songID = Encoding.decodeVL64(currentPacket.Substring(2));
                            if (DB.checkExists("SELECT id FROM soundmachine_songs WHERE id = '" + songID + "' AND machineid = '" + Room.floorItemManager.soundMachineID + "'"))
                            {
                                DB.runQuery("UPDATE soundmachine_songs SET machineid = '0' WHERE id = '" + songID + "' AND burnt = '1'"); // If the song is burnt atleast once, then the song is removed from this machine
                                DB.runQuery("DELETE FROM soundmachine_songs WHERE id = '" + songID + "' AND burnt = '0' LIMIT 1"); // If the song isn't burnt; delete song from database
                                DB.runQuery("DELETE FROM soundmachine_playlists WHERE machineid = '" + Room.floorItemManager.soundMachineID + "' AND songid = '" + songID + "'"); // Remove song from playlist
                                Room.sendData(new HabboPacketBuilder("EC").Append(soundMachineManager.getMachinePlaylist(Room.floorItemManager.soundMachineID)).Build());
                            }
                        }
                        return true;
                    }

                #region Song editor
                case "Co": // Soundmachine - song editor - initialize soundsets and samples
                    {
                        if (_isOwner && Room != null && _isOwner == true && Room.floorItemManager.soundMachineID > 0)
                        {
                            songEditor = new virtualSongEditor(Room.floorItemManager.soundMachineID, userID);
                            songEditor.loadSoundsets();
                            sendData(new HabboPacketBuilder("Dm").Append(songEditor.getSoundsets()).Build());
                            sendData(new HabboPacketBuilder("Dn").Append(soundMachineManager.getHandSoundsets(userID)).Build());
                        }
                        return true;
                    }

                case "C[": // Soundmachine - song editor - add soundset
                    {
                        if (songEditor != null && _isOwner && Room != null && _isOwner == true && Room.floorItemManager.soundMachineID > 0)
                        {
                            int soundSetID = Encoding.decodeVL64(currentPacket.Substring(2));
                            int slotID = Encoding.decodeVL64(currentPacket.Substring(Encoding.encodeVL64(soundSetID).Length + 2));
                            if (slotID > 0 && slotID < 5 && songEditor.slotFree(slotID))
                            {
                                songEditor.addSoundset(soundSetID, slotID);
                                sendData(new HabboPacketBuilder("Dn").Append(soundMachineManager.getHandSoundsets(userID)).Build());
                                sendData(new HabboPacketBuilder("Dm").Append(songEditor.getSoundsets()).Build());
                            }
                        }
                        return true;
                    }

                case @"C\": // Soundmachine - song editor - remove soundset
                    {
                        if (songEditor != null && _isOwner && Room != null && _isOwner == true && Room.floorItemManager.soundMachineID > 0)
                        {
                            int slotID = Encoding.decodeVL64(currentPacket.Substring(2));
                            if (songEditor.slotFree(slotID) == false)
                            {
                                songEditor.removeSoundset(slotID);
                                sendData(new HabboPacketBuilder("Dm").Append(songEditor.getSoundsets()).Build());
                                sendData(new HabboPacketBuilder("Dn").Append(soundMachineManager.getHandSoundsets(userID)).Build());
                            }
                        }
                        return true;
                    }

                case "Cp": // Soundmachine - song editor - save new song
                    {
                        if (songEditor != null && _isOwner && Room != null && _isOwner == true && Room.floorItemManager.soundMachineID > 0)
                        {
                            int nameLength = Encoding.decodeB64(currentPacket.Substring(2, 2));
                            string Title = currentPacket.Substring(4, nameLength);
                            string Data = currentPacket.Substring(nameLength + 6);
                            int Length = soundMachineManager.calculateSongLength(Data);

                            if (Length != -1)
                            {
                                Title = DB.Stripslash(stringManager.filterSwearwords(Title));
                                Data = DB.Stripslash(Data);
                                DB.runQuery("INSERT INTO soundmachine_songs (userid,machineid,title,length,data) VALUES ('" + userID + "','" + Room.floorItemManager.soundMachineID + "','" + Title + "','" + Length + "','" + DB.Stripslash(Data) + "')");

                                sendData(new HabboPacketBuilder("EB").Append(soundMachineManager.getMachineSongList(Room.floorItemManager.soundMachineID)).Build());
                                sendData(new HabboPacketBuilder("EK").Append(Encoding.encodeVL64(Room.floorItemManager.soundMachineID)).Append(Title).Separator().Build());
                            }
                        }
                        return true;
                    }

                case "Cq": // Soundmachine - song editor - request edit of existing song
                    {
                        if (_isOwner && Room != null && _isOwner == true && Room.floorItemManager.soundMachineID > 0)
                        {
                            int songID = Encoding.decodeVL64(currentPacket.Substring(2));
                            sendData(new HabboPacketBuilder("Dl").Append(soundMachineManager.getSong(songID)).Build());

                            songEditor = new virtualSongEditor(Room.floorItemManager.soundMachineID, userID);
                            songEditor.loadSoundsets();

                            sendData(new HabboPacketBuilder("Dm").Append(songEditor.getSoundsets()).Build());
                            sendData(new HabboPacketBuilder("Dn").Append(soundMachineManager.getHandSoundsets(userID)).Build());
                        }
                        return true;
                    }

                case "Cr": // Soundmachine - song editor - save edited existing song
                    {
                        if (songEditor != null && _isOwner && Room != null && _isOwner == true && Room.floorItemManager.soundMachineID > 0)
                        {
                            int songID = Encoding.decodeVL64(currentPacket.Substring(2));
                            if (DB.checkExists("SELECT id FROM soundmachine_songs WHERE id = '" + songID + "' AND userid = '" + userID + "' AND machineid = '" + Room.floorItemManager.soundMachineID + "'"))
                            {
                                int idLength = Encoding.encodeVL64(songID).Length;
                                int nameLength = Encoding.decodeB64(currentPacket.Substring(idLength + 2, 2));
                                string Title = currentPacket.Substring(idLength + 4, nameLength);
                                string Data = currentPacket.Substring(idLength + nameLength + 6);
                                int Length = soundMachineManager.calculateSongLength(Data);
                                if (Length != -1)
                                {
                                    Title = DB.Stripslash(stringManager.filterSwearwords(Title));
                                    Data = DB.Stripslash(Data);
                                    DB.runQuery("UPDATE soundmachine_songs SET title = '" + Title + "',data = '" + Data + "',length = '" + Length + "' WHERE id = '" + songID + "' LIMIT 1");

                                    sendData("ES");
                                    sendData(new HabboPacketBuilder("EB").Append(soundMachineManager.getMachineSongList(Room.floorItemManager.soundMachineID)).Build());
                                    Room.sendData(new HabboPacketBuilder("EC").Append(soundMachineManager.getMachinePlaylist(Room.floorItemManager.soundMachineID)).Build());
                                }
                            }
                        }
                        return true;
                    }
                #endregion Song editor
                #endregion

                default:
                    return false; // Packet was not handled
            }
            return true; // Packet was handled
        }
    }
}
