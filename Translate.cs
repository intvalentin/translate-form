using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;


namespace Translate
{
    class Translate
    {
        public Translate()
        {

        }
        public Translate(string dirImport, string dirExport)
        {
            //TranslateFile(dirImport, dirExport);
        }

        private class Node
        {
            public string name, value;
            public Node parent = null;
            public Dictionary<string, Node> childs = new Dictionary<string, Node>();
            public int childsNumber = 0, height = 0, getNumber;
            public bool addCurly = false;
        }


        private string PrintTree(Node a, bool hadMoreC, int nr)
        {
            string jsonConversion = "";

            if (a.name != "ROOT")
                jsonConversion += "\"" + a.name + "\": ";
            if (a.childsNumber > 0)
            {
                hadMoreC = true;
                jsonConversion += Environment.NewLine + "{" + Environment.NewLine;
            }
            if (a.value != null && nr >= 1 && a.getNumber < a.parent.childsNumber)
            {
                jsonConversion += "\"" + a.value + "\"," + Environment.NewLine;

            }


            if (a.value != null && a.parent.childsNumber == a.getNumber)
            {
                jsonConversion += "\"" + a.value + "\"" + Environment.NewLine;
                Node op = a;
                while (op.parent != null)
                {
                    if (op.parent.parent != null && op.parent.getNumber < op.parent.parent.childsNumber)
                    {
                        jsonConversion += "}," + Environment.NewLine;
                        break;
                    }
                    else if (op.parent.parent != null && op.parent.getNumber == op.parent.parent.childsNumber)
                    {
                        jsonConversion += "}" + Environment.NewLine;
                    }
                    else
                    {
                        jsonConversion += "}" + Environment.NewLine;
                    }
                    op = op.parent;
                }
            }

            foreach (var x in a.childs)
            {
                if (x.Value.parent.childs.Count > 0)
                    jsonConversion += PrintTree(x.Value, true, nr);
                else
                    jsonConversion += PrintTree(x.Value, false, nr);

            }
            return jsonConversion;
        }


        public byte[] TranslateFile(byte[] data,string name)
        {
           
            if (name == "excel")
                return ExportFileAsJson(data);
            else 
            {
                return ExportFileAsExcel(data);
            }

            throw new Exception("Error! Name of file is not valid");
        }

        //Export excel as json
        public byte[] ExportFileAsJson(byte[] file)
        {
            try
            {
                Node startNode = new Node(), aux, lastNode = startNode, endNode = null;
                startNode.name = "ROOT";

                byte[] bin = file;
                List<string> excelData = new List<string>();


                //create a new Excel package in a memorystream
                using (MemoryStream stream = new MemoryStream(bin))
                using (ExcelPackage excelPackage = new ExcelPackage(stream))
                {
                    //loop all worksheets
                    foreach (ExcelWorksheet worksheet in excelPackage.Workbook.Worksheets)
                    {
                        //loop all rows
                        for (int i = 2; i <= worksheet.Dimension.End.Row; i++)
                        {
                            //loop all columns in a row
                            for (int j = worksheet.Dimension.Start.Column; j <= worksheet.Dimension.End.Column; j++)
                            {
                                //add the cell data to the List
                                if (worksheet.Cells[i, j].Value != null)
                                {

                                    string[] rowReader;
                                    //check if first or second column
                                    if (j % 2 != 0)
                                    {
                                        //split first row by '.'
                                        rowReader = worksheet.Cells[i, j].Value.ToString().Split('.');

                                        for (int k = 0; k < rowReader.Length; k++)
                                        {
                                            if (!lastNode.childs.ContainsKey(rowReader[k]))
                                            {
                                                aux = new Node();
                                                aux.name = rowReader[k];
                                                aux.parent = lastNode;
                                                aux.height = aux.parent.height + 1;
                                                aux.getNumber = lastNode.childsNumber + 1;
                                                lastNode.childs.Add(aux.name, aux);
                                                lastNode.childsNumber++;
                                                lastNode = aux;
                                                endNode = aux;

                                            }
                                            else
                                            {
                                                lastNode = lastNode.childs[rowReader[k]];
                                                endNode = lastNode;
                                            }
                                        }

                                        lastNode = startNode;
                                    }
                                    else
                                    {
                                        endNode.value = worksheet.Cells[i, j].Value.ToString();
                                    }



                                }
                            }
                        }
                    }
                }

                // Set a variable to the Documents path.
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                BinaryFormatter bf = new BinaryFormatter();
                using (var convertByte = new MemoryStream())
                {
                    string testFail = PrintTree(startNode, true, startNode.childsNumber);
                    Console.WriteLine(testFail);
                    //Convert tree to json in string
                    //Convert the string to json object
                    JObject o = JObject.Parse(PrintTree(startNode, true, startNode.childsNumber));

                    bf.Serialize(convertByte, JsonConvert.SerializeObject(o));
                    return convertByte.ToArray();
                }
            }
            catch(Exception e)
            {
                throw new Exception("Error! Something went wrong!" +e);
            }

        }

