using Dapper;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

internal class Program
{
    private static void Main(string[] args)
    {
        try
        {
            string[] lines = System.IO.File.ReadAllLines(@"MatchingNozzelsConfig.txt");
            string xmlFilePath = lines[0];
            string queryString = lines[1];
            string server = lines[2];
            string database = lines[3];
            string username = lines[4];
            string password = lines[5];

            string connectinoString = "" + server + ";" + database + ";" + username + ";"+ password + "";
            
            List<string> xmlNosselsID = getNozzelFromXML(xmlFilePath);
            List<string> dbNosselsID = loadNozzelsDB(connectinoString, queryString);
            
            if(xmlNosselsID.Count < dbNosselsID.Count)
            {
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Console.WriteLine("DB has more values than XML");
                    wirteLog("DB has more values than XML", w);
                }
            }

            if(xmlNosselsID.Count > dbNosselsID.Count)
            {
                var DBNotXML = xmlNosselsID.Except(dbNosselsID).ToList();
                removeFromXML(DBNotXML, xmlFilePath);
                Console.WriteLine(DateTime.Now + "   : " + "The XML File has been corrected");
            }

            if (xmlNosselsID.Count == dbNosselsID.Count)
            {
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    wirteLog("The XML and Database are the same values", w);
                    Console.WriteLine("The XML and Database are the same values");
                }
            }
            
            //restartFHSservice();
        }
        catch (Exception ex)
        {
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Console.WriteLine(ex.Message);
                wirteLog(ex.Message, w);
            }
        }
       
        Console.ReadKey();
    }

    private static List<string> getNozzelFromXML(string xmlFilePath)
    {
        try
        {
            List<string> xmlNozzelID = new List<string>();
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlFilePath);
            XmlNodeList xnList = xml.SelectNodes("/Nozzles/Nozzle[@SerialNumber]");
            foreach (XmlNode noz in xnList)
            {
                xmlNozzelID.Add(noz.Attributes["SerialNumber"].Value.ToString());
            }
            return xmlNozzelID;
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                wirteLog("et Data From XML Successed", w);
            }
        }
        catch (Exception ex)
        {
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                wirteLog("Getting XML Data" + ex.Message, w);
            }
            throw;
        }
    }

    private static List<string> loadNozzelsDB(string connectionString, string queryString)
    {
        List<string> result = new List<string>();
        try
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    result = connection.Query<string>(queryString).ToList();
                    connection.Close();
                }
                catch(Exception ex)
                {
                    using (StreamWriter w = File.AppendText("log.txt"))
                    {
                        Console.WriteLine(ex.Message);
                        wirteLog("Getting Database Nozzels :" + ex.Message, w);
                        throw;
                    }
                }
            }
            
        }
        catch (Exception ex)
        {
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Console.WriteLine(ex.Message);
                wirteLog("Getting Database Nozzels :" + ex.Message, w);
                throw;
            }
        }
        return result;
    }

    private static void removeFromXML(List<string> list, string xmlPath)
    {
        try
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(xmlPath);  // 

            foreach (var item in list)
            {
                string nodePath = "/Nozzles/Nozzle[@SerialNumber='" + item + "']";
                XmlNode node = xml.SelectSingleNode(nodePath);

                // if found....
                if (node != null)
                {
                    // get its parent node
                    XmlNode parent = node.ParentNode;

                    // remove the child node
                    parent.RemoveChild(node);

                    // verify the new XML structure
                    string newXML = xml.OuterXml;

                    // save to file or whatever....
                    xml.Save(xmlPath);
                    
                }
            }
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                wirteLog(" Remove Difference from XML Successed", w);
            }
        }
        catch (Exception ex)
        {
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                wirteLog("Remove from XML"+ex.Message +"", w);
            }
            throw;
        }
       
    }

    // Restart FHS Service
    public static void restartFHSservice()
    {
        try
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd";
            process.StartInfo.Arguments = "/c net stop \"FHSService\" & net start \"FHSService\"";
            process.Start();
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                wirteLog("Process FHSService has restarted Successfuly", w);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                wirteLog("Restart FHS Serverc: " + ex.Message, w);
            }
           
        }
    }


    public static void wirteLog(string logMessage, TextWriter w)
    {
        w.WriteLine($"{DateTime.Now}" + " : "+logMessage);
        w.WriteLine("-------------------------------");
    }
}