using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


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
            Console.WriteLine("And extended by Gavin & Ramon");
            Console.WriteLine("Website: https://www.alavon.nl/");
            Console.WriteLine("Source of this application can be found on github at https://www.github.com/elertan/grandeomegacheat");
            Console.WriteLine("Oh and the app is input sensitive, so if you dont put in numbers when asked it will crash for example, to lazy to check for valid input!");
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
            var courseIndex = Convert.ToInt32(Console.ReadLine()) - 1;

            Console.WriteLine("Getting Chapters...");
            var chapters = await GetChapters(courses[courseIndex].Id);
            var exams = await GetExams(courses[courseIndex].Id);

            Console.WriteLine("------------------------------------");
            Console.WriteLine($"Use a number to select a chapter/exam of course {courses[courseIndex].Name}");
            int j = 1;
            Console.WriteLine("Chapters:");
            foreach (var chapter in chapters)
            {
                Console.WriteLine($"{j}) {chapter.Code}");
                j++;
            }
            Console.WriteLine("Exams:");
            foreach (var exam in exams)
            {
                Console.WriteLine($"{j}) {exam.Title}");
                j++;
            }
            var inputIndex = Convert.ToInt32(Console.ReadLine());

            if (inputIndex < chapters.Count) await CheatChapter(chapters[inputIndex - 1].Id);
            else await CheatExam(exams[inputIndex - 1].Id);
            
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("Would you like to do a other course? (Y/N)");

            if(Console.ReadKey().Key == ConsoleKey.Y){
                await MainPart();
            }

        }

        private static async Task CheatExam(int id)
        {
            Console.WriteLine("Do exam cheating...");
        }

        private static async Task CheatChapter(int id)
        {
            var teachingActivies = await GetTeachingActivies(id);
            int loopI = 1;
            foreach (var teachingActivity in teachingActivies)
            {
                if (teachingActivity.AmountOfHiddenValues == 0)
                {
                    Console.WriteLine($"Setting question {loopI} to success!");
                    await SetSuccesfulAssignment(GlobalId, teachingActivity.Id);
                }
                else
                {
                    Console.WriteLine($"Question {loopI}: Creating forward assignment");
                    var assignmentId = await CreateForwardAssignment(teachingActivity.Id);
                    Console.WriteLine($"Question {loopI}: Getting first step");
                    await GetFirstStep(assignmentId);
                    for (var counter = 0; counter < teachingActivity.AmountOfHiddenValues - 1; counter++)
                    {
                        Console.WriteLine($"Question {loopI}: Adding question part {counter + 1}/{teachingActivity.AmountOfHiddenValues}");
                        await AddSuccessfulAttempt(GlobalId, teachingActivity.Id);
                    }
                    Console.WriteLine($"Question {loopI}: Finalizing question {teachingActivity.AmountOfHiddenValues}/{teachingActivity.AmountOfHiddenValues}");
                    await SetSuccesfulAssignment(GlobalId, teachingActivity.Id);
                }
                loopI++;
            }

            Console.WriteLine($"Finished! Completed {loopI - 1} questions");
        }

        private static async Task SetSuccesfulAssignment(int chapterId, int teachingActivityId)
        {
            var response = await HttpClient.PostAsync(
                $"/api/v1/CustomAssignmentLogic/AddSuccessfulAssignment/{chapterId}/{teachingActivityId}",
                new StringContent(""));
        }

        private static async Task<int> CreateForwardAssignment(int teachingActivityId)
        {
            var response = await HttpClient.GetStringAsync(
                $"/api/v1/CustomAssignmentLogic/GetOrCreateForwardAssignmentCode/{teachingActivityId}");
            var data = (dynamic)JsonConvert.DeserializeObject(response);
            return data.code.Id;
        }

        private static async Task GetFirstStep(int id)
        {
            var response = await HttpClient.GetStringAsync(
                $"/api/v1/CustomAssignmentLogic/GetFirstStep/{id}");
        }

        private static async Task AddSuccessfulAttempt(int chapterId, int teachingActivityId)
        {
            var response = await HttpClient.PostAsync(
                $"/api/v1/CustomAssignmentLogic/AddSuccessfulAttempt/{chapterId}/{teachingActivityId}",
                new StringContent(""));
        }

        private static async Task<List<TeachingActivity>> GetTeachingActivies(int id)
        {
            var teachingActivities = new List<TeachingActivity>();

            var content = await HttpClient.GetStringAsync("/api/v1/CustomAssignmentLogic/GetTeachingActivities/" + id);
            var data = (dynamic)JsonConvert.DeserializeObject(content);
            foreach (var c in data)
            {
                var teachingActivity = new TeachingActivity();
                teachingActivity.Id = c.TeachingActivity.Id;
                teachingActivity.Kind = c.TeachingActivity.Kind;
                if (teachingActivity.Kind == "ForwardAssignment")
                {
                    var strValues = c.TeachingActivity.hidden_values.Value;
                    var hiddenValues = JsonConvert.DeserializeObject(strValues);
                    teachingActivity.AmountOfHiddenValues = hiddenValues.Count;
                }
                teachingActivities.Add(teachingActivity);
            }

            return teachingActivities;
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

        private static async Task<List<Exam>> GetExams(int index)
        {
            var exams = new List<Exam>();

            var content2 = await HttpClient.GetStringAsync("/api/v1/CustomAssignmentLogic/LoadExams/" + index);
            var data2 = (dynamic)JsonConvert.DeserializeObject(content2);
            foreach (var c in data2)
            {
                var exam = new Exam();
                exam.Id = c.Id;
                exam.Title = c.Title;
                exams.Add(exam);
            }

            return exams;
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
            GlobalId = responseData.Item2.Id;
            return true;
        }

        public static int GlobalId { get; set; }

        private static void GetPassword()
        {
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
