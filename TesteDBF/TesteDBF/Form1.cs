using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DbfDataReader;
using Advantage.Data.Provider;


namespace TesteDBF
{
    public partial class Form1 : Form
    {
        // private DbfTable dbfTable;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {




           // AdsConnection conn = new AdsConnection(@"data source=D:\Segunda;ServerType=local;");
            AdsConnection conn = new AdsConnection(textBox1.Text);
            try { 
         
            conn.Open();
            
            MessageBox.Show(conn.State.ToString());
            conn.Close();
            }catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            /*


            List<Estoque> lstEstoque = new List<Estoque>();

            var dbfPath = @"D:\LOG\Produtos.adt";
            DbfTable dbfTable = new DbfTable(dbfPath, EncodingProvider.GetEncoding(1252));
            var recordsEstoque = new DbfRecord(dbfTable);
            var records = new DbfRecord(dbfTable);


            while (dbfTable.Read(records))
            {
                lstEstoque.Add(new Estoque(
                   records.Values[0].ToString(),
                   Convert.ToInt32(records.Values[1].ToString())
                ));
            }*/


        }


    }
    public class Estoque
    {
        public Estoque(string codprod, Int32 estoque)
        {
            this.codprod = codprod;
            this.estoque = estoque;

        }
        public string codprod { get; set; }
        public Int32 estoque { get; set; }
    }
}
