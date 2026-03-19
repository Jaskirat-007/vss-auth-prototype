namespace VSSAuthPrototype.Services
{
    public interface IStorageService
    {
        /// <summary>
        /// PRIMARY: Gets a time-limited signed URL for a specific B2 object key.
        /// Calls GET /signed-url?key=... on the Flask backend.
        /// </summary>
        Task<string?> GetSignedUrlAsync(string key);

        /// <summary>
        /// FALLBACK: Finds a video URL by filename from /list-videos.
        /// Use if /signed-url endpoint isn't built yet.
        /// </summary>
        Task<string?> GetVideoUrlByFilenameAsync(string filename);

        /// <summary>
        /// FALLBACK: Finds a thumbnail URL by filename from /list-videos.
        /// Use if /signed-url endpoint isn't built yet.
        /// </summary>
        Task<string?> GetThumbnailUrlByFilenameAsync(string filename);
    }
}