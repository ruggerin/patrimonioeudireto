﻿using System;
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
        }
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Criptografia criptografia = new Criptografia(Crypto.CryptProvider.RC2);
            criptografia.Key = "23ko84jezk"; // chave
            txCodificado.Text = criptografia.Encrypt(txOriginal.Text);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Criptografia criptografia = new Criptografia(CryptProvider.RC2);
            criptografia.Key = "23ko84jezk";
            txDecodificado.Text = criptografia.Decrypt(txCodificado.Text);
        }
    }
}
