using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EuDiretoService
{
    public class Products
    {
        public Products(string codprod, string descricao, Int32 category_id, char status, Int32 estoque, double preco, JObject product_features)
        {
            product_code = codprod;
            product = descricao;
            this.category_ids = new List<string> { category_id.ToString() };
            amount = estoque;
            price = preco;
            this.product_features = product_features;
            this.status = status;

        }
        public string product { get; set; }
        public char status { get; set; }
        public List<string> category_ids { get; set; }
        public Int32 amount { get; set; }
        public double price { get; set; }
        public string product_code { get; set; }
        public JObject product_features { get; set; }


        
    }

   
}
