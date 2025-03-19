using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace UI_Telebot.Model
{
    class ExcelDbReader
    {
        private IConfiguration? configuration;
        public DataTable ReadExcel(IConfiguration _configuration)
        {
            configuration = _configuration;
            String filename = (configuration["BotConfiguration:LIBRARY_FILEPATH"] ?? "") + (configuration["BotConfiguration:LIBRARY_FILENAME"] ?? "");
            String connection = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filename + ";Extended Properties=\"Excel 12.0 Xml;HDR=YES;\"";
            OleDbConnection con = new OleDbConnection(connection);
            String Command = "Select * from [Лист1$]";
            OleDbCommand cmd = new OleDbCommand(Command, con);
            OleDbDataAdapter db = new OleDbDataAdapter(cmd);
            con.Open();
            DataTable dt = new DataTable();
            db.Fill(dt);

            return dt;
        }
    }
}
