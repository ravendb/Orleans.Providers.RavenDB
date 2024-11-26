using System.Security.Cryptography.X509Certificates;
using Raven.Client.Documents.Conventions;

namespace Orleans.Providers.RavenDB.Configuration
{
    public class RavenDbOptions
    {
        public string DatabaseName { get; set; }

        public X509Certificate2 Certificate { get; set; }

        public DocumentConventions Conventions { get; set; }

        public string[] Urls { get; set; }

    }
}
