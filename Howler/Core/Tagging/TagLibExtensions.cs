using TagLib;
using TagLib.Flac;
using TagLib.Ogg;

namespace Howler.Core.Tagging
{
    public static class TagLibExtensions
    {
        public static int GetRating(this Tag tag)
        {
            int rating = 0;
            int playcount;
            var id3V2Tag = tag as TagLib.Id3v2.Tag;
            if (id3V2Tag != null)
                ID3v2Tagger.GetRatingAndPlayCount(id3V2Tag, out rating, out playcount);
            else
            {
                var xiphCommentTag = tag as XiphComment;
                if (xiphCommentTag != null)
                    OggTagger.GetRatingAndPlayCount(xiphCommentTag, out rating, out playcount);
            }
            return rating;
        }

        public static string GetDate(this Tag tag)
        {
            Tag dateTag = tag;
            var combinedTag = tag as CombinedTag;
            if (combinedTag != null)
            {
                var tags = combinedTag.Tags;
                foreach (Tag subTag in tags)
                {
                    var subXiphCommentTag = subTag as XiphComment;
                    if (subXiphCommentTag != null)
                    {
                        dateTag = subTag;
                        break;
                    }

                    var subFlacMetadataTag = subTag as Metadata;
                    if (subFlacMetadataTag != null)
                    {
                        dateTag = subFlacMetadataTag.GetComment(false, null);
                        break;
                    }

                    var subId3V2Tag = subTag as TagLib.Id3v2.Tag;
                    if (subId3V2Tag != null)
                        dateTag = subTag;
                }
            }

            var xiphCommentTag = dateTag as XiphComment;
            if (xiphCommentTag != null)
                return OggTagger.GetDate(xiphCommentTag);

            var id3V2Tag = dateTag as TagLib.Id3v2.Tag;
            if (id3V2Tag != null)
                return ID3v2Tagger.GetDate(id3V2Tag);

            return "";
        }
    }
}
