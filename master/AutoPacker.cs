﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace TTG_Tools
{
    public partial class AutoPacker : Form
    {
        public AutoPacker()
        {
            InitializeComponent();
        }

        public static string ConvertHexToString(byte[] binContent, int poz, int len_string)
        {
            byte[] temp_hex_string = new byte[len_string];
            Array.Copy(binContent, poz, temp_hex_string, 0, len_string);
            return ASCIIEncoding.GetEncoding(MainMenu.settings.ASCII_N).GetString(temp_hex_string);
        }

        public static FileInfo[] fi;
        public static FileInfo[] fi_temp;
        //public static List<TextureWorker.Texture_format> tex_format = new List<TextureWorker.Texture_format>(); //Список с форматами текстур

        public static string customKey;
        public static int numKey;
        public static int selected_index;
        public static int EncVersion;
        public static bool encLangdb;
        public static bool encDDSonly;
        public static bool custKey;
        public static bool tsvFile;
        public static bool isIOS;

        public struct langdb
        {
            public byte[] head;
            public byte[] hz_data;
            public byte[] lenght_of_name;
            public string name;
            public byte[] lenght_of_text;
            public string text;
            public byte[] lenght_of_waw;
            public string waw;
            public byte[] lenght_of_animation;
            public string animation;
            public byte[] magic_bytes;
            public byte[] realID;
        }


        public static int number;
        langdb[] database = new langdb[5000];

        void AddNewReport(string report)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new ReportHandler(AddNewReport), report);
            }
            else
            {
                listBox1.Items.Add(report);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                listBox1.SelectedIndex = -1;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(MainMenu.settings.pathForInputFolder);
                fi = di.GetFiles();
            }
            catch
            {
                MessageBox.Show("Open and close program or fix path in config.xml!", "Error!");
                goto end2; 
            }

            encLangdb = checkEncLangdb.Checked;
            encDDSonly = checkEncDDS.Checked;
            custKey = checkCustomKey.Checked;

            if (checkUnicode.Checked) MainMenu.settings.unicodeSettings = 0;
            else MainMenu.settings.unicodeSettings = 1;

            tsvFile = tsvFilesRB.Checked;
            
            EncVersion = 2;
            if (comboBox2.SelectedIndex == 1) EncVersion = 7;
            
            customKey = textBox1.Text;
            isIOS = checkIOS.Checked;

            //if (comboBox1.SelectedIndex == 0 || comboBox1.SelectedIndex == -1)

            string versionOfGame = " ";// CheckVersionOfGameFromCombobox(comboBox1.SelectedIndex);
            numKey = comboBox1.SelectedIndex;
            selected_index = comboBox2.SelectedIndex;

            //Создаем нить для импорта текстур в DDS
            var processD3DTX = new ForThreads();
            processD3DTX.ReportForWork += AddNewReport;
            List<string> parametresD3DTX = new List<string>();
            parametresD3DTX.Add(versionOfGame);
            parametresD3DTX.Add(".dds");
            parametresD3DTX.Add(MainMenu.settings.pathForInputFolder);
            parametresD3DTX.Add(MainMenu.settings.pathForOutputFolder);
            parametresD3DTX.Add(MainMenu.settings.deleteD3DTXafterImport.ToString());
            parametresD3DTX.Add(MainMenu.settings.deleteDDSafterImport.ToString());
            var threadD3DTX = new Thread(new ParameterizedThreadStart(processD3DTX.DoImportEncoding));
            threadD3DTX.Start(parametresD3DTX);

            ////Создаем нить для импорта текста в LANDB
            //var processLANDB = new ForThreads();
            //processLANDB.ReportForWork += AddNewReport;
            //List<string> parametresLANDB = new List<string>();
            //parametresLANDB.Add(".landb");
            //parametresLANDB.Add(".txt");
            //parametresLANDB.Add(MainMenu.settings.pathForInputFolder);
            //parametresLANDB.Add(MainMenu.settings.pathForOutputFolder);
            //parametresLANDB.Add(MainMenu.settings.pathForTempFolder);
            //parametresLANDB.Add(false.ToString());
            //parametresLANDB.Add(false.ToString());
            //var threadLANDB = new Thread(new ParameterizedThreadStart(processLANDB.DoImportEncoding));
            //threadLANDB.Start(parametresLANDB);

            ////Создаем нить для импорта текста в LANGDB
            //var processLANGDB = new ForThreads();
            //processLANGDB.ReportForWork += AddNewReport;
            //List<string> parametresLANGDB = new List<string>();
            //parametresLANGDB.Add(".langdb");
            //parametresLANGDB.Add(".txt");
            //parametresLANGDB.Add(MainMenu.settings.pathForInputFolder);
            //parametresLANGDB.Add(MainMenu.settings.pathForOutputFolder);
            //parametresLANGDB.Add(MainMenu.settings.pathForTempFolder);
            //parametresLANGDB.Add(false.ToString());
            //parametresLANGDB.Add(false.ToString());
            //var threadLANGDB = new Thread(new ParameterizedThreadStart(processLANGDB.DoImportEncoding));
            //threadLANGDB.Start(parametresLANGDB);
            ////Работаем дальше


            for (int i = 0; i < fi.Length; i++)
            {
                if ((fi[i].Extension == ".lua") || (fi[i].Extension == ".lenc"))
                {
                    byte[] encKey;

                    if (custKey)
                    {
                        customKey = textBox1.Text;
                        encKey = Methods.stringToKey(customKey);

                        if (encKey == null)
                        {
                            MessageBox.Show("You must enter key encryption!", "Error");
                            goto end2;
                        }
                    }
                    else encKey = MainMenu.gamelist[comboBox1.SelectedIndex].key;

                    int version;
                    if (selected_index == 0) version = 2;
                    else version = 7;

                    FileStream fs = new FileStream(fi[i].FullName, FileMode.Open);
                    byte[] luaContent = Methods.ReadFull(fs);
                    fs.Close();

                    luaContent = Methods.encryptLua(luaContent, encKey, CheckNewEngine.Checked, version);

                    if (File.Exists(TTG_Tools.MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name)) File.Delete(TTG_Tools.MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name);
                    fs = new FileStream(TTG_Tools.MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name, FileMode.CreateNew);
                    fs.Write(luaContent, 0, luaContent.Length);
                    fs.Close();

                    listBox1.Items.Add("File " + fi[i].Name + " encrypted.");
                }
                else if (fi[i].Extension == ".font")
                {
                    int version;
                    if (selected_index == 0) version = 2;
                    else version = 7;



                    FileStream fs = new FileStream(fi[i].FullName, FileMode.Open);
                    byte[] fontContent = Methods.ReadFull(fs);
                    fs.Close();

                    byte[] checkHeader = new byte[4];
                    Array.Copy(fontContent, 0, checkHeader, 0, 4);
                    if ((Encoding.ASCII.GetString(checkHeader) != "5VSM") && (Encoding.ASCII.GetString(checkHeader) != "ERTM"))
                    {

                        if (Methods.FindStartOfStringSomething(fontContent, 0, "DDS ") < fontContent.Length - 100)
                        {
                            if (version == 2)
                            {
                                //Шифруем заголовок текстуры
                                int poz = Methods.FindStartOfStringSomething(fontContent, 0, "DDS ");
                                byte[] tempHeader = new byte[2048];
                                if (fontContent.Length - poz < tempHeader.Length) tempHeader = new byte[fontContent.Length - poz];

                                Array.Copy(fontContent, poz, tempHeader, 0, tempHeader.Length);
                                BlowFishCS.BlowFish encHeader = new BlowFishCS.BlowFish(MainMenu.gamelist[numKey].key, version);

                                tempHeader = encHeader.Crypt_ECB(tempHeader, version, false);

                                Array.Copy(fontContent, poz, tempHeader, 0, 2048);
                            }
                        }


                        //Шифруем шрифт
                        Methods.meta_crypt(fontContent, MainMenu.gamelist[numKey].key, version, false);

                        fs = new FileStream(MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name, FileMode.OpenOrCreate);
                        fs.Write(fontContent, 0, fontContent.Length);
                        fs.Close();

                        listBox1.Items.Add("File " + fi[i].Name + " encrypted!");

                    }
                }
                if (i + 1 < fi.Count())
                {
                    bool work = false;
                    int offset1 = 0;
                    int offset2 = 1;

                    if (fi[i].Extension == ".dlog" && fi[i + 1].Extension == ".txt" && GetNameOnly(i) == GetNameOnly(i + 1))
                    {
                        offset1 = 0;
                        offset2 = 1;
                        work = true;
                    }
                    if (fi[i + 1].Extension == ".dlog" && fi[i].Extension == ".txt" && Methods.DeleteCommentary(GetNameOnly(i), "(", ")") == GetNameOnly(i + 1))
                    {
                        offset1 = 1;
                        offset2 = 0;
                        work = true;
                    }

                    if (work)
                    {
                        List<Langdb> database = new List<Langdb>();
                        FileStream fs = new FileStream(fi[i + offset1].FullName, FileMode.Open);
                        byte[] binContent = Methods.ReadFull(fs);
                        byte version = 0;
                        ReadDlog(binContent, first_database, database, version);
                        int size_first = BitConverter.ToInt32(first_database[0].lenght_of_langdb1, 0);
                        fs.Close();
                        if (database.Count != 0)
                        {
                            List<TextCollector.TXT_collection> all_text = new List<TextCollector.TXT_collection>();
                            string error = string.Empty;
                            ImportTXT(fi[i + offset2].FullName, ref all_text, false, MainMenu.settings.ASCII_N, "\r\n", ref error);
                            for (int q = 0; q < all_text.Count; q++)
                            {
                                if (BitConverter.ToInt32(database[all_text[q].number - 1].lenght_of_textblok, 0) != 8)
                                {
                                    //if (BitConverter.ToInt32(database[all_text[q].number - 1 + propusk].count_text, 0) == 1)
                                    //{

                                    database[all_text[q].number - 1].text = all_text[q].text.Replace("\r\n", "\n");
                                    database[all_text[q].number - 1].lenght_of_text = BitConverter.GetBytes(database[all_text[q].number - 1].text.Length);

                                    /*database[all_text[q].number - 1].text = all_text[q].text.Replace("\r\n", "\n");
                                    database[all_text[q].number - 1].lenght_of_text = BitConverter.GetBytes(database[all_text[q].number - 1].text.Length);*/

                                    //}
                                    //else if (BitConverter.ToInt32(database[all_text[q].number - 1 + propusk].count_text, 0) == 2)
                                    //{
                                    //    database[all_text[q].number - 1 - propusk].text = all_text[q].text;
                                    //    database[all_text[q].number - 1 - propusk].lenght_of_text = BitConverter.GetBytes(database[all_text[q].number - 1].text.Length);
                                    //    q++;
                                    //    database[all_text[q].number - 1 - propusk].text = all_text[q].text;
                                    //    database[all_text[q].number - 1 - propusk].lenght_of_text = BitConverter.GetBytes(database[all_text[q].number - 1].text.Length);
                                    //    propusk++;
                                    //}
                                }
                            }
                            Methods.DeleteCurrentFile(MainMenu.settings.pathForOutputFolder + "\\" + fi[i + offset1].Name.ToString());
                            CreateDlog(first_database, database, 0, (MainMenu.settings.pathForOutputFolder + "\\" + fi[i + offset1].Name.ToString()));
                            listBox1.Items.Add("File " + fi[i + offset2].Name + " imported in " + fi[i + offset1].Name);
                        }
                        else
                        {
                            listBox1.Items.Add("File " + fi[i + offset1].Name + " is EMPTY!");
                        }
                        i++;
                        work = false;
                    }
                }
            }
            end2:
            int konec = 0;
        }

        public static List<TextCollector.TXT_collection> ImportTSV(string path, ref List<TextCollector.TXT_collection> txt_collection, string enter, ref string error)
        {
            string[] strings = File.ReadAllLines(path);
            for (int k = 0; k < strings.Length; k++)
            {
                string[] temp_strings = strings[k].Split('\t');
                if (MainMenu.settings.exportRealID)
                {
                    txt_collection.Add(new TextCollector.TXT_collection(0, Convert.ToInt32(temp_strings[0]), temp_strings[1], temp_strings[2], false));
                }
                else txt_collection.Add(new TextCollector.TXT_collection(Convert.ToInt32(temp_strings[0]), 0, temp_strings[1], temp_strings[2], false));
            }

            if (txt_collection.Count > 0)
            {
                for (int l = 0; l < txt_collection.Count; l++)
                {
                    txt_collection[l].text = txt_collection[l].text.Replace("\\n", "\n");
                }
            }

            return txt_collection;
        }

        public static List<TextCollector.TXT_collection> ImportTXT(string path, ref List<TextCollector.TXT_collection> txt_collection, bool to_translite, int ASCII, string enter, ref string error)
        {
            //StreamReader sr = new StreamReader(path, System.Text.ASCIIEncoding.GetEncoding(ASCII));
            //StreamReader sr = new StreamReader(path, System.Text.UnicodeEncoding.GetEncoding(ASCII));
            string[] texts = File.ReadAllLines(path);

            //string curLine;
            Int64 pos_str = 0;
            string name = "";
            bool number_find = false;
            int n_str = -1;
            //int number_of_next_list = 0;

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].IndexOf(")") > -1 && number_find == false)
                {
                    if ((Methods.IsNumeric(texts[i].Substring(0, texts[i].IndexOf(")")))))
                    {
                        pos_str = Convert.ToInt64(texts[i].Substring(0, texts[i].IndexOf(")")));
                        try
                        {
                            string[] s;
                            s = texts[i].Split(')');
                            if (s.Count() > 1)
                            {
                                //тут отрезаем тупые пробелы между скобкой
                                name = "";
                                string[] temp = s[1].Split(' ');
                                foreach (string s_temp in temp)
                                {
                                    if (s_temp != string.Empty)
                                    {
                                        if (name == string.Empty)
                                        { name += s_temp; }
                                        else
                                        { name += " " + s_temp; }
                                    }
                                }
                                //name = s[1]; 
                            }
                            else
                            { name = ""; }
                            //name = curLine.Substring(curLine.IndexOf(")"), (curLine.Length - curLine.IndexOf(")") - 1));
                            if (MainMenu.settings.exportRealID)
                            {
                                txt_collection.Add(new TextCollector.TXT_collection(0, (Int32)pos_str, name, "", false));
                            }
                            {
                                txt_collection.Add(new TextCollector.TXT_collection((Int32)pos_str, 0, name, "", false));
                            }
                            //number_of_next_list++;
                            number_find = true;
                            n_str++;
                        }
                        catch
                        {
                            MessageBox.Show("Error in string: " + texts[i] + "\r\n", "Error!");
                            error = "Error in string: " + texts[i];
                            break;
                        }
                    }
                    else
                        {
                            txt_collection[n_str].text += enter + texts[i];
                            number_find = false;
                        }
                    }
                    else
                    {
                        txt_collection[n_str].text += enter + texts[i];
                        number_find = false;
                    }
            }

                /*while ((curLine = sr.ReadLine()) != null)
                {
                    if (curLine.IndexOf(")") > -1 && number_find == false)
                    {
                        if (Methods.IsNumeric(curLine.Substring(0, curLine.IndexOf(")"))))
                        {
                            pos_str = Convert.ToInt64(curLine.Substring(0, curLine.IndexOf(")")));
                            try
                            {
                                //curLine = curLine.Replace(" ", string.Empty);
                                string[] s;
                                s = curLine.Split(')');
                                if (s.Count() > 1)
                                {
                                    //тут отрезаем тупые пробелы между скобкой
                                    name = "";
                                    string[] temp = s[1].Split(' ');
                                    foreach (string s_temp in temp)
                                    {
                                        if (s_temp != string.Empty)
                                        {
                                            if (name == string.Empty)
                                            { name += s_temp; }
                                            else
                                            { name += " " + s_temp; }
                                        }
                                    }
                                    //name = s[1]; 
                                }
                                else
                                { name = ""; }
                                //name = curLine.Substring(curLine.IndexOf(")"), (curLine.Length - curLine.IndexOf(")") - 1));
                                if (MainMenu.settings.exportRealID)
                                {
                                    txt_collection.Add(new TextCollector.TXT_collection(0, (Int32)pos_str, name, "", false));
                                }
                                {
                                    txt_collection.Add(new TextCollector.TXT_collection((Int32)pos_str, 0, name, "", false));
                                }
                                //number_of_next_list++;
                                number_find = true;
                                n_str++;
                            }
                            catch
                            {
                                MessageBox.Show("Error in string: " + curLine + "\r\n", "Error!");
                                error = "Error in string: " + curLine;
                                break;
                            }
                        }
                        else
                        {
                            txt_collection[n_str].text += enter + curLine;
                            number_find = false;
                        }
                    }
                    else
                    {
                        txt_collection[n_str].text += enter + curLine;
                        number_find = false;
                    }
                }
                sr.Close();*/

                for (int i = 0; i < txt_collection.Count; i++)
                {
                    if (txt_collection[i].text.Length > 0)
                    {
                        txt_collection[i].text = txt_collection[i].text.Substring(enter.Length, txt_collection[i].text.Length - enter.Length);
                    }
                }
            return txt_collection;
        }

        public static void ReadLangdb(byte[] binContent, langdb[] database, byte version)
        {
            List<langdb> db = new List<langdb>();
            {
                number = 0;
                int poz = 0;
                if (version == 0)
                {
                    database[0].head = new byte[95];
                    Array.Copy(binContent, poz, database[0].head, 0, 95);
                    poz = 95;
                }
                if (version == 1)
                {
                    database[0].head = new byte[52];
                    Array.Copy(binContent, poz, database[0].head, 0, 52);
                    poz = 52;
                }
                if (version == 2)
                {
                    database[0].head = new byte[48];
                    Array.Copy(binContent, poz, database[0].head, 0, 48);
                    poz = 48;
                }
                if (version == 3)//test
                {
                    database[0].head = new byte[1];
                    Array.Copy(binContent, poz, database[0].head, 0, 0);
                    poz = 0;
                }
                if(version == 4) //Temporary fix for some old langdb files
                {
                    database[0].head = new byte[76];
                    Array.Copy(binContent, poz, database[0].head, 0, 76);
                    poz = 76;
                }
                while (poz < binContent.Length)
                {
                    //8 байт неизвестного происхождения
                    database[number].hz_data = new byte[8];
                    Array.Copy(binContent, poz, database[number].hz_data, 0, 8);
                    database[number].realID = new byte[4];
                    Array.Copy(database[number].hz_data, 0, database[number].realID, 0, 4);
                    poz += 8;
                    //4 байта длинны имени
                    //в первых двух (0 и 1) версиях пропускаем дублирование длинны! 
                    if (version <= 1 || version == 4)
                    {
                        poz += 4;
                    }
                    database[number].lenght_of_name = new byte[4];
                    Array.Copy(binContent, poz, database[number].lenght_of_name, 0, 4);
                    poz += 4;
                    //получаем имя
                    int len_name = BitConverter.ToInt32(database[number].lenght_of_name, 0);
                        database[number].name = ConvertHexToString(binContent, poz, len_name);
                        poz += len_name;
                        //получаем 4 байта длины текста не забывая о 0 и 1 версии
                        if (version <= 1 || version == 4)
                        {
                            poz += 4;
                        }
                        database[number].lenght_of_text = new byte[4];
                        Array.Copy(binContent, poz, database[number].lenght_of_text, 0, 4);
                        poz += 4;
                        //получаем текст
                        int len_text = BitConverter.ToInt32(database[number].lenght_of_text, 0);
                        database[number].text = ConvertHexToString(binContent, poz, len_text);
                        poz += len_text;
                        //получаем 4 байта длины анимации не забывая о 0 и 1 версии
                        if (version <= 1 || version == 4)
                        {
                            poz += 4;
                        }
                        database[number].lenght_of_animation = new byte[4];
                        Array.Copy(binContent, poz, database[number].lenght_of_animation, 0, 4);
                        poz += 4;
                        //получаем анимацию
                        int len_animation = BitConverter.ToInt32(database[number].lenght_of_animation, 0);
                        database[number].animation = ConvertHexToString(binContent, poz, len_animation);
                        poz += len_animation;
                        //получаем 4 байта длины озвучки не забывая о 0 и 1 версии
                        if (version <= 1 || version == 4)
                        {
                            poz += 4;
                        }
                        database[number].lenght_of_waw = new byte[4];
                        Array.Copy(binContent, poz, database[number].lenght_of_waw, 0, 4);
                        poz += 4;
                        //получаем озвучки
                        int len_waw = BitConverter.ToInt32(database[number].lenght_of_waw, 0);
                        database[number].waw = ConvertHexToString(binContent, poz, len_waw);
                        poz += len_waw;
                        //получаем магические байты
                        database[number].magic_bytes = new byte[7];
                        Array.Copy(binContent, poz, database[number].magic_bytes, 0, 7);
                        poz += 7;
                        number++;
                    }
                number--;
                //langdb db = new langdb[number];
                
            }
        }


        public static void CreateLangdb(langdb[] database, byte version, string path)
        {
            //проверяем наличие файла, удаляем его и создаем пустой
            FileStream MyFileStream;

            if (System.IO.File.Exists(path) == true)
            {
                System.IO.File.Delete(path);
            }
            MyFileStream = new FileStream(path, FileMode.OpenOrCreate);
            //записываем заголовок
            int numb = 0;
            MyFileStream.Write(database[0].head, 0, database[0].head.Length);
            //записываем всё остальное
            while (numb <= number)
            {
                //сохраняем хз байты =)
                MyFileStream.Write(database[numb].hz_data, 0, database[numb].hz_data.Length);
                //имя
                SaveStringInfo(MyFileStream, database[numb].name, version);
                //текст
                SaveStringInfo(MyFileStream, database[numb].text, version);
                //анимация
                SaveStringInfo(MyFileStream, database[numb].animation, version);
                //озвучка
                SaveStringInfo(MyFileStream, database[numb].waw, version);
                //магические байты
                MyFileStream.Write(database[numb].magic_bytes, 0, database[numb].magic_bytes.Length);
                //счетчик++
                numb++;
            }
            //закрываем поток
            MyFileStream.Close();
        }

        public static void SaveStringInfo(FileStream MyFileStream, string data, byte version)
        {
            byte[] b = BitConverter.GetBytes(data.Length);
            if (version <= 1 || version == 4) //FIX THAT LATER
            {
                MyFileStream.Write(BitConverter.GetBytes(data.Length + 8), 0, 4);
            }
            MyFileStream.Write(b, 0, 4);
            if (data.Length > 0)
            {
                byte[] hex_data = (byte[])ASCIIEncoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(data);
                MyFileStream.Write(hex_data, 0, hex_data.Length);
            }
        }

        public static string GetNameOnly(int i)
        {
            return fi[i].Name.Substring(0, (fi[i].Name.Length - fi[i].Extension.Length));
        }

        public static void ExportDDSfromD3DTX(FileInfo[] inputFiles, int i, string pathOutput, string fileName)
        {

            //try
            {
                FileStream fs = new FileStream(inputFiles[i].FullName, FileMode.Open);
                byte[] binContent = Methods.ReadFull(fs);
                fs.Close();

                Methods.DeleteCurrentFile(pathOutput + "\\" + fileName);


                //listBox1.Items.Add("File " + inputFiles[i].Name + " exported in " + fileName);//ReportForWork("File " + inputFiles[i].Name + " exported in " + fileName);

            }
            //catch
            //{
            //    ReportForWork("Expoort from file: " + inputFiles[i].Name + " is incorrect!");
            //}
        }

        private void buttonDecrypt_Click(object sender, EventArgs e)
        {
            string versionOfGame = " ";//CheckVersionOfGameFromCombobox(comboBox1.SelectedIndex);
            numKey = comboBox1.SelectedIndex;
            selected_index = comboBox2.SelectedIndex;

            if (checkUnicode.Checked) MainMenu.settings.unicodeSettings = 0;
            else MainMenu.settings.unicodeSettings = 1;

            tsvFile = tsvFilesRB.Checked;

            custKey = checkCustomKey.Checked;

            byte[] encKey;

            string debug = null;

            if (custKey)
            {
                customKey = textBox1.Text;
                encKey = Methods.stringToKey(customKey);
            }
            else encKey = MainMenu.gamelist[comboBox1.SelectedIndex].key;

            Methods.DeleteCurrentFile("\\del.me");
            try
            {
                DirectoryInfo di = new DirectoryInfo(MainMenu.settings.pathForInputFolder);
                fi = di.GetFiles();
            }
            catch
            {
                MessageBox.Show("Open and close program or fix path in config.xml!", "Error!");
                goto end;
            }
            //Создаем нить для экспорта текста из LANGDB
            var processLANGDB = new ForThreads();
            processLANGDB.ReportForWork += AddNewReport;
            List<string> parametresLANGDB = new List<string>();
            parametresLANGDB.Add(".langdb");
            parametresLANGDB.Add(MainMenu.settings.pathForInputFolder);
            parametresLANGDB.Add(MainMenu.settings.pathForOutputFolder);
            parametresLANGDB.Add(versionOfGame);
            var threadLANGDB = new Thread(new ParameterizedThreadStart(processLANGDB.DoExportEncoding));
            threadLANGDB.Start(parametresLANGDB);

            //var processD3DTX = new ForThreads();
            //processLANGDB.ReportForWork += AddNewReport;
            //List<string> parametresD3DTX = new List<string>();
            //parametresD3DTX.Add(".d3dtx");
            //parametresD3DTX.Add(MainMenu.settings.pathForInputFolder);
            //parametresD3DTX.Add(MainMenu.settings.pathForOutputFolder);
            //parametresD3DTX.Add(MainMenu.settings.pathForTempFolder);
            //var threadD3DTX = new Thread(new ParameterizedThreadStart(processD3DTX.DoExportEncoding));
            //threadD3DTX.Start(parametresD3DTX);

            for (int i = 0; i < fi.Length; i++)
            {
                if (fi[i].Extension == ".dlog")
                {
                    List<Langdb> database = new List<Langdb>();
                    FileStream fs = new FileStream(fi[i].FullName, FileMode.Open);
                    byte[] binContent = Methods.ReadFull(fs);
                    ReadDlog(binContent, first_database, database, 0);
                    fs.Close();
                    List<TextCollector.TXT_collection> all_text = new List<TextCollector.TXT_collection>();

                    for (int q = 0; q < database.Count; q++)
                    {
                        if (BitConverter.ToInt32(database[q].lenght_of_textblok, 0) != 8)
                        {
                            if (database[q].text != "" && database[q].name != "" || (database[q].text != "" && database[q].name == ""))
                            {
                                all_text.Add(new TextCollector.TXT_collection((q + 1), 0, database[q].name, database[q].text, false));
                            }
                        }
                    }
                    List<TextCollector.TXT_collection> all_text_for_export = new List<TextCollector.TXT_collection>();
                    TextCollector.CreateExportingTXTfromOneFile(all_text, ref all_text_for_export);

                    string path = "";
                    if (i > 0)
                    {
                        if (fi[i].Extension == ".dlog" && fi[i - 1].Extension == ".txt" && Methods.DeleteCommentary(GetNameOnly(i - 1), "(", ")") == GetNameOnly(i))
                        {
                            path = MainMenu.settings.pathForOutputFolder + "\\" + fi[i - 1].Name;
                        }
                        else
                        {
                            path = MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".dlog") + ".txt";
                        }
                    }
                    else
                    {
                        path = MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".dlog") + ".txt";
                    }
                    Methods.DeleteCurrentFile(path);

                    FileStream MyExportStream = new FileStream(path, FileMode.OpenOrCreate);
                    int w = 0;
                    while (w < all_text_for_export.Count)
                    {
                        all_text_for_export[w].text = all_text_for_export[w].text.Replace("\n", "\r\n");
                        TextCollector.SaveString(MyExportStream, (all_text_for_export[w].number + ") " + all_text_for_export[w].name + "\r\n"), MainMenu.settings.ASCII_N);
                        //TextCollector.SaveString(MyExportStream, (BitConverter.ToString(database[all_text_for_export[w].number-1].hz_data)+"\r\n"), MainMenu.settings.ASCII_N);
                        TextCollector.SaveString(MyExportStream, (all_text_for_export[w].text + "\r\n"), MainMenu.settings.ASCII_N);
                        w++;
                    }
                    MyExportStream.Close();
                    if (i > 0)
                    {
                        if (fi[i].Extension == ".dlog" && fi[i - 1].Extension == ".txt" && Methods.DeleteCommentary(GetNameOnly(i - 1), "(", ")") == GetNameOnly(i))
                        {
                            listBox1.Items.Add("File " + fi[i].Name + " exported in " + fi[i - 1].Name);
                        }
                        else
                        {
                            listBox1.Items.Add("File " + fi[i].Name + " exported in " + Methods.GetNameOfFileOnly(fi[i].Name, ".dlog") + ".txt");
                        }
                    }
                    else
                    {
                        listBox1.Items.Add("File " + fi[i].Name + " exported in " + Methods.GetNameOfFileOnly(fi[i].Name, ".dlog") + ".txt");
                    }
                }
                else if (fi[i].Extension == ".landb")
                {
                    List<byte[]> header = new List<byte[]>();
                    byte[] lenght_of_all_text = new byte[4];

                    List<byte[]> end_of_file = new List<byte[]>();
                    List<Langdb> landb = new List<Langdb>();
                    FileStream fs = new FileStream(fi[i].FullName, FileMode.Open);
                    byte[] binContent = Methods.ReadFull(fs);
                    ReadLandb(binContent, landb, ref header, ref lenght_of_all_text, ref end_of_file);
                    fs.Close();

                    byte[] new_header = new byte[4];
                    Array.Copy(binContent, 0, new_header, 0, 4);
                    if (Encoding.ASCII.GetString(new_header) == "5VSM")
                    {
                        byte[] vers = new byte[4];
                        Array.Copy(binContent, 16, vers, 0, 4);
                        switch (BitConverter.ToInt32(vers, 0))
                        {
                            case 9:
                                versionOfGame = "WAU";
                                break;
                            case 10:
                                versionOfGame = "TFTB";
                                break;
                        }
                    }
                    else if (Encoding.ASCII.GetString(new_header) == "6VSM")
                    {
                        byte[] vers = new byte[4];
                        Array.Copy(binContent, 16, vers, 0, 4);

                        if (BitConverter.ToInt32(vers, 0) == 10) versionOfGame = "Batman";
                    }

                    if (landb.Count > 0)
                    {
                        List<TextCollector.TXT_collection> all_text = new List<TextCollector.TXT_collection>();

                        for (int q = 0; q < landb.Count; q++)
                        {
                            if (landb[q].text != "" && landb[q].name != "" || (landb[q].text != "" && landb[q].name == ""))
                            {
                                all_text.Add(new TextCollector.TXT_collection((q + 1), 0, landb[q].name, landb[q].text, false));
                            }
                        }
                        List<TextCollector.TXT_collection> all_text_for_export = new List<TextCollector.TXT_collection>();
                        TextCollector.CreateExportingTXTfromOneFile(all_text, ref all_text_for_export);

                        string path = "";
                        if (i > 0)
                        {
                            if (fi[i].Extension == ".landb" && fi[i - 1].Extension == ".txt" && Methods.DeleteCommentary(GetNameOnly(i - 1), "(", ")") == GetNameOnly(i))
                            {
                                path = MainMenu.settings.pathForOutputFolder + "\\" + fi[i - 1].Name;
                            }
                            else
                            {
                                path = MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".landb") + ".txt";
                            }
                        }
                        else
                        {
                            if (tsvFile) path = MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".landb") + ".tsv";
                            else path = MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".landb") + ".txt";
                        }
                        Methods.DeleteCurrentFile(path);

                        
                            FileStream MyExportStream = new FileStream(path, FileMode.CreateNew);
                            int w = 0;
                            while (w < all_text_for_export.Count)
                            {
                                byte[] name_of_file = new byte[4];
                                Array.Copy(landb[all_text_for_export[w].number - 1].hz_data, 0, name_of_file, 0, 4);
                                int qwer = BitConverter.ToInt32(name_of_file, 0);
                                if (!tsvFile) all_text_for_export[w].text = all_text_for_export[w].text.Replace("\n", "\r\n");
                                else all_text_for_export[w].text = all_text_for_export[w].text.Replace("\n", "\\n");


                                //тут добавил
                                if (versionOfGame != "TFTB")
                                {
                                    byte[] temp_string = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(all_text_for_export[w].name);
                                    temp_string = Encoding.Convert(Encoding.GetEncoding(MainMenu.settings.ASCII_N), Encoding.UTF8, temp_string);
                                    all_text_for_export[w].name = UnicodeEncoding.UTF8.GetString(temp_string);

                                    if (all_text_for_export[w].text.IndexOf("\0") > 0)
                                    {
                                        all_text_for_export[w].text = all_text_for_export[w].text.Replace("\0", "(ANSI)");
                                    }
                                    else
                                    {
                                        temp_string = Encoding.GetEncoding(MainMenu.settings.ASCII_N).GetBytes(all_text_for_export[w].text);
                                        temp_string = Encoding.Convert(Encoding.GetEncoding(MainMenu.settings.ASCII_N), Encoding.UTF8, temp_string);
                                        all_text_for_export[w].text = UnicodeEncoding.UTF8.GetString(temp_string);
                                    }
                                }
                                /*if ((versionOfGame == "TFTB") && (MainMenu.settings.unicodeSettings == 2))
                                {
                                    string alphabet = MainMenu.settings.additionalChar;
                                    //for (int a = alphabet.Length - 1; a > 0; a--)
                                    for (int a = 0; a < alphabet.Length; a++)
                                    {
                                        all_text_for_export[w].text = all_text_for_export[w].text.Replace(("Г" + alphabet[a].ToString()), alphabet[a].ToString());
                                    }
                                }*/


                                /*if ((versionOfGame == "TFTB") && (MainMenu.settings.unicodeSettings == 0))
                                {
                                    
                                }
                                if ((versionOfGame == "TFTB") && (MainMenu.settings.unicodeSettings == 2))
                                {
                                    string alphabet = MainMenu.settings.additionalChar;
                                    //for (int a = alphabet.Length - 1; a > 0; a--)
                                    for (int a = 0; a < alphabet.Length; a++)
                                    {
                                        all_text_for_export[w].text = all_text_for_export[w].text.Replace(("Г" + alphabet[a].ToString()), alphabet[a].ToString());
                                    }
                                }
                                else if ((versionOfGame == "TFTB") && (MainMenu.settings.unicodeSettings == 1))
                                {
                                    TextCollector.SaveString(MyExportStream, (all_text_for_export[w].number + ") " + all_text_for_export[w].name + "\r\n"), MainMenu.settings.ASCII_N); //+ qwer.ToString() +  "\r\n"), MainMenu.settings.ASCII_N);
                                    TextCollector.SaveString(MyExportStream, (all_text_for_export[w].text + "\r\n"), MainMenu.settings.ASCII_N);
                                    w++;
                                }*/
                                if (tsvFile)
                                {
                                    string tsv_str = all_text_for_export[w].number + "\t" + all_text_for_export[w].name + "\t" + all_text_for_export[w].text + "\r\n";
                                    TextCollector.SaveString(MyExportStream, tsv_str, MainMenu.settings.unicodeSettings);
                                }
                                else
                                {
                                    TextCollector.SaveString(MyExportStream, (all_text_for_export[w].number + ") " + all_text_for_export[w].name + "\r\n"), MainMenu.settings.unicodeSettings); //+ qwer.ToString() +  "\r\n"), MainMenu.settings.ASCII_N);
                                    TextCollector.SaveString(MyExportStream, (all_text_for_export[w].text + "\r\n"), MainMenu.settings.unicodeSettings);
                                }
                                w++;
                                
                            }
                            MyExportStream.Close();
                        //}
                        if (i > 0)
                        {
                            if (fi[i].Extension == ".landb" && fi[i - 1].Extension == ".txt" && Methods.DeleteCommentary(GetNameOnly(i - 1), "(", ")") == GetNameOnly(i))
                            {
                                //listBox1.Items.Add("File " + fi[i].Name + " exported in " + fi[i - 1].Name);
                                listBox1.Items.Add("File " + fi[i].Name + " exported in " + fi[i - 1].Name);
                            }
                            else
                            {
                                //listBox1.Items.Add("File " + fi[i].Name + " exported in " + Methods.GetNameOfFileOnly(fi[i].Name, ".landb") + ".txt");
                                if (tsvFile) listBox1.Items.Add("File " + fi[i].Name + " exported in " + Methods.GetNameOfFileOnly(fi[i].Name, ".landb") + ".tsv");
                                else listBox1.Items.Add("File " + fi[i].Name + " exported in " + Methods.GetNameOfFileOnly(fi[i].Name, ".landb") + ".txt");
                            }
                        }
                        else
                        {
                            //listBox1.Items.Add("File " + fi[i].Name + " exported in " + Methods.GetNameOfFileOnly(fi[i].Name, ".landb") + ".txt");
                            if (tsvFile) listBox1.Items.Add("File " + fi[i].Name + " exported in " + Methods.GetNameOfFileOnly(fi[i].Name, ".landb") + ".tsv");
                            else listBox1.Items.Add("File " + fi[i].Name + " exported in " + Methods.GetNameOfFileOnly(fi[i].Name, ".landb") + ".txt");
                        }
                    }
                    else
                    {
                        listBox1.Items.Add("File " + fi[i].Name + " is EMPTY!");
                    }
                }
                else if (fi[i].Extension == ".d3dtx")
                {
                    //try
                    //{
                    //старые добрые времена с текстурами без извращений
                    FileStream fs = new FileStream(fi[i].FullName, FileMode.Open);
                    byte[] d3dtx = Methods.ReadFull(fs);
                    fs.Close();

                    byte[] check_header = new byte[4];
                    Array.Copy(d3dtx, 0, check_header, 0, 4);
                    int version = 2;
                    if(selected_index == 1) version = 7;

                    int offset = 0;

                    byte[] check_ver = new byte[4];

                    if (Encoding.ASCII.GetString(check_header) == "5VSM" || Encoding.ASCII.GetString(check_header) == "6VSM")
                    {
                        Array.Copy(d3dtx, 16, check_ver, 0, 4);
                        /*if (BitConverter.ToInt32(check_ver, 0) < 7)
                        {
                            check_ver = new byte[4];
                            Array.Copy(d3dtx, 92, check_ver, 0, 4);
                        }
                        else
                        {
                            check_ver = new byte[4];
                            Array.Copy(d3dtx, 104, check_ver, 0, 4);
                        }*/

                        offset = 12 * BitConverter.ToInt32(check_ver, 0) + 16 + 4;
                        check_ver = new byte[4];
                        Array.Copy(d3dtx, offset, check_ver, 0, 4);
                    }
                    else Array.Copy(d3dtx, 4, check_ver, 0, 4);
                    #region Временно закомментированный код (эксперименты над отдельным классом экспорта текстур)
                    //if (((BitConverter.ToInt32(check_ver, 0) > 6) || (BitConverter.ToInt32(check_ver, 0) < 0)) && 
                    //    ((Encoding.ASCII.GetString(check_header) != "5VSM") && (Encoding.ASCII.GetString(check_header) != "ERTM")
                    //    && (Encoding.ASCII.GetString(check_header) != "NIBM")))
                    //{
                        //Пытаемся расшифровать файл

                        
                    //        string info = Methods.FindingDecrytKey(d3dtx, "texture");
                    //        listBox1.Items.Add("File " + fi[i].Name + " decrypted! " + info);
                            /*fs = new FileStream((MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".d3dtx") + ".dds"), FileMode.OpenOrCreate);
                            fs.Write(d3dtx, 0, d3dtx.Length);
                            fs.Close();*/

                     //       check_ver = new byte[4];
                     //       Array.Copy(d3dtx, 4, check_ver, 0, 4);


                        /*for (int a = 0; a < MainMenu.gamelist.Count; a++)
                        {
                            byte[] checkVerOld = new byte[4];
                            byte[] checkVerNew = new byte[4];
                            byte[] checkOldFile = new byte[d3dtx.Length]; //Проверочный файл (старый метод)
                            byte[] checkNewFile = new byte[d3dtx.Length]; //Проверочный файл (новый метод)

                            Array.Copy(d3dtx, 0, checkOldFile, 0, d3dtx.Length);
                            Array.Copy(d3dtx, 0, checkNewFile, 0, d3dtx.Length);

                            Methods.meta_crypt(checkOldFile, MainMenu.gamelist[a].key, 2, true);
                            Array.Copy(checkOldFile, 4, checkVerOld, 0, 4);
                            Methods.meta_crypt(checkNewFile, MainMenu.gamelist[a].key, 7, true);
                            Array.Copy(checkNewFile, 4, checkVerNew, 0, 4);

                            if ((BitConverter.ToInt32(checkVerOld, 0) > 0) && (BitConverter.ToInt32(checkVerOld, 0) < 6))
                            {
                                Array.Copy(checkOldFile, 0, d3dtx, 0, d3dtx.Length);
                                numKey = a;
                                version = 2;
                                break;
                            }
                            else if ((BitConverter.ToInt32(checkVerNew, 0) > 0) && (BitConverter.ToInt32(checkVerNew, 0) < 6))
                            {
                                Array.Copy(checkNewFile, 0, d3dtx, 0, d3dtx.Length);
                                numKey = a;
                                version = 7;
                                break;
                            }
                        }*/
                    //}
                   // if (comboBox1.SelectedIndex == 0)
                   // {
                  //  if (((BitConverter.ToInt32(check_ver, 0) < 6) && (BitConverter.ToInt32(check_ver, 0) > 0)) &&
                   //     (Encoding.ASCII.GetString(check_header) != "5VSM"))
                   //     {
                   //         versionOfGame = " ";

                     //       if ((Encoding.ASCII.GetString(check_header) != "ERTM") && (Encoding.ASCII.GetString(check_header) != "NIBM"))//Проверка старых текстур
                       //     {
                      //          int checkDDS = Methods.FindStartOfStringSomething(d3dtx, 0, "DDS");
                      //          if (checkDDS > (d3dtx.Length - 100))
                      //          {
                      //                  string result = Methods.FindingDecrytKey(d3dtx, "texture");
                       //                 listBox1.Items.Add("File " + fi[i].Name + " decrypted! " + result);
                                        /*fs = new FileStream((MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".d3dtx") + ".dds"), FileMode.OpenOrCreate);
                                        fs.Write(d3dtx, 0, d3dtx.Length);
                                        fs.Close();*/
                                    

                                    /*byte[] checkDDSLength = new byte[4];
                                    Array.Copy(d3dtx, poz, checkDDSLength, 0, 4);
                                    byte[] temp;
                                    if (BitConverter.ToInt32(checkDDSLength, 0) < 2048) temp = new byte[BitConverter.ToInt32(checkDDSLength, 0)]; //Проверка, если текстура окажется меньше 2048 байт
                                    else temp = new byte[2048];
                                    Array.Copy(d3dtx, poz, temp, 0, temp.Length);
                                    string result = Methods.FindingDecrytKey(temp, false, true);
                                    Array.Copy(temp, 0, d3dtx, poz, temp.Length);*/
                                    
                      //          }
                       //     }

                       //     if (Methods.FindStartOfStringSomething(d3dtx, 0, "DDS") < (d3dtx.Length - 100))
                       //     {
                       //         fs = new FileStream((MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".d3dtx") + ".dds"), FileMode.OpenOrCreate);
                       //         int start = Methods.FindStartOfStringSomething(d3dtx, 0, "DDS");
                       //         fs.Write(d3dtx, start, (d3dtx.Length - start));
                       //         fs.Close();
                       //         listBox1.Items.Add("Exported dds from: " + fi[i].Name);
                       //     }
                       //     else if (Methods.FindStartOfStringSomething(d3dtx, 0, "PVR!") < (d3dtx.Length - 100))
                       //     {
                       //         fs = new FileStream((MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".d3dtx") + ".pvr"), FileMode.OpenOrCreate);
                       //         int start = Methods.FindStartOfStringSomething(d3dtx, 0, "PVR!");

                       //         byte[] pvrheader = { 0x50, 0x56, 0x52, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                       //         0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00,
                       /*         0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x01,
                                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                                0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                                byte[] texType = new byte[4];
                                byte[] pvr_height = new byte[4];
                                byte[] pvr_width = new byte[4];
                                byte[] pvr_mipmap = new byte[4];
                                byte[] pvr_dataSize = new byte[4];

                                Array.Copy(d3dtx, start - 40, pvr_height, 0, 4);
                                Array.Copy(d3dtx, start - 36, pvr_width, 0, 4);
                                Array.Copy(d3dtx, start - 32, pvr_mipmap, 0, 4);
                                Array.Copy(d3dtx, start - 28, texType, 0, 4);
                                Array.Copy(d3dtx, start - 24, pvr_dataSize, 0, 4);

                                int dataSize = BitConverter.ToInt32(pvr_dataSize, 0);

                                
                                int mip = BitConverter.ToInt32(pvr_mipmap, 0) + 1;

                                string mip_info = mip.ToString();
                                if (mip <= 1) mip_info = "No mip-maps";

                                string info = " tex. format: ";
                                byte[] pvr_tex = new byte[8];

                                switch (BitConverter.ToUInt32(texType, 0))
                                {
                                    case 0x8010: //4444 RGBA
                                    case 0x8110: //4444 RGBA
                                        info += "4444 RGBA. Mip-maps count: " + mip_info;
                                        pvr_tex = BitConverter.GetBytes(0x404040461626772);
                                        Array.Copy(pvr_tex, 0, pvrheader, 8, pvr_tex.Length);
                                        break;
                                    case 0x830d: //PVRTC 4bpp RGBA
                                    case 0x820d: //PVRTC 4bpp RGBA
                                    case 0x20d: //PVRTC 4bpp RGBA
                                    case 0x8a0d: //PVRTC 4bpp RGBA
                                    case 0x8b0d: //PVRTC 4bpp RGBA
                                        info += "PVRTC 4bpp RGBA. Mip-maps count: " + mip_info;
                                        pvr_tex = BitConverter.GetBytes(3);
                                        Array.Copy(pvr_tex, 0, pvrheader, 8, pvr_tex.Length);
                                        break;
                                    case 0xa0d: //PVRTC 4bpp RGB
                                    case 0x30d: //PVRTC 4bpp RGB
                                        info += "PVRTC 4bpp RGB. Mip-maps count: " + mip_info;
                                        pvr_tex = BitConverter.GetBytes(2);
                                        Array.Copy(pvr_tex, 0, pvrheader, 8, pvr_tex.Length);
                                        break;
                                    case 0x13: //565 RGB
                                    case 0x113: //565 RGB
                                        info += "565 RGB. Mip-maps count: " + mip_info;
                                        pvr_tex = BitConverter.GetBytes(0x5060500626772);
                                        Array.Copy(pvr_tex, 0, pvrheader, 8, pvr_tex.Length);
                                        break;
                                    case 0x8011:
                                        info += "8888 RGBA. Mip-maps count: " + mip_info;
                                        pvr_tex = BitConverter.GetBytes(0x808080861626772);
                                        Array.Copy(pvr_tex, 0, pvrheader, 8, pvr_tex.Length);
                                        break;
                                    default:
                                        info += "Unknown format. Its code is " + Convert.ToString(BitConverter.ToUInt32(texType, 0)) + ". Mip-maps count: " + mip_info;
                                        break;
                                }

                                pvr_mipmap = new byte[4];
                                pvr_mipmap = BitConverter.GetBytes(mip);
                                Array.Copy(pvr_mipmap, 0, pvrheader, 44, pvr_mipmap.Length);
                                Array.Copy(pvr_height, 0, pvrheader, 24, pvr_height.Length);
                                Array.Copy(pvr_width, 0, pvrheader, 28, pvr_width.Length);

                                byte[] texContent = new byte[dataSize];

                                Array.Copy(d3dtx, start + 8, texContent, 0, dataSize);

                                fs.Write(pvrheader, 0, pvrheader.Length);
                                fs.Write(texContent, 0, texContent.Length);
                                fs.Close();

                                listBox1.Items.Add("Exported pvr from: " + fi[i].Name + info);
                            }
                        }*/
                        //начинаем перебирать сложные версии
                        /*if (versionOfGame == "PN2")
                        {*/
                    //else if ((BitConverter.ToInt32(check_ver, 0) == 6) && (Encoding.ASCII.GetString(check_header)) != "5VSM")
                    #endregion

                    string result = null;

                    if(((Encoding.ASCII.GetString(check_header) != "5VSM") && BitConverter.ToInt32(check_ver, 0) < 6))
                    {
                        bool pvr = false;
                        byte[] BinContent = TextureWorker.extract_old_textures(d3dtx, ref result, ref pvr);
                        if (BinContent != null)
                        {
                            string message = "File " + fi[i].Name + " exported in dds file. " + result;
                            string path = TTG_Tools.MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".d3dtx") + ".dds";
                            if (pvr)
                            {
                                message = "File " + fi[i].Name + " exported in pvr file. " + result;
                                path = TTG_Tools.MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".d3dtx") + ".pvr";
                            }
                            
                            fs = new FileStream(path, FileMode.OpenOrCreate);
                            fs.Write(BinContent, 0, BinContent.Length);
                            fs.Close();
                            listBox1.Items.Add(message);
                        }
                        else listBox1.Items.Add("Unknown error in file " + fi[i].Name + ". Please write me about it.");
                    }
                    
                    if ((BitConverter.ToInt32(check_ver, 0) == 6) && (Encoding.ASCII.GetString(check_header)) == "ERTM")
                        {
                            versionOfGame = "PN2";
                        }
                        else if ((BitConverter.ToInt32(check_ver, 0) >= 4) && (Encoding.ASCII.GetString(check_header) == "5VSM"))
                        {
                            switch (BitConverter.ToInt32(check_ver, 0))
                            {
                                case 4:
                                    versionOfGame = "WAU";
                                    break;
                                case 5:
                                    versionOfGame = "TFTB";
                                    break;
                                case 7:
                                    versionOfGame = "WDM"; //Для Walking Dead Michonne
                                    break;
                            }
                        }
                    else if (BitConverter.ToInt32(check_ver, 0) >= 8 && (Encoding.ASCII.GetString(check_header) == "6VSM"))
                    {
                        versionOfGame = "Batman";
                    }
                    
                        if (versionOfGame != " ")
                        {
                        try
                        {
                            int num = 0; //номер формата текстуры

                            int num_width = 0; //номер заголовка с шириной
                            int num_height = 0; //номер заголовка с высотой
                            int num_mipmaps = 0; //номер количества мип-мапов
                            int platform_pos = 0; //Позиция данных о платформе (сделано для долбаного PVR формата!)

                            switch (versionOfGame)
                            {
                                case "PN2":
                                    platform_pos = 0x60;
                                    num = 6;
                                    num_width = 5;
                                    num_height = 4;
                                    num_mipmaps = 3;
                                    break;
                                case "WAU":
                                    platform_pos = 0x6C;
                                    num = 6;
                                    num_width = 5;
                                    num_height = 4;
                                    num_mipmaps = 3;
                                    break;
                                case "TFTB":
                                    platform_pos = 0x6C;
                                    num = 7;
                                    num_width = 6;
                                    num_height = 5;
                                    num_mipmaps = 4;
                                    break;
                                case "WDM":
                                    platform_pos = 0x78;
                                    num = 6;
                                    num_width = 5;
                                    num_height = 4;
                                    num_mipmaps = 3;
                                    break;
                                case "Batman":
                                    platform_pos = offset + 16;//0x78;
                                                               //num = 8;
                                                               //num_width = 5;
                                                               // num_height = 4;
                                                               // num_mipmaps = 3;
                                    break;
                            }

                            List<chapterOfDDS> chaptersOfDDS = new List<chapterOfDDS>();
                            //int start = Methods.FindStartOfStringSomething(d3dtx, 0, ".d3dtx") + 5 + 2;
                            int start = Methods.FindStartOfStringSomething(d3dtx, 0, ".d3dtx") + 6;
                            int poz = start;
                            //List<byte[]> head = new List<byte[]>();
                            byte[] getPlatform = new byte[4];
                            Array.Copy(d3dtx, platform_pos, getPlatform, 0, 4);

                            //string res = Methods.GetChaptersOfDDS(d3dtx, poz, head, chaptersOfDDS, versionOfGame);
                            byte[] mips = new byte[4];
                            byte[] width = new byte[4];
                            byte[] height = new byte[4];
                            int tex_code = 0;


                            byte[] block_size = new byte[4];
                            Array.Copy(d3dtx, poz, block_size, 0, block_size.Length);
                            poz += 4;
                            byte[] content_size = new byte[4];
                            Array.Copy(d3dtx, poz, content_size, 0, content_size.Length);
                            poz += 4;
                            int size = BitConverter.ToInt32(content_size, 0);
                            poz += size;
                            poz += 4;
                            byte[] check = new byte[1];
                            Array.Copy(d3dtx, poz, check, 0, check.Length);
                            poz += 1;
                            byte ch = check[0];
                            if (ch == 0x31)
                            {
                                poz += 8;
                                byte[] temp_sz = new byte[4];
                                Array.Copy(d3dtx, poz, temp_sz, 0, temp_sz.Length);
                                poz += BitConverter.ToInt32(temp_sz, 0);
                            }

                            Array.Copy(d3dtx, poz, mips, 0, mips.Length);
                            poz += 4;
                            Array.Copy(d3dtx, poz, width, 0, width.Length);
                            poz += 4;
                            Array.Copy(d3dtx, poz, height, 0, height.Length);
                            poz += 12;
                            byte[] temp = new byte[4];
                            Array.Copy(d3dtx, poz, temp, 0, temp.Length);
                            tex_code = BitConverter.ToInt32(temp, 0);
                            poz += 100;

                            List<chapterOfDDS> chDDS = new List<chapterOfDDS>();
                            List<byte[]> temp_mas = new List<byte[]>();


                            for (int t = 0; t < BitConverter.ToInt32(mips, 0); t++)
                            {
                                byte[] some_shit = new byte[4];
                                poz += 8;
                                Array.Copy(d3dtx, poz, some_shit, 0, some_shit.Length);
                                temp_mas.Add(some_shit);
                                //chDDS.Add(new chapterOfDDS(nul, nul, nul, nul, some_shit, nul));
                                poz += 12;

                                if(t != BitConverter.ToInt32(mips, 0) - 1)
                                {
                                    poz += 4;
                                }
                            }

                            for(int t = 0; t < temp_mas.Count; t++)
                            {
                                int size_tex = BitConverter.ToInt32(temp_mas[t], 0);
                                byte[] nul = new byte[4];
                                byte[] some_shit = new byte[size_tex];

                                Array.Copy(d3dtx, poz, some_shit, 0, some_shit.Length);
                                poz += some_shit.Length;
                                chDDS.Add(new chapterOfDDS(nul, nul, nul, nul, some_shit, nul));
                            }

                            string tex_info = "MIP-map count: ";
                            /*if (BitConverter.ToInt32(head[num_mipmaps], 0) <= 1) tex_info += "no mip-maps";
                            else tex_info += BitConverter.ToInt32(head[num_mipmaps], 0); //Информация о текстурах*/

                            if (BitConverter.ToInt32(mips, 0) <= 1) tex_info += "no mip-maps";
                            else tex_info += BitConverter.ToInt32(mips, 0); //Информация о текстурах


                            string AdditionalInfo = null;
                                bool pvr = checkIOS.Checked;
                                int platform = BitConverter.ToInt32(getPlatform, 0);


                            byte[] Content = TextureWorker.extract_new_textures(tex_code, width, height, mips, platform, ref pvr, chDDS, ref AdditionalInfo);

                            //byte[] Content = TextureWorker.extract_new_textures(num, num_width, num_height, num_mipmaps, platform, ref pvr, head, chaptersOfDDS, ref AdditionalInfo);

                            AdditionalInfo += " " + tex_info;

                                //AdditionalInfo += " Tex code: " + BitConverter.ToString(head[num]);
                                

                                if (Content != null)
                                {
                                    string FilePath = MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".d3dtx") + ".dds";
                                    if (pvr) FilePath = MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".d3dtx") + ".pvr";
                                    fs = new FileStream(FilePath, FileMode.OpenOrCreate);
                                    fs.Write(Content, 0, Content.Length);
                                    fs.Close();

                                    if (pvr) listBox1.Items.Add("Exported pvr from: " + fi[i].Name + ", " + AdditionalInfo);
                                    else listBox1.Items.Add("Exported dds from: " + fi[i].Name + ", " + AdditionalInfo);
                                }
                                else
                                {
                                    listBox1.Items.Add("Unknown error in file " + fi[i].Name + ". Code of Texture: " + tex_code + ". Please write me about it.");
                                }
                                
                                //1 группа байт - это dxt5
                                //if (BitConverter.ToInt32(head[3], 0) == 1)
                                //{
                                /*if (BitConverter.ToInt32(head[num], 0) == 66)//dxt5
                                {
                                    Array.Copy(tex_format[1].tex_header, 0, dds_head, 32, tex_format[1].tex_header.Length);
                                    Array.Copy(chaptersOfDDS[chaptersOfDDS.Count - 1].lenght_of_chapter, 0, dds_head, 20, 4);
                                    tex_info += ", texture format: DXT5";
                                    //dds_head[87] = 0x35;
                                }
                                else if (BitConverter.ToInt32(head[num], 0) == 16)//alpha 8 bit (A8)
                                {
                                    Array.Copy(tex_format[2].tex_header, 0, dds_head, 32, tex_format[2].tex_header.Length);
                                    tex_info += ", texture format: Alpha 8 bit (A8)";
                                }
                                else if (BitConverter.ToInt32(head[num], 0) == 64)//dxt1
                                {
                                    Array.Copy(tex_format[0].tex_header, 0, dds_head, 32, tex_format[0].tex_header.Length);
                                    Array.Copy(chaptersOfDDS[chaptersOfDDS.Count - 1].lenght_of_chapter, 0, dds_head, 20, 4);
                                    tex_info += ", texture format: DXT1";
                                    //dds_head[67] = 0x31;//dxt1
                                }
                                else if (BitConverter.ToInt32(head[num], 0) == 37)//32f.32f.32f.32f
                                {
                                    Array.Copy(tex_format[3].tex_header, 0, dds_head, 32, tex_format[3].tex_header.Length);
                                    tex_info += ", texture format: 32f.32f.32f.32f";
                                }
                                else if (BitConverter.ToInt32(head[num], 0) == 0) //8888 RGBA
                                {
                                    dds_head = dds_head_mobile;
                                    Array.Copy(head[num_width], 0, dds_head, 12, 4);//ширина/длина
                                    Array.Copy(head[num_height], 0, dds_head, 16, 4);//ширина/длина
                                    Array.Copy(head[num_mipmaps], 0, dds_head, 28, 4);//Количество мип-мапов
                                    Array.Copy(tex_format[4].tex_header, 0, dds_head, 32, tex_format[4].tex_header.Length);
                                    tex_info += ", texture format: 8888 RGBA";
                                }
                                else if (BitConverter.ToInt32(head[num], 0) == 4) //4444 RGBA
                                {
                                    if (BitConverter.ToInt32(head[3], 0) > 1)
                                    {
                                        dds_head = dds_head_mobile;
                                        Array.Copy(head[num_width], 0, dds_head, 12, 4);//ширина/длина
                                        Array.Copy(head[num_height], 0, dds_head, 16, 4);//ширина/длина
                                        Array.Copy(head[num_mipmaps], 0, dds_head, 28, 4);//Количество мип-мапов
                                        Array.Copy(tex_format[6].tex_header, 0, dds_head, 32, tex_format[6].tex_header.Length);
                                    }
                                    else
                                    {
                                        dds_head = dds_head_mobile;
                                        Array.Copy(head[num_width], 0, dds_head, 12, 4);//ширина/длина
                                        Array.Copy(head[num_height], 0, dds_head, 16, 4);//ширина/длина
                                        Array.Copy(head[num_mipmaps], 0, dds_head, 28, 4);//Количество мип-мапов
                                        Array.Copy(tex_format[5].tex_header, 0, dds_head, 32, tex_format[5].tex_header.Length);
                                    }
                                    tex_info += ", texture format: 4444 RGBA";
                                }
                                else if ((BitConverter.ToInt32(head[num], 0) == 81) || (BitConverter.ToInt32(head[6], 0) == 83)) //PVRTC 4bpp
                                {
                                    Array.Copy(tex_format[7].tex_header, 0, dds_head, 32, tex_format[1].tex_header.Length);
                                    Array.Copy(chaptersOfDDS[chaptersOfDDS.Count - 1].lenght_of_chapter, 0, dds_head, 20, 4);
                                    tex_info += ", texture format: PVRTC 4bpp RGB/RGBA";
                                }
                                else if (BitConverter.ToInt32(head[num], 0) == 2) //PVRTC 2bpp
                                {
                                    Array.Copy(tex_format[8].tex_header, 0, dds_head, 32, tex_format[1].tex_header.Length);
                                    Array.Copy(chaptersOfDDS[chaptersOfDDS.Count - 1].lenght_of_chapter, 0, dds_head, 20, 4);
                                    tex_info += ", texture format: PVRTC 2bpp RGB/RGBA";
                                }
                                else if (BitConverter.ToInt32(head[num], 0) == 112) //ETC1
                                {
                                    if (BitConverter.ToInt32(head[3], 0) > 1)
                                    {
                                        dds_head = dds_head_mobile;
                                        Array.Copy(head[num_width], 0, dds_head, 12, 4);//ширина/длина
                                        Array.Copy(head[num_height], 0, dds_head, 16, 4);//ширина/длина
                                        Array.Copy(head[num_mipmaps], 0, dds_head, 28, 4);//Количество мип-мапов
                                        Array.Copy(tex_format[10].tex_header, 0, dds_head, 32, tex_format[10].tex_header.Length);
                                    }
                                    else
                                    {
                                        dds_head = dds_head_mobile;
                                        Array.Copy(head[num_width], 0, dds_head, 12, 4);//ширина/длина
                                        Array.Copy(head[num_height], 0, dds_head, 16, 4);//ширина/длина
                                        Array.Copy(head[num_mipmaps], 0, dds_head, 28, 4);//Количество мип-мапов
                                        Array.Copy(tex_format[9].tex_header, 0, dds_head, 32, tex_format[9].tex_header.Length);
                                    }
                                    tex_info += ", texture format: ETC1";
                                }
                                else
                                {
                                    //MessageBox.Show("File " + fi[i].FullName + " has unknown format!");
                                    Array.Copy(tex_format[0].tex_header, 0, dds_head, 32, tex_format[0].tex_header.Length);
                                    Array.Copy(chaptersOfDDS[chaptersOfDDS.Count - 1].lenght_of_chapter, 0, dds_head, 20, 4);
                                    tex_info += ", texture format: unknown (DXT1 is default)";
                                }*/

                                //fs.Write(dds_head, 0, dds_head.Length);

                                /*for (int k = chaptersOfDDS.Count - 1; k >= 0; k--)
                                {
                                    fs.Write(chaptersOfDDS[k].content_chapter, 0, chaptersOfDDS[k].content_chapter.Length);
                                }*/
                            }
                            catch
                            {
                                listBox1.Items.Add("Something is wrong. Please, contact with me.");
                            }
                        }
                }
                else if(fi[i].Extension == ".font") //Может, доработать чуть позже авторасшифровку в Font Editor и удалить отсюда этот код?
                {
                    FileStream fs = new FileStream(fi[i].FullName, FileMode.Open);
                    byte[] fontContent = Methods.ReadFull(fs);
                    fs.Close();

                    byte[] checkHeader = new byte[4];
                    Array.Copy(fontContent, 0, checkHeader, 0, 4);

                    if ((Encoding.ASCII.GetString(checkHeader) != "ERTM") && (Encoding.ASCII.GetString(checkHeader) != "5VSM"))
                    {
                        byte[] checkVer = new byte[4];
                        Array.Copy(fontContent, 4, checkVer, 0, 4);
                        //MessageBox.Show(BitConverter.ToInt32(checkVer, 0).ToString());

                        if ((BitConverter.ToInt32(checkVer, 0) < 0) || (BitConverter.ToInt32(checkVer, 0) > 6))
                        {
                            Methods.FindingDecrytKey(fontContent, "font");
                            //Methods.meta_crypt(fontContent, MainMenu.gamelist[selected_index].key, 2, true);
                            checkVer = new byte[4];
                            Array.Copy(fontContent, 4, checkVer, 0, 4);

                            if ((BitConverter.ToInt32(checkVer, 0) > 0) && (BitConverter.ToInt32(checkVer, 0) < 6))
                            {
                                if (File.Exists(MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name)) File.Delete(MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name);

                                fs = new FileStream(MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name, FileMode.CreateNew);
                                fs.Write(fontContent, 0, fontContent.Length);
                                fs.Close();

                                listBox1.Items.Add("File " + fi[i].Name + " decrypted!");
                            }
                            else listBox1.Items.Add("Font couldn't decrypt. Try another key.");
                        }
                        else
                        {
                            if (Methods.FindStartOfStringSomething(fontContent, 0, "DDS") > fontContent.Length - 100)
                            {
                                Methods.FindingDecrytKey(fontContent, "font");

                                if(File.Exists(MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name)) File.Delete(MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name);
                                fs = new FileStream(MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name, FileMode.CreateNew);
                                fs.Write(fontContent, 0, fontContent.Length);
                                fs.Close();

                                listBox1.Items.Add("File " + fi[i].Name + " decrypted!");
                            }
                            else listBox1.Items.Add("Font couldn't decrypt. Try another key.");
                        }
                    }

                }
                else if (fi[i].Extension == ".prop")
                {
                    listBox1.Items.Add("Temporary unavailable.");
                    /*byte[] header = new byte[0];
                    byte[] countOfBlock = new byte[0];
                    byte[] header2 = new byte[0];
                    byte[] lenght_of_all_text = new byte[4];
                    byte[] end_of_file = new byte[0];
                    List<Prop> prop = new List<Prop>();
                    FileStream fs = new FileStream(fi[i].FullName, FileMode.Open);
                    byte[] binContent = Methods.ReadFull(fs);
                    ReadProp(binContent, prop, ref header, ref countOfBlock, ref header2, ref lenght_of_all_text, ref end_of_file);
                    fs.Close();
                    if (prop.Count > 0)
                    {
                        List<TextCollector.TXT_collection> all_text = new List<TextCollector.TXT_collection>();
                        int c = 0;
                        for (int q = 0; q < prop.Count; q++)
                        {
                            for (int w = 0; w < prop[q].textInProp.Count; w++)
                            {
                                c++;
                                if (prop[q].textInProp[w].text != "" && prop[q].textInProp[w].name != "" || (prop[q].textInProp[w].text != "" && prop[q].textInProp[w].name == ""))
                                {

                                    all_text.Add(new TextCollector.TXT_collection(c, 0, prop[q].textInProp[w].name, prop[q].textInProp[w].text, false));
                                }
                            }
                        }
                        List<TextCollector.TXT_collection> all_text_for_export = new List<TextCollector.TXT_collection>();
                        TextCollector.CreateExportingTXTfromOneFile(all_text, ref all_text_for_export);

                        string path = "";
                        if (i > 0)
                        {
                            if (fi[i].Extension == ".prop" && fi[i - 1].Extension == ".txt" && Methods.DeleteCommentary(GetNameOnly(i - 1), "(", ")") == GetNameOnly(i))
                            {
                                path = MainMenu.settings.pathForOutputFolder + "\\" + fi[i - 1].Name;
                            }
                            else
                            {
                                path = MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".prop") + ".txt";
                            }
                        }
                        else
                        {
                            path = MainMenu.settings.pathForOutputFolder + "\\" + Methods.GetNameOfFileOnly(fi[i].Name, ".prop") + ".txt";
                        }
                        Methods.DeleteCurrentFile(path);

                        FileStream MyExportStream = new FileStream(path, FileMode.CreateNew);
                        for (int w = 0; w < all_text_for_export.Count; w++)
                        {
                            {
                                TextCollector.SaveString(MyExportStream, (all_text_for_export[w].number + ") " + all_text_for_export[w].name + "\r\n"), MainMenu.settings.ASCII_N);
                                TextCollector.SaveString(MyExportStream, (all_text_for_export[w].text + "\r\n"), MainMenu.settings.ASCII_N);
                            }
                        }
                        MyExportStream.Close();
                        if (i > 0)
                        {
                            if (fi[i].Extension == ".prop" && fi[i - 1].Extension == ".txt" && Methods.DeleteCommentary(GetNameOnly(i - 1), "(", ")") == GetNameOnly(i))
                            {
                                listBox1.Items.Add("File " + fi[i].Name + " exported in " + fi[i - 1].Name);
                            }
                            else
                            {
                                listBox1.Items.Add("File " + fi[i].Name + " exported in " + Methods.GetNameOfFileOnly(fi[i].Name, ".prop") + ".txt");
                            }
                        }
                        else
                        {
                            listBox1.Items.Add("File " + fi[i].Name + " exported in " + Methods.GetNameOfFileOnly(fi[i].Name, ".prop") + ".txt");
                        }
                    }
                    else
                    {
                        listBox1.Items.Add("File " + fi[i].Name + " is EMPTY!");
                    }*/
                }
                else if ((fi[i].Extension == ".lenc") || (fi[i].Extension == ".lua"))
                {
                    FileStream fs = new FileStream(fi[i].FullName, FileMode.Open);
                    byte[] luaContent = Methods.ReadFull(fs);
                    fs.Close();

                    int version;
                    if (selected_index == 0) version = 2;
                    else version = 7;
                    luaContent = Methods.decryptLua(luaContent, encKey, version);

                    fs = new FileStream(TTG_Tools.MainMenu.settings.pathForOutputFolder + "\\" + fi[i].Name, FileMode.OpenOrCreate);
                    fs.Write(luaContent, 0, luaContent.Length);
                    fs.Close();
                    listBox1.Items.Add("File " + fi[i].Name + " decrypted.");
                }
            }

            if (debug != null)
            {
                StreamWriter sw = new StreamWriter(MainMenu.settings.pathForOutputFolder + "\\Баги.txt");
                sw.Write(debug);
                sw.Close();
                listBox1.Items.Add("Bugs have been written in file " + MainMenu.settings.pathForOutputFolder + "\\Баги.txt");
            }

        end:
            int closing = 0;
        }
        public static void ReadProp(byte[] binContent, List<Prop> prop, ref byte[] header, ref byte[] countOfBlock, ref byte[] header2, ref byte[] lenght_of_all_text, ref byte[] end_of_file)
        {
            byte[] vers_bytes = new byte[16];
            Array.Copy(binContent, 4, vers_bytes, 0, 4);
            int vers = BitConverter.ToInt32(vers_bytes, 0);
            int poz = 0;
            int end = 0;
            bool first_blocks = false; //Если первое свойство имеет несколько значений
            bool second_blocks = false; //Если второе свойство имеет несколько значений


            //int end2 = 0;
            if (vers == 3)
            {
                //poz = 60;
                poz = 52;
                
                byte[] blockLength = new byte[4]; //блок с названием файла. Если его нет, то длина равняется 8 байт.
                                                  //Вообще, я так понял, тут и список зависимых свойств перечисляется.  
                Array.Copy(binContent, poz, blockLength, 0, 4);
                poz += BitConverter.ToInt32(blockLength, 0);

                header = new byte[poz];
                Array.Copy(binContent, 0, header, 0, poz);
                Array.Copy(binContent, poz, lenght_of_all_text, 0, 4);
                poz += 4;
                end = binContent.Length - BitConverter.ToInt32(lenght_of_all_text, 0) - poz + 8;
            }
            else
            {
                MessageBox.Show("Unkown prop. Please write me about this!");
            }
            //try
            {
                //после длины фаила 0x10 байт фигни.
                //далее количество текстовых блоков
                //потом блок 8 байт
                //блок имени 
                //4 байта количество строк
                //потом блоки текста(имя+текст)
                //и потом снова 8 байт и т.д.

                int z = 16;
                header2 = new byte[z];
                Array.Copy(binContent, poz, header2, 0, z);
                poz += z;

                countOfBlock = new byte[4];
                Array.Copy(binContent, poz, countOfBlock, 0, 4);
                int intCountOfBlock = BitConverter.ToInt32(countOfBlock, 0);
                poz += 4;
                bool withoutBlock = false;
                byte[] b = { 0x01, 0x00, 0x00, 0x00, 0xB4, 0xF4, 0x5A, 0x5F, 0x60, 0x6E, 0x9C, 0xCD, 0x00, 0x00, 0x00, 0x00 };
                if (header2 != b)
                {
                    withoutBlock = true;
                }
                while (prop.Count < intCountOfBlock)
                {
                    //кодить тут
                    byte[] hz_data = new byte[8];
                    Array.Copy(binContent, poz, hz_data, 0, 8);
                    poz += 8;

                    //имя блока
                    byte[] lenght_of_name = new byte[4];
                    Array.Copy(binContent, poz, lenght_of_name, 0, 4);
                    poz += 4;
                    //получаем что-то, если существует
                    int len_name = BitConverter.ToInt32(lenght_of_name, 0);
                    string name = Methods.ConvertHexToString(binContent, poz, len_name, MainMenu.settings.ASCII_N, false);
                    poz += len_name;

                    //получаем количество строк в блоке
                    byte[] countOfString = new byte[4];
                    Array.Copy(binContent, poz, countOfString, 0, 4);
                    int count_of_string = BitConverter.ToInt32(countOfString, 0);
                    poz += 4;

                    List<TextInProp> textInProp = new List<TextInProp>();
                    for (int i = 0; i < count_of_string; i++)
                    {
                        byte[] lenght_of_string_name = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_string_name, 0, 4);
                        poz += 4;
                        //получаем что-то, если существует
                        int len_string_name = BitConverter.ToInt32(lenght_of_string_name, 0);
                        string string_name = Methods.ConvertHexToString(binContent, poz, len_string_name, MainMenu.settings.ASCII_N, false);
                        poz += len_string_name;
                        byte[] lenght_of_string_text = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_string_text, 0, 4);
                        poz += 4;
                        //получаем текст
                        int len_string_text = BitConverter.ToInt32(lenght_of_string_text, 0);
                        string string_text = Methods.ConvertHexToString(binContent, poz, len_string_text, MainMenu.settings.ASCII_N, false);
                        poz += len_string_text;

                        textInProp.Add(new TextInProp(lenght_of_string_name, string_name, lenght_of_string_text, string_text));
                    }
                    prop.Add(new Prop(hz_data, lenght_of_name, name, textInProp));
                }
            }
            //catch { }
        }

        public static void CreateProp(byte[] header, byte[] countOfBlock, byte[] header2, List<Prop> prop, byte[] end_of_file, string path)
        {
            //проверяем наличие файла, удаляем его и создаем пустой
            FileStream MyFileStream;
            if (System.IO.File.Exists(path) == true)
            {
                System.IO.File.Delete(path);
            }
            MyFileStream = new FileStream(path, FileMode.OpenOrCreate);
            //записываем заголовок

            MyFileStream.Write(header, 0, header.Length);


            int sizeLangdb = 4;
            sizeLangdb += header2.Length;
            sizeLangdb += countOfBlock.Length;
            for (int i = 0; i < prop.Count; i++)
            {

                sizeLangdb += prop[i].hz_data.Length;
                sizeLangdb += 4;//4 байта на количество строк в блоке
                sizeLangdb += GetSizeOfByteMassiv(prop[i].lenght_of_name);//длина имени
                sizeLangdb += GetSizeOfString(prop[i].name);
                for (int j = 0; j < prop[i].textInProp.Count; j++)
                {
                    sizeLangdb += 8;
                    sizeLangdb += prop[i].textInProp[j].name.Length;
                    sizeLangdb += prop[i].textInProp[j].text.Length;
                }
                //sizeLangdb += GetSizeOfByteMassiv(prop[i].lenght_of_text);//длина текста
                //sizeLangdb += GetSizeOfString(prop[i].text);  
            }
            byte[] hex_size = BitConverter.GetBytes(sizeLangdb);
            MyFileStream.Write(hex_size, 0, hex_size.Length);
            MyFileStream.Write(header2, 0, header2.Length);
            MyFileStream.Write(countOfBlock, 0, countOfBlock.Length);


            ////записываем всё остальное
            for (int q = 0; q < prop.Count; q++)
            {
                //имя
                MyFileStream.Write(prop[q].hz_data, 0, prop[q].hz_data.Length);
                SaveStringInfoForProp(MyFileStream, prop[q].name, MainMenu.settings.ASCII_N);
                //количество строк в блоке
                byte[] hex_count = BitConverter.GetBytes(prop[q].textInProp.Count);
                MyFileStream.Write(hex_count, 0, hex_count.Length);
                //перечисляем имена со строками
                for (int w = 0; w < prop[q].textInProp.Count; w++)
                {
                    //имя
                    SaveStringInfoForProp(MyFileStream, prop[q].textInProp[w].name, MainMenu.settings.ASCII_N);
                    //текст
                    SaveStringInfoForProp(MyFileStream, prop[q].textInProp[w].text, MainMenu.settings.ASCII_N);
                }
            }
            //закрываем поток
            MyFileStream.Close();
        }

        public static void ReadDlog(byte[] binContent, dlog[] first_database, List<Langdb> database, byte version)
        {
            {
                int number = 0;
                int poz = 0;
                int poz_add = 0;
                int last_pozition = 0;
                int poz_start = 0;

                //вычисляем начало файла
                byte[] temp1 = new byte[4];
                byte[] temp2 = new byte[4];
                int tmp1 = 0;
                int tmp2 = 0;
                for (int i = poz; i < binContent.Length; i++)
                {
                    Array.Copy(binContent, i, temp1, 0, 4);
                    tmp1 = BitConverter.ToInt32(temp1, 0);
                    Array.Copy(binContent, i + 4, temp2, 0, 4);
                    tmp2 = BitConverter.ToInt32(temp2, 0);
                    if ((tmp1 - 8) == tmp2 && tmp2 < 255 && tmp2 != 0)
                    {
                        poz_add = i;
                        poz_start = i;
                        break;
                    }
                }
                poz_start = poz_add;
                first_database[0].head = new byte[poz_add];
                Array.Copy(binContent, poz, first_database[0].head, 0, poz_add);
                poz += poz_add;

                poz_add = 4;
                first_database[0].lenght_of_name_langdb = new byte[poz_add];
                Array.Copy(binContent, poz, first_database[0].lenght_of_name_langdb, 0, poz_add);
                poz += poz_add;

                poz_add = 4;
                first_database[0].lenght_of_name_langdb_minus_4 = new byte[poz_add];
                Array.Copy(binContent, poz, first_database[0].lenght_of_name_langdb_minus_4, 0, poz_add);
                poz += poz_add;

                int len_text = BitConverter.ToInt32(first_database[0].lenght_of_name_langdb_minus_4, 0);
                byte[] hex_text = new byte[len_text];
                //получаем имя базы
                first_database[number].name_of_langdb = Methods.ConvertHexToString(binContent, poz, len_text, MainMenu.settings.ASCII_N, false);
                poz += len_text;

                poz_add = 20;//0x14
                first_database[0].hlam = new byte[poz_add];
                Array.Copy(binContent, poz, first_database[0].hlam, 0, poz_add);
                poz += poz_add;
                //MessageBox.Show(poz.ToString());

                poz_add = 4;
                first_database[0].lenght_of_langdb1 = new byte[poz_add];
                Array.Copy(binContent, poz, first_database[0].lenght_of_langdb1, 0, poz_add);
                poz += poz_add;
                poz_start = poz;
                //poz -= 4;
                int lenght_of_langdb = BitConverter.ToInt32(first_database[0].lenght_of_langdb1, 0);
                poz_add = lenght_of_langdb;
                first_database[0].langdb = new byte[poz_add - 4];
                Array.Copy(binContent, poz, first_database[0].langdb, 0, poz_add - 4);
                poz += poz_add - 4;

                poz_add = binContent.Length - poz;
                first_database[0].other = new byte[poz_add];
                Array.Copy(binContent, poz, first_database[0].other, 0, poz_add);
                poz += poz_add;


                //начинаем langdb разбирать
                poz_add = 16;
                poz = 0;
                first_database[0].start_langdb = new byte[poz_add];
                Array.Copy(first_database[0].langdb, poz, first_database[0].start_langdb, 0, poz_add);
                poz_add = 0;
                poz = 16;
                #region
                try
                {
                    while (poz < first_database[0].langdb.Length)
                    {
                        //надо пропустить н-байт, которые хз_дата, а потом развинтить всё остальное
                        byte[] hz_data = new byte[20];
                        Array.Copy(first_database[0].langdb, poz, hz_data, 0, 20);
                        poz += 20;

                        poz += 4;
                        byte[] lenght_of_animation = new byte[4];
                        Array.Copy(first_database[0].langdb, poz, lenght_of_animation, 0, 4);
                        poz += 4;
                        //получаем анимацию
                        int len_animation = BitConverter.ToInt32(lenght_of_animation, 0);
                        string animation = Methods.ConvertHexToString(first_database[0].langdb, poz, len_animation, MainMenu.settings.ASCII_N, false);
                        poz += len_animation;

                        poz += 4;
                        byte[] lenght_of_waw = new byte[4];
                        Array.Copy(first_database[0].langdb, poz, lenght_of_waw, 0, 4);
                        poz += 4;
                        //получаем озвучки
                        int len_waw = BitConverter.ToInt32(lenght_of_waw, 0);
                        string waw = Methods.ConvertHexToString(first_database[0].langdb, poz, len_waw, MainMenu.settings.ASCII_N, false);
                        poz += len_waw;


                        //далее идут 8 байт.
                        //4 байта = длина текстового блока
                        byte[] lenght_of_textblok = new byte[4];
                        Array.Copy(first_database[0].langdb, poz, lenght_of_textblok, 0, 4);
                        poz += 4;
                        int len_of_text_blok = BitConverter.ToInt32(lenght_of_textblok, 0);
                        if (len_of_text_blok != 8)
                        {
                            //4 байта = номер строки в записи
                            byte[] count_text = new byte[4];
                            Array.Copy(first_database[0].langdb, poz, count_text, 0, 4);
                            poz += 4;

                            poz += 4;
                            byte[] lenght_of_name = new byte[4];
                            Array.Copy(first_database[0].langdb, poz, lenght_of_name, 0, 4);
                            poz += 4;
                            //получаем имя
                            int len_name = BitConverter.ToInt32(lenght_of_name, 0);

                            byte[] temp_hex_name = new byte[len_name];
                            Array.Copy(first_database[0].langdb, poz, temp_hex_name, 0, len_name);
                            string name = ASCIIEncoding.GetEncoding(1251).GetString(temp_hex_name);
                            poz += len_name;

                            poz += 4;
                            byte[] lenght_of_text = new byte[4];
                            Array.Copy(first_database[0].langdb, poz, lenght_of_text, 0, 4);
                            poz += 4;
                            //получаем текст
                            len_text = BitConverter.ToInt32(lenght_of_text, 0);
                            string text = Methods.ConvertHexToString(first_database[0].langdb, poz, len_text, MainMenu.settings.ASCII_N, false);
                            //MessageBox.Show(database[number].text);
                            poz += len_text;

                            //получаем магические байты
                            int len_magic = len_of_text_blok - 8 - 8 - len_name - len_text - 8;

                            if (BitConverter.ToInt32(count_text, 0) == 1)
                            {
                                byte[] magic_bytes = new byte[len_magic];
                                Array.Copy(first_database[0].langdb, poz, magic_bytes, 0, len_magic);
                                database.Add(new Langdb(hz_data, lenght_of_textblok, count_text, lenght_of_name, name, lenght_of_text, text, lenght_of_waw, waw, lenght_of_animation, animation, magic_bytes));
                                poz += len_magic;
                            }
                            else if ((BitConverter.ToInt32(count_text, 0) == 2))
                            {
                                byte[] magic_bytes = new byte[8];
                                Array.Copy(first_database[0].langdb, poz, magic_bytes, 0, 8);
                                database.Add(new Langdb(hz_data, lenght_of_textblok, count_text, lenght_of_name, name, lenght_of_text, text, lenght_of_waw, waw, lenght_of_animation, animation, magic_bytes));
                                poz += 8;

                                poz += 4;
                                lenght_of_name = new byte[4];
                                Array.Copy(first_database[0].langdb, poz, lenght_of_name, 0, 4);
                                poz += 4;
                                //получаем имя
                                len_name = BitConverter.ToInt32(lenght_of_name, 0);

                                temp_hex_name = new byte[len_name];
                                Array.Copy(first_database[0].langdb, poz, temp_hex_name, 0, len_name);
                                name = ASCIIEncoding.GetEncoding(1251).GetString(temp_hex_name);
                                poz += len_name;

                                poz += 4;
                                lenght_of_text = new byte[4];
                                Array.Copy(first_database[0].langdb, poz, lenght_of_text, 0, 4);
                                poz += 4;
                                //получаем текст
                                len_text = BitConverter.ToInt32(lenght_of_text, 0);
                                text = Methods.ConvertHexToString(first_database[0].langdb, poz, len_text, MainMenu.settings.ASCII_N, false);
                                //MessageBox.Show(database[number].text);
                                poz += len_text;

                                magic_bytes = new byte[8];
                                Array.Copy(first_database[0].langdb, poz, magic_bytes, 0, 8);
                                poz += 8;

                                database.Add(new Langdb(null, lenght_of_textblok, null, lenght_of_name, name, lenght_of_text, text, null, null, null, null, magic_bytes));
                            }
                            else
                            {
                                MessageBox.Show("Напишите разработчику! В одной из записей dlog находится три строки!");
                            }
                        }
                        else
                        {
                            //MessageBox.Show("Нулевая строчка!!!");
                            int len_magic = 4;
                            byte[] magic_bytes = new byte[len_magic];
                            Array.Copy(first_database[0].langdb, poz, magic_bytes, 0, len_magic);
                            poz += len_magic;
                            database.Add(new Langdb(hz_data, lenght_of_textblok, null, null, null, null, null, lenght_of_waw, waw, lenght_of_animation, animation, magic_bytes));
                        }
                        number++;
                        last_pozition = poz;
                    }
                }
                catch
                {
                    //MessageBox.Show('"' + "Конец" + '"' + ':' + (last_pozition - 8).ToString("x"));
                }
                #endregion
                first_database[0].end_langdb = new byte[(lenght_of_langdb - last_pozition - 4)];
                Array.Copy(first_database[0].langdb, last_pozition, first_database[0].end_langdb, 0, (lenght_of_langdb - last_pozition - 4));
                number--;
            }
        }
        public struct dlog
        {
            public byte[] head;
            public byte[] lenght_of_name_langdb_minus_4;
            public byte[] lenght_of_name_langdb;
            public string name_of_langdb;
            public byte[] hlam;
            public byte[] lenght_of_langdb1;
            public byte[] lenght_of_langdb2;
            public byte[] start_langdb;
            public byte[] langdb;
            public byte[] end_langdb;
            public byte[] other;
        }
        dlog[] first_database = new dlog[1];
        public static void ReadLandb(byte[] binContent, List<Langdb> landb, ref List<byte[]> header, ref byte[] lenght_of_all_text, ref List<byte[]> end_of_file)
        {

            int poz = 0;

            bool UnicodeSupport = false;
            byte[] ERTM = new byte[4];//первые 4 байта - версия
            Array.Copy(binContent, 0, ERTM, 0, 4);
            header.Add(ERTM);
            poz += 4;
            
            //string etalon = "5VSM";
            if (ConvertHexToString(ERTM, 0, 4) == "5VSM" || ConvertHexToString(ERTM, 0, 4) == "6VSM")
            {
                poz = 4;
                byte[] textBlock = new byte[4];//вторые - длина блока с текстом
                Array.Copy(binContent, poz, textBlock, 0, 4);
                int lenghtOfTextBlock = BitConverter.ToInt32(textBlock, 0);
                header.Add(textBlock);
                poz += 4;
                if (lenghtOfTextBlock > 60)
                {
                    byte[] otherBlock = new byte[4];//третьи - длина блока с остальным
                    Array.Copy(binContent, poz, otherBlock, 0, 4);
                    header.Add(otherBlock);
                    poz += 4;
                    byte[] somthing4Byte = new byte[4];
                    Array.Copy(binContent, poz, somthing4Byte, 0, 4);
                    header.Add(somthing4Byte);
                    poz += 4;
                    //проверяем версию
                    byte[] vers_bytes = new byte[4];
                    Array.Copy(binContent, poz, vers_bytes, 0, 4);
                    header.Add(vers_bytes);
                    poz += 4;
                    int vers = BitConverter.ToInt32(vers_bytes, 0);
                    int lenghtForHeaderForVersion = 0;//в зависимости от версии указанной в фаиле задаем в Case необходимые параметры
                    int startPoz = 0;
                    int len_magic = 0; //len_of_text_blok - 4 - 8 - len_name - len_text - 8; - В Волке вроде так

                    if (vers == 9 || vers == 10)
                    {


                        switch (vers)
                        {
                            case 9:
                                {
                                    lenghtForHeaderForVersion = 108;
                                    len_magic = 12;
                                    break;
                                }
                            case 10:
                                {
                                    lenghtForHeaderForVersion = 120;
                                    len_magic = 20;
                                    if (MainMenu.settings.unicodeSettings == 0) UnicodeSupport = true;
                                    break;
                                }
                        }
                        byte[] headerHash = new byte[lenghtForHeaderForVersion];
                        Array.Copy(binContent, poz, headerHash, 0, lenghtForHeaderForVersion);
                        header.Add(headerHash);
                        poz += lenghtForHeaderForVersion;
                        startPoz = poz;

                        for (int q = 0; q < 5; q++)
                        {
                            byte[] tempH = new byte[4];
                            Array.Copy(binContent, poz, tempH, 0, 4);
                            header.Add(tempH);
                            poz += 4;
                        }
                    }

                    else
                    { MessageBox.Show("Версия " + vers.ToString()); }

                    byte[] tempC = new byte[4];
                    Array.Copy(binContent, poz, tempC, 0, 4);
                    header.Add(tempC);
                    int countOfString = BitConverter.ToInt32(tempC, 0);

                    poz += 4;
                    int c = 0;
                    while (c < countOfString)
                    {
                        byte[] hz_data = new byte[16 + 40];
                        Array.Copy(binContent, poz, hz_data, 0, 16 + 40);
                        poz += 16 + 40;


                        //4 байта = длина текстового блока
                        byte[] lenght_of_textblok = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_textblok, 0, 4);
                        poz += 4;
                        int len_of_text_blok = BitConverter.ToInt32(lenght_of_textblok, 0);

                        poz += 4;//
                        byte[] lenght_of_name = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_name, 0, 4);
                        poz += 4;
                        //получаем имя
                        int len_name = BitConverter.ToInt32(lenght_of_name, 0);
                        string name = Methods.ConvertHexToString(binContent, poz, len_name, MainMenu.settings.ASCII_N, UnicodeSupport);
                        poz += len_name;

                        poz += 4;
                        byte[] lenght_of_text = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_text, 0, 4);
                        poz += 4;
                        //получаем текст
                        int len_text = BitConverter.ToInt32(lenght_of_text, 0);
                        string text = Methods.ConvertHexToString(binContent, poz, len_text, MainMenu.settings.ASCII_N, UnicodeSupport);

                        //MessageBox.Show(database[number].text);
                        poz += len_text;

                        byte[] magic_bytes = new byte[len_magic];
                        Array.Copy(binContent, poz, magic_bytes, 0, len_magic);
                        poz += len_magic;

                        landb.Add(new Langdb(hz_data, lenght_of_textblok, null, lenght_of_name, name, lenght_of_text, text, null, null, null, null, magic_bytes));
                        c++;
                    }
                    int hvost = lenghtOfTextBlock + startPoz - poz;
                    byte[] t = new byte[hvost];
                    Array.Copy(binContent, poz, t, 0, hvost);
                    end_of_file.Add(t);
                    poz += hvost;

                    t = new byte[binContent.Length - poz];
                    Array.Copy(binContent, poz, t, 0, binContent.Length - poz);
                    end_of_file.Add(t);

                }
            }
            else
            {
                header.Remove(ERTM);
                byte[] vers_bytes = new byte[16];
                Array.Copy(binContent, 0, vers_bytes, 0, 4);
                Array.Copy(binContent, 4, vers_bytes, 0, 4);

                int vers = BitConverter.ToInt32(vers_bytes, 0);
                int end = 0;
                //int end2 = 0;
                if (vers == 8)
                {
                    poz = 120;
                    byte[] headerH = new byte[poz];
                    Array.Copy(binContent, 0, headerH, 0, poz);
                    header.Add(headerH);
                    Array.Copy(binContent, poz, lenght_of_all_text, 0, 4);
                    poz += 4;
                    end = binContent.Length - BitConverter.ToInt32(lenght_of_all_text, 0) - poz + 8;
                    //end = 72;
                    //end2 = 92;
                }
                else if (vers == 9)
                {
                    poz = 132;
                    byte[] headerH = new byte[poz];
                    Array.Copy(binContent, 0, headerH, 0, poz);
                    header.Add(headerH);
                    Array.Copy(binContent, poz, lenght_of_all_text, 0, 4);
                    poz += 4;
                    end = binContent.Length - BitConverter.ToInt32(lenght_of_all_text, 0) - poz + 8;
                    //end = 80;
                    //end2 = 80;
                }
                else if (vers == 5)
                {
                    poz = binContent.Length;
                }
                else
                {
                    MessageBox.Show("Error. Please write me about this!");
                }
                //try
                {
                    while (poz < binContent.Length)
                    {
                        //if (poz == binContent.Length - end2)
                        //{
                        //    end_of_file = new byte[end2];
                        //    Array.Copy(binContent, poz, end_of_file, 0, end2);
                        //    break;
                        //}
                        //else 
                        if (poz == binContent.Length - end)
                        {
                            byte[] end_of_fileb = new byte[end];
                            Array.Copy(binContent, poz, end_of_fileb, 0, end);
                            end_of_file.Add(end_of_fileb);
                            break;
                        }

                        byte[] hz_data = new byte[16];
                        Array.Copy(binContent, poz, hz_data, 0, 16);
                        poz += 16;

                        poz += 4;
                        byte[] lenght_of_animation = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_animation, 0, 4);
                        poz += 4;

                        //получаем анимацию
                        int len_animation = BitConverter.ToInt32(lenght_of_animation, 0);
                        string animation = Methods.ConvertHexToString(binContent, poz, len_animation, MainMenu.settings.ASCII_N, false);
                        poz += len_animation;

                        poz += 4;
                        byte[] lenght_of_waw = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_waw, 0, 4);
                        poz += 4;
                        //получаем озвучки
                        int len_waw = BitConverter.ToInt32(lenght_of_waw, 0);
                        string waw = Methods.ConvertHexToString(binContent, poz, len_waw, MainMenu.settings.ASCII_N, false);
                        poz += len_waw;

                        byte[] count_text = new byte[12];
                        Array.Copy(binContent, poz, count_text, 0, 12);
                        poz += 12;

                        //4 байта = длина текстового блока
                        byte[] lenght_of_textblok = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_textblok, 0, 4);
                        poz += 4;
                        int len_of_text_blok = BitConverter.ToInt32(lenght_of_textblok, 0);

                        poz += 4;
                        byte[] lenght_of_name = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_name, 0, 4);
                        poz += 4;
                        //получаем имя
                        int len_name = BitConverter.ToInt32(lenght_of_name, 0);
                        string name = Methods.ConvertHexToString(binContent, poz, len_name, MainMenu.settings.ASCII_N, false);
                        poz += len_name;

                        poz += 4;
                        byte[] lenght_of_text = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_text, 0, 4);
                        poz += 4;
                        //получаем текст
                        int len_text = BitConverter.ToInt32(lenght_of_text, 0);
                        string text = Methods.ConvertHexToString(binContent, poz, len_text, MainMenu.settings.ASCII_N, false);
                        //MessageBox.Show(database[number].text);
                        poz += len_text;

                        //получаем магические байты
                        int len_magic = len_of_text_blok - 4 - 8 - len_name - len_text - 8;
                        byte[] magic_bytes = new byte[len_magic];
                        Array.Copy(binContent, poz, magic_bytes, 0, len_magic);
                        poz += len_magic;
                        landb.Add(new Langdb(hz_data, lenght_of_textblok, count_text, lenght_of_name, name, lenght_of_text, text, lenght_of_waw, waw, lenght_of_animation, animation, magic_bytes));
                    }
                }
            }
            //catch { }
        }
        public static void ReadLandbTFTB(byte[] binContent, List<Langdb> landb, ref List<byte[]> header, ref byte[] lenght_of_all_text, ref List<byte[]> end_of_file)
        {
            int poz = 0;

            byte[] ERTM = new byte[4];//первые 4 байта - версия
            Array.Copy(binContent, 0, ERTM, 0, 4);
            header.Add(ERTM);
            poz += 4;

            string etalon = "5VSM";
            if (ConvertHexToString(ERTM, 0, 4) == etalon)
            {
                poz = 4;
                byte[] textBlock = new byte[4];//вторые - длина блока с текстом
                Array.Copy(binContent, poz, textBlock, 0, 4);
                int lenghtOfTextBlock = BitConverter.ToInt32(textBlock, 0);
                header.Add(textBlock);
                poz += 4;
                if (lenghtOfTextBlock > 60)
                {
                    byte[] otherBlock = new byte[4];//третьи - длина блока с остальным
                    Array.Copy(binContent, poz, otherBlock, 0, 4);
                    header.Add(otherBlock);
                    poz += 4;
                    byte[] somthing4Byte = new byte[4];
                    Array.Copy(binContent, poz, somthing4Byte, 0, 4);
                    header.Add(somthing4Byte);
                    poz += 4;
                    //проверяем версию
                    byte[] vers_bytes = new byte[4];
                    Array.Copy(binContent, poz, vers_bytes, 0, 4);
                    header.Add(vers_bytes);
                    poz += 4;
                    int vers = BitConverter.ToInt32(vers_bytes, 0);
                    int startPoz = 0;
                    if (vers == 10)
                    {

                        byte[] headerHash = new byte[120];
                        Array.Copy(binContent, poz, headerHash, 0, 120);
                        header.Add(headerHash);
                        poz += 120;
                        startPoz = poz;
                        for (int q = 0; q < 5; q++)
                        {
                            byte[] tempH = new byte[4];
                            Array.Copy(binContent, poz, tempH, 0, 4);
                            header.Add(tempH);
                            poz += 4;
                        }
                    }
                    else { MessageBox.Show("Версия 8"); }

                    byte[] tempC = new byte[4];
                    Array.Copy(binContent, poz, tempC, 0, 4);
                    header.Add(tempC);
                    int countOfString = BitConverter.ToInt32(tempC, 0);

                    poz += 4;
                    int c = 0;
                    while (c < countOfString)
                    {
                        byte[] hz_data = new byte[16 + 40];
                        Array.Copy(binContent, poz, hz_data, 0, 16 + 40);
                        poz += 16 + 40;


                        //4 байта = длина текстового блока
                        byte[] lenght_of_textblok = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_textblok, 0, 4);
                        poz += 4;
                        int len_of_text_blok = BitConverter.ToInt32(lenght_of_textblok, 0);

                        poz += 4;//
                        byte[] lenght_of_name = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_name, 0, 4);
                        poz += 4;
                        //получаем имя
                        int len_name = BitConverter.ToInt32(lenght_of_name, 0);
                        string name = Methods.ConvertHexToString(binContent, poz, len_name, MainMenu.settings.ASCII_N, true);
                        poz += len_name;

                        poz += 4;
                        byte[] lenght_of_text = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_text, 0, 4);
                        poz += 4;
                        //получаем текст
                        int len_text = BitConverter.ToInt32(lenght_of_text, 0);
                        string text = Methods.ConvertHexToString(binContent, poz, len_text, MainMenu.settings.ASCII_N, true);
                        //MessageBox.Show(database[number].text);
                        poz += len_text;


                        int len_magic = 20;//len_of_text_blok - 4 - 8 - len_name - len_text - 8;
                        byte[] magic_bytes = new byte[len_magic];
                        Array.Copy(binContent, poz, magic_bytes, 0, len_magic);
                        poz += len_magic;

                        landb.Add(new Langdb(hz_data, lenght_of_textblok, null, lenght_of_name, name, lenght_of_text, text, null, null, null, null, magic_bytes));
                        c++;
                    }
                    int hvost = lenghtOfTextBlock + startPoz - poz;
                    byte[] t = new byte[hvost];
                    Array.Copy(binContent, poz, t, 0, hvost);
                    end_of_file.Add(t);
                    poz += hvost;

                    t = new byte[binContent.Length - poz];
                    Array.Copy(binContent, poz, t, 0, binContent.Length - poz);
                    end_of_file.Add(t);

                }


            }
            else
            {
                byte[] vers_bytes = new byte[16];
                Array.Copy(binContent, 0, vers_bytes, 0, 4);
                Array.Copy(binContent, 4, vers_bytes, 0, 4);
                int vers = BitConverter.ToInt32(vers_bytes, 0);
                int end = 0;
                //int end2 = 0;
                if (vers == 8)
                {
                    poz = 120;
                    byte[] headerH = new byte[poz];
                    Array.Copy(binContent, 0, headerH, 0, poz);
                    header.Add(headerH);
                    Array.Copy(binContent, poz, lenght_of_all_text, 0, 4);
                    poz += 4;
                    end = binContent.Length - BitConverter.ToInt32(lenght_of_all_text, 0) - poz + 8;
                    //end = 72;
                    //end2 = 92;
                }
                else if (vers == 9)
                {
                    poz = 132;
                    byte[] headerH = new byte[poz];
                    Array.Copy(binContent, 0, headerH, 0, poz);
                    header.Add(headerH);
                    Array.Copy(binContent, poz, lenght_of_all_text, 0, 4);
                    poz += 4;
                    end = binContent.Length - BitConverter.ToInt32(lenght_of_all_text, 0) - poz + 8;
                    //end = 80;
                    //end2 = 80;
                }
                else if (vers == 5)
                {
                    poz = binContent.Length;
                }
                else
                {
                    MessageBox.Show("Error. Please write me about this!");
                }
                //try
                {
                    while (poz < binContent.Length)
                    {
                        //if (poz == binContent.Length - end2)
                        //{
                        //    end_of_file = new byte[end2];
                        //    Array.Copy(binContent, poz, end_of_file, 0, end2);
                        //    break;
                        //}
                        //else 
                        if (poz == binContent.Length - end)
                        {
                            byte[] end_of_fileb = new byte[end];
                            Array.Copy(binContent, poz, end_of_fileb, 0, end);
                            end_of_file.Add(end_of_fileb);
                            break;
                        }

                        byte[] hz_data = new byte[16];
                        Array.Copy(binContent, poz, hz_data, 0, 16);
                        poz += 16;

                        poz += 4;
                        byte[] lenght_of_animation = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_animation, 0, 4);
                        poz += 4;

                        //получаем анимацию
                        int len_animation = BitConverter.ToInt32(lenght_of_animation, 0);
                        string animation = Methods.ConvertHexToString(binContent, poz, len_animation, MainMenu.settings.ASCII_N, true);
                        poz += len_animation;

                        poz += 4;
                        byte[] lenght_of_waw = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_waw, 0, 4);
                        poz += 4;
                        //получаем озвучки
                        int len_waw = BitConverter.ToInt32(lenght_of_waw, 0);
                        string waw = Methods.ConvertHexToString(binContent, poz, len_waw, MainMenu.settings.ASCII_N, true);
                        poz += len_waw;

                        byte[] count_text = new byte[12];
                        Array.Copy(binContent, poz, count_text, 0, 12);
                        poz += 12;

                        //4 байта = длина текстового блока
                        byte[] lenght_of_textblok = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_textblok, 0, 4);
                        poz += 4;
                        int len_of_text_blok = BitConverter.ToInt32(lenght_of_textblok, 0);

                        poz += 4;
                        byte[] lenght_of_name = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_name, 0, 4);
                        poz += 4;
                        //получаем имя
                        int len_name = BitConverter.ToInt32(lenght_of_name, 0);
                        string name = Methods.ConvertHexToString(binContent, poz, len_name, MainMenu.settings.ASCII_N, true);
                        poz += len_name;

                        poz += 4;
                        byte[] lenght_of_text = new byte[4];
                        Array.Copy(binContent, poz, lenght_of_text, 0, 4);
                        poz += 4;
                        //получаем текст
                        int len_text = BitConverter.ToInt32(lenght_of_text, 0);
                        string text = Methods.ConvertHexToString(binContent, poz, len_text, MainMenu.settings.ASCII_N, true);
                        //MessageBox.Show(database[number].text);
                        poz += len_text;

                        //получаем магические байты
                        int len_magic = len_of_text_blok - 4 - 8 - len_name - len_text - 8;
                        byte[] magic_bytes = new byte[len_magic];
                        Array.Copy(binContent, poz, magic_bytes, 0, len_magic);
                        poz += len_magic;
                        landb.Add(new Langdb(hz_data, lenght_of_textblok, count_text, lenght_of_name, name, lenght_of_text, text, lenght_of_waw, waw, lenght_of_animation, animation, magic_bytes));
                    }
                }
            }
            //catch { }
        }

        public class chapterOfDDS
        {
            public byte[] number_of_chapter;
            public byte[] one;
            public byte[] lenght_of_chapter;
            public byte[] kratnost;
            public byte[] content_chapter;
            public byte[] hz;

            public chapterOfDDS() { }
            public chapterOfDDS(byte[] number_of_chapter, byte[] one, byte[] lenght_of_chapter, byte[] kratnost, byte[] content_chapter, byte[] hz)
            {
                this.number_of_chapter = number_of_chapter;
                this.one = one;
                this.lenght_of_chapter = lenght_of_chapter;
                this.kratnost = kratnost;
                this.content_chapter = content_chapter;
                this.hz = hz;
            }

        }


        public class TextInProp
        {
            public byte[] lenght_of_name;
            public string name;
            public byte[] lenght_of_text;
            public string text;

            public TextInProp() { }
            public TextInProp(byte[] lenght_of_name, string name, byte[] lenght_of_text, string text)
            {
                this.lenght_of_name = lenght_of_name;
                this.lenght_of_text = lenght_of_text;
                this.name = name;
                this.text = text;
            }
        }

        public class Prop
        {
            public byte[] hz_data;
            public byte[] lenght_of_name;
            public string name;
            public List<TextInProp> textInProp;

            public Prop() { }
            public Prop(byte[] hz_data, byte[] lenght_of_name, string name, List<TextInProp> textInProp)
            {
                this.hz_data = hz_data;
                this.lenght_of_name = lenght_of_name;
                this.name = name;
                this.textInProp = textInProp;
            }
        }

        public class PropWithoutBlocks
        {
            public byte[] hz_data;
            public byte[] lenght_of_name;
            public string name;
            public byte[] lenght_of_text;
            public string text;

            public PropWithoutBlocks() { }
            public PropWithoutBlocks(byte[] hz_data, byte[] lenght_of_name, string name, byte[] lenght_of_text, string text)
            {
                this.hz_data = hz_data;
                this.lenght_of_name = lenght_of_name;
                this.name = name;
                this.lenght_of_text = lenght_of_text;
                this.text = text;
            }
        }

        public class Langdb
        {
            public byte[] hz_data;
            public byte[] lenght_of_textblok;
            public byte[] count_text;
            public byte[] lenght_of_name;
            public string name;
            public byte[] lenght_of_text;
            public string text;
            public byte[] lenght_of_waw;
            public string waw;
            public byte[] lenght_of_animation;
            public string animation;
            public byte[] magic_bytes;

            public Langdb() { }
            public Langdb(byte[] hz_data, byte[] lenght_of_textblok, byte[] count_text, byte[] lenght_of_name,
            string name, byte[] lenght_of_text, string text, byte[] lenght_of_waw,
            string waw, byte[] lenght_of_animation, string animation, byte[] magic_bytes)
            {
                this.animation = animation;
                this.hz_data = hz_data;
                this.lenght_of_animation = lenght_of_animation;
                this.lenght_of_name = lenght_of_name;
                this.lenght_of_text = lenght_of_text;
                this.lenght_of_textblok = lenght_of_textblok;
                this.lenght_of_waw = lenght_of_waw;
                this.magic_bytes = magic_bytes;
                this.name = name;
                this.text = text;
                this.waw = waw;
                this.count_text = count_text;
            }
        }

        public static void CreateDlog(dlog[] first_database, List<Langdb> database, byte version, string path)
        {
            //проверяем наличие файла, удаляем его и создаем пустой
            FileStream MyFileStream;
            if (System.IO.File.Exists(path) == true)
            {
                System.IO.File.Delete(path);
            }
            MyFileStream = new FileStream(path, FileMode.CreateNew);
            //записываем заголовок
            int numb = 0;

            //
            //высчитываем размер langdb-файла
            int sizeLangdb1 = 0;
            int sizeLangdb2 = 0;
            sizeLangdb1 += 4;//длина длины файла =)
            sizeLangdb1 += 16;//какая-то херь в начале
            for (int i = 0; i < database.Count; i++)
            {
                //int len_animation = database[i].animation.Length;
                //int len_waw = database[i].waw.Length;
                //int len_name = database[i].name.Length;
                //int len_text = database[i].text.Length;
                //int len_magic = database[i].magic_bytes.Length;
                //sizeLangdb1 += 20 + 4 + 4 + len_animation + 4 + 4 + len_waw + 8 + 4 + 4 + len_name + 4 + 4 + len_text + len_magic;
                if (BitConverter.ToInt32(database[i].lenght_of_textblok, 0) == 8)
                {
                    sizeLangdb1 += database[i].hz_data.Length;
                    sizeLangdb1 += database[i].lenght_of_animation.Length + database[i].lenght_of_animation.Length;//две длины анимации
                    sizeLangdb1 += database[i].animation.Length;
                    sizeLangdb1 += database[i].lenght_of_waw.Length + database[i].lenght_of_waw.Length;//две длины озвучки
                    sizeLangdb1 += database[i].waw.Length;
                    sizeLangdb1 += database[i].lenght_of_textblok.Length + GetSizeOfByteMassiv(database[i].count_text);// длина текстового блока
                    sizeLangdb1 += database[i].magic_bytes.Length;// длина магических байт   
                }
                else
                {
                    if (BitConverter.ToInt32(database[i].count_text, 0) == 1)
                    {
                        sizeLangdb1 += database[i].hz_data.Length;
                        sizeLangdb1 += database[i].lenght_of_animation.Length + database[i].lenght_of_animation.Length;//две длины анимации
                        sizeLangdb1 += database[i].animation.Length;
                        sizeLangdb1 += database[i].lenght_of_waw.Length + database[i].lenght_of_waw.Length;//две длины озвучки
                        sizeLangdb1 += database[i].waw.Length;
                        sizeLangdb1 += database[i].lenght_of_textblok.Length + GetSizeOfByteMassiv(database[i].count_text);// длина текстового блока
                        sizeLangdb1 += GetSizeOfByteMassiv(database[i].lenght_of_name) + GetSizeOfByteMassiv(database[i].lenght_of_name);//две длины имени
                        sizeLangdb1 += GetSizeOfString(database[i].name);
                        sizeLangdb1 += GetSizeOfByteMassiv(database[i].lenght_of_text) + GetSizeOfByteMassiv(database[i].lenght_of_text);//две длины текста
                        sizeLangdb1 += GetSizeOfString(database[i].text);
                        sizeLangdb1 += database[i].magic_bytes.Length;// длина магических байт   
                    }
                    else if (BitConverter.ToInt32(database[i].count_text, 0) == 2)
                    {
                        sizeLangdb1 += GetSizeOfByteMassiv(database[i].hz_data);
                        sizeLangdb1 += GetSizeOfByteMassiv(database[i].lenght_of_animation) + GetSizeOfByteMassiv(database[i].lenght_of_animation);//две длины анимации
                        sizeLangdb1 += GetSizeOfString(database[i].animation);
                        sizeLangdb1 += GetSizeOfByteMassiv(database[i].lenght_of_waw) + GetSizeOfByteMassiv(database[i].lenght_of_waw);//две длины озвучки
                        sizeLangdb1 += GetSizeOfString(database[i].waw);
                        sizeLangdb1 += GetSizeOfByteMassiv(database[i].lenght_of_textblok) + GetSizeOfByteMassiv(database[i].count_text);// длина текстового блока + инфа о количестве строк
                        sizeLangdb1 += GetSizeOfByteMassiv(database[i].lenght_of_name) + GetSizeOfByteMassiv(database[i].lenght_of_name);//две длины имени
                        sizeLangdb1 += GetSizeOfString(database[i].name);
                        sizeLangdb1 += GetSizeOfByteMassiv(database[i].lenght_of_text) + GetSizeOfByteMassiv(database[i].lenght_of_text);//две длины текста
                        sizeLangdb1 += GetSizeOfString(database[i].text);
                        sizeLangdb1 += GetSizeOfByteMassiv(database[i].magic_bytes);

                        sizeLangdb1 += GetSizeOfByteMassiv(database[i + 1].lenght_of_name) + GetSizeOfByteMassiv(database[i + 1].lenght_of_name);//две длины имени
                        sizeLangdb1 += GetSizeOfString(database[i + 1].name);
                        sizeLangdb1 += GetSizeOfByteMassiv(database[i + 1].lenght_of_text) + GetSizeOfByteMassiv(database[i + 1].lenght_of_text);//две длины текста
                        sizeLangdb1 += GetSizeOfString(database[i + 1].text);
                        sizeLangdb1 += database[i + 1].magic_bytes.Length;// длина магических байт  
                        i++;
                    }
                }
            }



            sizeLangdb1 += first_database[0].end_langdb.Length; //конец langdb

            sizeLangdb2 = sizeLangdb1 - first_database[0].end_langdb.Length - 12;

            // int temp = BitConverter.ToInt32(first_database[0].lenght_of_langdb1, 0);

            //MessageBox.Show(sizeLangdb2.ToString("x"));
            //длина файла найдена!
            byte[] temp = new byte[4];
            //Array.Copy(database[0].hz_data,0, ss, 0, 4);

            //MessageBox.Show("Высчитано: "+sizeLangdb.ToString() +" Оригинал: "+ (first_database[0].langdb.Length+4)+" "+ (BitConverter.ToInt32(ss,0).ToString()));
            //SaveHexInfo(MyFileStream, new byte[4]);
            //SaveHexInfo(MyFileStream, first_database[0].langdb);

            MyFileStream.Write(first_database[0].head, 0, first_database[0].head.Length);
            SaveStringInfo(MyFileStream, first_database[0].name_of_langdb, MainMenu.settings.ASCII_N, false);
            MyFileStream.Write(first_database[0].hlam, 0, first_database[0].hlam.Length);
            //сохраняем всю длинну langdb
            temp = BitConverter.GetBytes(sizeLangdb1);
            MyFileStream.Write(temp, 0, temp.Length);
            MyFileStream.Write(first_database[0].start_langdb, 0, first_database[0].start_langdb.Length);
            //SaveHexInfo(MyFileStream, new byte[4]);//сохранить вторую длину

            ////записываем всё остальное
            while (numb < database.Count)
            {
                //сохраняем хз байты =)
                if (numb == 0)
                {
                    temp = BitConverter.GetBytes(sizeLangdb2);
                    MyFileStream.Write(temp, 0, temp.Length);
                    //MessageBox.Show("2 "+sizeLangdb2.ToString("x"));
                    byte[] hz_temp = new byte[(database[numb].hz_data.Length - 4)];
                    //Array.Copy(database[numb].hz_data,);
                    Array.Copy(database[numb].hz_data, 4, hz_temp, 0, (database[numb].hz_data.Length - 4));
                    MyFileStream.Write(hz_temp, 0, hz_temp.Length);
                }
                else
                {
                    MyFileStream.Write(database[numb].hz_data, 0, database[numb].hz_data.Length);
                }
                //анимация
                SaveStringInfo(MyFileStream, database[numb].animation, MainMenu.settings.ASCII_N, false);
                //озвучка
                SaveStringInfo(MyFileStream, database[numb].waw, MainMenu.settings.ASCII_N, false);

                if (BitConverter.ToInt32(database[numb].lenght_of_textblok, 0) != 8)
                {
                    if (BitConverter.ToInt32(database[numb].count_text, 0) == 1)
                    {
                        temp = BitConverter.GetBytes((database[numb].name.Length + database[numb].text.Length + 8 + 8 + 8 + database[numb].magic_bytes.Length));
                        MyFileStream.Write(temp, 0, temp.Length);

                        MyFileStream.Write(database[numb].count_text, 0, database[numb].count_text.Length);
                        //имя
                        SaveStringInfo(MyFileStream, database[numb].name, MainMenu.settings.ASCII_N, false);
                        //текст
                        SaveStringInfo(MyFileStream, database[numb].text, MainMenu.settings.ASCII_N, false);
                        //магические байты
                        MyFileStream.Write(database[numb].magic_bytes, 0, database[numb].magic_bytes.Length);
                    }
                    else if (BitConverter.ToInt32(database[numb].count_text, 0) == 2)
                    {
                        temp = BitConverter.GetBytes((database[numb].name.Length + database[numb].text.Length + 8 + 8 + 8 + database[numb].magic_bytes.Length + database[numb + 1].name.Length + database[numb + 1].text.Length) + 8 + 8 + database[numb + 1].magic_bytes.Length);
                        MyFileStream.Write(temp, 0, temp.Length);

                        MyFileStream.Write(database[numb].count_text, 0, database[numb].count_text.Length);
                        //имя
                        SaveStringInfo(MyFileStream, database[numb].name, MainMenu.settings.ASCII_N, false);
                        //текст
                        SaveStringInfo(MyFileStream, database[numb].text, MainMenu.settings.ASCII_N, false);
                        //имя
                        //магические байты
                        MyFileStream.Write(database[numb].magic_bytes, 0, database[numb].magic_bytes.Length);
                        SaveStringInfo(MyFileStream, database[numb + 1].name, MainMenu.settings.ASCII_N, false);
                        //текст
                        SaveStringInfo(MyFileStream, database[numb + 1].text, MainMenu.settings.ASCII_N, false);
                        //магические байты
                        MyFileStream.Write(database[numb + 1].magic_bytes, 0, database[numb + 1].magic_bytes.Length);
                        numb++;
                    }
                }
                else
                {
                    MyFileStream.Write(database[numb].lenght_of_textblok, 0, database[numb].lenght_of_textblok.Length);
                    //магические байты
                    MyFileStream.Write(database[numb].magic_bytes, 0, database[numb].magic_bytes.Length);
                }


                //счетчик++
                numb++;
            }

            MyFileStream.Write(first_database[0].end_langdb, 0, first_database[0].end_langdb.Length);
            //temp = BitConverter.GetBytes(83);
            //MyFileStream.Write(temp, 0, temp.Length);
            MyFileStream.Write(first_database[0].other, 0, first_database[0].other.Length);
            //закрываем поток
            MyFileStream.Close();
        }
        public static void CreateLandb(List<byte[]> header, List<Langdb> landb, List<byte[]> end_of_file, string path, string verOfGame)
        {
            //проверяем наличие файла, удаляем его и создаем пустой
            FileStream MyFileStream;
            if (System.IO.File.Exists(path) == true)
            {
                System.IO.File.Delete(path);
            }
            MyFileStream = new FileStream(path, FileMode.OpenOrCreate);
            //записываем заголовок


            string etalon = "5VSM";
            if (ConvertHexToString(header[0], 0, 4) == "5VSM" || ConvertHexToString(header[0], 0, 4) == "6VSM")
            {
                int sizeLangdb = 8;
                for (int i = 0; i < landb.Count; i++)
                {
                    sizeLangdb += landb[i].hz_data.Length;
                    sizeLangdb += landb[i].lenght_of_textblok.Length;// длина текстового блока
                    sizeLangdb += GetSizeOfByteMassiv(landb[i].lenght_of_name) + GetSizeOfByteMassiv(landb[i].lenght_of_name);//две длины имени
                    sizeLangdb += BitConverter.ToInt32(landb[i].lenght_of_name, 0);//GetSizeOfString(landb[i].name);
                    sizeLangdb += GetSizeOfByteMassiv(landb[i].lenght_of_text) + GetSizeOfByteMassiv(landb[i].lenght_of_text);//две длины текста
                    sizeLangdb += BitConverter.ToInt32(landb[i].lenght_of_text, 0);//GetSizeOfString(landb[i].text);
                    sizeLangdb += landb[i].magic_bytes.Length;// длина магических байт   
                }
                sizeLangdb += end_of_file[0].Length + 16;
                byte[] hex_size = BitConverter.GetBytes(sizeLangdb);
                header[1] = hex_size;

                sizeLangdb += -end_of_file[0].Length - 16;
                hex_size = BitConverter.GetBytes(sizeLangdb);
                header[10] = hex_size;
                for (int i = 0; i < header.Count; i++)
                {
                    MyFileStream.Write(header[i], 0, header[i].Length);
                }
                int numb = 0;
                ////записываем всё остальное
                while (numb < landb.Count)
                {

                    MyFileStream.Write(landb[numb].hz_data, 0, landb[numb].hz_data.Length);

                    //byte[] temp = BitConverter.GetBytes((landb[numb].name.Length + landb[numb].text.Length + 8 + 8 + landb[numb].magic_bytes.Length) - 8);//-8 надо для борды, указывается всегда 12 хотя в Борде уже 20?

                    byte[] temp = BitConverter.GetBytes((BitConverter.ToInt32(landb[numb].lenght_of_name, 0) + BitConverter.ToInt32(landb[numb].lenght_of_text, 0) + 8 + 8 + landb[numb].magic_bytes.Length) - 8);//-8 надо для борды, указывается всегда 12 хотя в Борде уже 20?
                    if (verOfGame == "WAU") temp = BitConverter.GetBytes((BitConverter.ToInt32(landb[numb].lenght_of_name, 0) + BitConverter.ToInt32(landb[numb].lenght_of_text, 0) + 8 + 8 + landb[numb].magic_bytes.Length));//-8 надо для борды, указывается всегда 12 хотя в Борде уже 20?
                    //if ()
                    MyFileStream.Write(temp, 0, temp.Length);
                    if ((verOfGame == "TFTB") && MainMenu.settings.unicodeSettings == 0 || (verOfGame == "Batman"))
                    {
                        //имя
                        SaveStringInfo(MyFileStream, landb[numb].name, MainMenu.settings.ASCII_N, true);

                        //текст
                        if (landb[numb].text.IndexOf("\0") > 0)
                        {
                            SaveStringInfo(MyFileStream, landb[numb].text, MainMenu.settings.ASCII_N, false);
                        }
                        else SaveStringInfo(MyFileStream, landb[numb].text, MainMenu.settings.ASCII_N, true);
                    }
                    else
                    {
                        //имя
                        SaveStringInfo(MyFileStream, landb[numb].name, MainMenu.settings.ASCII_N, false);
                        //текст
                        SaveStringInfo(MyFileStream, landb[numb].text, MainMenu.settings.ASCII_N, false);
                    }

                    //магические байты
                    MyFileStream.Write(landb[numb].magic_bytes, 0, landb[numb].magic_bytes.Length);
                    //счетчик++
                    numb++;
                }
                for (int i = 0; i < end_of_file.Count; i++)
                {
                    MyFileStream.Write(end_of_file[i], 0, end_of_file[i].Length);
                }
                //закрываем поток
                MyFileStream.Close();
            }
            else
            {
                for (int i = 0; i < header.Count; i++)
                {
                    MyFileStream.Write(header[i], 0, header[i].Length);
                }
                int sizeLangdb = 8;
                for (int i = 0; i < landb.Count; i++)
                {

                    sizeLangdb += landb[i].hz_data.Length;
                    sizeLangdb += landb[i].lenght_of_animation.Length + landb[i].lenght_of_animation.Length;//две длины анимации
                    sizeLangdb += landb[i].animation.Length;
                    sizeLangdb += landb[i].lenght_of_waw.Length + landb[i].lenght_of_waw.Length;//две длины озвучки
                    sizeLangdb += landb[i].waw.Length;
                    sizeLangdb += landb[i].count_text.Length;
                    sizeLangdb += landb[i].lenght_of_textblok.Length;// длина текстового блока
                    sizeLangdb += GetSizeOfByteMassiv(landb[i].lenght_of_name) + GetSizeOfByteMassiv(landb[i].lenght_of_name);//две длины имени
                    sizeLangdb += GetSizeOfString(landb[i].name);
                    sizeLangdb += GetSizeOfByteMassiv(landb[i].lenght_of_text) + GetSizeOfByteMassiv(landb[i].lenght_of_text);//две длины текста
                    sizeLangdb += GetSizeOfString(landb[i].text);
                    sizeLangdb += landb[i].magic_bytes.Length;// длина магических байт   
                }
                byte[] hex_size = BitConverter.GetBytes(sizeLangdb);
                MyFileStream.Write(hex_size, 0, hex_size.Length);


                int numb = 0;
                ////записываем всё остальное
                while (numb < landb.Count)
                {

                    MyFileStream.Write(landb[numb].hz_data, 0, landb[numb].hz_data.Length);
                    //анимация
                    SaveStringInfo(MyFileStream, landb[numb].animation, MainMenu.settings.ASCII_N, false);
                    //озвучка
                    SaveStringInfo(MyFileStream, landb[numb].waw, MainMenu.settings.ASCII_N, false);

                    MyFileStream.Write(landb[numb].count_text, 0, landb[numb].count_text.Length);

                    byte[] temp = BitConverter.GetBytes((landb[numb].name.Length + landb[numb].text.Length + 4 + 8 + 8 + landb[numb].magic_bytes.Length));
                    MyFileStream.Write(temp, 0, temp.Length);

                    //имя
                    SaveStringInfo(MyFileStream, landb[numb].name, MainMenu.settings.ASCII_N, false);
                    //текст
                    SaveStringInfo(MyFileStream, landb[numb].text, MainMenu.settings.ASCII_N, false);

                    //магические байты
                    MyFileStream.Write(landb[numb].magic_bytes, 0, landb[numb].magic_bytes.Length);
                    //счетчик++
                    numb++;
                }
                for (int i = 0; i < end_of_file.Count; i++)
                {
                    MyFileStream.Write(end_of_file[i], 0, end_of_file[i].Length);
                }
            }
            //закрываем поток
            MyFileStream.Close();
        }



        public static Int32 GetSizeOfByteMassiv(byte[] str)
        {
            try
            {
                return str.Length;
            }
            catch
            {
                return 0;
            }
        }
        public static Int32 GetSizeOfString(string str)
        {
            try
            {
                return str.Length;
            }
            catch
            {
                return 0;
            }
        }
        public static void SaveStringInfo(FileStream MyFileStream, string data, int ASCII_N, bool Unicode)
        {
            byte[] b1 = BitConverter.GetBytes(data.Length + 8);
            byte[] b2 = BitConverter.GetBytes(data.Length);

            if (Unicode)
            {
                byte[] bin_data = Encoding.UTF8.GetBytes(data);
                b1 = BitConverter.GetBytes(bin_data.Length + 8);
                b2 = BitConverter.GetBytes(bin_data.Length);
            }

            MyFileStream.Write(b1, 0, b1.Length);
            MyFileStream.Write(b2, 0, b2.Length);
            if (data.Length > 0)
            {
                byte[] hex_data = new byte[data.Length];
                //
                if (Unicode == true) hex_data = (byte[])Encoding.UTF8.GetBytes(data);
                else hex_data = (byte[])ASCIIEncoding.GetEncoding(ASCII_N).GetBytes(data);
                MyFileStream.Write(hex_data, 0, hex_data.Length);
            }
        }
        public static void SaveStringInfoForProp(FileStream MyFileStream, string data, int ASCII_N)
        {
            byte[] b = BitConverter.GetBytes(data.Length);
            MyFileStream.Write(b, 0, b.Length);
            if (data.Length > 0)
            {
                byte[] hex_data = new byte[data.Length];
                hex_data = (byte[])ASCIIEncoding.GetEncoding(ASCII_N).GetBytes(data);
                MyFileStream.Write(hex_data, 0, hex_data.Length);
            }
        }

        public static string CheckVersionOfGameFromCombobox(int selectedIndexFromComboBox1)
        {
            switch (selectedIndexFromComboBox1)
            {
                case -1:
                    {
                        return " ";
                    }
                case 0:
                    {
                        return " ";
                    }
                case 1:
                    {
                        return "12 ";
                    }
                case 2:
                    {
                        return "13 ";
                    }
                case 3:
                    {
                        return "14 ";
                    }
                case 4:
                    {
                        return "15 ";
                    }
                case 5:
                    {
                        return "16 ";
                    }
                case 6:
                    {
                        return "WAU";
                    }
                case 7:
                    {
                        return "TFTB";
                    }
                default:
                    {
                        MessageBox.Show("Ошибка в выборе версии игры!", "Ошибка!");
                        return null;
                    }

            }
        }




        private void AutoPacker_Load(object sender, EventArgs e)
        {

            #region Грузим список blowfish ключей

            comboBox1.Items.Clear();

            for (int i = 0; i < MainMenu.gamelist.Count; i++)
            {
                comboBox1.Items.Add(i + " " + MainMenu.gamelist[i].gamename);
            }

            #endregion

            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 1; //Для работы с ttarch2 архивами
            if (MainMenu.settings.unicodeSettings == 0) checkUnicode.Checked = true;
            txtFilesRB.Checked = true;
            
        }

        private void AutoPacker_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
