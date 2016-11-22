using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACMESharp.Vault.Providers
{
    /// <summary>
    /// Basic wrapper around any entity that we save using this file-based
    /// provider in order to track common meta data about the entity.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EntityMeta<T>
    {
        public DateTime CreateDate
        { get; set; }

        public string CreateUser
        { get; set; }

        public string CreateHost
        { get; set; }

        public DateTime UpdateDate
        { get; set; }

        public string UpdateUser
        { get; set; }

        public T Entity
        { get; set; }
    }
}
