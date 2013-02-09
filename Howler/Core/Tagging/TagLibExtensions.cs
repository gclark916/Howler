using TagLib;
using TagLib.Ogg;

namespace Howler.Core.Tagging
{
    public static class TagLibExtensions
    {
        public static int GetRating(this Tag tag)
        {
            int rating = 0;
            int playcount;
            if (tag is TagLib.Id3v2.Tag)
                ID3v2Tagger.GetRatingAndPlayCount((TagLib.Id3v2.Tag)tag, out rating, out playcount);
            else if (tag is XiphComment)
                OggTagger.GetRatingAndPlayCount((XiphComment)tag, out rating, out playcount);
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
                    if (subTag is XiphComment)
                    {
                        dateTag = tag;
                        break;
                    }
                    if (subTag is TagLib.Flac.Metadata)
                    {
                        dateTag = ((TagLib.Flac.Metadata) subTag).GetComment(false, null);
                        break;
                    }
                    if (subTag is TagLib.Id3v2.Tag)
                        dateTag = tag;
                }
            }

            if (dateTag is XiphComment)
                return OggTagger.GetDate((XiphComment)dateTag);
            if (dateTag is TagLib.Id3v2.Tag)
                return ID3v2Tagger.GetDate((TagLib.Id3v2.Tag)dateTag);

            return "";
        }
    }
}
