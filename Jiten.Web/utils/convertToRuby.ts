export function convertToRuby(text: string): string {
    return text.replace(/([\u4E00-\u9FFF]+)\[([\u3040-\u309F]+)]/g, (_match, kanji, furigana) => {
        return `<ruby lang="ja">${kanji}<rp>(</rp><rt>${furigana}</rt><rp>)</rp></ruby>`;
    });

}
