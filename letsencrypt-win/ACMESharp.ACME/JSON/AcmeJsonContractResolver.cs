using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.JSON
{
    public class AcmeJsonContractResolver : CamelCasePropertyNamesContractResolver // DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            var propName = base.ResolvePropertyName(propertyName);
            if (!string.IsNullOrWhiteSpace(propName) && char.IsUpper(propName[0]))
            {
                var propNameChars = propName.ToCharArray();
                propNameChars[0] = char.ToLower(propNameChars[0]);
                propName = new string(propNameChars);
            }

            return propName;
        }
    }
}
