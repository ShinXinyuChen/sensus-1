// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SensusUI.UiProperties;
using System.Collections.ObjectModel;

namespace SensusService.Probes.User
{
    public class Script
    {
        #region static members
        private static JsonSerializerSettings JSON_SERIALIZER_SETTINGS = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor
        };

        public static Script FromJSON(string json)
        {
            Script script = null;

            try { script = JsonConvert.DeserializeObject<Script>(json, JSON_SERIALIZER_SETTINGS); }
            catch (Exception ex) { SensusServiceHelper.Get().Logger.Log("Failed to deserialize script:  " + ex.Message, LoggingLevel.Normal, typeof(Script)); }

            return script;
        }
        #endregion

        private string _id;
        private int _hashCode;
        private string _name;
        private int _delayMS;
        private ObservableCollection<Prompt> _prompts;
        private DateTimeOffset _firstRunTimestamp;
        private Datum _previousDatum;
        private Datum _currentDatum;

        private readonly object _locker = new object();

        public string Id
        {
            get { return _id; }
            set
            {
                _id = value;

                if (_id == null)
                    _hashCode = base.GetHashCode();
                else
                    _hashCode = _id.GetHashCode();
            }
        } 

        [EntryStringUiProperty("Name:", true, 1)]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        [EntryIntegerUiProperty("Delay (MS):", true, 2)]
        public int DelayMS
        {
            get { return _delayMS; }
            set { _delayMS = value; }
        }

        public ObservableCollection<Prompt> Prompts
        {
            get { return _prompts; }
        }

        public DateTimeOffset FirstRunTimestamp
        {
            get { return _firstRunTimestamp; }
            set { _firstRunTimestamp = value; }
        }

        public Datum PreviousDatum
        {
            get { return _previousDatum; }
            set { _previousDatum = value; }
        }

        public Datum CurrentDatum
        {
            get { return _currentDatum; }
            set { _currentDatum = value; }
        }

        [JsonIgnore]
        public bool Complete
        {
            get { return _prompts.Count == 0 || _prompts.All(p => p.Complete); }
        }

        /// <summary>
        /// Constructor for JSON deserialization.
        /// </summary>
        private Script()
        {
            _id = Guid.NewGuid().ToString();
            _hashCode = _id.GetHashCode();
            _delayMS = 0;
            _prompts = new ObservableCollection<Prompt>();
        }

        public Script(string name)
            : this()
        {
            _name = name;
        }

        public Script(string name, int delayMS)
            : this(name)
        {
            _delayMS = delayMS;
        }

        public void Save(string path)
        {
            using (StreamWriter file = new StreamWriter(path))
            {
                file.Write(JsonConvert.SerializeObject(this, JSON_SERIALIZER_SETTINGS));
                file.Close();
            }
        }

        public void RunAsync(Datum previousDatum, Datum currentDatum, Action<List<ScriptDatum>> callback)
        {
            #if __IOS__
            string userNotificationMessage = "Your input is requested.";
            #elif __ANDROID__
            string userNotificationMessage = null;
            #elif __WINDOWS_PHONE__
            TODO:  Should we use a message?
            #endif

            SensusServiceHelper.Get().ScheduleOneTimeCallback(cancellationToken =>
                {
                    SensusServiceHelper.Get().Logger.Log("Running script \"" + _name + "\".", LoggingLevel.Normal, GetType());

                    bool isRerun = true;
                    lock (_locker)
                    {
                        if (_firstRunTimestamp == DateTimeOffset.MinValue)
                        {
                            _firstRunTimestamp = DateTimeOffset.UtcNow;
                            isRerun = false;
                        }

                        if (previousDatum != null)
                            _previousDatum = previousDatum;

                        if (currentDatum != null)
                            _currentDatum = currentDatum;
                    }

                    List<ScriptDatum> data = new List<ScriptDatum>();

                    foreach (Prompt prompt in _prompts)
                        if (!prompt.Complete)
                        {
                            ManualResetEvent datumWait = new ManualResetEvent(false);

                            prompt.RunAsync(_previousDatum, _currentDatum, isRerun, _firstRunTimestamp, datum =>
                                {
                                    if (datum != null)
                                        data.Add(datum);

                                    datumWait.Set();
                                });

                            datumWait.WaitOne();
                        }

                    SensusServiceHelper.Get().Logger.Log("Script \"" + _name + "\" has finished running.", LoggingLevel.Normal, GetType());

                    callback(data);

                }, "Run Script", _delayMS, userNotificationMessage);
        }

        public Script Copy()
        {
            return JsonConvert.DeserializeObject<Script>(JsonConvert.SerializeObject(this, JSON_SERIALIZER_SETTINGS));
        }

        public override bool Equals(object obj)
        {
            Script s = obj as Script;

            return s != null && _id == s._id;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}