        //Export file to json
        //private void ExportFileAsExcel(string dirImport, string dirExport)
        public byte[] ExportFileAsExcel(byte[] file)
        {
            //Get file encoding
            Encoding enc;

            //To read json file
            string jsonFile;
            //Save last tag so if there are duplicates we skip them
            string lastTag = "";

            //Create new excel file 
            using (ExcelPackage excel = new ExcelPackage())
            {
                excel.Workbook.Worksheets.Add("Worksheet1");

                List<string[]> headerRow = new List<string[]>()
                {
                    new string[] { "TAG" , "VALOARE" }
                };

                //Choose a worksheet
                var excelWorksheet = excel.Workbook.Worksheets["Worksheet1"];
                excelWorksheet.Cells[1, 1].Value = "TAG";
                excelWorksheet.Cells[1, 2].Value = "VALUE";
                // Setting the properties 
                // of the first row 
                excelWorksheet.Row(1).Height = 20;
                excelWorksheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                excelWorksheet.Row(1).Style.Font.Bold = true;

                using (StreamReader r = new StreamReader(new MemoryStream(file)))
                {
                    r.Peek();
                    enc = r.CurrentEncoding;
                }
                using (StreamReader reader = new StreamReader(new MemoryStream(file), enc))
                {
                    jsonFile = reader.ReadToEnd();
                    Stack<string> stack = new Stack<string>();

                    try
                    {

                        var jobj = JObject.Parse(jsonFile);
                        JsonTextReader r = new JsonTextReader(new StringReader(jsonFile));
                        int recordIndex = 2;
                        while (r.Read())
                        {
                            if (r.Value != null)
                            {
                                if (r.TokenType.ToString() == "PropertyName" && stack.Count > 0)
                                {
                                    stack.Push(stack.Peek() + "." + r.Value.ToString());
                                }
                                else if (r.TokenType.ToString() == "PropertyName" && stack.Count == 0)
                                    stack.Push(r.Value.ToString());
                                if (r.TokenType.ToString() != "PropertyName")
                                {
                                    if(lastTag != stack.Peek())
                                    {
                                        lastTag = stack.Peek();
                                        excelWorksheet.Cells[recordIndex, 1].Value = stack.Pop();
                                        excelWorksheet.Cells[recordIndex, 2].Value = r.Value.ToString();
                                        recordIndex++;
                                    }
                                    else
                                    {
                                        stack.Pop();
                                    }
                                    
                                    
                                }
                            }
                            else
                            {
                                if (stack.Count > 0 && r.TokenType.ToString() == "EndObject")
                                    stack.Pop();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Fisierul JSON nu este valid!");
                        Console.WriteLine(e);
                    }
                }
                excelWorksheet.Column(1).AutoFit();
                excelWorksheet.Column(2).AutoFit();

                return excel.GetAsByteArray();
            }
        }

        public void saveJsonFileToDisk(string dir, byte[] file)
        {
            //Example call to generate json file from excel and how to create the file
            //Function is called with byte[] of that file and returns byte[]
            using (var memStream = new MemoryStream())
            {
                
                var binForm = new BinaryFormatter();
                memStream.Write(file, 0, file.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                obj = JObject.Parse(obj.ToString());
                File.WriteAllText(dir, obj.ToString());
            }
        }
        public void saveExcelFileToDisk(string p_strPath, byte[] file)
        {
            //Example call to generate excel file from json and how to create the file
            //Function is called with byte[] of that file and return byte[]
            using (var memStream = new MemoryStream())
            {
              
                var binForm = new BinaryFormatter();
                memStream.Write(file, 0, file.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                // var obj = binForm.Deserialize(memStream);
                //obj = JObject.Parse(obj.ToString());


                if (File.Exists(p_strPath))
                    File.Delete(p_strPath);

                // Create excel file on physical disk  
                FileStream objFileStrm = File.Create(p_strPath);
                objFileStrm.Close();

                // Write content to excel file  
                File.WriteAllBytes(p_strPath, file);
            }
        }
    }
}
