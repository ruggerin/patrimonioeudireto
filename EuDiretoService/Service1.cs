using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using RestSharp;
using RestSharp.Authenticators;
using System.Globalization;

namespace EuDiretoService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
            this.ServiceName = "EuDiretoSync";
        }
        Timer upProdutos = new Timer();
        OracleConnection con;
        DateTime ult_sinc_produtos = DateTime.MinValue;
        DateTime timer = DateTime.MinValue;

        protected override void OnStart(string[] args)
        {
            try { 
               con = new OracleConnection(Properties.Settings.Default.stringConection);
            }catch(Exception ex)
            {
                erroLogGeneration(ex.ToString());
            }
            upProdutos.Elapsed += new ElapsedEventHandler(OnElapsedTimeAsync); 
            upProdutos.Interval = 1* 1000;  
            upProdutos.Enabled = true;
            WriteDebug("Serviço iniciado, vs.:02-12-19: 15:44");
         
        }

        protected override void OnStop()
        {
        }

        private  void OnElapsedTimeAsync(object source, ElapsedEventArgs e)
        {
            
            if (timer.AddMinutes(Properties.Settings.Default.UpProdutos) <DateTime.Now)
            {
                upProdutos.Stop();
                WriteDebug("OnElapsedTimeAsync dentro do time programado, iniciando evento Cadastro de Produtos ");
                up_variants();
                WriteDebug("Fim OnElapsedTimeAsync");
                upProdutos.Start();
                timer = DateTime.Now;
                WriteDebug("upProdutos status enabled?: " + upProdutos.Enabled +" Proximo evento: "+timer.AddMinutes(Properties.Settings.Default.UpProdutos));

            }
         


        }
        private string ProdutoCadastrado(Products produto)
        {
            var client = new RestClient("https://eudireto.com/api/products?pcode="+ produto.product_code);
            client.Authenticator = new HttpBasicAuthenticator("ruggeri.barbosa@viacerta.com.br", "hK8421we27khQ80P90H3T9918DMx347k");
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            JObject produtosResponse = JObject.Parse(response.Content);
            WriteDebug(produto.product_code + "-"+produtosResponse["products"].Children().Count());
            if (produtosResponse["products"].Children().Count() == 0) {
                //Caso o produto não esteja cadastrado no sistema CS Cart, retornar 0;
                return "0";
            }else { 
                //Produto cadastrado, capturando id no sistema cs cart
                Int32 product_id =(Int32)produtosResponse["products"][0]["product_id"];

                //Verificando se exite alguma alteração na base do CS CART
                if( (string)produtosResponse["products"][0]["amount"] ==produto.amount.ToString() && Convert.ToDouble( (string)produtosResponse["products"][0]["price"], CultureInfo.InvariantCulture.NumberFormat)== produto.price && (string)produtosResponse["products"][0]["status"] == produto.status.ToString())
                {
                    return  "na";
                }
                else {
                    // WriteDebug("Codigo do produto Winthor:"+codprod+ "\tproduct_id: "+ product_id);
                    CultureInfo provider = new CultureInfo("en-us");
                    WriteDebug(produto.product_code+"\n"+
                        (string)produtosResponse["products"][0]["amount"] +"=="+ produto.amount.ToString()+"\n" +
                        Convert.ToDouble((string)produtosResponse["products"][0]["price"],provider) +"=="+produto.price+"\n"+
                        (string)produtosResponse["products"][0]["status"] +"=="+produto.status.ToString()

                    );
                return product_id.ToString();
                }
            }
         

        }

        private  void up_variants()
        {  
            List<Products> lstProdutos = LstProdutos();
            //WriteDebug(JsonConvert.SerializeObject( lstProdutos,Formatting.Indented));
            lstProdutos.ForEach(processarProduto);
            ult_sinc_produtos = DateTime.Now;

        }

        private void processarProduto(Products produto)
        {
            try {
                string produtct_id = ProdutoCadastrado(produto);
                if (produtct_id == "0") {
                    WriteDebug("Produto  não cadastrado no CS CART: " + produto.product_code.ToString());
                    if (produto.status == 'A'){
                        WriteDebug("Produto ativo, tentativa de cadastro");
                        var client = new RestClient("https://eudireto.com/api/products/");
                        client.Authenticator = new HttpBasicAuthenticator("ruggeri.barbosa@viacerta.com.br", "hK8421we27khQ80P90H3T9918DMx347k");
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Accept", "application/json");
                        WriteDebug(JsonConvert.SerializeObject(produto));
                        request.AddParameter("application/json", JsonConvert.SerializeObject(produto), ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        WriteDebug(response.Content);
                    }
                    else{
                        WriteDebug("Produto inativo, sem necessidade de cadastro");
                    }

                }
                else if(ProdutoCadastrado(produto) == "na")
                {
                    WriteDebug("Produto cadastrado, mas não possui modificações no ambiente Eu Direto");
                }
                else{
                    WriteDebug("Produto cadastrado, alterações pendentes ");
                    string product_id = ProdutoCadastrado(produto);
                    WriteDebug("Subindo produto codigo_distribuidor:" + produto.product_code + "(product_id: " + product_id + ")");
                    var client = new RestClient("https://eudireto.com/api/products/" + product_id);
                    client.Authenticator = new HttpBasicAuthenticator("ruggeri.barbosa@viacerta.com.br", "hK8421we27khQ80P90H3T9918DMx347k");
                    var request = new RestRequest(Method.PUT);
                    request.AddHeader("Accept", "application/json");
                    WriteDebug(JsonConvert.SerializeObject(produto));
                    request.AddParameter("application/json", JsonConvert.SerializeObject(produto), ParameterType.RequestBody);
                    IRestResponse response = client.Execute(request);
                    WriteDebug(response.Content);


                }

            }
            catch(Exception ex)
            {
                erroLogGeneration("Erro ao processar produto "+ produto.product_code+"\n"+ ex.ToString());
            }



        }
        

        private List<Products> LstProdutos()
        {
            string produtoproblema = "";
            try { 
            con.Open();
            DataSet dataSet = new DataSet();

            string query = @"
                select
                 lpad( pcprodut.codprod , 6, '0') CODPROD, 
                pcprodut.descricao,
                pcprodut.codepto categoria , 
                TRUNC((pcest.qtestger - ( pcest.qtreserv + pcest.qtbloqueada)) ,0)  estoque ,
                TRUNC(nvl( PCTABPR.pvenda,0),2)  PRECO,
                PCPRODUT.embalagem,
                pcprodut.nbm,
                PCPRODUT.codauxiliar ean,
                pcprodut.codauxiliar2 dun,
                case when  PCPRODUT.obs2 = 'FL' then 'D' ELSE 'A' END status,
                greatest(PCPRODUT.dtultalter, PCTABPR.dtultaltpvenda, PCEST.DTULTALTERSRVPRC) ULTIMA_MOVIMENTACAO 

                from pcprodut, pcest , PCTABPR
                where
                PCEST.codprod = PCPRODUT.CODPROD
                AND PCPRODUT.codprod = PCTABPR.codprod
                AND PCEST.codfilial  IN (:CODFILIAL)
                AND PCPRODUT.CODEPTO IN(12,64,14,71) /*DEPARTAMENTOS ELEGÍVEIS PARA VENDAS  (12,14,40,70,71,55,42,64,63)*/
               /* AND PCPRODUT.CODPROD = 4*/
                AND PCTABPR.numregiao IN(:REGIAO)
           
                AND greatest(PCPRODUT.dtultalter, PCTABPR.dtultaltpvenda, PCEST.DTULTALTERSRVPRC) >= to_date(:DTULTALT,'dd/mm/yyyy hh24:mi:ss')
                 ";
            OracleCommand fbcmd = new OracleCommand(query, con) { CommandType = CommandType.Text, BindByName = true };

            fbcmd.Parameters.Add(":CODFILIAL", Properties.Settings.Default.codfilial);
            fbcmd.Parameters.Add(":REGIAO", Properties.Settings.Default.RegiaoTblPreco);
            fbcmd.Parameters.Add(":DTULTALT", ult_sinc_produtos.ToString("dd/MM/yyyy HH:mm:ss"));

            OracleDataAdapter da = new OracleDataAdapter(fbcmd);
            da.Fill(dataSet);
            con.Close();
            List<Products> itemsRows = new List<Products>();
            for (int cont = 0; cont < dataSet.Tables[0].Rows.Count; cont++)
            {
                    //Criando jObject da sub-classe product_features
                JObject feature = SerializeFeatures(dataSet.Tables[0].Rows[cont]["NBM"].ToString(), dataSet.Tables[0].Rows[cont]["EAN"].ToString(), dataSet.Tables[0].Rows[cont]["DUN"].ToString());
                string  codprod =  dataSet.Tables[0].Rows[cont]["CODPROD"].ToString();
                produtoproblema = dataSet.Tables[0].Rows[cont]["CODPROD"].ToString();
                Int32  estoque =    Convert.ToInt32(dataSet.Tables[0].Rows[cont]["ESTOQUE"].ToString());
                string descricao =  dataSet.Tables[0].Rows[cont]["DESCRICAO"].ToString();
                CultureInfo provider = new CultureInfo("en-us");
                double preco =     Convert.ToDouble( dataSet.Tables[0].Rows[cont]["PRECO"].ToString().Replace(",","."),provider);
                char status = (char)dataSet.Tables[0].Rows[cont]["STATUS"].ToString().ToCharArray()[0];
                Int32 categoria = category_code(dataSet.Tables[0].Rows[cont]["CATEGORIA"].ToString());
                itemsRows.Add(new Products(codprod,descricao,categoria,status,estoque, preco, feature));
            }
            con.Close();
            return itemsRows;
            }
            catch(Exception ex)
            {
                erroLogGeneration(ex.ToString() + "\n Produto problema: " + produtoproblema);
                if(con.State == ConnectionState.Open)
                {
                    con.Close();
                }
                return null;
            }
        }

        private JObject SerializeFeatures(string ncm, string ean , string dun)
        {

            string objetostr = "{" +
               //NCM
               "\"551\":{" +
                   "\"feature_type\":\"T\"," +
                   "\"value\":\"" + ncm + "\"" +
               "}," +
               //EAN
               "\"552\":{" +
                   "\"feature_type\":\"T\"," +
                   "\"value\":\"" + ean + "\"" +
               "}," +
               //DUN
               "\"553\":{" +
                   "\"feature_type\":\"T\"," +
                   "\"value\":\"" + dun + "\"" +
               "}" +
           "}";

            JObject feature = JObject.Parse(objetostr);
            return feature;
        }

        public void WriteDebug(string texto)
        {
            if (Properties.Settings.Default.debugmode)
            {
                WriteToFile(DateTime.Now+"\t:"+texto);
            }
        }


        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\Log_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }

        private Int32 category_code(string codigo_interno)
        {
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "categorias.json";
            JObject teste = JObject.Parse(File.ReadAllText(filepath));
            return (Int32)teste[codigo_interno]["category_id"];
        }

        public void erroLogGeneration(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ErroExceptions_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(DateTime.Now + "\t:" + Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(DateTime.Now + "\t:" + Message);
                }
            }
            upProdutos.Stop();
            upProdutos.Start();


        }
                  

        internal class Products
        {   
            public Products(string codprod, string descricao,Int32 category_id, char status, Int32 estoque, double preco, JObject product_features)
            {
                product_code = codprod;
                product = descricao;
                this.category_ids = new List<string> { category_id.ToString() };
                amount = estoque;
                price = preco;
                this.product_features =  product_features;
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
}
