using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using iTunesLib;

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
            var sources = ItuneApp.Sources;
            var playlists = sources.ItemByName["Library"].Playlists;
            var playlistName = args.Length > 0 ? args[0] : "...";
            playlistName = GetValidPlaylistName(playlistName, playlists);
            
            var lib = playlists.ItemByName[playlistName];
            lib.Shuffle = true;
            lib.PlayFirstTrack();
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
