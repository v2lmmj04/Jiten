import { MediaType } from '~/types';

export function getMediaTypeText(mediaType: MediaType): string {
    switch (mediaType) {
        case MediaType.Anime:
            return 'Anime';
        case MediaType.Drama:
            return 'Drama';
        case MediaType.Movie:
            return 'Movie';
        case MediaType.Novel:
            return 'Novel';
        case MediaType.NonFiction:
            return 'Non-Fiction';
        case MediaType.VideoGame:
            return 'Video Game';
        case MediaType.VisualNovel:
            return 'Visual Novel';
        case MediaType.WebNovel:
            return 'Web Novel';
        default:
            return 'Unknown';
    }
}
