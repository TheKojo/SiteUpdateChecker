using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace SiteChecker
{
    class CheckStarter
    {

        public static IConfigurationRoot configuration;


        static void Main(string[] args)
        {
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            string dir = configuration["output_directory"];
            int seconds = configuration.GetSection("wait_seconds").Get<int>();
            var websites = configuration.GetSection("websites").Get<string[]>();
            var labels = configuration.GetSection("labels").Get<string[]>();
            var sounds = configuration.GetSection("alert_sound").Get<string[]>();
            var exclusions = configuration.GetSection("string_exclusions").Get<string[]>();
            var inclusions = configuration.GetSection("string_inclusions").Get<string[]>();
            //string[,] deserialized = JsonConvert.DeserializeObject<string[,]>(configuration["string_exclusions"]);

            while (true)
            {
                for (int idx = 0; idx < websites.Length; idx++)
                {
                    string path = dir + labels[idx] + ".txt";
                    string pathPrev = dir + labels[idx] + "_prev.txt";
                    string urlAddress = websites[idx];
                    string currText = "";
                    string inbetweenIndicator = " ... ";
                    if (File.Exists(path))
                    {
                         currText = File.ReadAllText(path);
                    }
                    Console.WriteLine(DateTime.Now.ToString());
                    Console.WriteLine("Accessing URL: " + urlAddress);

                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(urlAddress);
                        request.UseDefaultCredentials = true;
                        request.UserAgent = configuration["user_agent"];
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            Stream receiveStream = response.GetResponseStream();
                            StreamReader readStream = null;
                            if (response.CharacterSet == null)
                            {
                                readStream = new StreamReader(receiveStream);
                            }
                            else
                            {
                                readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                            }
                            string data = readStream.ReadToEnd();
                            /*string[,] removals = new string[,] { { "<script id=\"__st\">", "<script>window.ShopifyPaypal" },
                                                         { "Monorail.produce", "var loaded" },
                                                         { "var HE_DOMAIN", "sswLangs.data" }
                                                       };*/

                            string[] removals = exclusions[idx].Split(new string[] {" , "}, System.StringSplitOptions.None);
                            for (int j = 0; j < removals.Length; j++)
                            {
                                if (removals[j] != "")
                                {
                                    int inbetweenLoc = removals[j].IndexOf(inbetweenIndicator);
                                    if (inbetweenLoc != -1)
                                    {
                                        string startStringToRemove = removals[j].Substring(0, inbetweenLoc);
                                        string endStringToRemove = removals[j].Substring(inbetweenLoc + inbetweenIndicator.Length);
                                        int removeStart = 0;
                                        int removeEnd = 0;
                                        int removeLength = 0;
                                        do
                                        {
                                            if (startStringToRemove == "<div id=\"TopPanelDF\"><style>")
                                            {
                                                string test = "";
                                            }
                                            removeStart = data.IndexOf(startStringToRemove);
                                            removeEnd = data.IndexOf(endStringToRemove)+ endStringToRemove.Length;
                                            removeLength = removeEnd - removeStart;
                                            if (removeStart != -1 && removeEnd != -1 && removeLength > 0)
                                            {
                                                //Console.Write(removeLength);
                                                data = data.Remove(removeStart, removeLength);
                                            }
                                        } while (removeStart != -1 && removeEnd != -1 && removeLength > 0);

                                    }
                                    else
                                    {
                                        string stringToRemove = removals[j];
                                        data = data.Replace(stringToRemove, "");
                                    }

                                }
                            }

                            string[] adds = inclusions[idx].Split(new string[] { " , " }, System.StringSplitOptions.None);
                            for (int j = 0; j < adds.Length; j++)
                            {
                                if (adds[j] != ""){
                                    int inbetweenLoc = adds[j].IndexOf(inbetweenIndicator);
                                    if (inbetweenLoc != -1)
                                    {
                                        string startStringToRemove = adds[j].Substring(0, inbetweenLoc);
                                        string endStringToRemove = adds[j].Substring(inbetweenLoc + inbetweenIndicator.Length);
                                        int searchStart = data.IndexOf(startStringToRemove);
                                        int searchEnd = data.IndexOf(endStringToRemove, searchStart);
                                        int searchLength = searchEnd - searchStart;
                                        if (searchStart != -1 && searchEnd != -1 && searchLength > 0)
                                        {
                                            data = data.Substring(searchStart, searchLength);
                                        }
                                    }
                                }
                            }
                            /*for (int j = 0; j < removals.GetLength(0); j++)
                            {
                                int removeStart = data.IndexOf(removals[j, 0]);
                                int removeEnd = data.IndexOf(removals[j, 1]);
                                int removeLength = removeEnd - removeStart;
                                if (removeStart >= 0 && removeEnd >= 0)
                                {
                                    data = data.Remove(removeStart, removeLength);
                                }
                                
                            }*/

                            Console.WriteLine(data.Substring(0, 20) + "...");

                            bool sameHtml = currText == data;
                            Console.WriteLine("Same HTML as previous check? : " + sameHtml);
                            System.Media.SoundPlayer player = new System.Media.SoundPlayer(sounds[idx]);
                            if (!sameHtml)
                            {
                                player.Play();
                                File.WriteAllText(path, data);
                                File.WriteAllText(pathPrev, currText);
                            }
                            response.Close();
                            readStream.Close();
                        }
                        Console.WriteLine("\n\n======================\n\n");

                    }
                    catch (Exception ex)
                    {
                        System.Media.SoundPlayer playerErr = new System.Media.SoundPlayer(configuration["error_sound"]);
                        //playerErr.Play();
                        Console.WriteLine("!!!!!!!!!! ERROR !!!!!!!!!!");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                        Console.WriteLine("\n\n======================\n\n");
                    }
                }
                System.Threading.Thread.Sleep(seconds * 1000);
            }

        }
    }
}
