using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace TechnicalTest
{
    class GovApiClient
    {
        // Class to store data to be displayed
        class StationInfo
        {
            public string Id { set; get; }
            public string Name { set; get; }
            public double MinLevel { set; get; }
            public DateTime DateTimeAtMinLevel { set; get; }
            public double MaxLevel { set; get; }
            public DateTime DateTimeAtMaxLevel { set; get; }
            public double AverageLevel { set; get; }
        }

        // Url to get all stations whose "River name" value is "Stort":
        private readonly string UrlToGetAllStationsOnStort = @"http://environment.data.gov.uk/flood-monitoring/id/stations?riverName=Stort";

        // Url to get all readings of each station
        private readonly string UrlToGetAllReadings;

        // Singleton Pattern - only one client instance can exist.
        private static GovApiClient _client;

        private GovApiClient()
        {
            StationList = new List<StationInfo>();
            
            // Make URL to get last 7 day info & set 1000 to the parameter _limit.
            var today = DateTime.Today;
            // Set -7 from Today
            var oneWeekAgo = today.AddDays(-7);

            // {id}: placeholder, _sorted: descending order, _limit: 1000 records at most
            UrlToGetAllReadings = @"http://environment.data.gov.uk/flood-monitoring/id/stations/{id}/readings?_sorted&_limit=1000&startdate=" + oneWeekAgo.ToString("yyyy-MM-dd") + "&enddate=" + today.ToString("yyyy-MM-dd");
        }

        static GovApiClient GetClientForGvtApi()
        {
            if (_client == null)
                _client = new GovApiClient();
            return _client;
        }

        // Property for a list of all stations on the River Stort
        List<StationInfo> StationList { get; set; }

        // Get all stations on the River Stort
        void GetAllStations()
        {
            var responseData = SendRequest(UrlToGetAllStationsOnStort);
            SetBasicStationInfoFromJson(responseData.Result);
        }

        // Get the all readings from each station
        void GetAllReadings()
        {
            foreach (var station in StationList)
            {
                var url = UrlToGetAllReadings.Replace("{id}", station.Id);
                var responseData = SendRequest(url);
                SetLevelFromJson(responseData.Result, station);
            }
        }

        // Send request to the Government flood monitoring site
        async Task<string> SendRequest(string url)
        {
            string result = null;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Send Request
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    // Get string data in Json format
                    result = await response.Content.ReadAsStringAsync();
                }
            }
            return result;
        }

        // create a station instance and set it to the retrived values from jsonString.
        private void SetBasicStationInfoFromJson(string jsonString)
        {
            var information = JObject.Parse(jsonString);
            var stations = information["items"].Select(t => new { Id = (string)t["notation"], Name = (string)t["label"] }).ToArray();
            foreach (var s in stations.OrderBy(s=>s.Name))
            {
                StationList.Add(new StationInfo { Id = s.Id, Name = s.Name });
            }
        }

        // Set the station instance to the levels and datetime from jsonString.
        private void SetLevelFromJson(string jsonString, StationInfo station)
        {
            var information = JObject.Parse(jsonString);
            var stations = information["items"].Select(t => new { @DateTime = (DateTime)t["dateTime"], Value = (double)t["value"] }).ToArray();
            var maxVal = Double.MinValue;
            var minVal = Double.MaxValue;
            var sumVal = 0.0D;
            DateTime dateTimeAtMax = DateTime.Now;
            DateTime dateTimeFAtMin = DateTime.Now;
            // Find max level, min level and sum levels for average
            foreach (var s in stations)
            {
                // max level must be the one occured last
                if (maxVal < s.Value)
                {
                    maxVal = s.Value;
                    dateTimeAtMax = s.DateTime;
                }
                // min level must be the one occured first
                if (minVal >= s.Value)
                {
                    minVal = s.Value;
                    dateTimeFAtMin = s.DateTime;
                }
                sumVal += s.Value;
            }
            station.MaxLevel = maxVal;
            station.MinLevel = minVal;
            station.DateTimeAtMaxLevel = dateTimeAtMax;
            station.DateTimeAtMinLevel = dateTimeFAtMin;
            station.AverageLevel = sumVal / stations.Length;
        }

        // Display station info
        void DisplayStationInfo()
        {
            foreach(var s in StationList)
            {
                Console.WriteLine("*****************************************************");
                Console.WriteLine("Name            : {0}", s.Name);
                Console.WriteLine("Min Level (Date): {0:f3} ({1})", s.MinLevel, s.DateTimeAtMinLevel);
                Console.WriteLine("Max Level (Date): {0:f3} ({1})", s.MaxLevel, s.DateTimeAtMaxLevel);
                Console.WriteLine("Average         : {0:f3}", s.AverageLevel);
            }
            Console.WriteLine("*****************************************************");
            Console.WriteLine("Press a key to exit");
        }

        static void Main(string[] args)
        {

            var client = GovApiClient.GetClientForGvtApi();
            
            //Stopwatch stopWatch = Stopwatch.StartNew();
            
            client.GetAllStations();
            
            //stopWatch.Stop();
            //Console.WriteLine(stopWatch.Elapsed);
            //stopWatch.Start();
            
            client.GetAllReadings();
            
            //stopWatch.Stop();
            //Console.WriteLine(stopWatch.Elapsed);
            
            client.DisplayStationInfo();
            Console.ReadKey();
        }
    }
}
