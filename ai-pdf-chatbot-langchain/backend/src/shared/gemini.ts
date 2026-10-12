export const GEMINI_BASE_URL = 'https://generativelanguage.googleapis.com/v1beta/openai/';

export function geminiApiKey(): string {
  const key = process.env.GEMINI_API_KEY?.trim();
  if (!key) throw new Error('Chưa cấu hình GEMINI_API_KEY trong backend/.env.');
  return key;
}
