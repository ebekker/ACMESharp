using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ACMESharp
{
    [TestClass]
    public class JsonTests
    {
        [TestMethod]
        public void TestGenerateJson()
        {
            var foo = new Foo
            {
                Prop1 = "Value1",
                Prop2 = Int32.MaxValue,
                Prop3a = true,
                Prop3b = (bool?)null,
                Prop3c = false,
                Prop4a = DateTime.Now,
                Prop4b = (DateTime?)null,
                Prop4c = DateTime.MaxValue,
            };

            using (var s = new MemoryStream())
            {
                JsonHelper.Save(s, foo);

                var json = Encoding.UTF8.GetString(s.ToArray());
            }
        }

        [TestMethod]
        public void TestDynamicExtendedKeys()
        {
            var json = @"
{
  ""Prop1"": ""Value1"",
  ""Prop2"": 2147483647,
  ""Prop3a"": true,
  ""Prop3b"": null,
  ""Prop3c"": false,
  ""Prop4a"": ""2015-12-09T12:35:27.5548604-05:00"",
  ""Prop4b"": null,
  ""Prop4c"": ""9999-12-31T23:59:59.9999999"",

  ""Prop5"": ""9999-12-31T23:59:59.9999999"",
  ""Prop6"": true,
  ""Prop7"": ""false""
}";
            using (var s = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var foo = JsonHelper.Load<Foo>(s);
            }
        }

        public class Foo
        {
            [JsonExtensionData]
            private readonly IDictionary<string, JToken> _expando = new Dictionary<string, JToken>();
             
            public JToken this[string name]
            {
                get { return _expando?[name]; }
                set { _expando[name] = value; }
            }

            public string Prop1
            { get; set; }

            public int Prop2
            { get; set; }

            public bool Prop3a
            { get; set; }

            public bool? Prop3b
            { get; set; }

            public bool? Prop3c
            { get; set; }

            public DateTime Prop4a
            { get; set; }

            public DateTime? Prop4b
            { get; set; }

            public DateTime? Prop4c
            { get; set; }

            public IEnumerable<string> GetPropertyNames()
            {
                return _expando.Keys;
            }
        }

        /*
        public class Foo : IDynamicMetaObjectProvider
        {
            private DynamicOnWriteObject _dyn = new DynamicOnWriteObject();

            public object this[string name]
            {
                get { return _dyn.Get(name); }
                set { _dyn.Set(name, value); }
            }

            public string Prop1
            { get; set; }

            public int Prop2
            { get; set; }

            public bool Prop3a
            { get; set; }

            public bool? Prop3b
            { get; set; }

            public bool? Prop3c
            { get; set; }

            public DateTime Prop4a
            { get; set; }

            public DateTime? Prop4b
            { get; set; }

            public DateTime? Prop4c
            { get; set; }

            public IEnumerable<string> GetPropertyNames()
            {
                return _dyn.GetDynamicMemberNames();
            }

            public DynamicMetaObject GetMetaObject(Expression parameter)
            {
                return _dyn.GetMetaObject(parameter);
            }
        }

        public class DynamicOnWriteObject : DynamicObject
        {
            private Dictionary<string, object> _extProps =
                    new Dictionary<string, object>();

            private Dictionary<string, GetBinder> _getBinders =
                    new Dictionary<string, GetBinder>();

            private Dictionary<string, SetBinder> _setBinders =
                    new Dictionary<string, SetBinder>();

            public object Get(string name)
            {
                GetBinder binder;
                if (!_getBinders.TryGetValue(name, out binder))
                {
                    binder = new GetBinder(name);
                    _getBinders[name] = binder;
                }

                object result;
                TryGetMember(binder, out result);
                return result;
            }

            public void Set(string name, object value)
            {
                SetBinder binder;
                if (!_setBinders.TryGetValue(name, out binder))
                {
                    binder = new SetBinder(name);
                    _setBinders[name] = binder;
                }

                TrySetMember(binder, value);
            }

            public override bool TrySetMember(SetMemberBinder binder,
                    object value)
            {
                _extProps[binder.Name] = value;
                return true;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                return _extProps.TryGetValue(binder.Name, out result);
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return _extProps.Keys;
            }

            public class GetBinder : GetMemberBinder
            {
                public GetBinder(string name)
                    : base(name, false)
                { }

                public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
                {
                    throw new NotImplementedException();
                }
            }

            public class SetBinder : SetMemberBinder
            {
                public SetBinder(string name)
                    : base(name, false)
                { }

                public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value,
                    DynamicMetaObject errorSuggestion)
                {
                    throw new NotImplementedException();
                }
            }
        }
        */
    }
}
