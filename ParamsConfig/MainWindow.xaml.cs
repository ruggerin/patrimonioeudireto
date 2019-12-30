using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management.Automation;

namespace ParamsConfig
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {

        Parametros parametros;
        public MainWindow()
        {
            InitializeComponent();
            parametros = new Parametros();
            parametros.CarregarConfiguracoes();
            PreencherCampos();

        }

        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            tempo_sincronismo_cad_produtos.Focus();

        }

        private void TreeViewItem_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            eudireto_vendedor_id.Focus();
        }

        private void TreeViewItem_MouseDoubleClick_2(object sender, MouseButtonEventArgs e)
        {
            winthor_key.Focus();
        }

        void PreencherCampos()
        {
         
            tempo_sincronismo_cad_produtos.Text =   parametros.tempo_sincronismo_cad_produtos.ToString();
            regiao_tbl_preco.Text = parametros.regiao_tbl_preco.ToString();
            codfilial.Text = parametros.codfilial;
            modo_debug.IsChecked = parametros.modo_debug ? true : false;
            eudireto_vendedor_id.Text = parametros.eudireto_vendedor_id.ToString();
            eudireto_api_usuario.Text = parametros.eudireto_api_usuario;
            eudireto_api_senha.Text = parametros.eudireto_api_senha;
            eudireto_api_host.Text = parametros.eudireto_api_host;
            eudireto_api_port.Text = parametros.eudireto_api_port.ToString();
            winthor_host.Text = parametros.winthor_host;
            winthor_port.Text = parametros.winthor_port.ToString();
            winthor_service_name.Text = parametros.winthor_service_name;
            winthor_user.Text = parametros.winthor_user;         
            winthor_key.Password =parametros.winthor_key;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               

                parametros.tempo_sincronismo_cad_produtos           = Convert.ToInt32(tempo_sincronismo_cad_produtos.Text);
                parametros.regiao_tbl_preco                         = Convert.ToInt32(regiao_tbl_preco.Text);
                parametros.codfilial                                = codfilial.Text;
                parametros.modo_debug                               = modo_debug.IsChecked==true? true:false;
                parametros.eudireto_vendedor_id                     = Convert.ToInt32(eudireto_vendedor_id.Text) ;
                parametros.eudireto_api_usuario                     = eudireto_api_usuario.Text  ;
                parametros.eudireto_api_senha                       = eudireto_api_senha.Text  ;
                parametros.eudireto_api_host                        = eudireto_api_host.Text  ;
                parametros.eudireto_api_port                        = Convert.ToInt32(eudireto_api_port.Text) ;
                parametros.winthor_host                             = winthor_host.Text  ;
                parametros.winthor_port                             = Convert.ToInt32( winthor_port.Text);
                parametros.winthor_service_name                     = winthor_service_name.Text;
                parametros.winthor_user                             = winthor_user.Text;
                parametros.winthor_key                              = winthor_key.Password;
                parametros.SalvarParametros();

                MessageBox.Show("Parâmetros salvos com sucesso.");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {/*
            try
            {
                PowerShell ps = PowerShell.Create();
                string cmd = "New-Service -Name \"EuDireto Sync\" -BinaryPathName \"" + AppDomain.CurrentDomain.BaseDirectory + "EuDiretoService.exe" + "\"";
                Clipboard.SetText(cmd);
                ps.AddCommand(cmd);
                ps.Invoke();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }*/
            this.Close();
        }
    }
}
