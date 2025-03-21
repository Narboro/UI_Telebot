using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.XWPF.UserModel;
using ICell = NPOI.SS.UserModel.ICell;

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
            con.Close();
            con.Dispose();
            return dt;
        }

        public void WriteExcel(IConfiguration _configuration, List<string>? enteredValue)
        {
            configuration = _configuration;
            String? filename = (configuration["BotConfiguration:LIBRARY_FILEPATH"] ?? "") + (configuration["BotConfiguration:LIBRARY_FILENAME"] ?? "");

            try
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite))
                {
                    IWorkbook workbook = new XSSFWorkbook(fs);
                    ISheet sheet = workbook.GetSheetAt(0); // Get the first sheet
                    IFormulaEvaluator evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();

                    for (int rw = 1; rw <= sheet.LastRowNum; rw++) // Start from 1 to skip header
                    {
                        IRow currentRow = sheet.GetRow(rw);
                        if (currentRow != null)
                        {
                            string? code = currentRow?.GetCell(22)?.ToString();
                            if (code == enteredValue?[0])
                            {
                                ICell c1 = currentRow?.GetCell(7);
                                if (c1 == null) c1 = currentRow?.CreateCell(7);

                                if (enteredValue?[3] == (string)null)
                                {
                                    c1?.SetCellValue(enteredValue?[3]);
                                    c1?.SetCellType(CellType.Blank);
                                }
                                else
                                {
                                    c1?.SetCellFormula($"DateValue(\"{enteredValue?[3]}\")");
                                    c1?.SetCellType(CellType.Formula);
                                }
                                evaluator.EvaluateFormulaCell(c1);
                                ICell? c2 = currentRow?.GetCell(8);
                                if (c2 == null) c2 = currentRow?.CreateCell(8);
                                c2?.SetCellValue(enteredValue?[1]);
                                ICell? c3 = currentRow?.GetCell(9);
                                if (c3 == null) c3 = currentRow?.CreateCell(9);
                                c3?.SetCellValue(enteredValue?[2]);
                            }
                        }
                    }

                    // Save the changes
                    using (FileStream fsOut = new FileStream(filename, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(fsOut);
                    }
                }
            } catch(Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
