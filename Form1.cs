using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Translate
{
    public partial class form1 : Form
    {
        DialogResult resultImport,resultExport ;
        private string ftype;
        public form1()
        {
            InitializeComponent();
        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e)
        {
            openDialog();
            label3.Text = "";
            textBox2.Text = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openDialog();
            label3.Text = "";
            textBox2.Text = "";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            label3.Text = "";
            openExport();
        }
        private void textBox2_MouseClick(object sender, MouseEventArgs e)
        {
            label3.Text = "";
            openExport();
        }
        private void openDialog()
        {
            this.resultImport = Import.ShowDialog();
           
            try
            {
                textBox1.Text = Import.FileName;
                string[] fileType = Import.FileName.Split('.');
                this.ftype = fileType[fileType.Length - 1].ToUpper();

                label1.Text = "Imported " + ftype + " file";
                if (this.ftype == "JSON")
                {  
                    string name = fileType[fileType.Length - 2];
                    label2.Text = "Export as EXCEL(.xlsx)";
                    Export.FileName = name;
                    Export.DefaultExt = ".xlsx";
                }
                else
                {
                    string name = fileType[fileType.Length - 2];
                    label2.Text = "Export as JSON(.json)";
                    Export.FileName = name;
                    Export.DefaultExt = ".json";


                }
                if (resultImport != DialogResult.OK)
                {
                    textBox1.Text = "";
                    label1.Text = "";
                    label2.Text = "";
                    label3.Text = "";
                }

            }
            catch
            {
                if (resultImport != DialogResult.OK)
                {
                    textBox1.Text = "";
                    label1.Text = "";
                    label2.Text = "";
                    label3.Text = "";
                }
            }
        }
       
        private void openExport()
        {
            if (this.resultImport == DialogResult.OK)
            {   
                resultExport = Export.ShowDialog();
                if(resultExport == DialogResult.OK)
                    saveFile();
            }
            else
            {
                MessageBox.Show("Please import a file first!", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            
        }

        private void saveFile()
        {
            try
            {
                Translate tr = new Translate();
                if (this.ftype == "JSON")
                {
                    byte[] excel = tr.TranslateFile(File.ReadAllBytes(@Import.FileName), "json");
                    tr.saveExcelFileToDisk(@Export.FileName, excel);
                    textBox2.Text = Export.FileName;
                    label3.Text = "Succes!";
                }
                else
                {
                    textBox2.Text = Export.FileName;
                    byte[] json = tr.TranslateFile(File.ReadAllBytes(@Import.FileName), "excel");
                    tr.saveJsonFileToDisk(@Export.FileName, json);
                    label3.Text = "Succes!";
                }



            }
            catch(Exception e)
            {
                textBox1.Text = "";
                MessageBox.Show("Please import a file first!   "+e, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

       
    }
}
