using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Threading;

namespace SiteChecker
{
    class CheckStarter
    {

        public static IConfigurationRoot configuration  = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

        static string dir = configuration["output_directory"];
        static int seconds = configuration.GetSection("wait_seconds").Get<int>();
        static string[] websites = configuration.GetSection("websites").Get<string[]>();
        static string[] labels = configuration.GetSection("labels").Get<string[]>();
        static string[] sounds = configuration.GetSection("alert_sound").Get<string[]>();
        static string[] alarms = configuration.GetSection("alarm").Get<string[]>();
        static string[] exclusions = configuration.GetSection("string_exclusions").Get<string[]>();
        static string[] inclusions = configuration.GetSection("string_inclusions").Get<string[]>();
        static int alarmTime = configuration.GetSection("alarm_seconds").Get<int>();
        static StreamWriter w = File.AppendText("D:\\sitecheck_log.txt");
        //static StreamReader r = File.OpenText("D:\\sitecheck_log.txt");

        static void Main(string[] args)
        {
            string[,] conArr = new string[11, 1];
            conArr = new string[5, 1];
            while (true)
            {
                try
                {
                    checkSites();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        static void checkSites()
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

                        string[] removals = exclusions[idx].Split(new string[] { " , " }, System.StringSplitOptions.None);
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
                                        if (startStringToRemove == "hostname\":")
                                        {
                                            string test = "";
                                        }
                                        removeStart = data.IndexOf(startStringToRemove);
                                        removeEnd = -1;
                                        if (removeStart > -1)
                                        {
                                            removeEnd = data.IndexOf(endStringToRemove, removeStart) + endStringToRemove.Length;
                                        }                                        
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
                            if (adds[j] != "")
                            {
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

                        Console.WriteLine(data.Substring(0, 20) + "...");

                        bool sameHtml = currText == data;
                        Console.WriteLine("Same HTML as previous check? : " + sameHtml);
                        System.Media.SoundPlayer player = new System.Media.SoundPlayer(sounds[idx]);
                        if (!sameHtml)
                        {
                            if (alarms[idx] != "")
                            {
                                RingAlarm(idx);
                                //player.SoundLocation = alarms[idx];
                                //player.PlayLooping();
                            }
                            else
                            {
                                player.Play();
                            }
                            File.WriteAllText(path, data);
                            File.WriteAllText(pathPrev, currText);
                            Log("Update for "+ labels[idx], w);
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
                    Log("Error accessing " + labels[idx] + "; " + ex.Message + ex.StackTrace, w);
                }
            }
            System.Threading.Thread.Sleep(seconds * 1000);
        }

        static void RingAlarm(int idx)
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer(sounds[idx]);
            player.SoundLocation = alarms[idx];
            player.PlayLooping();
            try
            {
                Reader.ReadLine(alarmTime*1000);
            }
            catch(TimeoutException)
            {
                Console.WriteLine("Alarm timeout");
            }
            //Console.ReadLine();
            player.Stop();
        }

        public static void Log(string logMessage, TextWriter w)
        {
            w.Write("\r\nLog Entry : ");
            w.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
            w.WriteLine("  :");
            w.WriteLine($"  :{logMessage}");
            w.WriteLine("-------------------------------");
            //DumpLog(r);
        }

        public static void DumpLog(StreamReader r)
        {
            string line;
            while ((line = r.ReadLine()) != null)
            {
                Console.WriteLine(line);
            }
        }
    }

    class Reader
    {
        private static Thread inputThread;
        private static AutoResetEvent getInput, gotInput;
        private static string input;

        static Reader()
        {
            getInput = new AutoResetEvent(false);
            gotInput = new AutoResetEvent(false);
            inputThread = new Thread(reader);
            inputThread.IsBackground = true;
            inputThread.Start();
        }

        private static void reader()
        {
            while (true)
            {
                getInput.WaitOne();
                input = Console.ReadLine();
                gotInput.Set();
            }
        }


        // omit the parameter to read a line without a timeout
        public static string ReadLine(int timeOutMillisecs = Timeout.Infinite)
        {
            getInput.Set();
            bool success = gotInput.WaitOne(timeOutMillisecs);
            if (success)
                return input;
            else
                throw new TimeoutException("User did not provide input within the timelimit.");
        }
    }
}
