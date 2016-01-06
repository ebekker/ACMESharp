using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using ACMESharp.HTTP;
using System.Text;

namespace ACMESharp.Util
{
    public static class JsonHelper
    {
        private static Newtonsoft.Json.JsonSerializerSettings JSS_TNH_NONE =
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None,
                    Converters = new List<JsonConverter>
                    {
                        AcmeEntitySerializer.INSTANCE
                    }
                };

        private static Newtonsoft.Json.JsonSerializerSettings JSS_TNH_OBJ =
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects,
                    Converters = new List<JsonConverter>
                    {
                        AcmeEntitySerializer.INSTANCE
                    }
                };

        private static Newtonsoft.Json.JsonSerializerSettings JSS_TNH_AUTO =
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    Converters = new List<JsonConverter>
                    {
                        AcmeEntitySerializer.INSTANCE
                    }
                };

        //private static Newtonsoft.Json.JsonSerializerSettings JSS_TNH_ALL =
        //        new Newtonsoft.Json.JsonSerializerSettings
        //        {
        //            Formatting = Newtonsoft.Json.Formatting.Indented,
        //            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
        //            Converters = new List<JsonConverter>
        //            {
        //                AcmeEntitySerializer.INSTANCE
        //            }
        //        };


        /// <summary>
        /// Serializes the given object as JSON to the target stream
        /// adding type name annotation to any embeded object.
        /// </summary>
        public static void Save(Stream s, object obj)
        {
            using (var w = new StreamWriter(s))
            {
                w.Write(JsonConvert.SerializeObject(obj, JSS_TNH_OBJ));
            }
        }

        /// <summary>
        /// Serializes the given object as JSON to the target stream
        /// adding type name annotations as specified.
        /// </summary>
        /// <param name="noneOrAuto">if true, automatically decides whether
        ///         type name annotations are needed on a case-by-case basis;
        ///         if false, adds no type name annotations</param>
        public static void Save(Stream s, object obj, bool noneOrAuto)
        {
            using (var w = new StreamWriter(s))
            {
                w.Write(JsonConvert.SerializeObject(obj,
                        noneOrAuto ? JSS_TNH_AUTO : JSS_TNH_NONE));
            }
        }

        public static string Save(object obj)
        {
            using (var ms = new MemoryStream())
            {
                Save(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static string Save(object obj, bool noneOrAuto)
        {
            using (var ms = new MemoryStream())
            {
                Save(ms, obj, noneOrAuto);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static T Load<T>(Stream s)
        {
            using (var r = new StreamReader(s))
            {
                return JsonConvert.DeserializeObject<T>(r.ReadToEnd(), JSS_TNH_AUTO);
            }
        }

        public static T Load<T>(string s)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(s)))
            {
                return Load<T>(ms);
            }
        }

        public class AcmeEntitySerializer : Newtonsoft.Json.JsonConverter
        {
            public static readonly AcmeEntitySerializer INSTANCE = new AcmeEntitySerializer();

            public override bool CanRead
            {
                get
                {
                    return false;
                }
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(ACME.Challenge).IsAssignableFrom(objectType)
                        || typeof(ACME.ChallengeAnswer).IsAssignableFrom(objectType)

                        // false

                        //||typeof(AcmeServerDirectory) == objectType
                        //|| typeof(LinkCollection) == objectType
                        //|| (objectType.IsGenericType
                        //        && typeof(EntityDictionary<IIdentifiable>).GetGenericTypeDefinition() ==
                        //                objectType.GetGenericTypeDefinition())
                        //|| typeof(EntityDictionary<RegistrationInfo>) == objectType;
                        ;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (typeof(AcmeServerDirectory) == objectType)
                {
                    var jarr = JArray.Load(reader);
                    var sdir = existingValue as AcmeServerDirectory;

                    if (jarr == null)
                        sdir = null;
                    else
                    {
                        if (sdir == null)
                            sdir = new AcmeServerDirectory();
                        foreach (var jt in jarr)
                        {
                            var kv = jt.ToObject<KeyValuePair<string, string>>();
                            sdir[kv.Key] = kv.Value;
                        }
                    }

                    return sdir;
                }
                else if (typeof(LinkCollection) == objectType)
                {
                    var jarr = JArray.Load(reader);
                    var lc = existingValue as LinkCollection;

                    if (jarr == null)
                        lc = null;
                    else
                    {
                        if (lc == null)
                            lc = new LinkCollection();
                        foreach (var jt in jarr)
                        {
                            lc.Add(new Link(jt.ToObject<string>()));
                        }
                    }

                    return lc;
                }

                throw new NotSupportedException("Unsupported type");
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var objectType = value.GetType();
                if (typeof(AcmeServerDirectory) == objectType)
                {
                    var sd = (AcmeServerDirectory)value;
                    var jt = JToken.FromObject(sd);
                    jt.WriteTo(writer);
                }
                else if (typeof(LinkCollection) == objectType)
                {
                    var lc = (LinkCollection)value;
                    writer.WriteStartArray();
                    foreach (var l in ((IEnumerable<Link>)lc))
                        writer.WriteValue(l.Value);
                    writer.WriteEndArray();
                }

                // TODO:  MAJOR HACK HERE!!!!!
                // THIS WHOLE THING NEEDS TO BE EXTINGUISHED!

                else if (typeof(ACME.Challenge).IsAssignableFrom(objectType))
                {
                    var ch = (ACME.Challenge)value;
                    var jt = JToken.FromObject(value);
                    ((JObject)jt).AddFirst(new JProperty("$type", value.GetType().AssemblyQualifiedName));
                    if (ch.Answer != null)
                    {
                        ((JObject)jt[nameof(ch.Answer)]).AddFirst(
                                new JProperty("$type", ch.Answer.GetType().AssemblyQualifiedName));
                    }
                    jt.WriteTo(writer, this);
                }
                else if (typeof(ACME.ChallengeAnswer).IsAssignableFrom(objectType))
                {
                    var jt = JToken.FromObject(value);
                    ((JObject)jt).AddFirst(new JProperty("$type", value.GetType().AssemblyQualifiedName));
                    jt.WriteTo(writer);
                }
                else
                {
                    throw new NotSupportedException("Unsupported type");
                }
            }
        }
    }
}
