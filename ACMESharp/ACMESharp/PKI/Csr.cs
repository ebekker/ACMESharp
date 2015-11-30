namespace ACMESharp.PKI
{
    /// <summary>
    /// Represents a Certificate Signing Request (CSR).
    /// </summary>
    public class Csr
    {
        public Csr(string pem)
        {
            Pem = pem;
        }

        public string Pem
        { get; private set; }

        //public void ExportAsDer(Stream s)
        //{
        //    using (var xr = new X509Request(Pem))
        //    {
        //        using (var bio = BIO.MemoryBuffer())
        //        {
        //            xr.Write_DER(bio);
        //            var arr = bio.ReadBytes((int)bio.BytesPending);
        //            s.Write(arr.Array, arr.Offset, arr.Count);
        //        }
        //    }
        //}

        //public void Save(Stream s)
        //{
        //    using (var w = new StreamWriter(s))
        //    {
        //        w.Write(JsonConvert.SerializeObject(this));
        //    }
        //}

        //public static Csr Load(Stream s)
        //{
        //    using (var r = new StreamReader(s))
        //    {
        //        return JsonConvert.DeserializeObject<Csr>(r.ReadToEnd());
        //    }
        //}

        //public static void ConvertPemToDer(Stream source, Stream target)
        //{
        //    using (var ts = new StreamReader(source))
        //    {
        //        using (var xr = new X509Request(ts.ReadToEnd()))
        //        {
        //            using (var bio = BIO.MemoryBuffer())
        //            {
        //                xr.Write_DER(bio);
        //                var arr = bio.ReadBytes((int)bio.BytesPending);
        //                target.Write(arr.Array, arr.Offset, arr.Count);
        //            }
        //        }
        //    }
        //}

        //public static void ConvertPemToDer(string sourcePath, string targetPath,
        //        FileMode fileMode = FileMode.Create)
        //{
        //    using (FileStream source = new FileStream(sourcePath, FileMode.Open),
        //            target = new FileStream(targetPath, fileMode))
        //    {
        //        ConvertPemToDer(source, target);
        //    }
        //}
    }

}
