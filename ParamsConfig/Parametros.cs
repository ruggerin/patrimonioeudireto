using System;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Crypto;

namespace ParamsConfig
{
    class Parametros
    {
        JObject DesArquivoConfiguracoes()
        {
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\propriedades.json";
            string[] readText = File.ReadAllLines(filepath);
            string config = "";
            foreach (string s in readText)
            {
                config = config + s + "\n";
            }
            return JObject.Parse(config);
        }




        public Int32 sincronismo_cad_produtos()
        {
            CarregarConfiguracoes();

            return tempo_sincronismo_cad_produtos;
        }

        public bool debug_mode()
        {
            CarregarConfiguracoes();
            return modo_debug;
        }

        public void SalvarParametros()
        {
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\propriedades.json";


            Criptografia criptografia = new Criptografia(CryptProvider.RC2);
            criptografia.Key = "23ko84jezk";

            winthor_key = criptografia.Encrypt(winthor_key);
            string parametros =  JsonConvert.SerializeObject(this,Formatting.Indented);
            // Create a file to write to.   
            using (StreamWriter sw = File.CreateText(filepath))
            {
                sw.Write(parametros);
            }
        }
            
        


        public void CarregarConfiguracoes()
        {
            JObject config = DesArquivoConfiguracoes();

            tempo_sincronismo_cad_produtos = (Int32)config["tempo_sincronismo_cad_produtos"];
            regiao_tbl_preco = (Int32)config["regiao_tbl_preco"];
            codfilial = (string)config["codfilial"];
            modo_debug = (bool)config["modo_debug"];
            eudireto_vendedor_id = (Int32)config["eudireto_vendedor_id"];
            eudireto_api_usuario = (string)config["eudireto_api_usuario"];
            eudireto_api_senha = (string)config["eudireto_api_senha"];
            eudireto_api_host = (string)config["eudireto_api_host"];
            eudireto_api_port = (Int32)config["eudireto_api_port"];
            winthor_host = (string)config["winthor_host"];
            winthor_port = (Int32)config["winthor_port"];
            winthor_service_name = (string)config["winthor_service_name"];
            winthor_user = (string)config["winthor_user"];
            ult_sinc_produtos = (string)config["ult_sinc_produtos"] == null ? DateTime.MinValue : (DateTime)config["ult_sinc_produtos"];
 
            Criptografia criptografia = new Criptografia(CryptProvider.RC2);
            criptografia.Key = "23ko84jezk";        
            winthor_key = criptografia.Decrypt( (string)config["winthor_key"]);


        }
        public int tempo_sincronismo_cad_produtos { get; set; }
        public int regiao_tbl_preco { get; set; }
        public string codfilial { get; set; }
        public bool modo_debug { get; set; }
        public int eudireto_vendedor_id { get; set; }
        public string eudireto_api_usuario { get; set; }
        public string eudireto_api_senha { get; set; }
        public string eudireto_api_host { get; set; }
        public int eudireto_api_port { get; set; }
        public string winthor_host { get; set; }
        public int winthor_port { get; set; }
        public string winthor_service_name { get; set; }
        public string winthor_user { get; set; }
        public string winthor_key { get; set; }
        public DateTime ult_sinc_produtos { get; set; }

    }
    class AcessoWinthor
    {

        public AcessoWinthor()
        {
            Parametros parametros = new Parametros();
            parametros.CarregarConfiguracoes();

            winthor_host = parametros.winthor_host;
            winthor_port = parametros.winthor_port;
            winthor_service_name = parametros.winthor_service_name;
            winthor_user = parametros.winthor_user;
            winthor_key = parametros.winthor_key;

        }




        public string winthor_host { get; set; }
        public int winthor_port { get; set; }
        public string winthor_service_name { get; set; }
        public string winthor_user { get; set; }
        public string winthor_key { get; set; }
    }
    public class AcessoEuDireto
    {
        public AcessoEuDireto()
        {
            Parametros parametros = new Parametros();
            parametros.CarregarConfiguracoes();
            eudireto_api_usuario = parametros.eudireto_api_usuario;
            eudireto_api_senha = parametros.eudireto_api_senha;
            eudireto_api_host = parametros.eudireto_api_host;
            eudireto_api_port = parametros.eudireto_api_port;
            eudireto_vendedor_id = parametros.eudireto_vendedor_id;

        }

        public string eudireto_api_usuario { get; set; }
        public string eudireto_api_senha { get; set; }
        public string eudireto_api_host { get; set; }
        public int eudireto_api_port { get; set; }
        public int eudireto_vendedor_id { get; set; }
    }

    public class FiltroWinthor
    {
        public FiltroWinthor()
        {
            Parametros parametros = new Parametros();
            parametros.CarregarConfiguracoes();
            regiao_tbl_preco = parametros.regiao_tbl_preco;
            codfilial = parametros.codfilial;
        }
        public int regiao_tbl_preco { get; set; }
        public string codfilial { get; set; }
    }

   

}
