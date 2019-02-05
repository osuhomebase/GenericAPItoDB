using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api2db
{
    class RoomLocationDbContext : DbContext
    {
        public RoomLocationDbContext() : base("name=NyuHousingApps") { }
        public DbSet<RoomLocations> RoomLocations
        {
            get;
            set;
        }
    }
}
