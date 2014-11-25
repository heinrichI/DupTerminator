using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.IO;
using System.Drawing;

namespace DupTerminator
{
    /// <summary>
    /// Класс определяющий какие настройки есть в программе
    /// </summary>
    public class SettingsAppFields
    {
        //Путь до файла настроек
        //Чтобы добавить настройку в программу просто добавьте суда строку вида -
        //public ТИП ИМЯ_ПЕРЕМЕННОЙ = значение_переменной_по_умолчанию;
        /*public String TextValue = @"File Settings";
        public DateTime DateValue = new DateTime(2011, 1, 1);
        public Decimal DecimalValue = 555;*/

        //public FormWindowState Win_State = FormWindowState.Normal;
        //public int Win_Left = 0;
        //public int Win_Top = 0;
        //public int splitDistance = 150;

        public Boolean IsConfirmDelete = false;
        public Boolean IsCheckUpdate = true;
        public Boolean IsOrientationVert = true;
        public Boolean IsSameFileName = false;
        public Boolean IsSaveLoadListDub = true;
        public Boolean IsCheckNonExistentOnLoad = false;
        public Boolean IsAllowDelAllFiles = false;
        public Boolean IsDontUpdateSize = false;
        public bool IsScanMax = true;
        public int MaxFile = 250000;
        public int PathHistoryLength = 20;
        public List<string> PathHistory;
        public long[] limits = { 0, long.MaxValue }; //Min Max file size
        //public String FolderToMove = String.Empty;
        public String IncludePattern = String.Empty;
        public String ExcludePattern = String.Empty;
        public SerializableFont ProgramFont = new SerializableFont(Form.DefaultFont);
        public SerializableFont ListRowFont = new SerializableFont(Form.DefaultFont);
        public SerializableColor ColorRow1 = SerializableColor.FromColor(Color.White);
        public SerializableColor ColorRow2 = SerializableColor.FromRGB(187, 221, 255);
        public SerializableColor ColorRowError = SerializableColor.FromColor(Color.Red);
        public SerializableColor ColorRowNotExist = SerializableColor.FromColor(Color.LightGray);//*/
        public String Language = String.Empty;
        public String LastJob = Const.defaultDirectory;
        public Boolean FastCheck = true;
        public uint FastCheckFileSizeMb = 5;
        public uint FastCheckBufferKb = 1; 
        public Boolean UseDB = true;
    }

    //[Serializable]
    public class SerializableColor
    {
        public int RGBColor { get; set; }

        /// <summary>
        /// Intended for xml serialization purposes only
        /// </summary>
        private SerializableColor() { }

        public SerializableColor(Color c)
        {
            RGBColor = c.ToArgb();
        }

        public static SerializableColor FromRGB(int red, int green, int blue)
        {
            return new SerializableColor(Color.FromArgb(red, green, blue));
        }
       
        public static SerializableColor FromColor(Color c)
        {
            return new SerializableColor(c);
        }

        public Color ToColor()
        {
            return Color.FromArgb(RGBColor);
        }
    }

    /// <summary>
    /// Font descriptor, that can be xml-serialized
    /// </summary>
    public class SerializableFont
    {
        public string FontFamily { get; set; }
        public GraphicsUnit GraphicsUnit { get; set; }
        public float Size { get; set; }
        public FontStyle Style { get; set; }

        /// <summary>
        /// Intended for xml serialization purposes only
        /// </summary>
        private SerializableFont() { }

        public SerializableFont(Font f)
        {
            FontFamily = f.FontFamily.Name;
            GraphicsUnit = f.Unit;
            Size = f.Size;
            Style = f.Style;
        }

        public static SerializableFont FromFont(Font f)
        {
            return new SerializableFont(f);
        }

        public Font ToFont()
        {
            return new Font(FontFamily, Size, Style,
                GraphicsUnit);
        }
    }

    /// <summary>
    /// Класс работы с настройками
    /// </summary>
    public class SettingsApp
    {
        public SettingsAppFields Fields;
        private String XMLFilePath = Path.Combine(Environment.CurrentDirectory, "settings.xml");

        public SettingsApp()
        {
            Fields = new SettingsAppFields();
        }

        //Запись настроек в файл
        public void WriteXml()
        {
            /*XmlSerializer ser = new XmlSerializer(typeof(SettingsAppFields));
            TextWriter writer = new StreamWriter(Fields.XMLFilePath, false); //перезапись
            ser.Serialize(writer, Fields);
            writer.Close();//*/
            /*using (TextWriter writer = new StreamWriter(Fields.XMLFilePath, false))
            //using (System.Xml.XmlWriter writer = new System.Xml.XmlWriter(Fields.XMLFilePath, false))
            {
                ser.Serialize(writer, Fields);
                //writer.Flush();
                writer.Close();
            }*/
            /*XmlSerializer ser = new XmlSerializer(typeof(SettingsAppFields));
            using (FileStream fs = new FileStream(XMLFilePath, FileMode.Create))
            {
                using (TextWriter writer = new StreamWriter(fs))
                {
                    ser.Serialize(writer, Fields);
                }
            }*/
            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(SettingsAppFields));
                using (TextWriter writer = new StreamWriter(XMLFilePath, false))
                {
                    ser.Serialize(writer, Fields);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + Environment.NewLine + ((System.InvalidOperationException)ex.InnerException).InnerException.ToString());
            }//*/
        }

        //Чтение настроек из файла
        public void ReadXml()
        {
            if (File.Exists(XMLFilePath))
            {
                try
                {
                    /*XmlSerializer ser = new XmlSerializer(typeof(SettingsAppFields));
                    TextReader reader = new StreamReader(Fields.XMLFilePath);
                    Fields = ser.Deserialize(reader) as SettingsAppFields;
                    reader.Close();*/
                    XmlSerializer ser = new XmlSerializer(typeof(SettingsAppFields));
                    using (TextReader reader = new StreamReader(XMLFilePath))
                    {
                        Fields = ser.Deserialize(reader) as SettingsAppFields;
                    }
                }
                catch
                {
                    MessageBox.Show("Format settings.xml not match with exist");
                    //MessageBox.Show("Формат settings.xml не сопадает с существующим");
                }//*/
                /*XmlSerializer ser = new XmlSerializer(typeof(SettingsAppFields));
                using (FileStream fs = new FileStream(XMLFilePath, FileMode.Open))
                {
                    using (TextReader reader = new StreamReader(fs))
                    {
                        Fields = ser.Deserialize(reader) as SettingsAppFields;
                    }
                }*/
            }
            /*else
            {
                //можно написать вывод какова то сообщения если файла не существует
                MessageBox.Show(Fields.XMLFilePath + " не существует!");
            }*/
        }
    }
}
