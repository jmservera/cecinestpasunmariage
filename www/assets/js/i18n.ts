declare global {
  interface Window {
    translations: Record<string, string>;
    lang: string;
  }
}

function getTranslation(key: string): string {
  return window.translations[key] || key; // Fallback to key if translation is missing
}

export { getTranslation };
