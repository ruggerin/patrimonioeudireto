using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using RestSharp;
using RestSharp.Authenticators;
using System.Net.Sockets;

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
        
        DateTime ult_sinc_produtos = DateTime.MinValue;
        DateTime timer = DateTime.MinValue;
      

        protected override void OnStart(string[] args)
        {  
            upProdutos.Elapsed += new ElapsedEventHandler(OnElapsedTimeAsync); 
            upProdutos.Interval = 1* 1000;  
            upProdutos.Enabled = true;
            WriteDebugHeader("Serviço iniciado, vs.:27-12-19: 15:44");
            WriteDebugHeader("Carregando arquivo de configurações");
          

        }

        protected override void OnStop()
        {

        }

        private  void OnElapsedTimeAsync(object source, ElapsedEventArgs e)
        {
           
            if (timer.AddMinutes(new Parametros().sincronismo_cad_produtos()) <DateTime.Now)
            {
                upProdutos.Stop();
                WriteDebugHeader("Verificando status Servidores[");
                WriteDebug("Vericiando status servidor Eu Direto:");
                //bool statusEuDiretoServer = PingHost(eudireto_api_host , eudireto_api_host_porta);
                bool statusEuDiretoServer = true ;
                WriteDebug("Conexão com servidor Eu Direto estabelecida?: " + statusEuDiretoServer);
                WriteDebug("Vericiando status servidor Winthor - Oracle");
                AcessoWinthor parametros = new AcessoWinthor();
                bool statusWinthor = PingHost(parametros.winthor_host, parametros.winthor_port);
                WriteDebug("Conexão com servidor Winthor - Oracle estabelecida?: " + statusWinthor);
                WriteDebugHeader("\n] Verificação status Servidore Concluída");

                if (statusEuDiretoServer && statusWinthor)
                {
                    WriteDebugHeader("Início evento Cadastro de Produtos[");
                    up_variants();
                    WriteDebugHeader("\n]\nFim evento 'Cadastro de Produtos");
                }
                else
                {
                    WriteDebugHeader("Não foi possível se conectar ao servidor");
                }
                upProdutos.Start();
                timer = DateTime.Now;
                WriteDebugHeader("Ciclo de atualização ativo?: " + upProdutos.Enabled +" Proximo evento: "+timer.AddMinutes(new Parametros().sincronismo_cad_produtos()));

            }
         


        }
        //Verificar se o produto está cadastrado no Eu Direto Admin
        private string ProdutoCadastrado(Products produto){
            AcessoEuDireto parametros = new AcessoEuDireto();
            var client = new RestClient("https://" + parametros.eudireto_api_host + "/api/products?pcode=" + produto.product_code);
            client.Authenticator = new HttpBasicAuthenticator(parametros.eudireto_api_usuario, parametros.eudireto_api_senha);
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            JObject produtosResponse = JObject.Parse(response.Content);
            WriteDebug(response.Content);
            if (produtosResponse["products"].Children().Count() == 0) {
                //Caso o produto não esteja cadastrado no sistema Eu Direto Admin, retornar 0;
                return "0";
            }else { 
                //Produto cadastrado, capturando id no sistema Eu Direto Admin
                Int32 product_id =(Int32)produtosResponse["products"][0]["product_id"];

                //Verificando se exite alguma alteração na base do Eu Direto Admin
                if( 
                    //Verificação de estoque
                    (string)produtosResponse["products"][0]["amount"] ==produto.amount.ToString() && 
                    //Verificação de preço
                    Convert.ToDouble( (string)produtosResponse["products"][0]["price"], System.Globalization.CultureInfo.InvariantCulture.NumberFormat)== produto.price && 
                    //Verificação de status
                    (string)produtosResponse["products"][0]["status"] == produto.status.ToString()
                )
                {
                    //Resposta da função caso não haja nem uma alteração a ser enviada ao ambiente Eu Direto
                    return  "na";
                }
                else {
                    //Caso exista alguma diferença entre as bases, imprime a diferença e envia para o método de atualização
                    System.Globalization.CultureInfo provider = new System.Globalization.CultureInfo("en-us");
                    WriteDebug(produto.product_code+"\n"+
                      "Estoque: "+  (string)produtosResponse["products"][0]["amount"] +"=="+ produto.amount.ToString()+"\n" +
                      "Preço: "+  Convert.ToDouble((string)produtosResponse["products"][0]["price"],provider) +"=="+produto.price+"\n"+
                      "Ativo: " +  (string)produtosResponse["products"][0]["status"] +"=="+produto.status.ToString()
                    );
                return product_id.ToString();
                }
            }
         

        }

        private  void up_variants()
        {  
            List<Products> lstProdutos = LstProdutos();
            //Imprime a coleção de informação de produtos
            //WriteDebug(JsonConvert.SerializeObject( lstProdutos,Formatting.Indented));
            lstProdutos.ForEach(processarProduto);
            ult_sinc_produtos = DateTime.Now;

        }
               
        private void processarProduto(Products produto)
        {
          
            try {
                string product_id = ProdutoCadastrado(produto);
                AcessoEuDireto parametros = new AcessoEuDireto();
                if (product_id == "0") {
                    //Cadastrar novo produto
                    WriteDebug("Produto  não cadastrado no Eu Direto Admin: " + produto.product_code.ToString());
                    if (produto.status == 'A'){
                        //Verifica se o produto está ativo na base do Winthor
                        WriteDebug("Produto ativo, tentativa de cadastro");
                        var client = new RestClient(@"https://"+ parametros.eudireto_api_host + "/api/products/");
                        client.Authenticator = new HttpBasicAuthenticator(parametros.eudireto_api_usuario, parametros.eudireto_api_senha);
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Accept", "application/json");
                        WriteDebug(JsonConvert.SerializeObject(produto));
                        request.AddParameter("application/json", JsonConvert.SerializeObject(produto), ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        WriteDebug(response.Content);
                    }
                    else{
                        //Caso produto esteja inativo, o serviço ignora o cadastro
                        WriteDebug("Produto inativo, sem necessidade de cadastro");
                    }

                }
                else if(product_id == "na"){
                    //Produto cadastado, mas não nescessita de atualização no Eu Direto
                    WriteDebug("Produto cadastrado, mas não possui modificações no ambiente Eu Direto");
                }
                else{
                    //Produto cadastrado na base do Eu Direto, e nescessita de atualizações de informações
                    WriteDebug("Produto cadastrado, alterações pendentes ");                    
                    WriteDebug("Subindo produto codigo_distribuidor:" + produto.product_code + "(product_id: " + product_id + ")");
                    var client = new RestClient(@"https://" + parametros.eudireto_api_host + "/api/products/" + product_id);
                    client.Authenticator = new HttpBasicAuthenticator(parametros.eudireto_api_usuario, parametros.eudireto_api_senha);
                    var request = new RestRequest(Method.PUT);
                    request.AddHeader("Accept", "application/json");

                    //Criação de um novo objeto para atualizar para atualizar somente campos específicos, status, estoque, preço e características
                    List<Products> tmp_lst_produtos = new List<Products>();
                    tmp_lst_produtos.Add(produto);

                    var product_update = (from p in tmp_lst_produtos select new  {  p.status, p.amount, p.price, p.product_features,p.weight}).First();                   
                    request.AddParameter("application/json", JsonConvert.SerializeObject(product_update), ParameterType.RequestBody);
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
            OracleConnection con = oCon();
            string produtoproblema = "";
            try {
               

                List<Categories> categories = atualizarListaCategorias();

                //return null;
                
                con.Open();
                DataSet dataSet = new DataSet();

                string query = Properties.Settings.Default.query_colecao_produtos;
                OracleCommand fbcmd = new OracleCommand(query, con) { CommandType = CommandType.Text, BindByName = true };
                FiltroWinthor filtroWinthor = new FiltroWinthor();
                fbcmd.Parameters.Add(":CODFILIAL", filtroWinthor.codfilial);
                fbcmd.Parameters.Add(":REGIAO", filtroWinthor.regiao_tbl_preco);
                fbcmd.Parameters.Add(":DTULTALT", ult_sinc_produtos.ToString("dd/MM/yyyy HH:mm:ss"));

                OracleDataAdapter da = new OracleDataAdapter(fbcmd);
                da.Fill(dataSet);
                con.Close();
                List<Products> itemsRows = new List<Products>();
                for (int cont = 0; cont < dataSet.Tables[0].Rows.Count; cont++)
                {
                    //Criando jObject da sub-classe product_features
             
                    JObject feature = SerializeFeatures(dataSet.Tables[0].Rows[cont]["NBM"].ToString(), dataSet.Tables[0].Rows[cont]["EAN"].ToString(), dataSet.Tables[0].Rows[cont]["DUN"].ToString(), dataSet.Tables[0].Rows[cont]["EMBALAGEM"].ToString());
                    string  codprod =  dataSet.Tables[0].Rows[cont]["CODPROD"].ToString();
                    produtoproblema = dataSet.Tables[0].Rows[cont]["CODPROD"].ToString();
                    Int32  estoque =    Convert.ToInt32(dataSet.Tables[0].Rows[cont]["ESTOQUE"].ToString());
                    string descricao =  dataSet.Tables[0].Rows[cont]["DESCRICAO"].ToString();
                    System.Globalization.CultureInfo provider = new System.Globalization.CultureInfo("en-us");
                    double preco =     Convert.ToDouble( dataSet.Tables[0].Rows[cont]["PRECO"].ToString().Replace(",","."),provider);
                    double peso = Convert.ToDouble(dataSet.Tables[0].Rows[cont]["pesobruto"].ToString().Replace(",", "."), provider); 
                    char status = (char)dataSet.Tables[0].Rows[cont]["STATUS"].ToString().ToCharArray()[0];
                    int category_id = 0;
                    if(categories.Where(x => x.category == dataSet.Tables[0].Rows[cont]["CATEGORIA"].ToString()).Count() == 0){
                      category_id =   cadastrarCatetory(dataSet.Tables[0].Rows[cont]["CATEGORIA"].ToString());
                      categories = atualizarListaCategorias();
                    }
                    else
                    {
                        Categories categoriesY =    categories.Where(x => x.category == dataSet.Tables[0].Rows[cont]["CATEGORIA"].ToString()).First();
                        category_id = categoriesY.category_id;
                    }
                    
                    itemsRows.Add(new Products(codprod,descricao, category_id, peso, status, estoque, preco, feature));
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
             
        public void WriteDebug(string texto)
        {
            if (new Parametros().debug_mode())
            {
                WriteToFile("\t"+DateTime.Now+"\t:"+texto);
            }
        }

        public void WriteDebugHeader(string texto)
        {
            if (new Parametros().debug_mode())
            {
                WriteToFile(DateTime.Now + "\t:" + texto);
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

        //Adaptação para criação de uma sub-classe com descrição de numeros inteiros
        public JObject SerializeFeatures(string ncm, string ean, string dun, string embalagem)
        {

            string objetostr = "{" +
               //NCM
               "\"556\":{" +
                   "\"feature_type\":\"T\"," +
                   "\"value\":\"" + ncm + "\"" +
               "}," +
               //EAN
               "\"555\":{" +
                   "\"feature_type\":\"T\"," +
                   "\"value\":\"" + ean + "\"" +
               "}," +
               //DUN
               "\"554\":{" +
                   "\"feature_type\":\"T\"," +
                   "\"value\":\"" + dun + "\"" +
               "}," +
               "\"557\":{" +
                   "\"feature_type\":\"T\"," +
                   "\"value\":\"" + embalagem + "\"" +
               "}," +
           "}";

            JObject feature = JObject.Parse(objetostr);
            return feature;
        }

        public List<Categories> atualizarListaCategorias()
        {
            AcessoEuDireto acessoEuDireto = new AcessoEuDireto();
            WriteDebugHeader("Atualizando informações das categorias do Eu Direto");
            var client = new RestClient("https://" + acessoEuDireto.eudireto_api_host + "/api/categories");
            client.Authenticator = new HttpBasicAuthenticator(acessoEuDireto.eudireto_api_usuario, acessoEuDireto.eudireto_api_senha);
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            JObject produtosResponse = JObject.Parse(response.Content);
            IList<JToken> pReponseCategorys = produtosResponse["categories"].Children().ToList();
            List<Categories> categories = new List<Categories>();
            foreach (JToken cat in pReponseCategorys)
            {
                //WriteDebug("Verificando se a categoria \"" + cat["category"].ToString()+"\" Contida no eu direto ");
                //int vExiste = categories.Where(x => x.category ==cat["category"].ToString() ).Count(); Vou usar isso depois
                categories.Add(new Categories(Convert.ToInt32(cat["category_id"].ToString()), cat["category"].ToString()));

            }
            WriteDebugHeader("Total categorias baixadas"+categories.Count() );

            return categories;
        }

        public int cadastrarCatetory(string category)
        {
            int new_category_id = 0;
            AcessoEuDireto acessoEuDireto = new AcessoEuDireto();
            try
            {
                string serialize =
                 "{" +
                     "\"category\": \"" + category + "\"," +
                     "\"position\": \"0\"," +
                     "\"status\": \"A\"," +
                     "\"company_id\": \""+ acessoEuDireto.eudireto_vendedor_id + "\"" +
                 "}";

                var client = new RestClient("https://" + acessoEuDireto.eudireto_api_host + "/api/categories");
                client.Authenticator = new HttpBasicAuthenticator(acessoEuDireto.eudireto_api_usuario, acessoEuDireto.eudireto_api_senha);
                var request = new RestRequest(Method.POST);               
                request.AddHeader("Accept", "application/json");             
                request.AddParameter("application/json", serialize, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                JObject categoryResponse = JObject.Parse(response.Content);
                new_category_id = (Int32)categoryResponse["category_id"];
                WriteDebug(response.Content.ToString());

            }
            catch(Exception ex)
            {
                erroLogGeneration(ex.ToString());
            }
            return new_category_id;

        }

        public  bool PingHost(string hostUri, int portNumber)
        {
            try
            {
                using (var client = new TcpClient(hostUri, portNumber))
                    return true;
            }
            catch (SocketException ex)
            {
                WriteDebug("host '"+hostUri+":"+portNumber+"' não diponível, Erro:\n" +ex.ToString());
                return false;
            }
        }

        public OracleConnection oCon()
        {
            AcessoWinthor acessoWinthor = new AcessoWinthor();
            string stringConnection = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=" + acessoWinthor.winthor_host + ")(PORT=" + acessoWinthor.winthor_port + "))(CONNECT_DATA=(SERVICE_NAME=" + acessoWinthor.winthor_service_name + ")));user id=" + acessoWinthor.winthor_user + ";password=" + acessoWinthor.winthor_key + ";";
            return new OracleConnection(stringConnection);
        }


       
    }
}
