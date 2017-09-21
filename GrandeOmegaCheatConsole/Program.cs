using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace GrandeOmegaCheatConsole
{
    class Program
    {
        private const string BaseUrl = "https://grandeomega.com";
        private const string XSRFTokenHeaderName = "X-XSRF-TOKEN";
        public static string Username { get; set; }
        public static string Password { get; set; }
        public static string Name { get; set; }
        public static HttpClient HttpClient { get; set; }
        public static CookieContainer CookieContainer { get; set; }

        static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            CookieContainer = new CookieContainer();
            HttpClient = new HttpClient(new HttpClientHandler {CookieContainer = CookieContainer}) { BaseAddress = new Uri(BaseUrl)};

            // Default Request Headers
            HttpClient.DefaultRequestHeaders.Add("Origin", BaseUrl);
            HttpClient.DefaultRequestHeaders.Connection.Add("keep-alive");
            HttpClient.DefaultRequestHeaders.Host = "grandeomega.com";
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36");

            Console.WriteLine("GrandeOmega Cheat created by Dennis Kievits (Alavon)");
            Console.WriteLine("Website: https://www.alavon.nl/");
            Console.WriteLine("Source of this application can be found on github at https://www.github.com/elertan/grandeomegacheat");
            Console.WriteLine("\n\n");
            Console.WriteLine("Receiving Anti Forgery Token...");
            // Receive AntiForgeryToken
            var content = await HttpClient.GetStringAsync("/Homepage/Benefits");
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);
            var value = htmlDoc.DocumentNode.Descendants("input").First(el => el.Attributes.Any(attr => attr.Name == "name") && el.Attributes.First(attr => attr.Name == "name").Value == "__RequestVerificationToken").Attributes.First(attr => attr.Name == "value").Value;

            // Add token te required headers
            HttpClient.DefaultRequestHeaders.Add(XSRFTokenHeaderName, value);

            HttpClient.DefaultRequestHeaders.Referrer = new Uri("https://grandeomega.com/Homepage/Benefits");
            HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.8));
            HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.8));
            HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("nl", 0.6));
            HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de", 0.4));
            HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ko", 0.2));
            HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("pt", 0.2));

            //HttpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            //HttpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            //HttpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            HttpClient.DefaultRequestHeaders.Add("DNT", "1");

            while (true)
            {
                GetUsername();
                GetPassword();

                var success = await Login();
                if (!success)
                {
                    Console.Clear();
                    continue;
                }
                break;
            }

            await MainPart();
        }

        private static async Task MainPart()
        {
            Console.Clear();
            Console.WriteLine($"Welcome, {Name}");

            Console.WriteLine("Getting courses...");
            var courses = await GetCourses();

            Console.WriteLine("------------------------------------");
            Console.WriteLine("Use a number to select a course");
            int i = 1;
            foreach (var course in courses)
            {
                Console.WriteLine($"{i}) {course.Name}");
                i++;
            }
            var index = Convert.ToInt32(Console.ReadLine()) - 1;

            Console.WriteLine("Getting Chapters...");
            var chapters = await GetChapters(courses[index].Id);

            Console.WriteLine("------------------------------------");
            Console.WriteLine($"Use a number to select a chapter of course {courses[index].Name}");
            int i2 = 1;
            foreach (var chapter in chapters)
            {
                Console.WriteLine($"{i2}) {chapter.Code}");
                i2++;
            }
            var index2 = Convert.ToInt32(Console.ReadLine());

            Console.ReadKey();
        }

        private static async Task<List<Chapter>> GetChapters(int index)
        {
            var chapters = new List<Chapter>();

            var content = await HttpClient.GetStringAsync("/api/v1/CustomAssignmentLogic/LoadChapters/" + index);
            var data = (dynamic)JsonConvert.DeserializeObject(content);
            foreach (var c in data)
            {
                var chapter = new Chapter();
                chapter.Id = c.Chapter.Id;
                chapter.Code = c.Chapter.Code;
                chapters.Add(chapter);
            }

            return chapters;
        }

        private static async Task<List<Course>> GetCourses()
        {
            var courses = new List<Course>();
            var content = await HttpClient.GetStringAsync("/api/v1/Course?page_index=0&page_size=100");
            var data = (dynamic) JsonConvert.DeserializeObject(content);
            foreach (var item in data.Items)
            {
                var course = new Course();
                course.Id = item.Item.Id;
                course.Name = item.Item.Name;
                courses.Add(course);
            }
            return courses;
        }

        private static async Task<bool> Login()
        {
            // Actual Login
            var dictionary = new Dictionary<string, string>
            {
                { "Username", "" },
                { "Email", Username },
                { "Password", Password },
                { "Role", "student" }
            };

            var data = JsonConvert.SerializeObject(dictionary);
            var loginResponse = await HttpClient.PostAsync("/api/authentication/Login", new StringContent(data, Encoding.UTF8, "application/json"));

            var content = await loginResponse.Content.ReadAsStringAsync();
            var responseData = (dynamic)JsonConvert.DeserializeObject(content);
            if (responseData.Item1 == "none")
            {
                Console.WriteLine("Login Failed.\nPlease check your username and/or password.");
                Console.ReadKey();
                return false;
            }
            Name = $"{responseData.Item2.Name} {responseData.Item2.Surname}";
            return true;
        }

        private static void GetPassword()
        {
#if DEBUG
            Password = "FetzhKzWmXE=";
            return;
#endif
            Console.Write("Enter password: ");
            Password = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(Password))
            {
                Console.WriteLine("Wrong input. Try again");
                GetUsername();
            }
        }

        private static void GetUsername()
        {
#if DEBUG
            Username = "0946572@hr.nl";
            return;
#endif

            Console.Write("Enter username (0900000@hr.nl): ");
            Username = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(Username))
            {
                Console.WriteLine("Wrong input. Try again");
                GetUsername();
            }
        }
    }
}