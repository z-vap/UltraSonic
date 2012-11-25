﻿using Subsonic.Rest.Api;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace UltraSonic
{
    public partial class MainWindow
    {
        private void PlayNextTrack()
        {
            Dispatcher.Invoke(() =>
            {
                bool playNextTrack = false;
                int playlistTrack = _nowPlayingTrack != null ? _playlistTrackItems.IndexOf(_nowPlayingTrack) : PlaylistTrackGrid.SelectedIndex;

                if (playlistTrack == _playlistTrackItems.Count - 1)
                {
                    if (_repeatPlaylist)
                    {
                        if (_playbackFollowsCursor)
                            PlaylistTrackGrid.SelectedIndex = 0;

                        playNextTrack = true;
                    }
                }
                else
                {
                    if (_playbackFollowsCursor)
                        PlaylistTrackGrid.SelectedIndex = playlistTrack + 1;

                    playNextTrack = true;
                }

                StopMusic();

                if (playNextTrack)
                    PlayTrack(_playlistTrackItems[playlistTrack + 1]);
            });
        }

        private void PlayPreviousTrack()
        {
            Dispatcher.Invoke(() =>
            {
                int playlistTrack = _nowPlayingTrack != null ? _playlistTrackItems.IndexOf(_nowPlayingTrack) : PlaylistTrackGrid.SelectedIndex;

                if (_playbackFollowsCursor)
                    PlaylistTrackGrid.SelectedIndex--;

                StopMusic();

                PlayTrack(_playlistTrackItems[playlistTrack - 1]);
            });
        }

        private void PlayMusic()
        {
            if (MediaPlayer.Source != null)
                MediaPlayer.Play();

            MusicPlayStatusLabel.Content = "Playing";
        }

        private void PauseMusic()
        {
            if (MediaPlayer.Source != null)
                MediaPlayer.Pause();

            MusicPlayStatusLabel.Content = "Paused";
        }

        private void StopMusic()
        {
            if (MediaPlayer.Source != null)
            {
                MediaPlayer.Stop();
                MediaPlayer.Position = TimeSpan.FromSeconds(0);
                MediaPlayer.Source = null;
                _nowPlayingTrack = null;
                _position = TimeSpan.FromSeconds(0);
                _currentAlbumArt = null;
                MusicCoverArt.Source = null;
                MusicArtistLabel.Text = null;
                MusicAlbumLabel.Text = null;
                MusicTitleLabel.Text = null;
                ProgressSlider.Value = MediaPlayer.Position.TotalMilliseconds;
                MusicTimeRemainingLabel.Content = string.Format("{0:mm\\:ss} / {1:mm\\:ss}", TimeSpan.FromMilliseconds(MediaPlayer.Position.TotalMilliseconds), TimeSpan.FromMilliseconds(_position.TotalMilliseconds));
                UpdateTitle();

            }

            MusicPlayStatusLabel.Content = "Stopped";
        }

        private void QueueTrack(TrackItem trackItem)
        {
            if (_streamItems == null) return;

            Child child = trackItem.Child;
            string fileName = GetMusicFilename(child);
            Uri fileNameUri = new Uri(fileName);

            if (_streamItems.All(s => s.OriginalString == fileName) && IsTrackCached(fileName, child))
            {
                QueueTrack(fileNameUri, trackItem);
                UpdateAlbumArt(child);
            }
            else
            {
                _streamItems.Enqueue(fileNameUri);
                UpdateAlbumArt(child);

                if (_useDiskCache)
                {
                    DownloadStatusLabel.Content = string.Format("Caching: {0}", child.Title);
                    Task<long> streamTask = SubsonicApi.StreamAsync(child.Id, fileName, _maxBitrate == 0 ? null : (int?)_maxBitrate, null, null, null, null, GetCancellationToken("QueueTrack"));
                    streamTask.ContinueWith(t => QueueTrack(streamTask, trackItem));
                }
                else
                {
                    QueueTrack(new Uri(SubsonicApi.BuildStreamUrl(child.Id)), trackItem); // Works with non-SSL servers
                }
            }
        }

        private void QueueTrack(Uri uri, TrackItem trackItem)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    StopMusic();
                    MediaPlayer.Source = uri;
                    ProgressSlider.Value = 0;
                    _nowPlayingTrack = trackItem;
                    PlayMusic();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("{0}\n{1}", ex.Message, ex.StackTrace), string.Format("Exception in {0}", AppName), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private void PlayTrack(TrackItem trackItem)
        {
            UpdateAlbumArt(trackItem.Child);
            QueueTrack(trackItem);
        }
    }
}