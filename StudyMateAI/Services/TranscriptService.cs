using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.ClosedCaptions;

namespace StudyMateAI.Services
{
    public class TranscriptService
    {
        private readonly YoutubeClient _youtubeClient;

        public TranscriptService()
        {
            _youtubeClient = new YoutubeClient();
        }

        public async Task<string?> GetTranscriptAsync(string videoId)
        {
            try
            {
                // Get the manifest of available tracks
                var trackManifest = await _youtubeClient.Videos.ClosedCaptions.GetManifestAsync(videoId);

                // Try to find a track in Turkish first, then English, then auto-generated
                var trackInfo = trackManifest.GetByLanguage("tr") ?? 
                                trackManifest.GetByLanguage("en") ?? 
                                trackManifest.Tracks.FirstOrDefault();

                if (trackInfo == null)
                    return null;

                // Download the closed caption track
                var track = await _youtubeClient.Videos.ClosedCaptions.GetAsync(trackInfo);

                // Combine captions into a single string with timestamps
                var sb = new StringBuilder();
                foreach (var caption in track.Captions)
                {
                    // Format: [00:00:15] This is the text
                    sb.AppendLine($"[{caption.Offset:hh\\:mm\\:ss}] {caption.Text}");
                }

                return sb.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
