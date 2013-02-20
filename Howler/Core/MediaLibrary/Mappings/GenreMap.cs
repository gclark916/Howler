using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Mapping;
using Howler.Core.MediaLibrary.Entities;

namespace Howler.Core.MediaLibrary.Mappings
{
    class GenreMap : ClassMap<Genre>
    {
        public GenreMap()
        {
            Id(genre => genre.Id);
            Map(genre => genre.Name)
                .Unique().Not.Nullable();
            HasManyToMany(genre => genre.Tracks)
                .Inverse()
                .Cascade.SaveUpdate()
                .Table(TableNames.TrackGenre);
        }
    }
}
