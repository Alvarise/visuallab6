using Newtonsoft.Json;
using ReactiveUI;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Weather
{
    public class LatLonData
    {
        public string lat { get; set; }
        public string lon { get; set; }
    }

    public class Main
    {
        public float temp { get; set; }
        public float temp_min { get; set; }
        public float temp_max { get; set; }
        public int pressure { get; set; }
        public int humidity { get; set; }
    }

    public class WeatherMain
    {
        public string main { get; set; }
    }

    public class Wind
    {
        public float speed { get; set; }
    }

    public class City
    {
        public string name { get; set; }
    }

    public class ListItem
    {
        public Main main { get; set; }
        public WeatherMain[] weather { get; set; }
        public Wind wind { get; set; }
        public string dt_txt { get; set; }
    }

    public class WeatherData
    {
        public List<ListItem> list { get; set; }
        public City city {  get; set; }
    }

    public class WeatherState(string name, string temp, string time) : ReactiveObject
    {
        private string name = name;
        private string temp = temp + "°C";
        private string time = time + ":00";

        public string Name
        {
            get => name;
            set => this.RaiseAndSetIfChanged(ref name, value);
        }

        public string Temp
        {
            get => temp;
            set => this.RaiseAndSetIfChanged(ref temp, value);
        }

        public string Time
        {
            get => time;
            set => this.RaiseAndSetIfChanged(ref time, value);
        }
    }

    public class DayState(string date, string name, string minTemp, string maxTemp) : ReactiveObject
    {
        private string date = date;
        private string name = name;
        private string edgeTemp = minTemp + "°C" + " - " + maxTemp + "°C";

        public string Date
        {
            get => date;
            set => this.RaiseAndSetIfChanged(ref date, value);
        }

        public string Name
        {
            get => name;
            set => this.RaiseAndSetIfChanged(ref name, value);
        }

        public string EdgeTemp
        {
            get => edgeTemp;
            set => this.RaiseAndSetIfChanged(ref edgeTemp, value);
        }
    }

    internal class Weather : ReactiveObject
    {
        private string date = "";
        private string city = "";
        private string name = "";
        private string temp = "";
        private string humidity = "";
        private string wind = "";
        private string pressure = "";
        public ObservableCollection<WeatherState> states { get; } = [];
        public ObservableCollection<DayState> days { get; } = [];

        public string Date
        {
            get => date;
            set => this.RaiseAndSetIfChanged(ref date, value);
        }

        public string City
        {
            get => city;
            set => this.RaiseAndSetIfChanged(ref city, value);
        }

        public string Name
        {
            get => name;
            set => this.RaiseAndSetIfChanged(ref name, value);
        }

        public string Temp
        {
            get => temp;
            set => this.RaiseAndSetIfChanged(ref temp, value);
        }

        public string Humidity
        {
            get => humidity;
            set => this.RaiseAndSetIfChanged(ref humidity, value);
        }

        public string Wind
        {
            get => wind;
            set => this.RaiseAndSetIfChanged(ref wind, value);
        }

        public string Pressure
        {
            get => pressure;
            set => this.RaiseAndSetIfChanged(ref pressure, value);
        }

        private static List<List<T>> SplitList<T>(List<T> list, int parts)
        {
            int size = (int)Math.Ceiling((double)list.Count / parts);
            return list
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / size)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        private RestResponse LoadLatLon(string city)
        {
            RestClient latlonCall = new("http://api.openweathermap.org/geo/1.0/direct");
            RestRequest latlonRequest = new();

            latlonRequest.AddParameter("q", city);
            latlonRequest.AddParameter("limit", "1");
            latlonRequest.AddParameter("appid", "1fe04ce6fbfc3905a51ca67238ad1dd9");

            return latlonCall.Execute(latlonRequest);
        }

        private RestResponse LoadWeather(LatLonData[] latlonData)
        {
            RestClient weatherCall = new("http://api.openweathermap.org/data/2.5/forecast");
            RestRequest weatherRequest = new();

            weatherRequest.AddParameter("lat", latlonData[0].lat);
            weatherRequest.AddParameter("lon", latlonData[0].lon);
            weatherRequest.AddParameter("appid", "1fe04ce6fbfc3905a51ca67238ad1dd9");

            return weatherCall.Execute(weatherRequest);
        }

        public void Parse(string city)
        {
            if (city == null)
                return;

            RestResponse latlonResponse = LoadLatLon(city);
            if(latlonResponse.Content == "[]")
                return;

            LatLonData[] latlonData = JsonConvert.DeserializeObject<LatLonData[]>(latlonResponse.Content);
            RestResponse responseWeather = LoadWeather(latlonData);
            if (!responseWeather.IsSuccessful)
                return;

            if(date != "")
            {
                states.Clear();
                days.Clear();
            }

            WeatherData weatherData = JsonConvert.DeserializeObject<WeatherData>(responseWeather.Content);
            City = weatherData.city.name;
            Date = DateTime.Parse(weatherData.list[0].dt_txt).DayOfWeek.ToString() + ", "
                + DateTime.Parse(weatherData.list[0].dt_txt).Day.ToString();
            Name = weatherData.list[0].weather[0].main;
            Temp = ((int)weatherData.list[0].main.temp - 273).ToString() + "°C";
            Humidity = "Humidity:\n" + weatherData.list[0].main.humidity.ToString() + "%";
            Wind = "Wind:\n" + weatherData.list[0].wind.speed.ToString() + " m/s";
            Pressure = "Pressure:\n" + weatherData.list[0].main.pressure.ToString() + " mm";
            for (int i = 0; i < 8; i++)
                states.Add(new(weatherData.list[i].weather[0].main,
                            ((int)weatherData.list[i].main.temp - 273).ToString(),
                            DateTime.Parse(weatherData.list[i].dt_txt).Hour.ToString()));

            List<List<ListItem>> split = SplitList(weatherData.list, 5);
            for (int i = 1; i < 5; i++)
            {
                string weekDay;
                string day;
                weekDay = DateTime.Parse(split[i][0].dt_txt).DayOfWeek.ToString();
                day = DateTime.Parse(split[i][0].dt_txt).Day.ToString();

                days.Add(new(DateTime.Parse(split[i][0].dt_txt).DayOfWeek.ToString() + ", " +
                    DateTime.Parse(split[i][0].dt_txt).Day.ToString(),
                    split[i][0].weather[0].main,
                    ((int)split[i].Min(item => item.main.temp_min) - 273).ToString(),
                    ((int)split[i].Max(item => item.main.temp_max) - 273).ToString()));
            }
        }
    }
}