using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Threading;
using System.Text.RegularExpressions;

namespace SiteChecker
{

    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using System.Threading;
    using System.Collections.Generic;

    namespace SiteChecker
    {
        class CheckStarter
        {

            public static IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                    .AddJsonFile("appsettings.json", false)
                    .Build();

            static string dir = configuration["output_directory"];
            static int seconds = configuration.GetSection("wait_seconds").Get<int>();
            static string[] configWebsites = configuration.GetSection("websites").Get<string[]>();
            static string[] configLabels = configuration.GetSection("labels").Get<string[]>();
            static string[] configSounds = configuration.GetSection("alert_sound").Get<string[]>();
            static string[] configAlarms = configuration.GetSection("alarm").Get<string[]>();
            static int alarmTime = configuration.GetSection("alarm_seconds").Get<int>();
            static StreamWriter w = File.AppendText("D:\\sitecheck_log.txt");
            //static StreamReader r = File.OpenText("D:\\sitecheck_log.txt");
            static List<Website> websites = new List<Website>();
            static HashSet<string> TagsWithNoClosing = new HashSet<string>() { "br", "meta", "link", "input", "!--", "img"};
            static HashSet<string> TagsToSkip = new HashSet<string>() { "script" };

            static void Main(string[] args)
            {
                string[,] conArr = new string[11, 1];
                conArr = new string[5, 1];
                while (true)
                {
                    //try
                    //{
                        initWebsites();
                        checkSites();
                        System.Threading.Thread.Sleep(seconds * 1000);
                    /*}
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }*/
                }
            }

            static void initWebsites()
            {
                for (int idx = 0; idx < configWebsites.Length; idx++)
                {
                    Website site = new Website(configWebsites[idx], "", configSounds[idx]);
                    websites.Add(site);
                }
            }

            static void checkSites()
            {
                foreach (Website site in websites)
                {
                    Console.WriteLine(DateTime.Now.ToString());
                    Console.WriteLine("Accessing URL: " + site.Url);
                    //try
                    //{
                        //Setup request
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(site.Url);
                        request.UseDefaultCredentials = true;
                        request.UserAgent = configuration["user_agent"];

                        //Setup cookies
                        request.CookieContainer = new CookieContainer();
                        string CookieStr = configuration["cookie"];
                        CookieContainer cookiecontainer = new CookieContainer();
                        string[] cookies = CookieStr.Split(';');
                        foreach (string cookie in cookies)
                            cookiecontainer.SetCookies(new Uri(site.Url), cookie);
                        request.CookieContainer = cookiecontainer;
                        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7";

                        //Send request
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                        //Process response
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
                            data = trimUntil(data, "<html");
                            HTMLElement htmlNode = new HTMLElement();
                            htmlNode.TagType = "html";
                            HTMLElement htmlData = parseHTMLData(new HTMLData(data));
                            var test = 1;
                        }
                    //}
                    /*catch (Exception ex)
                    {
                        System.Media.SoundPlayer playerErr = new System.Media.SoundPlayer(configuration["error_sound"]);
                        playerErr.Play();
                        Console.WriteLine("!!!!!!!!!! ERROR !!!!!!!!!!");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                        Console.WriteLine("\n\n======================\n\n");
                        Log("Error accessing " + site.Name + "; " + ex.Message + ex.StackTrace, w);
                    }*/
                }
            }

            static string trimUntil(string baseStr, string strToFind)
            {
                int idx = baseStr.IndexOf(strToFind);
                if (idx >= 0)
                {
                    return baseStr.Substring(1).Substring(idx);
                }
                else
                {
                    return baseStr;
                }
                
            }

