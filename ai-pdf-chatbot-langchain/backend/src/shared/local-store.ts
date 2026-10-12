import fs from 'node:fs/promises';
import path from 'node:path';
import { MemoryVectorStore } from 'langchain/vectorstores/memory';
import { OpenAIEmbeddings } from '@langchain/openai';
import { Document } from '@langchain/core/documents';

const storePath = path.resolve(process.env.LOCAL_VECTOR_PATH || './data/documents.json');

class LocalVectorStore extends MemoryVectorStore {
  async addVectors(vectors: number[][], documents: Document[]): Promise<void> {
    await super.addVectors(vectors, documents);
    await fs.mkdir(path.dirname(storePath), { recursive: true });
    await fs.writeFile(storePath + '.tmp', JSON.stringify(this.memoryVectors));
    await fs.rename(storePath + '.tmp', storePath);
  }
}

export async function localRetriever(k: number, filter: Record<string, unknown>) {
  const embeddings = new OpenAIEmbeddings({
    model: process.env.OLLAMA_EMBEDDING_MODEL || 'nomic-embed-text',
    apiKey: 'ollama',
    batchSize: 8,
    configuration: { baseURL: `${process.env.OLLAMA_BASE_URL || 'http://127.0.0.1:11434'}/v1/` },
  });
  const store = new LocalVectorStore(embeddings);
  try {
    store.memoryVectors = JSON.parse(await fs.readFile(storePath, 'utf8'));
  } catch (error) {
    if ((error as NodeJS.ErrnoException).code !== 'ENOENT') throw error;
  }
  return store.asRetriever({
    k,
    filter: document => Object.entries(filter).every(([key, value]) => document.metadata[key] === value),
  });
}
