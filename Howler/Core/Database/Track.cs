using System.Linq;
using TagLib;

namespace Howler.Core.Database
{
    partial class Track
    {
        public IPicture GetPicture()
        {
            using (File file = File.Create(Path))
                return file.Tag.Pictures.FirstOrDefault();
        }
    }
}
