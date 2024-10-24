﻿using ClosedXML.Excel;
using CRM.Conexao;
using CRM.Views;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace CRM.Controls
{

    public partial class RelacaoAntiparasitario : UserControl
    {
        private conexaoCRM _conexaoCRM;
        string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Relações");

        public RelacaoAntiparasitario()
        {
            InitializeComponent();
            _conexaoCRM = new conexaoCRM();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StartProcessing();
        }

        private async void StartProcessing()
        {
            ProgressBar.IsEnabled = true;
            await ProcessarDados();
            ProgressBar.Value = 100;
            Antiparasitario_Concluido();
        }

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return phoneNumber;

            var digits = Regex.Replace(phoneNumber, @"[^\d]", "");

            if (digits.Length == 11)
            {
                return $"(+55) {digits.Substring(0, 2)} {digits.Substring(2, 5)}-{digits.Substring(7, 4)}";
            }
            else if (digits.Length == 10)
            {
                return $"(+55) {digits.Substring(0, 2)} {digits.Substring(2, 4)}-{digits.Substring(6, 4)}";
            }
            else if (digits.Length == 9)
            {
                return $"(+55) 31 {digits.Substring(0, 5)}-{digits.Substring(5, 4)}";
            }
            else if (digits.Length == 8)
            {
                return $"(+55) 31 {digits.Substring(0, 4)}-{digits.Substring(4, 4)}";
            }

            return phoneNumber;
        }

        private async Task ProcessarDados()
        {
            await Task.Run(() =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressLabel.Content = "Carregando arquivo de entrada...";
                        ProgressBar.Value = 17;
                    });
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var inputFilePath = System.IO.Path.Combine(desktopPath, "Banco.xlsx");
                    string relacoesPath = Path.Combine(desktopPath, "Relações");

                    if (!Directory.Exists(relacoesPath))
                    {
                        Directory.CreateDirectory(relacoesPath);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        ProgressLabel.Content = "Processando dados...";
                        ProgressBar.Value = 34;
                    });

                    var outputFilePath = System.IO.Path.Combine(relacoesPath, "Antiparasitario.xlsx");

                    var workbook = new XLWorkbook(inputFilePath);
                    var worksheet = workbook.Worksheet(1);
                    var dataTable = worksheet.RangeUsed().AsTable().AsNativeDataTable();

                    dataTable.Columns.Add("Grupo", typeof(int));
                    dataTable.Columns.Add("DataFiltro", typeof(string));

                    var caracteresIndesejados = new List<string> { "@", "*", "#", "MERCADO LIVRE", "HUBBI", "CONSUMIDOR FINAL" };
                    var pattern = string.Join("|", caracteresIndesejados.Select(Regex.Escape));

                    var filteredRows = dataTable.AsEnumerable()
                        .Where(row => !Regex.IsMatch(row.Field<string>("nome"), pattern))
                        .ToList();

                    var dataAtual = DateTime.Today;
                    var isSegundaFeira = dataAtual.DayOfWeek == DayOfWeek.Monday;

                    var gruposProdutos = new Dictionary<int, List<int>>
                    {
                        { 13, new List<int> { 52333 } },
                        { 27, new List<int> { 3996 } },
                        { 29, new List<int> { 964, 725, 722, 11, 15, 493, 499, 494, 47211, 48769, 49689, 43919, 41404, 52799, 51370, 731, 43616 } },
                        { 34, new List<int> { 38103, 45158, 45156 } },
                        { 36, new List<int> { 52966, 52967, 52968, 52965, 52969 } },
                        { 47, new List<int> { 45640 } },
                        { 83, new List<int> { 152, 39382, 39150, 39740, 39772, 43785, 49816, 49739, 49738 } },
                        { 89, new List<int> { 41464, 4166, 4421, 4165, 4164, 4163 } },
                        { 104, new List<int> { 49720, 49722, 51354, 51949, 49721, 49723 } },
                        { 119, new List<int> { 318 } },
                        { 143, new List<int> { 45640 } },
                        { 149, new List<int> { 48778 } },
                        { 167, new List<int> { } },
                        { 179, new List<int> { 2060, 498, 310 } },
                        { 239, new List<int> { 320, 41533 } }
                    };

                    var resultadosCompletos = new DataTable();

                    resultadosCompletos.Columns.Add("nome", typeof(string));
                    resultadosCompletos.Columns.Add("fone", typeof(string));
                    resultadosCompletos.Columns.Add("fone2", typeof(string));
                    resultadosCompletos.Columns.Add("Nome_Produto", typeof(string));
                    resultadosCompletos.Columns.Add("Data da Venda", typeof(string));
                    resultadosCompletos.Columns.Add("Grupo", typeof(int));

                    foreach (var grupo in gruposProdutos)
                    {
                        var dias = grupo.Key;
                        var produtos = grupo.Value;

                        Dispatcher.Invoke(() =>
                        {
                            ProgressLabel.Content = $"Processando grupo de {dias} dias...";
                            ProgressBar.Value = 51;
                        });

                        var datasFiltro = new List<string>
                        {
                            dataAtual.AddDays(-dias).ToString("dd/MM/yyyy")
                        };

                        if (isSegundaFeira)
                        {
                            datasFiltro.Add(dataAtual.AddDays(-dias - 1).ToString("dd/MM/yyyy"));
                            datasFiltro.Add(dataAtual.AddDays(-dias - 2).ToString("dd/MM/yyyy"));
                            datasFiltro.Add(dataAtual.AddDays(-dias - 3).ToString("dd/MM/yyyy"));
                        }

                        foreach (var dataFiltro in datasFiltro)
                        {
                            var dfFiltrado = filteredRows
                                .Where(row =>
                                {
                                    var produto = row.Field<object>("Produto");
                                    int produtoInt;

                                    // Tentar converter para int, se falhar, usar um valor padrão ou pular a linha
                                    if (produto != null && int.TryParse(produto.ToString(), out produtoInt))
                                    {
                                        return row.Field<string>("Data da Venda") == dataFiltro &&
                                               produtos.Contains(produtoInt);
                                    }

                                    return false;
                                })
                                .ToList();

                            foreach (var row in dfFiltrado)
                            {
                                var newRow = resultadosCompletos.NewRow();
                                newRow["nome"] = row["Nome"];
                                newRow["fone"] = FormatPhoneNumber(row["fone"].ToString());
                                newRow["fone2"] = FormatPhoneNumber(row["fone2"].ToString());
                                newRow["Nome_Produto"] = row["Nome_Produto"];
                                newRow["Data da Venda"] = row["Data da Venda"];
                                newRow["Grupo"] = dias;

                                resultadosCompletos.Rows.Add(newRow);
                            }
                        }
                        Dispatcher.Invoke(() =>
                        {
                            ProgressLabel.Content = "Salvando resultados...";
                            ProgressBar.Value = 68;
                        });
                    }

                    var newWorkbook = new XLWorkbook();
                    var newWorksheet = newWorkbook.Worksheets.Add("Resultado");
                    newWorksheet.Cell(1, 1).InsertTable(resultadosCompletos);
                    newWorkbook.SaveAs(outputFilePath);

                    Dispatcher.Invoke(() =>
                    {
                        ProgressLabel.Content = "Processo concluído!";
                        ProgressBar.Value = 85;
                        MessageBox.Show($"Arquivo salvo: {outputFilePath}", "Concluído", MessageBoxButton.OK);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        private void Antiparasitario_Concluido()
        {
            _conexaoCRM.AtualizarExecucaoVerificar("antiparasitario");
        }

    }
}