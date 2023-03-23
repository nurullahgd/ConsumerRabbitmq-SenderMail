using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consumer
{
    public class Article
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public Guid MagazineId { get; set; }
        public Guid AuthorId { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}
