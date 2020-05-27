using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using DbfDataReader;
using Advantage.Data.Provider;

namespace EuDiretoService
{
    class Evento_SincronismoProdutos
    {
        Service1 service;
        LogsManager logs;
        public Evento_SincronismoProdutos(Service1 service , LogsManager logsManager)
        {
            this.service = service;
            this.logs = logsManager;
        }

        //Verificar se o produto está cadastrado no Eu Direto Admin
        private string ProdutoCadastrado(Products produto)
        {
            AcessoEuDireto parametros = new AcessoEuDireto();
            var client = new RestClient("https://" + parametros.eudireto_api_host + "/api/products?pcode=" + produto.product_code);
            client.Authenticator = new HttpBasicAuthenticator(parametros.eudireto_api_usuario, parametros.eudireto_api_senha);
            var request = new RestRequest(Method.GET);
            IRestResponse response = client.Execute(request);
            JObject produtosResponse = JObject.Parse(response.Content);
            logs.WriteDebug(response.Content);
            if (produtosResponse["products"].Children().Count() == 0)
            {
                //Caso o produto não esteja cadastrado no sistema Eu Direto Admin, retornar 0;
                return "0";
            }
            else
            {
                //Produto cadastrado, capturando id no sistema Eu Direto Admin
                Int32 product_id = (Int32)produtosResponse["products"][0]["product_id"];

                //Verifica se o produto foi desativado manualmente no sistema Eu Direto, status "Disapproved"
                if ((string)produtosResponse["products"][0]["status"]=="X" )   {
                    logs.WriteDebug("Produto desaprovado no sistema Eu Direto, ignorando a atualização");
                    return "na";
                }

                //Verificando se exite alguma alteração na base do Eu Direto Admin
                if (
                    //Verificação de estoque
                    (string)produtosResponse["products"][0]["amount"]== produto.amount.ToString()  &&
                    //Verificação de preço
                    Convert.ToDouble((string)produtosResponse["products"][0]["price"], System.Globalization.CultureInfo.InvariantCulture.NumberFormat) == produto.price &&
                    //Verificação de status
                    (string)produtosResponse["products"][0]["status"]== produto.status.ToString()
                )
                {
                    //Resposta da função caso não haja nem uma alteração a ser enviada ao ambiente Eu Direto
                    return "na";
                }
                else
                {
                    //Caso exista alguma diferença entre as bases, imprime a diferença e envia para o método de atualização
                    System.Globalization.CultureInfo provider = new System.Globalization.CultureInfo("en-us");
                    
                    return product_id.ToString();
                }
            }


        }

        public void up_variants()
        {
            //Carregar lista de produtos a partir dos filtros definidos
            List<Products> lstProdutos = LstProdutos();


            //Inicia evento de atualização a partir da lista gerada pela consulta
            lstProdutos.ForEach(processarProduto);
            try{
                Parametros parametros = new Parametros();
                parametros.CarregarConfiguracoes();
                parametros.ult_sinc_produtos = DateTime.Now;
                parametros.SalvarParametros();
            }
            catch (Exception ex)
            {
                logs.erroLogGeneration(ex.ToString(), service);
            }
        }

        private void processarProduto(Products produto)
        {

            try
            {
                string product_id = ProdutoCadastrado(produto);
                AcessoEuDireto parametros = new AcessoEuDireto();
                
                if (product_id.Equals("0"))
                {
                    //Cadastrar novo produto
                    logs.WriteDebug("Produto  não cadastrado no Eu Direto Admin: " + produto.product_code.ToString());
                    if (produto.status.Equals('A'))
                    {
                        //Verifica se o produto está ativo na base do Winthor
                        logs.WriteDebug("Produto ativo, tentativa de cadastro");
                        var client = new RestClient(@"https://" + parametros.eudireto_api_host + "/api/products/");
                        client.Authenticator = new HttpBasicAuthenticator(parametros.eudireto_api_usuario, parametros.eudireto_api_senha);
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("Accept", "application/json");
                        logs.WriteDebug(JsonConvert.SerializeObject(produto));
                        request.AddParameter("application/json", JsonConvert.SerializeObject(produto), ParameterType.RequestBody);
                        IRestResponse response = client.Execute(request);
                        logs.WriteDebug(response.Content);
                    }
                    else
                    {
                        //Caso produto esteja inativo, o serviço ignora o cadastro
                        logs.WriteDebug("Produto inativo, sem necessidade de cadastro");
                    }

                }
                else if (product_id.Equals( "na"))
                {
                    //Produto cadastado, mas não nescessita de atualização no Eu Direto
                    logs.WriteDebug("Produto cadastrado, mas não possui modificações no ambiente Eu Direto");
                }
                else
                {
                    //Produto cadastrado na base do Eu Direto, e nescessita de atualizações de informações
                    logs.WriteDebug("Produto cadastrado, alterações pendentes ");
                    logs.WriteDebug("Subindo produto codigo_distribuidor:" + produto.product_code + "(product_id: " + product_id + ")");
                    var client = new RestClient(@"https://" + parametros.eudireto_api_host + "/api/products/" + product_id);
                    client.Authenticator = new HttpBasicAuthenticator(parametros.eudireto_api_usuario, parametros.eudireto_api_senha);
                    var request = new RestRequest(Method.PUT);
                    request.AddHeader("Accept", "application/json");

                    //Criação de um novo objeto para atualizar para atualizar somente campos específicos, status, estoque, preço e características
                    List<Products> tmp_lst_produtos = new List<Products>();
                    tmp_lst_produtos.Add(produto);

                    var product_update = (from p in tmp_lst_produtos select new { p.status, p.amount, p.price, p.product_features, p.weight }).First();
                    request.AddParameter("application/json", JsonConvert.SerializeObject(product_update), ParameterType.RequestBody);
                    IRestResponse response = client.Execute(request);
                    logs.WriteDebug(response.Content);

                }

            }
            catch (Exception ex)
            {
                logs.erroLogGeneration("Erro ao processar produto " + produto.product_code + "\n" + ex.ToString(), service);
            }



        }

