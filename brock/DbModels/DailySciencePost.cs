using brock.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
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
        public DateTime PostedAtUtc { get; set; }
        public DateTime CreatedDateUtc { get; set; }
        public bool Ignore { get; set; }
    }

    public class DailySciencePostContext : DbContext
    {

        public DailySciencePostContext(string connectionString)
        {
            Database.Connection.ConnectionString = connectionString;
        }
        public DbSet<DailySciencePost> DailySciencePost { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer<DailySciencePostContext>(null);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
