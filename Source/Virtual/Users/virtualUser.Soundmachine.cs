using System;
using System.Text;

using Holo.Data.Repositories.Furniture;
using Holo.Data.Repositories.SoundMachine;
using Holo.Data.Repositories.Users;
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
                                SoundMachineRepository.Instance.ClearPlaylist(Room.floorItemManager.soundMachineID);
                                for (int i = 0; i < Amount; i++)
                                {
                                    int songID = Encoding.decodeVL64(currentPacket);
                                    SoundMachineRepository.Instance.AddToPlaylist(Room.floorItemManager.soundMachineID, songID, i);
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
                            if (_Credits > 0 && SoundMachineRepository.Instance.SongExistsForUserAndMachine(songID, userID, Room.floorItemManager.soundMachineID))
                            {
                                string[] songData = SoundMachineRepository.Instance.GetSongTitleAndLength(songID);
                                string Status = Encoding.encodeVL64(songID) + _Username + Convert.ToChar(10) + DateTime.Today.Day + Convert.ToChar(10) + DateTime.Today.Month + Convert.ToChar(10) + DateTime.Today.Year + Convert.ToChar(10) + songData[1] + Convert.ToChar(10) + songData[0];

                                FurnitureRepository.Instance.CreateItemWithVar(Config.Soundmachine_burnToDisk_diskTemplateID, userID, Status);
                                SoundMachineRepository.Instance.SetSongBurnt(songID);
                                UserRepository.Instance.DecrementCredits(userID);

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
                            if (SoundMachineRepository.Instance.SongExistsInMachine(songID, Room.floorItemManager.soundMachineID))
                            {
                                SoundMachineRepository.Instance.RemoveSongFromMachineIfBurnt(songID); // If the song is burnt atleast once, then the song is removed from this machine
                                SoundMachineRepository.Instance.DeleteUnburntSong(songID); // If the song isn't burnt; delete song from database
                                SoundMachineRepository.Instance.RemoveFromPlaylist(Room.floorItemManager.soundMachineID, songID); // Remove song from playlist
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
                                Title = stringManager.filterSwearwords(Title);
                                SoundMachineRepository.Instance.CreateSong(userID, Room.floorItemManager.soundMachineID, Title, Length, Data);

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
                            if (SoundMachineRepository.Instance.SongExistsForUserAndMachine(songID, userID, Room.floorItemManager.soundMachineID))
                            {
                                int idLength = Encoding.encodeVL64(songID).Length;
                                int nameLength = Encoding.decodeB64(currentPacket.Substring(idLength + 2, 2));
                                string Title = currentPacket.Substring(idLength + 4, nameLength);
                                string Data = currentPacket.Substring(idLength + nameLength + 6);
                                int Length = soundMachineManager.calculateSongLength(Data);
                                if (Length != -1)
                                {
                                    Title = stringManager.filterSwearwords(Title);
                                    SoundMachineRepository.Instance.UpdateSong(songID, Title, Data, Length);

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
