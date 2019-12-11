using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EuDiretoService
{
    public class Categories
    {   
        public Categories(int category_id , string category)
        {
            this.category_id = category_id;
            this.category = category;
        }
        public int category_id { get; set; }
        public string category { get; set; }
    }
}
