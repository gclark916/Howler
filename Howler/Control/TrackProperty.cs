using System.ComponentModel;

namespace Howler.Control
{
    internal enum TrackProperty
    {
        Album,
        [Description("Album Artist")]
        AlbumArtist, 
        Artist,
        [Description("BPM")]
        Bpm, 
        Bitrate,
        [Description("Bits per sample")]
        BitsPerSample,
        [Description("Channels")]
        ChannelCount, 
        Codec, 
        Date,
        [Description("Date Added")]
        DateAdded,
        [Description("Last Played")]
        DateLastPlayed,
        [Description("Disc #")]
        DiscNumber, 
        Duration, 
        Genre, 
        MusicBrainzReleaseId, 
        MusicBrainzAlbumId, 
        Path, 
        Playcount, 
        Rating,
        [Description("Sample rate")]
        SampleRate, 
        Size, 
        Summary, 
        Title,
        [Description("Track #")]
        TrackNumber
    }
}