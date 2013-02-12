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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections;

namespace Howler.Core.Tagging
{
    internal static class ID3v2Tagger
    {
        public enum DateGranularity
        {
            Year, Month, Day, Hour, Minute, Second
        }

        // What we call ourselves in POPM tags.
        private const string HowlerUsername = "Howler";
        private const string BansheeUsername = "Banshee";
        private const string QuodLibetUsername = "quodlibet@lists.sacredchao.net";
        private const string MediaMonkeyUsername = "no@email";
        private const string WindowsMediaPlayerUsername = "Windows Media Player 9 Series";

        // Ordered list of ID3v2 POPM authors to attempt when importing.
        // Banshee must be listed first, to ensure that we give priority to our own ratings.
        // If new entries are added to this list, also make sure that
        // PopmToBanshee and BansheeToPopm are still accurate.
        private static string[] POPM_known_creator_list = {
            HowlerUsername,
            BansheeUsername,
            MediaMonkeyUsername,
            QuodLibetUsername,
            WindowsMediaPlayerUsername };

        // Converts ID3v2 POPM rating to Banshee rating
        private static int PopmToBanshee(byte popm_rating)
        {
            // The following schemes are used by the other POPM-compatible players:
            // WMP/Vista: "Windows Media Player 9 Series" ratings:
            //   1 = 1, 2 = 64, 3=128, 4=196 (not 192), 5=255
            // MediaMonkey: "no@email" ratings:
            //   0.5=26, 1=51, 1.5=76, 2=102, 2.5=128,
            //   3=153, 3.5=178, 4=204, 4.5=230, 5=255
            // Quod Libet: "quodlibet@lists.sacredchao.net" ratings
            //   (but that email can be changed):
            //   arbitrary scale from 0-255
            // Compatible with all these rating scales (what we'll use):
            //   unrated=0, 1=1-63, 2=64-127, 3=128-191, 4=192-254, 5=255
            if (popm_rating == 0x0)// unrated
                return 0;
            if (popm_rating < 0x40)// 1-63
                return 1;
            if (popm_rating < 0x80)// 64-127
                return 2;
            if (popm_rating < 0xC0)// 128-191
                return 3;
            if (popm_rating < 0xFF)// 192-254
                return 4;
            return 5;// 255
        }

        // Converts Banshee rating to ID3v2 POPM rating
        private static byte BansheeToPopm(int banshee_rating)
        {
            switch (banshee_rating)
            {
                case 1:
                    return 0x1;
                case 2:
                    return 0x40;// 64
                case 3:
                    return 0x80;// 128
                case 4:
                    return 0xC0;// 192
                case 5:
                    return 0xFF;// 255
                default:
                    return 0x0;// unrated/unknown
            }
        }

        // Converts MediaMonkey rating to ID3v2 POPM rating
        // Should work for Windows Media Player ratings too
        // from http://www.mediamonkey.com/forum/viewtopic.php?f=7&t=40532#p226576
        private static byte MediaMonkeyToPopm(int rating)
        {
            if (rating <= 0)
                return 0;
            if (rating <= 25) 
                return (byte)(rating + 3);
            if (rating <= 45)
                return (byte)(rating + 24);
            if (rating <= 65)
                return (byte)(rating + 68);
            if (rating <= 85)
                return (byte)(rating + 116);
            return (byte)(rating + 152);
        }

        // Converts Howler rating to ID3v2 POPM rating
        private static byte HowlerToPopm(int howlerRating)
        {
            return howlerRating < 0 ? (byte)255 : (byte)howlerRating;
        }

        // Overwrites all POPM frames with the new rating and playcount.
        // If no *known-compatible* frames are found, a new "Banshee"-authored
        // frame is also created to store this information.
        public static void StoreRating(int rating, TagLib.Id3v2.Tag id3v2tag)
        {
            bool howlerPopmFound = false;
            var popmFrames = id3v2tag.GetFrames<TagLib.Id3v2.PopularimeterFrame>();
            foreach (TagLib.Id3v2.PopularimeterFrame popm in popmFrames)
            {
                switch(popm.User)
                {
                    case "Banshee":
                    case "quodlibet@lists.sacredchao.net":
                        // A Howler "0" rating is saved as a Banshee "1" because
                        // Banshee interprets a POPM of 0 as no rating
                        if (rating == 0)
                            popm.Rating = BansheeToPopm(1);
                        else
                        {
                            int bansheeRating = (int)Math.Ceiling((double)rating / 5.0);
                            popm.Rating = BansheeToPopm(bansheeRating);
                        }
                        break;
                    case "Windows Media Player 9 Series":
                    case "no@email":
                        popm.Rating = MediaMonkeyToPopm(rating);
                        break;
                    case "Howler":
                        popm.Rating = HowlerToPopm(rating);
                        howlerPopmFound = true;
                        break;
                    default:
                        System.Console.Out.Write("Unrecognized POPM user {0}", popm.User);
                        break;
                }
            }

            if (!howlerPopmFound)
            {
                // Create a Howler popm frame if it doesn't exist
                TagLib.Id3v2.PopularimeterFrame popm = TagLib.Id3v2.PopularimeterFrame.Get(id3v2tag,
                                                                                            HowlerUsername,
                                                                                            true);
                popm.Rating = HowlerToPopm(rating);
            }
        }

