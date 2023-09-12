namespace SIT.WebServer.Providers
{
    public class WeatherProvider
    {
        public WeatherProvider() 
        {
           
        }


        public class WeatherClass
        {
            public int acceleration { get; set; } = 7;
            public string time { get; set; } = TimeSpan.FromDays(1).ToString();
            public string date { get; set; } = DateTime.Now.ToString();
            public Weather weather { get; set; }

            public class Weather {

                private Random _random = new Random(); 

                public Weather()
                {

                }

                public double cloud => _random.NextDouble();
                public double wind_speed => _random.NextDouble();
                public Dictionary<string, double> wind_direction => new Dictionary<string, double>() { { "x", 0 }, { "y", 0 }, { "z", 0 } };
                public double wind_gustiness => _random.NextDouble();
                public double rain => _random.NextDouble();
                public double rain_intensity => _random.NextDouble();
                public double fog => _random.NextDouble();
                public double temp => _random.NextDouble();
                public double pressure => _random.NextDouble();
                public double time => _random.NextDouble();
                public double date => _random.NextDouble();
                public double timestamp => _random.NextDouble();
            }
        }
    }
}
