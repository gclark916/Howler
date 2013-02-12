// This file uses significant portions of the StreamRatingTagger.cs file
// from Banshee, its copyright information is below:
//
// StreamRatingTagger.cs
//
// Author:
//   Nicholas Parker <nickbp@gmail.com>
//
// Copyright (C) 2008-2009 Nicholas Parker
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Globalization;
using System.Collections;

namespace Howler.Core.Tagging
{

    // Applicable for Vorbis, Speex, and many (most?) FLAC files
    // Follows the naming standard established by the Quod Libet team
    // See: http://code.google.com/p/quodlibet/wiki/Specs_VorbisComments
    internal static class OggTagger
    {
        // What we call ourselves in rating/playcount tags.
        private const string HowlerName = "HOWLER";

        // Prefix to rating field names (lowercase)
        private const string RatingPrefix = "RATING:";

        private const string MediaMonkeyRatingField = "RATING";

        // Prefix to playcount field names (lowercase)
        private const string PlaycountPrefix = "PLAYCOUNT:";

        // Converts Ogg rating to Banshee rating
        private static int OggToBanshee(string oggRatingStr)
        {
            double oggRating;
            if (Double.TryParse(oggRatingStr, NumberStyles.Number,
                    CultureInfo.InvariantCulture, out oggRating))
            {
                // Quod Libet Ogg ratings are stored as a value
                // between 0.0 and 1.0 inclusive, where unrated = 0.5.
                if (oggRating == 0.5)// unrated
                    return 0;
                if (oggRating > 0.8)// (0.8,1.0]
                    return 5;
                if (oggRating > 0.6)// (0.6,0.8]
                    return 4;
                if (oggRating > 0.4)// (0.4,0.5),(0.5,0.6]
                    return 3;
                if (oggRating > 0.2)// (0.2,0.4]
                    return 2;
                else // [0.0,0.2]
                    return 1;
            }

            return 0;
        }

        // Converts Banshee rating to Ogg rating
        private static string BansheeToOgg(int bansheeRating)
        {
            // I went with this scaling so that if we switch to fractional stars
            // in the future (such as "0.25 stars"), we'll have room for that.
            switch (bansheeRating)
            {
                case 1:
                    return "0.2";
                case 2:
                    return "0.4";
                case 3:
                    return "0.6";
                case 4:
                    return "0.8";
                case 5:
                    return "1.0";
                default:
                    return "0.5";// unrated/unknown
            }
        }

        // Scans the file for ogg rating/playcount tags as defined by the Quod Libet standard
        // If a Banshee tag is found, it is given priority.
        // If a Banshee tag is not found, the last rating/playcount tags found are used
        public static void GetRatingAndPlayCount(TagLib.Ogg.XiphComment xiphtag,
                                                    out int rating, out int playcount)
        {
            rating = -1;
            playcount = 0;
            bool howlerRatingDone = false, howlerPlaycountDone = false, mediaMonkeyFormat = false;
            string ratingRaw = "", playcountRaw = "";

            foreach (string fieldname in xiphtag)
            {

                if (!howlerRatingDone)
                {
                    if (fieldname.StartsWith(RatingPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        ratingRaw = xiphtag.GetFirstField(fieldname);
                        string ratingCreator = fieldname.Substring(RatingPrefix.Length);
                        if (String.Compare(ratingCreator, HowlerName, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // We made this rating, consider it authoritative.
                            howlerRatingDone = true;
                            // Don't return -- we might not have seen a playcount yet.
                        }
                    }
                    else if (String.Compare(fieldname, MediaMonkeyRatingField, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        ratingRaw = xiphtag.GetFirstField(fieldname);
                        mediaMonkeyFormat = true;
                    }

                }
                else if (!howlerPlaycountDone &&
                            fieldname.StartsWith(PlaycountPrefix, StringComparison.OrdinalIgnoreCase))
                {

                    playcountRaw = xiphtag.GetFirstField(fieldname);
                    string playcountCreator = fieldname.Substring(PlaycountPrefix.Length);
                    if (string.Compare(playcountCreator, HowlerName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // We made this playcount, consider it authoritative.
                        howlerPlaycountDone = true;
                        // Don't return -- we might not have seen a rating yet.
                    }
                }
            }
            if (!string.IsNullOrEmpty(ratingRaw))
            {
                if (howlerRatingDone || mediaMonkeyFormat)
                    rating = int.Parse(ratingRaw, CultureInfo.InvariantCulture);
                else
                {
                    int bansheeRating = OggToBanshee(ratingRaw);
                    rating = bansheeRating == 0 ? -1 : bansheeRating * 5;
                }

            }
            if (!string.IsNullOrEmpty(playcountRaw))
            {
                playcount = int.Parse(playcountRaw, CultureInfo.InvariantCulture);
            }
        }

        // Scans the file for ogg rating/playcount tags as defined by the Quod Libet standard
        // All applicable tags are overwritten with the new values, regardless of tag author
        public static void StoreRating(int rating, TagLib.Ogg.XiphComment xiphtag)
        {
            ArrayList ratingFieldnames = new ArrayList();

            // Collect list of rating tags to be updated:
            foreach (string fieldname in xiphtag)
            {
                if (fieldname.StartsWith(RatingPrefix, StringComparison.OrdinalIgnoreCase) 
                    || String.Compare(fieldname, MediaMonkeyRatingField, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    ratingFieldnames.Add(fieldname);
                }
            }
            // Add "HOWLER" tags if no rating tags were found and track is not "unrated":
            if (ratingFieldnames.Count == 0 && rating >= 0)
            {
                ratingFieldnames.Add(RatingPrefix + HowlerName);
            }

            if (rating < 0)
            {
                foreach (string ratingname in ratingFieldnames)
                    xiphtag.RemoveField(ratingname);
            }
            else
            {
                int bansheeRating = (int) Math.Ceiling(rating / 20.0);
                string bansheeRatingString = BansheeToOgg(bansheeRating);
                foreach (string ratingname in ratingFieldnames)
                {
                    if (String.Compare(ratingname, String.Concat(RatingPrefix, HowlerName), StringComparison.OrdinalIgnoreCase) == 0
                        || String.Compare(ratingname, MediaMonkeyRatingField, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        xiphtag.SetField(ratingname, rating.ToString(CultureInfo.InvariantCulture));
                    }

                    else if (ratingname.StartsWith(RatingPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        xiphtag.SetField(ratingname, bansheeRatingString);
                    }
                }
            }
        }

        public static void StorePlayCount(int playcount, TagLib.Ogg.XiphComment xiphtag)
        {
            ArrayList playcountFieldnames = new ArrayList();

            // Collect list of  playcount tags to be updated:
            foreach (string fieldname in xiphtag)
            {
                if (fieldname.StartsWith(PlaycountPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    playcountFieldnames.Add(fieldname);
                }
            }
            // Add "BANSHEE" tags if no playcount tags were found:
            if (playcountFieldnames.Count == 0)
            {
                playcountFieldnames.Add(PlaycountPrefix + HowlerName);
            }

            string oggPlaycount = playcount.ToString(CultureInfo.InvariantCulture);
            foreach (string playcountname in playcountFieldnames)
            {
                xiphtag.SetField(playcountname, oggPlaycount);
            }
        }

        internal static string GetDate(TagLib.Ogg.XiphComment xiphComment)
        {
            string date = "";
            string[] fields = xiphComment.GetField("DATE");
            foreach (string field in fields)
            {
                if (field.Length > date.Length)
                    date = field;
            }

            return date;
        }
    }
}