            static HTMLElement parseHTMLData(HTMLData htmlData)
            {
                HTMLElement currentNode = new HTMLElement();
                string tagName = parseTagName(htmlData.Data);
                string className = parseClassName(htmlData.Data);
                currentNode.TagType = tagName;
                currentNode.Class = className;
                Console.WriteLine(currentNode.TagType);
                if (tagName == "html") {
                    var y = 1;
                }
                if (tagName == "")
                {
                    var y = 1;
                }

                //No expected closing tag and therefore has no children, return node and continue
                if (TagsWithNoClosing.Contains(currentNode.TagType))
                {
                    return currentNode;
                }

                //Don't need to process; skip to next tag
                if (TagsToSkip.Contains(currentNode.TagType))
                {
                    htmlData.Data = trimUntil(htmlData.Data, "</"+ currentNode.TagType);
                    return currentNode;
                }

                //Reached closing tag, return
                if (htmlData.Data.IndexOf("/" + currentNode.TagType + ">") == 0)
                {
                    return currentNode;
                }

                htmlData.Data = trimUntil(htmlData.Data, "<");
                while (htmlData.Data.IndexOf("/" + currentNode.TagType + ">") != 0)
                {
                    //Recursively add children to current element
                    currentNode.Children.Add(parseHTMLData(htmlData));
                    htmlData.Data = trimUntil(htmlData.Data, "<");
                }
                return currentNode;
            }

            static string parseTagName(string data)
            {
                string ret = "";
                for (int i = 0; i < data.Length; i++)
                {
                    char currChar = data[i];
                    if (currChar == ' ' || currChar == '>')
                    {
                        break;
                    }
                    if (currChar != '/')
                    {
                        ret += data[i];
                    }
                }
                return ret;
            }

            static string parseClassName(string data)
            {
                string ret = "";
                var classLocation = Regex.Match(data, " class[ \t]*=[ \t]*['\"]");
                int tagEndLocation = data.IndexOf('>');

                //Element doesn't have a class; return
                if (tagEndLocation < classLocation.Index || classLocation.Value == "")
                {
                    return ret;
                }
                
                //Build class name until next quote
                for (int i = classLocation.Index + classLocation.Value.Length + 1; i < data.Length; i++)
                {
                    char currChar = data[i];
                    if (currChar == '"' || currChar == '\'')
                    {
                        break;
                    }
                    ret += data[i];
                }
                return ret;
            }

            static void RingAlarm(int idx)
            {
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(configSounds[idx]);
                player.SoundLocation = configAlarms[idx];
                player.PlayLooping();
                try
                {
                    Reader.ReadLine(alarmTime * 1000);
                }
                catch (TimeoutException)
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


    /*    class CheckStarter
        {

            public static IConfigurationRoot configuration = new ConfigurationBuilder()
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

            static bool countMode = false;
            static string stringToCount = "Sold-out";
            static bool useAlarm = true;

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
                    catch (Exception ex)
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
                    string currOccurrences = "";
                    string inbetweenIndicator = " ... ";
                    if (File.Exists(path))
                    {
                        currText = File.ReadAllText(path);
                        currOccurrences = currText;
                    }
                    Console.WriteLine(DateTime.Now.ToString());
                    Console.WriteLine("Accessing URL: " + urlAddress);

                    try
                    {

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
                            if (countMode)
                            {
                                string numOccurrences = Regex.Matches(data, stringToCount).Count+"";
                                bool sameHtml = currOccurrences == numOccurrences;
                                Console.WriteLine("Same HTML as previous check? : " + sameHtml + ", counted occurrences is: "+ numOccurrences);
                                System.Media.SoundPlayer player = new System.Media.SoundPlayer(sounds[idx]);
                                if (!sameHtml)
                                {
                                    if (useAlarm)
                                    {
                                        RingAlarm(idx);
                                        //player.SoundLocation = alarms[idx];
                                        //player.PlayLooping();
                                    }
                                    else
                                    {
                                        player.Play();
                                    }
                                    File.WriteAllText(path, numOccurrences);
                                    File.WriteAllText(pathPrev, currOccurrences);
                                    Log("Update for " + labels[idx], w);
                                }
                            }
                            else
                            {
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
                                //Console.WriteLine(data.Substring(0, 20) + "...");

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
                                    Log("Update for " + labels[idx], w);
                                }

                            }
                            response.Close();
                            readStream.Close();
                        }
                        Console.WriteLine("\n\n======================\n\n");

                    }
                    catch (Exception ex)
                    {
                        System.Media.SoundPlayer playerErr = new System.Media.SoundPlayer(configuration["error_sound"]);
                        playerErr.Play();
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
                //player.SoundLocation = alarms[idx];
                player.PlayLooping();
                try
                {
                    Reader.ReadLine(alarmTime * 1000);
                }
                catch (TimeoutException)
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

    */
}
