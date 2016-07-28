using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using iTunesLib;
using System.IO;
using System.Text.RegularExpressions;

namespace ItunesLauncher
{
    // ReSharper disable once InconsistentNaming
    internal class iTunesLauncher
    {
        public static iTunesApp ItuneApp;

        private static void Main(string[] args)
        {
            ItuneApp = new iTunesApp();
            ItuneApp.OnQuittingEvent += OnQuit;
            if (args.Length > 1 && args[0] == "--save" && args[1].Length > 1)
            {
                var newPath = args[1];
                SavePlayLists(ItuneApp, newPath);
                return;
            }
            var sources = ItuneApp.Sources;
            var playlists = sources.ItemByName["Library"].Playlists;
            var playlistName = args.Length > 0 ? args[0] : "...";
            playlistName = GetValidPlaylistName(playlistName, playlists);
            
            var lib = playlists.ItemByName[playlistName];
            lib.Shuffle = true;
            lib.PlayFirstTrack();
        }

        private static void SavePlayLists(iTunesApp ituneApp, string newPath)
        {
            var form = new Form
            {
                StartPosition = FormStartPosition.CenterScreen,
                Width = 250,
                Height = 60,
                Text = "Do not close me"
            };
            form.Show();
            var text = new Label
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 250
            };
            var count = 0;
            form.Controls.Add(text);

            if (newPath.LastIndexOf(@"\", StringComparison.Ordinal) == newPath.Length-1)
            {
                newPath = newPath.Substring(0, newPath.LastIndexOf(@"\", StringComparison.Ordinal));
            }

            var songDict = GetLocationAndNewPath(ituneApp, newPath, text);
            
            foreach(var keyVal in songDict)
            {
                var location = keyVal.Key;
                var newLocation = keyVal.Value;
                Directory.CreateDirectory(newLocation.Substring(0, newLocation.LastIndexOf(@"\", StringComparison.Ordinal)));
                try
                {
                    File.Copy(location, newLocation, true);
                }
                catch (UnauthorizedAccessException)
                {
                    throw new UnauthorizedAccessException($"The file {newLocation} already exists. Its highly suggested to do a backup in an empty/unexisting directory");
                }
                
                ++count;
                text.Text = $"Creating a backup... {count}/{songDict.Count} completed.";
                form.Refresh();
            }
        }

        private static string MakeValidFileName(string name)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return Regex.Replace(name, invalidRegStr, "_");
        }

        private static Dictionary<string, string> GetLocationAndNewPath(iTunesApp iTunesApp, string newPath, Label label)
        {
            var playlists = iTunesApp.Sources.ItemByName["Library"].Playlists;
            var songDict = new Dictionary<string, string>();
            for (var i = 0; i < playlists.Count; ++i)
            {
                var playlist = playlists[i + 1];


                var name = playlist.Name;
                if (name != "Library") continue;
                var tracks = playlist.Tracks;
                var count = 0;
                label.Text = $"{count} of {tracks.Count} files prepared.";
                label.Refresh();
                for (var j = 0; j < tracks.Count; ++j)
                {
                    var song = tracks[j + 1];
                    if (ITTrackKind.ITTrackKindFile == song.Kind)
                    {
                        var track = (IITFileOrCDTrack)song;
                        var location = track.Location;
                        var title = song.Name;
                        var artist = song.Artist;
                        var album = song.Album;
                        if(location.IndexOf("monstercat", 0, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            artist = "Monstercat";
                        }
                        if(title == null)
                        {
                            throw new Exception("The song must have a title");
                        }
                        if (artist == null)
                        {
                            var path = location.Substring(0, location.LastIndexOf(@"\", StringComparison.Ordinal));
                            var pathIndex = path.LastIndexOf(@"\", StringComparison.Ordinal) + 1;
                            artist = location.Substring(pathIndex, location.LastIndexOf(@"\", StringComparison.Ordinal)-pathIndex);
                        }
                        if(album == null)
                        {
                            album = artist;
                        }
                        title = MakeValidFileName(title);
                        artist = MakeValidFileName(artist);
                        album = MakeValidFileName(album);
                        var lastDot = location.LastIndexOf(".", StringComparison.Ordinal);
                        var ext = location.Substring(lastDot);
                        var newSongPath = $@"{newPath}\{artist}\{album}\{title}{ext}";
                        if (!songDict.ContainsKey(location))
                        {
                            songDict.Add(location, newSongPath);
                        }

                    }
                    ++count;
                    label.Text = $"{count} of {tracks.Count} files prepared.";
                    label.Refresh();
                }
            }
            return songDict;
        }

        private static int _attempts = -1;
        private static string GetValidPlaylistName(string playlistName, IITPlaylistCollection playlists)
        {
            while (++_attempts < 3)
            {
                var playlist = new object[playlists.Count - 13];
                for (var i = 0; i + 13 < playlists.Count; ++i)
                {
                    var plays = playlists[i + 14];
                    playlist[i] = plays.Name;
                    if (plays.Name.Equals(playlistName, StringComparison.CurrentCultureIgnoreCase))
                        return plays.Name;
                }
                playlistName = ShowDialog("Select one playlist from the dropdown.", "Playlist selector", playlist, playlistName);
            }
            MessageBox.Show("Attempted 3 times with bad playlist name, exiting.", "Exiting", MessageBoxButtons.OK);
            Environment.Exit(0);
            throw new Exception("This thing didn't exit.");
        }

        private static void OnQuit()
        {
            ItuneApp.Quit();
        }

        public static string ShowDialog(string text, string caption, object[] playlists, string playlistName)
        {
            var prompt = new Form()
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            var textLabel = new Label() { Left = 50, Top = 20, Text = text , Width = 400};
            var comboBox = new ComboBox() {Left = 50, Top = 50, Width = 400};
            comboBox.Items.AddRange(playlists);
            comboBox.SelectedText = playlistName;
            if (comboBox.SelectedText == "") comboBox.SelectedItem = playlists[0];
            var confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 80, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(comboBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? comboBox.SelectedItem.ToString() : "";
        }
    }
}
