namespace Orleans.Providers.RavenDb.Configuration
{
    public class RavenDbGrainStorageOptions : RavenDbOptions
    {
        public GrainKeyGenerator? KeyGenerator { get; set; } = null;

        public bool UseClusterWideTransactions { get; set; } = false;

    }

    public delegate string GrainKeyGenerator(string grainType, GrainId grainId);

}