        private List<Products> LstProdutos()
        {
            DbfTable dbfTable;
            string produtoproblema = "";
            try
            {
                List<Categories> categories = atualizarListaCategorias();

                List<Estoque> lstEstoque = new List<Estoque>();
                Parametros parametros = new Parametros();
                parametros.CarregarConfiguracoes();
                logs.WriteDebug(parametros.codfilial);
                var dbfPath = parametros.dbf_host + @"\estoque.DBF";
            
                
                dbfTable = new DbfTable(dbfPath, EncodingProvider.GetEncoding(1252));
                var recordsEstoque = new DbfRecord(dbfTable);
                var records = new DbfRecord(dbfTable);


                while (dbfTable.Read(records))
                {
                  
                    
                    lstEstoque.Add(new Estoque(
                       records.Values[0].ToString(),
                       Convert.ToInt32(records.Values[1].ToString())
                    ));
                }

                dbfTable.Close();
                List<Products> itemsRows = new List<Products>();
                 
                AdsConnection conn = new AdsConnection(@"data source="+ parametros.dbf_host + ";ServerType=local;");
                conn.Open();
                AdsCommand cmd = new AdsCommand("select * from produtos ", conn);
                AdsDataAdapter adapter = new AdsDataAdapter(cmd);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                for (Int32 i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    
                    JObject feature = null;
                    string codprod = ds.Tables[0].Rows[i]["codigo"].ToString();
                    produtoproblema = codprod;
                    // produtoproblema = dataSet.Tables[0].Rows[cont]["CODPROD"].ToString();
                    Int32 estoque = 0;
                    if (lstEstoque.Where(tbl => tbl.codprod == records.Values[0].ToString()).Count() > 0)
                    {
                        Estoque est = lstEstoque.Where(tbl => tbl.codprod == records.Values[0].ToString()).First();
                        estoque = est.estoque;
                    }
                    string descricao = ds.Tables[0].Rows[i]["descricao"].ToString();
                    System.Globalization.CultureInfo provider = new System.Globalization.CultureInfo("en-us");
                    //logs.WriteDebug(ds.Tables[0].Rows[i]["preco"].ToString().Replace(",", "."));
                    double preco = ds.Tables[0].Rows[i]["preco"].ToString().Length > 0? Convert.ToDouble(ds.Tables[0].Rows[i]["preco"].ToString().Replace(",", "."), provider) : 0;
                    double peso = Convert.ToDouble(ds.Tables[0].Rows[i]["pesoLiq"].ToString().Replace(",", "."), provider);
                    char status = (char)(ds.Tables[0].Rows[i]["palmtop"].ToString() == "True" ? "A" : "D").ToCharArray()[0];
                    int category_id = 0;
                    //logs.WriteDebug(ds.Tables[0].Rows[i]["palmtop"].ToString());
                    //logs.WriteDebug(status.ToString());
                    itemsRows.Add(new Products(codprod, descricao, category_id, peso, status, estoque, preco, feature));
                }
                return itemsRows;

               
            }
            catch (Exception ex)
            {
                logs.erroLogGeneration(ex.ToString() + "\n Produto problema: " + produtoproblema,service);
              
                return null;
            }
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
            logs.WriteDebugHeader("Atualizando informações das categorias do Eu Direto");
            var client = new RestClient("https://" + acessoEuDireto.eudireto_api_host + "/api/categories?items_per_page=10000");
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
            logs.WriteDebugHeader("Total categorias baixadas" + categories.Count());

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
                     "\"company_id\": \"" + acessoEuDireto.eudireto_vendedor_id + "\"" +
                 "}";

                var client = new RestClient("https://" + acessoEuDireto.eudireto_api_host + "/api/categories");
                client.Authenticator = new HttpBasicAuthenticator(acessoEuDireto.eudireto_api_usuario, acessoEuDireto.eudireto_api_senha);
                var request = new RestRequest(Method.POST);
                request.AddHeader("Accept", "application/json");
                request.AddParameter("application/json", serialize, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                JObject categoryResponse = JObject.Parse(response.Content);
                new_category_id = (Int32)categoryResponse["category_id"];
                logs.WriteDebug(response.Content.ToString());

            }
            catch (Exception ex)
            {
                logs.erroLogGeneration(ex.ToString(), service);
            }
            return new_category_id;

        }




        
    }
    public class Estoque
    {
        public Estoque(string codprod, Int32 estoque)
        {
            this.codprod = codprod;
            this.estoque = estoque;

        }
        public string  codprod { get; set; }
        public Int32  estoque { get; set; }
    }
}
