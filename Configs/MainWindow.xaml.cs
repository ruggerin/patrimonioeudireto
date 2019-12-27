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
using Crypto;




namespace Configs
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ConfiguracoesGerais configuracoesGerais = new ConfiguracoesGerais();
            tabWindows.NavigationService.Content = configuracoesGerais;
        }
        //Abrir tela de configurações gerais
        private void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConfiguracoesGerais configuracoesGerais = new ConfiguracoesGerais();
            tabWindows.NavigationService.Content = configuracoesGerais;
            
        }


        /*  Criptografia criptografia = new Criptografia(CryptProvider.RC2);
            criptografia.Key = "23ko84jezk";
            txDecodificado.Text = criptografia.Decrypt(txCodificado.Text);

            Criptografia criptografia = new Criptografia(Crypto.CryptProvider.RC2);
            criptografia.Key = "23ko84jezk"; // chave
            txCodificado.Text = criptografia.Encrypt(txOriginal.Text);

         */
    }
}
