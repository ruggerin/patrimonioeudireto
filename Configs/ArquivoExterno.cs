using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Configs
{
    class ArquivoExterno
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
            winthor_key = (string)config["winthor_key"];


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
    }
}
