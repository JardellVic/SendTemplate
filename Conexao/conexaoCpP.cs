﻿using System.Data.SqlClient;
using System.Data;

namespace CRM.Conexao
{
    class conexaoCpP
    {
        private readonly string connectionString = "Server=cloudecia.jnmoura.com.br,1504;Database=CAESECIAMG_10221;User Id=CAESECIAMG_10221_POWERBI;Password=KI8msYRpRsRJifEw2ouw;TrustServerCertificate=True;";

        public DataTable FetchData(DateTime startDate, DateTime endDate)
        {
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                SELECT 
                    p.codigo,
                    p.nome,
                    p.fone,
                    p.fone2,
                    vi.Produto,
                    pr.Nome AS Nome_Produto
                FROM 
                    pessoa p
                INNER JOIN 
                    cliente c ON p.Codigo = c.Pessoa
                INNER JOIN 
                    venda v ON v.cliente = c.Pessoa
                INNER JOIN 
                    venda_item vi ON vi.venda = v.Codigo
                INNER JOIN 
                    produto pr ON vi.Produto = pr.codigo
                WHERE 
                    v.data BETWEEN @startDate AND @endDate
                ORDER BY 
                    vi.venda DESC;
                ";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@startDate", startDate);
                cmd.Parameters.AddWithValue("@endDate", endDate);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }
    }
}
