﻿using ADK;
using Newtonsoft.Json;
using Planetarium.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Planetarium.Plugins.SolarSystem
{
    internal class OrbitalElementsManager
    {
        private static readonly string OrbitalElementsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ADK", "OrbitalElements");

        internal OrbitalElementsManager()
        {
            if (!Directory.Exists(OrbitalElementsPath))
            {
                try
                {
                    Directory.CreateDirectory(OrbitalElementsPath);
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Unable to create directory for orbital elements: {OrbitalElementsPath}, Details: {ex}");
                }
            }
        }

        internal List<GenericMoonData> Download()
        {
            JsonSerializer serializer = new JsonSerializer();
            List<GenericMoonData> orbits = null;

            string cachedFilePath = Path.Combine(OrbitalElementsPath, "SatellitesOrbits.dat");
            if (File.Exists(cachedFilePath))
            {
                Debug.WriteLine("Cached orbital elements file exists, try to parse");
                try
                {
                    using (StreamReader file = File.OpenText(cachedFilePath))
                    {
                        orbits = (List<GenericMoonData>)serializer.Deserialize(file, typeof(List<GenericMoonData>));
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Unable to read cached orbital elements file, Details: {ex}");
                }
            }
            else
            {
                Debug.WriteLine("No cached orbital elements found.");
            }

            if (orbits == null)
            {
                Debug.WriteLine("Read default orbital elements.");
                try
                {
                    using (StreamReader file = File.OpenText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data/SatellitesOrbits.dat")))
                    {
                        orbits = (List<GenericMoonData>)serializer.Deserialize(file, typeof(List<GenericMoonData>));
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Unable to read default orbital elements file, Details: {ex}");
                }
            }

            DateTime today = DateTime.Today;
            double jdToday = new Date(today).ToJulianDay();
            if (orbits != null && orbits.Any(orb => Math.Abs(jdToday - orb.jd0) >= 1))
            {
                Debug.WriteLine("Orbital elements are obsolete, downloading from web.");

                string startDate = today.ToString("yyyy-MM-dd");
                string endDate = today.AddDays(2).ToString("yyyy-MM-dd");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                int count = 0;
                foreach (var orbit in orbits)
                {
                    string url = $"https://ssd.jpl.nasa.gov/horizons_batch.cgi?batch=1&COMMAND='{(orbit.planet * 100 + orbit.satellite)}'&CENTER='500@{(orbit.planet * 100 + 99)}'&MAKE_EPHEM='YES'&TABLE_TYPE='ELEMENTS'&START_TIME='{startDate}'&STOP_TIME='{endDate}'&STEP_SIZE='2 d'&OUT_UNITS='AU-D'&REF_PLANE='ECLIPTIC'&REF_SYSTEM='J2000'&TP_TYPE='ABSOLUTE'&CSV_FORMAT='YES'&OBJ_DATA='YES'";
                    try
                    {
                        var request = WebRequest.Create(url);
                        using (var response = (HttpWebResponse)request.GetResponse())
                        using (var receiveStream = response.GetResponseStream())
                        using (var reader = new StreamReader(receiveStream))
                        {
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                ParseOrbit(orbit, reader.ReadToEnd());
                                count++;
                            }
                            else
                            {
                                Trace.TraceError($"Unable to download orbital elements from url: {url}, status code: {response.StatusCode}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError($"Unable to download orbital elements from url: {url}, Details: {ex}");
                    }
                }

                Debug.WriteLine($"{count}/{orbits.Count} orbital elements downloaded.");

                Debug.WriteLine("Saving orbital elements to cache...");
                try
                {
                    using (StreamWriter file = File.CreateText(cachedFilePath))
                    {
                        serializer.Formatting = Formatting.Indented;
                        serializer.Serialize(file, orbits);
                    }
                    Debug.WriteLine("Orbital elements saved to cache.");
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Unable to save orbital elements to cache, Details: {ex}");
                }
            }
            else
            {
                Debug.WriteLine("Orbital elements are up to date.");
            }

            return orbits;
        }

        static void ParseOrbit(GenericMoonData orbit, string response)
        {
            List<string> lines = response.Split('\n').ToList();
            string soeMarker = lines.FirstOrDefault(ln => ln == "$$SOE");
            int soeMarkerIndex = lines.IndexOf(soeMarker);
            string header = lines[soeMarkerIndex - 2];
            string orbitLine = lines[soeMarkerIndex + 1];

            List<string> headerItems = header.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToList();
            List<string> orbitItems = orbitLine.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToList();

            orbit.jd0 = double.Parse(orbitItems[headerItems.IndexOf("JDTDB")], CultureInfo.InvariantCulture);
            orbit.e = double.Parse(orbitItems[headerItems.IndexOf("EC")], CultureInfo.InvariantCulture);
            orbit.i = double.Parse(orbitItems[headerItems.IndexOf("IN")], CultureInfo.InvariantCulture);
            orbit.node0 = double.Parse(orbitItems[headerItems.IndexOf("OM")], CultureInfo.InvariantCulture);
            orbit.omega0 = double.Parse(orbitItems[headerItems.IndexOf("W")], CultureInfo.InvariantCulture);
            orbit.n = double.Parse(orbitItems[headerItems.IndexOf("N")], CultureInfo.InvariantCulture);
            orbit.M0 = double.Parse(orbitItems[headerItems.IndexOf("MA")], CultureInfo.InvariantCulture);
            orbit.a = double.Parse(orbitItems[headerItems.IndexOf("A")], CultureInfo.InvariantCulture);

            string magLine = lines.FirstOrDefault(ln => ln.Contains("V(1,0)"));
            if (!string.IsNullOrEmpty(magLine))
            {
                magLine = magLine.Substring(magLine.IndexOf("V(1,0)"));
                List<string> magItems = magLine.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToList();
                string mag = magItems.ElementAt(1).Split(' ').First();
                orbit.mag = double.Parse(mag, CultureInfo.InvariantCulture);
            }

            string radiusLine = lines.FirstOrDefault(ln => ln.Contains("Radius"));
            if (!string.IsNullOrEmpty(radiusLine))
            {
                radiusLine = radiusLine.Substring(radiusLine.IndexOf("Radius"));
                List<string> radiusItems = radiusLine.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToList();
                string radius = radiusItems.ElementAt(1).Split(' ').First().Split('x').First();
                orbit.radius = double.Parse(radius, CultureInfo.InvariantCulture);
            }
        }
    }
}