        public static void StorePlayCount(int playcount, TagLib.Id3v2.Tag id3v2tag)
        {
            bool known_frames_found = false;
            foreach (TagLib.Id3v2.PopularimeterFrame popm in
                        id3v2tag.GetFrames<TagLib.Id3v2.PopularimeterFrame>())
            {
                if (System.Array.IndexOf(POPM_known_creator_list, popm.User) >= 0)
                {
                    // Found a known-good POPM frame, don't need to create a "Banshee" frame.
                    known_frames_found = true;
                }

                popm.PlayCount = (ulong)playcount;
            }

            if (!known_frames_found)
            {
                // No known-good frames found, create a new POPM frame (with creator string "Banshee")
                TagLib.Id3v2.PopularimeterFrame popm = TagLib.Id3v2.PopularimeterFrame.Get(id3v2tag,
                                                                                            HowlerUsername,
                                                                                            true);
                popm.PlayCount = (ulong)playcount;
            }
        }

        // Scans the file for *known-compatible* POPM frames, with priority given to
        // frames at the top of the known creator list.
        public static void GetRatingAndPlayCount(TagLib.Id3v2.Tag id3v2tag,
                                                    out int rating, out int playcount)
        {
            rating = -1;
            playcount = 0;
            TagLib.Id3v2.PopularimeterFrame popm = null;
            for (int i = 0; i < POPM_known_creator_list.Length; i++)
            {
                popm = TagLib.Id3v2.PopularimeterFrame.Get(id3v2tag,
                                                            POPM_known_creator_list[i],
                                                            false);
                if (popm != null)
                {
                    break;
                }
            }

            if (popm != null)
            {
                rating = PopmToHowler(popm);
                playcount = (int)popm.PlayCount;
            }
        }

        private static int PopmToHowler(TagLib.Id3v2.PopularimeterFrame popm)
        {
            int howlerRating = -1;
 	        switch (popm.User)
            {
                case BansheeUsername:
                case QuodLibetUsername:
                    int bansheeRating = PopmToBanshee(popm.Rating);
                    howlerRating = bansheeRating * 20;
                    break;
                case MediaMonkeyUsername:
                case WindowsMediaPlayerUsername:
                    howlerRating = PopmToMediaMonkey(popm.Rating);
                    break;
                case HowlerUsername:
                    howlerRating = popm.Rating > 100 ? -1 : popm.Rating;
                    break;
            }

            return howlerRating;
        }

        private static int PopmToMediaMonkey(byte p)
        {
            if (p > 85 + 116)
                return p - 152;
            if (p > 65 + 68)
                return p - 116;
            if (p > 45 + 24)
                return p - 68;
            if (p > 25 + 3)
                return p - 24;
            return p;
        }

        public static void SetDates(TagLib.Id3v2.Tag tag, IList<Tuple<DateTime, DateGranularity>> tuples)
        {
            if (tuples == null)
                throw new ArgumentNullException("tuples");

            string[] formattedDates = new string[tuples.Count];
            for (int index = 0; index < tuples.Count; index++)
            {
                switch (tuples[index].Item2)
                {
                    case DateGranularity.Year:
                        formattedDates[index] = tuples[index].Item1.ToString("yyyy", CultureInfo.InvariantCulture);
                        break;
                    case DateGranularity.Month:
                        formattedDates[index] = tuples[index].Item1.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                        break;
                    case DateGranularity.Day:
                        formattedDates[index] = tuples[index].Item1.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        break;
                    case DateGranularity.Hour:
                        formattedDates[index] = tuples[index].Item1.ToString("yyyy-MM-ddTHH", CultureInfo.InvariantCulture);
                        break;
                    case DateGranularity.Minute:
                        formattedDates[index] = tuples[index].Item1.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
                        break;
                    case DateGranularity.Second:
                        formattedDates[index] = tuples[index].Item1.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                        break;
                }
            }

            tag.SetTextFrame("TDRC", formattedDates);
        }

        public static string GetDate(TagLib.Id3v2.Tag tag)
        {
            String date = "";
            var frames = tag.GetFrames<TagLib.Id3v2.TextInformationFrame>("TDRC");
            foreach (TagLib.Id3v2.TextInformationFrame frame in frames)
            {
                foreach (string text in frame.Text)
                    if (text.Length > date.Length)
                        date = text;
            }

            return date;
        }

    }

}
