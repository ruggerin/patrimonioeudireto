using System;
using System.ServiceProcess;
using System.Timers;


namespace EuDiretoService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
            this.ServiceName = "EuDiretoSync";
        }
       public Timer upProdutos = new Timer();
        
       // DateTime ult_sinc_produtos = DateTime.MinValue;
        DateTime timer = DateTime.MinValue;
      

        protected override void OnStart(string[] args)
        {            
            upProdutos.Elapsed += new ElapsedEventHandler(OnElapsedTimeAsync); 
            upProdutos.Interval = 1* 1000;  
            upProdutos.Enabled = true;
            new LogsManager().WriteDebugHeader("Serviço iniciado, vs.:01-01-20: 15:44");
            new LogsManager().WriteDebugHeader("Carregando arquivo de configurações");
         
        }

        protected override void OnStop()
        {

        }

        private  void OnElapsedTimeAsync(object source, ElapsedEventArgs e)
        {
           
            if (timer.AddMinutes(new Parametros().sincronismo_cad_produtos()) <DateTime.Now)
            {
                LogsManager logsManager = new LogsManager();
                upProdutos.Stop();
                logsManager.WriteDebugHeader("Verificando status Servidores[");
                logsManager.WriteDebug("Vericiando status servidor Eu Direto:");
                //bool statusEuDiretoServer = PingHost(eudireto_api_host , eudireto_api_host_porta);
                bool statusEuDiretoServer = true ;
                logsManager.WriteDebug("Conexão com servidor Eu Direto estabelecida?: " + statusEuDiretoServer);
                logsManager.WriteDebug("Vericiando status servidor Winthor - Oracle");
                AcessoWinthor parametros = new AcessoWinthor();
                bool statusWinthor = PingHost(parametros.winthor_host, parametros.winthor_port);
                logsManager.WriteDebug("Conexão com servidor Winthor - Oracle estabelecida?: " + statusWinthor);
                logsManager.WriteDebugHeader("\n] Verificação status Servidore Concluída");

                if (statusEuDiretoServer && statusWinthor)
                {
                    logsManager.WriteDebugHeader("Início evento Cadastro de Produtos[");
                    new Evento_SincronismoProdutos( this, logsManager).up_variants();
                    logsManager.WriteDebugHeader("\n]\nFim evento 'Cadastro de Produtos");
                }
                else
                {
                    logsManager.WriteDebugHeader("Não foi possível se conectar ao servidor");
                }
                upProdutos.Start();
                timer = DateTime.Now;
                logsManager.WriteDebugHeader("Ciclo de atualização ativo?: " + upProdutos.Enabled +" Proximo evento: "+timer.AddMinutes(new Parametros().sincronismo_cad_produtos()));
            }       
        }

        public  bool PingHost(string hostUri, int portNumber)
        {  
            try
            {
                using (var client = new System.Net.Sockets.TcpClient(hostUri, portNumber))
                return true;
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                new LogsManager().WriteDebug("host '"+hostUri+":"+portNumber+"' não diponível, Erro:\n" +ex.ToString());
                return false;
            }
        }
       
    }
}
