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

namespace Configs
{
    /// <summary>
    /// Interação lógica para Gerais.xam
    /// </summary>
    public partial class Gerais : Page
    {
        public Gerais()
        {
            InitializeComponent();
            ArquivoExterno config = new ArquivoExterno();
            config.CarregarConfiguracoes();
            tbMinSincProdutos.Text = config.tempo_sincronismo_cad_produtos.ToString(); 

        }
    }
}
