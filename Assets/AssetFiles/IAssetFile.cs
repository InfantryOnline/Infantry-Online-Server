namespace Assets
{
    /// <summary>
    /// Interface to every type of asset file; implementation highly encouraged for external usage of assets.
    /// </summary>
    public interface IAssetFile<TDataContainer>
    {
        /// <summary>
        /// Returns a strongly-typed data (or list of data) object for this file; contains the entirety of the file's contents.
        /// </summary>
        TDataContainer Data { get; }
    }
}
