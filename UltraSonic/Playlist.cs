﻿using System.Windows.Controls;
using Subsonic.Rest.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using UltraSonic.Static;

namespace UltraSonic
{
    public partial class MainWindow
    {
        private void UpdatePlaylists()
        {
            if (SubsonicApi == null) return;
            SubsonicApi.GetPlaylistsAsync().ContinueWith(UpdatePlaylists, GetCancellationToken("UpdatePlaylists"));
        }

        private void UpdatePlaylists(IEnumerable<Playlist> playlists)
        {
            if (SubsonicApi == null) return;

            Dispatcher.Invoke(() =>
            {
                _playlistItems.Clear();
                
                foreach (DataGridColumn column in PlaylistsDataGrid.Columns)
                {
                    column.Width = column.MinWidth;
                    column.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
                }

                foreach (PlaylistItem playlistItem in playlists.Select(playlist => new PlaylistItem
                {
                    Duration = TimeSpan.FromSeconds(playlist.Duration),
                    Name = playlist.Name,
                    Tracks = playlist.SongCount,
                    Playlist = playlist
                }))
                {
                    _playlistItems.Add(playlistItem);
                }

                if (SubsonicApi.ServerApiVersion >= Version.Parse("1.8.0")) // Get starred tracks to create dynamic Starred playlist
                    SubsonicApi.GetStarredAsync(GetCancellationToken("UpdatePlaylists")).ContinueWith(AddStarredToPlaylists);
            });
        }

        private TrackItem AddTrackItemToPlaylist(TrackItem trackItem)
        {
            TrackItem playlistTrackItem = new TrackItem();
            trackItem.CopyTo(playlistTrackItem);
            playlistTrackItem.Source = trackItem;
            playlistTrackItem.Duration = TimeSpan.FromSeconds(playlistTrackItem.Child.Duration);
            playlistTrackItem.PlaylistGuid = Guid.NewGuid();

            Dispatcher.Invoke(() => _playlistTrackItems.Add(playlistTrackItem));

            return playlistTrackItem;
        }
    }
}
