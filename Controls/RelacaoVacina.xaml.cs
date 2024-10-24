﻿using CRM.conexao;
using CRM.Conexao;
using OfficeOpenXml;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace CRM.Controls
{
    
    public partial class RelacaoVacina : UserControl
    {
        conexaoCRM conexao = new conexaoCRM();
        private DataTable _dataTable;
        private conexaoMouraVacina dbHelper;
        public RelacaoVacina()
        {
            InitializeComponent();
            SetupDates();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            dbHelper = new conexaoMouraVacina();
        }

        private void SetupDates()
        {
            DateTime today = DateTime.Now;
            DateTime startDate = today.AddMonths(-11).AddDays(-28);
            dataInicial.Text = startDate.ToString("dd/MM/yyyy");
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

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime startDate = DateTime.ParseExact(dataInicial.Text, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                DateTime endDate = startDate;

                progressBar.Visibility = Visibility.Visible;
                progressBar.IsIndeterminate = true;

                _dataTable = await Task.Run(() => dbHelper.FetchData(startDate, endDate));

                if (_dataTable != null && _dataTable.Rows.Count > 0)
                {
                    lblTotalRecords.Content = $"Total de registros: {_dataTable.Rows.Count}";
                    btnExportarExcel.IsEnabled = true;
                }
                else
                {
                    lblTotalRecords.Content = "Nenhum registro encontrado.";
                    btnExportarExcel.IsEnabled = false;
                }

                progressBar.Visibility = Visibility.Collapsed;
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"Erro ao converter a data: {ex.Message}");
            }
        }

        private async void btnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_dataTable == null || _dataTable.Rows.Count == 0)
            {
                MessageBox.Show("Nenhum dado para exportar.");
                return;
            }
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string relacoesPath = System.IO.Path.Combine(desktopPath, "Relações");
            if (!Directory.Exists(relacoesPath))
            {
                Directory.CreateDirectory(relacoesPath);
            }
            string outputPath = System.IO.Path.Combine(relacoesPath, "Vacina.xlsx");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputPath));

            // Filtrar as colunas desejadas
            DataTable filteredTable = FilterColumns(_dataTable);

            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = true;

            await Task.Run(() => SaveToExcel(filteredTable, outputPath));

            progressBar.Visibility = Visibility.Collapsed;

            MessageBox.Show($"Arquivo salvo!", "Concluído", MessageBoxButton.OK);
            conexao.AtualizarExecucao("vacina");
        }

        private DataTable FilterColumns(DataTable dataTable)
        {
            DataTable filteredTable = new DataTable();

            filteredTable.Columns.Add("nome", typeof(string));
            filteredTable.Columns.Add("Data", typeof(string));
            filteredTable.Columns.Add("fone", typeof(string));
            filteredTable.Columns.Add("Pet", typeof(string));
            filteredTable.Columns.Add("Serviço", typeof(string));

            var filteredRows = dataTable.AsEnumerable()
                .Where(row => !row["Proprietario"].ToString().Contains("#") &&
                              !row["Proprietario"].ToString().Contains("@") &&
                              !row["Proprietario"].ToString().Contains("&") &&
                              !row["Proprietario"].ToString().Contains("HUBBI") &&
                              !row["Proprietario"].ToString().Contains("MERCADO LIVRE") &&
                              !row["Proprietario"].ToString().Contains("CONSUMIDOR FINAL"));

            foreach (var row in filteredRows)
            {
                DataRow newRow = filteredTable.NewRow();
                newRow["nome"] = row["Proprietario"];
                newRow["Data"] = Convert.ToDateTime(row["data"]).ToString("dd/MM/yyyy");
                newRow["fone"] = FormatPhoneNumber(row["fone"].ToString());
                newRow["Pet"] = row["nome_animal"];
                newRow["Serviço"] = row["Servico"];
                filteredTable.Rows.Add(newRow);
            }

            return filteredTable;
        }

        private void SaveToExcel(DataTable dataTable, string filepath)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Dados");
                worksheet.Cells["A1"].LoadFromDataTable(dataTable, true);

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                package.SaveAs(new FileInfo(filepath));
            }
        }

    }
}