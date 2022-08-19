using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brock.DbModels
{
    public class DailySciencePost
    {
        public int Id {  get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime PostedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class DailySciencePostContext : DbContext
    {
        public DbSet<DailySciencePost> DailySciencePost { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer<DailySciencePostContext>(null);
            Database.Connection.ConnectionString = "Server = localhost\\SQLEXPRESS; Database = brock; Trusted_Connection = True;";
            base.OnModelCreating(modelBuilder);
        }
    }
}
