using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace net.zmau.GTimeLineReader
{
    enum TravelMode
    {
        WALK, BIKE, CAR, BUS, AVION, UNKNOWN
    }

    class Travel
    {
        public TravelMode Mode { get; set; }
        public string ModeAsString
        {
            set
            {
                switch (value)
                {
                    case "WALK": case "WALKING": Mode = TravelMode.WALK; break;
                    case "CYCLING": Mode = TravelMode.BIKE; break;
                    case "IN_PASSENGER_VEHICLE": Mode = TravelMode.CAR; break;
                    case "IN_BUS": Mode = TravelMode.BUS; break;
                    case "FLYING": Mode = TravelMode.AVION; break;
                    case "UNKNOWN_ACTIVITY_TYPE": Mode = TravelMode.UNKNOWN; break;

                }
            }
        }
        public float Distance { get; set; }
        public DateTime Time { get; set; }

        public override string ToString()
        {
            return $"{Mode} {Distance} {Time}";
        }
    }

    class ModalSum
    {
        TravelMode Mode { get; set; }
        float DistanceTraveled { get; set; }
        int TripsCount { get; set; }

        public ModalSum(TravelMode Mode, float Distance, int tripsCount)
        {
            this.Mode = Mode;
            this.DistanceTraveled = Distance;
            TripsCount = tripsCount;
        }

        public override string ToString()
        {
            return $"{Mode} {String.Format("{0:N2}", DistanceTraveled/1000)}km {TripsCount}";
        }
    }

    internal class TimelineProcessor
    {
        private List<Travel> _moving;
        private List<ModalSum> _modalSplit;
        public TimelineProcessor()
        {
            _moving = new List<Travel>();
            _modalSplit = new List<ModalSum>();
        }

        public void readJson(string jsonFilePath)
        {
            if (ConfigurationManager.AppSettings["perMonth"].Equals("true"))
            {
                _moving = new List<Travel>();
                _modalSplit = new List<ModalSum>();
            }
            StreamReader reader = File.OpenText(jsonFilePath);
            using (JsonTextReader jsonReader = new JsonTextReader(reader))
            {
                try
                {
                    JObject content = (JObject)(JToken.ReadFrom(jsonReader));
                    if(content == null || content["timelineObjects"] == null)
                    {
                        return;
                    }
                    JArray items = (JArray)content["timelineObjects"];
                    var segments = items.Where(item => ((JObject)item).ContainsKey("activitySegment")).ToList();
                    foreach(var segmentToken in segments)
                    {
                        try
                        {
                            JToken segment = (JToken)(segmentToken["activitySegment"]!);
                            Travel travel = new Travel();
                            travel.ModeAsString = (string)segment["activityType"]!;
                            travel.Time = DateTime.ParseExact(((string)segment["duration"]["startTimestamp"]).Substring(0, 16), "MM/dd/yyyy HH:mm", null);
                            if (travel.Mode == TravelMode.UNKNOWN)
                            {
                                if (((JObject)segment).ContainsKey("simplifiedRawPath"))
                                {
                                    var simplifiedRawPath = segment["simplifiedRawPath"];
                                    if (((JObject)simplifiedRawPath).ContainsKey("distanceMeters"))
                                    {

                                        travel.Distance = (float)simplifiedRawPath["distanceMeters"];
                                    }
                                    else Console.WriteLine($"{travel.Time} : No simplifiedRawPath for UNKNOWN TravelMode");
                                }
                                else Console.WriteLine($"{travel.Time} : No simplifiedRawPath:distanceMeters for UNKNOWN TravelMode");
                            }
                            else
                            {
                                if (((JObject)segment).ContainsKey("distanceMeters"))
                                    travel.Distance = (float)segment["distanceMeters"];
                                else if (((JObject)segment).ContainsKey("distance"))
                                    travel.Distance = (float)segment["distance"];
                                //else Console.WriteLine($"{travel.Time} : no distance"); // TODO log somewhere 
                            }
                            _moving.Add(travel);
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
                catch (InvalidCastException e)
                {
                }
                catch (JsonReaderException) // e.g. status not integer
                {
                }
                catch (Exception)
                {
                }
            }
        }

        public void process()
        {
            _modalSplit.AddRange(
                _moving.GroupBy(m => m.Mode)
                    .Select(g => new ModalSum(g.First().Mode, g.Sum(mode => mode.Distance), g.Count())
                    ));
        }
        public void Write()
        {
            foreach(var modalShare in _modalSplit)
            {
                Console.WriteLine(modalShare);
            }
        }
    }
}
