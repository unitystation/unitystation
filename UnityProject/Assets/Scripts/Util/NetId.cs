public static class NetId
{
    /// <summary>
    /// Created to allow for comparison of invalid NetIDs (uint)
    /// as code was built on UNET and NetIDs are replaced with uint in mirror
    /// </summary>
    /// <returns></returns>
    public const uint Invalid = uint.MaxValue; //if game ever gets to max value then the server will go kaput so its perfect for invalid test
    /// <summary>
    /// Compare with NetIDs to see if they are Empty or not network synced yet 
    /// (maybe they were improperly network spawned or networkconnection is slow)
    /// </summary>
    public const uint Empty = 0;
}