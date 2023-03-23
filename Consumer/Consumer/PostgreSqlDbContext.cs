using Microsoft.EntityFrameworkCore;

namespace Consumer
{
    public class PostgreSqlDbContext :DbContext
    {
        public PostgreSqlDbContext(DbContextOptions options) : base (options)
        {

        }
        
        public DbSet<Article> Articles { get; set; }    
    }
}
