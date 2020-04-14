﻿using Astrarium.Algorithms;
using Astrarium.Objects;
using Astrarium.Types;
using Astrarium.Types.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Astrarium
{
    public class Sky : ISky
    {
        private delegate ICollection<SearchResultItem> SearchDelegate(SkyContext context, string searchString, int maxCount = 50);
        private delegate void GetInfoDelegate<T>(CelestialObjectInfo<T> body) where T : CelestialObject;

        private Dictionary<Type, EphemerisConfig> EphemConfigs = new Dictionary<Type, EphemerisConfig>();
        private Dictionary<Type, Delegate> InfoProviders = new Dictionary<Type, Delegate>();
        private List<SearchDelegate> SearchProviders = new List<SearchDelegate>();
        private List<AstroEventsConfig> EventConfigs = new List<AstroEventsConfig>();
        private Dictionary<string, Constellation> Constellations = new Dictionary<string, Constellation>();

        private List<BaseCalc> Calculators = new List<BaseCalc>();
        private List<BaseAstroEventsProvider> EventProviders = new List<BaseAstroEventsProvider>();
        public SkyContext Context { get; private set; }

        public event Action Calculated;
        public event Action DateTimeSyncChanged;

        private ManualResetEvent dateTimeSyncResetEvent = new ManualResetEvent(false);
        private bool dateTimeSync = false;
        public bool DateTimeSync
        {
            get { return dateTimeSync; }
            set
            {
                if (dateTimeSync != value)
                {
                    dateTimeSync = value;
                    if (dateTimeSync)
                    {
                        dateTimeSyncResetEvent.Set();
                    }
                    else
                    {
                        dateTimeSyncResetEvent.Reset();
                    }
                    DateTimeSyncChanged?.Invoke();
                }
            }
        }

        public Sky()
        {
            new Thread(() =>
            {
                do
                {
                    dateTimeSyncResetEvent.WaitOne();
                    Context.JulianDay = new Date(DateTime.Now).ToJulianEphemerisDay();
                    Calculate();
                }
                while (true);
            })
            { IsBackground = true }.Start();
        }

        public void Initialize(SkyContext context, ICollection<BaseCalc> calculators, ICollection<BaseAstroEventsProvider> eventProviders)
        {
            Context = context;
            Calculators.AddRange(calculators);
            EventProviders.AddRange(eventProviders);
            
            foreach (var calc in Calculators)
            {
                calc.Initialize();

                Type calcType = calc.GetType();

                IEnumerable<Type> genericCalcTypes = calcType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICelestialObjectCalc<>));

                foreach (Type genericCalcType in genericCalcTypes)
                {
                    Type bodyType = genericCalcType.GetGenericArguments().First();
                    Type concreteCalc = typeof(ICelestialObjectCalc<>).MakeGenericType(bodyType);

                    // Ephemeris configs
                    EphemerisConfig config = Activator.CreateInstance(typeof(EphemerisConfig<>).MakeGenericType(bodyType)) as EphemerisConfig;
                    concreteCalc.GetMethod(nameof(ICelestialObjectCalc<CelestialObject>.ConfigureEphemeris)).Invoke(calc, new object[] { config });
                    if (!config.IsEmpty)
                    {
                        EphemConfigs[bodyType] = config;
                    }

                    // Info provider
                    Type genericGetInfoFuncType = typeof(GetInfoDelegate<>).MakeGenericType(bodyType);
                    InfoProviders[bodyType] = concreteCalc.GetMethod(nameof(ICelestialObjectCalc<CelestialObject>.GetInfo)).CreateDelegate(genericGetInfoFuncType, calc) as Delegate;
                    
                    // Search provider
                    var searchFunc = concreteCalc.GetMethod(nameof(ICelestialObjectCalc<CelestialObject>.Search)).CreateDelegate(typeof(SearchDelegate), calc) as SearchDelegate;
                    if (!SearchProviders.Contains(searchFunc))
                    {
                        SearchProviders.Add(searchFunc);
                    }
                }
            }

            foreach (var eventProvider in EventProviders)
            {
                // Astro events provider
                AstroEventsConfig eventsConfig = new AstroEventsConfig();
                eventProvider.ConfigureAstroEvents(eventsConfig);
                if (eventsConfig.Any())
                {
                    EventConfigs.Add(eventsConfig);
                }
            }

            LoadConstNames();
        }

        /// <summary>
        /// Loads constellation labels data
        /// </summary>
        // TODO: move to reader
        private void LoadConstNames()
        {
            string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data/ConNames.dat");
            string line = "";
            using (var sr = new StreamReader(file, Encoding.Default))
            {
                while (line != null && !sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    var chunks = line.Split(';');
                    string code = chunks[0].Trim().ToUpper();                    
                    string name = chunks[1].Trim();
                    string genitive = chunks[2].Trim();

                    Constellations.Add(code, new Constellation()
                    {
                        Code = code,
                        LatinName = name,
                        LatinGenitiveName = genitive
                    });
                }
            }
        }

        public void SetDate(double jd)
        {
            DateTimeSync = false;
            Context.JulianDay = jd;
            Calculate();
        }

        public void Calculate()
        {
            foreach (var calc in Calculators)
            {
                calc.Calculate(Context);
            }
            Calculated?.Invoke();
        }

        public List<List<Ephemeris>> GetEphemerides(CelestialObject body, double from, double to, double step, IEnumerable<string> categories, CancellationToken? cancelToken = null, IProgress<double> progress = null)
        {
            List<List<Ephemeris>> ephemerides = new List<List<Ephemeris>>();

            var config = EphemConfigs[body.GetType()];

            var itemsToBeCalled = config.Filter(categories);

            for (double jd = from; jd < to; jd += step)
            {
                if (cancelToken != null && cancelToken.Value.IsCancellationRequested)
                {
                    break;
                }
                else
                {
                    progress?.Report((jd - from) / (to - from) * 100);
                }

                var context = new SkyContext(jd, Context.GeoLocation);

                List<Ephemeris> ephemeris = new List<Ephemeris>();

                foreach (var item in itemsToBeCalled)
                {
                    ephemeris.Add(new Ephemeris()
                    {
                        Key = item.Category,
                        Value = item.Formula.DynamicInvoke(context, body),
                        Formatter = item.Formatter ?? Formatters.GetDefault(item.Category)
                    });
                }

                ephemerides.Add(ephemeris);
            }

            return ephemerides;
        }

        public CelestialObjectInfo GetInfo(CelestialObject body)
        {
            Type bodyType = body.GetType();

            if (InfoProviders.ContainsKey(bodyType))
            {
                var ephem = GetEphemerides(body, Context.JulianDay, Context.JulianDay + 1, 1, GetEphemerisCategories(body)).First();

                var infoType = typeof(CelestialObjectInfo<>).MakeGenericType(bodyType);
                var info = (CelestialObjectInfo)Activator.CreateInstance(infoType, Context, body, ephem);

                InfoProviders[bodyType].DynamicInvoke(info);

                return info;
            }
            else
            {
                return null;
            }
        }

        public ICollection<string> GetEventsCategories()
        {
            return EventConfigs.SelectMany(c => c).Select(i => i.Key).Distinct().ToArray();
        }

        public ICollection<string> GetEphemerisCategories(CelestialObject body)
        {
            var type = body.GetType();
            if (EphemConfigs.ContainsKey(type))
            {
                return EphemConfigs[type].GetCategories(body);
            }
            else
            {
                return new string[0];
            }
        }

        public ICollection<AstroEvent> GetEvents(double jdFrom, double jdTo, IEnumerable<string> categories, CancellationToken? cancelToken = null)
        {
            var context = new AstroEventsContext()
            {
                From = jdFrom,
                To = jdTo,
                GeoLocation = Context.GeoLocation
            };

            var events = new List<AstroEvent>();
            foreach (var item in EventConfigs.SelectMany(c => c).Where(i => categories.Contains(i.Key)))
            {
                if (cancelToken != null && cancelToken.Value.IsCancellationRequested)
                {
                    break;
                }
                else
                {
                    events.AddRange(item.Formula.Invoke(context));
                }
            }

            return events
                .OrderBy(e => e.NoExactTime ? e.JulianDay + 1 : e.JulianDay)
                .Where(e => e.JulianDay >= context.From && e.JulianDay < context.To)
                .ToArray();
        }

        public ICollection<SearchResultItem> Search(string searchString, Func<CelestialObject, bool> filter, int maxCount = 50)
        {
            var filterFunc = filter ?? ((b) => true);
            var results = new List<SearchResultItem>();
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                foreach (var searchProvider in SearchProviders)
                {
                    if (results.Count < maxCount)
                    {
                        results.AddRange(searchProvider(Context, searchString, maxCount).Where(r => filterFunc(r.Body)));
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return results.Take(maxCount).OrderBy(r => r.Name).ToList();
        }
       
        public Constellation GetConstellation(string code)
        {
            code = code.ToUpper();
            if (Constellations.ContainsKey(code))
            {
                return Constellations[code];
            }
            else
            {
                return null;
            }
        }
    }
}