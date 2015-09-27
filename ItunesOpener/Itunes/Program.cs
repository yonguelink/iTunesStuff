using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Itunes
{
    class Program
    {
        public static iTunesLib.iTunesApp ituneApp;
        static void Main(string[] args)
        {
            ituneApp = new iTunesLib.iTunesApp();
            iTunesLib.IITSourceCollection sources = ituneApp.Sources;
            iTunesLib.IITPlaylistCollection playlists = sources.ItemByName["Library"].Playlists;
            iTunesLib.IITPlaylist lib = playlists.ItemByName[args[0]];
            lib.Shuffle = true;
            lib.PlayFirstTrack();
            ituneApp.OnQuittingEvent += new iTunesLib._IiTunesEvents_OnQuittingEventEventHandler(onQuit);
        }

        private static void onQuit()
        {
            ituneApp.Quit();
        }
    }
}
