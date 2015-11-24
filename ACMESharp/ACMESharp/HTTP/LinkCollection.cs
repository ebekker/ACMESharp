using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ACMESharp.HTTP
{
    /// <summary>
    /// A collection of HTTP header <see cref="Link">Link</see> objects.
    /// </summary>
    /// <remarks>
    /// This collection implements multiple generic-typed enumerable interfaces
    /// but for backward compatibility, the default implemenation, i.e. the
    /// IEnumerable non-generic implementation, returns a sequence of strings
    /// which are the complete, formatted Link values.
    /// </remarks>
    public class LinkCollection : IEnumerable<string>, IEnumerable<Link>, ILookup<string, string>
    {
        private List<Link> _Links = new List<Link>();

        public LinkCollection()
        { }

        public LinkCollection(IEnumerable<Link> links)
        {
            if (links != null)
                foreach (var l in links)
                Add(l);
        }

        public LinkCollection(IEnumerable<string> linkValues)
        {
            if (linkValues != null)
                foreach (var lv in linkValues)
                    Add(new Link(lv));
        }

        public IEnumerable<string> this[string key]
        {
            get
            {
                return _Links.Where(x => x.Relation == key).Select(x => x.Uri);
            }
        }

        public int Count
        {
            get
            {
                return _Links.Count;
            }
        }

        public void Add(Link link)
        {
            _Links.Add(link);
        }

        public Link GetFirstOrDefault(string key)
        {
            return _Links.FirstOrDefault(x => x.Relation == key);
        }

        public bool Contains(string key)
        {
            return GetFirstOrDefault(key) != null;
        }

        IEnumerator<Link> IEnumerable<Link>.GetEnumerator()
        {
            return _Links.GetEnumerator();
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            foreach (var l in _Links)
                yield return l.Value;
        }

        IEnumerator<IGrouping<string, string>> IEnumerable<IGrouping<string, string>>.GetEnumerator()
        {
            return _Links.ToLookup(x => x.Relation, x => x.Uri).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<string>)this).GetEnumerator();
        }

        // We may use this in the future to build a more
        // efficient version of the Lookup interface
        //
        //private class LinkGrouping : IGrouping<string, string>
        //{
        //    public string Relation
        //    { get; set; }
        //
        //    public IEnumerable<Link> Links
        //    { get; set; }
        //
        //    public string Key
        //    {
        //        get
        //        {
        //            return Relation;
        //        }
        //    }
        //
        //    public IEnumerator<string> GetEnumerator()
        //    {
        //        foreach (var l in Links)
        //            yield return l.Uri;
        //    }
        //
        //    IEnumerator IEnumerable.GetEnumerator()
        //    {
        //        foreach (var l in Links)
        //            yield return l.Uri;
        //    }
        //}
    }
}
