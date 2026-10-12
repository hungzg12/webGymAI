import { BaseChatModel } from '@langchain/core/language_models/chat_models';
import { initChatModel } from 'langchain/chat_models/universal';
import { ChatOpenAI } from '@langchain/openai';
import { GEMINI_BASE_URL, geminiApiKey } from './gemini.js';

const SUPPORTED_PROVIDERS = [
  'openai',
  'anthropic',
  'azure_openai',
  'cohere',
  'google-vertexai',
  'google-vertexai-web',
  'google-genai',
  'ollama',
  'together',
  'fireworks',
  'mistralai',
  'groq',
  'bedrock',
  'cerebras',
  'deepseek',
  'xai',
] as const;
/**
 * Load a chat model from a fully specified name.
 * @param fullySpecifiedName - String in the format 'provider/model' or 'provider/account/provider/model'.
 * @returns A Promise that resolves to a BaseChatModel instance.
 */
export async function loadChatModel(
  fullySpecifiedName: string,
  temperature: number = 0.2,
): Promise<BaseChatModel> {
  if (fullySpecifiedName.startsWith('ollama/')) {
    return new ChatOpenAI({
      model: fullySpecifiedName.slice('ollama/'.length),
      apiKey: 'ollama',
      temperature,
      configuration: { baseURL: `${process.env.OLLAMA_BASE_URL || 'http://127.0.0.1:11434'}/v1/` },
    });
  }
  if (fullySpecifiedName.startsWith('gemini/')) {
    return new ChatOpenAI({
      model: fullySpecifiedName.slice('gemini/'.length),
      apiKey: geminiApiKey(),
      temperature,
      configuration: { baseURL: GEMINI_BASE_URL },
    });
  }
  const index = fullySpecifiedName.indexOf('/');
  if (index === -1) {
    // If there's no "/", assume it's just the model
    if (
      !SUPPORTED_PROVIDERS.includes(
        fullySpecifiedName as (typeof SUPPORTED_PROVIDERS)[number],
      )
    ) {
      throw new Error(`Unsupported model: ${fullySpecifiedName}`);
    }
    return await initChatModel(fullySpecifiedName, {
      temperature: temperature,
    });
  } else {
    const provider = fullySpecifiedName.slice(0, index);
    const model = fullySpecifiedName.slice(index + 1);
    if (
      !SUPPORTED_PROVIDERS.includes(
        provider as (typeof SUPPORTED_PROVIDERS)[number],
      )
    ) {
      throw new Error(`Unsupported provider: ${provider}`);
    }
    return await initChatModel(model, {
      modelProvider: provider,
      temperature: temperature,
    });
  }
}
